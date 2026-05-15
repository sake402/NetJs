using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial record class GlobalCompilationVisitor
    {
        ILLinkerAssembly.Type.Member GetLinkerMemeberSubstitution(string signature)
        {
            var members = Symbols.LinkerSubstitutions.SelectMany(s => s.Types.SelectMany(t => t.Members.Select(m => (t, m))));
            var matchingMember = members.LastOrDefault(m => m.t.NormalizedFullName + "." + m.m.NormalizedSignature == signature).m;
            return matchingMember;
        }

        public Optional<object?> EvaluateExpressionAsConstant(ExpressionSyntax expression, TranslatorSyntaxVisitor visitor)
        {
            var cValue = EvaluateConstant(expression, visitor);
            if (cValue.HasValue)
                return cValue;
            var symbol = TryGetSymbol(expression, visitor);
            if (symbol != null)
            {
                var template = symbol.GetTemplateAttribute(this);
                if (template != null && template.ConstructorArguments.Length > 0)
                {
                    var arg = (string?)template.ConstructorArguments[0].Value;
                    var val = arg?.RemoveComments();
                    if (val != null && (bool.TryParse(val, out _) || double.TryParse(val, out _) || (val.Length >= 2 && val[0] == '"' && val[val.Length - 1] == '"')))
                    {
                        return new Optional<object?>(val);
                    }
                }
                //var metadata = GetMetadata(symbol);
                //if (metadata != null)
                //{
                var signature = symbol.ToString();// metadata.Signature;
                var matchingMember = GetLinkerMemeberSubstitution(signature);
                if (matchingMember != null && matchingMember.Body == "stub")
                {
                    return new Optional<object?>(matchingMember.Value);
                }
                //}
            }
            return new Optional<object?>();
        }

        public bool? EvaluateConditionalExpressionAsConstant(ExpressionSyntax expression, TranslatorSyntaxVisitor visitor, out ExpressionSyntax rewritten)
        {
            var cValue = EvaluateExpressionAsConstant(expression, visitor);
            if (cValue.HasValue)
            {
                if (cValue.Value is bool b)
                {
                    rewritten = SyntaxFactory.LiteralExpression(b ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
                    return b;
                }
                if (cValue.Value is string str)
                {
                    if (str == "true")
                    {
                        rewritten = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                        return true;
                    }
                    if (str == "false")
                    {
                        rewritten = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                        return false;
                    }
                }
            }
            if (expression is BinaryExpressionSyntax binary)
            {
                var op = binary.OperatorToken.ValueText;
                var cLeft = EvaluateExpressionAsConstant(binary.Left, visitor);
                var cRight = EvaluateExpressionAsConstant(binary.Right, visitor);
                double AsDouble(object o)
                {
                    if (o is int i)
                        return i;
                    if (o is uint u)
                        return u;
                    if (o is float f)
                        return f;
                    if (o is double d)
                        return d;
                    if (o is bool b)
                        return b ? 1 : 0;
                    if (o is string s && double.TryParse(s, out d))
                        return d;
                    throw new InvalidOperationException($"Unsupported conditional compilation expression type of {o.GetType()}");
                }
                if (cLeft.HasValue && cRight.HasValue && cLeft.Value != null && cRight.Value != null)
                {
                    bool? result = null;
                    switch (op)
                    {
                        case ">":
                            result = AsDouble(cLeft.Value) > AsDouble(cRight.Value);
                            break;
                        case ">=":
                            result = AsDouble(cLeft.Value) >= AsDouble(cRight.Value);
                            break;
                        case "<":
                            result = AsDouble(cLeft.Value) < AsDouble(cRight.Value);
                            break;
                        case "<=":
                            result = AsDouble(cLeft.Value) <= AsDouble(cRight.Value);
                            break;
                        case "==":
                            result = AsDouble(cLeft.Value) == AsDouble(cRight.Value);
                            break;
                        case "!=":
                            result = AsDouble(cLeft.Value) != AsDouble(cRight.Value);
                            break;
                    }
                    if (result != null)
                    {
                        rewritten = SyntaxFactory.LiteralExpression(result.Value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
                        return result.Value;
                    }
                }
                var left = EvaluateConditionalExpressionAsConstant(binary.Left, visitor, out var leftReWrite);
                var right = EvaluateConditionalExpressionAsConstant(binary.Right, visitor, out var rightReWrite);
                ExpressionSyntax RewiteBinaryExpression()
                {
                    ExpressionSyntax rwLeft = leftReWrite;
                    ExpressionSyntax rwRight = rightReWrite;
                    if (left == true)
                    {
                        rwLeft = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                    }
                    else if (left == false)
                    {
                        rwLeft = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                    }
                    if (right == true)
                    {
                        rwRight = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                    }
                    else if (right == false)
                    {
                        rwRight = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                    }
                    return SyntaxFactory.BinaryExpression(binary.Kind(), rwLeft, rwRight);
                }
                if (left != null || right != null)
                {
                    switch (op)
                    {
                        case "|":
                            if (left == true)
                            {
                                rewritten = RewiteBinaryExpression();
                                return true;
                            }
                            if (right == true)
                            {
                                rewritten = RewiteBinaryExpression();
                                return true;
                            }
                            if (left == false && right == false)
                            {
                                rewritten = RewiteBinaryExpression();
                                return false;
                            }
                            break;
                        case "||":
                            if (left == true)
                            {
                                rewritten = RewiteBinaryExpression();
                                return true;
                            }
                            if (right == true)
                            {
                                rewritten = RewiteBinaryExpression();
                                return true;
                            }
                            if (left == false && right == false)
                            {
                                rewritten = RewiteBinaryExpression();
                                return false;
                            }
                            break;
                        case "&":
                            if (left == false)
                            {
                                rewritten = RewiteBinaryExpression();
                                return false;
                            }
                            if (right == false)
                            {
                                rewritten = RewiteBinaryExpression();
                                return false;
                            }
                            if (left == true && right == true)
                            {
                                rewritten = RewiteBinaryExpression();
                                return true;
                            }
                            break;
                        case "&&":
                            if (left == false)
                            {
                                rewritten = RewiteBinaryExpression();
                                return false;
                            }
                            if (right == false)
                            {
                                rewritten = RewiteBinaryExpression();
                                return false;
                            }
                            if (left == true && right == true)
                            {
                                rewritten = RewiteBinaryExpression();
                                return true;
                            }
                            break;
                        case "^" when left != null && right != null:
                            {
                                var value = left.Value ^ right.Value;
                                rewritten = RewiteBinaryExpression();
                                return value;
                            }
                    }
                }
                rewritten = expression;
                return null;
            }
            else if (expression is ParenthesizedExpressionSyntax pr)
            {
                return EvaluateConditionalExpressionAsConstant(pr.Expression, visitor, out rewritten);
            }
            else if (expression is PrefixUnaryExpressionSyntax un && un.IsKind(SyntaxKind.LogicalNotExpression))
            {
                var t = EvaluateConditionalExpressionAsConstant(un.Operand, visitor, out rewritten);
                if (t == true)
                    return false;
                if (t == false)
                    return true;
            }
            rewritten = expression;
            return null;
        }

        public bool LinkTrimOutMethod(IMethodSymbol method)
        {
            var att = method.GetAttributes().Where(a => a.AttributeClass?.Name == "CompExactlyDependsOnAttribute");
            if (!att.Any())
                return false;
            return !att.Any(a =>
            {
                var type = (INamedTypeSymbol)a.ConstructorArguments.Single().Value!;
                var signature = $"{type}.IsSupported";
                var member = GetLinkerMemeberSubstitution(signature);
                if (member?.Body != "stub") return false;
                return member.Value == "true";
            });
        }
    }
}
