using NetJs;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    //[NetJs.ForcePartial(typeof(Delegate))]
    //public class Delegate_Partial : ForcedPartialBase<Delegate>
    //{
    //    //public Delegate_Partial(object target, string method)
    //    //{
    //    //    //Script.Write("super(target, method)");
    //    //    Setup();
    //    //}

    //    //public Delegate_Partial(object target, MethodInfo method) : this(target, method, (parameters) =>
    //    //{
    //    //    return method.Invoke(target, parameters);
    //    //})
    //    //{
    //    //}

    //    //public Delegate_Partial(object? target, MethodInfo method, Func<object[], object?> jsFunction)
    //    //{
    //    //    //This._target = target
    //    //    //Script.Write("super(target, method.Name)");
    //    //    NetJs.Script.Write("this.method_info = method");
    //    //    NetJs.Script.Write("this._target = target"); //assign a dummy handle to the method handle
    //    //    this["$jsFunction"] = jsFunction;
    //    //    Setup();
    //    //}

    //    //void Setup()
    //    //{
    //    //    //This.method_info = method;
    //    //    NetJs.Script.Write("this.method = 109848493483"); //assign a dummy handle to the method handle
    //    //    NetJs.Script.Write("this.method_is_virtual = true"); //amke sure the GetVirtualMethod_internalImpl is called
    //    //}

    //}

    internal class AnonymousFunctionMethodInfo : MethodInfo
    {
        Delegate _delegate;
        object? _target;
        //Func<object[], object?> _jsFunction;
        public AnonymousFunctionMethodInfo(Delegate _delegate, object? _target, MethodModel anonymousMethodModel)
        {
            this._delegate = _delegate;
            if (_target != null)
            {
                TypePrototype? prototype;
                if (NetJs.Script.TypeOf(_target).NativeEquals("function")) //a static class prototype
                {
                    prototype = _target.As<TypePrototype>();
                }
                else //instance method
                {
                    this._target = _target;
                    prototype = NetJs.Script.GetClassPrototypeOf(_target);
                }
                var nativeFunctionName = _delegate[Constants.NativeDelagateFunctionName]!["name"].As<string>();
                if (nativeFunctionName.Length > 0)
                {
                    unchecked
                    {
                        _model = prototype.Metadata!.Methods!.Filter(f => f.Name.NativeEquals(nativeFunctionName))[0] ?? throw null!;
                    }
                }
                else
                {
                    //anonymous method
                    _model = anonymousMethodModel;
                }
            }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => new EmptyCAHolder();
        public override MethodAttributes Attributes
        {
            get
            {
                MethodAttributes att = MethodAttributes.Public;
                if (_target == null || NetJs.Script.TypeOf(_target).NativeEquals("function"))
                {
                    att |= MethodAttributes.Static;
                }
                return att;
            }
        }

        public override RuntimeMethodHandle MethodHandle => new RuntimeMethodHandle(NetJs.Script.IsDefined(_model?.Handle) ? _model!.Handle.As<IntPtr>() : IntPtr.Zero);
        public override string Name => _model?.Name ?? "function";
        public override Type? DeclaringType => _target?.GetType() ?? typeof(object);
        public override Type? ReflectedType => _target?.GetType() ?? typeof(object);

        public override MethodInfo GetBaseDefinition()
        {
            return null!;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return Attribute.GetCustomAttributes(this, inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return Attribute.GetCustomAttributes(this, attributeType, inherit);
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return 0;
        }

        internal override int GetParametersCount()
        {
            return _model.As<MethodModel>().Parameters?.Length ?? 0;
        }

        public override ParameterInfo[] GetParameters()
        {
            if (_parameters != null)
                return _parameters;
            var parameters = _model.As<MethodModel>().Parameters ?? null;
            var infos = parameters?.Map((p, i, all) =>
            {
                var parameterTypeHandle = p.ParameterType;
                //if (NetJs.Script.TypeOf(parameterTypeHandle).NativeEquals("function"))
                //{
                //    var args = method._typeArguments!.Map(t => t.As<RuntimeType>()._prototype);
                //    parameterTypeHandle = NetJs.Script.Write<uint>("parameterTypeHandle( ...args)");
                //}
                return new RuntimeParameterInfo_Partial(p, AppDomain.GetType(parameterTypeHandle.As<uint>()) ?? throw new InvalidOperationException(), this, i).As<RuntimeParameterInfo>();
            }).AsNetArray() ?? Array.Empty<ParameterInfo>();
            return _parameters = infos;
        }

        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            var jsFunction = this._delegate[Constants.NativeDelagateFunctionName];
            parameters = parameters?.Map(p => NetJs.Script.Unbox(p));
            return NetJs.Script.Write<object>("jsFunction.apply(obj, parameters)");
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return Attribute.IsDefined(this, attributeType, inherit);
            //return _delegate.Method.IsDefined(attributeType, inherit);
            //return false;
        }
    }

    public partial class Delegate
    {
        [NetJs.MemberReplace(nameof(AllocDelegateLike_internal))]
        private protected static MulticastDelegate AllocDelegateLike_internalImpl(Delegate d)
        {
            var prototype = d.GetClassPrototype();
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
                    unchecked
                    {
                        var del = delegates[i];
                        r = NetJs.Script.Write<object?>("del.Invoke.apply(del, args)");
                    }
                } while (++i < len);
                return r;
            }
            _delegate[NetJs.Constants.NativeDelagateFunctionName] = trampoline;
            return _delegate;
        }

        [NetJs.MemberReplace(nameof(CreateDelegate_internal))]
        private static Delegate? CreateDelegate_internalImpl(QCallTypeHandle type, object? target, MethodInfo info, bool throwOnBindFailure)
        {
            var delegateType = type.QCallTypeHandleToRuntimeType();
            var _delegate = delegateType._prototype.New();
            var ctor = _delegate[NetJs.Constants.DefaultConstructorName];
            object? trampoline()
            {
                //TODO: More likely the delegate caller has already type checked parameters or enforced by the compiler
                //We should consider invoking the native js method directly instead of calling info.Invoke()(slower) which does type checking again on the arguments
                var prototype = info.DeclaringType.As<RuntimeType>()._prototype;

                //Our convention for a generic method is Method<T>(arg) => Method(T, arg)
                //We therefore must insert the generic arg first
                object[] args = NetJs.Script.NewArray<object>();
                unchecked
                {
                    if (NetJs.Script.IsDefined(info._model.As<MethodModel>().GenericArguments) &&
                        info._model.As<MethodModel>().GenericArguments!.Length > 0)
                    {
                        args = NetJs.Script.NewArray<object>();
                        for (int i = 0; i < info.As<RuntimeMethodInfo>()._typeArguments!.Length; i++)
                        {
                            args.Push(info.As<RuntimeMethodInfo>()._typeArguments![i].As<RuntimeType>()._prototype);
                        }
                    }
                    var methodArgs = NetJs.Script.Arguments();
                    if (info._model.Flags.TypeHasFlag(MemberFlagsModel.IsStatic)
                        && NetJs.Script.IsDefined(info._model.As<MethodModel>().Parameters)
                        && methodArgs.Length == info._model.As<MethodModel>().Parameters!.Length - 1)//method expect an extra(this) parameter not known by the delegate signature, bind it to target
                    {
                        args.Push(target);
                    }
                    for (int i = 0; i < methodArgs.Length; i++)
                    {
                        args.Push(methodArgs[i]);
                    }
                }
                return RuntimeHelpers.NativeFunctionDispatch(info._model.Flags.TypeHasFlag(MemberFlagsModel.IsStatic) ? prototype : target, info._model, args);
                //return info.Invoke(target, NetJs.Script.Arguments());
            }
            RuntimeHelpers.NativeFunctionDispatch(_delegate, NetJs.Constants.DefaultConstructorName, target, trampoline);
            //NetJs.Script.Write("ctor.call(_delegate, target, trampoline)");
            return _delegate.As<Delegate>();
        }

        //        [NetJs.MemberReplace(nameof(GetVirtualMethod_internal))]
        //        private MethodInfo GetVirtualMethod_internalImpl()
        //        {
        //#pragma warning disable CS0184 // 'is' expression's given expression is never of the provided type
        //            //if (this is Delegate_Partial)
        //            {
        //                //var jsFunction = this["$jsFunction"].As<Func<object[], object?>>();
        //                return new JSNativeFunctionMethodInfo(this);
        //            }
        //#pragma warning restore CS0184 // 'is' expression's given expression is never of the provided type
        //            throw new NotImplementedException();
        //        }

        [NetJs.MemberReplace(nameof(GetMethodImpl))]
        protected virtual MethodInfo GetMethodImplImpl()
        {
            //var methodInfo = this["$methodInfo"].As<MethodInfo>() ?? null; //uses $methodInfo instead of a field to keep the StructLlayout of this Delegate intact
            if (method_info != null)
                return method_info;
            var target = this[Constants.NativeDelagateFunctionTargetName];
            if (NetJs.Script.IsDefined(target))
            {
                var fn = this[Constants.NativeDelagateFunctionName];
                TypePrototype? prototype;
                string? nativeFunctionName = NetJs.Script.IsDefined(fn) ? (fn!["name"].As<string>() ?? null) : null;
                if (NetJs.Script.TypeOf(target).NativeEquals("function")) //a static class prototype
                {
                    prototype = target.As<TypePrototype>();
                }
                else //instance method
                {
                    prototype = NetJs.Script.GetClassPrototypeOf(target!);
                }
                if (prototype != null && nativeFunctionName != null && nativeFunctionName.Length > 0)
                {
                    unchecked
                    {
                        var methodModel = prototype.Metadata!.Methods!.Filter(f => (f.OutputName ?? f.Name).NativeEquals(nativeFunctionName))[0] ?? null;
                        if (NetJs.Script.IsDefined(methodModel))
                        {
                            var info = (MethodInfo?)AppDomain.GetMember(methodModel!.Handle.As<uint>());
                            if (info != null)
                            {
                                method_info = info;
                                return info;
                            }
                        }
                    }
                }
            }
            var model = this[Constants.NativeDelagateAnonymousFunctionModel].As<MethodModel>();
            Debug.Assert((model ?? null) != null);
            method_info = new AnonymousFunctionMethodInfo(this, target, model!);
            return method_info;
        }

        //[NetJs.MemberReplace(nameof(DynamicInvokeImpl))]
        //protected virtual object? DynamicInvokeImplImpl(object?[]? args)
        //{
        //    MethodInfo method = Method;

        //    object? target = _target;

        //    data ??= CreateDelegateData();

        //    // replace all Type.Missing with default values defined on parameters of the delegate if any
        //    MethodInfo? invoke = GetType().GetMethod("Invoke");
        //    if (invoke != null && args != null)
        //    {
        //        ReadOnlySpan<ParameterInfo> delegateParameters = invoke.GetParametersAsSpan();
        //        for (int i = 0; i < args.Length; i++)
        //        {
        //            if (args[i] == Type.Missing)
        //            {
        //                ParameterInfo dlgParam = delegateParameters[i];
        //                if (dlgParam.HasDefaultValue)
        //                {
        //                    args[i] = dlgParam.DefaultValue;
        //                }
        //            }
        //        }
        //    }

        //    if (method.IsStatic)
        //    {
        //        //
        //        // The delegate is bound to _target
        //        //
        //        if (data.curried_first_arg)
        //        {
        //            if (args is null)
        //            {
        //                args = new object?[] { target };
        //            }
        //            else
        //            {
        //                Array.Resize(ref args, args.Length + 1);
        //                Array.Copy(args, 0, args, 1, args.Length - 1);
        //                args[0] = target;
        //            }

        //            target = null;
        //        }
        //    }
        //    else
        //    {
        //        if (_target is null && args?.Length > 0)
        //        {
        //            target = args[0];
        //            Array.Copy(args, 1, args, 0, args.Length - 1);
        //            Array.Resize(ref args, args.Length - 1);
        //        }
        //    }

        //    return method.Invoke(target, args);
        //}

        [NetJs.Name(NetJs.Constants.IsTypeName)]
        public static bool Is(object instance)
        {
            var thisPrototype = NetJs.Script.Write<TypePrototype>("this");
            var thatPrototype = instance.GetClassPrototype();
            if (thisPrototype == thatPrototype)
            {
                return true;
            }
            //if a generic delegate like Func<int> and Func<long>, they wont equate, but the originals (Func<> and Func<>) must equate for this to pass
            if (NetJs.Script.IsDefined(thisPrototype.OpenGenericPrototype) && NetJs.Script.IsDefined(thatPrototype.OpenGenericPrototype))
            {
                if (thisPrototype.OpenGenericPrototype == thatPrototype.OpenGenericPrototype &&
                    NetJs.Script.IsDefined(thisPrototype.Metadata?.Methods) &&
                    NetJs.Script.IsDefined(thatPrototype.Metadata?.Methods))
                {
                    unchecked
                    {
                        MethodModel? thisInvoke = null;
                        for (int i = 0; i < thisPrototype.Metadata!.Methods!.Length; i++)
                        {
                            if (thisPrototype.Metadata!.Methods[i].Name.NativeEquals("Invoke"))
                            {
                                thisInvoke = thisPrototype.Metadata!.Methods[i];
                                break;
                            }
                        }
                        if (thisInvoke == null)
                            return false;
                        MethodModel? thatInvoke = null;
                        for (int i = 0; i < thatPrototype.Metadata!.Methods!.Length; i++)
                        {
                            if (thatPrototype.Metadata!.Methods[i].Name == "Invoke")
                            {
                                thatInvoke = thatPrototype.Metadata!.Methods[i];
                                break;
                            }
                        }
                        if (thatInvoke == null)
                            return false;
                        if ((thisInvoke.Parameters?.Length ?? 0) == (thatInvoke.Parameters?.Length ?? 0))
                        {
                            var len = thisInvoke.Parameters?.Length ?? 0;
                            for (int i = 0; i < len; i++)
                            {
                                if (thisInvoke.Parameters![i].ParameterType == thatInvoke.Parameters![i].ParameterType)
                                {
                                    continue;
                                }
                                if (thisPrototype.Flags.TypeHasFlag(TypeFlagsModel.IsGenericType) && //Casting that Action<object>, to this Action<T> is allowed due to contravariance
                                    !thisPrototype.Flags.TypeHasFlag(TypeFlagsModel.IsValueType))
                                {
                                    if (thisInvoke.Parameters![i].Flags.TypeHasFlag(ParameterFlagsModel.ContravariantIn) &&
                                        thatInvoke.Parameters![i].Flags.TypeHasFlag(ParameterFlagsModel.ContravariantIn))
                                    {
                                        var thisParamType = AppDomain.GetType(thisInvoke.Parameters![i].ParameterType.As<uint>());
                                        var thatParamType = AppDomain.GetType(thatInvoke.Parameters![i].ParameterType.As<uint>());
                                        if (thatParamType!.IsAssignableFrom(thisParamType))
                                            continue;
                                    }
                                }
                                return false;
                            }
                        }
                        //If we get here, the parameters match
                        if (NetJs.Script.IsUndefined(thisInvoke.ReturnType) && NetJs.Script.IsUndefined(thatInvoke.ReturnType)) //No return type on both
                        {
                            return true;
                        }
                        if (NetJs.Script.IsDefined(thisInvoke.ReturnType) && NetJs.Script.IsDefined(thatInvoke.ReturnType)) //If we get here, the parameters match
                        {
                            if (thisInvoke.ReturnType == thatInvoke.ReturnType)
                                return true;
                            if (thisInvoke.Flags.TypeHasFlag(MemberFlagsModel.ReturnTypeIsCovariantOut) && //Casting that Func<T>, to this Func<object> is allowed due to contravariance
                                thatInvoke.Flags.TypeHasFlag(MemberFlagsModel.ReturnTypeIsCovariantOut))
                            {
                                var thisReturnType = AppDomain.GetType(thisInvoke.ReturnType.As<uint>());
                                var thatReturnType = AppDomain.GetType(thatInvoke.ReturnType.As<uint>());
                                if (thisReturnType!.IsAssignableFrom(thatReturnType))
                                    return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

    }
}
