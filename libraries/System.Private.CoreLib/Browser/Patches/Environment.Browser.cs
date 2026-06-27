using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace System
{
    public static partial class Environment
    {

        [NetJs.MemberReplace(nameof(GetEnvironmentVariableCore))]
        private static unsafe string? GetEnvironmentVariableCoreImpl(string variable)
        {
            Debug.Assert(variable != null);

            if (s_environment == null)
            {
                return null;
                //return Utf8StringMarshaller.ConvertToManaged(Interop.Sys.GetEnv(variable));
            }

            variable = TrimStringOnFirstZero(variable);
            lock (s_environment)
            {
                s_environment.TryGetValue(variable, out string? value);
                return value;
            }
        }

        [NetJs.MemberReplace(nameof(GetSystemEnvironmentVariables))]
        private static unsafe Dictionary<string, string> GetSystemEnvironmentVariablesImpl()
        {
            var results = new Dictionary<string, string>();
            return results;
        }

        [NetJs.MemberReplace(nameof(ExitCode))]
        public static int ExitCodeImpl { get; set; }

        [NetJs.MemberReplace(nameof(GetProcessorCount))]
        internal static int GetProcessorCountImpl()
        {
            return 1;
        }

        [NetJs.MemberReplace(nameof(Exit))]
        public static void ExitImpl(int exitCode)
        {

        }

        [NetJs.MemberReplace(nameof(GetCommandLineArgs))]
        public static string[] GetCommandLineArgsImpl()
        {
            return [];
        }

        [NetJs.MemberReplace(nameof(FailFast))]
        internal static void FailFastImpl(string? message, Exception? exception, string? errorSource)
        {

        }

        public static string Version => "1.0";
    }
}
