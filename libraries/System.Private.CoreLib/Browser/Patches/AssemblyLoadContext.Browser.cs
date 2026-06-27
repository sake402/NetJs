using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace System.Runtime.Loader
{
    public partial class AssemblyLoadContext
    {
        [NetJs.MemberReplace(nameof(PrepareForAssemblyLoadContextRelease))]
        private static void PrepareForAssemblyLoadContextReleaseImpl(IntPtr nativeAssemblyLoadContext, IntPtr assemblyLoadContextStrong)
        {

        }

        [NetJs.MemberReplace(nameof(GetLoadContextForAssembly))]
        private static IntPtr GetLoadContextForAssemblyImpl(RuntimeAssembly rtAsm)
        {
            return (IntPtr)rtAsm.As<RuntimeAssembly_Partial>()._model.Handle;
        }

        [NetJs.MemberReplace(nameof(InternalLoadFile))]
        private static Assembly InternalLoadFileImpl(IntPtr nativeAssemblyLoadContext, string? assemblyFile, ref StackCrawlMark stackMark)
        {
            throw new NotImplementedException();
        }

        [NetJs.MemberReplace(nameof(InternalInitializeNativeALC))]
        private static IntPtr InternalInitializeNativeALCImpl(IntPtr thisHandlePtr, IntPtr name, bool representsTPALoadContext, bool isCollectible)
        {
            return thisHandlePtr;
        }

        [NetJs.MemberReplace(nameof(InternalLoadFromStream))]
        private static Assembly InternalLoadFromStreamImpl(IntPtr nativeAssemblyLoadContext, IntPtr assm, int assmLength, IntPtr symbols, int symbolsLength)
        {
            throw new NotImplementedException();
        }

        [NetJs.MemberReplace(nameof(InternalGetLoadedAssemblies))]
        private static Assembly[] InternalGetLoadedAssembliesImpl()
        {
            return AppDomain.GlobalAssemblyRegistry.Values;
        }
    }
}
