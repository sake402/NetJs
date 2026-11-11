using NetJs;

namespace System.Reflection
{
    [NetJs.ForcePartial(typeof(RuntimeEventInfo))]
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    internal sealed partial class RuntimeEventInfo_Partial : ForcedPartialBase<RuntimeEventInfo>
    {
        MonoEventInfo? _info;
        //internal EventModel _model;
        internal RuntimeEventInfo_Partial(EventModel model)
        {
            THIS._model = model;
        }

        [NetJs.MemberReplace]
        private static void get_event_info(RuntimeEventInfo ev, out MonoEventInfo info)
        {
            if (ev.As<RuntimeEventInfo_Partial>()._info != null)
            {
                info = ev.As<RuntimeEventInfo_Partial>()._info.As<MonoEventInfo>();
                return;
            }
            var minfo = new MonoEventInfo();
            minfo.declaring_type = AppDomain.GetType(ev._model.DeclaringType.As<uint>()) ?? throw null!;
            minfo.reflected_type = minfo.declaring_type;//TODO
            var model = ev.As<RuntimeEventInfo>()._model.As<EventModel>();
            minfo.name = model.Name;
            minfo.add_method = NetJs.Script.IsDefined(model.AddMethod) ? new RuntimeMethodInfo(model.AddMethod!) : null!;
            minfo.remove_method = NetJs.Script.IsDefined(model.RemoveMethod) ? new RuntimeMethodInfo(model.RemoveMethod!) : null!;
            minfo.raise_method = NetJs.Script.IsDefined(model.RaiseMethod) ? new RuntimeMethodInfo(model.RaiseMethod!) : null!;
            minfo.attrs = EventAttributes.None;
            //minfo.other_methods = null;
            ev.As<RuntimeEventInfo_Partial>()._info = minfo;
            info = minfo;
        }

        [NetJs.MemberReplace]
        internal static int get_metadata_token(RuntimeEventInfo monoEvent)
        {
            return NetJs.Script.IsDefined(monoEvent._model.OriginalHandle) ? monoEvent._model.OriginalHandle.As<int>() : monoEvent._model.Handle.As<int>();
        }

        [NetJs.MemberReplace]
        private static EventInfo? internal_from_handle_type(IntPtr event_handle, IntPtr type_handle)
        {
            return (EventInfo?)AppDomain.GetMember((uint)event_handle);
        }

    }
}
