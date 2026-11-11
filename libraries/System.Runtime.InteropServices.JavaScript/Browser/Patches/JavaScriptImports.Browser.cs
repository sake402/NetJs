using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static unsafe partial class JavaScriptImports
    {
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(HasProperty))]
        [NetJs.Template("{propertyName} in {self}")]
        public static extern bool HasPropertyImpl(JSObject self, string propertyName);
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(GetTypeOfProperty))]
        [NetJs.Template("typeof {self}[{propertyName}]")]
        public static extern string GetTypeOfPropertyImpl(JSObject self, string propertyName);
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(GetPropertyAsBoolean))]
        [NetJs.Template("{self}[{propertyName}]")]
        public static extern bool GetPropertyAsBooleanImpl(JSObject self, string propertyName);
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(GetPropertyAsInt32))]
        [NetJs.Template("{self}[{propertyName}]")]
        public static extern int GetPropertyAsInt32Impl(JSObject self, string propertyName);
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(GetPropertyAsDouble))]
        [NetJs.Template("{self}[{propertyName}]")]
        public static extern double GetPropertyAsDoubleImpl(JSObject self, string propertyName);
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(GetPropertyAsString))]
        [NetJs.Template("{self}[{propertyName}]")]
        public static extern string GetPropertyAsStringImpl(JSObject self, string propertyName);
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(GetPropertyAsJSObject))]
        [NetJs.Template("{self}[{propertyName}]")]
        public static extern JSObject GetPropertyAsJSObjectImpl(JSObject self, string propertyName);
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(GetPropertyAsByteArray))]
        [NetJs.Template("{self}[{propertyName}]")]
        public static extern byte[] GetPropertyAsByteArrayImpl(JSObject self, string propertyName);

        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(SetPropertyBool))]
        [NetJs.Template("{self}[{propertyName}] = {value}")]
        public static extern void SetPropertyBoolImpl(JSObject self, string propertyName, bool value);
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(SetPropertyInt))]
        [NetJs.Template("{self}[{propertyName}] = {value}")]
        public static extern void SetPropertyIntImpl(JSObject self, string propertyName, int value);
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(SetPropertyDouble))]
        [NetJs.Template("{self}[{propertyName}] = {value}")]
        public static extern void SetPropertyDoubleImpl(JSObject self, string propertyName, double value);
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(SetPropertyString))]
        [NetJs.Template("{self}[{propertyName}] = {value}")]
        public static extern void SetPropertyStringImpl(JSObject self, string propertyName, string value);
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(SetPropertyJSObject))]
        [NetJs.Template("{self}[{propertyName}] = {value}")]
        public static extern void SetPropertyJSObjectImpl(JSObject self, string propertyName, JSObject value);
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(SetPropertyBytes))]
        [NetJs.Template("{self}[{propertyName}] = {value}")]
        public static extern void SetPropertyBytesImpl(JSObject self, string propertyName, byte[] value);

        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(GetGlobalThis))]
        [NetJs.Template("globalThis")]
        public static extern JSObject GetGlobalThisImpl();
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(GetDotnetInstance))]
        [NetJs.Template("{}")]
        public static extern JSObject GetDotnetInstanceImpl();
        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(DynamicImport))]
        [NetJs.Template("{}")]
        public static extern System.Threading.Tasks.Task<JSObject> DynamicImportImpl(string moduleName, string moduleUrl);

        [NetJs.NoJSImport]
        [NetJs.MemberReplace(nameof(BindCSFunction))]
        [NetJs.Template("")]
        public static extern void BindCSFunctionImpl(IntPtr monoMethod, string assemblyName, string namespaceName, string shortClassName, string methodName, int signatureHash, IntPtr signature);

        [NetJs.NoJSImport]
        [NetJs.MemberReplace("Log")]
        [NetJs.Template("globalThis.console.log")]
        public static extern void LogImpl(string message);
    }
}
