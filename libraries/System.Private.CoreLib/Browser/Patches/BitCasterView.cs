namespace NetJs
{
    using System;
    using System.Runtime.InteropServices;

    [NetJs.ObjectLiteral]
    public class BitCasterViewEventArg : EventArgs
    {
        public int ByteOffset { get; set; }
        public Type Type { get; set; }
        public object Value { get; set; }
    }
    public class BitCasterView : IArrayProxyHandler
    {
        internal Array _array;
        internal Type _elementType;
        internal int _byteOffset;
        internal int _byteCount;
        internal event EventHandler<BitCasterViewEventArg>? OnWrite;
        internal Type? ProxyType { get; set; }
        //internal event EventHandler? OnRead;
        public BitCasterView(Array dataSource, int byteOffset = 0, int byteCount = -1, Type? elementType = null)
        {
            this._array = dataSource;
            _elementType = elementType ?? Array.GetArrayElementType(dataSource) ?? typeof(byte);
            var size = _elementType.As<RuntimeType>()._prototype.Size;
            if (byteCount < 0)
                byteCount = _array.Length * size - byteOffset;
            _byteCount = byteCount;
            _byteOffset = byteOffset;
        }

        public BitCasterView Offset(int byteOffset, int byteCount = -1)
        {
            if (byteOffset == 0)
                return this;
            return new BitCasterView(_array, _byteOffset + byteOffset, byteCount, _elementType) { ProxyType = ProxyType };
        }

        public void CopyFrom(BitCasterView other)
        {
            if (other._elementType == _elementType) //fast copy. same element type
            {
                unchecked
                {
                    var size = _elementType.As<RuntimeType>()._prototype.Size;
                    var offset = _byteOffset / size;
                    var otherOffset = other._byteOffset / size;
                    var minByteCount = Math.Min(_byteCount, other._byteCount);
                    for (int i = 0; i < minByteCount / size; i++)
                    {
                        _array[i + offset] = other._array[i + otherOffset];
                    }
                }
            }
            else
            {
                for (int i = 0; i < other._byteCount; i++)
                {
                    write<byte>(other.read<byte>(i), i);
                }
            }
        }

        internal T read<T>(int byteOffset)
        {
            return BitCaster.ReInterpretRead(_elementType, typeof(T), _array, _byteOffset + byteOffset).As<T>();
        }

        internal void write<T>(T value, int byteOffset)
        {
            BitCaster.ReInterpretWrite(_elementType, typeof(T), _array, value.As<object>(), _byteOffset + byteOffset);
            OnWrite?.Invoke(this, new BitCasterViewEventArg { Type = typeof(T), ByteOffset = byteOffset, Value = value! });
        }

        internal object read(Type T, int byteOffset)
        {
            return BitCaster.ReInterpretRead(_elementType, T, _array, _byteOffset + byteOffset);
        }

        internal void write(Type T, object value, int byteOffset)
        {
            BitCaster.ReInterpretWrite(_elementType, T, _array, value, _byteOffset + byteOffset);
        }

        public sbyte getInt8(int byteOffset)
        {
            return read<sbyte>(byteOffset);
        }

        public short getInt16(int byteOffset, bool littleEndian = false)
        {
            return read<short>(byteOffset);
        }
        public int getInt32(int byteOffset, bool littleEndian = false)
        {
            return read<int>(byteOffset);
        }
        public byte getUint8(int byteOffset)
        {
            return read<byte>(byteOffset);
        }
        public ushort getUint16(int byteOffset, bool littleEndian = false)
        {
            return read<ushort>(byteOffset);
        }
        public uint getUint32(int byteOffset, bool littleEndian = false)
        {
            return read<uint>(byteOffset);
        }
        public float getFloat32(int byteOffset, bool littleEndian = false)
        {
            return read<float>(byteOffset);
        }
        public double getFloat64(int byteOffset, bool littleEndian = false)
        {
            return read<double>(byteOffset);
        }
        public long getBigInt64(int byteOffset, bool littleEndian = false)
        {
            return read<long>(byteOffset);
        }
        public ulong getBigUint64(int byteOffset, bool littleEndian = false)
        {
            return read<ulong>(byteOffset);
        }

        public void setInt8(int byteOffset, sbyte value)
        {
            write(value, byteOffset);
        }
        public void setInt16(int byteOffset, short value, bool littleEndian = false)
        {
            write(value, byteOffset);
        }
        public void setInt32(int byteOffset, int value, bool littleEndian = false)
        {
            write(value, byteOffset);
        }
        public void setUint8(int byteOffset, byte value)
        {
            write(value, byteOffset);
        }
        public void setUint16(int byteOffset, ushort value, bool littleEndian = false)
        {
            write(value, byteOffset);
        }
        public void setUint32(int byteOffset, uint value, bool littleEndian = false)
        {
            write(value, byteOffset);
        }
        public void setFloat32(int byteOffset, float value, bool littleEndian = false)
        {
            write(value, byteOffset);
        }
        public void setFloat64(int byteOffset, double value, bool littleEndian = false)
        {
            write(value, byteOffset);
        }
        public void setBigInt64(int byteOffset, long value, bool littleEndian = false)
        {
            write(value, byteOffset);
        }
        public void setBigUint64(int byteOffset, ulong value, bool littleEndian = false)
        {
            write(value, byteOffset);
        }

        public Array Array => _array;
        public Type ElementType => ProxyType ?? _elementType;

        public object? Get(object target, string property, object receiver)
        {
            var propertyName = NetJs.Script.TypeOf(property);
            if (propertyName.NativeEquals("string"))
            {
                if (property.NativeEquals(Constants.IsProxy))
                {
                    return true.As<object>();
                }
                if (property.NativeEquals(Constants.ProxyHandler))
                {
                    return this;
                }
                if (property.NativeEquals(Constants.ProxyType))
                {
                    return _array.GetType();
                }
                if (propertyName.NativeEquals(Array.ElementTypeName))
                {
                    return this[Array.ElementTypeName].As<object>();
                }
                if (propertyName.NativeEquals(Array.SizesName))
                {
                    return this[Array.SizesName].As<object>();
                }
                if (propertyName.NativeEquals(Array.LowerBoundsName))
                {
                    return this[Array.LowerBoundsName].As<object>();
                }
                if (property.NativeEquals("length"))
                {
                    return _byteCount.As<object>();
                }
                if (property.NativeStartsWith("$"))
                {
                    return _array[property];
                }
            }
            var index = NetJs.Script.ParseInt(property);
            if (!NetJs.Script.IsNaN(index))
            {
                return read(ElementType, index * Marshal.SizeOf(ElementType));
            }
            return Window.Reflect.get(_array, property, _array);
            //return NetJs.Script.Write<object>("Reflect.get(this.{nameof(ArrayWindowProxyHandler._array)}, property, this.{nameof(ArrayWindowProxyHandler._array)})");
        }
        public bool Set(object target, string property, object value, object receiver)
        {
            var propertyType = NetJs.Script.TypeOf(property);
            if (propertyType.NativeEquals("string"))
            {
                unchecked
                {
                    var index = NetJs.Script.ParseInt(property);
                    if (!NetJs.Script.IsNaN(index))
                    {
                        write(ElementType, value, index * Marshal.SizeOf(ElementType));
                        return true;
                    }
                }
                //if (property.NativeEquals(Array.ElementTypeName))
                //{
                //    this[Array.ElementTypeName] = value;
                //    _elementType = value.As<Type>();
                //}
                //else if (property.NativeEquals(Array.SizesName))
                //{
                //    this[Array.SizesName] = value;
                //}
                //else if (property.NativeEquals(Array.LowerBoundsName))
                //{
                //    this[Array.LowerBoundsName] = value;
                //}
            }
            //else
            //{
            //    Window.Reflect.set(_array, property, value, _array);
            //    //NetJs.Script.Write<object>("Reflect.set(this.{nameof(ArrayWindowProxyHandler._array)}, property, value, this.{nameof(ArrayWindowProxyHandler._array)})");
            //}
            return true;
        }

    }
}
