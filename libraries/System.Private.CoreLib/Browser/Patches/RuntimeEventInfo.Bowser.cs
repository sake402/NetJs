using NetJs;

namespace System.Reflection
{
    [NetJs.ForcePartial(typeof(RuntimeEventInfo))]
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    internal sealed partial class RuntimeEventInfo_Partial : ForcedPartialBase<RuntimeEventInfo>
    {
        internal EventModel _model;
        internal RuntimeEventInfo_Partial(EventModel model)
        {
            _model = model;
        }

        [NetJs.MemberReplace]
        private static void get_event_info(RuntimeEventInfo ev, out MonoEventInfo info)
        {
            var minfo = new MonoEventInfo();
            minfo.declaring_type = ev.DeclaringType;
            minfo.reflected_type = ev.ReflectedType;
            var model = ev.As<RuntimeEventInfo_Partial>()._model;
            minfo.name = model.Name;
            minfo.add_method = model.AddMethod != null ? new RuntimeMethodInfo(model.AddMethod) : null!;
            minfo.remove_method = model.RemoveMethod != null ? new RuntimeMethodInfo(model.RemoveMethod) : null!;
            minfo.raise_method = model.RaiseMethod != null ? new RuntimeMethodInfo(model.RaiseMethod) : null!;
            minfo.attrs = EventAttributes.None;
            //minfo.other_methods = null;
            info = minfo;
        }

        [NetJs.MemberReplace]
        internal static int get_metadata_token(RuntimeEventInfo monoEvent)
        {
            return (int)monoEvent.As<RuntimeEventInfo_Partial>()._model.Handle;
        }

        [NetJs.MemberReplace]
        private static EventInfo? internal_from_handle_type(IntPtr event_handle, IntPtr type_handle)
        {
            return (EventInfo?)AppDomain.GetMember((uint)event_handle);
        }

    }
}
