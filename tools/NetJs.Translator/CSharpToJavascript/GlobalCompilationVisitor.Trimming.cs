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
        private Dictionary<string, ILLinkerAssembly.Type.Member>? _linkerSubstitutionsIndex;

        void InitializeLinkerSubstitutionsIndex()
        {
            var index = new Dictionary<string, ILLinkerAssembly.Type.Member>(StringComparer.Ordinal);

            foreach (var substitution in Symbols.LinkerSubstitutions.Concat(ImportedNames.LinkerSubstitutions))
            {
                if (substitution.Types != null)
                {
                    foreach (var type in substitution.Types)
                    {
                        var typePrefix = type.NormalizedFullName + ".";

                        foreach (var member in type.Members)
                        {
                            var fullSignature = typePrefix + member.NormalizedSignature;

                            // Overwriting guarantees that the last one added wins
                            index[fullSignature] = member;
                        }
                    }
                }
            }

            _linkerSubstitutionsIndex = index;
        }
        public ILLinkerAssembly.Type.Member GetLinkerMemeberSubstitution(string signature)
        {
            if (_linkerSubstitutionsIndex == null)
            {
                InitializeLinkerSubstitutionsIndex();
            }

            if (_linkerSubstitutionsIndex!.TryGetValue(signature, out var matchingMember))
            {
                return matchingMember;
            }

            return null!;
        }

        public Optional<object?> EvaluateExpressionAsConstant(ExpressionSyntax expression, TranslatorSyntaxVisitor visitor)
        {
            var cValue = EvaluateConstant(expression, visitor);
            if (cValue.HasValue)
            {
                return cValue;
            }

            var symbol = TryGetSymbol(expression, visitor);
            if (symbol != null)
            {
                var template = symbol.GetTemplateAttribute(this, null);
                if (template != null && template.ConstructorArguments.Length > 0)
                {
                    var arg = (string?)template.ConstructorArguments[0].Value;
                    var val = arg?.RemoveComments();

                    if (!string.IsNullOrEmpty(val))
                    {
                        ReadOnlySpan<char> valSpan = val.AsSpan();

                        if (bool.TryParse(val, out _) ||
                            double.TryParse(val, out _) ||
                            (valSpan.Length >= 2 && valSpan[0] == '"' && valSpan[valSpan.Length - 1] == '"'))
                        {
                            return new Optional<object?>(val);
                        }
                    }
                }

                var signature = symbol.CreateSignature(this, withGlobalNamespace: false);

                var matchingMember = GetLinkerMemeberSubstitution(signature);
                if (matchingMember != null && matchingMember.Body == "stub")
                {
                    return new Optional<object?>(matchingMember.Value);
                }
            }

            return default;
        }

        //public Optional<object?> EvaluateExpressionAsConstant(ExpressionSyntax expression, TranslatorSyntaxVisitor visitor)
        //{
        //    var cValue = EvaluateConstant(expression, visitor);
        //    if (cValue.HasValue)
        //        return cValue;
        //    var symbol = TryGetSymbol(expression, visitor);
        //    if (symbol != null)
        //    {
        //        var template = symbol.GetTemplateAttribute(this, null);
        //        if (template != null && template.ConstructorArguments.Length > 0)
        //        {
        //            var arg = (string?)template.ConstructorArguments[0].Value;
        //            var val = arg?.RemoveComments();
        //            if (val != null && (bool.TryParse(val, out _) || double.TryParse(val, out _) || (val.Length >= 2 && val[0] == '"' && val[val.Length - 1] == '"')))
        //            {
        //                return new Optional<object?>(val);
        //            }
        //        }
        //        //var metadata = GetMetadata(symbol);
        //        //if (metadata != null)
        //        //{
        //        var signature = symbol.ToString();// metadata.Signature;
        //        var matchingMember = GetLinkerMemeberSubstitution(signature);
        //        if (matchingMember != null && matchingMember.Body == "stub")
        //        {
        //            return new Optional<object?>(matchingMember.Value);
        //        }
        //        //}
        //    }
        //    return new Optional<object?>();
        //}
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


            static bool TryToDouble(object o, out double value)
            {
                switch (o)
                {
                    case int i: value = i; return true;
                    case uint u: value = u; return true;
                    case float f: value = f; return true;
                    case double d: value = d; return true;
                    case bool b: value = b ? 1 : 0; return true;
                    case string s when double.TryParse(s, out double dParsed): value = dParsed; return true;
                    default: value = 0; return false;
                }
            }

            static ExpressionSyntax CreateRewrittenBinary(SyntaxKind binaryKind, bool? left, bool? right, ExpressionSyntax rwLeft, ExpressionSyntax rwRight)
            {
                if (left == true) rwLeft = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                else if (left == false) rwLeft = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

                if (right == true) rwRight = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                else if (right == false) rwRight = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

                return SyntaxFactory.BinaryExpression(binaryKind, rwLeft, rwRight);
            }

            if (expression is BinaryExpressionSyntax binary)
            {
                var kind = binary.Kind();

                // 1. Handle Arithmetic/Comparison Operators Directly
                if (kind is SyntaxKind.GreaterThanExpression or SyntaxKind.GreaterThanOrEqualExpression or
                            SyntaxKind.LessThanExpression or SyntaxKind.LessThanOrEqualExpression or
                            SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression)
                {
                    var cLeft = EvaluateExpressionAsConstant(binary.Left, visitor);
                    var cRight = EvaluateExpressionAsConstant(binary.Right, visitor);

                    if (cLeft.HasValue && cRight.HasValue && cLeft.Value != null && cRight.Value != null)
                    {
                        // Convert values cleanly without delegate allocation or hard crashes
                        if (TryToDouble(cLeft.Value, out double leftVal) && TryToDouble(cRight.Value, out double rightVal))
                        {
                            bool result = kind switch
                            {
                                SyntaxKind.GreaterThanExpression => leftVal > rightVal,
                                SyntaxKind.GreaterThanOrEqualExpression => leftVal >= rightVal,
                                SyntaxKind.LessThanExpression => leftVal < rightVal,
                                SyntaxKind.LessThanOrEqualExpression => leftVal <= rightVal,
                                SyntaxKind.EqualsExpression => leftVal == rightVal,
                                SyntaxKind.NotEqualsExpression => leftVal != rightVal,
                                _ => false
                            };

                            rewritten = SyntaxFactory.LiteralExpression(result ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
                            return result;
                        }
                    }
                }

                // 2. Handle Logical Short-Circuit Operators Recursively
                var left = EvaluateConditionalExpressionAsConstant(binary.Left, visitor, out var leftReWrite);

                // Optimization: Apply short-circuit optimization for || and && before checking the right branch
                if (kind is SyntaxKind.LogicalOrExpression && left == true)
                {
                    var rightDummy = EvaluateConditionalExpressionAsConstant(binary.Right, visitor, out var rightReWriteDummy);
                    rewritten = CreateRewrittenBinary(kind, left, rightDummy, leftReWrite, rightReWriteDummy);
                    return true;
                }
                if (kind is SyntaxKind.LogicalAndExpression && left == false)
                {
                    var rightDummy = EvaluateConditionalExpressionAsConstant(binary.Right, visitor, out var rightReWriteDummy);
                    rewritten = CreateRewrittenBinary(kind, left, rightDummy, leftReWrite, rightReWriteDummy);
                    return false;
                }

                // Evaluate the right side only if we didn't short-circuit
                var right = EvaluateConditionalExpressionAsConstant(binary.Right, visitor, out var rightReWrite);

                if (left != null || right != null)
                {
                    switch (kind)
                    {
                        case SyntaxKind.BitwiseOrExpression:
                        case SyntaxKind.LogicalOrExpression:
                            if (left == true || right == true)
                            {
                                rewritten = CreateRewrittenBinary(kind, left, right, leftReWrite, rightReWrite);
                                return true;
                            }
                            if (left == false && right == false)
                            {
                                rewritten = CreateRewrittenBinary(kind, left, right, leftReWrite, rightReWrite);
                                return false;
                            }
                            break;

                        case SyntaxKind.BitwiseAndExpression:
                        case SyntaxKind.LogicalAndExpression:
                            if (left == false || right == false)
                            {
                                rewritten = CreateRewrittenBinary(kind, left, right, leftReWrite, rightReWrite);
                                return false;
                            }
                            if (left == true && right == true)
                            {
                                rewritten = CreateRewrittenBinary(kind, left, right, leftReWrite, rightReWrite);
                                return true;
                            }
                            break;

                        case SyntaxKind.ExclusiveOrExpression:
                            if (left != null && right != null)
                            {
                                bool value = left.Value ^ right.Value;
                                rewritten = CreateRewrittenBinary(kind, left, right, leftReWrite, rightReWrite);
                                return value;
                            }
                            break;
                    }
                }

                rewritten = expression;
                return null;
            }

            if (expression is ParenthesizedExpressionSyntax pr)
            {
                return EvaluateConditionalExpressionAsConstant(pr.Expression, visitor, out rewritten);
            }

            if (expression is PrefixUnaryExpressionSyntax un && un.IsKind(SyntaxKind.LogicalNotExpression))
            {
                var t = EvaluateConditionalExpressionAsConstant(un.Operand, visitor, out rewritten);
                if (t == true) return false;
                if (t == false) return true;
            }

            rewritten = expression;
            return null;
        }

        //public bool? EvaluateConditionalExpressionAsConstant(ExpressionSyntax expression, TranslatorSyntaxVisitor visitor, out ExpressionSyntax rewritten)
        //{
        //    var cValue = EvaluateExpressionAsConstant(expression, visitor);
        //    if (cValue.HasValue)
        //    {
        //        if (cValue.Value is bool b)
        //        {
        //            rewritten = SyntaxFactory.LiteralExpression(b ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
        //            return b;
        //        }
        //        if (cValue.Value is string str)
        //        {
        //            if (str == "true")
        //            {
        //                rewritten = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
        //                return true;
        //            }
        //            if (str == "false")
        //            {
        //                rewritten = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
        //                return false;
        //            }
        //        }
        //    }
        //    if (expression is BinaryExpressionSyntax binary)
        //    {
        //        var op = binary.OperatorToken.ValueText;
        //        var cLeft = EvaluateExpressionAsConstant(binary.Left, visitor);
        //        var cRight = EvaluateExpressionAsConstant(binary.Right, visitor);
        //        double AsDouble(object o)
        //        {
        //            if (o is int i)
        //                return i;
        //            if (o is uint u)
        //                return u;
        //            if (o is float f)
        //                return f;
        //            if (o is double d)
        //                return d;
        //            if (o is bool b)
        //                return b ? 1 : 0;
        //            if (o is string s && double.TryParse(s, out d))
        //                return d;
        //            throw new InvalidOperationException($"Unsupported conditional compilation expression type of {o.GetType()}");
        //        }
        //        if (cLeft.HasValue && cRight.HasValue && cLeft.Value != null && cRight.Value != null)
        //        {
        //            bool? result = null;
        //            switch (op)
        //            {
        //                case ">":
        //                    result = AsDouble(cLeft.Value) > AsDouble(cRight.Value);
        //                    break;
        //                case ">=":
        //                    result = AsDouble(cLeft.Value) >= AsDouble(cRight.Value);
        //                    break;
        //                case "<":
        //                    result = AsDouble(cLeft.Value) < AsDouble(cRight.Value);
        //                    break;
        //                case "<=":
        //                    result = AsDouble(cLeft.Value) <= AsDouble(cRight.Value);
        //                    break;
        //                case "==":
        //                    result = AsDouble(cLeft.Value) == AsDouble(cRight.Value);
        //                    break;
        //                case "!=":
        //                    result = AsDouble(cLeft.Value) != AsDouble(cRight.Value);
        //                    break;
        //            }
        //            if (result != null)
        //            {
        //                rewritten = SyntaxFactory.LiteralExpression(result.Value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
        //                return result.Value;
        //            }
        //        }
        //        var left = EvaluateConditionalExpressionAsConstant(binary.Left, visitor, out var leftReWrite);
        //        var right = EvaluateConditionalExpressionAsConstant(binary.Right, visitor, out var rightReWrite);
        //        ExpressionSyntax RewiteBinaryExpression()
        //        {
        //            ExpressionSyntax rwLeft = leftReWrite;
        //            ExpressionSyntax rwRight = rightReWrite;
        //            if (left == true)
        //            {
        //                rwLeft = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
        //            }
        //            else if (left == false)
        //            {
        //                rwLeft = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
        //            }
        //            if (right == true)
        //            {
        //                rwRight = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
        //            }
        //            else if (right == false)
        //            {
        //                rwRight = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
        //            }
        //            return SyntaxFactory.BinaryExpression(binary.Kind(), rwLeft, rwRight);
        //        }
        //        if (left != null || right != null)
        //        {
        //            switch (op)
        //            {
        //                case "|":
        //                    if (left == true)
        //                    {
        //                        rewritten = RewiteBinaryExpression();
        //                        return true;
        //                    }
        //                    if (right == true)
        //                    {
        //                        rewritten = RewiteBinaryExpression();
        //                        return true;
        //                    }
        //                    if (left == false && right == false)
        //                    {
        //                        rewritten = RewiteBinaryExpression();
        //                        return false;
        //                    }
        //                    break;
        //                case "||":
        //                    if (left == true)
        //                    {
        //                        rewritten = RewiteBinaryExpression();
        //                        return true;
        //                    }
        //                    if (right == true)
        //                    {
        //                        rewritten = RewiteBinaryExpression();
        //                        return true;
        //                    }
        //                    if (left == false && right == false)
        //                    {
        //                        rewritten = RewiteBinaryExpression();
        //                        return false;
        //                    }
        //                    break;
        //                case "&":
        //                    if (left == false)
        //                    {
        //                        rewritten = RewiteBinaryExpression();
        //                        return false;
        //                    }
        //                    if (right == false)
        //                    {
        //                        rewritten = RewiteBinaryExpression();
        //                        return false;
        //                    }
        //                    if (left == true && right == true)
        //                    {
        //                        rewritten = RewiteBinaryExpression();
        //                        return true;
        //                    }
        //                    break;
        //                case "&&":
        //                    if (left == false)
        //                    {
        //                        rewritten = RewiteBinaryExpression();
        //                        return false;
        //                    }
        //                    if (right == false)
        //                    {
        //                        rewritten = RewiteBinaryExpression();
        //                        return false;
        //                    }
        //                    if (left == true && right == true)
        //                    {
        //                        rewritten = RewiteBinaryExpression();
        //                        return true;
        //                    }
        //                    break;
        //                case "^" when left != null && right != null:
        //                    {
        //                        var value = left.Value ^ right.Value;
        //                        rewritten = RewiteBinaryExpression();
        //                        return value;
        //                    }
        //            }
        //        }
        //        rewritten = expression;
        //        return null;
        //    }
        //    else if (expression is ParenthesizedExpressionSyntax pr)
        //    {
        //        return EvaluateConditionalExpressionAsConstant(pr.Expression, visitor, out rewritten);
        //    }
        //    else if (expression is PrefixUnaryExpressionSyntax un && un.IsKind(SyntaxKind.LogicalNotExpression))
        //    {
        //        var t = EvaluateConditionalExpressionAsConstant(un.Operand, visitor, out rewritten);
        //        if (t == true)
        //            return false;
        //        if (t == false)
        //            return true;
        //    }
        //    rewritten = expression;
        //    return null;
        //}

        public bool LinkTrimOutMethod(IMethodSymbol method)
        {
            return false;
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
