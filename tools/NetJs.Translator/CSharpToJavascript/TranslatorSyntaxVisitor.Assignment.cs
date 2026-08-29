using NetJs.Translator.CSharpToJavascript;
using NetJs.Translator.CSharpToJavascript.AssignmentConverter;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter;

namespace NetJs.Translator.CSharpToJavascript
{
    ////Convert likes of "ABC"u8 to ReadOnlySpan<byte>
    //public class Utf8StringToReadOnlySpanOfByteAssignmentConverter : IAssignmentConverter
    //{
    //    public Type ExpressionType => typeof(LiteralExpressionSyntax);

    //    public bool CanConvertTo(TranslatorSyntaxVisitor visitor, INamedTypeSymbol lhsType, CSharpSyntaxNode rhsExpression)
    //    {
    //        var literalExpression = (LiteralExpressionSyntax)rhsExpression;
    //        if (literalExpression.IsKind(SyntaxKind.Utf8StringLiteralExpression))
    //        {
    //            var readOnlySpan = (INamedTypeSymbol)visitor.Global.GetTypeSymbol("System.ReadOnlySpan<>", visitor);
    //            var ssbyte = (ITypeSymbol)visitor.Global.GetTypeSymbol("System.Byte", visitor);
    //            readOnlySpan = readOnlySpan.Construct(ssbyte);
    //            return lhsType.Equals(readOnlySpan, SymbolEqualityComparer.Default);
    //        }
    //        return false;
    //    }

    //    public void WriteAssignment(TranslatorSyntaxVisitor visitor, INamedTypeSymbol readOnlySpanType, CSharpSyntaxNode rhsExpression)
    //    {
    //        var literalExpression = (LiteralExpressionSyntax)rhsExpression;
    //        var bytes = literalExpression.Token.ValueText.ToArray().Select(e => ((int)e) & 0xFF);
    //        var constructor = readOnlySpanType.GetMembers(".ctor").Cast<IMethodSymbol>().Single(t => t.Parameters.Count() == 1 && t.Parameters[0].Type.IsArray(out _));
    //        visitor.WriteConstructorCall(rhsExpression, readOnlySpanType, constructor, null, [new CodeNode(() =>
    //        {
    //            //visitor.WriteCreateArray(rhsExpression, SyntaxFactory.ParseTypeName("System.Byte"), () =>
    //            //{
    //            //    visitor.Writer.Write(rhsExpression,"" );
    //            //}, null);
    //            visitor.Writer.Write(rhsExpression,"[");
    //            int ix = 0;
    //            foreach (var b in bytes)
    //            {
    //                if (ix > 0)
    //                    visitor.Writer.Write(rhsExpression,", ");
    //                visitor.Writer.Write(rhsExpression,b.ToString());
    //                ix++;
    //            }
    //            visitor.Writer.Write(rhsExpression,"]");
    //        })]);
    //    }
    //}


    public partial class TranslatorSyntaxVisitor
    {
        static IAssignmentConverter[] assignmentConverters =
        [
            new CollectionExpressionToReadOnlySpanAssignmentConverter(),
        ];

        public bool NeedBoxing(ITypeSymbol toType, ITypeSymbol fromType)
        {
            //In JS, everything is on the heap, even struct.
            //The only time we need boxing is if we are converting to object/interface a native JS primitive type that has a one to one maps to .Net primitive type
            //Or from a type parameter, the runtime will check if boxing is really neccessary for the type parameter
            if (!fromType.IsAbstract && (fromType.Kind == SymbolKind.TypeParameter || fromType.IsJsPrimitive()))
            {
                if (SymbolEqualityComparer.Default.Equals(toType, _global.SystemObject) ||
                    (toType.TypeKind == TypeKind.Interface && fromType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, toType))) ||
                    (toType.TypeKind == TypeKind.Interface && fromType.Kind == SymbolKind.TypeParameter))
                    return true;
            }
            //If casting an declared enum (a number) to Enum, cause a box too
            if (fromType.TypeKind == TypeKind.Enum && SymbolEqualityComparer.Default.Equals(toType, _global.SystemEnum))
            {
                return true;
            }
            return false;
        }

        public void WriteVariableAssignment(CSharpSyntaxNode node,
            CSharpSyntaxNode? lhsExpression,
            ISymbol? lhs,
            string? _operator,
            CodeNode rhsNode,
            ISymbol? rhs = null,
            bool enableBoxing = true)
        {
            //Visit(lhsExpression);
            //if (_operator!= null)
            //{
            //    CurrentTypeWriter.Write(node, _operator);
            //}
            //VisitNode(rhsNode);
            //return;

            var rhsExpression = rhsNode.IsT0 ? rhsNode.AsT0 : null;
            if (rhsExpression == null && rhs == null)
            {
                throw new InvalidOperationException("Either rhsExpression or rhs must be provided");
            }
            var rhsAsExpression = (rhsExpression as ArgumentSyntax)?.Expression ?? (ExpressionSyntax?)rhsExpression;

            lhs ??= lhsExpression != null ? GetExpressionBoundTarget(lhsExpression).TypeSyntaxOrSymbol as ISymbol : null;
            ISymbol? lhsRefTarget = (lhs as IParameterSymbol) ??
                (lhs as IFieldSymbol) ??
                (ISymbol?)(lhs as ILocalSymbol) ??
                (ISymbol?)(lhs as IPropertySymbol) ??
                lhs as IMethodSymbol;
            var lhsType = (lhs as IParameterSymbol)?.Type ??
                (lhs as ILocalSymbol)?.Type ??
                (lhs as IFieldSymbol)?.Type ??
                (lhs as IPropertySymbol)?.Type ??
                (lhs as IMethodSymbol)?.ReturnType ??
                lhs as ITypeSymbol;
            var lhsRefKind = lhs?.GetRefKind();

            if ((lhsRefKind == null || lhsRefKind == RefKind.None) && lhsExpression is ExpressionSyntax expression)
            {
                var _lhsRefKind = GetRefKind(expression);
                if (_lhsRefKind != null && _lhsRefKind != RefKind.None)
                    lhsRefKind = _lhsRefKind;
            }

            rhs ??= GetExpressionBoundTarget(rhsExpression!).TypeSyntaxOrSymbol as ISymbol;
            ISymbol? rhsRefTarget = (rhs as IParameterSymbol) ??
                (rhs as IFieldSymbol) ??
                (rhs as ILocalSymbol) ??
                (rhs as IPropertySymbol) ??
                (ISymbol?)(rhs as IDiscardSymbol) ??
                rhs as IMethodSymbol;
            var rhsType = (rhs as IParameterSymbol)?.Type ??
                (rhs as ILocalSymbol)?.Type ??
                (rhs as IFieldSymbol)?.Type ??
                (rhs as IPropertySymbol)?.Type ??
                (rhs is IMethodSymbol m && rhsExpression.IsKind(SyntaxKind.InvocationExpression) ? m.ReturnType : null) ??
                (rhs is IMethodSymbol m2 && m2.Name == ImplicitOperatorName ? m2.Parameters.First().Type : null) ??
                rhs as ITypeSymbol ??
                (rhsExpression != null ? _global.TryGetTypeSymbol(rhsExpression, this) : null);
            var rhsRefKind = rhs?.GetRefKind();

            if (rhsRefKind == null || rhsRefKind == RefKind.None)
            {
                var _rhsRefKind = rhsAsExpression != null ? GetRefKind(rhsAsExpression) : null;
                if (_rhsRefKind != null && _rhsRefKind != RefKind.None)
                    rhsRefKind = _rhsRefKind;
            }

            if (rhsRefKind == null)
            {
                if (rhsExpression.IsKind(SyntaxKind.ArrayCreationExpression) || rhsExpression.IsKind(SyntaxKind.ObjectCreationExpression) || rhsExpression.IsKind(SyntaxKind.ImplicitObjectCreationExpression))
                {
                    rhsRefKind = RefKind.None;
                }
            }
            bool explicitRhsRef = false;
            if (rhsExpression.IsKind(SyntaxKind.RefExpression))
            {
                explicitRhsRef = true;
                rhsRefKind = RefKind.Ref;
            }
            else if (rhsExpression.IsKind(SyntaxKind.Argument) && !string.IsNullOrEmpty(((ArgumentSyntax)rhsExpression).RefKindKeyword.ToString()))
            {
                explicitRhsRef = true;
                rhsRefKind = RefKind.Ref;
            }

            bool leftDereference = false;
            bool rightDereference = false;
            if (lhsRefKind != null && rhsRefKind != null)
            {
                //if (lhsRefKind.Value != rhsRefKind.Value)
                //{
                if (lhsRefKind != RefKind.None && rhsRefKind == RefKind.None)
                {
                    leftDereference = true;
                }
                else if (lhsRefKind != RefKind.None && rhsRefKind != RefKind.None && !explicitRhsRef)
                {
                    leftDereference = true;
                }
                if (!(rhs is IParameterSymbol pr && pr.IsThis))
                {
                    if (lhsRefKind == RefKind.None && rhsRefKind != RefKind.None)
                    {
                        rightDereference = true;
                    }
                    else if (lhsRefKind != RefKind.None && rhsRefKind != RefKind.None && !explicitRhsRef)
                    {
                        rightDereference = true;
                    }
                }
                //}
                //else
                //{
                //    if (lhs?.Kind == SymbolKind.Method && rhs?.Kind == SymbolKind.Method)
                //    {
                //        leftDereference = true;
                //        rightDereference = true;
                //    }
                //}
            }
            //else 
            //{
            //    rhsRefKind = RefKind.Out;
            //}

            if (lhsExpression != null)
            {
                Visit(lhsExpression);
                if (leftDereference)
                {
                    //assigning a ref from a non-ref
                    //eg ref int field; int a; field = a;
                    //Dereference and rewrite as field.$v = a
                    //TryDereference(lhsExpression);
                }
                CurrentTypeWriter.Write(node, $" {_operator} ");
                if (lhsExpression.IsKind(SyntaxKind.DeclarationExpression)/* is DeclarationExpressionSyntax*/)
                {
                    CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.DestructureName}(");
                }
            }
            bool needBoxing = lhsType != null && rhsType != null && NeedBoxing(lhsType, rhsType);
            bool doBoxing = enableBoxing && needBoxing;
            //if (rhsType == null || rhsRefTarget == null)
            //{
            //    var expressionBoundMember = GetExpressionBoundMember(rhsExpression).TypeSyntaxOrSymbol;
            //    var expressionReturnType = _global.ResolveTypeSymbol(GetExpressionReturnType(rhsExpression), this, out var dc, out _);
            //    rhs = rhs ?? expressionBoundMember as ISymbol;
            //    rhsType = rhsType ??
            //        (expressionBoundMember as IParameterSymbol)?.Type ??
            //        (expressionBoundMember as ILocalSymbol)?.Type ??
            //        (expressionBoundMember as IFieldSymbol)?.Type ??
            //        (expressionBoundMember as IPropertySymbol)?.Type ??
            //        (dc as IParameterSymbol)?.Type ??
            //        (dc as IFieldSymbol)?.Type ??
            //        (dc as IPropertySymbol)?.Type ??
            //        (expressionReturnType as ITypeSymbol) ??
            //        expressionBoundMember as ITypeSymbol;
            //    rhsRefTarget = rhsRefTarget ?? (expressionBoundMember as IFieldSymbol) ?? (ISymbol?)(expressionBoundMember as ILocalSymbol);
            //    if (rhsRefKind == null)
            //        rhsRefKind = (rhsRefTarget as IParameterSymbol)?.RefKind ??
            //            (rhsRefTarget as ILocalSymbol)?.RefKind ??
            //            (rhsRefTarget as IFieldSymbol)?.RefKind ??
            //            (rhsRefTarget as IPropertySymbol)?.RefKind ?? null;
            //}
            //if (lhsType != null && TryWriteConstant(node, lhsType, rhsExpression))
            //return;
            //if (rhsType == null)
            //{
            //    var mrhs = GetExpressionReturnType(rhsExpression!);
            //    var rhsSymbol = mrhs.TypeSyntaxOrSymbol as ISymbol;
            //    rhsType = (ITypeSymbol?)_global.ResolveTypeSymbol(mrhs, this, out _, out _);
            //}
            //if (rhsExpression is RefExpressionSyntax refExpression)
            //{
            //    //rhsRefKind = RefKind.Ref;
            //    rhsExpression = refExpression.Expression;
            //}
            //if ((lhsRefKind == RefKind.Ref || lhsRefKind == RefKind.RefReadOnly) && rhsRefTarget != null && rhsRefKind == RefKind.None) //assigning a non ref to reference eg ref int a = field;
            //{
            //    WriteCreateRef(node, () =>
            //    {
            //        WriteVariableAssignment(node, lhsType, rhsExpression, rhsType);
            //    }, _readOnly: lhsRefKind == RefKind.RefReadOnly);
            //}
            //else if (lhsRefKind == RefKind.None && (rhsRefKind == RefKind.Ref || rhsRefKind == RefKind.RefReadOnly)) //assigning a ref to non reference eg ref field; int a = field;
            //{
            //    WriteVariableAssignment(node, lhsType, rhsExpression, rhsType);
            //    Writer.Write(node, ".");
            //    Writer.Write(node, Constants.RefValueName);
            //}
            //else
            IDisposable? dispose = null;
            {
                if (needBoxing)
                {
                    if (rhsExpression is ArgumentSyntax arg)
                        dispose = BoxPrimitiveAssignmentSyntaxEmitter.Disable(arg.Expression);
                    else
                        dispose = BoxPrimitiveAssignmentSyntaxEmitter.Disable(rhsExpression);
                }
                if (doBoxing)
                {
                    CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.BoxName}(");
                }

                //if (rhsType != null &&
                //    lhsType != null &&
                //    !lhsType.Equals(rhsType, SymbolEqualityComparer.Default) &&
                //    _global.Compilation.HasImplicitConversion(rhsType, lhsType))
                //{
                //    if (lhsType.IsNullable(out var t) && t!.Equals(rhsType, SymbolEqualityComparer.Default)) //we can directly assign T to T?, no need of operator
                //    { }
                //    else
                //        if (rhsAsExpression != null && TryInvokeMethodOperator(node, ImplicitOperatorName, (ITypeSymbol?)lhsType, null, [rhsAsExpression]))
                //            goto RhsEmitted;
                //}

                if (rhsExpression != null && TryCastUsingExternalInterface(node, lhsType, rhsType, rhsExpression))
                {
                }
                else if (rhsExpression != null)
                {
                    IAssignmentConverter? converter = null;
                    if (lhsType is INamedTypeSymbol nt)
                    {
                        converter = assignmentConverters.FirstOrDefault(a => a.ExpressionType == rhsExpression.GetType() && a.CanConvertTo(this, nt, rhsExpression));
                    }

                    if (converter != null)
                    {
                        converter.WriteAssignment(this, (INamedTypeSymbol)lhsType!, rhsExpression);
                    }
                    else
                    {
                        Visit(rhsExpression);
                    }
                }
                else
                {
                    VisitNode(rhsNode);
                }
                //RhsEmitted:
                if (rightDereference)
                {
                    //TryDereference(rhsExpression!);
                }
                //if (rhsType != null &&
                //    rhsType.SpecialType != SpecialType.System_Void &&
                //    rhsType.SpecialType != SpecialType.System_Array &&
                //    rhsType.TypeKind != TypeKind.TypeParameter &&
                //    !rhsType.IsNumericType() &&
                //    !rhsType.IsJsPrimitive() && //value type (except js primitive) must copy to assign to a new value
                //    !rhsType.IsPointer(out _) && //pointer is a value type, no need to clone though since our implementation of RefOrPointer is immutable by default
                //    rhsType.IsValueType &&

                //    lhsType != null &&
                //    (lhsType.Kind == SymbolKind.Field || lhsType.Kind == SymbolKind.Local || lhsType.Kind == SymbolKind.Parameter))
                //{
                //    if (rhsExpression.IsKind(SyntaxKind.IdentifierName))
                //    {
                //        CurrentTypeWriter.Write(node, ".Clone()"); //we generated this method (ICloneable.Clone) for all valuetypes, if they didnt implement ICloneable
                //    }
                //}
                if (doBoxing)
                {
                    CurrentTypeWriter.Write(node, $", ");
                    var rhsMetadata = rhsType!.Kind != SymbolKind.TypeParameter ? _global.GetRequiredMetadata(rhsType!) : null;
                    CurrentTypeWriter.Write(node, rhsMetadata?.InvocationName ?? rhsType.ComputeOutputTypeName(_global));
                    CurrentTypeWriter.Write(node, $")");
                }
                dispose?.Dispose();
            }

            if (lhsExpression != null)
            {
                if (lhsExpression.IsKind(SyntaxKind.DeclarationExpression)/* is DeclarationExpressionSyntax*/)
                {
                    CurrentTypeWriter.Write(node, ")");
                }
            }
        }

        public override void VisitEqualsValueClause(EqualsValueClauseSyntax node)
        {
            CurrentTypeWriter.Write(node, " = ");
            ISymbol? lhsType = null;
            if (node.Parent is VariableDeclaratorSyntax vd)
            {
                lhsType = CurrentClosure.GetIdentifierType(vd.Identifier.ValueText).TypeSyntaxOrSymbol as ISymbol;
            }
            else if (node.Parent is IdentifierNameSyntax id)
            {
                lhsType = CurrentClosure.GetIdentifierType(id.Identifier.ValueText).TypeSyntaxOrSymbol as ISymbol;
            }
            WriteVariableAssignment(node, null, lhsType, null, node.Value, null);
            //base.VisitEqualsValueClause(node);
        }

        void ExpandAndInvokeAsBinaryOperator(CSharpSyntaxNode node, string _operator, ExpressionSyntax leftOperand, ExpressionSyntax rightOperand)
        {
            var kind = _operator switch
            {
                "+=" => SyntaxKind.AddExpression,
                "-=" => SyntaxKind.SubtractExpression,
                "*=" => SyntaxKind.MultiplyExpression,
                "=" => SyntaxKind.DivideExpression,
                "%=" => SyntaxKind.ModuloExpression,
                "|=" => SyntaxKind.BitwiseOrExpression,
                "&=" => SyntaxKind.BitwiseAndExpression,
                "^=" => SyntaxKind.ExclusiveOrExpression,
                ">>=" => SyntaxKind.RightShiftExpression,
                "<<=" => SyntaxKind.LeftShiftExpression,
                _ => SyntaxKind.None
            };
            if (kind != SyntaxKind.None)
            {
                var newNode = SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    leftOperand.WithoutLeadingTrivia().WithoutTrailingTrivia(),
                    SyntaxFactory.BinaryExpression(
                        kind,
                        leftOperand.WithoutLeadingTrivia().WithoutTrailingTrivia(),
                        rightOperand.WithoutLeadingTrivia().WithoutTrailingTrivia())
                    );
                //ReplaceAndVisit(node, newNode);

                //var mnewNode = (ExpressionStatementSyntax)node.Parent!.ReplaceNode(node, newNode)!;
                //Visit(mnewNode.Expression);

                Visit(newNode);
                return;
            }
            throw new InvalidOperationException($"Operator {_operator} not yet supported!");
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if (TryInvokeMethodOperator(node, node.OperatorToken.ValueText, null, node.Left, null, [node.Left, node.Right]))
                return;
            var rhsType = _global.TryGetSymbol(node.Right, this);// GetExpressionBoundTarget(node.Right).TypeSyntaxOrSymbol as ISymbol;
            //if (rhsType == null)
            //{
            //    rhsType = _global.ResolveSymbol(GetExpressionReturnSymbol(node.Right), this/*, out _, out _*/);
            //}
            var lhsType = _global.TryGetSymbol(node.Left, this);// GetExpressionBoundTarget(node.Left).TypeSyntaxOrSymbol as ISymbol;
            //if (lhsType == null)
            //{
            //    lhsType = _global.ResolveSymbol(GetExpressionReturnSymbol(node.Left), this/*, out _, out _*/);
            //}
            var assignmentType = (rhsType != null ? _global.GetTypeSymbol(rhsType) : null) ?? (lhsType != null ? _global.GetTypeSymbol(lhsType) : null);

            IDisposable? disposeDelegateType = null;
            if (assignmentType is ITypeSymbol ts)
            {
                if (ts.IsDelegate(out var delegateReturnType, out var delegateParameterTypes))
                {
                    disposeDelegateType = CurrentClosure.DefineAnonymousMethodParameterTypes(delegateReturnType == null ? delegateParameterTypes! : [.. delegateParameterTypes!, delegateReturnType]);
                }
            }
            //Visit(node.Left);
            //Writer.Write(node, $" {node.OperatorToken.ValueText.Trim()} ");
            //if (node.Left.IsKind(SyntaxKind.DeclarationExpression)/* is DeclarationExpressionSyntax*/)
            //{
            //    Writer.Write(node, $"{_global.GlobalName}.Destructure(");
            //}
            WriteVariableAssignment(node, node.Left, lhsType, node.OperatorToken.ValueText, node.Right, rhsType);
            //Visit(node.Right);
            //if (node.Left.IsKind(SyntaxKind.DeclarationExpression)/* is DeclarationExpressionSyntax*/)
            //{
            //    Writer.Write(node, ")");
            //}
            disposeDelegateType?.Dispose();
        }
    }
}
