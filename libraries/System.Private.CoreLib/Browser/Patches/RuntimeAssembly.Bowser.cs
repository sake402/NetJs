using NetJs;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace System.Reflection
{
    [NetJs.ForcePartial(typeof(RuntimeAssembly))]
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    internal sealed partial class RuntimeAssembly_Partial : ForcedPartialBase<RuntimeAssembly>
    {
        [NetJs.NativeDelegate]
        [NetJs.External]
        internal delegate void OnCompleted();

        internal RuntimeModule_Partial _module;
        internal RuntimeType[] _types = [];
        internal AssemblyModel _model;
        uint _nextTypeHandle = 0x4000;

        public RuntimeAssembly_Partial(AssemblyModel model, string assemblyName)
        {
            NetJs.Script.Write("this._mono_assembly = model.h");
            this._model = model;
            _module = new RuntimeModule_Partial(this);
            if (model.AssemblyFlags.TypeHasFlag(AssemblyFlags.Entry))
                Assembly._entry = this.As<Assembly>();
            //_nextTypeHandle = _model.TypeNames.Length.As<uint>();
        }

        internal static TypeProxyHandler CreateTypeProxy(string metadataFullTypeName)
        {
            var proxyHandler = new TypeProxyHandler(metadataFullTypeName);
            object? proxy = null;
            NetJs.Script.Write("proxy = new Proxy({}, proxyHandler)");
            return proxy.As<TypeProxyHandler>();
        }

        internal static NativeFunction<TypeProxyHandler> CreateGenericTypeProxy(string metadataFullTypeName)
        {
            TypeProxyHandler? _handler = null;
            NativeFunction<TypeProxyHandler> deferedType = () => _handler ??= CreateTypeProxy(metadataFullTypeName);
            return deferedType;
        }

        /// <summary>
        /// Define a proxy to a type/prototype not yet created. 
        /// </summary>
        /// <param name="metadataFullTypeName"></param>
        [NetJs.Name(NetJs.Constants.AssemblyTypeProxyName)]
        void TypeProxy(string metadataFullTypeName)
        {
            if (!AppDomain.GlobalPrototypeRegistry.ContainsKey(metadataFullTypeName))
            {
                var proxy = metadataFullTypeName.NativeEndsWith(">") ? CreateGenericTypeProxy(metadataFullTypeName).As<TypeProxyHandler>() : CreateTypeProxy(metadataFullTypeName);
                AppDomain.GlobalPrototypeRegistry.SetNested(metadataFullTypeName.NativeReplaceAll("<", "$").NativeReplaceAll(",", "$").NativeReplaceAll(">", "$"), proxy.As<TypePrototype>());
            }
        }

        internal ulong CreateTypeHandle()
        {
            var handle = ((_model.Handle.As<uint>() << ReflectionHandleExtension.AssemblyShift) | (_nextTypeHandle << ReflectionHandleExtension.TypeShift));
            //_model.TypeNames.Push(typeName);
            _nextTypeHandle++;
            return handle.As<ulong>();
        }

        internal void NewTypeHandle(TypePrototype prototype)
        {
            prototype.TypeHandle = CreateTypeHandle();
            //Now that we changed the handle, some metadata field may be referencing this handle, we need to rebuild the metadata
            prototype.MetadataBackingField = null;
        }

        TypeModel GetModel(TypePrototype prototype, TypePrototype? parent = null)
        {
            //var localAssemblyTypeName = prototype.FullName;
            //if (localAssemblyTypeName.NativeStartsWith("$"))
            //{
            //    var firstDot = localAssemblyTypeName.NativeIndexOf(".");
            //    localAssemblyTypeName = localAssemblyTypeName.NativeSubstring(firstDot + 1);
            //}
            TypeModel? typeMetadata = prototype.Metadata ?? null;
            //unchecked
            //{
            //    typeMetadata = _model.Types?.Filter(t =>
            //    {
            //        if (NetJs.Script.IsUndefinedOrNull(t.Handle))
            //            return false;
            //        return _model.TypeNames[t.Handle.As<uint>().GetTypeHandle()].NativeEquals(localAssemblyTypeName);
            //    })[0];
            //}
            //type has no metadata exported, create one
            if (NetJs.Script.IsUndefinedOrNull(typeMetadata))
            {
                //prototype.TypeHandle = CreateTypeHandle();
                //var pth = prototype.FullName.Split('.');
                //var pth = prototype.FullName.NativeSplit(".");
                typeMetadata = new TypeModel
                {
                    //Flags = prototype.Flags,
                    //Name = pth[pth.Length - 1],
                    Handle = NetJs.Script.IsDefined(prototype.TypeHandle) ? prototype.TypeHandle : CreateTypeHandle()
                };
            }
            //A nested class within a generic class should obtain a new type handle different from the type inside the generic type definition
            var isNestedClass = prototype.Flags.TypeHasFlag(TypeFlagsModel.IsNested);
            if (isNestedClass && NetJs.Script.IsDefined(parent) && NetJs.Script.IsDefined(parent!.Arguments))
            {
                NewTypeHandle(prototype);
            }
            return typeMetadata!;
        }

        string GetJsName(string metadataFullTypeName)
        {
            return metadataFullTypeName.NativeReplaceAll("<", "$").NativeReplaceAll(",", "$").NativeReplaceAll(">", "$");
        }

        [NetJs.Name(NetJs.Constants.AssemblyStructName)]
        NetJs.Union<TypePrototype, TypePrototypeProvider> DefineStruct(string metadataFullTypeName, TypePrototypeProvider provider)
        {
            return DefineType(metadataFullTypeName, provider, TypeFlagsModel.IsValueType);
        }

        [NetJs.Name(NetJs.Constants.AssemblyNestedStructName)]
        NetJs.Union<TypePrototype, TypePrototypeProvider> DefineNestedStruct(string metadataFullTypeName, TypePrototypeProvider provider, TypePrototype parent, NativeAction<Union<TypePrototype, TypePrototypeProvider>>? typePrototypeSink = null)
        {
            return DefineType(metadataFullTypeName, provider, TypeFlagsModel.IsValueType | TypeFlagsModel.IsNested, parent, typePrototypeSink);
        }

        [NetJs.Name(NetJs.Constants.AssemblyNestedClassName)]
        NetJs.Union<TypePrototype, TypePrototypeProvider> DefineNestedClass(string metadataFullTypeName, TypePrototypeProvider provider, TypePrototype parent, NativeAction<Union<TypePrototype, TypePrototypeProvider>>? typePrototypeSink = null)
        {
            return DefineType(metadataFullTypeName, provider, TypeFlagsModel.IsNested, parent, typePrototypeSink);
        }

        //SimpleDictionary<Union<TypePrototype, TypePrototypeProvider>>? prototypeRegistry;
        //TypePrototype[]? genericTypes;
        [NetJs.Name(NetJs.Constants.AssemblyDefineClassName)]
        internal NetJs.Union<TypePrototype, TypePrototypeProvider> DefineType(
            string metadataFullTypeName,
            TypePrototypeProvider provider,
            TypeFlagsModel flags,
            TypePrototype? parent = null,
            NativeAction<Union<TypePrototype, TypePrototypeProvider>>? typePrototypeSink = null)
        {
            if (NetJs.Script.IsUndefined(flags))
                flags = TypeFlagsModel.None;
            //provider.As<TypePrototype>().MetadataFullName = fullTypeName;
            var jsName = GetJsName(metadataFullTypeName);
            //bool isNestedClass = Constants.NestedClassAsNestedStaticObject && typeMetadata!.Flags.TypeHasFlag(TypeFlagsModel.IsNested);
            //if (_isCompleted) //if the assembly was marked completed, any other class defined after that is a nested class
            var isNestedClass = flags.TypeHasFlag(TypeFlagsModel.IsNested);
            var prototypeRegistry = AppDomain.GlobalPrototypeRegistry;
            //Dont try reading namespace for nested types, it will return the nested static method/property within the containing class anyway, and get recursive
            var existing = !isNestedClass ? prototypeRegistry.GetNested(jsName) : null;
            if (NetJs.Script.IsDefined(existing))
            {
                //if we have created a typestub, this is existing as Proxy type with handler TypeProxyHandler, now we have its prototype
#pragma warning disable CS0184 // 'is' expression's given expression is never of the provided type
                //if (!(existing is TypeProxyHandler))
                if (!(NetJs.Script.Write<bool>("existing.$isProxy === true")) && NetJs.Script.TypeOf(existing).NativeNotEquals("function"))
                    return existing!;
#pragma warning restore CS0184 // 'is' expression's given expression is never of the provided type
            }

            var len = metadataFullTypeName.Length;
            var lastChar = len > 0 ? metadataFullTypeName.NativeCharCodeAt(len - 1) : '\0';
            var isGenericDefinition = (lastChar == '$' || lastChar == '>');
            //||
            //    metadataFullTypeName.NativeSplit(".").Some(e => e.NativeEndsWith("$") || e.NativeEndsWith(">"));

            //If this type depends on itself, its proxy was created before we even run DefineType, otherwize create a new proxy for it,
            //and pass the proxy into the provider as $self so it can be used in the type definition,
            //and later we will update the proxy with the real type and prototype
            var selfProxy = NetJs.Script.TypeOf(existing).NativeEquals("function") ?
                existing.As<NativeFunction<TypeProxyHandler>>()() :
                existing.As<TypeProxyHandler>() ?? (!isGenericDefinition ? CreateTypeProxy(metadataFullTypeName) : null);
            var genericTypes = AppDomain.GenericTypeParameters;
            var genericProvider = isGenericDefinition ? provider.As<GenericTypePrototypeProvider>() : null;
            //TypePrototype prototype = !isGenericDefinition ? provider(selfProxy!, null, null) : NetJs.Script.Write<TypePrototype>("provider.apply(null, genericTypes)");
            TypePrototype prototype;
            if (!isGenericDefinition)
            {
                prototype = provider(selfProxy!);
            }
            else
            {
                var paramCount = NetJs.Script.Write<int>("genericProvider.length");
                unchecked
                {
                    //Fast paths with known arguments
                    if (paramCount == 0)
                    {
                        prototype = genericProvider();
                    }
                    else if (paramCount == 1)
                    {
                        prototype = genericProvider(genericTypes[0]);
                    }
                    else if (paramCount == 2)
                    {
                        prototype = genericProvider(genericTypes[0], genericTypes[1]);
                    }
                    else if (paramCount == 3)
                    {
                        prototype = genericProvider(genericTypes[0], genericTypes[1], genericTypes[2]);
                    }
                    else if (paramCount == 4)
                    {
                        prototype = genericProvider(genericTypes[0], genericTypes[1], genericTypes[2], genericTypes[3]);
                    }
                    else if (paramCount == 5)
                    {
                        prototype = genericProvider(genericTypes[0], genericTypes[1], genericTypes[2], genericTypes[3], genericTypes[4]);
                    }
                    else if (paramCount == 6)
                    {
                        prototype = genericProvider(genericTypes[0], genericTypes[1], genericTypes[2], genericTypes[3], genericTypes[4], genericTypes[5]);
                    }
                    else if (paramCount == 7)
                    {
                        prototype = genericProvider(genericTypes[0], genericTypes[1], genericTypes[2], genericTypes[3], genericTypes[4], genericTypes[5], genericTypes[6]);
                    }
                    else if (paramCount == 8)
                    {
                        prototype = genericProvider(genericTypes[0], genericTypes[1], genericTypes[2], genericTypes[3], genericTypes[4], genericTypes[5], genericTypes[6], genericTypes[7]);
                    }
                    else
                    {
                        //Slower but will rarely be used
                        var args = NetJs.Script.NewArray<object>(paramCount);
                        for(int i = 0; i < paramCount; i++)
                        {
                            unchecked
                            {
                                args[i] = genericTypes[i];
                            }
                        }
                        prototype = NetJs.Script.Write<TypePrototype>("genericProvider.apply(null, args)");
                        //prototype = NetJs.Script.Write<TypePrototype>("genericProvider( ...genericTypes.slice(0, paramCount))");
                    }
                }
            }
            //prototype.MetadataFullName = metadataFullTypeName;
            //TypeModel typeMetadata = GetModel(prototype, parent);
            flags = prototype.Flags;
            bool isInterface = prototype.Kind == TypeKindModel.Interface;
            bool isInterfaceMixin = isInterface && NetJs.Script.Write<int>("provider.length") >= 2;
            RuntimeType? type = null;
            if (isInterfaceMixin)
            {
                type = RuntimeType.Create(THIS, prototype, metadataFullTypeName, genericProvider);
            }
            else if (isGenericDefinition)
            {
                type = RuntimeType.Create(THIS, prototype, metadataFullTypeName, genericProvider);
            }
            else
            {
                //if this type is a nested type withing a generic type, it needs a new runtime handle, just like its instantiated generic parent
                if (isNestedClass && flags.TypeHasFlag(TypeFlagsModel.IsGenericType))
                {
                    NewTypeHandle(prototype);
                    //typeMetadata = prototype.Metadata!;
                }
                type = RuntimeType.Create(THIS, prototype, metadataFullTypeName, null);
            }
            //Now that we have the concrete type and some js closure already holds the stub/proxy
            //Supply the real things to the proxy so it can forward it as neccessary
            if (selfProxy != null)
            {
                selfProxy.TargetType = type;
                selfProxy.Prototype = prototype;
            }
            if (NetJs.Script.IsDefined(existing))
            {
                //existing.As<TypeProxyHandler>().TargetType = type;
                //existing.As<TypeProxyHandler>().Prototype = prototype;
                //remove the typeStub just before we insert the real type
                prototypeRegistry.RemoveNested(jsName);
            }
            //bool typeCompleted = false;
            //dont try so set inner types, they are managed and readonly static within the containing type
            if (!isNestedClass)
            {
                static NativeFunction<Union<TypePrototype, TypePrototypeProvider>, bool>? CreateCompleter(RuntimeType type, RuntimeAssembly_Partial assembly)
                {
                    bool Completer(Union<TypePrototype, TypePrototypeProvider> _)
                    {
                        if (assembly._isCompleted && !type._isCompleted)
                        {
                            type.Complete();
                        }
                        return type._isCompleted;
                    }
                    return Completer;
                }
                //Dont initialize type until they are actually accessed
                //Dont static initialize open generic types
                prototypeRegistry.SetNested(jsName, isGenericDefinition ? provider : prototype, onAccess: CreateCompleter(type, this));
            }
            if (!isInterfaceMixin && !isGenericDefinition)
                AppDomain.SetupDefaults(type);
            if (NetJs.Script.IsDefined(typePrototypeSink))
            {
                if (!isGenericDefinition)
                    typePrototypeSink!(prototype!);
                else
                    typePrototypeSink!(provider);
            }
            if (isNestedClass)
            {
                if (prototype != null && NetJs.Script.IsDefined(parent))
                {
                    prototype.Parent = parent;
                }
                //Initialize nested types immedialty. If we are crrating it, it means we already access it
                RegisterCompletionNotification(type);
            }
            if (!isInterfaceMixin && !isGenericDefinition && prototype != null)
            {
                if (NetJs.Script.IsDefined(prototype[Constants.ExportMethodName]))
                {
                    prototype.DoExports();
                }
            }
            return (!isGenericDefinition ? prototype : null) ?? provider.As<TypePrototype>();
        }

        internal static string InsertGenericNames(string fullTypeName, string[] genericArguments)
        {
            unchecked
            {
                var len = fullTypeName.Length;
                if (len == 0 || fullTypeName.NativeCharCodeAt(len - 1) != '>')
                    throw new InvalidOperationException();
                var indexOfLessThan = fullTypeName.NativeLastIndexOf("<");
                if (indexOfLessThan == -1)
                {
                    throw new InvalidOperationException();
                }
                int nArgs = len - indexOfLessThan - 1;
                if (nArgs != genericArguments.Length)
                    throw new InvalidOperationException("Number of generic arguments doesnt match");
                return $"{fullTypeName.NativeSlice(0, indexOfLessThan)}<{genericArguments.Join(",")}>";
            }
        }

        [NetJs.Name("$mix")]
        internal TypePrototype Mixin(string metadataFullTypeName, TypePrototype[] genericArguments, TypePrototype? mix, TypePrototypeProvider getPrototype)
        {
            static bool endsWithGenericId(string fullName)
            {
                var len = fullName.Length;
                if (len < 2)
                    return false;
                var lastChar = fullName.NativeCharCodeAt(len - 1);
                var beforeLastChar = fullName.NativeCharCodeAt(len - 2);
                return lastChar == '>' && (beforeLastChar == '<' || beforeLastChar == ',');
                //return fullName.NativeEndsWith("<>") || fullName.NativeEndsWith(",>");
            }
            static bool IsGenericTypeDefinition(TypePrototype t)
            {
                //It is very much possible that the t(TypePrototype) we have here is actually a System.Type, if we had created a stub of it that isn't replace yet
                //But we can be very sure it isn't a generic type
                //#pragma warning disable CS0184 // 'is' expression's given expression is never of the provided type
                //                if (t is Type)
                //                {
                //                    //the only thing the stub has at this point is just its fullName
                //                    return endsWithGenericId(t.As<Type>().FullName!);
                //                }
                //#pragma warning restore CS0184 // 'is' expression's given expression is never of the provided type
                return t.IsGenericParameter() || endsWithGenericId(t.FullName);
                //return !t.Type!.IsGenericTypeDefinition;
            }
            unchecked
            {
                string cacheKey;
                //string fullNameWithGenericArguments = metadataFullTypeName;
                string metadataFullNameWithGenericArguments = metadataFullTypeName;
                var argLen = genericArguments.Length;
                bool hasNonGenericDef = false;
                if (argLen > 0)
                {
                    //string[] fullNames = NetJs.Script.NewArray<string>(argLen);
                    string[] metadataFullNames = NetJs.Script.NewArray<string>(argLen);

                    for (int i = 0; i < argLen; i++)
                    {
                        var arg = genericArguments[i];
                        //fullNames[i] = m.FullName ?? "";
                        if (arg.IsGenericParameter())
                        {
                            metadataFullNames[i] = "";
                        }
                        else
                        {
                            metadataFullNames[i] = arg.MetadataFullName ?? "";
                        }
                        if (!hasNonGenericDef && !IsGenericTypeDefinition(arg))
                        {
                            hasNonGenericDef = true;
                        }
                    }

                    // Apply string construction only once 
                    metadataFullNameWithGenericArguments = InsertGenericNames(metadataFullTypeName, metadataFullNames);
                    cacheKey = metadataFullNameWithGenericArguments;

                    //fullNameWithGenericArguments = InsertGenericNames(metadataFullTypeName, genericArguments.Map(m => m?.FullName ?? ""));
                    //metadataFullNameWithGenericArguments = InsertGenericNames(metadataFullTypeName, genericArguments.Map(m =>
                    //{
                    //    if (m.IsGenericParameter())
                    //        return "";
                    //    return m?.MetadataFullName ?? "";
                    //}));
                    //cacheKey = metadataFullNameWithGenericArguments;
                    if (NetJs.Script.IsDefined(mix))
                    {
                        cacheKey += "+" + mix!.MetadataFullName;
                    }
                }
                else
                {
                    cacheKey = metadataFullTypeName;
                    if (NetJs.Script.IsDefined(mix))
                        cacheKey += "+" + mix!.MetadataFullName;
                }
                var existingPrototype = AppDomain.GlobalPrototypeRegistry[cacheKey];
                if (NetJs.Script.IsDefined(existingPrototype))
                    return existingPrototype.As<TypePrototype>();
                //If the type we are mixing for depends on itself, we need to pass this into the getPrototype so it can be used in the mixin definition
                var selfProxy = CreateTypeProxy(metadataFullNameWithGenericArguments);
                //var prototype = NetJs.Script.Write<TypePrototype>("getPrototype(selfProxy)");
                var prototype = getPrototype(selfProxy);
                AppDomain.GlobalPrototypeRegistry[cacheKey] = prototype;
                //this is a new class prototype, define its System.Type if any of the typArgument is not a genericName
                if (argLen > 0 && hasNonGenericDef)
                {
                    var genericType = AppDomain.GlobalTypeRegistry[metadataFullTypeName];
                    RuntimeType[] typesList = NetJs.Script.NewArray<RuntimeType>(argLen);
                    for (int i = 0; i < argLen; i++)
                    {
                        typesList[i] = genericArguments[i].Type.As<RuntimeType>()!;
                    }
                    var newType = genericType.MakeGenericTypeInternal(typesList, prototype, metadataFullNameWithGenericArguments);
                    selfProxy.TargetType = newType;
                    selfProxy.Prototype = prototype;
                    AppDomain.GlobalTypeRegistry[metadataFullNameWithGenericArguments!] = newType;
                }
                return prototype;
            }
        }

        [NetJs.Name(NetJs.Constants.InterfaceMixin)]
        TypePrototype InterfaceMixin(string metadataFullTypeName, TypePrototype[] mixes, TypePrototypeProvider getPrototype)
        {
            if (mixes.Length != 1)
                throw new InvalidOperationException("Interface mixin must be 1");
            unchecked
            {
                return Mixin(metadataFullTypeName, [], mixes[0], getPrototype);
            }
        }

        [NetJs.Name(NetJs.Constants.GenericInterfaceMixin)]
        TypePrototype GenericInterfaceMixin(string metadataFullTypeName, TypePrototype[] mixes, TypePrototypeProvider getPrototype)
        {
            if (mixes.Length < 2)
                throw new InvalidOperationException("Generic Interface mixin must be at least 2");
            unchecked
            {
                return Mixin(metadataFullTypeName, mixes.ArraySlice(0, mixes.Length - 1).As<TypePrototype[]>(), mixes[mixes.Length - 1], getPrototype);
            }
        }

        [NetJs.Name(NetJs.Constants.GenericType)]
        internal TypePrototype GenericType(string metadataFullTypeName, TypePrototype[] genericArgs, TypePrototypeProvider getPrototype)
        {
            return Mixin(metadataFullTypeName, genericArgs, null, getPrototype);
        }

        //        [NetJs.Name("$dlg")]
        //        TypePrototype Delegate(string fullTypeName, TypePrototype returnType, TypePrototype[] parameters)
        //        {
        //            var jsName = GetJsName(fullTypeName);
        //            var existing = AppDomain.GlobalPrototypeRegistry.GetNested(jsName);
        //            if (Script.IsDefined(existing))
        //            {
        //                //if we have created a typestub, this is existing as Proxy type with handler TypeProxyHandler, now we have its prototype
        //#pragma warning disable CS0184 // 'is' expression's given expression is never of the provided type
        //                //if (!(existing is TypeProxyHandler))
        //                if (!(Script.Write<bool>("existing.$isProxy === true")))
        //                    return existing.As<TypePrototype>()!;
        //#pragma warning restore CS0184 // 'is' expression's given expression is never of the provided type
        //            }
        //        }

        internal bool _isCompleted;
        internal OnCompleted[] onCompleted = [];

        internal void RegisterCompletionNotification(RuntimeType type)
        {
            if (_isCompleted && !type._isCompleted)
            {
                type.Complete();
            }
            else
            {
                onCompleted.Push(() =>
                {
                    if (!type._isCompleted)
                    {
                        type.Complete();
                    }
                });
            }
        }

        [NetJs.Name("$do_complete")]
        internal void Complete()
        {
            _isCompleted = true;
            onCompleted.ForEach(o => o());
            onCompleted = null!;
        }


        internal RuntimeType? GetTypeInternal(string name, bool ignoreCase = false)
        {
            var firstComma = name.NativeIndexOf(",");
            if (firstComma >= 0)
            {
                var secondComma = name.NativeIndexOf(",", firstComma + 1);
                if (secondComma >= 0)
                {
                    //ignore the version, culture and token
                    name = name.NativeSubstring(0, secondComma);
                }
            }
            if (name.NativeEndsWith(", mscorlib"))
            {
                name = name.NativeSubstring(0, name.Length - 10) + ", System.Private.CoreLib";
            }
            for (int i = 0; i < _types.Length; i++)
            {
                var t = _types[i];
                if (t.InternalFullName.NativeEquals(name) || t.InternalAssemblyQualifiedName.NativeEquals(name))
                    return t;
                if (ignoreCase)
                {
                    if (t.InternalFullName.NativeToLower().NativeEquals(name) || t.InternalAssemblyQualifiedName.NativeToLower().NativeEquals(name))
                        return t;
                }
            }
            return null;
        }


        [NetJs.MemberReplace]
        private static void GetEntryPoint(QCallAssembly assembly, ObjectHandleOnStack res)
        {
            var massembly = assembly.QCallAssemblyHandleToRuntimeType().As<RuntimeAssembly_Partial>();
            var model = massembly._model;
            var method = (MethodInfo?)AppDomain.GetMember(model.Entry.As<uint>());
            res.GetObjectHandleOnStack<MethodInfo?>() = method;
        }

        [NetJs.MemberReplace]
        private static void GetManifestResourceNames(QCallAssembly assembly_h, ObjectHandleOnStack res)
        {
            var assembly = assembly_h.QCallAssemblyHandleToRuntimeType().As<RuntimeAssembly_Partial>();
            var names = assembly._model.Manifests?.Map(e => e.Name);
            res.GetObjectHandleOnStack<string[]?>() = names?.AsNetArray();
        }

        [NetJs.MemberReplace]
        private static void GetExportedTypes(QCallAssembly assembly_h, ObjectHandleOnStack res)
        {
            var assembly = assembly_h.QCallAssemblyHandleToRuntimeType().As<RuntimeAssembly_Partial>();
            res.GetObjectHandleOnStack<Type[]?>() = assembly._types.Filter(e => e._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsPublic)).AsNetArray();
        }

        [NetJs.MemberReplace]
        private static void GetTopLevelForwardedTypes(QCallAssembly assembly_h, ObjectHandleOnStack res)
        {
            var assembly = assembly_h.QCallAssemblyHandleToRuntimeType().As<RuntimeAssembly_Partial>();
            res.GetObjectHandleOnStack<Type[]?>() = [];
        }

        [NetJs.MemberReplace("GetInfo(QCallAssembly, ObjectHandleOnStack, AssemblyInfoKind)")]
        [NetJs.MemberParameterTypesMayNotMatch]
        private static void GetInfoImpl(QCallAssembly assembly, ObjectHandleOnStack res, int kind)
        {
            var runtimeAssembly = assembly.QCallAssemblyHandleToRuntimeType().As<RuntimeAssembly_Partial>();
            switch ((int)kind)
            {
                //Location
                case 1:
                    res.GetObjectHandleOnStack<string?>() = "localhost";
                    break;
                //CodeBase = 2,
                case 2:
                    res.GetObjectHandleOnStack<string?>() = "localhost";
                    break;
                //FullName = 3,
                case 3:
                    res.GetObjectHandleOnStack<string>() = runtimeAssembly._model.FullName;
                    break;
                //ImageRuntimeVersion = 4
                case 4:
                    res.GetObjectHandleOnStack<string>() = runtimeAssembly._model.Version;
                    break;
            }
        }

        [NetJs.MemberReplace]
        private static bool GetManifestResourceInfoInternal(QCallAssembly assembly, string name, ManifestResourceInfo info)
        {
            var runtimeAssembly = assembly.QCallAssemblyHandleToRuntimeType().As<RuntimeAssembly_Partial>();
            var manifest = runtimeAssembly._model.Manifests?.ArrayFirstOrDefault(a => a.Name == name);
            if (manifest != null)
            {
                //info.ResourceLocation = ResourceLocation.Embedded;
                NetJs.Script.Debugger();
                return true;
            }
            return false;
        }

        [NetJs.MemberReplace]
        private static IntPtr /* byte* */ GetManifestResourceInternal(QCallAssembly assembly, string name, out int size, ObjectHandleOnStack module)
        {
            var runtimeAssembly = assembly.QCallAssemblyHandleToRuntimeType().As<RuntimeAssembly_Partial>();
            var manifest = runtimeAssembly._model.Manifests?.ArrayFirstOrDefault(a => a.Name == name);
            if (manifest?.Data != null)
            {
                byte[] bytes;
                //= (NetJs.Script.IsArray(manifest.Data) || NetJs.Script.InstanceOf(manifest.Data, typeof(Array))) ?
                //    manifest.Data.As<byte[]>() : 
                //    NetJs.Script.ArrayFrom(Window.Uint8Array.fromBase64(manifest.Data));
                if ((NetJs.Script.IsArray(manifest.Data) || NetJs.Script.InstanceOf(manifest.Data, typeof(Array))))
                {
                    bytes = manifest.Data.As<byte[]>();
                }
                else
                {
                    bytes = NetJs.Script.ArrayFrom(Window.Uint8Array.fromBase64(manifest.Data));
                    Array.AddMetadata(bytes, typeof(byte));
                }
                //Convert.FromBase64String(manifest.Data);
                //we dont want to keep converting from base64 to byte[], cache by replacinf the original string
                manifest.Data = bytes.As<string>();
                size = bytes.Length;
                module.GetObjectHandleOnStack<RuntimeModule_Partial>() = runtimeAssembly._module;
                return RuntimeHelpers.CreateArrayReferenceT(bytes).As<IntPtr>();
            }
            size = 0;
            return IntPtr.Zero;
        }

        [NetJs.MemberReplace]
        private static void GetManifestModuleInternal(QCallAssembly assembly, ObjectHandleOnStack res)
        {
            var runtimeAssembly = assembly.QCallAssemblyHandleToRuntimeType().As<RuntimeAssembly_Partial>();
            res.GetObjectHandleOnStack<RuntimeModule_Partial>() = runtimeAssembly._module;
        }

        [NetJs.MemberReplace]
        private static void GetModulesInternal(QCallAssembly assembly, ObjectHandleOnStack res)
        {
            var runtimeAssembly = assembly.QCallAssemblyHandleToRuntimeType().As<RuntimeAssembly_Partial>();
            res.GetObjectHandleOnStack<RuntimeModule_Partial[]>() = [runtimeAssembly._module];
        }

        [NetJs.MemberReplace]
        private static extern IntPtr InternalGetReferencedAssemblies(Assembly assembly);

        [NetJs.MemberReplace(nameof(RuntimeAssembly.GetReferencedAssemblies))]
        internal static AssemblyName[] GetReferencedAssembliesOverride(Assembly assembly)
        {
            var runtimeAssembly = assembly.As<RuntimeAssembly_Partial>();
            return runtimeAssembly._model.ReferencedAssembliesHandle.Map(h => AppDomain.GetAssemblyName(h.As<uint>())).Filter(h => h != null).Map(n => new AssemblyName(n!)).AsNetArray();
        }

        [NetJs.MemberReplace]
        private static unsafe bool InternalTryGetRawMetadata(QCallAssembly assembly, out byte* blob, out int length)
        {
            throw new NotSupportedException();
        }

        //Not supporting satellite assemblies, return the original assembly for any culture, version or throwOnFileNotFound
        [NetJs.MemberReplace]
        internal static Assembly InternalGetSatelliteAssembly(Assembly assembly, CultureInfo culture, Version? version, bool throwOnFileNotFound)
        {
            return assembly;
        }
    }
}
