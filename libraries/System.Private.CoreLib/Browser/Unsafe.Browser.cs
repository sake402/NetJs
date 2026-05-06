using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
    public static unsafe partial class Unsafe
    {
        static Ref<object> _nullRef = new(null!, null!);
        [NetJs.MemberReplace(nameof(AsRef) + "<>(void*)")]
        public static ref T AsRefImpl<T>(void* source)
            where T : allows ref struct
        {
            if (source == null || NetJs.Script.TypeOf(source).NativeEquals("number")/* && NetJs.Script.Write<int>("source") == 1*/)
            {
                // NetJs.Script.TypeOf(source).NativeEquals("number")
                // Reference to fake non-null pointer. Such a reference can be used
                // for pinning but must never be dereferenced. This is useful for interop with methods that do not accept null pointers for zero-sized buffers.
                // </summary>
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
            var isNullRef = NetJs.Script.Write<bool>("value == nullReff");
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

        [NetJs.MemberReplace(nameof(As) + "<,>")]
        public static ref TTo AsImpl<TFrom, TTo>(ref TFrom source)
            where TFrom : allows ref struct
            where TTo : allows ref struct
        {
            var reff = NetJs.Script.Write<Ref<object>>("source");
            var fromSize = SizeOf<TFrom>();
            var toSize = SizeOf<TTo>();
            if (fromSize != toSize)
            {
                //var mreff = new Ref<TTo>(reff);
                var mreff = NetJs.Script.Write<Ref<object>>("new ($.$spc.System.Ref$$(TTo))().$ctor$5(reff)");
                mreff._type = typeof(TTo);
                reff = mreff.As<Ref<object>>();
            }
            //if (fromSize > toSize)
            //{
            //    reff = reff with { _primitiveWindowItems = fromSize / toSize };
            //}
            //else if (toSize > fromSize)
            //{
            //    reff = reff with { _primitiveWindowItems = toSize / fromSize };
            //}
            NetJs.Script.Write("return reff");
            throw null!;
        }
        
        [NetJs.MemberReplace(nameof(Add) + "<>(ref T, nint)")]
        public static ref T AddImplNint<T>(ref T source, nint elementOffset)
            where T : allows ref struct
        {
            RefOrPointer<object> reff = NetJs.Script.Write<RefOrPointer<object>>("source");
            reff = reff.Add(elementOffset.As<int>());
            NetJs.Script.Write("return reff");
            throw null!;
        }

        [NetJs.MemberReplace(nameof(Add) + "<>(ref T, int)")]
        public static ref T AddImplInt<T>(ref T source, int elementOffset)
            where T : allows ref struct
        {
            RefOrPointer<object> reff = NetJs.Script.Write<RefOrPointer<object>>("source");
            reff = reff.Add(elementOffset);
            NetJs.Script.Write("return reff");
            throw null!;
        }

        [NetJs.MemberReplace(nameof(Add) + "<>(ref T, nuint)")]
        public static ref T AddImplNuint<T>(ref T source, nuint elementOffset)
            where T : allows ref struct
        {
            RefOrPointer<object> reff = NetJs.Script.Write<RefOrPointer<object>>("source");
            reff = reff.Add(elementOffset.As<int>());
            NetJs.Script.Write("return reff");
            throw null!;
        }

        [NetJs.MemberReplace(nameof(Add) + "<>(void*, int)")]
        public static void* AddImplVPtr<T>(void* source, int elementOffset)
            where T : allows ref struct
        {
            RefOrPointer<object> reff = NetJs.Script.Write<RefOrPointer<object>>("source");
            reff = reff.Add(elementOffset);
            NetJs.Script.Write("return reff");
            throw null!;
        }

        [NetJs.MemberReplace(nameof(SizeOf) + "<>")]
        public static int SizeOfImpl<T>()
            where T : allows ref struct
        {
            return System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
        }

        [NetJs.MemberReplace(nameof(AddByteOffset) + "<>(ref T, nint)")]
        public static ref T AddByteOffsetImpl<T>(ref T source, nint byteOffset)
            where T : allows ref struct
        {
            RefOrPointer<object> reff = NetJs.Script.Write<RefOrPointer<object>>("source");
            reff = reff.AddByteOffset((int)byteOffset);// with { _primitiveWindowItems = byteOffset.As<int>() };
            NetJs.Script.Write("return reff");
            throw null!;
        }

        [NetJs.MemberReplace(nameof(SubtractByteOffset) + "<>(ref T, nint)")]
        public static ref T SubtractByteOffsetImpl<T>(ref T source, nint byteOffset)
            where T : allows ref struct
        {
            RefOrPointer<object> reff = NetJs.Script.Write<RefOrPointer<object>>("source");
            reff = reff.AddByteOffset(-(int)byteOffset);// with { _primitiveWindowItems = byteOffset.As<int>() };
            NetJs.Script.Write("return reff");
            throw null!;
        }

        [NetJs.MemberReplace(nameof(ByteOffset) + "<>(ref readonly T, ref readonly T)")]
        public static nint ByteOffsetImpl<T>([AllowNull] ref readonly T origin, [AllowNull] ref readonly T target)
            where T : allows ref struct
        {
            RefOrPointer<object> reffo = NetJs.Script.Write<RefOrPointer<object>>("origin");
            RefOrPointer<object> refft = NetJs.Script.Write<RefOrPointer<object>>("target");
            if (reffo.Overlaps(refft))
                return (nint)refft.Subtract(reffo);
            return int.MaxValue;
        }

        [NetJs.MemberReplace(nameof(AreSame) + "<>")]
        public static bool AreSameImpl<T>([AllowNull] ref readonly T left, [AllowNull] ref readonly T right)
            where T : allows ref struct
        {
            RefOrPointer<object> mleft = NetJs.Script.Write<RefOrPointer<object>>("left");
            RefOrPointer<object> mright = NetJs.Script.Write<RefOrPointer<object>>("right");
            return ReferenceEquals(mleft, mright) || (mleft._parentRef == mright._parentRef && mleft.As<RefOrPointer<object>>()._byteOffset == mright.As<RefOrPointer<object>>()._byteOffset);
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
            RefOrPointer<object> source = NetJs.Script.Write<RefOrPointer<object>>("src");
            RefOrPointer<object> destination = NetJs.Script.Write<RefOrPointer<object>>("dest");
            var sourceSize = source.SizeOfItem;
            var destinationSize = destination.SizeOfItem;
            if (sourceSize != destinationSize)
            {
                nuint byteRemaining = lenBytes;
                if (destinationSize > sourceSize)
                {
                    int s_i = 0;
                    int d_i = 0;
                    ulong ReadOne()
                    {
                        ulong result = 0;
                        int i = 0;
                        while (i < destinationSize)
                        {
                            result |= source.GetAt(s_i).As<ulong>() << (i * 8 * sourceSize);
                            s_i++;
                            i++;
                        }
                        return result;
                    }
                    while (byteRemaining > 0)
                    {
                        destination.SetAt(ReadOne().As<object>(), d_i);
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
                        var s = source.GetAt(s_i.As<int>()).As<ulong>();
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
                nuint sourceOffset = source._arrayOffset.As<nuint>();
                nuint destOffset = destination._arrayOffset.As<nuint>();
                lenBytes /= (nuint)sourceSize;
                unchecked
                {
                    if (source._array != null && destination._array != null)
                    {
                        for (nuint i = 0; i < lenBytes; i++)
                        {
                            destination._array[i + destOffset] = source._array[i + sourceOffset];
                        }
                    }
                    else if (source._array != null && destination._array == null)
                    {
                        for (nuint i = 0; i < lenBytes; i++)
                        {
                            destination.SetAt(source._array[i + sourceOffset], i.As<int>());
                        }
                    }
                    else if (source._array == null && destination._array != null)
                    {
                        for (nuint i = 0; i < lenBytes; i++)
                        {
                            destination._array[i + destOffset] = source.GetAt(i.As<int>());
                        }
                    }
                    else if (source._array == null && destination._array == null)
                    {
                        if (source._parentRef != null && destination._parentRef != null && destination._byteOffset == 0 && source._byteOffset == 0)
                        {
                            var nDest = NetJs.Script.RefAsVoidPointer(NetJs.Script.Write<RefOrPointer<object>>("destination._parentRef"));
                            var nSrc = NetJs.Script.RefAsVoidPointer(NetJs.Script.Write<RefOrPointer<object>>("source._parentRef"));
                            CopyBlockFinal(nDest, nSrc, lenBytes);
                        }
                        else
                        {
                            for (nuint i = 0; i < lenBytes; i++)
                            {
                                destination.SetAt(source.GetAt(i.As<int>()), (i + destOffset).As<int>());
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
    }
}