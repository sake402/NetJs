using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.String
{
    sealed class NativeStringMethodsSyntaxEmitter : SyntaxEmitter<InvocationExpressionSyntax>
    {
        public override bool TryEmit(InvocationExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            var method = visitor.Global.TryGetSymbol(node, visitor) as IMethodSymbol;
            if (method != null && method.ContainingType.SpecialType == SpecialType.System_String)
            {
                CodeNode WriteCharAsString(ExpressionSyntax arg)
                {
                    return new CodeNode(() =>
                    {
                        if (arg.IsKind(SyntaxKind.CharacterLiteralExpression))
                        {
                            var txt = ((LiteralExpressionSyntax)arg).Token.ValueText;
                            visitor.CurrentTypeWriter.Write(node, $"\"{(txt[0] == '\\' ? "\\" : "")}{txt}\"");
                        }
                        else
                        {
                            var val = visitor.Global.TryGetSymbol(arg, visitor);
                            if (val is IFieldSymbol fs && fs.IsConst)
                            {
                                var txt = ((char)fs.ConstantValue!).ToString();
                                visitor.CurrentTypeWriter.Write(node, $"\"{(txt[0] == '\\' ? "\\" : "")}{txt}\"");
                            }
                            else
                            {
                                visitor.CurrentTypeWriter.Write(node, $"String.fromCharCode(");
                                visitor.Visit(arg);
                                visitor.CurrentTypeWriter.Write(node, $")");
                            }
                        }
                    });
                }
                var lhs = node.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) ? ((MemberAccessExpressionSyntax)node.Expression).Expression :
                    node.Expression.IsKind(SyntaxKind.IdentifierName) ? new CodeNode(() => visitor.CurrentTypeWriter.Write(node, "this")) : null;
                if (method.Name == nameof(string.Substring) &&
                    node.ArgumentList.Arguments.Count == 1)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeSlice", lhsExpression: lhs, arguments: [node.ArgumentList.Arguments[0].Expression], methodFilter: m => m.Parameters.Length == 1);
                    return true;
                }
                else if (method.Name == nameof(string.Substring) &&
                    node.ArgumentList.Arguments.Count == 2)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeSubstring", lhsExpression: lhs, arguments: [node.ArgumentList.Arguments[0].Expression, node.ArgumentList.Arguments[1].Expression]);
                    return true;
                }
                else if (method.Name == nameof(string.ToLower) &&
                    node.ArgumentList.Arguments.Count == 0)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeToLocaleLower", lhsExpression: lhs);
                    return true;
                }
                else if (method.Name == nameof(string.ToUpper) &&
                    node.ArgumentList.Arguments.Count == 0)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeToLocaleUpper", lhsExpression: lhs);
                    return true;
                }
                else if (method.Name == nameof(string.ToLowerInvariant) &&
                    node.ArgumentList.Arguments.Count == 0)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeToLower", lhsExpression: lhs);
                    return true;
                }
                else if (method.Name == nameof(string.ToUpperInvariant) &&
                    node.ArgumentList.Arguments.Count == 0)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeToUpper", lhsExpression: lhs);
                    return true;
                }
                else if (method.Name == nameof(string.Trim) &&
                    node.ArgumentList.Arguments.Count == 0)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeTrim", lhsExpression: lhs);
                    return true;
                }
                else if (method.Name == nameof(string.TrimStart) &&
                    node.ArgumentList.Arguments.Count == 0)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeTrimStart", lhsExpression: lhs);
                    return true;
                }
                else if (method.Name == nameof(string.TrimEnd) &&
                    node.ArgumentList.Arguments.Count == 0)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeTrimEnd", lhsExpression: lhs);
                    return true;
                }
                else if (method.Name == nameof(string.StartsWith) &&
                    node.ArgumentList.Arguments.Count == 1 &&
                    visitor.Global.TryGetTypeSymbol(node.ArgumentList.Arguments[0], visitor)?.SpecialType == SpecialType.System_String)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeStartsWith", lhsExpression: lhs, arguments: [node.ArgumentList.Arguments[0].Expression]);
                    return true;
                }
                else if (method.Name == nameof(string.StartsWith) &&
                    node.ArgumentList.Arguments.Count == 1 &&
                    visitor.Global.TryGetTypeSymbol(node.ArgumentList.Arguments[0], visitor)?.SpecialType == SpecialType.System_Char)
                {
                    var arg = node.ArgumentList.Arguments[0].Expression;
                    visitor.WriteMethodInvocation(node, "System.String.NativeStartsWith", lhsExpression: lhs, arguments: [WriteCharAsString(arg)]);
                    return true;
                }
                else if (method.Name == nameof(string.EndsWith) &&
                    node.ArgumentList.Arguments.Count == 1 &&
                    visitor.Global.TryGetTypeSymbol(node.ArgumentList.Arguments[0], visitor)?.SpecialType == SpecialType.System_String)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeEndsWith", lhsExpression: lhs, arguments: [node.ArgumentList.Arguments[0].Expression]);
                    return true;
                }
                else if (method.Name == nameof(string.EndsWith) &&
                    node.ArgumentList.Arguments.Count == 1 &&
                    visitor.Global.TryGetTypeSymbol(node.ArgumentList.Arguments[0], visitor)?.SpecialType == SpecialType.System_Char)
                {
                    var arg = node.ArgumentList.Arguments[0].Expression;
                    visitor.WriteMethodInvocation(node, "System.String.NativeEndsWith", lhsExpression: lhs, arguments: [WriteCharAsString(arg)]);
                    return true;
                }
                else if (method.Name == nameof(string.Replace) &&
                    node.ArgumentList.Arguments.Count == 2 &&
                    visitor.Global.TryGetTypeSymbol(node.ArgumentList.Arguments[0], visitor)?.SpecialType == SpecialType.System_String &&
                    visitor.Global.TryGetTypeSymbol(node.ArgumentList.Arguments[1], visitor)?.SpecialType == SpecialType.System_String)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeReplaceAll", lhsExpression: lhs, arguments: [node.ArgumentList.Arguments[0].Expression, node.ArgumentList.Arguments[1].Expression]);
                    return true;
                }
                else if (method.Name == nameof(string.IndexOf) &&
                    node.ArgumentList.Arguments.Count == 1 &&
                    visitor.Global.TryGetTypeSymbol(node.ArgumentList.Arguments[0], visitor)?.SpecialType == SpecialType.System_String)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeIndexOf", lhsExpression: lhs, arguments: [node.ArgumentList.Arguments[0].Expression]);
                    return true;
                }
                else if (method.Name == nameof(string.IndexOf) &&
                    node.ArgumentList.Arguments.Count == 2 &&
                    visitor.Global.TryGetTypeSymbol(node.ArgumentList.Arguments[0], visitor)?.SpecialType == SpecialType.System_String &&
                    visitor.Global.TryGetTypeSymbol(node.ArgumentList.Arguments[1], visitor)?.SpecialType == SpecialType.System_Int32)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeIndexOf", lhsExpression: lhs, arguments: [node.ArgumentList.Arguments[0].Expression, node.ArgumentList.Arguments[1].Expression], methodFilter: m => m.Parameters.Length == 2);
                    return true;
                }
                else if (method.Name == nameof(string.LastIndexOf) &&
                    node.ArgumentList.Arguments.Count == 1 &&
                    visitor.Global.TryGetTypeSymbol(node.ArgumentList.Arguments[0], visitor)?.SpecialType == SpecialType.System_String)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeLastIndexOf", lhsExpression: lhs, arguments: [node.ArgumentList.Arguments[0].Expression], methodFilter: m => m.Parameters.Length == 1);
                    return true;
                }
                else if (method.Name == nameof(string.Contains) &&
                    node.ArgumentList.Arguments.Count == 1 &&
                    visitor.Global.TryGetTypeSymbol(node.ArgumentList.Arguments[0], visitor)?.SpecialType == SpecialType.System_String)
                {
                    visitor.WriteMethodInvocation(node, "System.String.NativeContains", lhsExpression: lhs, arguments: [node.ArgumentList.Arguments[0].Expression], methodFilter: m => m.Parameters.Length == 1);
                    return true;
                }
                else if (method.Name == nameof(string.Contains) &&
                    node.ArgumentList.Arguments.Count == 1 &&
                    visitor.Global.TryGetTypeSymbol(node.ArgumentList.Arguments[0], visitor)?.SpecialType == SpecialType.System_Char)
                {
                    var arg = node.ArgumentList.Arguments[0].Expression;
                    visitor.WriteMethodInvocation(node, "System.String.NativeContains", lhsExpression: lhs, arguments: [WriteCharAsString(arg)], methodFilter: m => m.Parameters.Length == 1);
                    return true;
                }
                else if (method.Name == nameof(string.Split) &&
                    node.ArgumentList.Arguments.Count == 1 &&
                    visitor.Global.TryGetTypeSymbol(node.ArgumentList.Arguments[0], visitor)?.SpecialType == SpecialType.System_Char)
                {
                    var arg = node.ArgumentList.Arguments[0].Expression;
                    visitor.WriteMethodInvocation(node, "System.Array.AddMetadata", arguments: [new CodeNode(() =>
                    {
                        visitor.WriteMethodInvocation(node, "System.String.NativeSplit", lhsExpression: lhs, arguments: [WriteCharAsString(arg)], methodFilter: m => m.Parameters.Length == 1);
                    }), new CodeNode(() =>
                    {
                         visitor.CurrentTypeWriter.Write(node, visitor.Global.SystemString.ComputeOutputTypeName(visitor.Global));
                    })]);
                    return true;
                }
            }
            return false;
        }
    }
}
