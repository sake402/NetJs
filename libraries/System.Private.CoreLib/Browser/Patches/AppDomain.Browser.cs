using NetJs;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace System
{
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    [NetJs.OutputOrder(int.MinValue + 4)]
    public sealed partial class AppDomain
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal static SimpleDictionary<Union<TypePrototype, TypePrototypeProvider>> GlobalPrototypeRegistry;


        internal static SimpleDictionary<AssemblyModel> GlobalAssemblyMetadataRegistry;
        internal static SimpleDictionary<RuntimeAssembly> GlobalAssemblyRegistry;
        internal static SimpleDictionary<RuntimeType> GlobalTypeRegistry;
        internal static TypePrototype[] GenericTypeParameters;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        [Name("$initd")]
        static void EnsureInitDataStructure()
        {
            if (NetJs.Script.IsUndefinedOrNull(GlobalAssemblyMetadataRegistry))
            {
                //GlobalAssemblyRegistry = new SimpleDictionary<RuntimeAssembly>();
                //GlobalTypeRegistry = new SimpleDictionary<RuntimeType>();
                //GlobalPrototypeRegistry = Script.Write<SimpleDictionary<TypePrototypeRegistrar>>("window.dotnetJs");
                GlobalPrototypeRegistry = Script.Write<SimpleDictionary<Union<TypePrototype, TypePrototypeProvider>>>($"window.{Constants.ProjectName}");
                GlobalAssemblyRegistry = Script.Write<SimpleDictionary<RuntimeAssembly>>($"window.{Constants.ProjectName}.$assemblies");
                GlobalTypeRegistry = Script.Write<SimpleDictionary<RuntimeType>>($"window.{Constants.ProjectName}.$types");
                GlobalAssemblyMetadataRegistry = new SimpleDictionary<AssemblyModel>();
            }
        }
        [Name(Constants.AppDomainInitialize)]
        static void Initialize(RuntimeAssembly coreAssembly)
        {
            EnsureInitDataStructure();
            GenericTypeParameters = Script.CreateArrayFromValues(
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T1"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T2"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T3"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T4"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T5"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T6"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T7"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T8"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T9"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T10"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T11"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T12"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T13"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T14"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T15"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T16"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T17"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T18"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T19"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T20"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T21"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T22"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T23"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T24"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T25"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T26"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T27"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T28"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T29"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T30"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T31"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.$T32"));

            //Script.Write($"{Constants.GlobalName}.{Constants.AssemblyRegistryName} = this.{Constants.AssemblyRegistryName}");
            //Script.Write($"{Constants.GlobalName}.{Constants.AssemblyMetadataRegistryName} = this.{Constants.AssemblyMetadataRegistryName}");
            //Redirect subsequent BootDefine($bt) to DefineType($cls), lest we end up with an inner whose runtime type is not initialized
            NativeFunction<string, TypePrototype, TypePrototype?, NativeAction<Union<TypePrototype, TypePrototypeProvider>>?, NetJs.Union<TypePrototype, TypePrototypeProvider>> redirectBootType = (name, prototype, parent, typePrototypeSink) =>
            {
                bool isGenericType = NetJs.Script.TypeOf(prototype).NativeEquals("function") && NetJs.Script.Write<int>("prototype.length") != 0;
                if (isGenericType) //generic type
                {
                    var provider = prototype.As<TypePrototypeProvider>();
                    return coreAssembly.As<RuntimeAssembly_Partial>().DefineType(name, provider, TypeFlagsModel.IsNested, parent, typePrototypeSink);
                }
                //if (prototype.Flags.TypeHasFlag(TypeFlagsModel.IsGenericType))
                //return coreAssembly.As<RuntimeAssembly_Partial>().DefineG(name, (t) => prototype, prototype.Flags, parent, typePrototypeSink);
                //else
                return coreAssembly.As<RuntimeAssembly_Partial>().DefineType(name, (t) => prototype, prototype.Flags, parent, typePrototypeSink);
            };
            Script.Write($"{Constants.GlobalName}.{Constants.AssemblyBootClassName} = {nameof(redirectBootType)}");

            Script.Write($"{Constants.GlobalName}.castPtr2Address = {Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.{Constants.InteropUtilityName}.{Constants.PointerToAddress}");
            Script.Write($"{Constants.GlobalName}.castAddress2Ptr = {Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.{Constants.InteropUtilityName}.{Constants.AddressToPointer}");
            Script.Write($"{Constants.GlobalName}.virtualAddressOffset = {Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.{Constants.InteropUtilityName}.{Constants.VirtualAddressOffset}");
            Script.Write($"{Constants.GlobalName}.{Constants.IntegerCheckedName} = {Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.{Constants.InteropUtilityName}.{Constants.IntegerCheckedName}");
            Script.Write($"{Constants.GlobalName}.{Constants.CreateArrayName} = {Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.{Constants.InteropUtilityName}.{Constants.CreateArrayName}");
            Script.Write($"{Constants.GlobalName}.{Constants.ToArrayName} = {Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.{Constants.InteropUtilityName}.{Constants.ToArrayName}");
            Script.Write($"{Constants.GlobalName}.{Constants.TypeArrayName} = {Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.{Constants.InteropUtilityName}.{Constants.TypeArrayName}");
            Script.Write($"{Constants.GlobalName}.{Constants.TypeRefOrPointerName} = {Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.{Constants.InteropUtilityName}.{Constants.TypeRefOrPointerName}");
            Script.Write($"{Constants.GlobalName}.{Constants.TypePointerName} = {Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.{Constants.InteropUtilityName}.{Constants.TypePointerName}");
            Script.Write($"{Constants.GlobalName}.{Constants.TypeNullableName} = {Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.{Constants.InteropUtilityName}.{Constants.TypeNullableName}");
            Script.Write($"{Constants.GlobalName}.{Constants.GetCoreAssemblyName} = {Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.{Constants.InteropUtilityName}.{Constants.GetCoreAssemblyName}");
            Script.Write($"{Constants.GlobalName}.{Constants.DestructureName} = {Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.{Constants.InteropUtilityName}.{Constants.DestructureName}");
            Script.Write($"{Constants.GlobalName}.{Constants.PrototypeImplementsInterface} = {Constants.GlobalName}.{Constants.SystemPrivateCoreLibSlug}.{Constants.InteropUtilityName}.{Constants.PrototypeImplementsInterface}");
            //Script.Write($"$.{Constants.AssemblyStubName} = $.System.AppDomain.{Constants.AssemblyStubName}");

            //Create the runtime type for all boot types found in GlobalPrototypeRegistry
            TypePrototype[] bootTypes = GlobalPrototypeRegistry["$bts"].As<TypePrototype[]>();
            RuntimeType[] retryBootTypes = [];
            for (int i = 0; i < bootTypes.Length; i++)
            {
                unchecked
                {
                    var prototype = bootTypes[i];
                    //var metadata = prototype.Metadata ?? new TypeModel() { Handle = 0.As<ulong>() };
                    var runtimeType = RuntimeType.Create(coreAssembly, prototype, prototype.MetadataFullName, null);
                    //these are boot types, some of the required dependency may not be available yet when initializing the type
                    //We will retry the failed ones when the coreAssembly build is complete
                    try
                    {
                        runtimeType.Complete();
                    }
                    catch
                    {
                        retryBootTypes.push(runtimeType);
                    }
                }
            }
            if (retryBootTypes.Length > 0)
            {
                coreAssembly.As<RuntimeAssembly_Partial>().onCompleted.push(() =>
                {
                    for (int i = 0; i < retryBootTypes.Length; i++)
                    {
                        unchecked
                        {
                            var runtimeType = retryBootTypes[i];
                            runtimeType.Complete();
                        }
                    }
                });
            }
            NetJs.Script.Delete(GlobalPrototypeRegistry, "$bts");
            //Script.Write($"{Constants.GlobalName}.typesReady = true");
        }

        //[Name(Constants.AssemblyMetadataRegistryName)]
        //public static void ReflectionData(string assemblyName, AssemblyModel assemblyMetadata)
        //{
        //    GlobalMetadataRegistry[assemblyMetadata.Handle.As<uint>().GetAssemblyHandle()] = assemblyMetadata;
        //    GlobalMetadataRegistry[assemblyName] = assemblyMetadata;
        //}

        [Name(Constants.AssemblyRegistryName)]
        internal static void CreateAssembly(string assemblyName, /*string assemblySlug,*/ AssemblyModel assemblyMetadata, NativeAction<RuntimeAssembly> action)
        {
            EnsureInitDataStructure();
            var existing = GlobalAssemblyMetadataRegistry[assemblyName];
            if (NetJs.Script.IsDefined(existing))
            {
                throw null!;
            }
            //existing = GlobalMetadataRegistry[assemblySlug];
            //if (NetJs.Script.IsDefined(existing))
            //{
            //    throw null!;
            //}
            existing = GlobalAssemblyMetadataRegistry[assemblyMetadata.Handle.As<uint>().GetAssemblyHandle()];
            if (NetJs.Script.IsDefined(existing))
            {
                throw null!;
            }
            GlobalAssemblyMetadataRegistry[assemblyMetadata.Handle.As<uint>().GetAssemblyHandle()] = assemblyMetadata;
            GlobalAssemblyMetadataRegistry[assemblyName] = assemblyMetadata;
            //GlobalMetadataRegistry[assemblySlug] = assemblyMetadata;

            var existingA = GlobalAssemblyRegistry[assemblyName];
            if (NetJs.Script.IsDefined(existingA))
            {
                throw null!;
            }
            //existingA = GlobalAssemblyRegistry[assemblySlug];
            //if (NetJs.Script.IsDefined(existingA))
            //{
            //    throw null!;
            //}
            existingA = GlobalAssemblyRegistry[assemblyMetadata.Handle.As<uint>().GetAssemblyHandle()];
            if (NetJs.Script.IsDefined(existingA))
            {
                throw null!;
            }

            var assembly = GlobalAssemblyRegistry[assemblyName];
            if (Script.IsUndefinedOrNull(assembly))
            {
                assembly = new RuntimeAssembly_Partial(assemblyMetadata, assemblyName).As<RuntimeAssembly>();
                GlobalAssemblyRegistry[assemblyMetadata.Handle.As<uint>().GetAssemblyHandle()] = assembly;
                GlobalAssemblyRegistry[assemblyName] = assembly;
                //GlobalAssemblyRegistry[assemblySlug] = assembly;
            }
            action(assembly);
            assembly.As<RuntimeAssembly_Partial>().Complete();
        }

        //internal static void SetupDefaults(Type type)
        //{
        //    //if (Script.TypeOf(type.Prototype) != "function")
        //    //{
        //    //    if (!Script.Write<bool>($"type._prototype.{Constants.IsTypeName}"))
        //    //    {
        //    //        bool Is(object value)
        //    //        {
        //    //            if (Script.InstanceOf(value, type))
        //    //                return true;
        //    //            return false;
        //    //        }
        //    //        type.Prototype[Constants.IsTypeName] = Is;
        //    //    }
        //    //}
        //}

        //public Assembly[] GetAssemblies()
        //{
        //    return _assemblies.Values.Unique();
        //}

        //public static AppDomain CurrentDomain { get; } = new AppDomain();

        //public static string GetTypeName(uint typeHandle)
        //{
        //    var assemblyHandle = typeHandle.GetAssemblyHandle();
        //    var assemblyMetadata = GlobalMetadataRegistry[assemblyHandle];
        //    return assemblyMetadata.TypeNames[typeHandle.GetTypeHandle()];
        //}

        public static string? GetAssemblyName(uint assemblyHandle)
        {
            var metadata = GlobalAssemblyMetadataRegistry[assemblyHandle.GetAssemblyHandle()];
            return metadata?.FullName;
        }

        public static AssemblyModel? GetAssemblyMetadata(uint assemblyHandle)
        {
            var metadata = GlobalAssemblyMetadataRegistry[assemblyHandle.GetAssemblyHandle()];
            return metadata;
        }

        internal static RuntimeAssembly? GetAssembly(uint assemblyHandle)
        {
            var assembly = GlobalAssemblyRegistry[assemblyHandle.GetAssemblyHandle()];
            return assembly;
        }

        internal static RuntimeType? GetType(uint typeHandle)
        {
            var value = AppDomain.GlobalTypeRegistry[typeHandle.GetAssemblyAndTypeHandle()];
            if (Script.IsUndefined(value))
                return null;
            return value;
        }

        internal static MemberInfo? GetMember(uint memberHandle)
        {
            var type = GetType(memberHandle);
            return type?.GetMemberInternal(memberHandle);
        }

        internal static RuntimeType? GetTypeInternal(string? typeName, bool ignoreCase = false, bool throwOnError = false)
        {
            if (typeName == null)
            {
                if (throwOnError)
                    throw new ArgumentNullException(nameof(typeName));
                return null;
            }
            string? assemblyName = null;
            var firstComma = typeName.NativeIndexOf(",");
            if (firstComma >= 0)
            {
                var secondComma = typeName.NativeIndexOf(",", firstComma + 1);
                if (secondComma >= 0)
                {
                    //ignore the version, culture and token
                    assemblyName = typeName.NativeSubstring(firstComma + 1, secondComma - firstComma - 1).NativeTrim();
                }
                else
                {
                    assemblyName = typeName.NativeSubstring(firstComma + 1).NativeTrim();
                }
                typeName = typeName.NativeSubstring(0, firstComma);
            }
            if (assemblyName!.NativeEquals("mscorlib"))
            {
                assemblyName = "System.Private.CoreLib";
            }
            var assemblies = GlobalAssemblyRegistry.Values;
            unchecked
            {
                for (int i = 0; i < assemblies.Length; i++)
                {
                    var assembly = assemblies[i];
                    if (assemblyName is null || assembly.As<RuntimeAssembly_Partial>()._model.FullName!.NativeEquals(assemblyName))
                    {
                        var type = assembly.As<RuntimeAssembly_Partial>().GetTypeInternal(typeName);
                        if (type is not null)
                            return type;
                    }
                }
            }
            if (throwOnError)
                throw new InvalidOperationException($"Cannot find {typeName}");
            return null;
        }

    }
}
