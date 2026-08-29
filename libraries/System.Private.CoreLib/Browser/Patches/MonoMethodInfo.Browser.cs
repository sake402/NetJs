using NetJs;
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
            var method = (MethodBase)AppDomain.GetMember(handle.As<uint>())!;
            if (method._monoInfo != null)
            {
                info = method._monoInfo.Value;
                return;
            }
            MonoMethodInfo minfo = default!;
            var dt = AppDomain.GetType(method._model.DeclaringType.As<uint>());
            var returnTypeHandle = method._model.As<MethodModel>().ReturnType;
            if (NetJs.Script.TypeOf(returnTypeHandle).NativeEquals("function"))
            {
                var args = method.As<RuntimeMethodInfo>()._typeArguments!.Map(t => t.As<RuntimeType>()._prototype);
                returnTypeHandle = NetJs.Script.Write<uint>("returnTypeHandle( ...args)");
            }
            var rt = (NetJs.Script.IsDefined(returnTypeHandle) ? AppDomain.GetType(returnTypeHandle.As<uint>()) : null) ?? typeof(void);
            NetJs.Script.Write("minfo.{nameof(System.Reflection.MonoMethodInfo.parent)} = dt");
            NetJs.Script.Write("minfo.{nameof(System.Reflection.MonoMethodInfo.ret)} = rt");
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
            method._monoInfo = minfo;
            info = minfo;
        }

        [NetJs.MemberReplace]
        private static ParameterInfo[] get_parameter_info(IntPtr handle, MemberInfo member)
        {
            var method = member.As<RuntimeMethodInfo>();
            if (method._parameters != null)
                return method._parameters;
            var parameters = method._model.As<MethodModel>().Parameters ?? null;
            var infos = parameters?.Map((p, i, all) =>
            {
                var parameterTypeHandle = p.ParameterType.ResolveHandle(method._typeArguments);
                return new RuntimeParameterInfo_Partial(p, AppDomain.GetType(parameterTypeHandle.As<uint>()) ?? throw new InvalidOperationException(), method, i).As<RuntimeParameterInfo>();
            }).AsNetArray() ??
                Array.Empty<ParameterInfo>();
            return method._parameters = infos;
        }

        [NetJs.MemberReplace]
        private static MarshalAsAttribute get_retval_marshal(IntPtr handle)
        {
            return null!;
        }
    }
}
