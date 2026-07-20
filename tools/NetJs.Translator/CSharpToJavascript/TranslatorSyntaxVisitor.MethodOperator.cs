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
        public const string ExplicitOperatorName = "op_Explicit";
        public const string ImplicitOperatorName = "op_Implicit";
        //operators we can safely rewite like a += b => a = a + b
        static readonly string[] RewitableOperators = ["+=", "-=", "*=", "/=", "%=", ">>=", "<<=", "|=", "&=", "^="];
        public bool TryInvokeMethodOperator(CSharpSyntaxNode node,
            string _operator, 
            ITypeSymbol? leftOperandType, 
            CSharpSyntaxNode? leftOperand,
            ITypeSymbol? rightOperandType,
            IEnumerable<CSharpSyntaxNode> arguments, 
            Action? prologue = null)
        {
            var conversion = node.FindClosestParent<ConversionOperatorDeclarationSyntax>();
            var conversionMethod = conversion != null ? _global.GetSymbol(conversion, this) : null;
            var rightOperand = arguments.FirstOrDefault();
            if (leftOperand != null)
            {
                if (leftOperandType == null)
                {
                    leftOperandType = _global.TryGetTypeSymbol(leftOperand, this);
                }
                if (leftOperand == rightOperand)
                {
                    rightOperand = arguments.Last();
                    rightOperandType ??= _global.TryGetTypeSymbol(rightOperand, this);
                }
            }
            else if (leftOperandType == null)
            {
                leftOperand = arguments.First();
                rightOperand = arguments.Last();
                leftOperandType = _global.TryGetTypeSymbol(leftOperand, this);
                rightOperandType ??= _global.TryGetTypeSymbol(rightOperand, this);
                leftOperandType = leftOperandType ?? rightOperandType;
            }
            if (rightOperandType == null && rightOperand != null)
            {
                rightOperandType = _global.TryGetTypeSymbol(rightOperand, this);
            }
            //js can handle native numeric operation, no need to call operator
            if (leftOperandType != null &&
                rightOperandType != null &&
                (leftOperandType.IsNumberNumericType() || leftOperandType.IsLongNumericType()) &&
                (rightOperandType.IsNumberNumericType() || rightOperandType.IsLongNumericType()))
                return false;
            bool IsAssignmentRewriteCandidate()
            {
                return true;
                //if (!leftOperand.IsKind(SyntaxKind.IdentifierName))
                //    return true;
                ////JS is funny. Without an operator to overload it, These expressions will return a number
                ////b = true;
                ////b &= false; //n becomes 0 number
                ////We override this behaviour using boolean operator
                //if (leftOperandType.IsJsBoolean())
                //    return true;
                //if (!leftOperandType.IsJsPrimitive())
                //    return true;
                ////we always need to rewite integer division so we can call the operator to truncate the remainder
                //if (_operator == "/=" && leftOperandType.IsJsNativeIntegerNumeric())
                //    return true;
                //return false;
            }
            Action? localPrologue = null;
            Action? localEpilogue = null;
            ///if we have an operator such as a+=b and the datatype is not a jsprimitive, rewrite it to a=a+b and visit it
            if (RewitableOperators.Contains(_operator) && leftOperand != null && leftOperandType != null && rightOperand != null && IsAssignmentRewriteCandidate())
            {
                localPrologue = () =>
                {
                    Visit(leftOperand);
                    CurrentTypeWriter.Write(node, " = ");
                };
                _operator = _operator.Substring(0, _operator.Length - 1);
                arguments = [leftOperand, rightOperand];

                //var kind = _operator switch
                //{
                //    "+=" => SyntaxKind.AddExpression,
                //    "-=" => SyntaxKind.SubtractExpression,
                //    "*=" => SyntaxKind.MultiplyExpression,
                //    "=" => SyntaxKind.DivideExpression,
                //    "%=" => SyntaxKind.ModuloExpression,
                //    "|=" => SyntaxKind.BitwiseOrExpression,
                //    "&=" => SyntaxKind.BitwiseAndExpression,
                //    "^=" => SyntaxKind.ExclusiveOrExpression,
                //    ">>=" => SyntaxKind.RightShiftExpression,
                //    "<<=" => SyntaxKind.LeftShiftExpression,
                //    _ => SyntaxKind.None
                //};
                //if (kind != SyntaxKind.None)
                //{
                //    var newNode = SyntaxFactory.AssignmentExpression(
                //        SyntaxKind.SimpleAssignmentExpression,
                //        leftOperand.WithoutLeadingTrivia().WithoutTrailingTrivia(),
                //        SyntaxFactory.BinaryExpression(
                //            kind,
                //            leftOperand.WithoutLeadingTrivia().WithoutTrailingTrivia(),
                //            rightOperand.WithoutLeadingTrivia().WithoutTrailingTrivia())
                //        );
                //    var mnewNode = (ExpressionStatementSyntax)node.Parent!.ReplaceNode(node, newNode)!;
                //    Visit(mnewNode.Expression);
                //    return true;
                //}
            }
            else if (_operator == "++" || _operator == "--")
            {
                if (node.IsKind(SyntaxKind.PreIncrementExpression)) //++value
                {
                    localPrologue = () =>
                    {
                        Visit(leftOperand);
                        CurrentTypeWriter.Write(node, " = ");
                    };
                }
                else if (node.IsKind(SyntaxKind.PostIncrementExpression)) //value++ => (()=>{ var oldp = value; value = value+1; return oldp; })()
                {
                    localPrologue = () =>
                    {
                        CurrentTypeWriter.WriteLine(node, $"(() => //{leftOperand}++");
                        CurrentTypeWriter.WriteLine(node, "{", true);
                        CurrentTypeWriter.Write(node, "const $old = ", true);
                        Visit(leftOperand);
                        CurrentTypeWriter.WriteLine(node, ";");
                        CurrentTypeWriter.Write(node, "", true);
                        Visit(rightOperand);
                        CurrentTypeWriter.Write(node, " = ");
                    };
                    localEpilogue = () =>
                    {
                        CurrentTypeWriter.WriteLine(node, ";");
                        CurrentTypeWriter.WriteLine(node, "return $old;", true);
                        CurrentTypeWriter.Write(node, "})()", true);
                    };
                }
            }
            if (leftOperandType is ITypeSymbol ts)
            {
                if (ts.IsNullable(out var t))
                    ts = t!;
                bool isChecked = _global.Evaluate("checked", this) != null;
                List<IMethodSymbol>? operators = null;
                if (isChecked)
                {
                    operators = ts.GetMembers(_operator.ResolveOperatorMethodName(arguments.Count(), true), _global, false/*operator must be defined on the containing type, not inherited*/).Cast<IMethodSymbol>().ToList();
                }
                if (operators == null || !operators.Any())
                    operators = ts.GetMembers(_operator.ResolveOperatorMethodName(arguments.Count()), _global, false/*operator must be defined on the containing type, not inherited*/).Cast<IMethodSymbol>().ToList();
                if (_operator == ExplicitOperatorName || _operator == ImplicitOperatorName)
                {
                    operators = operators.Where(e => e.ReturnType.Equals(leftOperandType, SymbolEqualityComparer.Default) && e.Parameters.First().Type.Equals(rightOperandType, SymbolEqualityComparer.Default)).ToList();
                }
                var operatorMethod = GetBestOverloadMethod(ts, operators, null, arguments, null, out _);
                //fix recursive conversion call within itself
                if (conversionMethod != null && SymbolEqualityComparer.Default.Equals(operatorMethod, conversionMethod))
                    return false;
                if (operatorMethod is IMethodSymbol ms && ms.IsInvokable(_global))
                {
                    prologue?.Invoke();
                    localPrologue?.Invoke();
                    //If the operator is defined static on a Typeparameter, we make sure we write it as T.method()
                    WriteMethodInvocation(node, ms, null, arguments.Select(a => new CodeNode(a)), ms.IsStatic && ts is ITypeParameterSymbol ttp ? new CodeNode(() =>
                    {
                        CurrentTypeWriter.Write(node, ts.Name);
                    }) : null, ms.IsStatic ? ts as ITypeParameterSymbol : null, null, false);
                    localEpilogue?.Invoke();
                    return true;
                }
            }
            //if the left is just a literal that could have being converted to any other numeric type
            //find the operator on the rhs then
            if (leftOperand is LiteralExpressionSyntax && rightOperand != null)
            {
                if (rightOperandType is ITypeSymbol rts)
                {
                    if (rts.IsNullable(out var t))
                        rts = t!;
                    bool isChecked = _global.Evaluate("checked", this) != null;
                    List<IMethodSymbol>? operators = null;
                    if (isChecked)
                    {
                        operators = rts.GetMembers(_operator.ResolveOperatorMethodName(arguments.Count(), true), _global, false/*operator must be defined on the containing type, not inherited*/).Cast<IMethodSymbol>().ToList();
                    }
                    if (operators == null || !operators.Any())
                        operators = rts.GetMembers(_operator.ResolveOperatorMethodName(arguments.Count()), _global, false/*operator must be defined on the containing type, not inherited*/).Cast<IMethodSymbol>().ToList();
                    //here the arguments needs to become the lhs itself
                    var operatorMethod = GetBestOverloadMethod(rts, operators, null, arguments, null, out _);
                    //fix recursive conversion call within itself
                    if (conversionMethod != null && SymbolEqualityComparer.Default.Equals(operatorMethod, conversionMethod))
                        return false;
                    if (operatorMethod is IMethodSymbol ms && ms.IsInvokable(_global))
                    {
                        prologue?.Invoke();
                        localPrologue?.Invoke();
                        WriteMethodInvocation(node, ms, null, arguments.Select(a => new CodeNode(a)), null, null, null, false);
                        localEpilogue?.Invoke();
                        return true;
                    }
                }
            }
            if (_operator == ExplicitOperatorName || _operator == ImplicitOperatorName) //the operator may be defined on the rhs
            {
                if (rightOperandType is ITypeSymbol rts)
                {
                    if (rts.IsNullable(out var t))
                        rts = t!;
                    bool isChecked = _global.Evaluate("checked", this) != null;
                    List<IMethodSymbol>? operators = null;
                    if (isChecked)
                    {
                        operators = rts.GetMembers(_operator.ResolveOperatorMethodName(arguments.Count(), true), _global, false/*operator must be defined on the containing type, not inherited*/).Cast<IMethodSymbol>().ToList();
                    }
                    if (operators == null || !operators.Any())
                        operators = rts.GetMembers(_operator.ResolveOperatorMethodName(arguments.Count()), _global, false/*operator must be defined on the containing type, not inherited*/).Cast<IMethodSymbol>().ToList();
                    operators = operators.Where(e => e.ReturnType.Equals(leftOperandType, SymbolEqualityComparer.Default) && e.Parameters.First().Type.Equals(rightOperandType, SymbolEqualityComparer.Default)).ToList();
                    var operatorMethod = GetBestOverloadMethod(rts, operators, null, arguments, null, out _);
                    //fix recursive conversion call within itself
                    if (conversionMethod != null && SymbolEqualityComparer.Default.Equals(operatorMethod, conversionMethod))
                        return false;
                    if (operatorMethod is IMethodSymbol ms && ms.IsInvokable(_global))
                    {
                        prologue?.Invoke();
                        localPrologue?.Invoke();
                        WriteMethodInvocation(node, ms, null, arguments.Select(a => new CodeNode(a)), null, null, null, false);
                        localEpilogue?.Invoke();
                        return true;
                    }
                }
            }
            if (_operator == ExplicitOperatorName)
            {
                //(double)long
                //if we get here, its because we cant find an explicit operator on both lhs(double) and rhs(long) that satisfy this conversion
                //But if there is an implicit converter operator defined, the the cast was not actually neccessary and we can do it implicitly
                //Call this method again with implicit operator
                return TryInvokeMethodOperator(node, ImplicitOperatorName, leftOperandType, leftOperand, rightOperandType, arguments, prologue);
            }
            return false;
        }
    }
}
