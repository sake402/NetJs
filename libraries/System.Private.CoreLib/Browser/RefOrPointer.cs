using System;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Window;

namespace NetJs
{
    //public interface IRefOrPointer
    //{
    //    //int? _arrayOffset { get; }
    //}

    public abstract class RefOrPointer
    {
        static internal readonly Pointer<object> _pinnedPointer = new Pointer<object>(null!, null!);
        public abstract int SizeOfItem { get; }
        public abstract Type Type { get; }
        public abstract object? RefValue { get; set; }
        public abstract RefOrPointer Clone();
        internal abstract bool IsPointer { get; }
        public static int Compare(RefOrPointer? first, RefOrPointer? second)
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
            Debug.Assert(first.As<RefOrPointer<object>>().Overlaps(second), "Reference/Pointer must overlap to compare them");
            return (first.As<RefOrPointer<object>>()._arrayOffset ?? 0) - (second.As<RefOrPointer<object>>()._arrayOffset ?? 0);
        }
        public RefOrPointer GetRefWithBackingArrayOrObject(out int byteOffset, out int arrayOffset)
        {
            RefOrPointer? where = this;
            int bOffset = 0;
            int arrOffset = 0;
            while (where != null)
            {
                bOffset += where.As<RefOrPointer<object>>()._byteOffset ?? 0;
                arrOffset += where.As<RefOrPointer<object>>()._arrayOffset ?? 0;
                if (where.As<RefOrPointer<object>>()._array != null)
                {
                    arrayOffset = arrOffset;
                    byteOffset = bOffset;
                    return where;
                }
                else if (where.As<RefOrPointer<object>>().__dataView != null)
                {
                    arrayOffset = arrOffset;
                    byteOffset = bOffset;
                    return where;
                }
                else if (where.As<RefOrPointer<object>>()._object != null)
                {
                    //if (where.As<RefOrPointer<object>>().Type.As<RuntimeType>()._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsInlineArray) ||
                    //    where.As<RefOrPointer<object>>().Type.As<RuntimeType>()._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsPureStruct))
                    //{
                    arrayOffset = arrOffset;
                    byteOffset = bOffset;
                    return where;
                    //}
                }
                else if (where.As<RefOrPointer<object>>()._parentRef != null)
                {
                    where = where.As<RefOrPointer<object>>()._parentRef;
                }
                else
                {
                    arrayOffset = arrOffset;
                    byteOffset = bOffset;
                    return where;
                }
            }
            byteOffset = 0;
            arrayOffset = 0;
            return this;
        }

        [NetJs.Name(Constants.DefaultTypeName)]
        public static RefOrPointer Default()
        {
            return _pinnedPointer;
        }
    }

    public abstract class RefOrPointer<T> : RefOrPointer
    {
        //Faster dataSourcetype instead of slow casting every time
        [NetJs.External]
        enum DataSourceType
        {
            _object,
            _array,
            _dataView,
            _parentRef
        }
        internal uint _virtualAddress;
        internal object? _dataSource;
        DataSourceType? _dataSourceType;
        internal int? _byteOffset;
        internal NativeFunction<int?, T> _getter;
        internal NativeAction<T, int?> _setter;
        internal T? _object
        {
            get
            {
                if (_dataSourceType == DataSourceType._object)
                    return _dataSource.As<T>();
                if (_dataSourceType != null)
                    return ((object?)null).As<T>();
                if (_dataSource is RefOrPointer)
                    return ((object?)null).As<T>();
                if (ReferenceEquals(Type, typeof(void)))//void *
                {
                    _dataSourceType = DataSourceType._object;
                    return _dataSource.As<T>();
                }
                if (_dataSource is T t)
                {
                    _dataSourceType = DataSourceType._object;
                    return t;
                }
                return ((object?)null).As<T>();
            }
        }

        internal T[]? _array
        {
            get
            {
                if (_dataSourceType == DataSourceType._array)
                    return _dataSource.As<T[]>();
                if (_dataSourceType != null)
                    return null;
                if (_dataSource != null && _dataSource[NetJs.Constants.IsProxy].As<bool>() == true) //compat with proxy array (ArrayProxyHandler), which is not a real array, but a proxy to an array
                {
                    var handler = _dataSource[NetJs.Constants.ProxyHandler];
                    if (handler is IArrayProxyHandler)
                    {
                        _dataSourceType = DataSourceType._array;
                        return _dataSource.As<T[]>();
                    }
                }
                if (_dataSource is T[] ts)
                {
                    _dataSourceType = DataSourceType._array;
                    return ts;
                }
                return null;
            }
        }
        internal DataView? __dataView
        {
            get
            {
                if (_dataSourceType == DataSourceType._dataView)
                    return _dataSource.As<DataView>();
                if (_dataSourceType != null)
                    return null;
                if (_dataSource is DataView dv)
                {
                    _dataSourceType = DataSourceType._dataView;
                    return dv;
                }
                return null;
            }
        }

        internal RefOrPointer? _parentRef
        {
            get
            {
                if (_dataSourceType == DataSourceType._parentRef)
                    return _dataSource.As<RefOrPointer>();
                if (_dataSourceType != null)
                    return null;
                //TChar* p = stringPointer;
                //ref p
                //dataType is a reference, like int*, this make this ref itself an int** or ref int*. It is an _object(allows us to replace the whole _object) reference to p
                //if (typeof(RefOrPointer).IsAssignableFrom(typeof(T)))
                var kt = Type.As<RuntimeType>()._prototype.KnownType;
                if (kt == KnownTypeHandle.SystemReference || kt == KnownTypeHandle.SystemPointer)
                    return null;
                if (_dataSource is RefOrPointer ro)
                {
                    _dataSourceType = DataSourceType._parentRef;
                    return ro;
                }
                return null;
            }
        }

        internal int? _arrayOffset => _byteOffset == null ? null : _byteOffset == 0 ? 0 : _byteOffset / SizeOfItem;
        internal RefOrPointer(RefOrPointer parent)
        {
            //TODO: We dont want too much nested ref, walk to the root and use that
            //var root = parent.GetRefWithBackingArrayOrObject(out var byteOffset, out _);
            this._dataSource = parent;
            _dataSourceType = DataSourceType._parentRef;
        }

        internal RefOrPointer(NativeFunction<int?, T> getter, NativeAction<T, int?> setter)
        {
            this._getter = getter;
            this._setter = setter;
        }

        [Name(NetJs.Constants.RefValueName)]
        T v
        {
            get => GetAt();
            set => SetAt(value);
        }

        internal int? _sizeOfItem;
        public override int SizeOfItem => _sizeOfItem ??= Marshal.CalculateSizeOf(Type.As<RuntimeType>());
        internal Type? _type;
        public override Type Type => _type ??= typeof(T);
        bool IsConvertibleToNativeByteArray => Type.As<RuntimeType>()._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsInlineArray) ||
            Type.As<RuntimeType>()._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsPureStruct);
        public T Value
        {
            get => GetAt();
            set => SetAt(value);
        }

        public override object? RefValue
        {
            get => GetAt(0);
            set => SetAt((T)value, 0);
        }

        byte[]? GetNativeByteArray()
        {
            if (_object != null && IsConvertibleToNativeByteArray)
            {
                return RuntimeHelpers.StructToByteArray(_object);
            }
            return null;
        }

        public Array? GetBackingArray()
        {
            if (_object != null && IsConvertibleToNativeByteArray)
            {
                return _object.fieldsToObjectArray;
            }
            return _array;
        }

        public T[] ToArray(int length = -1)
        {
            var root = GetRefWithBackingArrayOrObject(out var byteOffset, out var arrayOffset).As<RefOrPointer<T>>();
            var arr = root._array;
            var obj = root._object;
            if (arr == null && obj != null)
                return [obj];
            if (arr == null)
                throw new InvalidOperationException("Not based on an array");
            if (arrayOffset == 0 && length < 0)
                return arr.As<T[]>();
            int start = arrayOffset;
            if (length < 0)
                length = arr.Length - start;
            var newArray = new T[length];
            Array.Copy(arr, start, newArray, 0, length);
            return newArray;
        }

        internal DataView? _dataView;
        public DataView GetDataView()
        {
            if (this == RefOrPointer._pinnedPointer.As<RefOrPointer<T>>())
                return null!;
            if (__dataView != null)
                return __dataView;
            if (_dataSourceType == DataSourceType._parentRef || _dataSource is RefOrPointer)
                return _dataSource.As<RefOrPointer<object>>().GetDataView();
            if (_dataView != null)
                return _dataView;
            var array = GetBackingArray().As<T[]>();
            if (array == null && _object == null)
                throw new InvalidOperationException("Not based on an array/object");
            var knownType = Type.As<RuntimeType>()._prototype.KnownType;
            if (knownType == KnownTypeHandle.SystemEnum)
            {
                knownType = Type.As<RuntimeType>()._prototype.As<EnumPrototype>().UnderlyingType.KnownType;
            }
            if (!knownType.IsNumeric())
                throw new InvalidOperationException("Not supported on non-numeric types");
            if (_byteOffset != null)
                throw new InvalidOperationException("Not supported on non-root ref");
            T[] arr = array ?? NetJs.Script.CreateArrayFromValues(_object!);
            var bytes = RuntimeHelpers.ArrayBufferFrom(arr, knownType) ?? throw new NotSupportedException("Size not supported");
            _dataView = new DataView(bytes);
            //_dataView["array"] = arr;
            return _dataView;
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


        //void UpdateOriginalArrayItem(int arrayStartIndex, int byteStartIndex, int itemCount)
        //{
        //    if (_dataView != null)
        //    {
        //        RuntimeHelpers.UpdateArrayFromDataView(_array!, Type, _dataView, arrayStartIndex, byteStartIndex, itemCount);
        //        ////copy it, this dataView could be invalidated, while modifying the underlying array
        //        //var dataView = _dataView;
        //        //var knownType = Type.As<RuntimeType>()._prototype.KnownType;
        //        //if (knownType == KnownTypeHandle.SystemEnum)
        //        //{
        //        //    knownType = Type.As<RuntimeType>()._prototype.As<EnumPrototype>().UnderlyingType.KnownType;
        //        //}
        //        //Array originalArray = _array!;
        //        ////byte should start in the right place. eg for 2 sized item [0,1] => 0, [2,3]=>2
        //        //byteStartIndex = (byteStartIndex / SizeOfItem) * SizeOfItem;
        //        //do
        //        //{
        //        //    var value = RuntimeHelpers.ReadDataView<object>(dataView, knownType, byteStartIndex);
        //        //    //var value = knownType switch
        //        //    //{
        //        //    //    KnownTypeHandle.SystemSByte => dataView.getInt8(byteStartIndex).As<object>(),
        //        //    //    KnownTypeHandle.SystemByte => dataView.getUint8(byteStartIndex).As<object>(),
        //        //    //    KnownTypeHandle.SystemInt16 => dataView.getInt16(byteStartIndex, true).As<object>(),
        //        //    //    KnownTypeHandle.SystemUInt16 or KnownTypeHandle.SystemChar => dataView.getUint16(byteStartIndex, true).As<object>(),
        //        //    //    KnownTypeHandle.SystemInt32 or KnownTypeHandle.SystemIntPtr => dataView.getInt32(byteStartIndex, true).As<object>(),
        //        //    //    KnownTypeHandle.SystemUint32 or KnownTypeHandle.SystemUIntPtr => dataView.getUint32(byteStartIndex, true).As<object>(),
        //        //    //    KnownTypeHandle.SystemInt64 => dataView.getBigInt64(byteStartIndex, true).As<object>(),
        //        //    //    KnownTypeHandle.SystemUint64 => dataView.getBigUint64(byteStartIndex, true).As<object>(),
        //        //    //    KnownTypeHandle.SystemSingle => dataView.getFloat32(byteStartIndex, true).As<object>(),
        //        //    //    KnownTypeHandle.SystemDouble => dataView.getFloat64(byteStartIndex, true).As<object>(),
        //        //    //    // Handle other sizes as needed
        //        //    //    _ => throw new NotSupportedException("Size not supported")
        //        //    //};
        //        //    originalArray[arrayStartIndex] = value;
        //        //    byteStartIndex += SizeOfItem;
        //        //    arrayStartIndex++;
        //        //} while (--count > 0);
        //    }
        //}

        public T GetAt(int? _offset = null)
        {
            if (NetJs.Script.TypeOf(_offset).NativeEquals("bigint"))
                _offset = NetJs.Script.Write<int>("Number(_offset)");
            int offset = _offset ?? 0;
            if (_parentRef != null)
            {
                var parentO = _parentRef.As<RefOrPointer<object>>();
                var arrayRootRef = GetRefWithBackingArrayOrObject(out var byteOffset, out var arrayOffset).As<RefOrPointer<object>>();
                if (byteOffset < 0)
                    byteOffset = 0;
                byteOffset += offset * SizeOfItem;
                var thisIsNumeric = Type.As<RuntimeType>()._prototype.KnownType.IsNumeric();
                var parentIsNumeric = arrayRootRef != null ? arrayRootRef.Type.As<RuntimeType>()._prototype.KnownType.IsNumeric() : false;
                var isNumeric = parentIsNumeric && thisIsNumeric;
                //var parentIsPureStruct = parentO.Type.As<RuntimeType>()._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsPureStruct);
                //if (arrayRootRef == null)
                //byteOffset = 0;
                if (isNumeric && (arrayRootRef?._array != null || arrayRootRef?._object != null))
                {
                    var sourceView = arrayRootRef.GetDataView();
                    var knownType = Type.As<RuntimeType>()._prototype.KnownType;
                    if (knownType == KnownTypeHandle.SystemEnum)
                    {
                        knownType = Type.As<RuntimeType>()._prototype.As<EnumPrototype>().UnderlyingType.KnownType;
                    }
                    var value = RuntimeHelpers.ReadDataView<T>(sourceView, knownType, byteOffset);
                    return value;
                }
                else
                {
                    var isIntegerNumeric = _parentRef.Type.As<RuntimeType>()._prototype.KnownType.IsIntegerNumeric() &&
                        Type.As<RuntimeType>()._prototype.KnownType.IsIntegerNumeric();
                    offset += _arrayOffset ?? 0;
                    var sourceSize = _parentRef.SizeOfItem;
                    var thisSize = SizeOfItem;
                    if (thisSize > sourceSize) //eg int > byte, getting int from underlying byte[]
                    {
                        var ratio = Math.DivRem(thisSize, sourceSize);
                        Debug.Assert(ratio.Remainder == 0);
                        ulong numeric = 0;
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
                        else if (Type.As<RuntimeType>()._prototype.KnownType == KnownTypeHandle.SystemDouble)
                        {
                            NetJs.Script.Write("const bytes = new Uint8Array(raw)");
                            NetJs.Script.Write("const view = new DataView(bytes.buffer)");
                            return NetJs.Script.Write<T>("view.getFloat64(0, true)");
                        }
                        else if (Type.As<RuntimeType>()._prototype.KnownType == KnownTypeHandle.SystemSingle)
                        {
                            NetJs.Script.Write("const bytes = new Uint8Array(raw)");
                            NetJs.Script.Write("const view = new DataView(bytes.buffer)");
                            return NetJs.Script.Write<T>("view.getFloat32(0, true)");
                        }
                    }
                    else if (thisSize < sourceSize) //eg byte < int, getting byte from underlying int[]
                    {
                        var ratio = Math.DivRem(sourceSize, thisSize);
                        Debug.Assert(ratio.Remainder == 0);
                        var d = (ulong)parentO.GetAt(offset / ratio.Quotient).As<uint>();
                        var i = offset % ratio.Quotient;
                        if (isIntegerNumeric)
                        {
                            return (T)(d >> (8 * i)).As<object>();
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
            offset += _arrayOffset ?? 0;
            if (_object.As<object>() != null && Type.As<RuntimeType>()._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsInlineArray))
            {
                return _object!.fieldsToObjectArray[offset].As<T>();
            }
            else if (_object.As<object>() != null && _object!.GetClassPrototype().Flags.TypeHasFlag(TypeFlagsModel.IsValueType) && Type.As<RuntimeType>()._prototype.KnownType.IsNumeric())
            {
                return _object!.GetField(offset, Type.As<RuntimeType>()._prototype).As<T>();
            }
            else
                return _getter(_byteOffset == null && _offset == null ? null : offset);
        }

        public void SetAt(T value, int? _offset = null)
        {
            if (NetJs.Script.TypeOf(_offset).NativeEquals("bigint"))
                _offset = NetJs.Script.Write<int>("Number(_offset)");
            int offset = _offset ?? 0;
            if (_parentRef != null)
            {
                var parentO = _parentRef.As<RefOrPointer<object>>();
                var arrayRootRef = GetRefWithBackingArrayOrObject(out var byteOffset, out var arrayOffset).As<RefOrPointer<object>?>();
                if (byteOffset < 0)
                    byteOffset = 0;
                byteOffset += offset * SizeOfItem;
                int arrayOffsetInRoot = arrayRootRef != null ? byteOffset / arrayRootRef.SizeOfItem : 0;
                var thisIsNumeric = Type.As<RuntimeType>()._prototype.KnownType.IsNumeric();
                var parentIsNumeric = arrayRootRef != null ? arrayRootRef.Type.As<RuntimeType>()._prototype.KnownType.IsNumeric() : false;
                var isNumeric = parentIsNumeric && thisIsNumeric;
                if (isNumeric && (arrayRootRef?._array != null || arrayRootRef?._object != null))
                {
                    var sourceView = arrayRootRef.GetDataView();// ?? parentO.GetDataView();
                    var knownType = Type.As<RuntimeType>()._prototype.KnownType;
                    if (knownType == KnownTypeHandle.SystemEnum)
                    {
                        knownType = Type.As<RuntimeType>()._prototype.As<EnumPrototype>().UnderlyingType.KnownType;
                    }
                    RuntimeHelpers.WriteDataView<T>(sourceView, knownType, value, byteOffset);
                    //update the original array from the dataView
                    if (arrayRootRef._array != null)
                        RuntimeHelpers.UpdateArrayFromDataView(arrayRootRef._array!, arrayRootRef.Type, sourceView, byteOffset, SizeOfItem / arrayRootRef.SizeOfItem);
                    else
                    {
                        var rootKnownType = parentO.Type.As<RuntimeType>()._prototype.KnownType;
                        if (rootKnownType == KnownTypeHandle.SystemEnum)
                        {
                            rootKnownType = arrayRootRef.Type.As<RuntimeType>()._prototype.As<EnumPrototype>().UnderlyingType.KnownType;
                        }
                        var rootValue = RuntimeHelpers.ReadDataView<object>(sourceView, rootKnownType, 0);
                        arrayRootRef.SetAt(rootValue.As<object>(), 0);
                    }
                    return;
                }
                else
                {
                    var isIntegerNumeric = _parentRef.Type.As<RuntimeType>()._prototype.KnownType.IsIntegerNumeric() &&
                        Type.As<RuntimeType>()._prototype.KnownType.IsIntegerNumeric();
                    offset += _arrayOffset ?? 0;
                    var sourceSize = _parentRef.SizeOfItem;
                    var thisSize = SizeOfItem;
                    if (thisSize > sourceSize) //eg int > byte, setting int to underlying byte[]
                    {
                        if (isIntegerNumeric)
                        {
                            var ratio = Math.DivRem(thisSize, sourceSize);
                            Debug.Assert(ratio.Remainder == 0);
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
                                var longValue = ((ulong)value.As<uint>() >> (i * sourceSize * 8)) & mask;
                                var parValue = NetJs.Script.Write<object>($"{NetJs.Constants.GlobalName}.{NetJs.Constants.CastName}({nameof(longValue)}, {nameof(parentPrototype)})");
                                parentO.SetAt(parValue, offset * ratio.Quotient + i);
                            }
                            return;
                        }
                    }
                    else if (thisSize < sourceSize) //eg byte < int, setting byte to underlying int[]
                    {
                        if (isIntegerNumeric)
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
                            var maskSet = (ulong)value.As<uint>();
                            var maskClear = ~(0xffUL << (8 * i));
                            longValue = (longValue & maskClear) | (maskSet << (8 * i));
                            var parentPrototype = parentO.Type.As<RuntimeType>()._prototype;
                            var dd = NetJs.Script.Write<object>($"{NetJs.Constants.GlobalName}.{NetJs.Constants.CastName}({nameof(longValue)}, {nameof(parentPrototype)})");
                            parentO.SetAt(dd, offset / ratio.Quotient);
                            return;
                        }
                    }
                    parentO.SetAt(value.As<object>(), _byteOffset == null && _offset == null ? null : offset);
                    return;
                }
            }
            else
            {
                _dataView = null; //if dataView exists, it is no longer valid, not in sync with the backing array
                offset += _arrayOffset ?? 0;
                if (_object.As<object>() != null && Type.As<RuntimeType>()._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsInlineArray))
                {
                    _object!.fieldsToObjectArray[offset] = value.As<T>()!;
                }
                else if (_object.As<object>() != null && _object!.GetClassPrototype().Flags.TypeHasFlag(TypeFlagsModel.IsValueType) && Type.As<RuntimeType>()._prototype.KnownType.IsNumeric())
                {
                    _object.As<object>().SetField(offset, Type.As<RuntimeType>()._prototype, value.As<object>());
                }
                else
                    _setter(value, _byteOffset == null && _offset == null ? null : offset);
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
                dst.Value.As<Array>()[(dst._arrayOffset ?? 0) + i] = Value.As<Array>()[(_arrayOffset ?? 0) + i];
            }
        }

        public RefOrPointer<T> this[int offset]
        {
            get
            {
                if (offset == 0 && _byteOffset != null)
                    return this;
                var clone = Clone().As<RefOrPointer<T>>();
                clone._dataSource = this;
                clone._byteOffset = offset * SizeOfItem;
                clone._sizeOfItem = _sizeOfItem; //No point recomputing this if it is already computed
                clone._getter = null!;
                clone._setter = null!;
                return clone;
                //return this with
                //{
                //    _parentRef = this,
                //    _byteOffset = offset * SizeOfItem,
                //    _sizeOfItem = _sizeOfItem, //No point recomputing this if it is already computed
                //    _getter = null!,
                //    _setter = null!
                //};
            }
        }

        public RefOrPointer<T> AddByteOffset(int offset)
        {
            if (offset == 0 && _byteOffset != null)
                return this;
            var root = GetRefWithBackingArrayOrObject(out var totalByteOffset, out _);
            //if (root != null)
            //{
            RefOrPointer<T> clone = IsPointer ? new Pointer<T>(root) : new Ref<T>(root);
            clone._dataSource = root;
            clone._byteOffset = totalByteOffset + offset;
            clone._sizeOfItem = _sizeOfItem; //No point recomputing this if it is already computedy
            clone._type = _type;
            clone._getter = null!;
            clone._setter = null!;
            return clone;
            //return this with
            //{
            //    _parentRef = root,
            //    _byteOffset = totalByteOffset + offset,
            //    _sizeOfItem = _sizeOfItem, //No point recomputing this if it is already computed
            //    _getter = null!,
            //    _setter = null!
            //};
            //}
            //RefOrPointer<T> clone2 = IsPointer ? new Pointer<T>(this) : new Ref<T>(this);
            //clone2._dataSource = this;
            //clone2._byteOffset = offset;
            //clone2._sizeOfItem = _sizeOfItem; //No point recomputing this if it is already computed
            //clone2._type = _type;
            //clone2._getter = null!;
            //clone2._setter = null!;
            //return clone2;
            //return this with
            //{
            //    _parentRef = this,
            //    _byteOffset = offset,
            //    _sizeOfItem = _sizeOfItem, //No point recomputing this if it is already computed
            //    _getter = null!,
            //    _setter = null!
            //};
        }

        public RefOrPointer<T> Add(long offset)
        {
            if (offset == 0 && _byteOffset != null)
                return this;
            int iOffset = (int)offset;
            var root = GetRefWithBackingArrayOrObject(out var totalByteOffset, out _);
            //if (root != null)
            //{
            RefOrPointer<T> clone = IsPointer ? new Pointer<T>(root) : new Ref<T>(root);
            clone._dataSource = root;
            clone._byteOffset = totalByteOffset + iOffset * SizeOfItem;
            clone._sizeOfItem = _sizeOfItem; //No point recomputing this if it is already computed
            clone._type = _type;
            clone._getter = null!;
            clone._setter = null!;
            return clone;
            //return this with
            //{
            //    _parentRef = root,
            //    _byteOffset = totalByteOffset + iOffset * SizeOfItem,
            //    _sizeOfItem = _sizeOfItem, //No point recomputing this if it is already computed
            //    _getter = null!,
            //    _setter = null!
            //};
            //}
            //RefOrPointer<T> clone2 = IsPointer ? new Pointer<T>(this) : new Ref<T>(this);
            //clone2._dataSource = this;
            //clone2._byteOffset = iOffset * SizeOfItem;
            //clone2._sizeOfItem = _sizeOfItem; //No point recomputing this if it is already computed
            //clone2._type = _type;
            //clone2._getter = null!;
            //clone2._setter = null!;
            //return clone2;
            //return this with
            //{
            //    _parentRef = this,
            //    _byteOffset = iOffset * SizeOfItem,
            //    _sizeOfItem = _sizeOfItem, //No point recomputing this if it is already computed
            //    _getter = null!,
            //    _setter = null!
            //};
        }

        public void Advance(long offset)
        {
            int iOffset = (int)offset;
            _byteOffset += iOffset * SizeOfItem;
        }

        public bool Overlaps(RefOrPointer? second)
        {
            if (second == null)
                return false;
            //Subtracting two pointers should point to same memory allocation
            var array1 = _array;
            var array2 = second.As<RefOrPointer<object>>()._array;
            //if (array1 is not null || array2 is not null)
            //return ReferenceEquals(array1, array2);
            var root1 = GetRefWithBackingArrayOrObject(out _, out _);
            var root2 = second.As<RefOrPointer<object>>().GetRefWithBackingArrayOrObject(out _, out _);
            return ReferenceEquals(root1, root2);
            //var parent1 = _parentRef.As<RefOrPointer<object>>();
            //var parent2 = second.As<RefOrPointer<object>>()._parentRef.As<RefOrPointer<object>>();
            //if (parent1 is not null && parent2 is not null)
            //    return parent1.Overlaps(parent2);
            //return ReferenceEquals(parent1, parent2);
        }

        public long Subtract(RefOrPointer second)
        {
            //Subtracting two pointers should point to same memory allocation
            Debug.Assert(Overlaps(second), "Reference/Pointer must overlap before comparing them");
            return (_arrayOffset ?? 0) - (second.As<RefOrPointer<object>>()._arrayOffset ?? 0);
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

        ///// <summary>
        ///// We already ascetain that size if from and to are the same, just check if safe to cast their bits
        ///// </summary>
        ///// <param name="tfrom"></param>
        ///// <param name="tto"></param>
        ///// <returns></returns>
        //protected static bool BitsIsDirectlyConvertible(Type tfrom, Type tto)
        //{
        //    var tFrom = tfrom.As<RuntimeType>()._prototype!.KnownType;
        //    var tTo = tto.As<RuntimeType>()._prototype!.KnownType;
        //    if (tFrom.IsNumeric() && tTo.IsNumeric())
        //        return true;
        //    return false;
        //}

        //Minimize GC pressure with caching of refs from a ref;
        internal SimpleDictionary<RefOrPointer>? castCache;
        public RefOrPointer<TTo>? Cast<TTo>()
        {
            var toModel = typeof(TTo).As<RuntimeType>()._prototype;
            var fromModel = Type.As<RuntimeType>()._prototype;
            if (fromModel == toModel)
                return this.As<RefOrPointer<TTo>>();
            var toCacheKey = toModel.TypeHandle;
            var fromCacheKey = fromModel.TypeHandle;
            if (castCache != null)
            {
                var val = castCache[toCacheKey.As<int>()];
                if (NetJs.Script.IsDefined(val))
                    return val.As<RefOrPointer<TTo>>();
            }
            RefOrPointer<TTo>? AddToCache(RefOrPointer<TTo> result)
            {
                castCache ??= new();
                castCache[toCacheKey.As<int>()] = result;
                result.castCache ??= new();
                result.castCache[fromCacheKey.As<int>()] = this;
                return result;
            }
            var toSize = Marshal.CalculateSizeOf(typeof(TTo).As<RuntimeType>());
            var fromSize = SizeOfItem;
            //var thisIsPointer = fromModel.Name.NativeStartsWith(nameof(Pointer<>));

            //If both are numeric type, create a new TTo ref such that it can read from the TFrom ref
            if (/*fromSize != toSize && */toModel.KnownType.IsNumeric() && fromModel.KnownType.IsNumeric())
            {
                if (IsPointer)
                    return AddToCache(new Pointer<TTo>(this));
                else
                    return AddToCache(new Ref<TTo>(this));
            }

            //Casting a struct pointer to a numeric pointer ((byte*)&guid) should return a reference to the backing fields array of the struct
            if (_object.As<object>() != null && fromModel.Flags.TypeHasFlag(TypeFlagsModel.IsValueType | TypeFlagsModel.IsPureStruct) && toModel.KnownType.IsNumeric())
            {
                return AddToCache(_object!.GetFieldRefOrPointer<TTo>(0, IsPointer));
            }

            //If we are casting a struct to a struct, non primitive
            //OR
            //If we are tring to cast a numeric array to a structlayout object,
            //allow it by pulling the provided array into the backing field array of the target type
            bool castingStructToStruct = toModel.Flags.TypeHasFlag(TypeFlagsModel.IsValueType) &&
            fromModel.Flags.TypeHasFlag(TypeFlagsModel.IsValueType) &&
            !toModel.KnownType.IsPrimitive() &&
            !fromModel.KnownType.IsPrimitive();

            bool castingNumericToStruct = toModel.Flags.TypeHasFlag(TypeFlagsModel.IsValueType | TypeFlagsModel.IsStructLayout) &&
            !toModel.KnownType.IsNumeric() &&
            fromModel.KnownType.IsIntegerNumeric();

            if (castingStructToStruct || castingNumericToStruct)
            {
                var root = GetRefWithBackingArrayOrObject(out _, out int arrayOffset).As<RefOrPointer<object>>();
                if (root != null)
                {
                    Array? array = root._array;
                    object? obj = root._object;
                    if (array != null && arrayOffset > 0)
                    {
                        array = JSProxy.Create<Array>(new ArrayWindowProxyHandler(array, arrayOffset, array.Length - arrayOffset));
                    }
                    var newObject = toModel.New().As<TTo>()!;// NetJs.Script.Write<TTo>("new TTo()")!;
                    //var newObject = Activator.CreateInstance<TTo>()!;
                    if (obj != null)
                        newObject._fields = obj._fields;
                    else if (array != null)
                    {
                        ArrayBuffer? buffer;
                        if (newObject.IsPureStruct && (buffer = RuntimeHelpers.ArrayBufferFrom(array, null)) != null)
                        {
                            var dataView = new DataView(buffer);
                            newObject._fields = dataView;
                        }
                        else
                        {
                            newObject._fields = array.As<object[]>();
                        }
                    }
                    newObject.innerObjects = null;
                    var result = RuntimeHelpers.CreateObjectReferenceT<TTo>((i) =>
                    {
                        return newObject;
                        throw null!;
                    }, (value, i) =>
                    {
                        if (_byteOffset == null/*reference is not being indexed, safe to handle as object replacement*/ && value is TTo)//Need to be sure, another runtime type could find its way here, depend on ref usage
                        {
                            newObject._fields = value!._fields;
                            if (obj != null)
                                obj._fields = value._fields;
                            if (array != null)
                                RuntimeHelpers.UpdateArrayFromDataView(array, Type, value.fieldsAsDataView);
                        }
                        else if (i != null || _arrayOffset != null) //object is being array indexed
                        {
                            var index = i ?? _arrayOffset;
                            if (array != null)
                            {
                                unsafe
                                {
                                    if (castingNumericToStruct && value is TTo)
                                    {
                                        RuntimeHelpers.UpdateArrayFromDataView(array, Type, value.fieldsAsDataView);
                                    }
                                    else
                                    {
                                        array[i.As<int>()] = value.As<object>();
                                    }
                                }
                            }
                            else
                            {
                                newObject.As<object>().SetField(index.As<int>(), 1, value.As<object>());
                                if (obj != null)
                                    obj.SetField(index.As<int>(), 1, value.As<object>()); //we expect the backing field of this original object and newObject to be the same. But we update the original too anyway
                            }
                        }
                        else
                            throw null!;
                    });
                    //result._type = typeof(TTo);
                    return AddToCache(result);
                }
                else
                {
                    throw null!;
                }
            }

            //if (!fromModel.Flags.TypeHasFlag(TypeFlagsModel.IsValueType) && toModel.KnownType == KnownTypeHandle.SystemObject)
            //{
            //if (IsPointer)
            //    return new Pointer<TTo>(this);
            //else
            //    return new Ref<TTo>(this);
            //}

            //if (toSize != fromSize || (Type != typeof(TTo) && BitsIsDirectlyConvertible(Type, typeof(TTo)))  /*size is equal, and bits is directly convertible. eg double => long*/)
            //{
            //    var newRef = new Pointer<TTo>(this);
            //    return newRef;
            //}

            return null;
        }
    }

    public class Ref<T> : RefOrPointer<T>
    {
        protected Ref(Ref<T> original) : base(original)
        {
        }

        internal Ref(RefOrPointer parent) : base(parent)
        {
        }

        internal Ref(NativeFunction<int?, T> getter, NativeAction<T, int?> setter) : base(getter, setter)
        {
        }

        internal override bool IsPointer => false;
        public override RefOrPointer Clone()
        {
            if (this == RefOrPointer._pinnedPointer.As<Ref<T>>())
                return this;
            return new Ref<T>(_getter, _setter)
            {
                _dataSource = _dataSource,
                _sizeOfItem = _sizeOfItem,
                _type = _type,
                _byteOffset = _byteOffset,
                _dataView = _dataView,
                _virtualAddress = _virtualAddress
            };
        }

        [Name(NetJs.Constants.IsTypeName)]
        public static bool Is(object? value, NativeAction<Ref<T>> result)
        {
            //result = NetJs.Script.Write<Ref<T>>("undefined");
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
                ref T t = ref Unsafe.NullRef<T>();
                result(NetJs.Script.Write<Ref<T>>("t"));
                return value.As<int>() == 0 || value.As<int>() == 1;
            }

            if (value is RefOrPointer rref)
            {
                var newReff = rref.As<RefOrPointer<object>>().Cast<T>();
                if (newReff != null)
                {
                    result(newReff.As<Ref<T>>());
                }
                return true;
            }
            return false;
        }
    }

    public class Pointer<T> : RefOrPointer<T>
    {
        protected Pointer(Pointer<T> original) : base(original)
        {
        }

        internal Pointer(RefOrPointer parent) : base(parent)
        {
        }

        internal Pointer(NativeFunction<int?, T> getter, NativeAction<T, int?> setter) : base(getter, setter)
        {
        }
        internal override bool IsPointer => true;
        public override RefOrPointer Clone()
        {
            if (this == RefOrPointer._pinnedPointer.As<Pointer<T>>())
                return this;
            return new Pointer<T>(_getter, _setter)
            {
                _dataSource = _dataSource,
                _sizeOfItem = _sizeOfItem,
                _type = _type,
                _byteOffset = _byteOffset,
                _dataView = _dataView,
                _virtualAddress = _virtualAddress
            };
        }

        [Name(NetJs.Constants.IsTypeName)]
        public static bool Is(object? value, NativeAction<RefOrPointer<T>> result)
        {
            //result = NetJs.Script.Write<Pointer<T>>("undefined");
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
                if (value.As<int>() == 0 || value.As<int>() == 1)
                {
                    if (value.As<int>() == 0)
                        result = null;
                    else
                    {
                        result(RefOrPointer._pinnedPointer.As<RefOrPointer<T>>());
                    }
                    return true;
                }
                if (typeof(T).As<RuntimeType>()._prototype.KnownType == KnownTypeHandle.SystemVoid)
                    return true;
            }
            if (value is RefOrPointer rref)
            {
                var newReff = rref.As<RefOrPointer<object>>().Cast<T>();
                if (newReff != null)
                {
                    result(newReff.As<Pointer<T>>());
                }
                return true;
            }
            if (Array<T>.Is(value))
            {
                result(RuntimeHelpers.CreateArrayReference<T>(value.As<Array>(), createRef: false));
                return true;
            }
            return false;
        }
    }
}
