using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace System.Linq.Expressions
{
    public static class LambdaExpressionExtensions
    {
        public static Delegate CompileImpl(this LambdaExpression expression, NetJs.TypePrototype delegateType)
        {
            var exp = expression.Body;
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
                else
                {
                    throw new InvalidOperationException("Unimplemented expression type");
                }
            }
            var returnDelegate = delegateType.NewWithDefaultConstructor(null, currentDelegate);
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
