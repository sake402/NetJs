using NetJs;
using System;
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
        delegate void OnCompleted();

        internal RuntimeModule_Partial _module;
        internal RuntimeType[] _types = [];
        internal AssemblyModel _model;
        uint _nextTypeHandle = 0x8000;

        public RuntimeAssembly_Partial(AssemblyModel model, string assemblyName)
        {
            NetJs.Script.Write("this._mono_assembly = model.h");
            this._model = model;
            _module = new RuntimeModule_Partial(this);
            if (model.AssemblyFlags.TypeHasFlag(AssemblyFlags.Entry))
                Assembly._entry = this.As<Assembly>();
            //_nextTypeHandle = _model.TypeNames.Length.As<uint>();
        }

        internal static TypeProxyHandler CreateTypeProxy(string fullTypeName)
        {
            TypeProxyHandler CreateProxy()
            {
                var proxyHandler = new TypeProxyHandler(fullTypeName);
                object? proxy = null;
                NetJs.Script.Write("proxy = new Proxy({}, proxyHandler)");
                return proxy.As<TypeProxyHandler>();
            }
            //return JSProxy.Create<TypeProxyHandler>(new TypeProxyHandler(fullTypeName));
            if (fullTypeName.NativeEndsWith(">"))
            {
                TypeProxyHandler? _handler = null;
                NativeFunction<TypeProxyHandler> deferedType = () => _handler ??= CreateProxy();
                return deferedType.As<TypeProxyHandler>();
            }
            else
            {
                return CreateProxy();
            }
        }

        /// <summary>
        /// Define a proxy to a type/prototype not yet created. 
        /// </summary>
        /// <param name="fullTypeName"></param>
        [NetJs.Name(NetJs.Constants.AssemblyTypeProxyName)]
        void TypeProxy(string fullTypeName)
        {
            if (!AppDomain.GlobalPrototypeRegistry.ContainsKey(fullTypeName))
            {
                var proxy = CreateTypeProxy(fullTypeName);
                AppDomain.GlobalPrototypeRegistry.SetNested(fullTypeName.NativeReplaceAll("<", "$").NativeReplaceAll(",", "$").NativeReplaceAll(">", "$"), proxy.As<TypePrototype>());
            }
        }

        internal ulong CreateHandle(string typeName)
        {
            var handle = ((_model.Handle.As<uint>() << ReflectionHandleExtension.AssemblyShift) | (_nextTypeHandle << ReflectionHandleExtension.TypeShift));
            //_model.TypeNames.Push(typeName);
            _nextTypeHandle++;
            return handle.As<ulong>();
        }

        TypeModel GetModel(TypePrototype prototype, TypeFlagsModel flag, TypePrototype? parent = null)
        {
            var localAssemblyTypeName = prototype.FullName;
            if (localAssemblyTypeName.NativeStartsWith("$"))
            {
                var firstDot = localAssemblyTypeName.NativeIndexOf(".");
                localAssemblyTypeName = localAssemblyTypeName.NativeSubstring(firstDot + 1);
            }
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
                //var pth = prototype.FullName.Split('.');
                //var pth = prototype.FullName.NativeSplit(".");
                typeMetadata = new TypeModel
                {
                    Flags = flag,
                    //Name = pth[pth.Length - 1],
                    Handle = CreateHandle(localAssemblyTypeName)
                };
            }
            //A nested class within a generic class should obtain a new type handle different from the type inside the generic type definition
            var isNestedClass = flag.TypeHasFlag(TypeFlagsModel.IsNested);
            if (isNestedClass && NetJs.Script.IsDefined(parent) && NetJs.Script.IsDefined(parent!.Arguments))
            {
                typeMetadata!.Handle = CreateHandle(localAssemblyTypeName);
            }
            return typeMetadata!;
        }

        string GetJsName(string fullTypeName)
        {
            return fullTypeName.NativeReplaceAll("<", "$").NativeReplaceAll(",", "$").NativeReplaceAll(">", "$");
        }

        [NetJs.Name(NetJs.Constants.AssemblyStructName)]
        NetJs.Union<TypePrototype, TypePrototypeProvider> DefineStruct(string fullTypeName, TypePrototypeProvider provider)
        {
            return DefineType(fullTypeName, provider, TypeFlagsModel.IsValueType);
        }

        [NetJs.Name(NetJs.Constants.AssemblyNestedStructName)]
        NetJs.Union<TypePrototype, TypePrototypeProvider> DefineNestedStruct(string fullTypeName, TypePrototypeProvider provider, TypePrototype parent, Action<TypePrototype>? typePrototypeSink = null)
        {
            return DefineType(fullTypeName, provider, TypeFlagsModel.IsValueType | TypeFlagsModel.IsNested, parent, typePrototypeSink);
        }

        [NetJs.Name(NetJs.Constants.AssemblyNestedClassName)]
        NetJs.Union<TypePrototype, TypePrototypeProvider> DefineNestedClass(string fullTypeName, TypePrototypeProvider provider, TypePrototype parent, Action<TypePrototype>? typePrototypeSink = null)
        {
            return DefineType(fullTypeName, provider, TypeFlagsModel.IsNested, parent, typePrototypeSink);
        }

        [NetJs.Name(NetJs.Constants.AssemblyClassName)]
        NetJs.Union<TypePrototype, TypePrototypeProvider> DefineType(
            string fullTypeName,
            TypePrototypeProvider provider,
            TypeFlagsModel flags,
            TypePrototype? parent = null,
            [NativeDelegate] Action<TypePrototype>? typePrototypeSink = null)
        {
            if (NetJs.Script.IsUndefined(flags))
                flags = TypeFlagsModel.None;
            provider.As<object>()["$fn"] = fullTypeName.As<object>();
            var jsName = GetJsName(fullTypeName);
            //bool isNestedClass = Constants.NestedClassAsNestedStaticObject && typeMetadata!.Flags.TypeHasFlag(TypeFlagsModel.IsNested);
            //if (_isCompleted) //if the assembly was marked completed, any other class defined after that is a nested class
            var isNestedClass = flags.TypeHasFlag(TypeFlagsModel.IsNested);
            //Dont try reading namespace for nested types, it will return the nested static method/property within the containing class anyway, and get recursive
            var existing = !isNestedClass ? AppDomain.GlobalPrototypeRegistry.GetNested(jsName) : null;
            if (NetJs.Script.IsDefined(existing))
            {
                //if we have created a typestub, this is existing as Proxy type with handler TypeProxyHandler, now we have its prototype
#pragma warning disable CS0184 // 'is' expression's given expression is never of the provided type
                //if (!(existing is TypeProxyHandler))
                if (!(NetJs.Script.Write<bool>("existing.$isProxy === true")) && NetJs.Script.TypeOf(existing).NativeNotEquals("function"))
                    return existing!;
#pragma warning restore CS0184 // 'is' expression's given expression is never of the provided type
            }
            
            bool isGenericDefinition = fullTypeName.NativeEndsWith("$") || fullTypeName.NativeEndsWith(">");

            var selfProxy = NetJs.Script.TypeOf(existing).NativeEquals("function") ?
                existing.As<NativeFunction<TypeProxyHandler>>()() :
                existing.As<TypeProxyHandler>() ?? CreateTypeProxy(fullTypeName);
            var genericTypes = AppDomain.GenericTypes;
            TypePrototype prototype = !isGenericDefinition ? provider(selfProxy, null, null) : NetJs.Script.Write<TypePrototype>("provider( ...genericTypes)");
            TypeModel typeMetadata = GetModel(prototype, flags, parent);

            bool isInterface = typeMetadata.Kind == TypeKindModel.Interface;
            bool isInterfaceMixin = isInterface && NetJs.Script.Write<int>("provider.length") >= 2;
            RuntimeType? type = null;
            //If this type depends on itself, its proxy was created before we even run DefineType, otherwize create a new proxy for it,
            //and pass the proxy into the provider so it can be used in the type definition,
            //and later we will update the proxy with the real type and prototype
            if (isInterfaceMixin)
            {
                type = RuntimeType.Create(THIS, provider, typeMetadata, fullTypeName);
            }
            else if (isGenericDefinition)
            {
                type = RuntimeType.Create(THIS, provider, typeMetadata, fullTypeName);
            }
            else
            {
                //Pass the proxy object as this into the provider
                //existing = existing ?? CreateTypeProxy(fullTypeName).As<TypePrototype>();
                //prototype = NetJs.Script.Write<TypePrototype>("provider(selfProxy, null, null)");
                //prototype = provider(selfProxy, null, null);
                type = RuntimeType.Create(THIS, prototype, typeMetadata, fullTypeName);
            }
            //Now that we have the concrete type and some js closure already holds the stub/proxy
            //Supply the real things to the proxy so it can forward it as neccessary
            selfProxy.TargetType = type;
            selfProxy.Prototype = prototype;
            if (NetJs.Script.IsDefined(existing))
            {
                //existing.As<TypeProxyHandler>().TargetType = type;
                //existing.As<TypeProxyHandler>().Prototype = prototype;
                //remove the typeStub just before we insert the real type
                AppDomain.GlobalPrototypeRegistry.RemoveNested(jsName);
            }
            //bool typeCompleted = false;
            //dont try so set inner types, they are managed and readonly static within the containing type
            if (!isNestedClass)
            {
                //Dont initialize type until they are actually accessed
                AppDomain.GlobalPrototypeRegistry.SetNested(jsName, isGenericDefinition ? provider : prototype, onAccess: (mtype) =>
                {
                    if (_isCompleted && !type._isCompleted)
                    {
                        //typeCompleted = true;
                        type.Complete();
                    }
                    return type._isCompleted;
                    //else
                    //{
                    //    onCompleted.Push(() =>
                    //    {
                    //        if (!typeCompleted)
                    //        {
                    //            typeCompleted = true;
                    //            type.Complete();
                    //        }
                    //    });
                    //}
                });
            }
            if (!isInterfaceMixin && !isGenericDefinition)
                AppDomain.SetupDefaults(type);
            if (NetJs.Script.IsDefined(typePrototypeSink))
            {
                typePrototypeSink!(prototype!);
            }
            if (isNestedClass)
            {
                if (prototype != null)
                {
                    prototype.Parent = parent;
                }
                //Initialize nested types immedialty. If we are crrating it, it means we already access it
                RegisterCompletionNotification(type);
            }
            return prototype ?? provider.As<TypePrototype>();
        }

        internal static string InsertGenericNames(string fullTypeName, string[] genericArguments)
        {
            unchecked
            {
                if (!fullTypeName.NativeEndsWith(">"))
                    throw new InvalidOperationException();
                int nArgs = 1;
                int i = fullTypeName.Length - 2;
                while (fullTypeName.NativeCharCodeAt(i) != '<')
                {
                    nArgs++;
                    i--;
                }
                var name = fullTypeName.NativeSplit("<")[0];
                if (nArgs != genericArguments.Length)
                    throw new InvalidOperationException("Number of generic arguments doesnt match");
                var gn = genericArguments.Join(",");
                return name + "<" + gn + ">";
            }
        }

        //static SimpleDictionary<TypePrototype> mixinCache = new SimpleDictionary<TypePrototype>();
        [NetJs.Name("$mix")]
        TypePrototype Mixin(string fullTypeName, TypePrototype[] genericArguments, TypePrototype? mix, ParameterlessTypePrototypeProvider getPrototype)
        {
            unchecked
            {
                string cacheKey;
                string fullNameWithGenericArguments = fullTypeName;
                if (genericArguments.Length > 0)
                {
                    fullNameWithGenericArguments = InsertGenericNames(fullNameWithGenericArguments, genericArguments.Map(m => m?.FullName ?? ""));
                    cacheKey = fullNameWithGenericArguments;
                    if (NetJs.Script.IsDefined(mix))
                    {
                        cacheKey += "+" + mix!.FullName;
                    }
                }
                else
                {
                    cacheKey = fullTypeName;
                    if (NetJs.Script.IsDefined(mix))
                        cacheKey += "+" + mix!.FullName;
                }
                var existingPrototype = AppDomain.GlobalPrototypeRegistry[cacheKey];
                if (NetJs.Script.IsDefined(existingPrototype))
                    return existingPrototype.As<TypePrototype>();
                //If the type we are mixing for depends on itself, we need to pass this into the getPrototype so it can be used in the mixin definition
                var selfProxy = CreateTypeProxy(fullNameWithGenericArguments);
                //var prototype = NetJs.Script.Write<TypePrototype>("getPrototype(selfProxy)");
                var prototype = getPrototype(selfProxy);
                AppDomain.GlobalPrototypeRegistry[cacheKey] = prototype;
                bool IsGenericTypeDefinition(TypePrototype t)
                {
                    //It is very much possible that the t(TypePrototype) we have here is actually a System.Type, if we had created a stub of it that isn't replace yet
                    //But we can be very sure it isn't a generic type
#pragma warning disable CS0184 // 'is' expression's given expression is never of the provided type
                    if (t is Type)
                    {
                        //the only thing the stub has at this point is just its fullName
                        return t.As<Type>().FullName!.NativeEndsWith("<>") || t.As<Type>().FullName!.NativeEndsWith(",>");
                        //return false;
                    }
#pragma warning restore CS0184 // 'is' expression's given expression is never of the provided type
                    return (t.FullName.NativeEquals("") && t.Name.NativeStartsWith("$T")/*Test generic type*/) || t.FullName!.NativeEndsWith("<>") || t.FullName!.NativeEndsWith(",>");
                    //return !t.Type!.IsGenericTypeDefinition;
                }
                //this is a new class prototype, define its System.Type if any of the typArgument is not a genericName
                if (genericArguments.Length > 0 && genericArguments.Some(t => !IsGenericTypeDefinition(t)))
                {
                    var genericType = AppDomain.GlobalTypeRegistry[fullTypeName];
                    var newType = genericType.MakeGenericTypeInternal(genericArguments.Map(a => a.Type.As<RuntimeType>()!), prototype, fullNameWithGenericArguments);
                    selfProxy.TargetType = newType;
                    selfProxy.Prototype = prototype;
                    AppDomain.GlobalTypeRegistry[fullNameWithGenericArguments!] = newType;
                }
                return prototype;
            }
        }

        [NetJs.Name(NetJs.Constants.InterfaceMixin)]
        TypePrototype InterfaceMixin(string fullName, TypePrototype[] mixes, ParameterlessTypePrototypeProvider getPrototype)
        {
            if (mixes.Length != 1)
                throw new InvalidOperationException("Interface mixin must be 1");
            unchecked
            {
                return Mixin(fullName, [], mixes[0], getPrototype);
            }
        }

        [NetJs.Name(NetJs.Constants.GenericInterfaceMixin)]
        TypePrototype GenericInterfaceMixin(string fullName, TypePrototype[] mixes, ParameterlessTypePrototypeProvider getPrototype)
        {
            if (mixes.Length < 2)
                throw new InvalidOperationException("Generic Interface mixin must be at least 2");
            unchecked
            {
                return Mixin(fullName, mixes.Slice(0, mixes.Length - 1).As<TypePrototype[]>(), mixes[mixes.Length - 1], getPrototype);
            }
        }

        [NetJs.Name(NetJs.Constants.GenericType)]
        TypePrototype GenericType(string fullName, TypePrototype[] genericArgs, ParameterlessTypePrototypeProvider getPrototype)
        {
            return Mixin(fullName, genericArgs, null, getPrototype);
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

        bool _isCompleted;
        OnCompleted[] onCompleted = [];

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
            res.GetObjectHandleOnStack<string[]?>() = names;
        }

        [NetJs.MemberReplace]
        private static void GetExportedTypes(QCallAssembly assembly_h, ObjectHandleOnStack res)
        {
            var assembly = assembly_h.QCallAssemblyHandleToRuntimeType().As<RuntimeAssembly_Partial>();
            res.GetObjectHandleOnStack<Type[]?>() = assembly._types.Filter(e => e._model.Flags.TypeHasFlag(TypeFlagsModel.IsPublic));
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
                return RuntimeHelpers.CreateArrayReference(bytes).As<IntPtr>();
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
            return runtimeAssembly._model.ReferencedAssembliesHandle.Map(h => AppDomain.GetAssemblyName(h.As<uint>())).Filter(h => h != null).Map(n => new AssemblyName(n!));
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
