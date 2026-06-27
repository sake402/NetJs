using NetJs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Reflection
{
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    public partial class Assembly
    {
        internal static Assembly? _entry;

        [NetJs.MemberReplace(nameof(GetExecutingAssembly) + "(ref StackCrawlMark)")]
        [NetJs.Template("$asm")]
        public extern static Assembly GetExecutingAssemblyImpl();
        
        [NetJs.MemberReplace(nameof(GetCallingAssembly))]
        internal static RuntimeAssembly GetCallingAssemblyImpl(ref StackCrawlMark stackMark)
        {
            throw new NotImplementedException();
        }

        [NetJs.MemberReplace(nameof(GetEntryAssemblyNative))]
        internal static Assembly GetEntryAssemblyNativeImpl()
        {
            return _entry!;
        }


        [NetJs.MemberReplace(nameof(InternalLoad))]
        internal static Assembly InternalLoadImpl(string assemblyName, ref StackCrawlMark stackMark, IntPtr ptrLoadContextBinder)
        {
            throw new NotImplementedException();
        }

        [NetJs.MemberReplace(nameof(InternalGetType))]
        internal Type? InternalGetTypeImpl(Module? module, string name, bool throwOnError, bool ignoreCase)
        {
            if (module != null)
            {
                var type = module.Assembly.As<RuntimeAssembly_Partial>().GetTypeInternal(name, ignoreCase);
                if (type == null && throwOnError)
                    throw new InvalidOperationException("Not found");
                return type;
            }
            else
            {
                return AppDomain.GetTypeInternal(name, throwOnError, ignoreCase);
            }
        }
    }
}
