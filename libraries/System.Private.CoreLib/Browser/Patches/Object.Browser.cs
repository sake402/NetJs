using NetJs;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Window;

namespace System
{

    [NetJs.Boot]
    [NetJs.OutputOrder(int.MinValue + 1)]
    public partial class Object
    {
        [NetJs.Template("{T}._model")]
        public static extern TypeModel? getTypeModel<T>() where T : allows ref struct;
        //public extern object? this[string name]
        //{
        //    [NetJs.External]
        //    get;
        //    [NetJs.External]
        //    [param: NetJs.Box(false)]
        //    set;
        //}
        public extern object? this[NetJs.Union<Window.Symbol, string> name]
        {
            [NetJs.External]
            get;
            [NetJs.External]
            [param: NetJs.Box(false)]
            set;
        }

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        public virtual extern string toLocaleString();

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        public virtual extern object? valueOf();

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        public virtual extern bool hasOwnProperty(object v);

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        public virtual extern bool IsPrototypeOf(object v);

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        public virtual extern bool PropertyIsEnumerable(object v);
        [NetJs.Template("{this}.constructor")]
        public extern TypePrototype GetClassPrototype();
        [NetJs.Template("{value}.constructor")]
        public static extern TypePrototype GetClassPrototypeOf(object value);
        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("{name} in {obj}")]
        public static extern bool PropertyIn(object obj, string name);

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("{obj}.hasOwnProperty({name})")]
        public static extern bool HasOwnProperty(object obj, string name);
        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("Object.hasOwn({obj}, {name})")]
        public static extern bool HasOwn(object obj, string name);
        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("Object.getOwnPropertyNames({obj})")]
        [NetJs.Unbox(true)]
        public static extern string[] GetOwnPropertyNames(object obj);

        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("{T}")]
        public static extern TypePrototype GetPrototype<T>();
        [NetJs.Template("Object.getPrototypeOf({value})")]
        public static extern TypePrototype GetPrototypeOf(object value);
        [NetJs.Template("Object.setPrototypeOf({prototype}, {value})")]
        public static extern TypePrototype SetPrototypeOf(TypePrototype prototype, object value);
        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("Object.getOwnPropertyDescriptor({value}, {key})")]
        public static extern PropertyDescriptor GetOwnPropertyDescriptor(object value, string key);
        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("Object.getOwnPropertyDescriptors({value})")]
        public static extern SimpleDictionary<PropertyDescriptor> getOwnPropertyDescriptors(object value);
        [NetJs.Template("Object.keys({value})")]
        public static extern string[] keys(object value);
        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("Object.defineProperty({value}, {name}, {descriptor})")]
        public static extern TypePrototype DefineProperty(object value, string name, PropertyDescriptor descriptor);
        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("Object.defineProperties({value}, {descriptors})")]
        public static extern TypePrototype DefineProperties(object value, PropertyDescriptor[] descriptors);
        [NetJs.Convention(NetJs.Notation.CamelCase)]
        [NetJs.Template("Object.create({prototype})")]
        public static extern object Create(TypePrototype prototype);


        [NetJs.Template("{global.}$clone({this:!super})")]
        [NetJs.MemberReplace(nameof(MemberwiseClone))]
        protected extern object IntrisicMemberwiseClone();

        public static extern TypePrototype? Prototype
        {
            [NetJs.Template("Object.prototype")]
            get;
        }

        public static extern TypePrototype? Self
        {
            [NetJs.Template("Object")]
            get;
        }

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
            var isProxy = this[NetJs.Constants.IsProxy].As<bool>();
            if (isProxy == true)
            {
                return this[NetJs.Constants.ProxyType].As<Type>();
            }
            if (Array<object>.Is(value) || NetJs.Script.IsArray(value))
            {
                return Array.GetArrayType(value.As<Array>());
            }
            var isAnonymousObject = NetJs.Script.Write<bool>("value.constructor == window.Object");
            if (isAnonymousObject)
            {
                var asm = value[NetJs.Constants.AssemblyRegistryName].As<System.Reflection.RuntimeAssembly>() ?? typeof(object).Assembly.As<System.Reflection.RuntimeAssembly>();
                var referenceName = value[NetJs.Constants.AssemblyDefineClassName].As<string>();
                if (NetJs.Script.IsDefined(referenceName))
                {
                    var ireferenceName = nameof(AnonymousType<>) + "<" + referenceName + ">";
                    var existing = asm.As<System.Reflection.RuntimeAssembly_Partial>().GetTypeInternal(ireferenceName);
                    if (existing != null)
                        return existing;
                }
                referenceName ??= Guid.NewGuid().ToString();
                value[NetJs.Constants.AssemblyDefineClassName] = referenceName;
                value[NetJs.Constants.AssemblyRegistryName] = asm;
                var propertyNames = NetJs.Script.Write<string[]>("window.Object.getOwnPropertyNames(value)");
                TypePrototype objectPrototype = typeof(object).As<RuntimeType>()._prototype;
                var fakeArgument = new { };
                fakeArgument[Constants.PrototypeFullName] = referenceName;
                fakeArgument[Constants.PrototypeReferenceFullName] = referenceName;
                fakeArgument[Constants.PrototypeTypeName] = typeof(object);
                string getFullNameWithSeparator(string separator)
                {
                    return referenceName;
                }
                NativeFunction<string, string> getFullName = getFullNameWithSeparator;
                fakeArgument[Constants.PrototypeFullNameWithSpecialSeperator] = getFullName;
                var prototypeProvider = typeof(AnonymousType<>).As<RuntimeType>()._prototypeProvider;
                System.Reflection.RuntimeAssembly_Partial.isInTypeCreate++; //Make sure we dont complete the type creation of this anonymous type until we have set the metadata properties on the prototype
                var typePrototype = prototypeProvider!(fakeArgument.As<TypePrototype>())!;
                System.Reflection.RuntimeAssembly_Partial.isInTypeCreate--;
                //metadata.Handle = typePrototype.TypeHandle;
                //Set the metadata backing field of this prototype
                _ = typePrototype.Metadata;// Read the property to ensure we create the backing field, before updating it
                typePrototype["$" + NetJs.Constants.PrototypeMetadata].As<TypeModel>().Properties = propertyNames.filter(v => v[0] != '$').map((p, i, all) => new PropertyModel
                {
                    Name = p,
                    Handle = NetJs.Script.Number(NetJs.Script.BigInt(typePrototype.TypeHandle.As<int>()) | NetJs.Script.BigInt(1) << ReflectionHandleExtension.MemberShift).As<ulong>(),
                    PropertyType = typeof(object).As<RuntimeType>()._prototype.TypeHandle,
                    Flags = MemberFlagsModel.IsPublic | MemberFlagsModel.IsProperty,
                    DeclaringType = typePrototype.TypeHandle,
                    GetMethod = new MethodModel
                    {
                        Name = p,
                        DeclaringType = typePrototype.TypeHandle,
                        Flags = MemberFlagsModel.IsPublic | MemberFlagsModel.IsMethod,
                        Handle = NetJs.Script.Number(NetJs.Script.BigInt(typePrototype.TypeHandle.As<int>()) | NetJs.Script.BigInt(2) << ReflectionHandleExtension.MemberShift).As<ulong>(),
                    },
                    SetMethod = new MethodModel
                    {
                        Name = p,
                        DeclaringType = typePrototype.TypeHandle,
                        Flags = MemberFlagsModel.IsPublic | MemberFlagsModel.IsMethod,
                        Handle = NetJs.Script.Number(NetJs.Script.BigInt(typePrototype.TypeHandle.As<int>()) | NetJs.Script.BigInt(3) << ReflectionHandleExtension.MemberShift).As<ulong>(),
                    },
                });
                typePrototype.Type.As<RuntimeType>().Complete();
                return typePrototype.Type!;
            }
            var prototype = NetJs.Script.Write<TypePrototype>("window.Object.getPrototypeOf(value)");// Object.GetPrototypeOf(value);
            var pType = NetJs.Script.Write<Type>("value.constructor?.$type") ?? prototype.Type;
            if (NetJs.Script.IsDefined(pType))
            {
                return pType!;
            }
            //prototype = NetJs.Script.Write<TypePrototype>("value.constructor");
            //if (NetJs.Script.IsDefined(prototype) && NetJs.Script.IsDefined(prototype.Type))
            //return prototype!.Type!;
            var jsType = NetJs.Script.TypeOf(value);
            switch (jsType)
            {
                case "number":
                    return typeof(double);
                case "string":
                    return typeof(string);
                case "boolean":
                    return typeof(bool);
                case "bigint":
                    return typeof(long);
            }
            return typeof(object);
        }
        [NetJs.MemberReplace(nameof(ToString))]
        [NetJs.StaticCallConvention]
        //[NetJs.Template("{global.}" + NetJs.Constants.ToStringName + "({this:!super}, \"\")")] //make sure we dont pass super keyword in here. JS doesnt support it
        public virtual string ToStringImpl()
        {
            var value = NetJs.Script.Unbox(this);
            var type = NetJs.Script.TypeOf(value);
            if (type.NativeEquals("string"))
                return value.As<string>();
            if (type.NativeEquals("boolean"))
                return value.As<bool>() ? "True" : "False";
            if (type.NativeEquals("number") || type.NativeEquals("bigint"))
                return value.As<string>() + "";
            //var callerName = NetJs.Script.Write<string>("{global.}getCallerName()");
            //if (callerName.NativeNotEquals("ToString"))//not called by subclass? call the subsclass ToString if there is one
            //{
            //    var method = this["ToString"];
            //    if (NetJs.Script.IsDefined(method))
            //    {
            //        var str = NetJs.Script.Write<string>("method.call(this)");
            //        return str;
            //    }
            //}
            return GetType().ToString();
        }

        [NetJs.MemberReplace(nameof(GetHashCode))]
        [NetJs.StaticCallConvention]
        public virtual int GetHashCodeImpl()
        {
            //var callerName = NetJs.Script.Write<string>("{global.}getCallerName()");
            //var getHashCodeName = NetJs.Script.Write<string>("\"{nameof(System.Object.GetHashCode())}\"");
            //if (callerName.NativeNotEquals(getHashCodeName)) //not called by subclass? call the subsclass GetHashCode if there is one
            //{
            //    var method = this[getHashCodeName];
            //    if (NetJs.Script.IsDefined(method))
            //    {
            //        var unboxedThis = NetJs.Script.Unbox(this);
            //        var hashCode = NetJs.Script.Write<int>("method.call(unboxedThis)");
            //        return hashCode;
            //    }
            //}
            return RuntimeHelpers.GetHashCode(this);
        }

        [NetJs.MemberReplace(nameof(Equals) + "(object?)")]
        [NetJs.StaticCallConvention]
        public virtual bool EqualsImpl(object? obj)
        {
            //var callerName = NetJs.Script.Write<string>("{global.}getCallerName()");
            //var equalsName = "Equals";// NetJs.Script.Write<string>("\"{nameof(object.Equals(object))}\"");
            //if (callerName.NativeNotEquals(equalsName)) //not called by subclass? call the subsclass Equals if there is one
            //{
            //    var method = this[equalsName];
            //    if (NetJs.Script.IsDefined(method))
            //    {
            //        var unboxedThis = NetJs.Script.Unbox(this);
            //        var equals = NetJs.Script.Write<bool>("method.call(unboxedThis, obj)");
            //        return equals;
            //    }
            //}
            return this == obj;
        }

        //[NetJs.Reflectable(false)]
        //const bool FieldLayoutByByte = false;

        #region ObjectFieldAccess

        [NetJs.Name(NetJs.Constants.StructFieldsLayoutName)]
        [NetJs.Reflectable(false)]
        internal Union<object[], BitCasterView> _fields;
        //public extern object? this[int offset]
        //{
        //    [dotnetJs.External]
        //    get;
        //    [dotnetJs.External]
        //    set;
        //}

        //byte GetByte(int offset)
        //{
        //    unchecked
        //    {
        //        return _fields[offset].As<byte>();
        //    }
        //}

        //void SetByte(int offset, byte value)
        //{
        //    unchecked
        //    {
        //        _fields[offset] = value.As<object>();
        //    }
        //}

        //sbyte GetSByte(int offset)
        //{
        //    unchecked
        //    {
        //        return _fields[offset].As<sbyte>();
        //    }
        //}
        //void SetSByte(int offset, sbyte value)
        //{
        //    unchecked
        //    {
        //        _fields[offset] = value.As<object>();
        //    }
        //}

        //ushort GetUShort(int offset)
        //{
        //    unchecked
        //    {
        //        if (FieldLayoutByByte)
        //            return (_fields[offset].As<int>() | (_fields[offset + 1].As<int>() << 8)).As<ushort>();
        //        else
        //            return _fields[offset].As<ushort>();
        //    }
        //}

        //void SetUShort(int offset, ushort value)
        //{
        //    unchecked
        //    {
        //        if (FieldLayoutByByte)
        //        {
        //            _fields[offset] = (value & 0xFF).As<object>();
        //            _fields[offset + 1] = ((value >> 8) & 0xFF).As<object>();
        //        }
        //        else
        //        {
        //            _fields[offset] = value.As<object>();
        //        }
        //    }
        //}


        //short GetShort(int offset)
        //{
        //    unchecked
        //    {
        //        if (FieldLayoutByByte)
        //            return (_fields[offset].As<int>() | (_fields[offset + 1].As<int>() << 8)).As<short>();
        //        else
        //            return _fields[offset].As<short>();
        //    }
        //}
        //void SetShort(int offset, short value)
        //{
        //    unchecked
        //    {
        //        if (FieldLayoutByByte)
        //        {
        //            _fields[offset] = (value & 0xFF).As<object>();
        //            _fields[offset + 1] = ((value >> 8) & 0xFF).As<object>();
        //        }
        //        else
        //        {
        //            _fields[offset] = value.As<object>();
        //        }
        //    }
        //}


        //uint GetUInt(int offset)
        //{
        //    unchecked
        //    {
        //        if (FieldLayoutByByte)
        //        {
        //            return (
        //            _fields[offset].As<uint>() |
        //            (_fields[offset + 1].As<uint>() << 8) |
        //            (_fields[offset + 2].As<uint>() << 16) |
        //            (_fields[offset + 3].As<uint>() << 24)
        //            ).As<uint>();
        //        }
        //        else
        //        {
        //            return _fields[offset].As<uint>();
        //        }
        //    }
        //}

        //void SetUInt(int offset, uint value)
        //{
        //    unchecked
        //    {
        //        if (FieldLayoutByByte)
        //        {
        //            _fields[offset] = (value & 0xFF).As<object>();
        //            _fields[offset + 1] = ((value >> 8) & 0xFF).As<object>();
        //            _fields[offset + 1] = ((value >> 16) & 0xFF).As<object>();
        //            _fields[offset + 1] = ((value >> 24) & 0xFF).As<object>();
        //        }
        //        else
        //        {
        //            _fields[offset] = value.As<object>();
        //        }
        //    }
        //}

        //int GetInt(int offset)
        //{
        //    unchecked
        //    {
        //        if (FieldLayoutByByte)
        //        {
        //            return (
        //            _fields[offset].As<int>() |
        //            (_fields[offset + 1].As<int>() << 8) |
        //            (_fields[offset + 2].As<int>() << 16) |
        //            (_fields[offset + 3].As<int>() << 24)
        //            ).As<int>();
        //        }
        //        else
        //        {
        //            return _fields[offset].As<int>();
        //        }
        //    }
        //}

        //void SetInt(int offset, int value)
        //{
        //    unchecked
        //    {
        //        if (FieldLayoutByByte)
        //        {
        //            _fields[offset] = (value & 0xFF).As<object>();
        //            _fields[offset + 1] = ((value >> 8) & 0xFF).As<object>();
        //            _fields[offset + 2] = ((value >> 16) & 0xFF).As<object>();
        //            _fields[offset + 3] = ((value >> 24) & 0xFF).As<object>();
        //        }
        //        else
        //        {
        //            _fields[offset] = value.As<object>();
        //        }
        //    }
        //}

        //ulong GetULong(int offset)
        //{
        //    unchecked
        //    {
        //        if (FieldLayoutByByte)
        //        {
        //            return (
        //            _fields[offset].As<ulong>() |
        //            (_fields[offset + 1].As<ulong>() << 8) |
        //            (_fields[offset + 2].As<ulong>() << 16) |
        //            (_fields[offset + 3].As<ulong>() << 24) |
        //            (_fields[offset + 4].As<ulong>() << 32) |
        //            (_fields[offset + 5].As<ulong>() << 40) |
        //            (_fields[offset + 6].As<ulong>() << 48) |
        //            (_fields[offset + 7].As<ulong>() << 56)
        //            ).As<ulong>();
        //        }
        //        else
        //        {
        //            return _fields[offset].As<ulong>();
        //        }
        //    }
        //}

        //void SetULong(int offset, ulong value)
        //{
        //    unchecked
        //    {
        //        if (FieldLayoutByByte)
        //        {
        //            _fields[offset] = (value & 0xFF).As<object>();
        //            _fields[offset + 1] = ((value >> 8) & 0xFF).As<object>();
        //            _fields[offset + 2] = ((value >> 16) & 0xFF).As<object>();
        //            _fields[offset + 3] = ((value >> 24) & 0xFF).As<object>();
        //            _fields[offset + 4] = ((value >> 32) & 0xFF).As<object>();
        //            _fields[offset + 5] = ((value >> 40) & 0xFF).As<object>();
        //            _fields[offset + 6] = ((value >> 48) & 0xFF).As<object>();
        //            _fields[offset + 7] = ((value >> 56) & 0xFF).As<object>();
        //        }
        //        else
        //        {
        //            _fields[offset] = value.As<object>();
        //        }
        //    }
        //}

        //long GetLong(int offset)
        //{
        //    unchecked
        //    {
        //        if (FieldLayoutByByte)
        //        {
        //            return (
        //            _fields[offset].As<long>() |
        //            (_fields[offset + 1].As<long>() << 8) |
        //            (_fields[offset + 2].As<long>() << 16) |
        //            (_fields[offset + 3].As<long>() << 24) |
        //            (_fields[offset + 4].As<long>() << 32) |
        //            (_fields[offset + 5].As<long>() << 40) |
        //            (_fields[offset + 6].As<long>() << 48) |
        //            (_fields[offset + 7].As<long>() << 56)
        //            ).As<long>();
        //        }
        //        else
        //        {
        //            return _fields[offset].As<long>();
        //        }
        //    }
        //}

        //void SetLong(int offset, ulong value)
        //{
        //    unchecked
        //    {
        //        if (FieldLayoutByByte)
        //        {
        //            _fields[offset] = (value & 0xFF).As<object>();
        //            _fields[offset + 1] = ((value >> 8) & 0xFF).As<object>();
        //            _fields[offset + 2] = ((value >> 16) & 0xFF).As<object>();
        //            _fields[offset + 3] = ((value >> 24) & 0xFF).As<object>();
        //            _fields[offset + 4] = ((value >> 32) & 0xFF).As<object>();
        //            _fields[offset + 5] = ((value >> 40) & 0xFF).As<object>();
        //            _fields[offset + 6] = ((value >> 48) & 0xFF).As<object>();
        //            _fields[offset + 7] = ((value >> 56) & 0xFF).As<object>();
        //        }
        //        else
        //        {
        //            _fields[offset] = value.As<object>();
        //        }
        //    }
        //}

        //A struct whose member is only numeric type, safe to use DataView for its backing fields
        [NetJs.Name("$pureStruct")]
        internal bool IsPureStruct => GetClassPrototypeOf(this).Flags.TypeHasFlag(TypeFlagsModel.IsPureStruct);
        [NetJs.Name(Constants.StructFieldsAsArrayName)]
        internal object[] fieldsAsArray
        {
            get
            {
                if (_fields == null)
                {
                    var arr = NetJs.Script.NewArray<object>(GetClassPrototypeOf(this).Size);
                    Array.AddMetadata(arr, typeof(byte));
                    if (IsPureStruct)
                    {
                        var bv = new BitCasterView(arr);
                        _fields = bv;
                    }
                    else
                    {
                        _fields = arr;
                    }
                }
                if (Array.Is(_fields, null))
                    return _fields.As<object[]>();
                return _fields.As<BitCasterView>()._array.As<object[]>();
            }
        }

        internal BitCasterView fieldsAsDataView
        {
            get
            {
                Debug.Assert(IsPureStruct);
                return (_fields ??= new BitCasterView(fieldsAsArray)).As<BitCasterView>();
            }
        }

        internal Array fieldsToObjectArray => /*IsPureStruct ? fieldsAsDataView._dataSource :*/ fieldsAsArray;

        [NetJs.Name("$fieldRefs")]
        internal SimpleDictionary<RefOrPointer> fieldRefs;

        [NetJs.Name("$getFieldRefOrP")]
        public RefOrPointer<T> GetFieldRefOrPointer<T>(int byteOffset, bool pointer = false)
        {
            Debug.Assert(IsPureStruct);
            var knownType = typeof(T).As<RuntimeType>()._prototype.KnownType;
            if (knownType == KnownTypeHandle.SystemEnum)
            {
                knownType = typeof(T).As<RuntimeType>()._prototype.As<EnumPrototype>().UnderlyingType.KnownType;
            }
            T Get(int? i)
            {
                var totalByteOffset = byteOffset + (i ?? 0);
                var value = knownType switch
                {
                    KnownTypeHandle.SystemSByte => fieldsAsDataView.getInt8(totalByteOffset).As<T>(),
                    KnownTypeHandle.SystemByte => fieldsAsDataView.getUint8(totalByteOffset).As<T>(),
                    KnownTypeHandle.SystemInt16 => fieldsAsDataView.getInt16(totalByteOffset, true).As<T>(),
                    KnownTypeHandle.SystemUInt16 or KnownTypeHandle.SystemChar => fieldsAsDataView.getUint16(totalByteOffset, true).As<T>(),
                    KnownTypeHandle.SystemInt32 or KnownTypeHandle.SystemIntPtr => fieldsAsDataView.getInt32(totalByteOffset, true).As<T>(),
                    KnownTypeHandle.SystemUInt32 or KnownTypeHandle.SystemUIntPtr => fieldsAsDataView.getUint32(totalByteOffset, true).As<T>(),
                    KnownTypeHandle.SystemInt64 => fieldsAsDataView.getBigInt64(totalByteOffset, true).As<T>(),
                    KnownTypeHandle.SystemUInt64 => fieldsAsDataView.getBigUint64(totalByteOffset, true).As<T>(),
                    KnownTypeHandle.SystemSingle => fieldsAsDataView.getFloat32(totalByteOffset, true).As<T>(),
                    KnownTypeHandle.SystemDouble => fieldsAsDataView.getFloat64(totalByteOffset, true).As<T>(),
                    _ => throw null!
                };
                return value;
            }
            void Set(T value, int? i)
            {
                var totalByteOffset = byteOffset + (i ?? 0);
                switch (knownType)
                {
                    case KnownTypeHandle.SystemByte:
                        fieldsAsDataView.setUint8(totalByteOffset, value.As<byte>());
                        break;
                    case KnownTypeHandle.SystemSByte:
                        fieldsAsDataView.setInt8(totalByteOffset, value.As<sbyte>());
                        break;
                    case KnownTypeHandle.SystemChar:
                    case KnownTypeHandle.SystemUInt16:
                        fieldsAsDataView.setUint16(totalByteOffset, value.As<ushort>(), true);
                        break;
                    case KnownTypeHandle.SystemInt16:
                        fieldsAsDataView.setInt16(totalByteOffset, value.As<short>(), true);
                        break;
                    case KnownTypeHandle.SystemUInt32:
                    case KnownTypeHandle.SystemUIntPtr:
                        fieldsAsDataView.setUint32(totalByteOffset, value.As<uint>(), true);
                        break;
                    case KnownTypeHandle.SystemInt32:
                    case KnownTypeHandle.SystemIntPtr:
                        fieldsAsDataView.setInt32(totalByteOffset, value.As<int>(), true);
                        break;
                    case KnownTypeHandle.SystemUInt64:
                        fieldsAsDataView.setBigUint64(totalByteOffset, value.As<ulong>(), true);
                        break;
                    case KnownTypeHandle.SystemInt64:
                        fieldsAsDataView.setBigInt64(totalByteOffset, value.As<long>(), true);
                        break;
                    case KnownTypeHandle.SystemSingle:
                        fieldsAsDataView.setFloat32(totalByteOffset, value.As<float>(), true);
                        break;
                    case KnownTypeHandle.SystemDouble:
                        fieldsAsDataView.setFloat64(totalByteOffset, value.As<double>(), true);
                        break;
                    default:
                        throw null!;
                }
            }
            fieldRefs ??= new();
            if (pointer)
            {
                var existing = fieldRefs[byteOffset + 100000];
                if (NetJs.Script.IsUndefinedOrNull(existing))
                {
                    existing = new Pointer<T>(Get, Set);
                    fieldRefs[byteOffset + 100000] = existing;
                }
                return existing.As<Pointer<T>>();
            }
            else
            {
                var existing = fieldRefs[byteOffset + 200000];
                if (NetJs.Script.IsUndefinedOrNull(existing))
                {
                    existing = new Ref<T>(Get, Set);
                    fieldRefs[byteOffset + 200000] = existing;
                }
                return existing.As<Ref<T>>();
            }
        }

        [NetJs.Name("$innerObjects")]
        internal SimpleDictionary<object>? innerObjects;

        [NetJs.Name(NetJs.Constants.ObjectGetField)]
        internal object GetField(int offset, NetJs.Union<int, TypePrototype, Array> size)
        {
            object InnerStruct(TypePrototype prototype)
            {
                innerObjects ??= new();
                var mobject = innerObjects[offset];
                if (NetJs.Script.IsUndefinedOrNull(mobject))
                {
                    mobject = prototype.NewWithDefaultConstructor();
                    var byteCount = prototype.Size;
                    var ownDataView = fieldsAsDataView.Offset(offset, byteCount);
                    mobject.innerObjects = null;
                    mobject._fields = ownDataView;
                    innerObjects[offset] = mobject;
                }
                return mobject;
            }
            object InnerArray(TypePrototype elementPrototype, int counts)
            {
                unchecked
                {
                    var bytesSize = counts * elementPrototype.Size;
                    innerObjects ??= new();
                    var array = innerObjects[offset];
                    if (NetJs.Script.IsUndefinedOrNull(array))
                    {
                        var window = fieldsAsDataView.Offset(offset, bytesSize);
                        window.ProxyType = elementPrototype.Type ?? throw null!;
                        array = JSProxy.Create<object[]>(window);
                        innerObjects[offset] = array;
                    }
                    return array;
                }
            }
            if (IsPureStruct)
            {
                bool isInlineArray = Array.Is(size, null) && size.As<Array>().Length == 2;
                if (isInlineArray) //Array type laid inside this object
                {
                    unchecked
                    {
                        return InnerArray(size.As<Array>()[0].As<TypePrototype>(), size.As<Array>()[1].As<int>());
                    }
                }
                else
                {
                    var prototype = size.As<TypePrototype>();
                    var knownType = prototype.KnownType;
                    if (knownType == KnownTypeHandle.SystemEnum)
                    {
                        knownType = prototype.As<EnumPrototype>().UnderlyingType.KnownType;
                    }
                    var value = knownType switch
                    {
                        KnownTypeHandle.SystemSByte => fieldsAsDataView.getInt8(offset).As<object>(),
                        KnownTypeHandle.SystemByte => fieldsAsDataView.getUint8(offset).As<object>(),
                        KnownTypeHandle.SystemInt16 => fieldsAsDataView.getInt16(offset, true).As<object>(),
                        KnownTypeHandle.SystemUInt16 or KnownTypeHandle.SystemChar => fieldsAsDataView.getUint16(offset, true).As<object>(),
                        KnownTypeHandle.SystemInt32 or KnownTypeHandle.SystemIntPtr => fieldsAsDataView.getInt32(offset, true).As<object>(),
                        KnownTypeHandle.SystemUInt32 or KnownTypeHandle.SystemUIntPtr => fieldsAsDataView.getUint32(offset, true).As<object>(),
                        KnownTypeHandle.SystemInt64 => fieldsAsDataView.getBigInt64(offset, true).As<object>(),
                        KnownTypeHandle.SystemUInt64 => fieldsAsDataView.getBigUint64(offset, true).As<object>(),
                        KnownTypeHandle.SystemSingle => fieldsAsDataView.getFloat32(offset, true).As<object>(),
                        KnownTypeHandle.SystemDouble => fieldsAsDataView.getFloat64(offset, true).As<object>(),
                        _ => InnerStruct(prototype)
                    };
                    return value;
                }
            }
            unchecked
            {
                bool isNumber = NetJs.Script.TypeOf(size).NativeEquals("number");
                if (!isNumber)
                {
                    if (size.As<TypePrototype>().Flags.TypeHasFlag(TypeFlagsModel.IsValueType) &&
                        !size.As<TypePrototype>().Flags.TypeHasFlag(TypeFlagsModel.IsPrimitive)) //struct in struct, collapse the fields into this fields
                    {
                        return InnerStruct(size.As<TypePrototype>());
                    }
                }
                //bool isInlineArray = (size.As<uint>() & 0x80000000) != 0;
                //if (isInlineArray) //Array type laid inside this object
                //{
                //    size = (size.As<uint>() & 0x7FFFFFFF.As<uint>()).As<int>();
                //    innerObjects ??= new();
                //    var array = innerObjects[offset];
                //    if (NetJs.Script.IsUndefinedOrNull(array))
                //    {
                //        var window = fieldsAsDataView.Offset(offset, size.As<int>());
                //        //window.ProxyType = prototype.Type ?? throw null!;
                //        array = JSProxy.Create<object[]>(window);
                //        innerObjects[offset] = array;

                //        //arrayProxy = JSProxy.Create<object[]>(new ArrayWindowProxyHandler(fieldsAsArray, offset, size.As<int>()));
                //        //innerObjects[offset] = arrayProxy;
                //    }
                //    return array;
                //}
                return fieldsAsArray[offset];
            }
        }
        [NetJs.Name(NetJs.Constants.ObjectSetField)]
        internal void SetField(int offset, NetJs.Union<int, TypePrototype> size, object value)
        {
            void InnerArray(TypePrototype prototype, int counts)
            {
                unchecked
                {
                    var bytesSize = counts * prototype.Size;
                    var elementType = prototype.Type ?? throw null!;
                    for (int i = 0; i < counts && i < value.As<object[]>().Length; i++)
                    {
                        fieldsAsDataView.write(elementType, value.As<object[]>()[i], offset + i * prototype.Size);
                    }
                    return;
                }
            }
            if (IsPureStruct)
            {
                bool isInlineArray = Array.Is(size, null) && size.As<Array>().Length == 2;
                if (isInlineArray) //Array type laid inside this object
                {
                    unchecked
                    {
                        InnerArray(size.As<Array>()[0].As<TypePrototype>(), size.As<Array>()[1].As<int>());
                        return;
                    }
                }
                else
                {
                    var prototype = size.As<TypePrototype>();
                    var knownType = prototype.KnownType;
                    if (knownType == KnownTypeHandle.SystemEnum)
                    {
                        knownType = prototype.As<EnumPrototype>().UnderlyingType.KnownType;
                    }
                    var realSize = prototype.Size;
                    switch (knownType)
                    {
                        case KnownTypeHandle.SystemByte:
                            fieldsAsDataView.setUint8(offset, value.As<byte>());
                            break;
                        case KnownTypeHandle.SystemSByte:
                            fieldsAsDataView.setInt8(offset, value.As<sbyte>());
                            break;
                        case KnownTypeHandle.SystemChar:
                        case KnownTypeHandle.SystemUInt16:
                            fieldsAsDataView.setUint16(offset, value.As<ushort>(), true);
                            break;
                        case KnownTypeHandle.SystemInt16:
                            fieldsAsDataView.setInt16(offset, value.As<short>(), true);
                            break;
                        case KnownTypeHandle.SystemUInt32:
                        case KnownTypeHandle.SystemUIntPtr:
                            fieldsAsDataView.setUint32(offset, value.As<uint>(), true);
                            break;
                        case KnownTypeHandle.SystemInt32:
                        case KnownTypeHandle.SystemIntPtr:
                            fieldsAsDataView.setInt32(offset, value.As<int>(), true);
                            break;
                        case KnownTypeHandle.SystemUInt64:
                            fieldsAsDataView.setBigUint64(offset, value.As<ulong>(), true);
                            break;
                        case KnownTypeHandle.SystemInt64:
                            fieldsAsDataView.setBigInt64(offset, value.As<long>(), true);
                            break;
                        case KnownTypeHandle.SystemSingle:
                            fieldsAsDataView.setFloat32(offset, value.As<float>(), true);
                            break;
                        case KnownTypeHandle.SystemDouble:
                            fieldsAsDataView.setFloat64(offset, value.As<double>(), true);
                            break;
                        default:
                            fieldsAsDataView.write(prototype.Type!, value, offset);
                            break;
                    }
                    return;
                }
            }
            unchecked
            {
                bool isNumber = NetJs.Script.TypeOf(size).NativeEquals("number");
                if (!isNumber)
                {
                    if (size.As<TypePrototype>().Flags.TypeHasFlag(TypeFlagsModel.IsValueType) &&
                        !size.As<TypePrototype>().Flags.TypeHasFlag(TypeFlagsModel.IsPrimitive)) //struct in struct, collapse the fields into this fields
                    {
                        var realSize = size.As<TypePrototype>().Size;
                        if (fieldsAsArray.Length < offset)
                            NetJs.Script.Write("this.$fields.length = offset");
                        var sourceFields = value._fields;
                        if (sourceFields != null && sourceFields.As<object>()["$isProxy"].As<bool>() == true)
                        {
                            throw null;
                        }
                        //if (sourceFields.Length != realSize)
                        //{
                        //    throw null;
                        //}
                        fieldsAsArray.splice(offset, 0, value.fieldsAsArray);
                        return;
                    }
                }
                //bool isInlineArray = (size.As<uint>() & 0x80000000) != 0;
                //if (isInlineArray)
                //{
                //    size = (size.As<uint>() & 0x7FFFFFFF.As<uint>()).As<int>();
                //    if (fieldsAsArray.Length < offset)
                //        NetJs.Script.Write("this.$fields.length = offset");
                //    fieldsAsArray.splice(offset, size.As<int>(), value.As<object[]>());
                //    return;
                //}
                fieldsAsArray[offset] = value;
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

        //Make super keyword available in local function as this.$super
        [NetJs.Name(NetJs.Constants.SuperClassAccessName)]
        internal object Super => this["$$super"] ??= JSProxy.Create<object>(new SuperClassProxyHandler(this));


        [NetJs.MemberReplace(nameof(ReferenceEquals))]
        public static bool ReferenceEqualsImpl(object? objA, object? objB)
        {
            if (Constants.HandleStringAsValueTypePrimitive)
            {
                //String in c# is a reference type, but we box it in this port anyway because we use native js string
                //and the only way we can make its object member accessible is by boxing
                //Wen comparing two string reference though, sice they are boxed, they wont be equal as expected
                //This is a workaround
                if (objA is string sA && objB is string sB)
                {
                    return sA.NativeEquals(sB);
                }
            }
            else
            {
                //Both are string, not boxed
                if (NetJs.Script.TypeOf(objA).NativeEquals("string") && NetJs.Script.TypeOf(objB).NativeEquals("string"))
                {
                    //Unfortunately js doesnt allow us to test if the two string are same reference or not
                }
            }
            return objA == objB;
        }

        public static int GetHashCodeT<T>(T value)
        {
            if (value == null)
                return 0;
            var type = typeof(T).As<RuntimeType>();
            if (type.As<RuntimeType>()._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsEnum))
            {
                type = type._prototype.As<EnumPrototype>().UnderlyingType.Type.As<RuntimeType>();
            }
            if (type == typeof(long) || type == typeof(ulong))
            {
                var lvalue = value.As<long>();
                return (int)lvalue ^ (int)(lvalue >> 32);
            }
            if (type.As<RuntimeType>()._prototype.KnownType.IsIntegerNumeric())
                return value.As<int>() | 0;
            var getHashCodeName = NetJs.Script.Write<string>("\"{nameof(System.Object.GetHashCode())}\"");
            var method = value[getHashCodeName];
            if (NetJs.Script.IsDefined(method))
            {
                var hashCode = NetJs.Script.Write<int>("method.call(value)");
                return hashCode;
            }
            return RuntimeHelpers.GetHashCode(value);
        }

        [NetJs.Name("$$type")]
        internal Type TypeView => GetType();
        [NetJs.Name("$$hashCode")]
        internal int HashCodeView => GetHashCode();
        [NetJs.Name("$$string")]
        internal string? StringView => ToString();
    }
}
