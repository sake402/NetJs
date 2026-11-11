using NetJs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;
using Window;

[NetJs.Boot]
[NetJs.OutputOrder(int.MinValue)]
[NetJs.Reflectable(false)]
[NetJs.Name(Constants.InteropUtilityName)]
public static class InteropUtility
{
    //class VirtualAddressManager
    //{
    //    Map<uint, Union<object?, WeakRef<object>?>> virtualAddressSpaces = new();
    //    int[] allocated = [];// Array.from(virtualAddressSpaces.keys()).sort<int>((a, b) => a - b);
    //    public int[] Allocated => allocated;
    //    public void Set(uint key, Union<object?, WeakRef<object>?> value)
    //    {
    //        virtualAddressSpaces.set(key, value);
    //        allocated = Array.from(virtualAddressSpaces.keys()).sort<int>((a, b) => a - b);
    //    }
    //    public Union<object?, WeakRef<object>?> Get(uint key)
    //    {
    //        return virtualAddressSpaces.get(key);
    //    }
    //    public bool Has(uint key)
    //    {
    //        return virtualAddressSpaces.has(key);
    //    }
    //    public void Remove(uint key)
    //    {
    //        virtualAddressSpaces.delete(key);
    //        allocated = Array.from(virtualAddressSpaces.keys()).sort<int>((a, b) => a - b);
    //    }
    //    public void ForEach(Action<Union<object?, WeakRef<object>?>, uint> action)
    //    {
    //        virtualAddressSpaces.forEach((value, key) =>
    //        {
    //            action(value, key);
    //        });
    //    }
    //}

    class VirtualAddressStore
    {
        const int BLOCKS = 200000;
        //Map<uint, bool> freeBlocks = new();
        uint[] freeBlocks = new Window.Uint32Array(BLOCKS / 32).fill(-1).As<uint[]>();// NetJs.Script.NewArray<uint>(BLOCKS / 32);
        Map<uint, Union<object?, WeakRef<object>?>> usedBlocks = new();

        //Map<uint, Union<object?, WeakRef<object>?>> virtualAddressSpaces = new();
        //// Kept strictly sorted at all times
        //int[] allocated = NetJs.Script.NewArray<int>();
        //public int[] Allocated => allocated;

        public VirtualAddressStore()
        {
            unchecked
            {
                for (uint i = 0; i < BLOCKS; i++)
                {
                    //freeBlocks.set(i, true);
                    usedBlocks.set(i, null!);
                }
            }
        }

        public void Set(uint blockNumber, Union<object?, WeakRef<object>?> value)
        {
            unchecked
            {
                freeBlocks[blockNumber / 32] &= ~((1U << (blockNumber.As<int>() % 32)) >>> 0);
                var existing = usedBlocks.get(blockNumber) ?? null;
                //if (existing != null)
                //{
                //    object? val;
                //    object? ex;
                //    if (NetJs.Script.Write<bool>("value instanceof WeakRef"))
                //    {
                //        val = value.As<WeakRef<object>>().deref() ?? null;
                //    }
                //    else
                //    {
                //        val = value;
                //    }
                //    if (NetJs.Script.Write<bool>("existing instanceof WeakRef"))
                //    {
                //        ex = existing.As<WeakRef<object>>().deref() ?? null;
                //    }
                //    else
                //    {
                //        ex = existing;
                //    }
                //    if (ex != val)
                //    {
                //        Debugger.Break();
                //    }
                //}
                //freeBlocks.set(blockNumber, false);
                usedBlocks.set(blockNumber, value);
            }
            //// Only modify our sorted tracker if this is a brand NEW allocation
            //if (!virtualAddressSpaces.has(blockNumber))
            //{
            //    int intKey = blockNumber.As<int>();
            //    InsertSorted(intKey);
            //}

            //virtualAddressSpaces.set(blockNumber, value);
            //var number = new Window.Number(blockNumber);
        }

        public void Remove(uint blockNumber)
        {
            unchecked
            {
                freeBlocks[blockNumber / 32] |= (1U << (blockNumber.As<int>() % 32));
            }
            //freeBlocks.set(blockNumber, true);
            usedBlocks.set(blockNumber, null!);
            //nextFreeBlock = blockNumber;
            //if (virtualAddressSpaces.has(blockNumber))
            //{
            //    int intKey = blockNumber.As<int>();
            //    RemoveSorted(intKey);
            //    virtualAddressSpaces.delete(blockNumber);
            //}
        }

        public Union<object?, WeakRef<object>?> Get(uint blockNumber)
        {
            return usedBlocks.get(blockNumber);
            //return virtualAddressSpaces.get(blockNumber);
        }

        public bool IsFree(uint blockNumber)
        {
            unchecked
            {
                return (freeBlocks[blockNumber / 32] & (1U << (blockNumber.As<int>() % 32))) != 0;
            }
            //return freeBlocks.get(blockNumber);
            //return virtualAddressSpaces.has(blockNumber);
        }

        public void ForEach(NativeAction<Union<object?, WeakRef<object>?>, uint> action)
        {
            usedBlocks.forEach(action);
            //virtualAddressSpaces.forEach(action);
        }

        public uint GetFreeBlock(uint blocks)
        {
            unchecked
            {
                // --- OPTIMIZATION A: Lightning Fast Path for Single Block Allocations ---
                if (blocks == 1)
                {
                    int wordCount = freeBlocks.Length;
                    for (int i = 0; i < wordCount; i++)
                    {
                        uint word = freeBlocks[i];
                        if (word != 0) // 0 means all 32 blocks are full.
                        {
                            // Find the index of the lowest set bit (the first 1 bit) using CPU hardware intrinsics
                            uint lowestSetBit = word & ((~word) + 1);
                            int bitIdx = 31 - Math.clz32(lowestSetBit);

                            // Unset the bit (flip from 1 to 0) to mark it as allocated
                            freeBlocks[i] = word ^ (1U << bitIdx);

                            var free = (i * 32).As<uint>() + bitIdx.As<uint>();

                            var address = virtualAddressOffset + (free * virtualAddressSpaceSlotSize);
                            if (address == 0)
                            {
                                Debugger.Break();
                            }
                            return free;
                        }
                    }
                    throw new InvalidOperationException("Out of memory blocks");
                }

                // --- OPTIMIZATION B: Contiguous Multi-Block Memory Scanner ---
                uint contiguousCount = 0;
                uint startBlock = 0;

                for (uint i = 0; i < BLOCKS; i++)
                {
                    // Skip 32 fully allocated blocks instantly if we aren't currently tracking a continuous sequence
                    if (contiguousCount == 0 && (i & 31) == 0)
                    {
                        uint word = freeBlocks[i >> 5];
                        if (word == 0) // 0 means all 32 blocks are completely filled with 0s (used)
                        {
                            i += 31; // Leap ahead across this word boundary
                            continue;
                        }
                    }

                    // Direct Bit Extraction
                    uint wordIdx = i >> 5;
                    int bitIdx = (i & 31).As<int>();
                    bool isFree = (freeBlocks[wordIdx] & (1U << bitIdx)) != 0;

                    if (isFree)
                    {
                        if (contiguousCount == 0) startBlock = i;
                        contiguousCount++;

                        if (contiguousCount >= blocks)
                        {
                            // Inlined bit-flipping loop: Mark blocks as used (0) directly from startBlock to i
                            for (uint j = startBlock; j <= i; j++)
                            {
                                int allocWordIdx = (j >> 5).As<int>();
                                int allocBitIdx = (j & 31).As<int>();
                                freeBlocks[allocWordIdx] &= ~(1U << allocBitIdx);
                            }
                            return startBlock;
                        }
                    }
                    else
                    {
                        // Broken chain, reset continuous block tracker
                        contiguousCount = 0;
                    }
                }
            }
            throw new InvalidOperationException("Out of memory blocks");
        }

        //uint nextFreeBlock = 0;
        //public uint GetFreeBlock(uint blocks)
        //{
        //    uint blockNumber = 0;

        //    for (uint block = 0; block < BLOCKS/32; block++)
        //    {
        //        if (freeBlocks[block] == 0)
        //            continue;
        //        for (int i = 0; i < 32; i++)
        //        {
        //            if (block - blockNumber >= blocks)
        //            {
        //                return blockNumber.As<uint>();
        //            }
        //            var free = (freeBlocks[block / 32] & (1U << (block.As<int>() % 32))) != 0;
        //            if (!freeBlocks.get(block))
        //                blockNumber = block + 1;
        //        }
        //    }

        //    throw null!;
        //}
        //// --- High-Performance Binary Search Utilities ---

        //private void InsertSorted(int item)
        //{
        //    int low = 0;
        //    int high = allocated.Length - 1;
        //    int index = allocated.Length; // Default to appending at the end
        //    unchecked
        //    {
        //        while (low <= high)
        //        {
        //            int mid = (low + high) >>> 1;
        //            if (allocated[mid] >= item)
        //            {
        //                index = mid;
        //                high = mid - 1;
        //            }
        //            else
        //            {
        //                low = mid + 1;
        //            }
        //        }
        //    }
        //    // Fast splice: Insert the item at the exact index to maintain sorting
        //    allocated.splice(index, 0, item);
        //}

        //private void RemoveSorted(int item)
        //{
        //    int low = 0;
        //    int high = allocated.Length - 1;

        //    unchecked
        //    {
        //        while (low <= high)
        //        {
        //            int mid = (low + high) >>> 1;
        //            if (allocated[mid] == item)
        //            {
        //                // Found it! Pluck it out of the array
        //                allocated.splice(mid, 1);
        //                return;
        //            }
        //            else if (allocated[mid] > item)
        //            {
        //                high = mid - 1;
        //            }
        //            else
        //            {
        //                low = mid + 1;
        //            }
        //        }
        //    }
        //}
    }

    //Pointers are not numbers in NetJs
    //But we need an abstraction that let this be castable in both ways
    //We will map a pointer to a virtual address space
    const uint virtualAddressSpaceSlotSize = 1 * 1024;
#pragma warning disable CS3003 // Type is not CLS-compliant
    [Name(Constants.VirtualAddressOffset)]
    public const uint virtualAddressOffset = 0x80000000;
    //public const uint virtualBlockAddressOffset = 0x80000000;
    //public const uint virtualObjectAddressOffset = 0xC0000000;
#pragma warning restore CS3003 // Type is not CLS-compliant
    //static SimpleDictionary<Union<object?, WeakRef<object>?>> virtualAddressSpaces = new();
    static VirtualAddressStore virtualAddressStore = new();
    static Symbol handleValue = new();
    static InteropUtility()
    {
        Global.SetInterval(cleanupVirtualAddresses, 10000);
    }

    public static bool IsVirtualAddress(uint address) => (address & virtualAddressOffset) != 0;
    static void freeAddressSpace(uint start, uint blocks)
    {
        while (blocks-- > 0)
        {
            //NetJs.Script.Delete(virtualAddressSpaces[start]);
            virtualAddressStore.Remove(start);
            start++;
        }
    }

    //const pointerFinalizer = new FinalizationRegistry((startBlocks) => {
    //    freeAddressSpace(startBlock.start, startBlock.blocks);
    //});

    //static bool isFreeAddressSpace(uint n)
    //{
    //    return !virtualAddressSpaces.has(n);
    //    //virtualAddressSpaces ??= new();
    //    //var v = virtualAddressSpaces[n];
    //    //return v == NetJs.Script.Undefined;
    //}

    //static uint getContaguousAddressSpace(uint blocks)
    //{
    //    // 1. Get all allocated addresses and sort them numerically
    //    var allocated = virtualAddressStore.Allocated;// Array.from(virtualAddressSpaces.keys()).sort<int>((a, b) => a - b);

    //    int blockNumber = 0;

    //    unchecked
    //    {
    //        for (int i = 0; i < allocated.Length; i++)
    //        {
    //            var allocatedAddr = allocated[i];

    //            // If there's enough space between our candidate start and the next allocated address
    //            if (allocatedAddr - blockNumber >= blocks.As<int>())
    //            {
    //                return blockNumber.As<uint>();
    //            }

    //            // Otherwise, the next possible start is immediately after this allocated address
    //            blockNumber = allocatedAddr + 1;
    //        }
    //    }

    //    // If we checked all allocations and found no fit, return the space after the last allocation
    //    return blockNumber.As<uint>();
    //    //uint start = 0;
    //    //uint @checked = 0;
    //    //while (@checked < blocks)
    //    //{
    //    //    // Look ahead at the current offset
    //    //    if (virtualAddressSpaces.has(start + @checked))
    //    //    {
    //    //        // Collision found! Skip 'start' completely past this blocked address
    //    //        start = start + @checked + 1;
    //    //        @checked = 0; // Reset consecutive block counter
    //    //    }
    //    //    else
    //    //    {
    //    //        @checked++; // This block is free, check the next one
    //    //    }
    //    //}
    //    //return start;

    //    //uint nBlock = blocks;
    //    //while (true)
    //    //{
    //    //    if (!virtualAddressSpaces.has(start))
    //    //    {
    //    //        nBlock--;
    //    //    }
    //    //    else
    //    //    {
    //    //        nBlock = blocks;
    //    //    }
    //    //    start++;
    //    //    if (nBlock == 0)
    //    //        return start - blocks;
    //    //}
    //}

    static void markAddressSpaceUsed(uint blockNumber, uint blocks, RefOrPointer<object> pointer, bool weakReference)
    {
        //pointerFinalizer.register(pointer, { start, blocks });
        while (blocks-- > 0)
        {
            //virtualAddressSpaces[blockStart] = /*new WeakRef<object>*/(pointer);
            virtualAddressStore.Set(blockNumber, weakReference ? new WeakRef<object>(pointer) : pointer);
            blockNumber++;
            //if (blocks > 0)
            //pointer = pointer.Add(virtualAddressSpaceSlotSize);
        }
    }

    static void markAddressSpaceUsed(uint blockNumber, object? obj, bool weakReference)
    {
        //pointerFinalizer.register(pointer, { start, blocks });
        //virtualAddressSpaces[blockStart] = obj == null ? null : /*new WeakRef<object>*/(obj);
        virtualAddressStore.Set(blockNumber, obj == null ? null : weakReference ? new WeakRef<object>(obj) : obj);
    }

    internal static uint attachValueToHandle(uint handle, object? value)
    {
        if (handle < virtualAddressOffset)
        {
            Debugger.Break();
        }
        var number = new Window.Number(handle);
        number[handleValue] = value;
        return number.As<uint>();
    }

    public static uint castObject2Address(object? obj, uint handle = 0, bool overwriteExisting = false, bool weakReference = true, uint blocks = 1)
    {
        if (NetJs.Script.IsUndefined(blocks))
            blocks = 1;
        //cleanupVirtualAddresses();
        var add = obj != null ? obj[Constants.VirtualAddress] : Script.Undefined;
        if (NetJs.Script.IsDefined(add) && NetJs.Script.TypeOf(add).NativeNotEquals("function"))
            return add.As<uint>();
        uint virtualAddress;
        if (handle != 0)
        {
            handle = NetJs.Script.AsUnsigned(handle);
            if (handle < virtualAddressOffset)
            {
                throw new InvalidOperationException("Not an object virtual address");
            }
            //handle -= virtualAddressOffset;
            var block = handle / virtualAddressSpaceSlotSize;
            if (!overwriteExisting && !virtualAddressStore.IsFree(block))
            {
                throw new InvalidOperationException();
            }
            markAddressSpaceUsed(block, obj, weakReference);
            virtualAddress = handle;
        }
        else
        {
            uint freeBlockNumber = virtualAddressStore.GetFreeBlock(blocks);
            if (blocks > 1)
            {
                markAddressSpaceUsed(freeBlockNumber, blocks, obj.As<RefOrPointer<object>>(), weakReference);
            }
            else
            {
                markAddressSpaceUsed(freeBlockNumber, obj, weakReference);
            }
            virtualAddress = freeBlockNumber * virtualAddressSpaceSlotSize;
        }
        var addr = attachValueToHandle(handle != 0 ? handle : virtualAddressOffset + virtualAddress, obj);
        if (obj != null && NetJs.Script.TypeOf(obj).NativeNotEquals("string"))
            obj[Constants.VirtualAddress] = addr.As<object>();
        return addr;
    }

    public static object? castAddress2Object(uint address)
    {
        if (NetJs.Script.TypeOf(address).NativeEquals("object"))
        {
            var value = address[handleValue];
            if (NetJs.Script.IsDefined(value))
                return value;
        }
        //cleanupVirtualAddresses();
        address = NetJs.Script.AsUnsigned(address);
        if (address < virtualAddressOffset)
        {
            throw new InvalidOperationException("Not an object virtual address");
        }
        address -= virtualAddressOffset;
        var blockNumber = address / virtualAddressSpaceSlotSize;
        //var blockAddress = virtualAddressSpaceSlotSize * (address / virtualAddressSpaceSlotSize);
        var offsetInBlock = address % virtualAddressSpaceSlotSize;
        var data = virtualAddressStore.Get(blockNumber);
        if (NetJs.Script.Write<bool>("data instanceof WeakRef"))
        {
            return data.As<WeakRef<object>>().deref();
        }
        return data;
    }

    [Name(Constants.PointerToAddress)]
    public static uint castPtr2Address(RefOrPointer pointer)
    {
        //cleanupVirtualAddresses();
        if (pointer._dataSource == null &&
            pointer.As<RefOrPointer<object>>()._setter == null &&
            pointer.As<RefOrPointer<object>>()._getter == null)
        {
            //if (pointer == Unsafe._nullRef)
            //{
            return 0;
            //}
            //if (pointer == RefOrPointer._pinnedPointer)
            //{
            //return 1;
            //}
        }
        var addr = pointer.GetVirtualAddress();
        if (addr > 0)
            return addr;
        Array? array = null;
        object? _object = null;
        RefOrPointer root = pointer;
        int byteOffset = 0;
        if (pointer._root != null)
        {
            root = pointer._root;
            byteOffset += pointer._byteOffset ?? 0;
            array = pointer._root.As<RefOrPointer<object>>()._array;
            _object = pointer._root.As<RefOrPointer<object>>()._object;
        }
        else
        {
            var cur = pointer.As<RefOrPointer<object>>();
            while (cur is not null)
            {
                root = cur;
                byteOffset += cur._byteOffset ?? 0;
                if (cur._array != null)
                {
                    array = cur._array;
                    break;
                }
                else if (cur._object != null)
                {
                    if (NetJs.Script.TypeOf(cur._object).NativeEquals("object"))
                        _object = cur._object;
                    break;
                }
                cur = cur._parentRef.As<RefOrPointer<object>>();
            }
        }
        //if (root._byteOffset != 0) //Not really a root then, is it?
        //{
        //    Debugger.Break();
        //}
        if (root.GetVirtualAddress() == 0)
        {
            uint len = array?.Length.As<uint>() ?? 0;
            if (len == 0)
                len = 1;
            uint addressSpaces = ((len - 1) / virtualAddressSpaceSlotSize) + 1;
            if (array != null || _object != null)
            {
                var add = (array ?? _object)![Constants.VirtualAddress];
                if (NetJs.Script.IsDefined(add))
                {
                    if (root != pointer)
                    {
                        root.SetVirtualAddress(attachValueToHandle(add.As<uint>(), root));
                        var block = (add.As<uint>() - virtualAddressOffset) / virtualAddressSpaceSlotSize;
                        markAddressSpaceUsed(block, root, true);
                    }
                    var addd = attachValueToHandle(add.As<uint>() + byteOffset.As<uint>(), pointer);
                    pointer.SetVirtualAddress(addd);
                    return addd;
                }
            }
            var address = castObject2Address(root, blocks: addressSpaces);
            //uint freeBlockAddressSpace = getContaguousAddressSpace(addressSpaces.As<uint>());
            //markAddressSpaceUsed(freeBlockAddressSpace, addressSpaces.As<uint>(), root, true);
            root.SetVirtualAddress(address);
            if (array != null || _object != null)
            {
                (array ?? _object)![Constants.VirtualAddress] = root.GetVirtualAddress().As<object>();
            }
        }
        //else
        //{
        //    //markAddressSpaceUsed(freeBlockNumber, blocks, obj.As<RefOrPointer<object>>(), weakReference);
        //}
        addr = attachValueToHandle(root.GetVirtualAddress() + byteOffset.As<uint>(), pointer);
        if (root != pointer)
            pointer.SetVirtualAddress(addr);
        return addr;
    }

    [Name(Constants.AddressToPointer)]
    public static RefOrPointer<object>? castAddress2Ptr(uint address, TypePrototype? ptrType = null)
    {
        unchecked
        {
            if (address == 0)
                return RefOrPointer<object>._pinnedPointer;
            var data = castAddress2Object(address);// virtualAddressStore.Get(block);
            RefOrPointer<object> ptr;
            if (NetJs.Script.Write<bool>("data instanceof WeakRef"))
            {
                ptr = data.As<WeakRef<object>>().deref().As<RefOrPointer<object>>();
            }
            else
            {
                ptr = data.As<RefOrPointer<object>>();
            }
            if (NetJs.Script.IsDefined(ptr))
            {
                address = NetJs.Script.AsUnsigned(address);
                if (ptr.GetVirtualAddress() != 0)
                {
                    var offsetFromPtr = address - ptr.GetVirtualAddress();
                    if (offsetFromPtr != 0)
                        ptr = ptr.AddByteOffset(offsetFromPtr.As<int>());
                }
                else
                {
                    var offsetInBlock = address % virtualAddressSpaceSlotSize;
                    if (offsetInBlock != 0)
                        ptr = ptr.AddByteOffset(offsetInBlock.As<int>());
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
                                var mreff = NetJs.Script.Write<Ref<object>>($"new ($.{Constants.SystemPrivateCoreLibSlug}.System.Ref$$(ptrType.$args[0]))().$ctor$5(ptr)");
                                //mreff._type = ptrType!.Type;
                                return mreff.As<Ref<object>>();
                            }
                        }
                    }
                }
            }
            return ptr ?? throw null!;
        }
    }


    public static void free(uint address)
    {
        //virtualAddressSpaces.Remove(address);
        virtualAddressStore.Remove(address);
    }

    static void cleanupVirtualAddresses()
    {
        virtualAddressStore.ForEach((value, key) =>
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
                //virtualAddressSpaces.Remove(key);
                virtualAddressStore.Remove(key);
            }
        });
    }

    [Name(Constants.IntegerCheckedName)]
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

    [Name(Constants.ToArrayName)]
    public static T[] ToArray<T>(IEnumerable<T> spreadItems)
    {
        T[] ts = NetJs.Script.NewArray<T>();
        Array.AddMetadata(ts, typeof(T));
        var enumerator = spreadItems.GetEnumerator();
        while (enumerator.MoveNext())
        {
            T current = enumerator.Current;
            ts.push(current);
        }
        return ts;
    }

    [Name(Constants.GetCoreAssemblyName)]
    static RuntimeAssembly GetCoreAssembly()
    {
        var spcAssembly = AppDomain.GlobalAssemblyRegistry["NetJs.System.Private.CoreLib"];
        return spcAssembly;
    }

    [Name(Constants.TypeArrayName)]
    public static TypePrototype ArrayType(TypePrototype? type)
    {
        var array = typeof(Array<>).As<RuntimeType>()._prototypeProvider;
        if (type == null)
            return array.As<TypePrototype>();
        return array!(type);
    }

    [Name(Constants.TypeRefOrPointerName)]
    public static TypePrototype RefOrPointerType(TypePrototype? type)
    {
        var pointer = typeof(RefOrPointer).As<RuntimeType>()._prototype;
        return pointer;
    }

    [Name(Constants.TypePointerName)]
    public static TypePrototype PointerType(TypePrototype? type)
    {
        var pointer = typeof(Pointer<>).As<RuntimeType>()._prototypeProvider;
        if (type == null)
            return pointer.As<TypePrototype>();
        return pointer!(type);
    }

    [Name(Constants.TypeNullableName)]
    public static TypePrototype NullableType(TypePrototype? type)
    {
        var nullable = typeof(Nullable<>).As<RuntimeType>()._prototypeProvider;
        if (type == null)
            return nullable.As<TypePrototype>();
        return nullable!(type);
    }

    [Name(Constants.CreateArrayName)]
    public static Array NewArray(TypePrototype type, int length)
    {
        return Array.CreateFromScript(type.Type.As<RuntimeType>(), length);
    }

    [NetJs.NativeDelegate]
    [NetJs.External]
    delegate void CallDeconstructWithRefs(
        ref object? o1, ref object? o2, ref object? o3, ref object? o4,
        ref object? o5, ref object? o6, ref object? o7, ref object? o8,
        ref object? o9, ref object? o10, ref object? o11, ref object? o12,
        ref object? o13, ref object? o14, ref object? o15, ref object? o16);

    [NetJs.Name(Constants.DestructureName)]
    public static Array Destructure(object tuple, int args, [Name("...refs")][Spread] RefOrPointer<object?>[] refs)
    {
        if (!NetJs.Script.IsDefined(refs))
        {
            refs = NetJs.Script.NewArray<RefOrPointer<object?>>();
        }
        object?[] o = NetJs.Script.NewArray<object?>(16);
        var tuplePrototype = tuple.GetClassPrototype();
        if (tuplePrototype == Object.Self)
        {
            tuplePrototype = null;
        }
        string? deconstructName = "Deconstruct";
        var deconstructMethod = tuplePrototype?.Metadata?.Methods?.filter(m => m.Name == "Deconstruct" && m.Parameters?.Length == args).ArraySingleOrDefault() ??
            tuplePrototype?.Metadata?.Methods?.filter(m => m.Name == "Deconstruct").ArraySingleOrDefault();
        if (deconstructMethod != null)
        {
            deconstructName = deconstructMethod.GetOutputName();
        }
        var tupleDeconstructMethod = tuple[deconstructName].As<NativeAction>();
        if (NetJs.Script.IsDefined(tupleDeconstructMethod))
        {
            if (refs.Length > 0)
            {
                tupleDeconstructMethod.InvokeApply(tuple, refs);
            }
            else
            {
                unchecked
                {
                    for (int i = 0; i < 16; i++)
                    {
                        o[i] = null;
                    }
                    var mrefs = o.map((oo, i, all) =>
                    {
                        return NetJs.Script.CreateReference<object?>(() => all[i], (v) => all[i] = v);
                    });
                    tupleDeconstructMethod.InvokeApply(tuple, mrefs);
                }
            }
        }
        else
        {
            var tuple8Prototype = typeof(ValueTuple<,,,,,,,>).As<RuntimeType>()._prototype;
            var fields = tuple8Prototype.Metadata!.Fields!;
            for (int i = 0; ; i++)
            {
                unchecked
                {
                    var propertyName = fields[i].GetOutputName();
                    var value = tuple[propertyName];
                    if (!(NetJs.Script.IsUndefined(value)))
                    {
                        if (refs.Length > 0)
                        {
                            refs[i].Value = value;
                        }
                        else
                            o[i] = value;
                    }
                    else
                        break;
                }
            }
        }
        return o;
    }

    [NetJs.Name(Constants.PrototypeImplementsInterface)]
    public static bool ImplementsInterface(TypePrototype prototype, TypePrototype targetInterface)
    {
        if (targetInterface.Kind != TypeKindModel.Interface)
        {
            return false;
        }
        var interfaces = prototype.AllInterfaces;
        if (NetJs.Script.IsDefined(interfaces))
        {
            for (int i = 0; i < interfaces!.Length; i++)
            {
                unchecked
                {
                    var mInterface = interfaces[i];
                    if (mInterface == targetInterface)
                    {
                        return true;
                    }
                    if (mInterface.Flags.TypeHasFlag(TypeFlagsModel.IsGenericType) &&
                        targetInterface.Flags.TypeHasFlag(TypeFlagsModel.IsGenericType) &&
                        NetJs.Script.IsDefined(targetInterface.Metadata) &&
                        NetJs.Script.IsDefined(targetInterface.Metadata!.GenericParameters))
                    {
                        var originalInterface = mInterface.OpenGenericPrototype ?? mInterface;
                        var originalTargetInterface = targetInterface.OpenGenericPrototype ?? targetInterface;
                        if (originalInterface != originalTargetInterface)
                        {
                            continue;
                        }
                        var targetArgs = targetInterface.Arguments;
                        var arguments = mInterface.Arguments;
                        for (int ix = 0; ix < targetInterface.Metadata.GenericParameters!.Length; ix++)
                        {
                            var genericArg = targetInterface.Metadata.GenericParameters![ix];
                            var targetArg = targetArgs![ix];
                            var arg = arguments![ix];
                            if (genericArg.Flags.TypeHasFlag(GenericConstraintFlagsModel.HasNewConstraint))
                            {
                                if (!arg.Flags.TypeHasFlag(TypeFlagsModel.HasDefaultConstructor))
                                {
                                    return false;
                                }
                            }
                            if (genericArg.Flags.TypeHasFlag(GenericConstraintFlagsModel.HasClassConstraint))
                            {
                                if (!arg.Flags.TypeHasFlag(TypeFlagsModel.IsClass))
                                {
                                    return false;
                                }
                            }
                            if (genericArg.Flags.TypeHasFlag(GenericConstraintFlagsModel.HasStructConstraint))
                            {
                                if (!arg.Flags.TypeHasFlag(TypeFlagsModel.IsValueType))
                                {
                                    return false;
                                }
                            }
                            if (genericArg.Flags.TypeHasFlag(GenericConstraintFlagsModel.HasOutConstraint))
                            {
                                if (!targetArg.Type!.IsAssignableFrom(arg.Type))
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                if (targetArg != arg)
                                {
                                    return false;
                                }
                            }
                            if (NetJs.Script.IsDefined(genericArg.TypeConstraints))
                            {
                                for (int ixx = 0; ixx < genericArg.TypeConstraints!.Length; ixx++)
                                {
                                    var type = AppDomain.GetType(genericArg.TypeConstraints[ixx].As<uint>()) ?? throw null!;
                                    if (!type.IsAssignableFrom(arg.Type))
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                        return true;
                    }
                }
            }
        }
        return false;
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
