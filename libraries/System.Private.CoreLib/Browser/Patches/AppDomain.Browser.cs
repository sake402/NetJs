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


        internal static SimpleDictionary<AssemblyModel> GlobalMetadataRegistry;
        internal static SimpleDictionary<RuntimeAssembly> GlobalAssemblyRegistry;
        internal static SimpleDictionary<RuntimeType> GlobalTypeRegistry;
        internal static TypePrototype[] GenericTypeParameters;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        [Name("$initd")]
        static void EnsureInitDataStructure()
        {
            if (NetJs.Script.IsUndefinedOrNull(GlobalMetadataRegistry))
            {
                GlobalMetadataRegistry = new SimpleDictionary<AssemblyModel>();
                GlobalAssemblyRegistry = new SimpleDictionary<RuntimeAssembly>();
                GlobalTypeRegistry = new SimpleDictionary<RuntimeType>();
                //GlobalPrototypeRegistry = Script.Write<SimpleDictionary<TypePrototypeRegistrar>>("window.dotnetJs");
                GlobalPrototypeRegistry = Script.Write<SimpleDictionary<Union<TypePrototype, TypePrototypeProvider>>>($"window.{Constants.ProjectName}");
            }
        }
        [Name(Constants.AppDomainInitialize)]
        static void Initialize(RuntimeAssembly coreAssembly)
        {
            EnsureInitDataStructure();
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
                        retryBootTypes.Push(runtimeType);
                    }
                }
            }
            if (retryBootTypes.Length > 0)
            {
                coreAssembly.As<RuntimeAssembly_Partial>().onCompleted.Push(() =>
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
            GenericTypeParameters = Script.CreateArrayFromValues(
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T1"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T2"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T3"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T4"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T5"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T6"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T7"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T8"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T9"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T10"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T11"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T12"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T13"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T14"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T15"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T16"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T17"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T18"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T19"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T20"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T21"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T22"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T23"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T24"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T25"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T26"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T27"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T28"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T29"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T30"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T31"),
                Script.Write<TypePrototype>($"{Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.$T32"));

            //Script.Write($"{Constants.GlobalName}.{Constants.AssemblyRegistryName} = this.{Constants.AssemblyRegistryName}");
            //Script.Write($"{Constants.GlobalName}.{Constants.AssemblyMetadataRegistryName} = this.{Constants.AssemblyMetadataRegistryName}");
            //Redirect subsequent BootDefine($bt) to DefineType($cls), lest we end up with an inner whose runtime type that is not initialized
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

            Script.Write($"{Constants.GlobalName}.castPtr2Address = {Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.{nameof(InteropUtility)}.{nameof(InteropUtility.castPtr2Address)}");
            Script.Write($"{Constants.GlobalName}.castAddress2Ptr = {Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.{nameof(InteropUtility)}.{nameof(InteropUtility.castAddress2Ptr)}");
            Script.Write($"{Constants.GlobalName}.virtualAddressOffset = {Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.{nameof(InteropUtility)}.{nameof(InteropUtility.virtualAddressOffset)}");
            Script.Write($"{Constants.GlobalName}.{Constants.IntegerChecked} = {Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.{nameof(InteropUtility)}.{nameof(InteropUtility.IntegerChecked)}");
            Script.Write($"{Constants.GlobalName}.{Constants.ToArray} = {Constants.GlobalName}.{Constants.SystemPrivateCoreLib}.{nameof(InteropUtility)}.{nameof(InteropUtility.ToArray)}");
            //Script.Write($"$.{Constants.AssemblyStubName} = $.System.AppDomain.{Constants.AssemblyStubName}");
        }

        //[Name(Constants.AssemblyMetadataRegistryName)]
        //public static void ReflectionData(string assemblyName, AssemblyModel assemblyMetadata)
        //{
        //    GlobalMetadataRegistry[assemblyMetadata.Handle.As<uint>().GetAssemblyHandle()] = assemblyMetadata;
        //    GlobalMetadataRegistry[assemblyName] = assemblyMetadata;
        //}

        [Name(Constants.AssemblyRegistryName)]
        internal static void CreateAssembly(string assemblyName, AssemblyModel assemblyMetadata, NativeAction<RuntimeAssembly> action)
        {
            EnsureInitDataStructure();
            GlobalMetadataRegistry[assemblyMetadata.Handle.As<uint>().GetAssemblyHandle()] = assemblyMetadata;
            GlobalMetadataRegistry[assemblyName] = assemblyMetadata;
            var assembly = GlobalAssemblyRegistry[assemblyName];
            if (Script.IsUndefinedOrNull(assembly))
            {
                assembly = new RuntimeAssembly_Partial(assemblyMetadata, assemblyName).As<RuntimeAssembly>();
                GlobalAssemblyRegistry[assemblyMetadata.Handle.As<uint>().GetAssemblyHandle()] = assembly;
                GlobalAssemblyRegistry[assemblyName] = assembly;
                //precreate all types in this assembly as a stub
                //if (Script.IsDefined(metadata.Types))
                //{
                //    for (int i = 0; i < metadata.TypeNames.Length;i++)
                //    {
                //        var name = metadata.TypeNames[i];
                //        var adjustedName = name.NativeReplace("<", "$").NativeReplace(",", "$").NativeReplace(">", "$");
                //        assembly.DefineStub(name);
                //    }
                //}
            }
            action(assembly);
            assembly.As<RuntimeAssembly_Partial>().Complete();
        }

        internal static void SetupDefaults(Type type)
        {
            //if (Script.TypeOf(type.Prototype) != "function")
            //{
            //    if (!Script.Write<bool>($"type._prototype.{Constants.IsTypeName}"))
            //    {
            //        bool Is(object value)
            //        {
            //            if (Script.InstanceOf(value, type))
            //                return true;
            //            return false;
            //        }
            //        type.Prototype[Constants.IsTypeName] = Is;
            //    }
            //}
        }

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
            var metadata = GlobalMetadataRegistry[assemblyHandle.GetAssemblyHandle()];
            return metadata?.FullName;
        }

        public static AssemblyModel? GetAssemblyMetadata(uint assemblyHandle)
        {
            var metadata = GlobalMetadataRegistry[assemblyHandle.GetAssemblyHandle()];
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
            var assemblies = GlobalAssemblyRegistry.Values;
            for (int i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                var type = assembly.As<RuntimeAssembly_Partial>().GetTypeInternal(typeName);
                if (type != null)
                    return type;
            }
            if (throwOnError)
                throw new InvalidOperationException($"Cannot find {typeName}");
            return null;
        }

    }
}
