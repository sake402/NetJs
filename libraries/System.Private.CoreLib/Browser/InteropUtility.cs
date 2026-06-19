using NetJs;
using System;
using Window;

[NetJs.Boot]
[NetJs.OutputOrder(int.MinValue)]
[NetJs.Reflectable(false)]
public static class InteropUtility
{
    //Pointers are not numbers in NetJs
    //But we need an abstraction that let this be castable in both ways
    //We will map a pointer to a vrtual address space
    const uint virtualAddressSpaceSlotSize = 64 * 1024;
    public const uint virtualAddressOffset = 0x80000000;
    public const uint virtualBlockAddressOffset = 0x80000000;
    public const uint virtualObjectAddressOffset = 0xC0000000;
    static SimpleDictionary<WeakRef<object>> virtualAddressSpaces = new();

    static InteropUtility()
    {
        Global.SetInterval(cleanupVirtualAddresses, 10000);
    }

    public static bool IsVirtualAddress(uint address) => (address & virtualAddressOffset) != 0;
    static void freeAddressSpace(uint start, uint blocks)
    {
        while (blocks-- > 0)
        {
            NetJs.Script.Delete(virtualAddressSpaces[start]);
            start++;
        }
    }

    //const pointerFinalizer = new FinalizationRegistry((startBlocks) => {
    //    freeAddressSpace(startBlock.start, startBlock.blocks);
    //});

    static bool isFreeAddressSpace(uint n)
    {
        virtualAddressSpaces ??= new();
        var v = virtualAddressSpaces[n];
        return v == NetJs.Script.Undefined;
    }

    static uint getContaguousAddressSpace(uint blocks)
    {
        uint start = 0;
        uint nBlock = blocks;
        while (true)
        {
            if (isFreeAddressSpace(start))
            {
                nBlock--;
            }
            else
            {
                nBlock = blocks;
            }
            start++;
            if (nBlock == 0)
                return start - blocks;
        }
    }

    static void markAddressSpaceUsed(uint blockStart, uint blocks, RefOrPointer<object> pointer)
    {
        //pointerFinalizer.register(pointer, { start, blocks });
        while (blocks-- > 0)
        {
            virtualAddressSpaces[blockStart] = new WeakRef<object>(pointer);
            blockStart++;
            if (blocks > 0)
                pointer = pointer.Add(virtualAddressSpaceSlotSize);
        }
    }

    static void markAddressSpaceUsed(uint blockStart, object obj)
    {
        //pointerFinalizer.register(pointer, { start, blocks });
        virtualAddressSpaces[blockStart] = new WeakRef<object>(obj);
    }

    public static uint castObject2Address(object obj, uint handle = 0, bool deleteOld = false)
    {
        var add = obj["$virtualAddress"];
        if (NetJs.Script.IsDefined(add))
            return add.As<uint>();
        uint freeAddressSpace = handle == 0 ? getContaguousAddressSpace(1) : handle;
        if (!deleteOld && virtualAddressSpaces.ContainsKey(freeAddressSpace))
        {
            throw new InvalidOperationException();
        }
        markAddressSpaceUsed(freeAddressSpace, obj);
        virtualAddressSpaces[freeAddressSpace] = new WeakRef<object>(obj);
        obj["$virtualAddress"] = freeAddressSpace.As<object>();
        return virtualObjectAddressOffset + freeAddressSpace;
    }

    public static object castAddress2Object(uint address)
    {
        address = NetJs.Script.AsUnsigned(address);
        if (address < virtualObjectAddressOffset)
        {
            throw new InvalidOperationException("Not an object virtual address");
        }
        address -= virtualObjectAddressOffset;
        return virtualAddressSpaces[address].deref();
    }

    public static uint castPtr2Address(RefOrPointer<object> pointer)
    {
        if (pointer._virtualAddress > 0)
            return pointer._virtualAddress;
        Array? array = null;
        var cur = pointer;
        var root = pointer;
        int byteOffset = 0;
        while (cur is not null)
        {
            root = cur;
            if (cur._parentRef != null && cur._arrayOffset > 0)
            {
                byteOffset += cur._byteOffset;
            }
            else if (cur._array != null)
            {
                byteOffset += cur._byteOffset;
                array = cur._array;
                break;
            }
            else if (cur._object != null)
            {
                return castObject2Address(cur._object);
            }
            if (cur._parentRef != null)
                cur = cur._parentRef.As<RefOrPointer<object>>();
        }
        if (array != null)
        {
            if (root._virtualAddress == 0)
            {
                uint len = array.Length.As<uint>();
                if (len == 0)
                    len = 1;
                uint addressSpaces = ((len - 1) / virtualAddressSpaceSlotSize) + 1;
                uint freeBlockAddressSpace = getContaguousAddressSpace(addressSpaces.As<uint>());
                markAddressSpaceUsed(freeBlockAddressSpace, addressSpaces.As<uint>(), root);
                root._virtualAddress = virtualBlockAddressOffset + (freeBlockAddressSpace * virtualAddressSpaceSlotSize);
            }
            return pointer._virtualAddress = root._virtualAddress + byteOffset.As<uint>();
        }
        return castObject2Address(pointer);
    }

    public static RefOrPointer<object> castAddress2Ptr(uint address, TypePrototype? ptrType = null)
    {
        if (address < virtualBlockAddressOffset)
        {
            throw new InvalidOperationException("Not a block virtual address");
        }
        unchecked
        {
            address -= virtualBlockAddressOffset;
            var block = address / virtualAddressSpaceSlotSize;
            var ptr = virtualAddressSpaces[block].deref().As<RefOrPointer<object>>();
            var toModel = ptrType != null ? ptrType.Arguments![0] : null;
            var fromModel = ptr.GetClassPrototype().Arguments![0];
            if (fromModel != null && toModel != null)
            {
                //If both are numeric type, create a new TTo ref such that it can read from the TFrom ref
                if (toModel.KnownType.IsNumeric() && fromModel.KnownType.IsNumeric())
                {
                    var toSize = toModel.Size;
                    var fromSize = fromModel.Size;
                    if (NetJs.Script.IsDefined(fromSize) && NetJs.Script.IsDefined(toSize))
                    {
                        if (fromSize != toSize)
                        {
                            //var mreff = new Ref<TTo>(reff);
                            var mreff = NetJs.Script.Write<Ref<object>>("new ($.$spc.System.Ref$$(ptrType.$args[0]))().$ctor$5(ptr)");
                            //mreff._type = ptrType!.Type;
                            return mreff.As<Ref<object>>();
                        }
                    }
                }
            }
            return ptr;
        }
    }


    public static void free(uint address)
    {
        virtualAddressSpaces.Remove(address);
    }

    static void cleanupVirtualAddresses()
    {
        virtualAddressSpaces.ForEach((key, value) =>
        {
            var v = value.deref();
            if (NetJs.Script.IsUndefined(v))
            {
                virtualAddressSpaces.Remove(key);
            }
        });
    }

    public static int IntegerChecked(int value, int signed)
    {
        if (signed == 0)
        {
            if (value >= 0 && value.As<uint>() <= uint.MaxValue)
                return value;
        }
        else
        {
            if (value >= int.MinValue && value <= int.MaxValue)
                return value;
        }
        throw new OverflowException();
    }


    //public static int IntegerWrap(int value, int signed)
    //{
    //    if (signed == 0)
    //    {
    //        return value.As<uint>() & 0xFFFFFFFF;
    //    }
    //    else
    //    {
    //        return value & 0xFFFFFFFF;
    //    }
    //}
}
