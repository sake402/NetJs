using System.Diagnostics.CodeAnalysis;
using Window;

namespace System.Runtime.CompilerServices
{
    public static unsafe partial class Unsafe
    {
        internal static Ref<object> _nullRef = new(null!, null!);
        [NetJs.MemberReplace(nameof(AsRef) + "<>(void*)")]
        public static ref T AsRefImpl<T>(void* source)
            where T : allows ref struct
        {
            if (source == null || NetJs.Script.TypeOf(source).NativeEquals("number")/* && NetJs.Script.Write<int>("source") == 1*/)
            {
                // NetJs.Script.TypeOf(source).NativeEquals("number")
                // Reference to fake non-null pointer. Such a reference can be used
                // for pinning but must never be dereferenced. This is useful for interop with methods that do not accept null pointers for zero-sized buffers.
                var reff = _nullRef;
                NetJs.Script.Write("return reff");
            }
            NetJs.Script.Write("return source");
            throw null!;
        }
        [NetJs.MemberReplace(nameof(AsRef) + "<>(scoped ref readonly T)")]
        [NetJs.Template("{source}")]
        public static extern ref T AsRefImpl<T>(scoped ref readonly T source)
            where T : allows ref struct;
        //{
        //    NetJs.Script.Write("return source");
        //    throw null!;
        //}

        [NetJs.MemberReplace(nameof(AsPointer) + "<>")]
        public static void* AsPointerImpl<T>(ref readonly T value)
                    where T : allows ref struct
        {
            var nullReff = _nullRef;
            var isNullRef = NetJs.Script.Write<bool>("value === nullReff");
            if (isNullRef)
                return null;
            NetJs.Script.Write("return value");
            return null!;
        }

        [NetJs.MemberReplace(nameof(As) + "<>")]
        [NetJs.Template("{o}")]
        public static extern T? AsImpl<T>(object? o) where T : class?;
        //{
        //    return o.As<T>();
        //}
        static RefOrPointer<object> EnsureIsRefOrPointer<T>(ref T source)
            where T : allows ref struct
        {
            RefOrPointer<object> reff = NetJs.Script.Write<RefOrPointer<object>>("source");
            var isPointer = reff.IsPointer;
            if (NetJs.Script.IsUndefined(isPointer)) //reff we have is a simple ref, make a new ref out of it
            {
                if (NetJs.Script.IsDiscardRef(ref source)) //a discard ref shoulf not be wrapped, returns discard
                    return reff;
                var descriptor = Object.GetOwnPropertyDescriptor(reff, Constants.RefValueName);
                //reff = RuntimeHelpers.CreateObjectReferenceT<T>(descriptor.Get.As<NativeFunction<int?, T>>(), descriptor.Set.As<NativeAction<T, int?>>());
                reff = RuntimeHelpers.CreateObjectReferenceT<object>(descriptor.Get.As<NativeFunction<int?, object>>(), descriptor.Set.As<NativeAction<object, int?>>());
                var type = reff["$type"].As<TypePrototype>();
                if (NetJs.Script.IsDefined(type))
                {
                    reff._type = type.Type;
                }
                else
                {
                    reff._type = typeof(T);
                }
            }
            NetJs.Script.Write("return reff");
            throw null!;
        }

        static RefOrPointer<object> EnsureIsRefOrPointer<T>(void* source)
            where T : allows ref struct
        {
            RefOrPointer<object> reff = NetJs.Script.Write<RefOrPointer<object>>("source");
            var isPointer = reff.IsPointer;
            if (NetJs.Script.IsUndefined(isPointer)) //reff we have is a simple ref, make a new ref out of it
            {
                if (NetJs.Script.IsDiscardRef(reff)) //a discard ref shoulf not be wrapped, returns discard
                    return reff;
                var descriptor = Object.GetOwnPropertyDescriptor(reff, Constants.RefValueName);
                reff = RuntimeHelpers.CreateObjectReferenceT<object>(descriptor.Get.As<NativeFunction<int?, object>>(), descriptor.Set.As<NativeAction<object, int?>>());
                var type = reff["$type"].As<TypePrototype>();
                if (NetJs.Script.IsDefined(type))
                {
                    reff._type = type.Type;
                }
                else
                {
                    reff._type = typeof(T);
                }
            }
            NetJs.Script.Write("return reff");
            throw null!;
        }

        [NetJs.MemberReplace(nameof(As) + "<,>")]
        public static ref TTo AsImpl<TFrom, TTo>(ref TFrom source)
            where TFrom : allows ref struct
            where TTo : allows ref struct
        {
            if (Unsafe.IsNullRef(ref source))
            {
                NetJs.Script.Write("return source");
            }
            if (!NetJs.Script.IsDiscardRef(ref source)) //a discard ref shoulf not be wrapped, returns discard
            {
                RefOrPointer<object> mreff = EnsureIsRefOrPointer(ref source);// NetJs.Script.Write<RefOrPointer<object>>("source");
                var reff = NetJs.Script.Write<Ref<object>>("mreff.{nameof(NetJs.RefOrPointer<>.Cast<>())}(TTo)");
                if (reff != null)
                {
                    NetJs.Script.Write("return reff");
                }
            }
            NetJs.Script.Write("return source");
            throw null!;
        }

        static ref T DoAddReference<T>(ref T source, int offset, bool byElement)
            where T : allows ref struct
        {
            RefOrPointer<object> reff = EnsureIsRefOrPointer(ref source);// NetJs.Script.Write<RefOrPointer<object>>("source");
            reff = byElement ? reff.Add(offset) : reff.AddByteOffset(offset);
            NetJs.Script.Write("return reff");
            throw null!;
        }

        static void* DoAddReference<T>(void* source, int offset, bool byElement)
            where T : allows ref struct
        {
            RefOrPointer<object> reff = EnsureIsRefOrPointer<T>(source);// NetJs.Script.Write<RefOrPointer<object>>("source");
            reff = byElement ? reff.Add(offset) : reff.AddByteOffset(offset);
            NetJs.Script.Write("return reff");
            throw null!;
        }

        [NetJs.MemberReplace(nameof(Add) + "<>(ref T, nint)")]
        public static ref T AddImplNint<T>(ref T source, nint elementOffset)
            where T : allows ref struct
        {
            return ref DoAddReference(ref source, (int)elementOffset, true);
        }

        [NetJs.MemberReplace(nameof(Add) + "<>(ref T, int)")]
        public static ref T AddImplInt<T>(ref T source, int elementOffset)
            where T : allows ref struct
        {
            return ref DoAddReference(ref source, (int)elementOffset, true);
        }

        [NetJs.MemberReplace(nameof(Add) + "<>(ref T, nuint)")]
        public static ref T AddImplNuint<T>(ref T source, nuint elementOffset)
            where T : allows ref struct
        {
            return ref DoAddReference(ref source, (int)elementOffset, true);
        }

        [NetJs.MemberReplace(nameof(Add) + "<>(void*, int)")]
        public static void* AddImplVPtr<T>(void* source, int elementOffset)
            where T : allows ref struct
        {
            return DoAddReference<T>(source, (int)elementOffset, true);
        }

        [NetJs.MemberReplace(nameof(SizeOf) + "<>")]
        public static int SizeOfImpl<T>()
            where T : allows ref struct
        {
            return System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
        }


        [NetJs.MemberReplace(nameof(AddByteOffset) + "<>(ref T, nuint)")]
        public static ref T AddByteOffsetImplNuint<T>(ref T source, nuint byteOffset)
            where T : allows ref struct
        {
            return ref AddByteOffset(ref source, (nint)byteOffset);
        }

        [NetJs.MemberReplace(nameof(AddByteOffset) + "<>(ref T, nint)")]
        public static ref T AddByteOffsetImpl<T>(ref T source, nint byteOffset)
            where T : allows ref struct
        {
            return ref DoAddReference<T>(ref source, (int)byteOffset, false);
        }

        [NetJs.MemberReplace(nameof(AddByteOffset) + "<>(ref T, IntPtr)")]
        public static ref T AddByteOffsetImpl2<T>(ref T source, IntPtr byteOffset)
            where T : allows ref struct
        {
            return ref DoAddReference<T>(ref source, (int)byteOffset, false);
        }

        [NetJs.MemberReplace(nameof(SubtractByteOffset) + "<>(ref T, nint)")]
        public static ref T SubtractByteOffsetImpl<T>(ref T source, nint byteOffset)
            where T : allows ref struct
        {
            return ref DoAddReference<T>(ref source, -(int)byteOffset, false);
        }

        [NetJs.MemberReplace(nameof(SubtractByteOffset) + "<>(ref T, IntPtr)")]
        public static ref T SubtractByteOffsetIntPtrImpl<T>(ref T source, IntPtr byteOffset)
            where T : allows ref struct
        {
            return ref DoAddReference<T>(ref source, -(int)byteOffset, false);
        }

        [NetJs.MemberReplace(nameof(ByteOffset) + "<>(ref readonly T, ref readonly T)")]
        public static nint ByteOffsetImpl<T>([AllowNull] ref readonly T origin, [AllowNull] ref readonly T target)
            where T : allows ref struct
        {
            RefOrPointer<object> reffo = NetJs.Script.Write<RefOrPointer<object>>("origin");
            RefOrPointer<object> refft = NetJs.Script.Write<RefOrPointer<object>>("target");
            if (reffo.Overlaps(refft))
            {
                var elementOffset = (nint)refft.Subtract(reffo);
                return elementOffset * reffo.SizeOfItem;
            }
            return int.MaxValue;
        }

        [NetJs.MemberReplace(nameof(AreSame) + "<>")]
        public static bool AreSameImpl<T>([AllowNull] ref readonly T left, [AllowNull] ref readonly T right)
            where T : allows ref struct
        {
            RefOrPointer<object> mleft = NetJs.Script.Write<RefOrPointer<object>>("left");
            RefOrPointer<object> mright = NetJs.Script.Write<RefOrPointer<object>>("right");
            var rootLeft = mleft.GetRefWithBackingArrayOrObject(out var leftOffset, out _);
            var rootRight = mright.GetRefWithBackingArrayOrObject(out var rightOffset, out _);
            return rootLeft == rootRight && leftOffset == rightOffset;
            //return ReferenceEquals(mleft, mright) || (mleft._array == mright._array && mleft._parentRef == mright._parentRef && mleft.As<RefOrPointer<object>>()._byteOffset == mright.As<RefOrPointer<object>>()._byteOffset);
        }

        [NetJs.MemberReplace(nameof(IsAddressGreaterThan) + "<>(ref readonly T, ref readonly T)")]
        public static bool IsAddressGreaterThanImpl<T>([AllowNull] ref readonly T left, [AllowNull] ref readonly T right)
            where T : allows ref struct
        {
            RefOrPointer<object> mleft = NetJs.Script.Write<RefOrPointer<object>>("left");
            RefOrPointer<object> mright = NetJs.Script.Write<RefOrPointer<object>>("right");
            return mleft.Subtract(mright) > 0;
        }

        [NetJs.MemberReplace(nameof(IsAddressGreaterThanOrEqualTo) + "<>(ref readonly T, ref readonly T)")]
        public static bool IsAddressGreaterThanOrEqualToImpl<T>([AllowNull] ref readonly T left, [AllowNull] ref readonly T right)
            where T : allows ref struct
        {
            RefOrPointer<object> mleft = NetJs.Script.Write<RefOrPointer<object>>("left");
            RefOrPointer<object> mright = NetJs.Script.Write<RefOrPointer<object>>("right");
            return mleft.Subtract(mright) >= 0;
        }

        [NetJs.MemberReplace(nameof(IsAddressLessThan) + "<>(ref readonly T, ref readonly T)")]
        public static bool IsAddressLessThanImpl<T>([AllowNull] ref readonly T left, [AllowNull] ref readonly T right)
            where T : allows ref struct
        {
            RefOrPointer<object> mleft = NetJs.Script.Write<RefOrPointer<object>>("left");
            RefOrPointer<object> mright = NetJs.Script.Write<RefOrPointer<object>>("right");
            return mleft.Subtract(mright) < 0;
        }

        [NetJs.MemberReplace(nameof(IsAddressLessThanOrEqualTo) + "<>(ref readonly T, ref readonly T)")]
        public static bool IsAddressLessThanOrEqualToImpl<T>([AllowNull] ref readonly T left, [AllowNull] ref readonly T right)
            where T : allows ref struct
        {
            RefOrPointer<object> mleft = NetJs.Script.Write<RefOrPointer<object>>("left");
            RefOrPointer<object> mright = NetJs.Script.Write<RefOrPointer<object>>("right");
            return mleft.Subtract(mright) <= 0;
        }

        [NetJs.MemberReplace(nameof(ReadUnaligned) + "<>(void*)")]
        public static T ReadUnalignedImpl<T>(void* source)
            where T : allows ref struct
        {
            return As<byte, T>(ref Unsafe.AsRef<byte>(source));
        }

        [NetJs.MemberReplace(nameof(WriteUnaligned) + "<>(void*, T)")]
        public static void WriteUnalignedImpl<T>(void* destination, T value)
            where T : allows ref struct
        {
            As<byte, T>(ref Unsafe.AsRef<byte>(destination)) = value;
        }

        public static unsafe void CopyBlockFinal(void* dest, void* src, nuint lenBytes)
        {
            if (lenBytes == 0) //we could be dealing with a null reference, if len is zero
                return;
            RefOrPointer<object>? source = NetJs.Script.Write<RefOrPointer<object>?>("src");
            RefOrPointer<object> destination = NetJs.Script.Write<RefOrPointer<object>>("dest");
            var sourceSize = src != null ? source!.SizeOfItem : 0;
            var destinationSize = destination.SizeOfItem;
            if (src != null && sourceSize != destinationSize)
            {
                nuint byteRemaining = lenBytes;
                if (destinationSize > sourceSize)
                {
                    int s_i = 0;
                    int d_i = 0;
                    ulong ReadOneLong()
                    {
                        ulong result = 0;
                        int i = 0;
                        while (i < destinationSize)
                        {
                            result |= (ulong)source!.GetAt(s_i) << (i * 8 * sourceSize);
                            s_i++;
                            i++;
                        }
                        return result;
                    }
                    uint ReadOneNumber()
                    {
                        uint result = 0;
                        int i = 0;
                        while (i < destinationSize)
                        {
                            result |= (uint)source!.GetAt(s_i) << (i * 8 * sourceSize);
                            s_i++;
                            i++;
                        }
                        return result;
                    }
                    while (byteRemaining > 0)
                    {
                        if (destination.Type.As<RuntimeType>()._prototype.KnownType == KnownTypeHandle.SystemInt64 ||
                            destination.Type.As<RuntimeType>()._prototype.KnownType == KnownTypeHandle.SystemUint64)
                        {
                            destination.SetAt(ReadOneLong().As<object>(), d_i);
                        }
                        else
                        {
                            destination.SetAt(ReadOneNumber().As<object>(), d_i);
                        }
                        d_i++;
                        byteRemaining -= destinationSize.As<nuint>();
                    }
                }
                else
                {
                    int s_i = 0;
                    int d_i = 0;
                    ulong mask = destinationSize switch
                    {
                        1 => 0xFF,
                        2 => 0xFFFF,
                        4 => 0xFFFFFFFF,
                        8 => 0xFFFFFFFFFFFFFFFF,
                        _ => 0
                    };
                    while (byteRemaining > 0)
                    {
                        var s = source!.GetAt(s_i.As<int>()).As<ulong>();
                        s_i++;
                        int ix = 0;
                        while (ix < sourceSize)
                        {
                            s >>= (ix * 8 * destinationSize);
                            destination.SetAt((s & mask).As<object>(), d_i);
                            d_i++;
                        }
                        byteRemaining -= sourceSize.As<nuint>();
                    }
                }
            }
            else
            {
                //Fast path, both are same data type
                nuint sourceOffset = src != null ? (source!._arrayOffset.As<nuint?>() ?? 0) : 0;
                nuint destOffset = destination._arrayOffset.As<nuint?>() ?? 0;
                nuint lenItems = lenBytes;
                if (sourceSize != 0)
                    lenItems /= (nuint)sourceSize;
                unchecked
                {
                    var defaultDestinationValue = src == null ? NetJs.Script.GetDefaultValue(destination.Type.As<RuntimeType>()._prototype) : null; //default of zero wont work for ref and bool
                    if ((src == null || source!._array != null) && destination._array != null)
                    {
                        for (nuint i = 0; i < lenItems; i++)
                        {
                            destination._array[i + destOffset] = src == null ? defaultDestinationValue! : source!._array![i + sourceOffset];
                        }
                        //chaging array directly invalidates existing DataView
                        destination._dataView = null;
                    }
                    else if ((src == null || source!._array != null) && destination._array == null)
                    {
                        for (nuint i = 0; i < lenItems; i++)
                        {
                            destination.SetAt(src == null ? defaultDestinationValue! : source!._array![i + sourceOffset], i.As<int>());
                        }
                    }
                    else if (src != null && source!._array == null && destination._array != null)
                    {
                        for (nuint i = 0; i < lenItems; i++)
                        {
                            destination._array[i + destOffset] = src == null ? defaultDestinationValue! : source.GetAt(i.As<int>());
                        }
                        //chaging array directly invalidates existing DataView 
                        destination._dataView = null;
                    }
                    else //if (source?._array == null && destination._array == null)
                    {
                        if (source?._parentRef != null && destination._parentRef != null && (destination._byteOffset ?? 0) == 0 && (source._byteOffset ?? 0) == 0)
                        {
                            var nDest = NetJs.Script.RefAsVoidPointer(destination._parentRef.As<RefOrPointer<object>>());
                            var nSrc = NetJs.Script.RefAsVoidPointer(source._parentRef.As<RefOrPointer<object>>());
                            CopyBlockFinal(nDest, nSrc, lenBytes);
                        }
                        else
                        {
                            for (nuint i = 0; i < lenItems; i++)
                            {
                                destination.SetAt(src == null ? defaultDestinationValue! : source!.GetAt(i.As<int>()), i.As<int>());
                            }
                        }
                    }
                }
            }
        }


        [NetJs.MemberReplace(nameof(CopyBlock) + "(void*, void*, uint)")]
        public static void CopyBlockPtrImpl(void* destination, void* source, uint byteCount)
        {
            CopyBlockFinal(destination, source, byteCount);
        }

        [NetJs.MemberReplace(nameof(CopyBlock) + "(ref byte, ref readonly byte, uint)")]
        public static void CopyBlockByteImpl(ref byte destination, ref readonly byte source, uint byteCount)
        {
            CopyBlockFinal(NetJs.Script.RefAsPointer<byte>(ref destination), NetJs.Script.RefAsPointer<byte>(in source), byteCount);
        }

        [NetJs.MemberReplace(nameof(CopyBlockUnaligned) + "(void*, void*, uint)")]
        public static void CopyBlockUnalignedPtrImpl(void* destination, void* source, uint byteCount)
        {
            CopyBlockFinal(destination, source, byteCount);
        }

        [NetJs.MemberReplace(nameof(CopyBlockUnaligned) + "(ref byte, ref readonly byte, uint)")]
        public static void CopyBlockUnalignedByteImpl(ref byte destination, ref readonly byte source, uint byteCount)
        {
            CopyBlockFinal(NetJs.Script.RefAsPointer<byte>(ref destination), NetJs.Script.RefAsPointer<byte>(in source), byteCount);
        }

        [NetJs.MemberReplace(nameof(SkipInit) + "<>")]
        public static void SkipInitImpl<T>(out T value)
            where T : allows ref struct
        {
            if (NetJs.Script.Write<bool>("value.$v == undefined"))
                value = default!;
            NetJs.Script.Write("return");
            value = default!;
        }

        [NetJs.MemberReplace(nameof(BitCast) + "<,>")]
        public static TTo BitCastImpl<TFrom, TTo>(TFrom source)
            where TFrom : allows ref struct
            where TTo : allows ref struct
        {
            //Major use case of this method will have TFrom and TTo equal
            if (typeof(TFrom) == typeof(TTo))
                return NetJs.Script.Write<TTo>("source");
            var fromType = typeof(TFrom).As<RuntimeType>()._prototype;
            var toType = typeof(TTo).As<RuntimeType>()._prototype;
            if (fromType.KnownType == KnownTypeHandle.SystemEnum)
            {
                fromType = fromType.As<EnumPrototype>().UnderlyingType.Prototype;
            }
            if (toType.KnownType == KnownTypeHandle.SystemEnum)
            {
                toType = toType.As<EnumPrototype>().UnderlyingType.Prototype;
            }
            if (fromType != null &&
                toType != null &&
                fromType.KnownType.IsNumeric() &&
                toType.KnownType.IsNumeric())
            {
                return RuntimeHelpers.BitCast<TFrom, TTo>(source);
            }
            if (sizeof(TFrom) != sizeof(TTo) || !typeof(TFrom).IsValueType || !typeof(TTo).IsValueType)
            {
                ThrowHelper.ThrowNotSupportedException();
            }
            return ReadUnaligned<TTo>(ref As<TFrom, byte>(ref source));
        }
    }
}