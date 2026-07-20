using NetJs;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection
{
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    internal sealed unsafe partial class RuntimeMethodInfo
    {
        //internal MethodModel _model;

        //For generic method
        internal RuntimeMethodInfo? _genericMethod;
        internal Type[]? _typeArguments;
        internal RuntimePropertyInfo? _fromProperty;

        internal RuntimeMethodInfo(MethodModel model, RuntimePropertyInfo? fromProperty = null) : base(model)
        {
            mhandle = model.Handle.As<IntPtr>();
            name = model.Name;
            //reftype = model.ReturnType != null ? AppDomain.GetType(model.ReturnType.Value) : null;
            _model = model;
            _fromProperty = fromProperty;
        }

        [NetJs.MemberReplace(nameof(GetMethodBodyInternal))]
        internal static MethodBody GetMethodBodyInternalImpl(IntPtr handle)
        {
            return null!;
        }

        [NetJs.MemberReplace(nameof(GetMethodFromHandleInternalType_native))]
        private static MethodBase GetMethodFromHandleInternalType_nativeImpl(IntPtr method_handle, IntPtr type_handle, bool genericCheck)
        {
            var member = AppDomain.GetMember((uint)method_handle);
            return (MethodBase)member!;
        }

        [NetJs.MemberReplace(nameof(get_name))]
        internal static string get_nameImpl(MethodBase method)
        {
            return method.As<RuntimeMethodInfo>()._model.Name;
        }

        [NetJs.MemberReplace(nameof(get_base_method))]
        internal static RuntimeMethodInfo get_base_methodImpl(RuntimeMethodInfo method, bool definition)
        {
            var baseType = method.DeclaringType.BaseType.As<RuntimeType>();
            return baseType.GetMethod(method.Name).As<RuntimeMethodInfo>();
        }

        [NetJs.MemberReplace(nameof(get_metadata_token))]
        internal static int get_metadata_tokenImpl(RuntimeMethodInfo method)
        {
            return method._model.Handle.As<int>();
        }

        [NetJs.MemberReplace(nameof(InternalInvoke))]
        internal object? InternalInvokeImpl(object? obj, IntPtr* args, out Exception? exc)
        {
            var prototype = DeclaringType.As<RuntimeType>()._prototype;
            object[]? parameters = RuntimeHelpers.GetParametersFromPointer(args);
            if (parameters == null && NetJs.Script.IsDefined(_model.As<MethodModel>().Parameters) && _model.As<MethodModel>().Parameters!.Length > 0)
            {
                throw null!;
            }
            exc = null;
            return RuntimeHelpers.NativeFunctionDispatch(_model.Flags.TypeHasFlag(MemberFlagsModel.IsStatic) ? prototype : obj!, _model, parameters);
            //var methodName = NetJs.Script.IsDefined(_model.OutputName) ? _model.OutputName!.NativeReplace("@", _model.Name) : _model.Name;
            //var method = (obj ?? prototype)[methodName];
            //exc = null;
            //return NetJs.Script.Write<object>("method.apply(obj, parameters)");
        }

        [NetJs.MemberReplace(nameof(GetPInvoke))]
        internal void GetPInvokeImpl(out PInvokeAttributes flags, out string entryPoint, out string dllName)
        {
            throw new NotImplementedException();
        }

        [NetJs.MemberReplace(nameof(MakeGenericMethod_impl))]
        private MethodInfo MakeGenericMethod_implImpl(Type[] types)
        {
            //var model = _model.As<MethodModel>();
            //var parameters = model.Parameters;
            //bool modelCloned = false;
            //if (NetJs.Script.IsDefined(parameters))
            //{
            //    for (int i = 0; i < parameters!.Length; i++)
            //    {
            //        var parameterType = parameters[i].ParameterType;
            //        //if (NetJs.Script.TypeOf(parameterType).NativeEquals("function"))
            //        //{
            //        //    var args = types.Map(t => t.As<RuntimeType>()._prototype);
            //        //    parameterType = NetJs.Script.Write<uint>("parameterType(...args)");
            //        //    if (!modelCloned)
            //        //    {
            //        //        model=NetJs.Script.JSONParse<>
            //        //    }
            //        //}
            //    }
            //}
            return new RuntimeMethodInfo(_model.As<MethodModel>())
            {
                _genericMethod = this,
                _typeArguments = types
            };
        }

        [NetJs.MemberReplace(nameof(GetGenericArguments))]
        public Type[] GetGenericArgumentsImpl()
        {
            if (_typeArguments != null)
                return _typeArguments;
            if (_model.Flags.TypeHasFlag(MemberFlagsModel.IsGeneric))
            {
                var len = _model.As<MethodModel>().GenericArguments!.Length;
                var genericTypes = AppDomain.GenericTypeParameters.ArraySlice(0, len).Map(e => e.Type!);
                return genericTypes;
            }
            return Type.EmptyTypes;
        }

        [NetJs.MemberReplace(nameof(GetGenericMethodDefinition_impl))]
        private MethodInfo GetGenericMethodDefinition_implImpl()
        {
            return _genericMethod ?? this;
        }

        [NetJs.MemberReplace(nameof(IsGenericMethodDefinition))]
        public bool IsGenericMethodDefinitionImpl
        {
            get => _model.Flags.TypeHasFlag(MemberFlagsModel.IsGeneric) && _typeArguments == null;
        }

        [NetJs.MemberReplace(nameof(IsGenericMethod))]
        public bool IsGenericMethodImpl
        {
            get => _model.Flags.TypeHasFlag(MemberFlagsModel.IsGeneric);
        }
    }
}
