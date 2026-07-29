using System.Runtime.InteropServices;

namespace System.Reflection
{
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    public abstract partial class FieldInfo
    {
        [NetJs.MemberReplace(nameof(get_marshal_info))]
        private MarshalAsAttribute get_marshal_infoImpl()
        {
            return null!;
        }
    }
}
