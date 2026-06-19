using System;
using System.Collections.Generic;
using System.Text;


namespace System.Reflection
{
}
namespace System.Reflection
{
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    public partial class MethodBase
    {
        internal MethodBase(MethodModel model) : base(model)
        {
        }

        [NetJs.MemberReplace(nameof(GetCurrentMethod))]
        public static MethodBase? GetCurrentMethodImpl()
        {
            return null;
        }
    }
}
