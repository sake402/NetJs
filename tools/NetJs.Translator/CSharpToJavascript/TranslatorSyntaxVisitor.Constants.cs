using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        //Even though a long can be a constant in c#.
        //In this js port it cannot because long is implemented as a struct (not a native javascript number because of rounding error) we need to instantiate
        //The only way we can serialize it as a constant without loosing value is through a string or primitive values(if possible without loosing precision
        void WriteLongConstant(CSharpSyntaxNode node, long longValue, int _base)
        {
            var format = _base == 16 ? $"0x{longValue:X}" : _base == 2 ? $"0b{longValue:B}" : longValue.ToString();
            CurrentTypeWriter.Write(node, format);
            CurrentTypeWriter.Write(node, "n");
            return;
            //var longType = (INamedTypeSymbol)_global.GetTypeSymbol("long", this/*, out _, out _*/);
            //long ilong = long.Parse(longValue);
            //if (Math.Abs(ilong) < int.MaxValue) //if the long can fit an int, we call the int constructor otherwise string
            //{
            //    var intType = (ITypeSymbol)_global.GetTypeSymbol("int", this/*, out _, out _*/);
            //    var intBasedConstructor = longType.GetMembers(".ctor").Cast<IMethodSymbol>().Single(e => e.Parameters.Count() == 1 && e.Parameters.Single().Type.Equals(intType, SymbolEqualityComparer.Default));
            //    //WriteConstructorCall(node, longType, intBasedConstructor, null, [SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((int)ilong)))]);
            //    WriteConstructorCall(node, longType, intBasedConstructor, null, [new CodeNode(() =>
            //    {
            //        CurrentTypeWriter.Write(node, ((int)ilong).ToString());
            //    })]);
            //}
            //else
            //{
            //    var stringType = (ITypeSymbol)_global.GetTypeSymbol("string", this/*, out _, out _*/);
            //    var stringBasedConstructor = longType.GetMembers(".ctor").Cast<IMethodSymbol>().Single(e => e.Parameters.Count() == 1 && e.Parameters.Single().Type.Equals(stringType, SymbolEqualityComparer.Default));
            //    //WriteConstructorCall(node, longType, stringBasedConstructor, null, [SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(longValue)))]);
            //    WriteConstructorCall(node, longType, stringBasedConstructor, null, [new CodeNode(() =>
            //    {
            //        CurrentTypeWriter.Write(node, "\"");
            //        CurrentTypeWriter.Write(node, longValue.ToString());
            //        CurrentTypeWriter.Write(node, "\"");
            //    })]);
            //}
        }

        void WriteULongConstant(CSharpSyntaxNode node, ulong longValue, int _base)
        {
            var format = _base == 16 ? $"0x{longValue:X}" : _base == 2 ? $"0b{longValue:B}" : longValue.ToString();
            CurrentTypeWriter.Write(node, format);
            CurrentTypeWriter.Write(node, "n");
            return;
            //var longType = (INamedTypeSymbol)_global.GetTypeSymbol("ulong", this/*, out _, out _*/);
            //ulong ilong = ulong.Parse(longValue);
            //if (ilong < uint.MaxValue) //if the long can fit an int, we call the int constructor otherwise string
            //{
            //    var intType = (ITypeSymbol)_global.GetTypeSymbol("uint", this/*, out _, out _*/);
            //    var intBasedConstructor = longType.GetMembers(".ctor").Cast<IMethodSymbol>().Single(e => e.Parameters.Count() == 1 && e.Parameters.Single().Type.Equals(intType, SymbolEqualityComparer.Default));
            //    //WriteConstructorCall(node, longType, intBasedConstructor, null, [SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((int)ilong)))]);
            //    WriteConstructorCall(node, longType, intBasedConstructor, null, [new CodeNode(() =>
            //    {
            //        CurrentTypeWriter.Write(node, ((uint)ilong).ToString());
            //    })]);
            //}
            //else
            //{
            //    var stringType = (ITypeSymbol)_global.GetTypeSymbol("string", this/*, out _, out _*/);
            //    var stringBasedConstructor = longType.GetMembers(".ctor").Cast<IMethodSymbol>().Single(e => e.Parameters.Count() == 1 && e.Parameters.Single().Type.Equals(stringType, SymbolEqualityComparer.Default));
            //    //WriteConstructorCall(node, longType, stringBasedConstructor, null, [SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(longValue)))]);
            //    WriteConstructorCall(node, longType, stringBasedConstructor, null, [new CodeNode(() =>
            //    {
            //        CurrentTypeWriter.Write(node, "\"");
            //        CurrentTypeWriter.Write(node, longValue.ToString());
            //        CurrentTypeWriter.Write(node, "\"");
            //    })]);
            //}
        }

        void WriteDecimalConstant(CSharpSyntaxNode node, string decimalValue)
        {
            var decimalType = (INamedTypeSymbol)_global.GetSymbol("decimal", this/*, out _, out _*/);
            decimal idecimal = decimal.Parse(decimalValue);
            if (Math.Abs(idecimal) < int.MaxValue && !decimalValue.Contains('.')) //if the decimal can fit an int, we call the int constructor otherwise string
            {
                var intType = (ITypeSymbol)_global.GetSymbol("int", this/*, out _, out _*/);
                var intBasedConstructor = decimalType.GetMembers(".ctor").Cast<IMethodSymbol>().Single(e => e.Parameters.Count() == 1 && e.Parameters.Single().Type.Equals(intType, SymbolEqualityComparer.Default));
                //WriteConstructorCall(node, decimalType, intBasedConstructor, null, [SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((int)idecimal)))]);
                WriteConstructorCall(node, decimalType, intBasedConstructor, null, [new CodeNode(() =>
                {
                    CurrentTypeWriter.Write(node, ((int)idecimal).ToString());
                })]);
            }
            else
            {
                var stringType = (ITypeSymbol)_global.GetSymbol("string", this/*, out _, out _*/);
                var stringBasedConstructor = decimalType.GetMembers(".ctor").Cast<IMethodSymbol>().SingleOrDefault(e => e.Parameters.Count() == 1 && e.Parameters.Single().Type.Equals(stringType, SymbolEqualityComparer.Default));
                if (stringBasedConstructor != null)
                {
                    //WriteConstructorCall(node, decimalType, stringBasedConstructor, null, [SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(decimalValue)))]);
                    WriteConstructorCall(node, decimalType, stringBasedConstructor, null, [new CodeNode(() =>
                    {
                        CurrentTypeWriter.Write(node, "\"");
                        CurrentTypeWriter.Write(node, decimalValue);
                        CurrentTypeWriter.Write(node, "\"");
                    })]);
                }
                else
                {
                    var bits = decimal.GetBits(idecimal);
                    if (bits.Length == 4)
                    {
                        var intType = (ITypeSymbol)_global.GetSymbol("int", this/*, out _, out _*/);
                        var arrayBasedConstructor = decimalType.GetMembers(".ctor").Cast<IMethodSymbol>().SingleOrDefault(e => e.Parameters.Count() == 1 && e.Parameters.Single().Type.IsArray(out var i) && i.Equals(intType, SymbolEqualityComparer.Default));
                        //var parameter = SyntaxFactory.ArrayCreationExpression(
                        //    SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName("int")),
                        //    SyntaxFactory.InitializerExpression(
                        //        SyntaxKind.ArrayInitializerExpression,
                        //        SyntaxFactory.SeparatedList<ExpressionSyntax>(bits.Select(b => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(b))))));
                        //WriteConstructorCall(node, decimalType, arrayBasedConstructor, null, [SyntaxFactory.Argument(parameter)]);
                        WriteConstructorCall(node, decimalType, arrayBasedConstructor, null, [new CodeNode(()=>
                        {
                            CurrentTypeWriter.Write(node, "[");
                            int ix = 0;
                            foreach(var b in bits)
                            {
                                if (ix > 0)
                                    CurrentTypeWriter.Write(node, ", ");
                                CurrentTypeWriter.Write(node, b.ToString());
                                ix++;
                            }
                            CurrentTypeWriter.Write(node, "]");
                        })]);
                    }
                    else
                    {
                        var doubleType = (ITypeSymbol)_global.GetSymbol("double", this/*, out _, out _*/);
                        var doubleBasedConstructor = decimalType.GetMembers(".ctor").Cast<IMethodSymbol>().SingleOrDefault(e => e.Parameters.Count() == 1 && e.Parameters.Single().Type.Equals(doubleType, SymbolEqualityComparer.Default));
                        if (doubleBasedConstructor != null)
                        {
                            //WriteConstructorCall(node, decimalType, doubleBasedConstructor, null, [SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((double)idecimal)))]);
                            WriteConstructorCall(node, decimalType, doubleBasedConstructor, null, [new CodeNode(() =>
                            {
                                CurrentTypeWriter.Write(node, ((double)idecimal).ToString());
                            })]);
                        }
                    }
                }
            }
        }

        bool TryWriteConstant(CSharpSyntaxNode node, ITypeSymbol constantType, CSharpSyntaxNode? constantExpression, Optional<object?>? optionalConstantValue = null)
        {
            if (constantExpression == null && optionalConstantValue == null)
                throw new InvalidOperationException("One of constantExpression or optionalConstantValue is required");
            if (constantExpression != null && _factoryAssociationNode.TryGetValue(constantExpression, out var n))
            {
                constantExpression = (CSharpSyntaxNode)n;
            }
            var constantValue = optionalConstantValue ?? _global.EvaluateConstant(constantExpression!, this);
            if (constantValue.HasValue)
            {
                var valueType = constantValue.Value?.GetType();
                ITypeSymbol? valueTypeSymbol = valueType != null && valueType.IsValueType ? (ITypeSymbol?)_global.TryGetSymbol(valueType.FullName, this) : null;
                bool needBoxing = (valueType?.IsValueType ?? false) && valueTypeSymbol != null && SymbolEqualityComparer.Default.Equals(constantType, _global.SystemObject);
                if (needBoxing)
                {
                    CurrentTypeWriter.Write(node, _global.GlobalName);
                    CurrentTypeWriter.Write(node, ".");
                    CurrentTypeWriter.Write(node, Constants.BoxName);
                    CurrentTypeWriter.Write(node, "(");
                }
                //var longType = (INamedTypeSymbol)_global.GetTypeSymbol("long", this, out _, out _);
                //if (longType.Equals(constantType, SymbolEqualityComparer.Default))
                if (constantType.SpecialType == SpecialType.System_Int64 || constantValue.Value is long)
                {
                    int _base = 10;
                    if (constantExpression.IsKind(SyntaxKind.NumericLiteralExpression) && constantExpression is LiteralExpressionSyntax lt)
                    {
                        if (lt.Token.Text.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                            _base = 16;
                        else if (lt.Token.Text.StartsWith("0b", StringComparison.InvariantCultureIgnoreCase))
                            _base = 2;
                    }
                    WriteLongConstant(node, constantValue.Value is ulong ? (long)(ulong)constantValue.Value : constantValue.Value is long ? (long)constantValue.Value : Convert.ToInt64(constantValue.Value!), _base);
                }
                else if (constantType.SpecialType == SpecialType.System_UInt64 || constantValue.Value is ulong)
                {
                    int _base = 10;
                    if (constantExpression.IsKind(SyntaxKind.NumericLiteralExpression) && constantExpression is LiteralExpressionSyntax lt)
                    {
                        if (lt.Token.Text.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                            _base = 16;
                        else if (lt.Token.Text.StartsWith("0b", StringComparison.InvariantCultureIgnoreCase))
                            _base = 2;
                    }
                    WriteULongConstant(node, constantValue.Value is long ? (ulong)(long)constantValue.Value : constantValue.Value is ulong ? (ulong)constantValue.Value : Convert.ToUInt64(constantValue.Value!), _base);
                }
                else if (constantType.SpecialType == SpecialType.System_Decimal || constantValue.Value is decimal)
                {
                    //decimal handling
                    //Long literal is a different beast because js cannot actually handle it precisely
                    //This would have require us to estimate a constant expression in runtime instead of compile time
                    var val = constantValue.Value!.ToString();
                    WriteDecimalConstant(node, val);
                }
                else if ((constantExpression.IsKind(SyntaxKind.CharacterLiteralExpression) || constantType.SpecialType == SpecialType.System_Char) && constantValue.Value is char)
                {
                    var val = constantValue.Value!.ToString().GetLiteralString(SyntaxKind.CharacterLiteralExpression, _global);
                    CurrentTypeWriter.Write(node, val);
                }
                else
                {
                    if (constantType.SpecialType == SpecialType.System_String && constantValue.Value != null)
                        CurrentTypeWriter.Write(node, "\"");
                    if (constantValue.Value == null)
                    {
                        if (constantType.IsValueType && !constantType.IsNullable(out _))
                        {
                            CurrentTypeWriter.Write(node, _global.GetDefaultValue(constantType, true)!);
                        }
                        else
                            CurrentTypeWriter.Write(node, "null");
                    }
                    else
                    {
                        if (constantType.SpecialType == SpecialType.System_Boolean || constantValue.Value is bool)
                            CurrentTypeWriter.Write(node, (bool)constantValue.Value ? "true" : "false");
                        else if (constantType.SpecialType == SpecialType.System_Char || constantValue.Value is char)
                        {
                            var c = (int)(char)constantValue.Value;
                            CurrentTypeWriter.Write(node, c.ToString());
                        }
                        else if (constantType.SpecialType == SpecialType.System_Double && constantValue.Value is double d && d == double.PositiveInfinity)
                        {
                            CurrentTypeWriter.Write(node, "Number.POSITIVE_INFINITY");
                        }
                        else if (constantType.SpecialType == SpecialType.System_Double && constantValue.Value is double d2 && d2 == double.NegativeInfinity)
                        {
                            CurrentTypeWriter.Write(node, "Number.NEGATIVE_INFINITY");
                        }
                        else if (constantType.SpecialType == SpecialType.System_Double && constantValue.Value is double d3 && double.IsNaN(d3))
                        {
                            CurrentTypeWriter.Write(node, "Number.NaN");
                        }
                        else if (constantType.SpecialType == SpecialType.System_Single && constantValue.Value is float f && f == float.PositiveInfinity)
                        {
                            CurrentTypeWriter.Write(node, "Number.POSITIVE_INFINITY");
                        }
                        else if (constantType.SpecialType == SpecialType.System_Single && constantValue.Value is float f2 && f2 == float.NegativeInfinity)
                        {
                            CurrentTypeWriter.Write(node, "Number.NEGATIVE_INFINITY");
                        }
                        else if (constantType.SpecialType == SpecialType.System_Single && constantValue.Value is float f3 && float.IsNaN(f3))
                        {
                            CurrentTypeWriter.Write(node, "Number.NaN");
                        }
                        else
                        {
                            var str = constantValue.Value.ToString();
                            str = str.EscapeString();
                            CurrentTypeWriter.Write(node, str);
                        }
                    }
                    if (constantType.SpecialType == SpecialType.System_String && constantValue.Value != null)
                        CurrentTypeWriter.Write(node, "\"");
                }
                if (needBoxing)
                {
                    CurrentTypeWriter.Write(node, ", ");
                    CurrentTypeWriter.Write(node, valueTypeSymbol!.ComputeOutputTypeName(_global));
                    CurrentTypeWriter.Write(node, ")");
                }
                return true;
            }
            return false;
        }
    }
}
