namespace NetJs
{
    public enum InterfaceMixinMode
    {
        None,
        ProxyLookup,
        MixinMethod
    }
    [External]
    public static class Constants
    {
        [InlineConst]
        public const bool ExportClassName = true;
        //String in c# is a reference type, but we may box it in this port anyway because we use native js string
        //Afterall, the only way we can make its object member accessible is by boxing
        [InlineConst]
        public const bool HandleStringAsValueTypePrimitive = false;
        [InlineConst]
        public const bool DisableBootClass = false;
        [InlineConst]
        public const bool NestedClassAsNestedStaticObject = true;
        [InlineConst]
        public const bool StructFieldAlwaysLayout = true;
        [InlineConst]
        public const bool CompatibleExtensionPropertyGetSetMethod = true;
        [InlineConst]
        public const bool ComplexSwitchUseIfElse = true;
        //[InlineConst]
        //public const bool MinifyNamespace = true;
        //[InlineConst]
        //public const bool MinifyTypeName = true;
        //[InlineConst]
        //public const bool MinifyFieldName = true;
        //[InlineConst]
        //public const bool MinifyPropertyName = true;
        //[InlineConst]
        //public const bool MinifyMethodName = true;
        //[InlineConst]
        //public const bool MinifyEventName = true;
        [InlineConst]
        public const bool ResourceNamesInlined = true;
        [InlineConst]
        public const bool RewriteConditionalAccessExpressions = true;
        //[InlineConst]
        //public const bool GotoLabelsUseIfCondition = false;
        [InlineConst]
        public const InterfaceMixinMode UseInterfaceMixin = InterfaceMixinMode.None;
        [InlineConst]
        public const string ProjectName = "NetJs";
        [InlineConst]
        public const string GlobalName = "$";
        [InlineConst]
        public const string SystemPrivateCoreLibSlug = "$$";
        [InlineConst]
        public const string AssemblyRegistryName = "$asm";
        [InlineConst]
        public const string AssemblyDefineClassName = "$cls";
        [InlineConst]
        public const string AssemblyNestedClassName = "$ncls";
        [InlineConst]
        public const string AssemblyStructName = "$str";
        [InlineConst]
        public const string AssemblyNestedStructName = "$nstr";
        [InlineConst]
        public const string AssemblyBootClassName = "$bt";
        [InlineConst]
        public const string InterfaceMixin = "$mx";
        [InlineConst]
        public const string GenericInterfaceMixin = "$gm";
        [InlineConst]
        public const string GenericType = "$gt";
        [InlineConst]
        public const bool GenericMethodAsFactory = false;
        [InlineConst]
        public const string IfNotNull = "$ifnn";
        [InlineConst]
        public const string IfNotNullParameterName = "$t";
        [InlineConst]
        public const string AssemblyTypeProxyName = "$typeProxy";
        [InlineConst]
        public const string AppDomainInitialize = "$init";
        [InlineConst]
        public const string RunModuleInitializersName = "$minit";
        //[InlineConst]
        //public const string AssemblyMetadataRegistryName = "$meta";
        [InlineConst]
        public const string NamespaceRegistryName = "$ns";
        [InlineConst]
        public const string CastName = "$cast";
        [InlineConst]
        public const string TryCastName = "$tryCast";
        [InlineConst]
        public const string DefaultTypeName = "$default";
        [InlineConst]
        public const string CreateArrayName = "$array";
        [InlineConst]
        public const string BoxName = "$box";
        [InlineConst]
        public const string UnboxName = "$unbox";
        [InlineConst]
        public const string IsTypeName = "$is";
        [InlineConst]
        public const string With = "$with";
        [InlineConst]
        public const string TypeIsAssignableName = "$tis";
        [InlineConst]
        public const string DefaultConstructorName = "$ctor";
        [InlineConst]
        public const string StaticConstructorName = "$cctor";
        [InlineConst]
        public const string StaticInitializerName = "$sinit";
        [InlineConst]
        public const string ExportMethodName = "$exports";
        [InlineConst]
        public const string EnumMapName = "$map";
        [InlineConst]
        public const string EnumUnderlyingName = "$eut";
        [InlineConst]
        public const string Expression = "$exp";
        [InlineConst]
        public const string TypeArrayName = "$typeArray";
        [InlineConst]
        public const string TypeRefOrPointerName = "$typeRefOrPointer";
        [InlineConst]
        public const string TypePointerName = "$typePointer";
        [InlineConst]
        public const string TypeNullableName = "$typeNullable";
        [InlineConst]
        public const string TypeOf = "$typeOf";
        [InlineConst]
        public const string FirstOf = "$firstOf";
        [InlineConst]
        public const string SizeOf = "$sizeOf";
        [InlineConst]
        public const string TypePrototypeName = "$prototype";
        [InlineConst]
        public const string ToArrayName = "$toArray";
        [InlineConst]
        public const string ToStringName = "$toString";
        [InlineConst]
        public const string HashCodeKey = "$hashCode";
        [InlineConst]
        public const string GetHashCodeName = "$getHashCode";
        [InlineConst]
        public const string GetCoreAssemblyName = "$getCoreAssembly";
        [InlineConst]
        public const string BootName = "$boot";
        [InlineConst]
        public const string Equal = "$equals";
        [InlineConst]
        public const string CombineDelagate = "$combine";
        [InlineConst]
        public const string RemoveDelagate = "$remove";
        [InlineConst]
        public const string DiscardRefName = "$discardRef";
        [InlineConst]
        public const string FinalizerRegister = "$finalizer";
        [InlineConst]
        public const string TemplateVariablePrefix = "$v$";
        [InlineConst]
        public const string RefCreateName = "$ref";
        [InlineConst]
        public const string RefValueName = "$v";
        [InlineConst]
        public const string LazyVariableValueName = "$v";
        [InlineConst]
        public const string RefClassFullName = "System.Ref<>";
        [InlineConst]
        public const string TupleUnPack = "$tupleUnpack";
        [InlineConst]
        public const string StructFieldsLayoutName = "$fields";
        [InlineConst]
        public const string StaticStructFieldsLayoutName = "$sfields";
        [InlineConst]
        public const string SuperClassAccessName = "$super";
        [InlineConst]
        public const string PrototypeTypeName = "$type";
        [InlineConst]
        public const string PrototypeAssemblyName = "$asmb";
        [InlineConst]
        public const string PrototypeFullName = "$fullName";
        [InlineConst]
        public const string PrototypeMetadataFullName = "$mfullName";
        [InlineConst]
        public const string PrototypeTypeFlags = "$f";
        [InlineConst]
        public const string PrototypeKind = "$k";
        [InlineConst]
        public const string PrototypeKnownType = "$t";
        [InlineConst]
        public const string PrototypeStructSize = "$z";
        //[InlineConst]
        //public const string PrototypeBaseTypeHandle = "$b";
        [InlineConst]
        public const string PrototypeTypeHandle = "$h";
        [InlineConst]
        public const string PrototypeOpenGenericPrototype = "$og";
        [InlineConst]
        public const string PrototypeGenericArgumentCount = "$g";
        [InlineConst]
        public const string PrototypeMetadata = "$md";
        [InlineConst]
        public const string PrototypeBaseType = "$b";
        [InlineConst]
        public const string PrototypeInterfaces = "$i";
        [InlineConst]
        public const string NumericShift = "$nsh";
        [InlineConst]
        public const string Dispatch = "$dsp";
        [InlineConst]
        public const string DestructureName = "$destructure";
        [InlineConst]
        public const string NativeDelagateFunctionName = "$nativeFunction";
        [InlineConst]
        public const string NativeDelagateFunctionTargetName = "$target";
        [InlineConst]
        public const string NativeDelagateAnonymousFunctionModel = "$nfmodel";
        [InlineConst]
        public const string FileScopedTypeNameMangling = "$file";
        [InlineConst]
        public const string Clone = "Clone";
        [InlineConst]
        public const string IsProxy = "$isProxy";
        [InlineConst]
        public const string ProxyHandler = "$proxyHandler";
        [InlineConst]
        public const string ProxyType = "$type";
        [InlineConst]
        public const string IntegerCheckedName = "$checked";
        [InlineConst]
        public const string OutputFolderName = "wwwroot";
        [InlineConst]
        public const string ObjectGetField = "$getField";
        [InlineConst]
        public const string ObjectSetField = "$setField";
        [InlineConst]
        public const string ObjectGetStaticField = "$getSField";
        [InlineConst]
        public const string ObjectSetStaticField = "$setSField";
        [InlineConst]
        public const string InteropUtilityName = "$interop";
        [InlineConst]
        public const string PointerToAddress = "$ptr2Addr";
        [InlineConst]
        public const string AddressToPointer = "$addr2Ptr";
        [InlineConst]
        public const string VirtualAddressOffset = "$vAddrOff";
        [InlineConst]
        public const string RuntimeTypePrototypeName = "$p";
    }
}
