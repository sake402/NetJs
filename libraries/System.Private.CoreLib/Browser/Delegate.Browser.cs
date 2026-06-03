using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics.Colors;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    //[NetJs.ForcePartial(typeof(Delegate))]
    public class Delegate_Partial : ForcedPartialBase<Delegate>
    {
        public Delegate_Partial(object target, string method)
        {
            //Script.Write("super(target, method)");
            Setup();
        }

        public Delegate_Partial(object target, MethodInfo method) : this(target, method, (parameters) =>
        {
            return method.Invoke(target, parameters);
        })
        {
        }

        public Delegate_Partial(object? target, MethodInfo method, Func<object[], object?> jsFunction)
        {
            //This._target = target
            //Script.Write("super(target, method.Name)");
            NetJs.Script.Write("this.method_info = method");
            NetJs.Script.Write("this._target = target"); //assign a dummy handle to the method handle
            this["$jsFunction"] = jsFunction;
            Setup();
        }

        void Setup()
        {
            //This.method_info = method;
            NetJs.Script.Write("this.method = 109848493483"); //assign a dummy handle to the method handle
            NetJs.Script.Write("this.method_is_virtual = true"); //amke sure the GetVirtualMethod_internalImpl is called
        }
    }

    internal class JSFunctionMethodInfo : MethodInfo
    {
        object? _target;
        Func<object[], object?> _jsFunction;
        public JSFunctionMethodInfo(object? target, Func<object[], object?> jsFunction)
        {
            _target = target;
            _jsFunction = jsFunction;
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => new EmptyCAHolder();
        public override MethodAttributes Attributes => MethodAttributes.Public;
        public override RuntimeMethodHandle MethodHandle => new RuntimeMethodHandle(IntPtr.Zero);
        public override string Name => "function";
        public override Type? DeclaringType => _target?.GetType() ?? typeof(object);
        public override Type? ReflectedType => _target?.GetType() ?? typeof(object);

        public override MethodInfo GetBaseDefinition()
        {
            return null!;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return [];
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return [];
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return (MethodImplAttributes)0;
        }

        public override ParameterInfo[] GetParameters()
        {
            throw new NotImplementedException();
        }

        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            //var jsFunction = this["$jsFunction"].As<Func<object[], object?>>();
            //return jsFunction(parameters);
            return NetJs.Script.Write<object>("this._jsFunction.apply(this._target, parameters)");
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }
    }

    public partial class Delegate
    {
        [NetJs.MemberReplace(nameof(AllocDelegateLike_internal))]
        private protected static MulticastDelegate AllocDelegateLike_internalImpl(Delegate d)
        {
            var prototype = d.GetPrototype();
            var _delegate = prototype.New().As<MulticastDelegate>();
            _delegate.bound = d.bound;
            _delegate.data = d.data;
            _delegate.delegate_trampoline = d.delegate_trampoline;
            _delegate.extra_arg = d.extra_arg;
            _delegate.interp_invoke_impl = d.interp_invoke_impl;
            _delegate.interp_method = d.interp_method;
            _delegate.invoke_impl = d.invoke_impl;
            _delegate.method = d.method;
            _delegate.method_code = d.method_code;
            _delegate.method_info = d.method_info;
            _delegate.method_is_virtual = d.method_is_virtual;
            _delegate.method_ptr = d.method_ptr;
            _delegate.original_method_info = d.original_method_info;
            _delegate._target = d._target;
            object? trampoline()
            {
                object? r;
                int i = 0;
                var delegates = NetJs.Script.Write<Delegate[]>("_delegate.delegates");
                int len = delegates.Length;
                var args = NetJs.Script.Write<object[]>("arguments");
                do
                {
                    var del = delegates[i];
                    r = NetJs.Script.Write<object?>("del.Invoke.apply(del, args)");
                } while (++i < len);
                return r;
            }
            _delegate[NetJs.Constants.NativeDelagateFunction] = trampoline;
            return _delegate;
        }

        [NetJs.MemberReplace(nameof(CreateDelegate_internal))]
        private static Delegate? CreateDelegate_internalImpl(QCallTypeHandle type, object? target, MethodInfo info, bool throwOnBindFailure)
        {
            var returnType = type.QCallTypeHandleToRuntimeType();
            return null;// new Delegate_Partial(target, info).As<Delegate>();
        }

        [NetJs.MemberReplace(nameof(GetVirtualMethod_internal))]
        private MethodInfo GetVirtualMethod_internalImpl()
        {
#pragma warning disable CS0184 // 'is' expression's given expression is never of the provided type
            if (this is Delegate_Partial)
            {
                var jsFunction = this["$jsFunction"].As<Func<object[], object?>>();
                return new JSFunctionMethodInfo(_target, jsFunction);
            }
#pragma warning restore CS0184 // 'is' expression's given expression is never of the provided type
            throw new NotImplementedException();
        }

    }
}
