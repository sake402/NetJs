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
            return NetJs.Script.IsDefined(method._model.OriginalHandle) ? method._model.OriginalHandle.As<int>() : method._model.Handle.As<int>();
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
            var type = DeclaringType.As<RuntimeType>();
            var existing = type._genericMethods.ArrayFirstOrDefault(m =>
            {
                if (m._typeArguments != null && m._typeArguments.Length == types.Length)
                {
                    for (int i = 0; i < types.Length; i++)
                    {
                        if (m._typeArguments[i] != types[i])
                            return false;
                    }
                }
                return false;
            });
            if (existing != null)
                return existing;

            var model = NetJs.Script.JSONParse<MethodModel>(NetJs.Script.JSONStringify(_model));
            if (NetJs.Script.TypeOf(_model.As<MethodModel>().ReturnType).NativeEquals("function")) //copy the return type that cant serialize
            {
                model.ReturnType = _model.As<MethodModel>().ReturnType.ResolveHandle(types);
            }
            if (NetJs.Script.IsDefined(_model.As<MethodModel>().Parameters))
            {
                for(int i = 0; i < _model.As<MethodModel>().Parameters!.Length; i++)
                {
                    unchecked
                    {
                        if (NetJs.Script.TypeOf(_model.As<MethodModel>().Parameters![i].ParameterType).NativeEquals("function")) //copy the parameter types that cant serialize
                        {
                            model.Parameters![i].ParameterType = _model.As<MethodModel>().Parameters![i].ParameterType.ResolveHandle(types);
                        }
                    }
                }
            }
            model.GenericArguments = types.map(t => t.As<RuntimeType>()._prototype.As<string>());
            uint handle = type._nextGenericMemberHandle++;
            model.Handle = NetJs.Script.Write<ulong>("Number(BigInt(type._model.{nameof(MemberModel.Handle)}) | BigInt(handle) << 32n)");// (uint)((ulong)type._model.Handle.As<uint>() | (((ulong)handle) << ReflectionHandleExtension.MemberShift)).As<ulong>();
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
            var method = new RuntimeMethodInfo(model)
            {
                _genericMethod = this,
                _typeArguments = types
            };
            type._genericMethods.push(method);
            return method;
        }

        [NetJs.MemberReplace(nameof(GetGenericArguments))]
        public Type[] GetGenericArgumentsImpl()
        {
            if (_typeArguments != null)
                return _typeArguments;
            if (_model.Flags.TypeHasFlag(MemberFlagsModel.IsGeneric))
            {
                var len = _model.As<MethodModel>().GenericArguments!.Length;
                var genericTypes = AppDomain.GenericTypeParameters.slice(0, len).map(e => e.Type!);
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
