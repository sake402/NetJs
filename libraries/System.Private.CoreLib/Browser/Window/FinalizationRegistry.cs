using NetJs;

namespace Window
{
    [NetJs.External]
    public class FinalizationRegistry
    {
        public extern FinalizationRegistry(NativeAction<object> callBack);
        public extern void register(object value, object token);
    }
}
