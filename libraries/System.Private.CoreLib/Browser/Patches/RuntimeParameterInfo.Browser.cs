using NetJs;
using System.Collections.Generic;
using System.Text;

namespace System.Reflection
{
    [NetJs.ForcePartial(typeof(RuntimeParameterInfo))]
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    internal partial class RuntimeParameterInfo_Partial : ForcedPartialBase<RuntimeParameterInfo>
    {
        internal ParameterModel _model = new(); //initialize to non null as there are other constructors on RuntimeParameterInfo

        public RuntimeParameterInfo_Partial(ParameterModel model, RuntimeType type, MemberInfo member, int position)
        {
            var nm = model.Name;
            Script.Write("this.{nameof(ParameterInfo.NameImpl)} = nm");
            Script.Write("this.{nameof(ParameterInfo.ClassImpl)} = type");
            Script.Write("this.{nameof(ParameterInfo.PositionImpl)} = position");
            //This.NameImpl = model.Name;
            //This.ClassImpl = type;
            //This.PositionImpl = position;
            ParameterAttributes attrs = ParameterAttributes.None;
            if (NetJs.Script.IsDefined(model.Flags))
            {
                if (model.Flags.HasFlag(ParameterFlagsModel.Out))
                    attrs |= ParameterAttributes.Out;
                if (model.Flags.HasFlag(ParameterFlagsModel.Ref))
                    attrs |= ParameterAttributes.In;
                if (model.Flags.HasFlag(ParameterFlagsModel.Optional))
                    attrs |= ParameterAttributes.Optional;
                //if (model.Flags.HasFlag(ParameterFlagsModel.Params))
                //    attrs|= ParameterAttributes.Params;
            }
            Script.Write("this.{nameof(ParameterInfo.AttrsImpl)} = attrs");
            //Script.Write("this.DefaultValueImpl = null");
            Script.Write("this.{nameof(ParameterInfo.MemberImpl)} = member");
            //This.AttrsImpl = attrs;
            //This.DefaultValueImpl = defaultValue;
            //This.MemberImpl = member;
            //this.marshalAs = marshalAs;
            if (model.Flags.TypeHasFlag(ParameterFlagsModel.HasDefaultValue))
            {
                var value = model.DefaultValue ?? null;
                Script.Write("this.{nameof(ParameterInfo.DefaultValueImpl)} = value");
            }
            else
            {
                var missing = Missing.Value;
                Script.Write("this.{nameof(ParameterInfo.DefaultValueImpl)} = missing");
            }
            _model = model;
        }

        [NetJs.MemberReplace]
        internal int GetMetadataToken()
        {
            //return _model.Handle.As<int>();
            return 0;
        }

        [NetJs.MemberReplace]
        internal static Type[] GetTypeModifiers(Type type, MemberInfo member, int position, bool optional, int genericArgumentPosition = -1)
        {
            return Type.EmptyTypes;
        }
    }
}
