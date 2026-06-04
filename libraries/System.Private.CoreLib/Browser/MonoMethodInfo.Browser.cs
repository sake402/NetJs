using System.Runtime.InteropServices;

namespace System.Reflection
{
    [NetJs.ForcePartial(typeof(MonoMethodInfo))]
    //[NetJs.Boot]
    //[NetJs.Reflectable(false)]
    internal partial struct MonoMethodInfo_Partial
    {
        [NetJs.MemberReplace]
        private static int get_method_attributes(IntPtr handle)
        {
            MethodAttributes attrs = 0;
            var method = (MethodBase)AppDomain.GetMember(handle.As<uint>())!;
            if (method._model.Flags.TypeHasFlag(MemberFlagsModel.IsPublic))
            {
                attrs |= MethodAttributes.Public;
            }
            if (method._model.Flags.TypeHasFlag(MemberFlagsModel.IsPrivate))
            {
                attrs |= MethodAttributes.Private;
            }
            if (method._model.Flags.TypeHasFlag(MemberFlagsModel.IsStatic))
            {
                attrs |= MethodAttributes.Static;
            }
            if (method._model.Flags.TypeHasFlag(MemberFlagsModel.IsVirtual))
            {
                attrs |= MethodAttributes.Virtual;
            }
            return attrs.As<int>();
        }

        [NetJs.MemberReplace]
        private static void get_method_info(IntPtr handle, out MonoMethodInfo info)
        {
            MonoMethodInfo minfo = default!;
            var method = (MethodBase)AppDomain.GetMember(handle.As<uint>())!;
            var dt = AppDomain.GetType(method._model.DeclaringType.As<uint>());
            var rt = (NetJs.Script.IsDefined(method._model.As<MethodModel>().ReturnType) ?
                AppDomain.GetType(method._model.As<MethodModel>().ReturnType!.Value.As<uint>()) :
                null) ?? typeof(void);
            NetJs.Script.Write("minfo.parent = dt");
            NetJs.Script.Write("minfo.ret = rt");
            if (NetJs.Script.IsDefined(method._model.Flags))
            {
                if (method._model.Flags.TypeHasFlag(MemberFlagsModel.IsPublic))
                {
                    minfo.attrs |= MethodAttributes.Public;
                }
                if (method._model.Flags.TypeHasFlag(MemberFlagsModel.IsPrivate))
                {
                    minfo.attrs |= MethodAttributes.Private;
                }
                if (method._model.Flags.TypeHasFlag(MemberFlagsModel.IsStatic))
                {
                    minfo.attrs |= MethodAttributes.Static;
                }
                if (method._model.Flags.TypeHasFlag(MemberFlagsModel.IsVirtual))
                {
                    minfo.attrs |= MethodAttributes.Virtual;
                }
            }
            info = minfo;
        }

        [NetJs.MemberReplace]
        private static ParameterInfo[] get_parameter_info(IntPtr handle, MemberInfo member)
        {
            var method = member.As<RuntimeMethodInfo>();
            var parameters = method._model.As<MethodModel>().Parameters ?? null;
            return parameters?.Map((p, i, all) => new RuntimeParameterInfo_Partial(p, AppDomain.GetType(p.ParameterType.As<uint>()) ?? throw new InvalidOperationException(), method, i).As<RuntimeParameterInfo>())
                .AsNetArray() ??
                Array.Empty<ParameterInfo>();
        }

        [NetJs.MemberReplace]
        private static MarshalAsAttribute get_retval_marshal(IntPtr handle)
        {
            return null!;
        }
    }
}
