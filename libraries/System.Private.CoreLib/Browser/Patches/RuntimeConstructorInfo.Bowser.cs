using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    internal sealed unsafe partial class RuntimeConstructorInfo
    {
        internal RuntimeConstructorInfo(NetJs.ConstructorModel model)
        {
            mhandle = model.Handle.As<IntPtr>();
            name = model.Name;
            //reftype = model.ReturnType != null ? AppDomain.GetType(model.ReturnType.Value) : null;
            _model = model;
        }

        [NetJs.MemberReplace(nameof(InvokeClassConstructor))]
        internal static void InvokeClassConstructorIImpl(QCallTypeHandle type)
        {
            //var mtype = type.QCallTypeHandleToRuntimeType();
            //var prototype = mtype.DeclaringType.As<RuntimeType>()._prototype;
            //var dobject = NetJs.Script.Write<object>("new prototype()");
            //var ctor = dobject[_model.OutputName!];
            //NetJs.Script.Write("ctor.apply(dobject, parameters)");
            //return dobject;

        }

        [NetJs.MemberReplace(nameof(get_metadata_token))]
        internal static int get_metadata_tokenImpl(RuntimeConstructorInfo method)
        {
            return (int)method._model.Handle;
        }

        [NetJs.MemberReplace(nameof(InternalInvoke))]
        internal object InternalInvokeImpl(object? obj, IntPtr* args, out Exception? exc)
        {
            var prototype = DeclaringType.As<RuntimeType>()._prototype;
            var dobject = prototype.New();
            var ctorName = _model.GetOutputName();
            var ctor = dobject[ctorName];
            //If calling default constructor, it may not be exported, if the type does not explicitly define it
            if (NetJs.Script.IsDefined(ctor))
            {
                object[]? parameters = RuntimeHelpers.GetParametersFromPointer(args);
                if (parameters == null && NetJs.Script.IsDefined(_model.As<NetJs.ConstructorModel>().Parameters) && _model.As<NetJs.ConstructorModel>().Parameters!.Length > 0)
                {
                    throw null!;
                }
                RuntimeHelpers.NativeFunctionDispatch(dobject, ctorName, prototype, parameters);
                //NetJs.Script.Write("ctor.apply(dobject, parameters)");
            }
            else
            {
                if (args != null)
                {
                    throw null!;
                }
            }
            exc = null;
            return dobject;
        }

        [NetJs.MemberReplace(nameof(Invoke) + "(BindingFlags, Binder?, object?[]?, CultureInfo?)")]
        public object InvokeImpl(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            var prototype = DeclaringType.As<RuntimeType>()._prototype;
            var dobject = prototype.New();
            var outputName = _model.GetOutputName();
            var ctor = dobject[outputName];
            parameters = parameters?.Map(p => NetJs.Script.Unbox(p));
            //If calling default constructor, it may not be exported, if the type does not explicitly define it
            if (NetJs.Script.IsDefined(ctor))
                NetJs.Script.Write("ctor.apply(dobject, parameters)");
            return dobject;
        }
    }
}
