using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Numbers
{
    /// <summary>
    /// Casting a literal number should result in a compile time literal, if the value is within the range of the target type.
    /// This allows us to eliminate unnecessary casts like (int)1.0 which would otherwise result in a runtime cast in javascript.
    /// </summary>
    internal class UnneccesaryNumericCastSyntaxEmitter : SyntaxEmitter<CastExpressionSyntax>
    {
        public override bool TryEmit(CastExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            var fromType = visitor.Global.TryGetTypeSymbol(node.Expression, visitor);
            var toType = visitor.Global.TryGetTypeSymbol(node.Type, visitor);

            //Casting a literal number should result in a compile time literal
            if (fromType != null &&
                toType != null &&
                fromType.IsIntegerNumericType() &&
                toType.IsIntegerNumericType())
            {
                var literalValue = visitor.Global.EvaluateConstant(node.Expression, visitor);
                if (literalValue.HasValue)
                {
                    var minValue = toType.GetMembers("MinValue").SingleOrDefault();
                    var maxValue = toType.GetMembers("MaxValue").SingleOrDefault();
                    double? min = null, max = null;
                    if (minValue is IFieldSymbol minF && maxValue is IFieldSymbol maxF && minF.HasConstantValue && maxF.HasConstantValue)
                    {
                        if (minF.ConstantValue is char c)
                        {
                            min = (double)c;
                        }
                        else
                            min = Convert.ToDouble(minF.ConstantValue);
                        if (maxF.ConstantValue is char cc)
                        {
                            max = (double)cc;
                        }
                        else
                            max = Convert.ToDouble(maxF.ConstantValue);
                    }
                    else if (toType.IsType("System.IntPtr"))
                    {
                        min = int.MinValue;
                        max = int.MaxValue;
                    }
                    else if (toType.IsType("System.UIntPtr"))
                    {
                        min = uint.MinValue;
                        max = uint.MaxValue;
                    }
                    if (min != null && max != null)
                    {
                        double value;
                        if (literalValue.Value is char c)
                        {
                            value = (double)c;
                        }
                        else
                            value = Convert.ToDouble(literalValue.Value);
                        if (value >= min && value <= max)
                        {
                            visitor.Visit(node.Expression);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}