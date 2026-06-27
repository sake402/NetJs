using NetJs;

namespace System.Reflection
{
    [NetJs.ForcePartial(typeof(RuntimePropertyInfo))]
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    internal sealed partial class RuntimePropertyInfo_Partial : ForcedPartialBase<RuntimePropertyInfo>
    {
        //internal PropertyModel _model;
        internal RuntimePropertyInfo_Partial(PropertyModel model)
        {
            THIS._model = model;
            //_model = model;
        }

        [NetJs.MemberReplace]
        internal static void get_property_info(RuntimePropertyInfo prop, ref MonoPropertyInfo info, PInfo req_info)
        {
            var model = prop.As<RuntimePropertyInfo>()._model.As<PropertyModel>();
            if (req_info.HasFlag(PInfo.Name))
                info.name = model.Name;
            if (req_info.HasFlag(PInfo.GetMethod))
            {
                if (NetJs.Script.IsDefined(model.GetMethod))
                {
                    model.GetMethod!.Name = "get_" + model.Name;
                    if (NetJs.Script.IsDefined(model.OutputName))
                        model.GetMethod!.OutputName = "get_" + model.OutputName;
                    model.GetMethod!.DeclaringType = model.DeclaringType;
                    model.GetMethod!.ReturnType = model.PropertyType;
                    model.GetMethod!.Parameters = model.IndexParameters;
                }
                info.get_method = NetJs.Script.IsDefined(model.GetMethod) ? new RuntimeMethodInfo(model.GetMethod!) : null!;
            }
            if (req_info.HasFlag(PInfo.SetMethod))
            {
                if (NetJs.Script.IsDefined(model.SetMethod))
                {
                    model.SetMethod!.Name = "set_" + model.Name;
                    if (NetJs.Script.IsDefined(model.OutputName))
                        model.SetMethod!.OutputName = "set_" + model.OutputName;
                    model.SetMethod!.DeclaringType = model.DeclaringType;
                    //model.SetMethod!.Parameters =[ ..model.IndexParameters, model.PropertyType];
                }
                info.set_method = NetJs.Script.IsDefined(model.SetMethod) ? new RuntimeMethodInfo(model.SetMethod!) : null!;
            }
            if (req_info.HasFlag(PInfo.ReflectedType))
                info.parent = AppDomain.GetType(model.DeclaringType.As<uint>()) ?? throw new InvalidOperationException();
            if (req_info.HasFlag(PInfo.DeclaringType))
                info.declaring_type = AppDomain.GetType(model.DeclaringType.As<uint>()) ?? throw new InvalidOperationException();
            if (req_info.HasFlag(PInfo.DeclaringType))
            {
                PropertyAttributes att = default;
                if (NetJs.Script.IsDefined(model.Flags))
                {
                    if (model.Flags.TypeHasFlag(MemberFlagsModel.HasDefaultValue))
                    {
                        att |= PropertyAttributes.HasDefault;
                    }
                }
                info.attrs = att;
            }
        }

        [NetJs.MemberReplace]
        internal static Type[] GetTypeModifiers(RuntimePropertyInfo prop, bool optional, int genericArgumentPosition = -1)
        {
            return Type.EmptyTypes;
        }

        [NetJs.MemberReplace]
        internal static object get_default_value(RuntimePropertyInfo prop)
        {
            var model = prop.As<RuntimePropertyInfo>()._model.As<PropertyModel>();
            var prototype = AppDomain.GetType(model.PropertyType.As<uint>())!._prototype;
            return prototype![model.Name]!;
        }

        [NetJs.MemberReplace]
        internal static int get_metadata_token(RuntimePropertyInfo monoProperty)
        {
            var model = monoProperty.As<RuntimePropertyInfo>()._model;
            return (int)model.Handle;
        }

        [NetJs.MemberReplace]
        private static PropertyInfo internal_from_handle_type(IntPtr event_handle, IntPtr type_handle)
        {
            return (PropertyInfo)AppDomain.GetMember((uint)event_handle)!;
        }

    }
}
