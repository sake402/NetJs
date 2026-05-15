using NetJs;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System
{

    [NetJs.Boot]
    [NetJs.OutputOrder(int.MinValue + 1)]
    public partial class Object
    {
        [NetJs.Template("{T}._model")]
        public static extern TypeModel? GetTypeModel<T>() where T : allows ref struct;
        public extern object? this[string name]
        {
            [NetJs.External]
            get;
            [NetJs.External]
            [param: NetJs.Box(false)]
            set;
        }

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        public virtual extern string ToLocaleString();

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        public virtual extern object? ValueOf();

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        public virtual extern bool HasOwnProperty(object v);

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        public virtual extern bool IsPrototypeOf(object v);

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        public virtual extern bool PropertyIsEnumerable(object v);
        [NetJs.Template("{this}.constructor")]
        public  extern TypePrototype GetPrototype();

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("{obj}.hasOwnProperty({name})")]
        [NetJs.Unbox(true)]
        public static extern bool HasOwnProperty(object obj, string name);
        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("Object.getOwnPropertyNames({obj})")]
        [NetJs.Unbox(true)]
        public static extern string[] GetOwnPropertyNames(object obj);

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("{T}.prototype")]
        public static extern TypePrototype GetPrototype<T>();
        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("Object.getPrototypeOf({value})")]
        public static extern TypePrototype GetPrototypeOf(object value);

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("Object.defineProperty({value}, {name}, {descriptor})")]
        public static extern TypePrototype DefineProperty(object value, string name, PropertyDescriptor descriptor);


        [NetJs.Template("{global.}$clone({this:!super})")]
        [NetJs.MemberReplace(nameof(MemberwiseClone))]
        protected extern object IntrisicMemberwiseClone();

        /////String is a reference type. But we box it anyway like every other js primitive type,
        ////because we want to be able to access the methods exposed by String class on it, especially the interface methods.
        ////Calling ReferenceEquals on interned boxed string(unlike other primitive) should still work as expected, so we need to unbox 
        //[NetJs.MemberReplace(nameof(ReferenceEquals))]
        //public static bool ReferenceEqualsImpl(object? objA, object? objB)
        //{
        //    if (objA is string s1 && objB is string s2)
        //        return s1.NativeEquals(s2);
        //    return objA == objB;
        //}

        [NetJs.MemberReplace(nameof(GetType))]
        [NetJs.StaticCallConvention]
        public Type GetTypeImpl()
        {
            var value = this;
            if (value == null)
                throw new NullReferenceException();
            if (Array<object>.Is(value))
            {
                return Array.GetArrayType(value.As<Array>());
            }
            var prototype = NetJs.Script.Write<TypePrototype>("window.Object.getPrototypeOf(value)");// Object.GetPrototypeOf(value);
            var pType = prototype.Type ?? NetJs.Script.Write<Type>("value.constructor.$type");
            if (NetJs.Script.IsDefined(pType))
            {
                return pType;
            }
            prototype = NetJs.Script.Write<TypePrototype>("value.constructor");
            if (NetJs.Script.IsDefined(prototype) && NetJs.Script.IsDefined(prototype.Type))
                return prototype!.Type!;
            var jsType = NetJs.Script.TypeOf(value);
            switch (jsType)
            {
                case "number":
                    return typeof(double);
                case "string":
                    return typeof(string);
                case "boolean":
                    return typeof(bool);
            }
            return typeof(object);
        }
        [NetJs.MemberReplace(nameof(ToString))]
        //[NetJs.StaticCallConvention]
        //[NetJs.Template("{global.}" + NetJs.Constants.ToStringName + "({this:!super}, \"\")")] //make sure we dont pass super keyword in here. JS doesnt support it
        public virtual string ToStringImpl()
        {
            return GetType().ToString();
        }

        [NetJs.MemberReplace(nameof(GetHashCode))]
        //[NetJs.Template("{global.}" + NetJs.Constants.GetHashCodeName + "({this:!super})")] //make sure we dont pass super keyword in here. JS doesnt support it
        //[NetJs.StaticCallConvention]
        public virtual int GetHashCodeImpl()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        [NetJs.Reflectable(false)]
        const bool FieldLayoutByByte = false;

        #region ObjectFieldAccess

        [NetJs.Name(NetJs.Constants.StructFieldsLayoutName)]
        [NetJs.Reflectable(false)]
        internal object[] _fields = NetJs.Script.NewArray<object>();
        //public extern object? this[int offset]
        //{
        //    [dotnetJs.External]
        //    get;
        //    [dotnetJs.External]
        //    set;
        //}
        byte GetByte(int offset)
        {
            unchecked
            {
                return _fields[offset].As<byte>();
            }
        }

        void SetByte(int offset, byte value)
        {
            unchecked
            {
                _fields[offset] = value.As<object>();
            }
        }

        sbyte GetSByte(int offset)
        {
            unchecked
            {
                return _fields[offset].As<sbyte>();
            }
        }
        void SetSByte(int offset, sbyte value)
        {
            unchecked
            {
                _fields[offset] = value.As<object>();
            }
        }

        ushort GetUShort(int offset)
        {
            unchecked
            {
                if (FieldLayoutByByte)
                    return (_fields[offset].As<int>() | (_fields[offset + 1].As<int>() << 8)).As<ushort>();
                else
                    return _fields[offset].As<ushort>();
            }
        }

        void SetUShort(int offset, ushort value)
        {
            unchecked
            {
                if (FieldLayoutByByte)
                {
                    _fields[offset] = (value & 0xFF).As<object>();
                    _fields[offset + 1] = ((value >> 8) & 0xFF).As<object>();
                }
                else
                {
                    _fields[offset] = value.As<object>();
                }
            }
        }


        short GetShort(int offset)
        {
            unchecked
            {
                if (FieldLayoutByByte)
                    return (_fields[offset].As<int>() | (_fields[offset + 1].As<int>() << 8)).As<short>();
                else
                    return _fields[offset].As<short>();
            }
        }
        void SetShort(int offset, short value)
        {
            unchecked
            {
                if (FieldLayoutByByte)
                {
                    _fields[offset] = (value & 0xFF).As<object>();
                    _fields[offset + 1] = ((value >> 8) & 0xFF).As<object>();
                }
                else
                {
                    _fields[offset] = value.As<object>();
                }
            }
        }


        uint GetUInt(int offset)
        {
            unchecked
            {
                if (FieldLayoutByByte)
                {
                    return (
                    _fields[offset].As<uint>() |
                    (_fields[offset + 1].As<uint>() << 8) |
                    (_fields[offset + 2].As<uint>() << 16) |
                    (_fields[offset + 3].As<uint>() << 24)
                    ).As<uint>();
                }
                else
                {
                    return _fields[offset].As<uint>();
                }
            }
        }

        void SetUInt(int offset, uint value)
        {
            unchecked
            {
                if (FieldLayoutByByte)
                {
                    _fields[offset] = (value & 0xFF).As<object>();
                    _fields[offset + 1] = ((value >> 8) & 0xFF).As<object>();
                    _fields[offset + 1] = ((value >> 16) & 0xFF).As<object>();
                    _fields[offset + 1] = ((value >> 24) & 0xFF).As<object>();
                }
                else
                {
                    _fields[offset] = value.As<object>();
                }
            }
        }

        int GetInt(int offset)
        {
            unchecked
            {
                if (FieldLayoutByByte)
                {
                    return (
                    _fields[offset].As<int>() |
                    (_fields[offset + 1].As<int>() << 8) |
                    (_fields[offset + 2].As<int>() << 16) |
                    (_fields[offset + 3].As<int>() << 24)
                    ).As<int>();
                }
                else
                {
                    return _fields[offset].As<int>();
                }
            }
        }

        void SetInt(int offset, int value)
        {
            unchecked
            {
                if (FieldLayoutByByte)
                {
                    _fields[offset] = (value & 0xFF).As<object>();
                    _fields[offset + 1] = ((value >> 8) & 0xFF).As<object>();
                    _fields[offset + 2] = ((value >> 16) & 0xFF).As<object>();
                    _fields[offset + 3] = ((value >> 24) & 0xFF).As<object>();
                }
                else
                {
                    _fields[offset] = value.As<object>();
                }
            }
        }

        ulong GetULong(int offset)
        {
            unchecked
            {
                if (FieldLayoutByByte)
                {
                    return (
                    _fields[offset].As<ulong>() |
                    (_fields[offset + 1].As<ulong>() << 8) |
                    (_fields[offset + 2].As<ulong>() << 16) |
                    (_fields[offset + 3].As<ulong>() << 24) |
                    (_fields[offset + 4].As<ulong>() << 32) |
                    (_fields[offset + 5].As<ulong>() << 40) |
                    (_fields[offset + 6].As<ulong>() << 48) |
                    (_fields[offset + 7].As<ulong>() << 56)
                    ).As<ulong>();
                }
                else
                {
                    return _fields[offset].As<ulong>();
                }
            }
        }

        void SetULong(int offset, ulong value)
        {
            unchecked
            {
                if (FieldLayoutByByte)
                {
                    _fields[offset] = (value & 0xFF).As<object>();
                    _fields[offset + 1] = ((value >> 8) & 0xFF).As<object>();
                    _fields[offset + 2] = ((value >> 16) & 0xFF).As<object>();
                    _fields[offset + 3] = ((value >> 24) & 0xFF).As<object>();
                    _fields[offset + 4] = ((value >> 32) & 0xFF).As<object>();
                    _fields[offset + 5] = ((value >> 40) & 0xFF).As<object>();
                    _fields[offset + 6] = ((value >> 48) & 0xFF).As<object>();
                    _fields[offset + 7] = ((value >> 56) & 0xFF).As<object>();
                }
                else
                {
                    _fields[offset] = value.As<object>();
                }
            }
        }

        long GetLong(int offset)
        {
            unchecked
            {
                if (FieldLayoutByByte)
                {
                    return (
                    _fields[offset].As<long>() |
                    (_fields[offset + 1].As<long>() << 8) |
                    (_fields[offset + 2].As<long>() << 16) |
                    (_fields[offset + 3].As<long>() << 24) |
                    (_fields[offset + 4].As<long>() << 32) |
                    (_fields[offset + 5].As<long>() << 40) |
                    (_fields[offset + 6].As<long>() << 48) |
                    (_fields[offset + 7].As<long>() << 56)
                    ).As<long>();
                }
                else
                {
                    return _fields[offset].As<long>();
                }
            }
        }

        void SetLong(int offset, ulong value)
        {
            unchecked
            {
                if (FieldLayoutByByte)
                {
                    _fields[offset] = (value & 0xFF).As<object>();
                    _fields[offset + 1] = ((value >> 8) & 0xFF).As<object>();
                    _fields[offset + 2] = ((value >> 16) & 0xFF).As<object>();
                    _fields[offset + 3] = ((value >> 24) & 0xFF).As<object>();
                    _fields[offset + 4] = ((value >> 32) & 0xFF).As<object>();
                    _fields[offset + 5] = ((value >> 40) & 0xFF).As<object>();
                    _fields[offset + 6] = ((value >> 48) & 0xFF).As<object>();
                    _fields[offset + 7] = ((value >> 56) & 0xFF).As<object>();
                }
                else
                {
                    _fields[offset] = value.As<object>();
                }
            }
        }

        [NetJs.Name("$offsetObjects")]
        internal SimpleDictionary<object>? offsetObjects;
        object GetField(int offset, Union<int, TypePrototype> size)
        {
            unchecked
            {
                bool isNumber = NetJs.Script.TypeOf(size).NativeEquals("number");
                if (!isNumber) //Object of inline array type laid within this object
                {
                    offsetObjects ??= new();
                    var mobject = offsetObjects[offset];
                    if (NetJs.Script.IsUndefinedOrNull(mobject))
                    {
                        var realSize = size.As<TypePrototype>().Model!.Size ?? 0;
                        mobject = size.As<TypePrototype>().New();
                        var fields = JSProxy.Create<object[]>(new ArrayWindowProxyHandler(_fields, offset, realSize));
                        mobject._fields = fields;
                        offsetObjects[offset] = mobject;
                    }
                    return mobject;
                }
                else if (size.As<int>() > 1) //Array type laid inside this object
                {
                    offsetObjects ??= new();
                    var arrayProxy = offsetObjects[offset];
                    if (NetJs.Script.IsUndefinedOrNull(arrayProxy))
                    {
                        arrayProxy = JSProxy.Create<object[]>(new ArrayWindowProxyHandler(_fields, offset, size.As<int>()));
                        offsetObjects[offset] = arrayProxy;
                    }
                    return arrayProxy;
                }
                else
                {
                    return _fields[offset];
                }
            }
        }
        void SetField(int offset, Union<int, TypePrototype> size, object value)
        {
            unchecked
            {
                bool isNumber = NetJs.Script.TypeOf(size).NativeEquals("number");
                if (!isNumber)
                {
                    var realSize = size.As<TypePrototype>().Model!.Size ?? 0;
                    if (_fields.Length < offset)
                        NetJs.Script.Write("this.$fields.length = offset");
                    var sourceFields = value._fields;
                    if (sourceFields.As<object>()["$isProxy"].As<bool>() == true)
                    {
                        throw null;
                    }
                    //if (sourceFields.Length != realSize)
                    //{
                    //    throw null;
                    //}
                    _fields.Splice(offset, 0, sourceFields);
                }
                else if (size.As<int>() > 1)
                {
                    if (_fields.Length < offset)
                        NetJs.Script.Write("this.$fields.length = offset");
                    _fields.Splice(offset, size.As<int>(), value.As<object[]>());
                }
                else
                {
                    _fields[offset] = value;
                }
            }
        }


        [NetJs.Name(NetJs.Constants.StaticStructFieldsLayoutName)]
        [NetJs.Reflectable(false)]
        static object[] _sfields = NetJs.Script.NewArray<object>();
        static object GetSField(int offset)
        {
            unchecked
            {
                return _sfields[offset].As<object>();
            }
        }
        static void SetSField(int offset, object value)
        {
            unchecked
            {
                _sfields[offset] = value.As<object>();
            }
        }

        #endregion

    }
}
