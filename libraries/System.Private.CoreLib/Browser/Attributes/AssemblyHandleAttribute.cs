using System;

namespace NetJs
{
    [External]
    [NonScriptable]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class AssemblyHandleAttribute : Attribute
    {
        //Handle 1 is reserved for runtime
        public const int RuntimeHandle = 1;
        public const int SystemPrivateCoreLib = 1;
        public const int MicrosoftExtensionsPrimitives = 2;
        public const int MicrosoftExtensionsDependencyInjectionAbstractions = 3;
        public const int MicrosoftExtensionsDependencyInjection = 4;
        public const int SystemCollections = 5;
        public const int SystemCollectionsConcurrent = 6;
        public const int SystemCollectionsImmutable = 34;
        public const int SystemCollectionsNonGeneric = 35;
        public const int SystemCollectionsSpecialized = 36;
        public const int SystemComponentModel = 37;
        public const int SystemComponentModelAnnotations = 38;
        public const int SystemComponentModelPrimitives = 39;
        public const int SystemComponentModelTypeConverter = 40;
        public const int SystemConsole = 41;
        public const int SystemDataCommon = 43;
        public const int SystemDiagnosticsDiagnosticSource = 44;
        public const int SystemLinq = 7;
        public const int MicrosoftAspnetCoreComponents = 8;
        public const int MicrosoftAspnetCoreComponentsWeb = 9;
        public const int MicrosoftAspnetCoreComponentsForms = 10;
        public const int MicrosoftAspnetCoreComponentsQuickGrid = 11;
        public const int MicrosoftAspnetCoreComponentsWebAssembly = 12;
        public const int MicrosoftExtensionsConfiguration = 13;
        public const int MicrosoftExtensionsConfigurationAbstraction = 14;
        public const int MicrosoftExtensionsConfigurationBinder = 15;
        public const int MicrosoftExtensionsConfigurationFileExtensions = 16;
        public const int MicrosoftExtensionsConfigurationJson = 17;
        public const int MicrosoftExtensionsOptions = 18;
        public const int MicrosoftExtensionsOptionsConfigurationExtensions = 19;
        public const int MicrosoftExtensionsLogging = 20;
        public const int MicrosoftExtensionsLoggingAbstractions = 21;
        public const int MicrosoftBclAsyncInterfaces = 22;
        public const int MicrosoftCSharp = 23;
        public const int MicrosoftExtensionsDiagnostics = 24;
        public const int MicrosoftExtensionsDiagnosticsAbstractions = 25;
        public const int MicrosoftExtensionsFileProvidersAbstractions = 26;
        public const int MicrosoftExtensionsFileProvidersPhysical = 27;
        public const int MicrosoftExtensionsFileSystemGlobbing = 28;
        public const int MicrosoftExtensionsValidation = 29;
        public const int MicrosoftJSInterop = 30;
        public const int MicrosoftJSInteropWebAssembly = 31;
        public const int MicrosoftWin32Primitives = 32;
        public const int MicrosoftWin32Registry = 33;
        public const int SystemNetHttp = 45;
        public AssemblyHandleAttribute(uint handle) { }
    }
}