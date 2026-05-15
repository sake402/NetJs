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
        bool MemberIsLiteralInitialization(EqualsValueClauseSyntax? initializer, ITypeSymbol memberType)
        {
            bool isLiteralInit = initializer != null &&
                   (_global.EvaluateConstant(initializer.Value, this).HasValue || (initializer.Value is LiteralExpressionSyntax || (initializer.Value is PrefixUnaryExpressionSyntax pu && pu.Operand is LiteralExpressionSyntax))) &&
                   memberType.IsJsPrimitive();
            return isLiteralInit;
        }

        public void WriteMemberName(CSharpSyntaxNode node, ITypeSymbol symbol, string memberName/*, CodeNode? _this = null*/)
        {
            var member = symbol.GetMembers(memberName, _global).Single();
            WriteMemberName(node, symbol, member/*, _this*/);
        }

        public void WriteMemberName(CSharpSyntaxNode node, ITypeSymbol typeSymbol, ISymbol member
            //, CodeNode? thisExpression = null, bool? isGet = null, CodeNode? setValue = null
            )
        {
            var template = member.GetTemplateAttribute(_global);
            //if (template == null && isGet != null && member.Kind == SymbolKind.Property)
            //{
            //    var memberProperty = (IPropertySymbol)member;
            //    if (isGet == true && memberProperty.GetMethod != null)
            //    {
            //        template = memberProperty.GetMethod.GetTemplateAttribute(_global);
            //    }
            //    else if (isGet == false && memberProperty.SetMethod != null)
            //    {
            //        template = memberProperty.SetMethod.GetTemplateAttribute(_global);
            //    }
            //}
            //if (template != null)
            //{
            //    if (thisExpression == null && !member.IsStatic)
            //    {
            //        throw new InvalidOperationException("Cannot literarily write a templated member without providing this");
            //    }
            //    WriteMethodTemplate(node, thisExpression, typeSymbol, false, null, null, template, default);
            //    return;
            //}
            var metadata = _global.GetRequiredMetadata(member);
            var isStaticConvention = member.IsStaticCallConvention(_global);
            CurrentTypeWriter.Write(node, metadata.InvocationName ?? typeSymbol.Name);
            //if (isStaticConvention)
            //{
            //    if (thisExpression == null)
            //    {
            //        throw new InvalidOperationException("Cannot literarily write a member with static convention wthout providing the this");
            //    }
            //    if (isGet == false)
            //    {
            //        if (setValue == null)
            //            throw new InvalidOperationException("Must provide setValue when isGet is false");
            //        CurrentTypeWriter.Write(node, "$set");
            //    }
            //    else
            //        CurrentTypeWriter.Write(node, "$get");
            //    CurrentTypeWriter.Write(node, ".call(");
            //    VisitNode(thisExpression);
            //    if (isGet == false)
            //    {
            //        CurrentTypeWriter.Write(node, ", ");
            //        VisitNode(setValue!);
            //    }
            //    CurrentTypeWriter.Write(node, ")");
            //}
        }

        public void WriteMemberAccess(
             CSharpSyntaxNode node,
             CodeNode? thisExpression,
             ITypeSymbol? thisSymbol,
             string? memberName,
             ISymbol? member,
             CodeNode? setValue = null)
        {
            if (thisSymbol == null && thisExpression == null)
                throw new InvalidOperationException("Must supply one of lhsSymbol or thisExpression");
            if (memberName == null && member == null)
                throw new InvalidOperationException("Must supply one of memberName or member");
            if (thisSymbol == null)
            {
                if (thisExpression!.IsT0)
                {
                    thisSymbol = _global.GetTypeSymbol(thisExpression!.AsT0, this);
                }
                else
                {
                    throw new InvalidOperationException($"Cannot resolve expreession type of {thisExpression}");
                }
            }
            var lhsType = _global.GetTypeSymbol(thisSymbol);
            member ??= lhsType.GetMembers(memberName, _global).FirstOrDefault();
            memberName ??= member.Name;
            bool isStaticConvention = member?.IsStaticCallConvention(_global) ?? false;
            if (member is IFieldSymbol field &&
                field.IsConst &&
                field.ConstantValue != null &&
                (_global.OutputMode.HasFlag(OutputMode.InlineConstants) || _global.HasAttribute(member, typeof(InlineConstAttribute).FullName, this, false, out _) || _global.HasAttribute(member.ContainingType, typeof(InlineConstAttribute).FullName, this, false, out _)) &&
                !_global.HasAttribute(member, typeof(TemplateAttribute).FullName, this, false, out _))
            {
                TryWriteConstant(node, field.Type, null, new Optional<object?>(field.ConstantValue));
                //var systemString = _global.GetTypeSymbol("System.String", this, out _, out _);
                //if (field.Type.Equals(systemString, SymbolEqualityComparer.Default))
                //    Writer.Write(node, "\"");
                //Writer.Write(node, field.ConstantValue.ToString());
                //if (field.Type.Equals(systemString, SymbolEqualityComparer.Default))
                //    Writer.Write(node, "\"");
                return;
            }
            bool isAssignment = false;
            ExpressionSyntax? assignmentRhs = null;
            if (node.Parent is EqualsValueClauseSyntax eq)
            {
                if (eq.Value != node)
                {
                    isAssignment = true;
                    assignmentRhs = eq.Value;
                }
            }
            IMethodSymbol? method = member as IMethodSymbol;
            AttributeData? attribute = member?.GetTemplateAttribute(_global);
            if (attribute == null)
            {
                if (member is IPropertySymbol property)
                {
                    if (isAssignment)
                    {
                        method = property.SetMethod;
                        attribute = property.SetMethod?.GetTemplateAttribute(_global);
                    }
                    else
                    {
                        method = property.GetMethod;
                        attribute = property.GetMethod?.GetTemplateAttribute(_global);
                    }
                }
            }
            if (attribute != null)
            {
                WriteMethodTemplate(node, thisExpression, thisSymbol, false, method, null, attribute, default, assignmentRhs);
                return;
            }
            if (member != null)
            {
                var memberMetadata = _global.GetMetadata(member);
                if (member.IsStatic || isStaticConvention)
                {
                    if (thisSymbol is ITypeParameterSymbol tp)
                    {
                        CurrentTypeWriter.Write(node, tp.Name);
                        CurrentTypeWriter.Write(node, ".");
                        CurrentTypeWriter.Write(node, memberMetadata?.OverloadName ?? member.Name);
                    }
                    else
                    {
                        CurrentTypeWriter.Write(node, memberMetadata?.InvocationName ?? member.Name);
                    }
                    if (isStaticConvention)
                    {
                        if (setValue != null)
                        {
                            CurrentTypeWriter.Write(node, "$set.call(");
                        }
                        else
                            CurrentTypeWriter.Write(node, "$get.call(");
                        if (thisExpression != null)
                            VisitNode(thisExpression);
                        else
                            CurrentTypeWriter.Write(node, "this");
                        if (setValue != null)
                        {
                            CurrentTypeWriter.Write(node, ", ");
                            VisitNode(setValue);
                        }
                        CurrentTypeWriter.Write(node, ")");
                    }
                    return;
                }
                else
                {
                    memberName = memberMetadata?.InvocationName ?? memberName;
                }
            }
            var initialCurrentNamespace = currentExpressionNamespace;
            bool lhsWritten = false;
            if (thisExpression == null && member != null && !(member.IsStatic || isStaticConvention))
            {
                if (node.Parent is AssignmentExpressionSyntax assign && assign.Left == node && node.Parent?.Parent is InitializerExpressionSyntax)
                {
                }
                else if (node.Parent.IsKind(SyntaxKind.NameColon))
                {
                }
                else
                {
                    if (SymbolIsThisTypeMember(member, out _))
                    {
                        ConditionalAccessExpressionSyntax? ce = null;
                        if ((ce = node.FindClosestParent<ConditionalAccessExpressionSyntax>(isCandidate: (e) => e.Expression == node || e.WhenNotNull == node)) == null)
                            CurrentTypeWriter.Write(node, "this.");
                        else if (ce.Expression == node)
                            CurrentTypeWriter.Write(node, "this.");
                        else if (ce.WhenNotNull == node)
                            CurrentTypeWriter.Write(node, ".");
                        //lhsWritten = true;
                    }
                    //else
                    //{
                    //    ConditionalAccessExpressionSyntax? ce = null;
                    //    if (node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    //    {
                    //        if (((MemberAccessExpressionSyntax)node.Parent).Expression == node)
                    //        {
                    //            CurrentTypeWriter.Write(node, "this");
                    //            lhsWritten = true;
                    //        }
                    //        else if (((MemberAccessExpressionSyntax)node.Parent).Name == node)
                    //        {
                    //        }
                    //    }
                    //    else if ((ce = node.FindClosestParent<ConditionalAccessExpressionSyntax>()) != null && ce.WhenNotNull == node)
                    //    {
                    //    }
                    //    else
                    //    {
                    //        CurrentTypeWriter.Write(node, "this");
                    //        lhsWritten = true;
                    //    }
                    //}
                }
            }
            else
            {
                VisitNode(thisExpression);
                lhsWritten = thisExpression != null;
            }
            if (/*node.Expression.IsKind(SyntaxKind.IdentifierName) && */initialCurrentNamespace != currentExpressionNamespace)
            {
                //Visit(node.Name);
                CurrentTypeWriter.Write(node, memberName);
                //if the above expression is captured into a namespace, don't write the dot
            }
            else
            {
                if (lhsWritten)
                {
                    var lhsIsThis = thisExpression != null && thisExpression.IsT0 && thisExpression.AsT0.IsKind(SyntaxKind.ThisExpression);
                    if (!lhsIsThis && thisSymbol.GetRefKind() != RefKind.None)
                    {
                        TryDereference(node);
                    }
                    CurrentTypeWriter.Write(node, ".");
                }
                CurrentTypeWriter.Write(node, memberName);
            }
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            string? memberName = null;
            if (node.Name is GenericNameSyntax gn)
            {
                memberName = gn.Identifier.Text;
            }
            else
            {
                memberName = node.Name.Identifier.ValueText;
            }
            var lhsType = _global.TryGetTypeSymbol(node.Expression, this);
            if (lhsType != null)
            {
                var memberType = _global.GetSymbol(node, this);
                WriteMemberAccess(node, node.Expression, lhsType, memberName, memberType);
            }
            else
            {
                var type = _global.GetTypeSymbol(node, this);
                var metadata = _global.GetRequiredMetadata(type);
                CurrentTypeWriter.Write(node, metadata.InvocationName?? type.Name);
            }
            return;

            ////var memberType = GetExpressionReturnType(node);
            //var _memberType = GetExpressionBoundMember(node);
            //if (_memberType.TypeSyntaxOrSymbol != null)
            //{
            //    var memberType = _global.ResolveTypeSymbol(_memberType, this/*, out var declaringType, out _*/);
            //    if (memberType != null &&
            //            (
            //                _global.HasAttribute(memberType, typeof(InlineConstAttribute).FullName, this, false, out _) ||
            //                (declaringType != null && _global.HasAttribute(declaringType, typeof(InlineConstAttribute).FullName, this, false, out _))
            //            ))
            //    {
            //        var member = declaringType ?? ((ITypeSymbol)memberType).GetMembers(memberName, _global).FirstOrDefault();
            //        if (member is IFieldSymbol field && field.IsConst && field.ConstantValue != null)
            //        {
            //            var systemString = _global.GetTypeSymbol("System.String", this/*, out _, out _*/);
            //            if (field.Type.Equals(systemString, SymbolEqualityComparer.Default))
            //                Writer.Write(node, "\"");
            //            Writer.Write(node, field.ConstantValue.ToString());
            //            if (field.Type.Equals(systemString, SymbolEqualityComparer.Default))
            //                Writer.Write(node, "\"");
            //            return;
            //        }
            //    }
            //}
            ////var type = GetExpressionReturnType(node.Expression);
            //////var symbol = GetTypeSymbol(type, out _);
            //////we want to fully qualify member access names
            ////if (!node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression)/* is not MemberAccessExpressionSyntax*/ && type.TypeSyntaxOrSymbol is INamedTypeSymbol symbol)
            ////{
            ////    var metadata = global.ReversedSymbols[symbol.OriginalDefinition];
            ////    Writer.Write(node, metadata.InvocationName ?? symbol.Name);
            ////}
            ////else
            ////{
            ////}
            //CodeType lhsType = GetExpressionReturnType(node.Expression);
            //ISymbol? lhsSymbol = null;
            //if (lhsType.TypeSyntaxOrSymbol != null)
            //{
            //    lhsSymbol = _global.ResolveTypeSymbol(lhsType, this, out _, out _);
            //    if (lhsSymbol is ITypeSymbol typeSymbol)
            //    {
            //        var accessedMember = typeSymbol.GetMembers(memberName, _global).FirstOrDefault();
            //        bool isAssignment = false;
            //        ExpressionSyntax? assignmentRhs = null;
            //        if (node.Parent is EqualsValueClauseSyntax eq)
            //        {
            //            if (eq.Value != node)
            //            {
            //                isAssignment = true;
            //                assignmentRhs = eq.Value;
            //            }
            //        }
            //        IMethodSymbol? method = accessedMember as IMethodSymbol;
            //        AttributeData? attribute = accessedMember?.GetTemplateAttribute(_global);
            //        if (attribute == null)
            //        {
            //            if (accessedMember is IPropertySymbol property)
            //            {
            //                if (isAssignment)
            //                {
            //                    method = property.SetMethod;
            //                    attribute = property.SetMethod?.GetTemplateAttribute(_global);
            //                }
            //                else
            //                {
            //                    method = property.GetMethod;
            //                    attribute = property.GetMethod?.GetTemplateAttribute(_global);
            //                }
            //            }
            //        }
            //        if (attribute != null)
            //        {
            //            WriteMethodTemplate(node, node.Expression, lhsSymbol, false, method, null, attribute, default, assignmentRhs);
            //            return;
            //        }
            //        if (accessedMember != null)
            //        {
            //            var memberMetadata = _global.GetRequiredMetadata(accessedMember);
            //            if (accessedMember.IsStatic)
            //            {
            //                if (lhsSymbol is ITypeParameterSymbol tp)
            //                {
            //                    Writer.Write(node, tp.Name);
            //                    Writer.Write(node, ".");
            //                    Writer.Write(node, memberMetadata.OverloadName /*??memberMetadata.LocalName */?? accessedMember.Name);
            //                }
            //                else
            //                {
            //                    Writer.Write(node, memberMetadata.InvocationName ?? accessedMember.Name);
            //                }
            //                return;
            //            }
            //            else
            //            {
            //                memberName = memberMetadata.InvocationName ?? memberName;
            //                //if (true)
            //                //{
            //                //    Writer.Write(node, "this.");
            //                //    Writer.Write(node, memberName);
            //                //}
            //            }
            //        }
            //    }
            //}
            //var initialCurrentNamespace = currentExpressionNamespace;
            //Visit(node.Expression);
            //if (/*node.Expression.IsKind(SyntaxKind.IdentifierName) && */initialCurrentNamespace != currentExpressionNamespace)
            //{
            //    Visit(node.Name);
            //    //if the above expression is captured into a namespace, don't write the dot
            //}
            //else
            //{
            //    Writer.Write(node, ".");
            //    Writer.Write(node, memberName);
            //}
            //base.VisitMemberAccessExpression(node);
        }
    }
}
