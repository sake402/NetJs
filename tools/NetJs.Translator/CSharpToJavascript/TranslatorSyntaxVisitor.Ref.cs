using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        public bool TryDereference(CSharpSyntaxNode node)
        {
            if (!CurrentTypeWriter.EndsWith(Constants.RefValueName)) //skip if already derefed by the lower layer
            {
                CurrentTypeWriter.Write(node, ".");
                CurrentTypeWriter.Write(node, Constants.RefValueName);
                return true;
            }
            return false;
        }

        bool DereferenceIfReference(ExpressionSyntax node)
        {
            var expressionType = GetExpressionBoundTarget(node).TypeSyntaxOrSymbol as ISymbol;
            var refKind = expressionType?.GetRefKind();
            if (refKind != null && refKind != RefKind.None)
            {
                TryDereference(node);
                return true;
            }
            return false;
        }

        public void WriteCreateArrayRefOrPointer(CSharpSyntaxNode node, ITypeSymbol type, CodeNode arrayExpression, IEnumerable<CodeNode>? indexExpression)
        {
            WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.CreateArrayReferenceT",
                methodGenericTypes: [type],
                arguments: [arrayExpression, .. (indexExpression ?? Enumerable.Empty<CodeNode>())]);
            //var refStaticClass = (ITypeSymbol)_global.GetTypeSymbol("System.RefOrPointer", this);
            //var createMethod = (IMethodSymbol)refStaticClass.GetMembers("CreateFromArray").Single();
            //createMethod = createMethod.Construct(type);
            //WriteMethodInvocation(node, createMethod, null, null, [arrayExpression, .. indexExpression], null, null, false);
        }

        public void WriteCreateObjectRefOrPointer(CSharpSyntaxNode node, ITypeSymbol type, ExpressionSyntax objectTargetExpression, CodeNode? byteOffset = null)
        {
            WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.CreateObjectReferenceT", methodGenericTypes: [type], arguments: [
                new CodeNode(() => {
                    CurrentTypeWriter.Write(node, "() => ");
                    Visit(objectTargetExpression);
                }),
                new CodeNode(() => {
                    CurrentTypeWriter.Write(node, "($v) => ");
                    if (objectTargetExpression.IsKind(SyntaxKind.ThisExpression))
                    {
                        //The only time when c# allows this to be assigned is if it is a struct type.
                        //We clone the rhs into this
                        CurrentTypeWriter.Write(node, "$v.");
                        CurrentTypeWriter.Write(node, Constants.Clone);
                        CurrentTypeWriter.Write(node, "(");
                        Visit(objectTargetExpression);
                        CurrentTypeWriter.Write(node, ")");
                    }
                    else
                    {
                        Visit(objectTargetExpression);
                        CurrentTypeWriter.Write(node, " = $v");
                    }
                }),
                byteOffset ?? new CodeNode(() => {
                    CurrentTypeWriter.Write(node, "null");
                })
            ]);
        }

        public void WriteCreateRef(CSharpSyntaxNode node, ITypeSymbol type, string fieldName, string? prefix = null, string? suffix = null, bool _readOnly = false, bool inCurrentClosure = true)
        {
            if (inCurrentClosure)
            {
                CurrentTypeWriter.InsertAbove(node, () =>
                {
                    if (prefix != null)
                        CurrentTypeWriter.Write(node, prefix);
                    WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.CreateObjectReferenceT", methodGenericTypes: [type], arguments: [new CodeNode(() => {
                        CurrentTypeWriter.Write(node, "() => ");
                        CurrentTypeWriter.Write(node, fieldName);
                    }),
                    new CodeNode(() => {
                        CurrentTypeWriter.Write(node, "($v) => ");
                        CurrentTypeWriter.Write(node, fieldName);
                        CurrentTypeWriter.Write(node, " = $v");
                    })]);
                    if (suffix != null)
                        CurrentTypeWriter.Write(node, suffix);
                }, true);
            }
            else
            {
                if (prefix != null)
                    CurrentTypeWriter.Write(node, prefix);
                WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.CreateObjectReferenceT", methodGenericTypes: [type], arguments: [new CodeNode(() => {
                    CurrentTypeWriter.Write(node, "() => ");
                    CurrentTypeWriter.Write(node, fieldName);
                }),
                new CodeNode(() => {
                    CurrentTypeWriter.Write(node, "($v) => ");
                    CurrentTypeWriter.Write(node, fieldName);
                    CurrentTypeWriter.Write(node, " = $v");
                })]);
                if (suffix != null)
                    CurrentTypeWriter.Write(node, suffix);
            }
            //var str = $"{prefix}{{ get {Constants.RefValueName}(){{ return {fieldName}; }}";
            //if (!_readOnly)
            //{
            //    str += $", set {Constants.RefValueName}(v){{ {fieldName} = v; }}";
            //}
            //str += " }";
            //str += suffix;
            //if (inCurrentClosure)
            //    CurrentTypeWriter.InsertAbove(node, str, true);
            //else
            //    CurrentTypeWriter.Write(node, str, true);
        }

        public void WriteCreateRef(CSharpSyntaxNode node, ExpressionSyntax expression, ITypeSymbol? type, CodeNode? byteOffset = null)
        {
            type ??= _global.GetTypeSymbol(expression, this);
            if (expression.IsKind(SyntaxKind.ElementAccessExpression))
            {
                var element = ((ElementAccessExpressionSyntax)expression).Expression;
                var indexes = ((ElementAccessExpressionSyntax)expression).ArgumentList.Arguments.Select(e => new CodeNode(e));
                WriteCreateArrayRefOrPointer(node, type, element, indexes);
            }
            else
            {
                WriteCreateObjectRefOrPointer(node, type, expression, byteOffset: byteOffset);
            }
        }

        public void WriteCreateSimpleRef(CSharpSyntaxNode node, CodeNode expression, ITypeSymbol? type = null, bool _readOnly = false, bool _writeOnly = false)
        {
            CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.RefCreateName}(");
            if (!_writeOnly)
            {
                CurrentTypeWriter.Write(node, $"() => ");
                VisitNode(expression);
            }
            else
            {
                CurrentTypeWriter.Write(node, $"undefined");
            }
            CurrentTypeWriter.Write(node, $", ");
            if (!_readOnly)
            {
                CurrentTypeWriter.Write(node, $"($v) => ");
                if (expression.IsT0 && expression.AsT0.IsKind(SyntaxKind.ThisExpression))
                {
                    //The only time when c# allows this to be assigned is if it is a struct type.
                    //We clone the rhs into this
                    CurrentTypeWriter.Write(node, "$v.");
                    CurrentTypeWriter.Write(node, Constants.Clone);
                    CurrentTypeWriter.Write(node, "(");
                    Visit(expression.AsT0);
                    CurrentTypeWriter.Write(node, ")");
                }
                else
                {
                    VisitNode(expression);
                    CurrentTypeWriter.Write(node, $" = $v");
                }
            }
            else if (type != null)
            {
                CurrentTypeWriter.Write(node, $"undefined");
            }
            if (type != null)
            {
                CurrentTypeWriter.Write(node, $", ");
                CurrentTypeWriter.Write(node, type.ComputeOutputTypeName(_global));
            }
            CurrentTypeWriter.Write(node, $")");
        }

        public override void VisitRefExpression(RefExpressionSyntax node)
        {
            var refTarget = _global.GetSymbol(node.Expression, this);
            //Allows a type like string which is simply an array of chars on the heap to reference the firstChar and also able to increment the ref/pointer to other chars in the string
            if (node.Expression.IsKind(SyntaxKind.IdentifierName) &&
                refTarget is IFieldSymbol mfield &&
                mfield.RefKind == RefKind.None &&
                !mfield.IsStatic &&
                mfield.ContainingType.IsValueType &&
                IsFieldStructLayout(null, mfield, out var fieldOffset, out var fieldSize))
            {
                WriteCreateArrayRefOrPointer(node, mfield.Type, new CodeNode(() =>
                {
                    CurrentTypeWriter.Write(node, $"this.{Constants.StructFieldsLayoutName}");
                }), [new CodeNode(() =>
                {
                    CurrentTypeWriter.Write(node, fieldOffset.ToString());
                })]);
            }
            else if (node.Expression.IsKind(SyntaxKind.FieldExpression) ||
                (refTarget is ILocalSymbol local && local.RefKind == RefKind.None) ||
                (refTarget is IFieldSymbol field && field.RefKind == RefKind.None) ||
                (refTarget is IParameterSymbol parameter && parameter.RefKind == RefKind.None))
            {
                WriteCreateRef(node, node.Expression, _global.GetTypeSymbol(refTarget));
            }
            //if we have an array ref expression like ref _array[byteIndex]
            //we need to create a ref than can read and write the array at the specified index
            else if (node.Expression is ElementAccessExpressionSyntax elementAccess)
            {
                var target = elementAccess.Expression;
                var index = elementAccess.ArgumentList.Arguments.Select(e => new CodeNode(e));
                ITypeSymbol? arrayElementType = null;
                var type = _global.GetTypeSymbol(target, this);
                var isArrayType = type.IsArray(out arrayElementType);
                if (isArrayType && arrayElementType != null)
                {
                    //if (arrayElementType.IsArray(out _))//jagged array? eg ref jaggedArray[0], where jaggedArray is int[][]
                    //{
                    //    WriteCreateObjectRefOrPointer(node, arrayElementType, node.Expression);
                    //}
                    //else
                    {
                        WriteCreateArrayRefOrPointer(node, arrayElementType, target, index);
                    }
                    //var refStaticClass = (ITypeSymbol)_global.GetTypeSymbol("System.RefOrPointer", this);
                    //var createMethod = (IMethodSymbol)refStaticClass.GetMembers("CreateFromArray").Single();
                    //createMethod = createMethod.Construct(arrayElementType);
                    //WriteMethodInvocation(node, createMethod, null, null, [target, .. index], null, null, false);
                }
                else
                {
                    Visit(node.Expression);
                }
            }
            //if we have an array ref expression like ref *pointer,
            //we need to create a ref than can read and write the array at a specified index
            //A ref and pointer are implemented using the same runtime strutute though, so there are assignable and doesnt need any conversion
            else if (node.Expression is PrefixUnaryExpressionSyntax prefix && prefix.IsKind(SyntaxKind.PointerIndirectionExpression))
            {
                Visit(prefix.Operand);
                //var target = prefix.Operand;
                ////ITypeSymbol? objectType = _global.ResolveSymbol(GetExpressionReturnSymbol(target), this)!.GetTypeSymbol();
                ////if (objectType == null)
                ////throw new InvalidOperationException("Cannot infer refed type");
                ////WriteCreateRefOrPointer(node, objectType, target);
                //WriteCreateObjectRefOrPointer(node, target);
                ////var refStaticClass = (ITypeSymbol)_global.GetTypeSymbol("System.RefOrPointer", this);
                ////var createMethod = (IMethodSymbol)refStaticClass.GetMembers("CreateFromPointer").Single();
                ////createMethod = createMethod.Construct(objectType);
                ////WriteMethodInvocation(node, createMethod, null, null, [target], null, null, false);
            }
            else
            {
                Visit(node.Expression);
            }
            //base.VisitRefExpression(node);
        }
    }
}
