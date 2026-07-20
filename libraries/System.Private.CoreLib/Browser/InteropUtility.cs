using NetJs;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
#pragma warning disable CS3003 // Type is not CLS-compliant
    public const uint virtualAddressOffset = 0x80000000;
    public const uint virtualBlockAddressOffset = 0x80000000;
    public const uint virtualObjectAddressOffset = 0xC0000000;
#pragma warning restore CS3003 // Type is not CLS-compliant
    static SimpleDictionary<Union<object?, WeakRef<object>?>> virtualAddressSpaces = new();

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
            virtualAddressSpaces[blockStart] = /*new WeakRef<object>*/(pointer);
            blockStart++;
            if (blocks > 0)
                pointer = pointer.Add(virtualAddressSpaceSlotSize);
        }
    }

    static void markAddressSpaceUsed(uint blockStart, object? obj)
    {
        //pointerFinalizer.register(pointer, { start, blocks });
        virtualAddressSpaces[blockStart] = obj == null ? null : /*new WeakRef<object>*/(obj);
    }

    public static uint castObject2Address(object? obj, uint handle = 0, bool deleteOld = false)
    {
        var add = obj != null ? obj["$virtualAddress"] : Script.Undefined;
        if (NetJs.Script.IsDefined(add))
            return add.As<uint>();
        if (handle != 0)
        {
            handle = NetJs.Script.AsUnsigned(handle);
            if (handle < virtualObjectAddressOffset)
            {
                throw new InvalidOperationException("Not an object virtual address");
            }
            handle -= virtualObjectAddressOffset;
        }
        uint freeAddressSpace = handle == 0 ? getContaguousAddressSpace(1) : handle;
        if (!deleteOld && handle == 0 && virtualAddressSpaces.ContainsKey(freeAddressSpace))
        {
            throw new InvalidOperationException();
        }
        markAddressSpaceUsed(freeAddressSpace, obj);
        virtualAddressSpaces[freeAddressSpace] = obj == null ? null : /*new WeakRef<object>*/(obj);
        if (obj != null && NetJs.Script.TypeOf(obj).NativeNotEquals("string"))
            obj["$virtualAddress"] = handle != 0 ? (handle + virtualObjectAddressOffset).As<object>() : freeAddressSpace.As<object>();
        return virtualObjectAddressOffset + freeAddressSpace;
    }

    public static object? castAddress2Object(uint address)
    {
        address = NetJs.Script.AsUnsigned(address);
        if (address < virtualObjectAddressOffset)
        {
            throw new InvalidOperationException("Not an object virtual address");
        }
        address -= virtualObjectAddressOffset;
        var data = virtualAddressSpaces[address];
        if (NetJs.Script.Write<bool>("data instanceof WeakRef"))
        {
            return data.As<WeakRef<object>>().deref();
        }
        return data;
    }

    public static uint castPtr2Address(RefOrPointer<object> pointer)
    {
        if (pointer._dataSource == null && pointer._setter == null && pointer._getter == null)
        {
            //if (pointer == Unsafe._nullRef)
            //{
            return 0;
            //}
            //if (pointer == RefOrPointer._pinnedPointer)
            //{
            return 1;
            //}
        }
        if (pointer._virtualAddress > 0)
            return pointer._virtualAddress;
        Array? array = null;
        var cur = pointer;
        var root = pointer;
        int byteOffset = 0;
        while (cur is not null)
        {
            root = cur;
            if (cur._parentRef != null && cur._byteOffset > 0)
            {
                byteOffset += cur._byteOffset ?? 0;
            }
            else if (cur._array != null)
            {
                byteOffset += cur._byteOffset ?? 0;
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
            var data = virtualAddressSpaces[block];
            RefOrPointer<object> ptr;
            if (NetJs.Script.Write<bool>("data instanceof WeakRef"))
            {
                ptr = data.As<WeakRef<object>>().deref().As<RefOrPointer<object>>();
            }
            else
            {
                ptr = data.As<RefOrPointer<object>>();
            }
            var toModel = ptrType != null ? ptrType.Arguments![0] : null;
            var fromModel = ptr == null ? null : ptr.GetClassPrototype().Arguments![0];
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
            object? v;
            if (NetJs.Script.Write<bool>("value instanceof WeakRef"))
            {
                v = value.As<WeakRef<object>>().deref();
            }
            else
            {
                v = value;
            }
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

    public static T[] ToArray<T>(IEnumerable<T> spreadItems)
    {
        T[] ts = NetJs.Script.NewArray<T>();
        Array.AddMetadata(ts, typeof(T));
        var enumerator = spreadItems.GetEnumerator();
        while (enumerator.MoveNext())
        {
            T current = enumerator.Current;
            ts.Push(current);
        }
        return ts;
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
