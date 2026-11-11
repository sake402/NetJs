using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace System.Linq.Expressions
{
    public static class LambdaExpressionExtensions
    {
        static NetJs.NativeFunction<object, object?> BuildDelegate(Expression expression)
        {
            var exp = expression;
            NetJs.NativeFunction<object, object?> currentDelegate = (o) => o;
            while (exp != null)
            {
                if (exp is UnaryExpression un)
                {
                    exp = un.Operand;
                }
                else if (exp is MemberExpression mem)
                {
                    var originalDelegate = currentDelegate;
                    object? ReadMember(object source)
                    {
                        var o = originalDelegate(source);
                        if (mem.Member is PropertyInfo prop)
                            return prop.GetMethod!.Invoke(o, []);
                        return ((FieldInfo)mem.Member).GetValue(o);
                    }
                    currentDelegate = ReadMember;
                    exp = mem.Expression;
                }
                else if (exp is ParameterExpression pem)
                {
                    if (pem.Name != "value")
                    {
                        var originalDelegate = currentDelegate;
                        object? ReadParameter(object source)
                        {
                            var o = originalDelegate(source);
                            return o![pem.Name!];
                        }
                        currentDelegate = ReadParameter;
                    }
                    exp = null;
                }
                else if (exp is ConstantExpression ce)
                {
                    var originalDelegate = currentDelegate;
                    object? ReadConstant(object source)
                    {
                        return ce.Value;
                    }
                    currentDelegate = ReadConstant;
                    exp = null;
                }
                else if (exp is MethodCallExpression mc)
                {
                    var methodInfo = mc.Method;
                    var arguments = NetJs.Script.NewArray<NetJs.NativeFunction<object, object?>>();
                    foreach (var a in mc.Arguments)
                    {
                        arguments.push(BuildDelegate(a));
                    }
                    var originalDelegate = currentDelegate;
                    object? Call(object source)
                    {
                        var o = originalDelegate(source);
                        return methodInfo.Invoke(o, arguments.map(a => a(source)));
                    }
                    currentDelegate = Call;
                    exp = null;
                }
                else
                {
                    throw new InvalidOperationException("Unimplemented expression type");
                }
            }
            return currentDelegate;
        }
        public static Delegate CompileImpl(this LambdaExpression expression, NetJs.TypePrototype delegateType)
        {
            var exp = expression.Body;
            var returnDelegate = delegateType.NewWithDefaultConstructor(null, BuildDelegate(exp));
            return returnDelegate.As<Delegate>();
        }
    }

    [NetJs.ForcePartial(typeof(LambdaExpression))]
    public abstract class LambdaExpression_Partial
    {
        [NetJs.MemberReplace("Compile()")]
        public Delegate Compile()
        {
            var type = this.As<LambdaExpression>().TypeCore;
            return this.As<LambdaExpression>().CompileImpl(type[NetJs.Constants.TypePrototypeName].As<NetJs.TypePrototype>());
        }

        [NetJs.MemberReplace("Compile(bool)")]
        public Delegate Compile(bool preferInterpretation)
        {
            var type = this.As<LambdaExpression>().TypeCore;
            return this.As<LambdaExpression>().CompileImpl(type[NetJs.Constants.TypePrototypeName].As<NetJs.TypePrototype>());
        }
    }

    [NetJs.ForcePartial(typeof(Expression<>))]
    public class Expression_Partial<TDelegate>
    {
        [NetJs.MemberReplace("Compile()")]
        public TDelegate Compile()
        {
            return this.As<LambdaExpression>().CompileImpl(GetPrototype<TDelegate>()).As<TDelegate>();
        }

        [NetJs.MemberReplace("Compile(bool)")]
        public TDelegate Compile(bool preferInterpretation)
        {
            return this.As<LambdaExpression>().CompileImpl(GetPrototype<TDelegate>()).As<TDelegate>();
        }

    }
}
