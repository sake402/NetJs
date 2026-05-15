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
        internal static SimpleDictionary<AssemblyModel> GlobalMetadataRegistry;
        internal static SimpleDictionary<RuntimeAssembly> GlobalAssemblyRegistry;
        internal static SimpleDictionary<Union<TypePrototype, TypePrototypeProvider>> GlobalPrototypeRegistry;
        internal static SimpleDictionary<RuntimeType> GlobalTypeRegistry;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        [Name(Constants.AppDomainInitialize)]
        static void Initialize()
        {
            GlobalMetadataRegistry = new SimpleDictionary<AssemblyModel>();
            GlobalAssemblyRegistry = new SimpleDictionary<RuntimeAssembly>();
            GlobalTypeRegistry = new SimpleDictionary<RuntimeType>();
            //GlobalPrototypeRegistry = Script.Write<SimpleDictionary<TypePrototypeRegistrar>>("window.dotnetJs");
            GlobalPrototypeRegistry = Script.Write<SimpleDictionary<Union<TypePrototype, TypePrototypeProvider>>>($"window.{Constants.ProjectName}");
            //Script.Write($"{Constants.GlobalName}.typesReady = true");
            Script.Write($"{Constants.GlobalName}.{Constants.AssemblyRegistryName} = this.{Constants.AssemblyRegistryName}");
            Script.Write($"{Constants.GlobalName}.{Constants.AssemblyMetadataRegistryName} = this.{Constants.AssemblyMetadataRegistryName}");
            Script.Write($"{Constants.GlobalName}.castPtr2Address = {Constants.GlobalName}.$spc.{nameof(InteropUtility)}.{nameof(InteropUtility.castPtr2Address)}");
            Script.Write($"{Constants.GlobalName}.castAddress2Ptr = {Constants.GlobalName}.$spc.{nameof(InteropUtility)}.{nameof(InteropUtility.castAddress2Ptr)}");
            Script.Write($"{Constants.GlobalName}.virtualAddressOffset = {Constants.GlobalName}.$spc.{nameof(InteropUtility)}.{nameof(InteropUtility.virtualAddressOffset)}");
            //Script.Write($"$.{Constants.AssemblyStubName} = $.System.AppDomain.{Constants.AssemblyStubName}");
        }
        
        [Name(Constants.AssemblyMetadataRegistryName)]
        public static void ReflectionData(string assemblyName, AssemblyModel assemblyMetadata)
        {
            GlobalMetadataRegistry[assemblyMetadata.Handle.As<uint>().GetAssemblyHandle()] = assemblyMetadata;
            GlobalMetadataRegistry[assemblyName] = assemblyMetadata;
        }

        [Name(Constants.AssemblyRegistryName)]
        internal static void CreateAssembly(string assemblyName, Action<RuntimeAssembly> action)
        {
            var assembly = GlobalAssemblyRegistry[assemblyName];
            if (Script.IsUndefinedOrNull(assembly))
            {
                var metadata = GlobalMetadataRegistry[assemblyName];
                assembly = new RuntimeAssembly_Partial(metadata, assemblyName).As<RuntimeAssembly>();
                GlobalAssemblyRegistry[metadata.Handle.As<uint>().GetAssemblyHandle()] = assembly;
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
            NetJs.Script.Write("action(assembly)");
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

        public static string GetTypeName(uint typeHandle)
        {
            var assemblyHandle = typeHandle.GetAssemblyHandle();
            var assemblyMetadata = GlobalMetadataRegistry[assemblyHandle];
            return assemblyMetadata.TypeNames[typeHandle.GetTypeHandle()];
        }

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
