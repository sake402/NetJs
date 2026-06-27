using NetJs;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Window;


namespace System
{
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    internal unsafe partial class RuntimeType
    {
        //internal TypeModel _model;
        internal string _metadataFullName;
        internal RuntimeAssembly? _assembly;
        internal GenericTypePrototypeProvider? _prototypeProvider;
        internal TypePrototype _prototype;
        //TypeModel _metadata;
        internal RuntimeType? _parentGenericTypeDefinition = null;

        //for generic types
        internal RuntimeType[]? _typeArguments;

        //for type parameters
        internal int _genericParameterPosition = -1;
        internal GenericParameterConstraintModel? _constraintModel;
        internal RuntimeType[]? _typeConstraints;

        //for array types
        internal RuntimeType? _arrayElementType;
        internal int _arrayTypeRank;

        MethodInfo[] _methods = [];
        PropertyInfo[] _properties = [];
        FieldInfo[] _fields = [];
        ConstructorInfo[] _constructors = [];
        EventInfo[] _events = [];

        //we want a System.Type to extend its Javascript prototype so that we can do things like Type.StaticMember on both the System.Type and its equivalent JS Prototype.StaticMember
        //We however dont want the constructor of the js prototype called when we construct the mix
        internal static RuntimeType Create(RuntimeAssembly? assembly, TypePrototype prototype, string metadataFullTypeName, GenericTypePrototypeProvider? provider)
        {
            //bool isClass = prototype != null && Script.Write<bool>("typeof prototype === 'function' && prototype.hasOwnProperty('prototype') && !prototype.hasOwnProperty('arguments')");
            //if (isClass)
            //{
            //    Script.Write($"const typeInfo = class extends $.$mix($.System.Type, prototype) {{ constructor(){{ }} }}");
            //    Script.Write($"const type = new typeInfo().__ctor__(assembly, prototype, model, scriptFullName)");
            //    Script.Write($"return type");
            //    return null!; //make compiler happy, we already returned
            //}
            //else
            {
                return new RuntimeType(assembly, prototype, metadataFullTypeName, provider);
            }
        }

        //DO NOT CALL DIRECTLY. We only call this in the static function above
        //we are merging these two constructor overload into one (with union) so there is no overload on System.Type and we can call it deterministically from Script.Write above
        [Name("__ctor__")] //we are renaming this constructor so it doesn't conflict with the js prototype we mix with it above
        private RuntimeType(RuntimeAssembly? assembly, TypePrototype prototype, string metadataFullTypeName, GenericTypePrototypeProvider? provider)
        {
            _impl = new RuntimeTypeHandle(prototype.TypeHandle.As<IntPtr>());
            _prototype = prototype;
            _assembly = assembly;
            //_model = model;
            _metadataFullName = metadataFullTypeName;
            _prototypeProvider = provider;
            prototype.Assembly = assembly;
            prototype.MetadataFullName = metadataFullTypeName;
            prototype.Type = this;
            //prototype.As<TypePrototype>().Model = model;
            //Object.DefineProperty(prototype.As<object>(), Constants.PrototypeTypeName, new PropertyDescriptor { Value = this });
            //prototype.As<TypePrototype>().Type = this;
            if (_assembly != null)
            {
                //prototype.As<object>()[Constants.AssemblyRegistryName] = _assembly;
                _assembly.As<RuntimeAssembly_Partial>()._types.Push(this);
            }
            AppDomain.GlobalTypeRegistry[metadataFullTypeName] = this;
            var existing = AppDomain.GlobalTypeRegistry[prototype.TypeHandle.As<uint>().GetAssemblyAndTypeHandle()];
            if (NetJs.Script.IsDefined(existing))
            {
                throw null!;
            }
            AppDomain.GlobalTypeRegistry[prototype.TypeHandle.As<uint>().GetAssemblyAndTypeHandle()] = this;
        }


        public string InternalAssemblyQualifiedName => InternalName + ", " + _assembly?.FullName;

        public string InternalName
        {
            get
            {
                if (InternalFullName == null)
                    return null;
                var split = InternalFullName.NativeSplit(",");
                return split[split.Length - 1];
            }
        }

        public string InternalFullName
        {
            get
            {
                return _prototype?.FullName ?? _metadataFullName;
                //var fullName = GetTypeNameFromHandle(_metadata.FullName);
                //if (IsGenericType)
                //{
                //    return fullName + "<" + string.Join(", ", GetGenericArguments().Map(t => t.FullName!)) + ">";
                //}
                //return fullName;
            }
        }
        public string InternalNamespace => InternalFullName.Substring(0, InternalFullName.LastIndexOf('.'));//?? null;

        //internal Type(Assembly assembly, TypePrototypeProvider? prototypeProvider, TypeModel metadata, string scriptFullName)
        //{
        //    _assembly = assembly;
        //    _prototypeProvider = prototypeProvider;
        //    //if (prototypeProvider != null)
        //    //_prototype = prototypeProvider(null, null);
        //    _metadata = metadata;
        //    _scriptFullName = scriptFullName;
        //    if (metadata != null && _prototype != null)
        //        Initialize();
        //}

        //internal void InitializeFrom(TypePrototype? mprototype, TypeModel model)
        //{
        //    _prototype = mprototype;
        //    _model = model;
        //    Initialize();
        //}

        //internal void InitializeFrom(TypePrototypeProvider? prototypeProvider, TypeModel model)
        //{
        //    _prototypeProvider = prototypeProvider;
        //    //if (prototypeProvider != null)
        //    //_prototype = prototypeProvider(null, null);
        //    _model = model;
        //    Initialize();
        //}

        [Name("$do_self_init")]
        internal void SelfInitialize()
        {
            if (_model != null)
                return;
            if (NetJs.Constants.UseInterfaceMixin == NetJs.InterfaceMixinMode.None)
            {
                //copy all properties defined on the interfaces onto this prototype
                if (_prototype.Kind != TypeKindModel.Interface && NetJs.Script.IsDefined(_prototype.Interfaces))
                {
                    static void ImplementInterfaces(TypePrototype targetClass, TypePrototype[] interfaceDefinitions)
                    {
                        if (interfaceDefinitions.Length == 0)
                        {
                            return;
                        }
                        unchecked
                        {
                            // 1. Get the current direct prototype object of your class
                            var classProto = targetClass.Prototype!;

                            // 2. Get the base/parent class prototype it currently inherits from
                            var currentBaseProto = Object.GetPrototypeOf(classProto);

                            // 3. Create a SINGLE isolated intermediate link that wraps ALL interfaces
                            // It inherits from the old base class so we maintain the structural chain
                            var unifiedInterfaceLink = Object.Create(currentBaseProto);

                            // Track keys to avoid redundant descriptor operations
                            var appliedInstanceKeys = new Window.Set();
                            var appliedStaticKeys = new Window.Set();

                            bool unifiedInterfaceLinkHasChanges = false;
                            // 4. Loop through every provided interface definition
                            for (var i = 0; i < interfaceDefinitions.Length; i++)
                            {
                                var interfaceDef = interfaceDefinitions[i];

                                // --- PART A: Handle Instance Methods/Getters/Setters ---
                                if (NetJs.Script.IsDefined(interfaceDef.Prototype))
                                {
                                    var instanceKeys = Reflect.ownKeys(interfaceDef.Prototype!);
                                    var instLen = instanceKeys.Length;

                                    for (var j = 0; j < instLen; j++)
                                    {
                                        var key = instanceKeys[j];

                                        if (NetJs.Script.TypeOf(key).NativeNotEquals("string"))
                                            continue;

                                        if (key.NativeEquals("constructor") ||
                                            key.NativeEquals("prototype") ||
                                            key.NativeEquals("length") ||
                                            key.NativeEquals("name") ||
                                            key.NativeCharCodeAt(0) == '$')
                                        {
                                            continue;
                                        }

                                        // Skip if a previous interface in this execution batch already added it
                                        if (NetJs.Script.IsDefined(classProto[key]) || appliedInstanceKeys.has(key))
                                        {
                                            continue;
                                        }

                                        var descriptor = Object.GetOwnPropertyDescriptor(interfaceDef.Prototype!, key);
                                        if (NetJs.Script.IsDefined(descriptor))
                                        {
                                            Object.DefineProperty(unifiedInterfaceLink, key, descriptor);
                                            appliedInstanceKeys.add(key);
                                            unifiedInterfaceLinkHasChanges = true;
                                        }
                                    }
                                }

                                // --- PART B: Handle Static Constants/Methods ---
                                var staticKeys = Reflect.ownKeys(interfaceDef);
                                var statLen = staticKeys.Length;

                                for (int j = 0; j < statLen; j++)
                                {
                                    var key = staticKeys[j];

                                    if (NetJs.Script.TypeOf(key).NativeNotEquals("string"))
                                        continue;

                                    if (key.NativeEquals("constructor") ||
                                        key.NativeEquals("prototype") ||
                                        key.NativeEquals("length") ||
                                        key.NativeEquals("name") ||
                                        (key.NativeCharCodeAt(0) == '$' && key.NativeNotEquals(NetJs.Constants.IsTypeName) && key.NativeNotEquals(NetJs.Constants.DefaultTypeName)))
                                    {
                                        continue;
                                    }

                                    // Prioritize targetClass explicit statics, then earlier batch interfaces
                                    if (NetJs.Script.IsDefined(targetClass[key]) || appliedStaticKeys.has(key))
                                    {
                                        continue;
                                    }

                                    var descriptor = Object.GetOwnPropertyDescriptor(interfaceDef, key);
                                    if (NetJs.Script.IsDefined(descriptor))
                                    {
                                        Object.DefineProperty(targetClass, key, descriptor);
                                        appliedStaticKeys.add(key);
                                    }
                                }
                            }

                            // 5. 🚀 THE CRITICAL LINK: Splice the single unified layer into the chain.
                            // This alters the internal inheritance vector without assigning to the read-only property.
                            if (unifiedInterfaceLinkHasChanges)
                                Object.SetPrototypeOf(classProto, unifiedInterfaceLink);
                        }
                    }
                    ImplementInterfaces(_prototype, _prototype.Interfaces!);
                    //static void MergeDescriptors(TypePrototype interfaceDefinition, TypePrototype targetClass)
                    //{
                    //    var keys = Window.Reflect.ownKeys(interfaceDefinition); // Captures strings, symbols, and non-enumerables
                    //    var len = keys.Length;

                    //    for (int i = 0; i < len; i++)
                    //    {
                    //        unchecked
                    //        {
                    //            var key = keys[i];

                    //            // Avoid overwriting critical native engine foundations
                    //            if (key.NativeEquals("constructor") ||
                    //                key.NativeEquals("prototype") ||
                    //                key.NativeEquals("length") ||
                    //                key.NativeEquals("name") ||
                    //                key.NativeCharCodeAt(0) == '$')
                    //            {
                    //                continue;
                    //            }
                    //            if (NetJs.Script.KeyIn(key, targetClass))
                    //            {
                    //                continue;
                    //            }
                    //            var sourceDescriptor = Object.GetOwnPropertyDescriptor(interfaceDefinition, key);
                    //            if (NetJs.Script.IsUndefined(sourceDescriptor))
                    //            {
                    //                Object.DefineProperty(targetClass, key, sourceDescriptor);
                    //            }
                    //        }
                    //    }
                    //}
                    //for (int i = 0; i < _prototype.Interfaces!.Length; i++)
                    //{
                    //    unchecked
                    //    {
                    //        var it = _prototype.Interfaces[i];
                    //        if (NetJs.Script.IsDefined(_prototype.Prototype) && NetJs.Script.IsDefined(it.Prototype))
                    //        {
                    //            MergeDescriptors(it.Prototype!, _prototype.Prototype!);
                    //        }
                    //        //Statioc methods
                    //        MergeDescriptors(it, _prototype);
                    //    }
                    //}

                }
            }
            _model = _prototype.Metadata!;
            if (Script.IsDefined(_model.As<TypeModel>().Methods))
            {
                _model.As<TypeModel>().Methods!.ForEach(m =>
                {
                    var methodInfo = new RuntimeMethodInfo(m);
                    _methods.Push(methodInfo);
                });
            }
            if (Script.IsDefined(_model.As<TypeModel>().Properties))
            {
                _model.As<TypeModel>().Properties!.ForEach(m =>
                {
                    var propertyInfo = new RuntimePropertyInfo_Partial(m);
                    _properties.Push(propertyInfo.As<RuntimePropertyInfo>());
                });
            }
            if (Script.IsDefined(_model.As<TypeModel>().Fields))
            {
                _model.As<TypeModel>().Fields!.ForEach(m =>
                {
                    var fieldInfo = new RuntimeFieldInfo_Partial(m);
                    _fields.Push(fieldInfo.As<RuntimeFieldInfo>());
                });
            }
            if (Script.IsDefined(_model.As<TypeModel>().Constructors))
            {
                _model.As<TypeModel>().Constructors!.ForEach(m =>
                {
                    var constructorInfo = new RuntimeConstructorInfo(m);
                    _constructors.Push(constructorInfo);
                });
            }
            if (Script.IsDefined(_model.As<TypeModel>().Events))
            {
                _model.As<TypeModel>().Events!.ForEach(m =>
                {
                    var eventInfo = new RuntimeEventInfo_Partial(m);
                    _events.Push(eventInfo.As<RuntimeEventInfo>());
                });
            }
            if (_assembly != null && _prototype != null)
            {
                //bool isGenericDefinition = _scriptFullName.NativeEndsWith("$") || _scriptFullName.NativeEndsWith(">");
                //bool isInterface = _metadata!.Kind == TypeKindModel.Interface;
                //bool isInterfaceMixin = isInterface && _prototypeProvider != null && Script.Write<int>("this._prototypeProvider.length") >= 1;
                //AppDomain.GlobalTypeRegistry[_metadata.FullName] = this;
                ////dont try so set inner types, they are managed and readonly within the containing type
                //if (!_metadata.Flags.TypeHasFlag(TypeFlagsModel.IsNested))
                //    AppDomain.GlobalPrototypeRegistry.SetNested(_scriptFullName.NativeReplace("<", "$").NativeReplace(",", "$").NativeReplace(">", "$"), isGenericDefinition || isInterfaceMixin ? _prototypeProvider.As<TypePrototype>() : _prototype);
                //AppDomain.SetupDefaults(this);

                //var fn = FullName;
                //if (fn != null)
                //    AppDomain.GlobalPrototypeRegistry.SetNested(fn, _prototype);
                //AppDomain.GlobalPrototypeRegistry[_metadata.FullName] = _prototype;
                //AppDomain.GlobalTypeRegistry[_metadata.FullName] = this;
            }
        }


        [Name("$do_static_init")]
        void StaticInitialize()
        {
            if (_prototype != null)
            {
                if (Object.HasOwnProperty(_prototype, Constants.StaticInitializerName))
                    _prototype.StaticInitializeMembers();
                if (Object.HasOwnProperty(_prototype, Constants.StaticConstructorName))
                    _prototype.StaticConstructor();
                //if (Script.IsDefined(_prototype[Constants.StaticInitializerName]))
                //    _prototype.StaticInitializeMembers();
                //if (Script.IsDefined(_prototype[Constants.StaticConstructorName]))
                //    _prototype.StaticConstructor();
            }
        }

        internal bool _isCompleted;
        internal bool _isCompleting;
        [Name("$do_complete")]
        internal void Complete()
        {
            if (_isCompleted || _isCompleting)
                return;
            _isCompleting = true;
            try
            {
                SelfInitialize();
                if (!_prototype!.Flags.TypeHasFlag(TypeFlagsModel.IsGenericType) || _typeArguments != null) //dont run static init on an unsubstituted generic type definition
                    StaticInitialize();
                _isCompleted = true;
            }
            finally
            {
                _isCompleting = false;
            }
        }

        static bool MemberFilter(MemberInfo i, BindingFlags bindingAttr, Type[]? parameterTypes = null)
        {
            if (i.IsPublic && bindingAttr.TypeHasFlag(BindingFlags.Public))
                return true;
            if (i.IsPrivate && bindingAttr.TypeHasFlag(BindingFlags.NonPublic))
                return true;
            if (i.IsStatic && bindingAttr.TypeHasFlag(BindingFlags.Static))
                return true;
            if (!i.IsStatic && bindingAttr.TypeHasFlag(BindingFlags.Instance))
                return true;
            if (i.MemberType == MemberTypes.Method && bindingAttr.TypeHasFlag(BindingFlags.InvokeMethod))
            {
                if (parameterTypes == null)
                {
                    return true;
                }
                else
                {
                    if (parameterTypes.Length == i.As<RuntimeMethodInfo>().GetParameters().Length)
                    {
                        for (int ip = 0; ip < parameterTypes.Length; ip++)
                        {
                            if (parameterTypes[ip] != i.As<MethodInfo>().GetParameters()[ip].ParameterType)
                                return false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        //[Name(Constants.TypeIsAssignableName)]
        internal static bool IsAssignableInternal(RuntimeType lhs, RuntimeType rhs)
        {
            return IsSubClassOfInternal(rhs, lhs);
            //if (rhs == lhs || lhs.FullName == rhs.FullName)
            //    return true;
            //if (Script.IsDefined(rhs._model.As<TypeModel>().BaseType))
            //{
            //    var btfn = GetTypeFromHandle(rhs._model.As<TypeModel>().BaseType!.Value);
            //    if (btfn != null && IsAssignableInternal(lhs, btfn))
            //        return true;
            //}
            //if (Script.IsDefined(rhs._model.As<TypeModel>().Interfaces))
            //{
            //    for (int i = 0; i < rhs._model.As<TypeModel>().Interfaces!.Length; i++)
            //    {
            //        var btfn = GetTypeFromHandle(rhs._model.As<TypeModel>().Interfaces![i]);
            //        if (btfn != null && IsAssignableInternal(lhs, btfn))
            //            return true;
            //    }
            //}
            //return false;
        }

        [Name(Constants.TypeIsAssignableName)]
        internal static bool IsSubClassOfInternal(RuntimeType child, RuntimeType @base)
        {
            if (@base == child/* || child.FullName == @base.FullName*/)
                return true;
            //Ensure the child is initialized, else we cant access its model
            child.SelfInitialize();
            if (Script.IsDefined(child._model.As<TypeModel>().BaseType))
            {
                var childBase = GetTypeFromHandle(child._model.As<TypeModel>().BaseType!.Value.As<uint>());
                if (childBase != null && IsSubClassOfInternal(childBase, @base))
                    return true;
            }
            if (@base.IsInterface && Script.IsDefined(child._model.As<TypeModel>().Interfaces))
            {
                for (int i = 0; i < child._model.As<TypeModel>().Interfaces!.Length; i++)
                {
                    var childBase = GetTypeFromHandle(child._model.As<TypeModel>().Interfaces![i].As<uint>());
                    if (childBase != null && IsSubClassOfInternal(childBase, @base))
                        return true;
                }
            }
            return false;
        }

        internal MemberInfo[] GetMembersInternal(
             MemberTypes memberTypes,
             BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
             string? name = null,
             Type[]? parameterTypes = null,
             MemberListType listType = MemberListType.All)
        {

            MemberInfo[] Filter(MemberInfo[] info)
            {
                if (name != null)
                {
                    info = info.Filter(i => i.Name == name);
                }
                info = info.Filter(i =>
                {
                    return MemberFilter(i, bindingAttr, parameterTypes);
                });
                return info;
            }
            MemberInfo[] members = [];
            if (memberTypes.TypeHasFlag(MemberTypes.Constructor))
            {
                members = members.ArrayConcat(Filter(_constructors)).As<MemberInfo[]>();
            }
            if (memberTypes.TypeHasFlag(MemberTypes.Field))
            {
                members = members.ArrayConcat(Filter(_fields)).As<MemberInfo[]>();
            }
            if (memberTypes.TypeHasFlag(MemberTypes.Property))
            {
                members = members.ArrayConcat(Filter(_properties)).As<MemberInfo[]>();
            }
            if (memberTypes.TypeHasFlag(MemberTypes.Event))
            {
                members = members.ArrayConcat(Filter(_events)).As<MemberInfo[]>();
            }
            if (memberTypes.TypeHasFlag(MemberTypes.Method))
            {
                members = members.ArrayConcat(Filter(_methods)).As<MemberInfo[]>();
            }
            return members;
        }

        internal MemberInfo? GetMemberInternal(uint memberHandle)
        {
            return (MemberInfo?)_constructors.ArrayFirstOrDefault(c => c._model.Handle.As<uint>().GetMemberHandle() == memberHandle.GetMemberHandle()) ??
               (MemberInfo?)_fields.ArrayFirstOrDefault(c => c._model.Handle.As<uint>().GetMemberHandle() == memberHandle.GetMemberHandle()) ??
               (MemberInfo?)_properties.ArrayFirstOrDefault(c => c._model.Handle.As<uint>().GetMemberHandle() == memberHandle.GetMemberHandle()) ??
               (MemberInfo?)_properties.Map(p => p.GetMethod).Filter(c => c != null).ArrayFirstOrDefault(c => c!._model.Handle.As<uint>().GetMemberHandle() == memberHandle.GetMemberHandle()) ??
               (MemberInfo?)_properties.Map(p => p.SetMethod).Filter(c => c != null).ArrayFirstOrDefault(c => c!._model.Handle.As<uint>().GetMemberHandle() == memberHandle.GetMemberHandle()) ??
               (MemberInfo?)_events.ArrayFirstOrDefault(c => c._model.Handle.As<uint>().GetMemberHandle() == memberHandle.GetMemberHandle()) ??
               (MemberInfo?)_methods.ArrayFirstOrDefault(c => c._model.Handle.As<uint>().GetMemberHandle() == memberHandle.GetMemberHandle());
        }

        public static RuntimeType? GetTypeFromHandle(uint typeHandle)
        {
            return AppDomain.GetType(typeHandle.As<uint>());
        }

        //public static string? GetTypeNameFromHandle(uint typeHandle)
        //{
        //    var assemblyHandle = typeHandle.AssemblyHandle();
        //    var metadata = AppDomain.GlobalMetadataRegistry[assemblyHandle.As<uint>()];
        //    if (Script.IsUndefined(metadata))
        //        return null;
        //    var name = metadata.TypeNames[typeHandle.TypeHandle()];
        //    if (Script.IsUndefined(name))
        //        return null;
        //    return name;
        //}

        //public static Type? GetType(AssemblyModel assembly, int typeIndex)
        //{
        //    var typeName = assembly.TypeNames[typeIndex];
        //    return GetType(typeName);
        //}

        //public static string GetTypeName(AssemblyModel assembly, int typeIndex)
        //{
        //    var typeName = assembly.TypeNames[typeIndex];
        //    return typeName;
        //}

        //ulong UpdateGenericHandle(ulong handle, RuntimeType[] typeArguments)
        //{
        //    if (handle >= KnownTypeHandle.GenericType1Placeholder.As<ulong>() && handle <= KnownTypeHandle.GenericType31Placeholder.As<ulong>())
        //    {
        //        var index = handle.As<uint>() - KnownTypeHandle.GenericType1Placeholder.As<uint>();
        //        handle = typeArguments[index]._model.As<TypeModel>().Handle;
        //    }
        //    return handle.As<ulong>();
        //}

        internal RuntimeType MakeGenericTypeInternal(RuntimeType[] typeArguments, TypePrototype prototype, string scriptFullName)
        {
            if (_prototype.GenericArguments != typeArguments.Length)
                throw new ArgumentException("Incorrect number of type arguments supplied");
            //An instantiated generic type must have a different handle from its parent
            _assembly.As<RuntimeAssembly_Partial>().NewTypeHandle(prototype);
            //var newTypeModel = prototype!.Metadata!;// Script.JSONParse<TypeModel>(Script.JSONStringify(prototype.Metadata!));
            ////replace every generic type placeholder in the new model with the provided type arguments
            //if (Script.IsDefined(newTypeModel.GenericArguments))
            //    newTypeModel.GenericArguments = newTypeModel.GenericArguments!.Map(g => UpdateGenericHandle(g, typeArguments));
            //if (Script.IsDefined(newTypeModel.Fields))
            //{
            //    newTypeModel.Fields!.ForEach(f =>
            //    {
            //        var memberHandle = f.Handle.As<uint>().GetMemberHandle();
            //        f.Handle = (newTypeModel!.Handle.As<uint>() | (memberHandle.As<uint>() << ReflectionHandleExtension.MemberShift)).As<ulong>();
            //        if (Script.IsDefined(f.FieldType))
            //            f.FieldType = UpdateGenericHandle(f.FieldType, typeArguments);
            //    });
            //}
            //if (Script.IsDefined(newTypeModel.Properties))
            //{
            //    newTypeModel.Properties!.ForEach(f =>
            //    {
            //        var memberHandle = f.Handle.As<uint>().GetMemberHandle();
            //        f.Handle = (newTypeModel!.Handle.As<uint>() | (memberHandle.As<uint>() << ReflectionHandleExtension.MemberShift)).As<ulong>();
            //        if (Script.IsDefined(f.PropertyType))
            //            f.PropertyType = UpdateGenericHandle(f.PropertyType, typeArguments);
            //    });
            //}
            //if (Script.IsDefined(newTypeModel.Events))
            //{
            //    newTypeModel.Events!.ForEach(f =>
            //    {
            //        var memberHandle = f.Handle.As<uint>().GetMemberHandle();
            //        f.Handle = (newTypeModel!.Handle.As<uint>() | (memberHandle.As<uint>() << ReflectionHandleExtension.MemberShift)).As<ulong>();
            //        if (Script.IsDefined(f.EventHandlerType))
            //            f.EventHandlerType = UpdateGenericHandle(f.EventHandlerType, typeArguments);
            //    });
            //}
            //if (Script.IsDefined(newTypeModel.Methods))
            //{
            //    newTypeModel.Methods!.ForEach(f =>
            //    {
            //        var memberHandle = f.Handle.As<uint>().GetMemberHandle();
            //        f.Handle = (newTypeModel!.Handle.As<uint>() | (memberHandle.As<uint>() << ReflectionHandleExtension.MemberShift)).As<ulong>();
            //        if (Script.IsDefined(f.ReturnType))
            //        {
            //            f.ReturnType = UpdateGenericHandle(f.ReturnType.As<ulong>(), typeArguments);
            //        }
            //        if (Script.IsDefined(f.Parameters))
            //        {
            //            f.Parameters!.ForEach(p =>
            //            {
            //                if (Script.IsDefined(p.ParameterType))
            //                    p.ParameterType = UpdateGenericHandle(p.ParameterType, typeArguments);
            //            });
            //        }
            //    });
            //}
            var t = RuntimeType.Create(_assembly, prototype, scriptFullName, null);
            t._parentGenericTypeDefinition = _parentGenericTypeDefinition ?? this;
            if (prototype.Flags.TypeHasFlag(TypeFlagsModel.IsArray))
            {
                Debug.Assert(typeArguments.Length == 1);
                unchecked
                {
                    t._arrayElementType = typeArguments[0];
                    t._arrayTypeRank = 1;
                    prototype.Element = typeArguments[0]._prototype!;
                }
            }
            else
            {
                t._typeArguments = typeArguments;
                prototype.Arguments = typeArguments.Map(t => t._prototype!);
            }
            _assembly.As<RuntimeAssembly_Partial>().RegisterCompletionNotification(t);
            return t;
        }

        RuntimeType MakeGenericTypeInternal(params RuntimeType[] typeArguments)
        {
            if (_prototypeProvider == null)
                throw new InvalidOperationException();
            if (_prototype.GenericArguments != typeArguments.Length)
                throw new ArgumentException("Incorrect number of type arguments supplied");
            var newScriptName = RuntimeAssembly_Partial.InsertGenericNames(_metadataFullName, typeArguments.Map(t => t._metadataFullName));
            var existingPrototype = AppDomain.GlobalPrototypeRegistry[newScriptName];
            if (NetJs.Script.IsDefined(existingPrototype))
                return existingPrototype.As<TypePrototype>().Type.As<RuntimeType>();
            //If the type we are mixing for depends on itself, we need to pass this into the getPrototype so it can be used in the mixin definition
            var selfProxy = RuntimeAssembly_Partial.CreateTypeProxy(newScriptName);
            var genericTypes = typeArguments.Map(t => t.As<RuntimeType>()._prototype!);
            //var newPrototype = NetJs.Script.Write<TypePrototype>("this._prototypeProvider(selfProxy, gArgs, null)");
            //var newPrototype = _prototypeProvider(gArgs);
            var genericProvider = _prototypeProvider;
            var newPrototype = NetJs.Script.Write<TypePrototype>("genericProvider(genericTypes[0], genericTypes[1], genericTypes[2], genericTypes[3], genericTypes[4], genericTypes[5], genericTypes[6], genericTypes[7], genericTypes[8], genericTypes[9], genericTypes[10], genericTypes[11], genericTypes[12], genericTypes[13], genericTypes[14], genericTypes[15], genericTypes[16], genericTypes[17], genericTypes[18], genericTypes[19], genericTypes[20], genericTypes[21], genericTypes[22], genericTypes[23], genericTypes[24], genericTypes[25], genericTypes[26], genericTypes[27], genericTypes[28], genericTypes[29], genericTypes[30], genericTypes[31])");
            //var newPrototype = _prototypeProvider(typeArguments.Map(t => t.As<RuntimeType>()._prototype!), null);
            var newType = MakeGenericTypeInternal(typeArguments, newPrototype, newScriptName);
            selfProxy.TargetType = newType;
            selfProxy.Prototype = newPrototype;
            return newType;
        }

        internal Type[] GetGenericArgumentsInternalImpl()
        {
            if (!IsGenericType)
                return Type.EmptyTypes;
            if (IsGenericTypeDefinition)
            {
                var count = _prototype.GenericArguments;
                return AppDomain.GenericTypes.Slice(0, count).Map(e => e.Type!).AsNetArray();
            }
            //if (_typeArguments != null)
            return _typeArguments ?? throw null!;
            //return _typeArguments = _model.As<TypeModel>().GenericArguments?.Map((arg, i, all) =>
            //{
            //    var argType = GetTypeFromHandle(arg.As<uint>());
            //    var constraint = Script.IsDefined(_model.As<TypeModel>().GenericConstraints) ? _model.As<TypeModel>().GenericConstraints![i] : null;//?.Filter(c => c.ParameterName == arg)[0];
            //    bool firstConstraintIsClass;
            //    if (constraint != null && constraint.TypeConstraints?.Length > 0)
            //    {
            //        var firstConstraintType = GetTypeFromHandle(constraint.TypeConstraints[0].As<uint>());
            //        firstConstraintIsClass = firstConstraintType != null && !firstConstraintType.IsInterface;
            //    }
            //    else
            //    {
            //        firstConstraintIsClass = false;
            //    }
            //    var model = new TypeModel()
            //    {
            //        //Name = arg,
            //        Handle = 0,// arg,
            //        BaseType = constraint != null && firstConstraintIsClass ? constraint.TypeConstraints?[0] : null,
            //        Interfaces = constraint != null ? (firstConstraintIsClass ? constraint.TypeConstraints!.Slice(1).As<ulong[]>() : constraint.TypeConstraints) : null,
            //    };
            //    var type = Create(_assembly, AppDomain.GenericTypes[i], model, _scriptFullName, null);
            //    type._genericParameterPosition = i;
            //    type._typeConstraints = constraint?.TypeConstraints?.Map(c => GetTypeFromHandle(c.As<uint>()) ?? throw new InvalidOperationException());
            //    type._constraintModel = constraint;
            //    return type;
            //}).AsNetArray() ?? Array.Empty<RuntimeType>();
        }
        //static SimpleDictionary<RuntimeType> types = new SimpleDictionary<RuntimeType>();

        // Returns the type from which the current type directly inherits from (without reflection quirks).
        // The parent type is null for interfaces, pointers, byrefs and generic parameters.
        [NetJs.MemberReplace(nameof(GetParentType) + "(QCallTypeHandle, ObjectHandleOnStack)")]
        private static void GetParentTypeImpl(QCallTypeHandle type, ObjectHandleOnStack res)
        {
            var runtimeType = RuntimeHelpers.QCallTypeHandleToRuntimeType(type);
            if (runtimeType._prototype.Kind == TypeKindModel.Class ||
                runtimeType._prototype.Kind == TypeKindModel.Struct ||
                runtimeType._prototype.Kind == TypeKindModel.Delegate ||
                runtimeType._prototype.Kind == TypeKindModel.Enum)
            {
                if (NetJs.Script.IsDefined(runtimeType._model.As<TypeModel>().BaseType))
                {
                    res.GetObjectHandleOnStack<RuntimeType?>() = AppDomain.GetType(runtimeType._model.As<TypeModel>().BaseType!.Value.As<uint>());
                    return;
                }
            }
            res.GetObjectHandleOnStack<RuntimeType?>() = null;
        }

        [NetJs.MemberReplace(nameof(GetCorrespondingInflatedMethod))]
        private static MemberInfo GetCorrespondingInflatedMethodImpl(QCallTypeHandle type, MemberInfo generic)
        {
            return generic;
        }

        [NetJs.MemberReplace(nameof(make_array_type))]
        private static void make_array_typeImpl(QCallTypeHandle type, int rank, ObjectHandleOnStack res)
        {
            var tp = type.QCallTypeHandleToRuntimeType();
            var genericArray = AppDomain.GlobalTypeRegistry.GetNested($"{NetJs.Constants.SystemPrivateCoreLib}.System.Array<>");
            var genericArrayType = genericArray.MakeGenericTypeInternal([tp]);
            genericArrayType._arrayTypeRank = rank;
            genericArrayType._arrayElementType = tp;
            res.GetObjectHandleOnStack<Type>() = genericArrayType;
        }

        [NetJs.MemberReplace(nameof(make_byref_type))]
        private static void make_byref_typeImpl(QCallTypeHandle type, ObjectHandleOnStack res)
        {
            var tp = type.QCallTypeHandleToRuntimeType();
            var genericArray = AppDomain.GlobalTypeRegistry.GetNested($"{NetJs.Constants.SystemPrivateCoreLib}.System.RefOrPointer<>");
            var genericArrayType = genericArray.MakeGenericTypeInternal([tp]);
            res.GetObjectHandleOnStack<Type>() = genericArrayType;
        }

        [NetJs.MemberReplace(nameof(make_pointer_type))]
        private static void make_pointer_typeImpl(QCallTypeHandle type, ObjectHandleOnStack res)
        {
            var tp = type.QCallTypeHandleToRuntimeType();
            var genericArray = AppDomain.GlobalTypeRegistry.GetNested($"{NetJs.Constants.SystemPrivateCoreLib}.System.RefOrPointer<>");
            var genericArrayType = genericArray.MakeGenericTypeInternal([tp]);
            res.GetObjectHandleOnStack<Type>() = genericArrayType;
        }

        [NetJs.MemberReplace(nameof(MakeGenericType))]
        private static void MakeGenericTypeImpl(Type gt, Type[] types, ObjectHandleOnStack res)
        {
            var genericArrayType = gt.As<RuntimeType>().MakeGenericTypeInternal(types.As<RuntimeType[]>());
            res.GetObjectHandleOnStack<Type>() = genericArrayType;
        }

        [NetJs.MemberReplace(nameof(GetMethodsByName))]
        internal RuntimeMethodInfo[] GetMethodsByNameOverride(string? name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
        {
            return GetMembersInternal(MemberTypes.Method, bindingAttr, name, null, listType).As<RuntimeMethodInfo[]>().AsNetArray();
        }

        [NetJs.MemberReplace(nameof(GetConstructors_internal))]
        private RuntimeConstructorInfo[] GetConstructors_internalOverride(BindingFlags bindingAttr, RuntimeType reflectedType)
        {
            return GetMembersInternal(MemberTypes.Constructor, bindingAttr).As<RuntimeConstructorInfo[]>().AsNetArray();
        }

        [NetJs.MemberReplace(nameof(GetPropertiesByName))]
        private RuntimePropertyInfo[] GetPropertiesByNameOverride(string? name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
        {
            return GetMembersInternal(MemberTypes.Property, bindingAttr, name, null, listType).As<RuntimePropertyInfo[]>().AsNetArray();
        }

        [NetJs.MemberReplace(nameof(GetFields_internal))]
        private RuntimeFieldInfo[] GetFields_internalOverride(string? name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
        {
            return GetMembersInternal(MemberTypes.Field, bindingAttr, name, null, listType).As<RuntimeFieldInfo[]>().AsNetArray();
        }

        [NetJs.MemberReplace(nameof(GetEvents_internal))]
        private RuntimeFieldInfo[] GetEvents_internalOverride(string? name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
        {
            return GetMembersInternal(MemberTypes.Event, bindingAttr, name, null, listType).As<RuntimeFieldInfo[]>().AsNetArray();
        }

        [NetJs.MemberReplace(nameof(GetInterfaces))]
        private static void GetInterfacesImpl(QCallTypeHandle type, ObjectHandleOnStack res)
        {
            var mtype = type.QCallTypeHandleToRuntimeType();
            res.GetObjectHandleOnStack<RuntimeType[]?>() = (mtype._model.As<TypeModel>().Interfaces?.Map(i => RuntimeType.GetTypeFromHandle(i.As<uint>()) ?? throw new InvalidOperationException()) ?? []).AsNetArray();
        }

        [NetJs.MemberReplace(nameof(GetNestedTypes_internal))]
        private RuntimeType[] GetNestedTypes_internalOverride(string? displayName, BindingFlags bindingAttr, MemberListType listType)
        {
            return (_model.As<TypeModel>().NestedTypes?.Map(i => RuntimeType.GetTypeFromHandle(i.As<uint>()) ?? throw new InvalidOperationException())
                .Filter(nt => (displayName == null || nt.Name.Contains(displayName)) && MemberFilter(nt, bindingAttr, null)) ?? []).AsNetArray();
        }

        [NetJs.MemberReplace(nameof(GetDeclaringType))]
        private static void GetDeclaringTypeImpl(QCallTypeHandle type, ObjectHandleOnStack res)
        {
            var mtype = type.QCallTypeHandleToRuntimeType();
            res.GetObjectHandleOnStack<Type?>() = mtype._model.DeclaringType != 0 ? RuntimeType.GetTypeFromHandle(mtype._model.DeclaringType.As<uint>()) : null;
        }

        [NetJs.MemberReplace(nameof(GetName))]
        private static void GetNameImpl(QCallTypeHandle type, ObjectHandleOnStack res)
        {
            var mtype = type.QCallTypeHandleToRuntimeType();
            res.GetObjectHandleOnStack<string?>() = mtype.InternalName;
        }

        [NetJs.MemberReplace(nameof(GetNamespace))]
        private static void GetNamespaceImpl(QCallTypeHandle type, ObjectHandleOnStack res)
        {
            var mtype = type.QCallTypeHandleToRuntimeType();
            res.GetObjectHandleOnStack<string?>() = mtype.InternalNamespace;
        }

        [NetJs.MemberReplace(nameof(GetInterfaceMapData))]
        private static void GetInterfaceMapDataImpl(QCallTypeHandle t, QCallTypeHandle iface, out MethodInfo[] targets, out MethodInfo[] methods)
        {
            var targetType = t.QCallTypeHandleToRuntimeType();
            var interfaceType = iface.QCallTypeHandleToRuntimeType();
            targets = targetType.GetMembersInternal(MemberTypes.Method).As<MethodInfo[]>().AsNetArray();
            methods = interfaceType.GetMembersInternal(MemberTypes.Method).As<MethodInfo[]>().AsNetArray();
        }

        [NetJs.MemberReplace(nameof(GetPacking))]
        private static void GetPackingImpl(QCallTypeHandle type, out int packing, out int size)
        {
            var mtype = type.QCallTypeHandleToRuntimeType();
            packing = 0;
            size = 0;
        }

        [NetJs.MemberReplace(nameof(CreateInstanceInternal))]
        private static object CreateInstanceInternalImpl(QCallTypeHandle type)
        {
            var mtype = type.QCallTypeHandleToRuntimeType();
            return NetJs.Script.Write<object>("(new mtype._prototype).$ctor()");
        }

        [NetJs.MemberReplace(nameof(GetDeclaringMethod))]
        private static void GetDeclaringMethodImpl(QCallTypeHandle type, ObjectHandleOnStack res)
        {
            var mtype = type.QCallTypeHandleToRuntimeType();
            res.GetObjectHandleOnStack<MethodInfo?>() = null;
        }

        [NetJs.MemberReplace(nameof(getFullName))]
        internal static void getFullNameImpl(QCallTypeHandle type, ObjectHandleOnStack res, bool full_name, bool assembly_qualified)
        {
            var mtype = type.QCallTypeHandleToRuntimeType();
            var name = assembly_qualified ? mtype.InternalAssemblyQualifiedName : full_name ? mtype.InternalFullName : mtype.Name;
            res.GetObjectHandleOnStack<string?>() = name;
        }

        [NetJs.MemberReplace(nameof(GetGenericArgumentsInternal))]
        private static void GetGenericArgumentsInternalImpl(QCallTypeHandle type, ObjectHandleOnStack res, bool runtimeArray)
        {
            var mtype = type.QCallTypeHandleToRuntimeType();
            res.GetObjectHandleOnStack<Type[]>() = mtype.GetGenericArgumentsInternalImpl();
        }

        [NetJs.MemberReplace(nameof(GetGenericParameterPosition))]
        private static int GetGenericParameterPositionImpl(QCallTypeHandle type)
        {
            var mtype = type.QCallTypeHandleToRuntimeType();
            return mtype._genericParameterPosition;
        }

        [NetJs.MemberReplace(nameof(IsUnmanagedFunctionPointerInternal))]
        internal static bool IsUnmanagedFunctionPointerInternalImpl(QCallTypeHandle type)
        {
            return false;
        }

        [NetJs.MemberReplace(nameof(FunctionPointerReturnAndParameterTypes))]
        internal static IntPtr FunctionPointerReturnAndParameterTypesImpl(QCallTypeHandle type)
        {
            throw new NotImplementedException();
        }

        [NetJs.MemberReplace(nameof(GetFunctionPointerTypeModifiers))]
        internal static Type[] GetFunctionPointerTypeModifiersImpl(QCallTypeHandle type, int position, bool optional)
        {
            throw new NotImplementedException();
        }

        [NetJs.MemberReplace(nameof(GetCallingConventionFromFunctionPointerInternal))]
        internal static byte GetCallingConventionFromFunctionPointerInternalImpl(QCallTypeHandle type)
        {
            throw new NotImplementedException();
        }

        [NetJs.MemberReplace(nameof(GetGenericParameterConstraints))]
        public Type[] GetGenericParameterConstraintsOverride()
        {
            if (!IsGenericParameter)
                throw new InvalidOperationException(SR.Arg_NotGenericParameter);

            return _typeConstraints ?? Type.EmptyTypes;
        }

        [NetJs.MemberReplace(nameof(GetGenericParameterAttributes))]
        private GenericParameterAttributes GetGenericParameterAttributesOverride()
        {
            var flags = GenericParameterAttributes.None;
            if (_constraintModel != null)
            {
                if (_constraintModel.Flags.TypeHasFlag(GenericConstraintFlagsModel.HasNewConstraint))
                {
                    flags |= GenericParameterAttributes.DefaultConstructorConstraint;
                }
                if (_constraintModel.Flags.TypeHasFlag(GenericConstraintFlagsModel.HasClassConstraint))
                {
                    flags |= GenericParameterAttributes.ReferenceTypeConstraint;
                }
                if (_constraintModel.Flags.TypeHasFlag(GenericConstraintFlagsModel.HasStructConstraint))
                {
                    flags |= GenericParameterAttributes.NotNullableValueTypeConstraint;
                }
                //if (_constraintModel.Flags.TypeHasFlag(GenericConstraintFlagsModel.HasUnmanagedConstraint))
                //{
                //    flags |= GenericParameterAttributes.;
                //}
            }
            return flags;
        }

        [NetJs.MemberReplace(nameof(Assembly))]
        public Assembly AssemblyImpl => _assembly;

        //default implementation of Cache causes recursion
        //=>Cache=>Interlock.CompareExchange=>IsValueType=>Cache
        [NetJs.MemberReplace(nameof(Cache))]
        internal TypeCache CacheImpl => cache ??= new TypeCache();

        //[NetJs.MemberReplace(nameof(GetMethodsByName_native))]
        //internal static extern IntPtr GetMethodsByName_native_Impl(QCallTypeHandle type, IntPtr namePtr, BindingFlags bindingAttr, MemberListType listType)
        //{
        //    var ttype = type.QCallTypeHandleToRuntimeType();
        //    var name = System.Runtime.InteropServices.Marshal.MarshalObject(namePtr).As<string>();
        //}
    }
}