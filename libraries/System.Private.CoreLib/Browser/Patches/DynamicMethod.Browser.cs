using System;
using System.Collections.Generic;
using System.Text;

namespace System.Reflection.Emit
{
    public sealed partial class DynamicMethod
    {
        [NetJs.MemberReplace(nameof(create_dynamic_method))]
        private static void create_dynamic_methodImpl(DynamicMethod m, string name, MethodAttributes attributes, CallingConventions callingConvention)
        {
            throw new NotImplementedException();
        }

    }
}
