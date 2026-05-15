using LivingThing.Core.Frameworks.Common.OneOf;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetJs.Translator.CSharpToJavascript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        public IMethodSymbol? GetGetIndexer(OneOf<ElementAccessExpressionSyntax, ElementBindingExpressionSyntax> element)
        {
            var expression = element.IsT1 ? ((ConditionalAccessExpressionSyntax)element.AsT1.Parent!).Expression : element.AsT0.Expression;
            var arguments = element.IsT0 ? element.AsT0.ArgumentList.Arguments : element.AsT1!.ArgumentList.Arguments;
            var targetType = _global.TryGetTypeSymbol(expression, this);
            if (targetType != null)
            {
                var propertyIndexers = targetType.GetMembers("get_Item", _global).Where(e => e is IMethodSymbol p && p.Parameters.Count() == arguments.Count).Cast<IMethodSymbol>().ToList();
                var bestIndexer = GetBestOverloadMethod(targetType, propertyIndexers, null, arguments, null, out _);
                return bestIndexer;
            }
            return null;
        }

        public IMethodSymbol? GetSetIndexer(OneOf<ElementAccessExpressionSyntax, ElementBindingExpressionSyntax> element, ExpressionSyntax value)
        {
            var expression = element.IsT1 ? ((ConditionalAccessExpressionSyntax)element.AsT1.Parent!.Parent!).Expression : element.AsT0.Expression;
            var arguments = element.IsT0 ? element.AsT0.ArgumentList.Arguments : element.AsT1!.ArgumentList.Arguments;
            var targetType = _global.TryGetTypeSymbol(expression, this);
            if (targetType != null)
            {
                var propertyIndexers = targetType.GetMembers("set_Item", _global).Where(e => e is IMethodSymbol p && p.Parameters.Count() == arguments.Count + 1).Cast<IMethodSymbol>().ToList();
                var bestIndexer = GetBestOverloadMethod(targetType, propertyIndexers, null, arguments, value, out _);
                return bestIndexer;
            }
            return null;
        }
    }
}
