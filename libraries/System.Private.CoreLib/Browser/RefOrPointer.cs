using NetJs;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public interface IRefOrPointer
    {
        int SizeOfItem { get; }
        Type Type { get; }
        object? Value { get; set; }
        //int? _arrayOffset { get; }
    }

    public static class RefOrPointer
    {
        public static int Compare(IRefOrPointer? first, IRefOrPointer? second)
        {
            if (first == null)
                return second == null ? 0 : -1;
            if (second == null)
                return 1;
            //Comparing two pointers should point to same memory allocation
            Debug.Assert(first.As<RefOrPointer<object>>().Overlaps(second), "Reference/Pointer must overlap before comparing them");
            return first.As<RefOrPointer<object>>()._arrayOffset - second.As<RefOrPointer<object>>()._arrayOffset;
        }
    }

    public abstract record class RefOrPointer<T> : IRefOrPointer
    {
        //static RefOrPointer<object> _nullRef;

        internal T[]? _array;
        [NativeDelegate]
        internal Func<int?, T> _getter;
        [NativeDelegate]
        internal Action<T, int?> _setter;
        internal int _byteOffset;

        internal IRefOrPointer? _parentRef;
        //internal IRefOrPointer? _castFrom;

        //If we cast a primitive pointer type like byte* to int*,
        //this holds the number of items to read(4) from the underlying byte array and return as the result
        //When a case from int* to byte*, this becomes -4
        //internal int _primitiveWindowItems;
        //internal ulong _primitiveWindowItemMask => _primitiveWindowItems switch
        //{
        //    1 or -1 => 0xFF,
        //    2 or -2 => 0xFFFF,
        //    4 or -4 => 0xFFFFFFFF,
        //    8 or -8 => 0xFFFFFFFFFFFFFFFF,
        //    _ => 0
        //};
        internal int _arrayOffset => _byteOffset == 0 ? 0 : _byteOffset / SizeOfItem;
        internal RefOrPointer(IRefOrPointer parent)
        {
            this._parentRef = parent;
        }

        internal RefOrPointer([NativeDelegate] Func<int?, T> getter, [NativeDelegate] Action<T, int?> setter)
        {
            this._getter = getter;
            this._setter = setter;
        }

        [NetJs.Name(NetJs.Constants.RefValueName)]
        T v
        {
            get => GetAt(0);
            set => SetAt(value, 0);
        }

        internal int? _sizeOfItem;
        public int SizeOfItem => _sizeOfItem ??= Marshal.SizeOf(Type);
        internal Type? _type;
        public Type Type => _type ?? typeof(T);

        public T Value
        {
            get => GetAt(0);
            set => SetAt(value, 0);
        }

        object? IRefOrPointer.Value
        {
            get => GetAt(0);
            set => SetAt((T)value, 0);
        }
        public T[] ToArray(int length = -1)
        {
            if (_array == null)
                throw new InvalidOperationException("Not based on an array");
            if (_arrayOffset == 0 && length < 0)
                return _array;
            int start = _arrayOffset;
            if (length < 0)
                length = _array.Length - start;
            var newArray = new T[length];
            Array.Copy(_array, start, newArray, 0, length);
            return newArray;
        }

        public T GetAt(int offset)
        {
            offset += _arrayOffset;
            if (_parentRef != null)
            {
                var sourceSize = _parentRef.SizeOfItem;
                var thisSize = SizeOfItem;
                if (thisSize > sourceSize) //eg int > byte, getting int from underlying byte[]
                {
                    var ratio = Math.DivRem(thisSize, sourceSize);
                    Debug.Assert(ratio.Remainder == 0);
                    ulong numeric = 0;
                    var isNumeric = _parentRef.Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsIntegerNumeric() && Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsIntegerNumeric();
                    var raw = !isNumeric ? new object[ratio.Quotient] : null;
                    var parentO = _parentRef.As<RefOrPointer<object>>();
                    for (int i = 0; i < ratio.Quotient; i++)
                    {
                        var value = parentO.GetAt(offset * ratio.Quotient + i);
                        if (isNumeric)
                        {
                            numeric |= (ulong)value.As<uint>() << (i * sourceSize * 8);
                        }
                        else
                        {
                            raw![i] = value;
                        }
                    }
                    if (isNumeric)
                    {
                        return (T)numeric.As<object>();
                    }
                    else if (Type.As<RuntimeType>()._model.As<TypeModel>().KnownType == KnownTypeHandle.SystemDouble)
                    {
                        NetJs.Script.Write("const bytes = new Uint8Array(raw)");
                        NetJs.Script.Write("const view = new DataView(bytes.buffer)");
                        return NetJs.Script.Write<T>("view.getFloat64(0, true)");
                    }
                    else if (Type.As<RuntimeType>()._model.As<TypeModel>().KnownType == KnownTypeHandle.SystemFloat)
                    {
                        NetJs.Script.Write("const bytes = new Uint8Array(raw)");
                        NetJs.Script.Write("const view = new DataView(bytes.buffer)");
                        return NetJs.Script.Write<T>("view.getFloat32(0, true)");
                    }
                    else
                    {
                        throw null;
                        //var t = NetJs.Script.Write<T>("new T()")!;
                        //t._fields = raw;
                        //return t;
                    }
                }
                else if (thisSize < sourceSize) //eg byte < int, getting byte from underlying int[]
                {
                    var ratio = Math.DivRem(sourceSize, thisSize);
                    Debug.Assert(ratio.Remainder == 0);
                    var parentO = _parentRef.As<RefOrPointer<object>>();
                    var d = (ulong)parentO.GetAt(offset / ratio.Quotient).As<uint>();
                    var i = offset % ratio.Quotient;
                    if (_parentRef.Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsIntegerNumeric() && Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsIntegerNumeric())
                    {
                        return (T)(d >> (8 * i)).As<object>();
                    }
                    else
                    {
                        throw null;
                        //d.As<object>()._fields;
                    }
                }
            }
            //else if (_primitiveWindowItems > 0) //eg getting int from underlying byte[]
            //{
            //    ulong result = 0;
            //    for (int i = 0; i < _primitiveWindowItems; i++)
            //    {
            //        result |= _getter(ArrayOffset + i).As<ulong>() << (i * 8);
            //    }
            //    return result.As<T>();
            //}
            //else if (_primitiveWindowItems < 0)  //eg getting byte from underlying int[]
            //{
            //    ulong result = (_getter(ArrayOffset).As<ulong>() >> ((Math.Abs(_primitiveWindowItems) - 1) * 8)) & _primitiveWindowItemMask;
            //    return result.As<T>();
            //}
            return _getter(offset);
        }

        public void SetAt(T value, int offset)
        {
            offset += _arrayOffset;
            if (_parentRef != null)
            {
                var sourceSize = _parentRef.SizeOfItem;
                var thisSize = SizeOfItem;
                if (thisSize > sourceSize) //eg int > byte, setting int to underlying byte[]
                {
                    var ratio = Math.DivRem(thisSize, sourceSize);
                    Debug.Assert(ratio.Remainder == 0);
                    var parentO = _parentRef.As<RefOrPointer<object>>();
                    var isNumeric = _parentRef.Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsIntegerNumeric() && Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsIntegerNumeric();
                    ulong mask = sourceSize switch
                    {
                        1 => 0xFF,
                        2 => 0xFFFF,
                        4 => 0xFFFFFFFF,
                        8 => 0xFFFFFFFFFFFFFFFF,
                        _ => 0
                    };
                    var parentPrototype = parentO.Type.As<RuntimeType>()._prototype;
                    for (int i = 0; i < ratio.Quotient; i++)
                    {
                        if (isNumeric)
                        {
                            var mvalue = ((ulong)value.As<uint>() >> (i * sourceSize * 8)) & mask;
                            var dvalue = NetJs.Script.Write<object>($"{NetJs.Constants.GlobalName}.{NetJs.Constants.CastName}({nameof(mvalue)}, {nameof(parentPrototype)})");
                            parentO.SetAt(dvalue, offset * ratio.Quotient + i);
                        }
                        else
                        {
                            throw null!;
                        }
                    }
                }
                else if (thisSize < sourceSize) //eg byte < int, setting byte to underlying int[]
                {
                    var ratio = Math.DivRem(sourceSize, thisSize);
                    Debug.Assert(ratio.Remainder == 0);
                    var d = (ulong)_parentRef.As<RefOrPointer<object>>().GetAt(offset / ratio.Quotient).As<uint>();
                    var i = offset % ratio.Quotient;
                    var parentO = _parentRef.As<RefOrPointer<object>>();
                    if (_parentRef.Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsIntegerNumeric() && Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsIntegerNumeric())
                    {
                        var maskSet = (ulong)value.As<uint>();
                        var maskClear = ~(0xffUL << (8 * i));
                        d = (d & maskClear) | (maskSet << (8 * i));
                        var parentPrototype = parentO.Type.As<RuntimeType>()._prototype;
                        var dd = NetJs.Script.Write<object>($"{NetJs.Constants.GlobalName}.{NetJs.Constants.CastName}({nameof(d)}, {nameof(parentPrototype)})");
                        parentO.SetAt(dd, offset / ratio.Quotient);
                    }
                    else
                    {
                        throw null;
                    }
                }
            }
            //else if (_primitiveWindowItems > 0) //eg setting int to underlying byte[]
            //{
            //    ulong val = value.As<ulong>();
            //    for (int i = 0; i < _primitiveWindowItems; i++)
            //    {
            //        _setter((val >> (i * 8)).As<T>(), ArrayOffset + i);
            //    }
            //}
            //else if (_primitiveWindowItems < 0)  //eg setting byte to underlying int[]
            //{
            //    var currentValue = _getter(ArrayOffset).As<ulong>();
            //    var off = (ArrayOffset ?? 0) % Math.Abs(_primitiveWindowItems);
            //    var mask = _primitiveWindowItemMask << off;
            //    currentValue &= ~mask;
            //    currentValue |= value.As<ulong>() << off;
            //    _setter(currentValue.As<T>(), ArrayOffset);
            //}
            else
            {
                _setter(value, offset);
            }
        }

        //private T v
        //{
        //    get
        //    {
        //        return Value;
        //    }
        //    set
        //    {
        //        Value = value;
        //    }
        //}

        public void CopyTo(RefOrPointer<T> dst, int count)
        {
            if (!Array<T>.Is(Value) || !Array<T>.Is(dst.Value))
            {
                throw new InvalidOperationException("Both ref must be an array");
            }
            for (int i = 0; i < count; i++)
            {
                dst.Value.As<Array>()[dst._arrayOffset + i] = Value.As<Array>()[_arrayOffset + i];
            }
        }

        public RefOrPointer<T> this[int offset]
        {
            get
            {
                return this with { _byteOffset = _byteOffset + (offset * SizeOfItem) };
            }
        }

        public RefOrPointer<T> AddByteOffset(int offset)
        {
            return this with { _byteOffset = _byteOffset + offset };
        }

        public RefOrPointer<T> Add(long offset)
        {
            return this with { _byteOffset = _byteOffset + ((int)offset * SizeOfItem) };
        }

        public bool Overlaps(IRefOrPointer? second)
        {
            if (second == null)
                return false;
            //Subtracting two pointers should point to same memory allocation
            var array1 = _array;
            var array2 = second.As<RefOrPointer<object>>()._array;
            if (array1 is not null || array2 is not null)
                return ReferenceEquals(array1, array2);
            var parent1 = _parentRef.As<RefOrPointer<object>>();
            var parent2 = second.As<RefOrPointer<object>>()._parentRef.As<RefOrPointer<object>>();
            if (parent1 is not null && parent2 is not null)
                return parent1.Overlaps(parent2);
            return ReferenceEquals(parent1, parent2);
        }

        public long Subtract(IRefOrPointer second)
        {
            //Subtracting two pointers should point to same memory allocation
            Debug.Assert(Overlaps(second), "Reference/Pointer must overlap before comparing them");
            return _arrayOffset - second.As<RefOrPointer<object>>()._arrayOffset;
        }

        //public static implicit operator T(Ref<T> reference)
        //{
        //    return reference.Value;
        //}

        //public static implicit operator =(Ref<T> reference, T value)
        //{
        //    return reference.Value;
        //}

        public override string? ToString()
        {
            return Value?.ToString() ?? base.ToString();
        }


        //[NetJs.Template("{0}")]
        //[NetJs.Unbox(true)]
        //public static extern unsafe ref T As<T>(void* obj);

        //[NetJs.Template("{0}")]
        //public static extern unsafe ref T FromPointer(void* pointer);

        //[NetJs.Template("{0}")]
        //public static extern unsafe T* ToPointer(ref T valueRef);
    }

    public record class Ref<T> : RefOrPointer<T>
    {
        protected Ref(Ref<T> original) : base(original)
        {
        }

        internal Ref(IRefOrPointer parent) : base(parent)
        {
        }

        internal Ref(Func<int?, T> getter, Action<T, int?> setter) : base(getter, setter)
        {
        }

        [NetJs.Name(NetJs.Constants.IsTypeName)]
        public static bool Is(object? value, out Ref<T>? result)
        {
            result = NetJs.Script.Write<Ref<T>>("undefined");
            if (value == null)
                return false;
            var ps = Object.GetOwnPropertyNames(value);
            unchecked
            {
                //Haadle simple inline ref created by transpiler, not a real ref or pointer object, just has a property named Constants.RefValueName to hold the value
                if (ps.Length == 1 && ps[0] == NetJs.Constants.RefValueName)
                {
                    var val = NetJs.Script.Write<object>($"value.{NetJs.Constants.RefValueName}");
                    return val == null || val is T;
                }
            }
            if (NetJs.Script.TypeOf(value).NativeEquals("number"))
            {
                // Reference to fake non-null pointer. Such a reference can be used
                // for pinning but must never be dereferenced. This is useful for interop with methods that do not accept null pointers for zero-sized buffers.
                // </summary>
                ref T t = ref Unsafe.NullRef<T>();
                result = NetJs.Script.Write<Ref<T>>("t");
                return value.As<int>() == 0 || value.As<int>() == 1;
            }

            if (value is IRefOrPointer rref)
            {
                var toSize = Marshal.SizeOf<T>();
                var fromSize = rref.SizeOfItem;
                if (toSize != fromSize)
                {
                    //coarse the new ref to a new size
                    var newRef = new Ref<T>(rref);
                    //var newRef = rref.As<Ref<T>>() with
                    //{
                    //    _sizeOfItem = toSize,
                    //    _type = typeof(T),
                    //    _castFrom = rref
                    //};
                    result = newRef;
                }
                return true;
            }
            return false;
        }
    }

    public record class Pointer<T> : RefOrPointer<T>
    {
        static Pointer<T> _pinned = new Pointer<T>(null!, null!);
        protected Pointer(Pointer<T> original) : base(original)
        {
        }

        internal Pointer(IRefOrPointer parent) : base(parent)
        {
        }

        internal Pointer(Func<int?, T> getter, Action<T, int?> setter) : base(getter, setter)
        {
        }

        [NetJs.Name(NetJs.Constants.IsTypeName)]
        public static bool Is(object? value, out Pointer<T>? result)
        {
            result = NetJs.Script.Write<Pointer<T>>("undefined");
            if (value == null)
                return false;
            var ps = Object.GetOwnPropertyNames(value);
            unchecked
            {
                //Haadle simple inline ref created by transpiler, not a real ref or pointer object, just has a property named Constants.RefValueName to hold the value
                if (ps.Length == 1 && ps[0] == NetJs.Constants.RefValueName)
                {
                    var val = NetJs.Script.Write<object>($"value.{NetJs.Constants.RefValueName}");
                    return val == null || val is T;
                }
            }
            if (NetJs.Script.TypeOf(value).NativeEquals("number") && (value.As<int>() == 0 || value.As<int>() == 1))
            {
                if (value.As<int>() == 0)
                    result = null;
                else
                {
                    result = _pinned;
                }
                return true;
            }
            if (value is IRefOrPointer rref)
            {
                var toSize = Marshal.SizeOf<T>();
                var fromSize = rref.SizeOfItem;
                if (toSize != fromSize)
                {
                    //coarse the new ref to a new size
                    var newRef = new Pointer<T>(rref);
                    //var newRef = rref.As<Pointer<T>>() with
                    //{
                    //    _sizeOfItem = toSize,
                    //    _type = typeof(T),
                    //    _castFrom = rref
                    //};
                    result = newRef;
                }
                return true;
            }
            return false;
        }
    }
}
