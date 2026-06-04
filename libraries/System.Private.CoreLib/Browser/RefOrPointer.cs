using NetJs;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Window;

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
            if (first.As<RefOrPointer<object>>()._object != null && second.As<RefOrPointer<object>>()._object != null)
            {
                //object pointer, not sure how to compare this yet
                //Shold return zero if referencing the same object though
                return -1;
            }
            //Comparing two array pointers should point to same memory allocation
            Debug.Assert(first.As<RefOrPointer<object>>().Overlaps(second), "Reference/Pointer must overlap before comparing them");
            return first.As<RefOrPointer<object>>()._arrayOffset - second.As<RefOrPointer<object>>()._arrayOffset;
        }
    }

    public abstract record class RefOrPointer<T> : IRefOrPointer
    {
        //static RefOrPointer<object> _nullRef;

        internal uint _virtualAddress;
        internal T? _object;
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
        public Type Type => _type ??= typeof(T);

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
            if (_object != null)
                return [_object];
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
        internal DataView? _dataView;
        DataView DataView
        {
            get
            {
                if (_dataView != null)
                    return _dataView;
                if (_array == null && _object == null)
                    throw new InvalidOperationException("Not based on an array/object");
                var knownType = Type.As<RuntimeType>()._model.As<TypeModel>().KnownType;
                if (!knownType.IsNumeric())
                    throw new InvalidOperationException("Not supported on non-numeric types");
                if (_arrayOffset != 0)
                    throw new InvalidOperationException("Not supported on non-root ref");
                T[] arr = _array ?? NetJs.Script.CreateArrayFromValues(_object!);
                var bytes = knownType switch
                {
                    KnownTypeHandle.SystemSByte => new Window.Int8Array(arr).buffer,
                    KnownTypeHandle.SystemByte => new Window.Uint8Array(arr).buffer,
                    KnownTypeHandle.SystemInt16 => new Window.Int16Array(arr).buffer,
                    KnownTypeHandle.SystemUInt16 or KnownTypeHandle.SystemChar => new Window.Uint16Array(arr).buffer,
                    KnownTypeHandle.SystemInt32 or KnownTypeHandle.SystemIntPtr => new Window.Int32Array(arr).buffer,
                    KnownTypeHandle.SystemUint32 or KnownTypeHandle.SystemUintPtr => new Window.Uint32Array(arr).buffer,
                    KnownTypeHandle.SystemInt64 => new Window.BigInt64Array(arr).buffer,
                    KnownTypeHandle.SystemUint64 => new Window.BigUint64Array(arr).buffer,
                    KnownTypeHandle.SystemSingle => new Window.Float32Array(arr).buffer,
                    KnownTypeHandle.SystemDouble => new Window.Float64Array(arr).buffer,
                    // Handle other sizes as needed
                    _ => throw new NotSupportedException("Size not supported")
                };
                _dataView = new DataView(bytes);
                //_dataView["array"] = arr;
                return _dataView;
            }
        }

        //void UpdateOriginalArray()
        //{
        //    if (_dataView != null)
        //    {
        //        var knownType = Type.As<RuntimeType>()._model.As<TypeModel>().KnownType;
        //        //update the original array from the dataView
        //        //var originalArray = _dataView["array"].As<Array>();
        //        Array originalArray = _array!;
        //        if (NetJs.Script.IsDefined(originalArray))
        //        {
        //            for (int i = 0; i < originalArray.Length; i++)
        //            {
        //                var ix = i * SizeOfItem;
        //                var value = knownType switch
        //                {
        //                    KnownTypeHandle.SystemByte => _dataView.getUint8(ix).As<object>(),
        //                    KnownTypeHandle.SystemInt16 => _dataView.getInt16(ix, true).As<object>(),
        //                    KnownTypeHandle.SystemUInt16 or KnownTypeHandle.SystemChar => _dataView.getUint16(ix, true).As<object>(),
        //                    KnownTypeHandle.SystemInt32 or KnownTypeHandle.SystemIntPtr => _dataView.getInt32(ix, true).As<object>(),
        //                    KnownTypeHandle.SystemUint32 or KnownTypeHandle.SystemUintPtr => _dataView.getUint32(ix, true).As<object>(),
        //                    KnownTypeHandle.SystemInt64 => _dataView.getBigInt64(ix, true).As<object>(),
        //                    KnownTypeHandle.SystemUint64 => _dataView.getBigUint64(ix, true).As<object>(),
        //                    KnownTypeHandle.SystemFloat => _dataView.getFloat32(ix, true).As<object>(),
        //                    KnownTypeHandle.SystemDouble => _dataView.getFloat64(ix, true).As<object>(),
        //                    // Handle other sizes as needed
        //                    _ => throw new NotSupportedException("Size not supported")
        //                };
        //                originalArray[i] = value;
        //            }
        //        }
        //    }
        //}


        void UpdateOriginalArrayItem(int arrayStartIndex, int byteStartIndex, int count)
        {
            if (_dataView != null)
            {
                //copy it, this dataView could be invalidated, while modifying the underlying array
                var dataView = _dataView;
                var knownType = Type.As<RuntimeType>()._model.As<TypeModel>().KnownType;
                Array originalArray = _array!;
                //byte should start in the right place. eg for 2 sized item [0,1] => 0, [2,3]=>2
                byteStartIndex = (byteStartIndex / SizeOfItem) * SizeOfItem;
                do
                {
                    var value = knownType switch
                    {
                        KnownTypeHandle.SystemSByte => dataView.getInt8(byteStartIndex).As<object>(),
                        KnownTypeHandle.SystemByte => dataView.getUint8(byteStartIndex).As<object>(),
                        KnownTypeHandle.SystemInt16 => dataView.getInt16(byteStartIndex, true).As<object>(),
                        KnownTypeHandle.SystemUInt16 or KnownTypeHandle.SystemChar => dataView.getUint16(byteStartIndex, true).As<object>(),
                        KnownTypeHandle.SystemInt32 or KnownTypeHandle.SystemIntPtr => dataView.getInt32(byteStartIndex, true).As<object>(),
                        KnownTypeHandle.SystemUint32 or KnownTypeHandle.SystemUintPtr => dataView.getUint32(byteStartIndex, true).As<object>(),
                        KnownTypeHandle.SystemInt64 => dataView.getBigInt64(byteStartIndex, true).As<object>(),
                        KnownTypeHandle.SystemUint64 => dataView.getBigUint64(byteStartIndex, true).As<object>(),
                        KnownTypeHandle.SystemSingle => dataView.getFloat32(byteStartIndex, true).As<object>(),
                        KnownTypeHandle.SystemDouble => dataView.getFloat64(byteStartIndex, true).As<object>(),
                        // Handle other sizes as needed
                        _ => throw new NotSupportedException("Size not supported")
                    };
                    originalArray[arrayStartIndex] = value;
                    byteStartIndex += SizeOfItem;
                    arrayStartIndex++;
                } while (--count > 0);
            }
        }

        public T GetAt(int offset)
        {
            if (NetJs.Script.TypeOf(offset).NativeEquals("bigint"))
                offset = NetJs.Script.Write<int>("Number(offset)");
            if (_parentRef != null)
            {
                var parentO = _parentRef.As<RefOrPointer<object>>();
                var arrayRootRef = GetRefWithBackingArray(out var byteOffset, out var arrayOffset).As<RefOrPointer<object>>();
                byteOffset += offset * SizeOfItem;
                var isNumeric = parentO.Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsNumeric() &&
                        Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsNumeric();
                if (arrayRootRef == null)
                    byteOffset = 0;
                if (isNumeric && (arrayRootRef != null || parentO._object != null))
                {
                    var sourceView = arrayRootRef?.DataView ?? parentO.DataView;
                    var knownType = Type.As<RuntimeType>()._model.As<TypeModel>().KnownType;
                    var value = knownType switch
                    {
                        KnownTypeHandle.SystemSByte => sourceView.getInt8(byteOffset).As<T>(),
                        KnownTypeHandle.SystemByte => sourceView.getUint8(byteOffset).As<T>(),
                        KnownTypeHandle.SystemInt16 => sourceView.getInt16(byteOffset, true).As<T>(),
                        KnownTypeHandle.SystemUInt16 or KnownTypeHandle.SystemChar => sourceView.getUint16(byteOffset, true).As<T>(),
                        KnownTypeHandle.SystemInt32 or KnownTypeHandle.SystemIntPtr => sourceView.getInt32(byteOffset, true).As<T>(),
                        KnownTypeHandle.SystemUint32 or KnownTypeHandle.SystemUintPtr => sourceView.getUint32(byteOffset, true).As<T>(),
                        KnownTypeHandle.SystemInt64 => sourceView.getBigInt64(byteOffset, true).As<T>(),
                        KnownTypeHandle.SystemUint64 => sourceView.getBigUint64(byteOffset, true).As<T>(),
                        KnownTypeHandle.SystemSingle => sourceView.getFloat32(byteOffset, true).As<T>(),
                        KnownTypeHandle.SystemDouble => sourceView.getFloat64(byteOffset, true).As<T>(),
                        _ => throw new NotSupportedException("Size not supported")
                    };
                    return value;
                }
                else
                {
                    offset += _arrayOffset;
                    var sourceSize = _parentRef.SizeOfItem;
                    var thisSize = SizeOfItem;
                    if (thisSize > sourceSize) //eg int > byte, getting int from underlying byte[]
                    {
                        var ratio = Math.DivRem(thisSize, sourceSize);
                        Debug.Assert(ratio.Remainder == 0);
                        ulong numeric = 0;
                        var isIntegerNumeric = _parentRef.Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsIntegerNumeric() &&
                            Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsIntegerNumeric();
                        var raw = !isIntegerNumeric ? new object[ratio.Quotient] : null;
                        for (int i = 0; i < ratio.Quotient; i++)
                        {
                            var value = parentO.GetAt(offset * ratio.Quotient + i);
                            if (isIntegerNumeric)
                            {
                                numeric |= (ulong)value.As<uint>() << (i * sourceSize * 8);
                            }
                            else
                            {
                                raw![i] = value;
                            }
                        }
                        if (isIntegerNumeric)
                        {
                            return (T)numeric.As<object>();
                        }
                        else if (Type.As<RuntimeType>()._model.As<TypeModel>().KnownType == KnownTypeHandle.SystemDouble)
                        {
                            NetJs.Script.Write("const bytes = new Uint8Array(raw)");
                            NetJs.Script.Write("const view = new DataView(bytes.buffer)");
                            return NetJs.Script.Write<T>("view.getFloat64(0, true)");
                        }
                        else if (Type.As<RuntimeType>()._model.As<TypeModel>().KnownType == KnownTypeHandle.SystemSingle)
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
                    else
                    {
                        return (T)parentO.GetAt(offset);
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
            offset += _arrayOffset;
            return _getter(offset);
        }

        public void SetAt(T value, int offset)
        {
            if (NetJs.Script.TypeOf(offset).NativeEquals("bigint"))
                offset = NetJs.Script.Write<int>("Number(offset)");
            if (_parentRef != null)
            {
                var parentO = _parentRef.As<RefOrPointer<object>>();
                var arrayRootRef = GetRefWithBackingArray(out var byteOffset, out var arrayOffset).As<RefOrPointer<object>?>();
                byteOffset += offset * SizeOfItem;
                int arrayOffsetInRoot = arrayRootRef != null ? byteOffset / arrayRootRef.SizeOfItem : 0;
                var isNumeric = parentO.Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsNumeric() &&
                        Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsNumeric();
                if (arrayRootRef == null)
                {
                    byteOffset = offset;
                }
                if (isNumeric && (arrayRootRef != null || parentO._object != null))
                {
                    var sourceView = arrayRootRef?.DataView ?? parentO.DataView;
                    var knownType = Type.As<RuntimeType>()._model.As<TypeModel>().KnownType;
                    switch (knownType)
                    {
                        case KnownTypeHandle.SystemByte:
                            sourceView.setUint8(byteOffset, value.As<byte>());
                            break;
                        case KnownTypeHandle.SystemSByte:
                            sourceView.setInt8(byteOffset, value.As<sbyte>());
                            break;
                        case KnownTypeHandle.SystemChar:
                        case KnownTypeHandle.SystemUInt16:
                            sourceView.setUint16(byteOffset, value.As<ushort>(), true);
                            break;
                        case KnownTypeHandle.SystemInt16:
                            sourceView.setInt16(byteOffset, value.As<short>(), true);
                            break;
                        case KnownTypeHandle.SystemUint32:
                        case KnownTypeHandle.SystemUintPtr:
                            sourceView.setUint32(byteOffset, value.As<uint>(), true);
                            break;
                        case KnownTypeHandle.SystemInt32:
                        case KnownTypeHandle.SystemIntPtr:
                            sourceView.setInt32(byteOffset, value.As<int>(), true);
                            break;
                        case KnownTypeHandle.SystemUint64:
                            sourceView.setBigUint64(byteOffset, value.As<ulong>(), true);
                            break;
                        case KnownTypeHandle.SystemInt64:
                            sourceView.setBigInt64(byteOffset, value.As<long>(), true);
                            break;
                        case KnownTypeHandle.SystemSingle:
                            sourceView.setFloat32(byteOffset, value.As<float>(), true);
                            break;
                        case KnownTypeHandle.SystemDouble:
                            sourceView.setFloat64(byteOffset, value.As<double>(), true);
                            break;
                        default:
                            throw new NotSupportedException("Size not supported");
                    }
                    if (arrayRootRef != null)
                    {
                        //update the original array from the dataView
                        //rootRef.UpdateOriginalArray();
                        arrayRootRef?.UpdateOriginalArrayItem(arrayOffsetInRoot, byteOffset, SizeOfItem / _parentRef.SizeOfItem);
                    }
                    else
                    {
                        var parentKnownType = parentO.Type.As<RuntimeType>()._model.As<TypeModel>().KnownType;
                        //Object reference, update the parent
                        var parentValue = parentKnownType switch
                        {
                            KnownTypeHandle.SystemSByte => sourceView.getInt8(0).As<T>(),
                            KnownTypeHandle.SystemByte => sourceView.getUint8(0).As<T>(),
                            KnownTypeHandle.SystemInt16 => sourceView.getInt16(0, true).As<T>(),
                            KnownTypeHandle.SystemUInt16 or KnownTypeHandle.SystemChar => sourceView.getUint16(0, true).As<T>(),
                            KnownTypeHandle.SystemInt32 or KnownTypeHandle.SystemIntPtr => sourceView.getInt32(0, true).As<T>(),
                            KnownTypeHandle.SystemUint32 or KnownTypeHandle.SystemUintPtr => sourceView.getUint32(0, true).As<T>(),
                            KnownTypeHandle.SystemInt64 => sourceView.getBigInt64(0, true).As<T>(),
                            KnownTypeHandle.SystemUint64 => sourceView.getBigUint64(0, true).As<T>(),
                            KnownTypeHandle.SystemSingle => sourceView.getFloat32(0, true).As<T>(),
                            KnownTypeHandle.SystemDouble => sourceView.getFloat64(0, true).As<T>(),
                            _ => throw new NotSupportedException("Size not supported")
                        };
                        parentO.SetAt(parentValue.As<object>(), 0);
                    }
                    return;
                }
                else
                {
                    offset += _arrayOffset;
                    var sourceSize = _parentRef.SizeOfItem;
                    var thisSize = SizeOfItem;
                    if (thisSize > sourceSize) //eg int > byte, setting int to underlying byte[]
                    {
                        var ratio = Math.DivRem(thisSize, sourceSize);
                        Debug.Assert(ratio.Remainder == 0);
                        var isIntegerNumeric = _parentRef.Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsIntegerNumeric() &&
                            Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsIntegerNumeric();
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
                            if (isIntegerNumeric)
                            {
                                var longValue = ((ulong)value.As<uint>() >> (i * sourceSize * 8)) & mask;
                                var parValue = NetJs.Script.Write<object>($"{NetJs.Constants.GlobalName}.{NetJs.Constants.CastName}({nameof(longValue)}, {nameof(parentPrototype)})");
                                parentO.SetAt(parValue, offset * ratio.Quotient + i);
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
                        var parValue = _parentRef.As<RefOrPointer<object>>().GetAt(offset / ratio.Quotient);
                        if (NetJs.Script.IsUndefined(parValue)) //this can happen with ref to unititialized local reference
                        {
                            parValue = 0.As<object>();
                        }
                        var longValue = (ulong)parValue.As<uint>();
                        var i = offset % ratio.Quotient;
                        if (_parentRef.Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsIntegerNumeric() && Type.As<RuntimeType>()._model.As<TypeModel>().KnownType.IsIntegerNumeric())
                        {
                            var maskSet = (ulong)value.As<uint>();
                            var maskClear = ~(0xffUL << (8 * i));
                            longValue = (longValue & maskClear) | (maskSet << (8 * i));
                            var parentPrototype = parentO.Type.As<RuntimeType>()._prototype;
                            var dd = NetJs.Script.Write<object>($"{NetJs.Constants.GlobalName}.{NetJs.Constants.CastName}({nameof(longValue)}, {nameof(parentPrototype)})");
                            parentO.SetAt(dd, offset / ratio.Quotient);
                        }
                        else
                        {
                            throw null;
                        }
                    }
                    else
                    {
                        parentO.SetAt(value.As<object>(), offset);
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
                _dataView = null; //if dataView exists, it is no longer valid, not in sync with the backing array
                offset += _arrayOffset;
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
                if (offset == 0)
                    return this;
                return this with
                {
                    _parentRef = this,
                    _byteOffset = offset * SizeOfItem,
                    _sizeOfItem = _sizeOfItem, //No point recomputing this if it is already computed
                    _getter = null!,
                    _setter = null!
                };
            }
        }

        public RefOrPointer<T> AddByteOffset(int offset)
        {
            if (offset == 0)
                return this;
            var root = GetRefWithBackingArray(out var totalByteOffset, out _);
            if (root != null)
            {
                return this with
                {
                    _parentRef = root,
                    _byteOffset = totalByteOffset + offset,
                    _sizeOfItem = _sizeOfItem, //No point recomputing this if it is already computed
                    _getter = null!,
                    _setter = null!
                };
            }
            return this with
            {
                _parentRef = this,
                _byteOffset = offset,
                _sizeOfItem = _sizeOfItem, //No point recomputing this if it is already computed
                _getter = null!,
                _setter = null!
            };
        }

        public RefOrPointer<T> Add(long offset)
        {
            int iOffset = (int)offset;
            if (iOffset == 0)
                return this;
            var root = GetRefWithBackingArray(out var totalByteOffset, out _);
            if (root != null)
            {
                return this with
                {
                    _parentRef = root,
                    _byteOffset = totalByteOffset + iOffset * SizeOfItem,
                    _sizeOfItem = _sizeOfItem, //No point recomputing this if it is already computed
                    _getter = null!,
                    _setter = null!
                };
            }
            return this with
            {
                _parentRef = this,
                _byteOffset = iOffset * SizeOfItem,
                _sizeOfItem = _sizeOfItem, //No point recomputing this if it is already computed
                _getter = null!,
                _setter = null!
            };
        }

        public void Advance(long offset)
        {
            int iOffset = (int)offset;
            _byteOffset += iOffset * SizeOfItem;
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

        public IRefOrPointer? GetRefWithBackingArray(out int byteOffset, out int arrayOffset)
        {
            IRefOrPointer? where = this;
            int bOffset = 0;
            int arrOffset = 0;
            while (where != null)
            {
                bOffset += where.As<RefOrPointer<object>>()._byteOffset;
                arrOffset += where.As<RefOrPointer<object>>()._arrayOffset;
                if (where.As<RefOrPointer<object>>()._parentRef == null &&
                    where.As<RefOrPointer<object>>()._array != null)
                {
                    arrayOffset = arrOffset;
                    byteOffset = bOffset;
                    return where;
                }
                where = where.As<RefOrPointer<object>>()._parentRef;
            }
            byteOffset = -1;
            arrayOffset = -1;
            return null;
        }
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

        /// <summary>
        /// We already ascetain that size if from and to are the same, just check if safe to cast their bits
        /// </summary>
        /// <param name="tfrom"></param>
        /// <param name="tto"></param>
        /// <returns></returns>
        protected static bool BitsIsDirectlyConvertible(Type tfrom, Type tto)
        {
            var tFrom = tfrom.As<RuntimeType>()._prototype!.Metadata!.KnownType;
            var tTo = tto.As<RuntimeType>()._prototype!.Metadata!.KnownType;
            if (tFrom.IsNumeric() && tTo.IsNumeric())
                return true;
            return false;
        }
    }

    public record class Ref<T> : RefOrPointer<T>
    {
        protected Ref(Ref<T> original) : base(original)
        {
        }

        internal Ref(IRefOrPointer parent) : base(parent)
        {
        }

        internal Ref([NativeDelegate] Func<int?, T> getter, [NativeDelegate] Action<T, int?> setter) : base(getter, setter)
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
                if (toSize != fromSize || (rref.Type != typeof(T) && BitsIsDirectlyConvertible(rref.Type, typeof(T)))  /*size is equal, and bits is directly convertible. eg double => long*/)
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

        internal Pointer([NativeDelegate] Func<int?, T> getter, [NativeDelegate] Action<T, int?> setter) : base(getter, setter)
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
                if (toSize != fromSize || (rref.Type != typeof(T) && BitsIsDirectlyConvertible(rref.Type, typeof(T)))  /*size is equal, and bits is directly convertible. eg double => long*/)
                {
                    var newRef = new Pointer<T>(rref);
                    result = newRef;
                }
                return true;
            }
            return false;
        }
    }
}
