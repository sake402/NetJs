using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization.ValueDeserializers;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        bool IsFieldStructLayout(CSharpSyntaxNode? member, ISymbol? field, out int fieldOffset)
        {
            if (field == null && member == null)
                throw new InvalidOperationException("Expected one of member or field");
            field ??= _global.GetTypeSymbol(member!, this);
            var isStatic = field.IsStatic;// ?? false;// member.IsSt
            if (field is IFieldSymbol fs && fs.IsConst)
            {
                fieldOffset = -1;
                return false;
            }
            if (_global.HasAttribute(field, typeof(FieldOffsetAttribute).FullName!, this, false, out var fieldOffsetAttribute))
            {
                var offsetArg = fieldOffsetAttribute![0];
                fieldOffset = (int)fieldOffsetAttribute[0];
            }
            else
            {
                LayoutKind layoutKind = field.ContainingType.IsValueType ? LayoutKind.Sequential : LayoutKind.Auto;
                if (_global.HasAttribute(field.ContainingType, typeof(StructLayoutAttribute).FullName, this, false, out var structLayoutAttribute))
                {
                    layoutKind = (LayoutKind)(int)structLayoutAttribute![0];
                }
                else
                {
                    fieldOffset = -1;
                    return false;
                }
                //var fields = field.ContainingType.GetMembers().Where(m =>
                //(m.Kind == SymbolKind.Field && ((IFieldSymbol)m).AssociatedSymbol == null) ||
                //(m.Kind == SymbolKind.Event) ||
                //(m.Kind == SymbolKind.Property && ((IPropertySymbol)m).IsAutoProperty()));
                switch (layoutKind)
                {
                    case LayoutKind.Auto:
                    //fields = fields.OrderBy(m => m.Name).ToList();
                    //break;
                    case LayoutKind.Sequential:
                        if (member != null)
                        {
                            var type = member.FindClosestParent<TypeDeclarationSyntax>() ?? throw new InvalidOperationException();
                            var members = type.Members.Where(m => m.Modifiers.IsStatic() || !isStatic).SelectMany(m =>
                            {
                                if (m is BaseFieldDeclarationSyntax vd)
                                {
                                    if (vd.Modifiers.IsConst())
                                        return [];
                                    return vd.Declaration.Variables.Cast<CSharpSyntaxNode>();
                                }
                                return [(CSharpSyntaxNode)m];
                            });
                            fieldOffset = Array.IndexOf(members.ToArray(), member);
                        }
                        else
                        {
                            var fields = field.ContainingType.GetMembers().Where(m => m.IsStatic || !isStatic).Where(m =>
                                (m.Kind == SymbolKind.Field && ((IFieldSymbol)m).AssociatedSymbol == null && !((IFieldSymbol)m).IsConst) ||
                                (m.Kind == SymbolKind.Event) ||
                                (m.Kind == SymbolKind.Property && ((IPropertySymbol)m).IsAutoProperty()))
                                .OrderBy(f =>
                                {
                                    if (_global.HasAttribute(f, typeof(FieldOffsetAttribute).FullName!, this, false, out var fieldOffsetAttribute))
                                    {
                                        var offsetArg = fieldOffsetAttribute![0];
                                        return (int)fieldOffsetAttribute[0];
                                    }
                                    if (f.Kind == SymbolKind.Field)
                                        return int.MaxValue / 2;
                                    return int.MaxValue;
                                })
                                .ToArray();
                            fieldOffset = Array.IndexOf(fields, field);
                        }
                        if (fieldOffset == -1)
                            throw new InvalidOperationException();
                        break;
                    default:
                    case LayoutKind.Explicit:
                        fieldOffset = -1;
                        return false;
                        //throw new InvalidOperationException("Must have FieldOffsetAttribute already");
                        //break;
                }
                //fieldOffset = Array.IndexOf(fields.ToArray(), field);
                //foreach (var f in fields)
                //{
                //    if (SymbolEqualityComparer.Default.Equals(f, field))
                //        break;
                //    var fType = (f as IFieldSymbol)!.Type;
                //    fieldOffset += 1;//_global.GetTypeSizeInBytes(fType, this);
                //}
            }
            return true;
        }

        bool TryWriteFieldLayout(CSharpSyntaxNode member, ISymbol field, ITypeSymbol fieldType, string fieldName, string? modifier, string? comment)
        {
            //bool isBootClass = _global.HasAttribute(field.ContainingSymbol, typeof(BootAttribute).FullName, this, true, out _);
            int fieldOffset = 0;
            int inlineSize = 0;
            //Dont use field layout for boot classes, as they dont inherit from System.Object really
            if (
                //!isBootClass &&
                !field.IsStatic &&
                (IsFieldStructLayout(member, field, out fieldOffset) || _global.IsInlineArray(field.ContainingType, out inlineSize)) &&
                !field.ContainingType.IsType("System.Exception")/*Exception inherit native JS error, not object*/)
            {
                if (inlineSize > 0)
                {
                    //No one is going to reference this fields, no point in creating them
                    //for (int i = 0; i < inlineSize; i++)
                    //{
                    //    CurrentTypeWriter.WriteLine(member, $"/*{comment}*/ {modifier} get {fieldName}${i + 1}() {{ return this.Get{(field.IsStatic ? "S" : "")}Field({i}); }}", true);
                    //    CurrentTypeWriter.WriteLine(member, $"/*{comment}*/ {modifier} set {fieldName}${i + 1}(value) {{ this.Set{(field.IsStatic ? "S" : "")}Field({i}, value); }}", true);
                    //}
                }
                else
                {
                    CurrentTypeWriter.WriteLine(member, $"/*{comment}*/ {modifier} get {fieldName}() {{ return this.Get{(field.IsStatic ? "S" : "")}Field({fieldOffset}); }}", true);
                    CurrentTypeWriter.WriteLine(member, $"/*{comment}*/ {modifier} set {fieldName}(value) {{ this.Set{(field.IsStatic ? "S" : "")}Field({fieldOffset}, value); }}", true);
                }
                return true;
            }
            return false;
        }

        void WriteField(BaseFieldDeclarationSyntax node)
        {
            //if (_global.HasAttribute(node, typeof(InlineConstAttribute).FullName!))
            //return;
            string? modifier = null;
            if (node.Modifiers.Any(e => e.ValueText == "static" || e.ValueText == "const"))
            {
                modifier += "static ";
            }
            foreach (var var in node.Declaration.Variables)
            {
                EnsureImported(node.Declaration.Type);
                IFieldSymbol? fieldSymbol = null;
                IEventSymbol? eventSymbol = null;
                var symbol = _global.GetTypeSymbol(var, this/*, out _, out _*/);
                fieldSymbol = symbol as IFieldSymbol;
                eventSymbol = symbol as IEventSymbol;
                if (fieldSymbol == null && eventSymbol == null)
                    throw new InvalidOperationException();
                ITypeSymbol type = fieldSymbol?.Type ?? eventSymbol?.Type!;
                if (_global.HasAttribute(symbol, typeof(InlineConstAttribute).FullName!, this, false, out _))
                    continue;
                if (_global.HasAttribute(symbol, typeof(TemplateAttribute).FullName!, this, false, out _))
                    continue;
                var fieldMetadata = _global.GetRequiredMetadata(symbol);
                var declaringSymbolMeta = _global.GetRequiredMetadata(symbol.ContainingSymbol);
                var fieldName = fieldMetadata.OverloadName ?? Utilities.ResolveIdentifierName(var.Identifier);
                //if (fieldSymbol != null)
                CurrentClosure.DefineIdentifierType(symbol.Name, CodeSymbol.From(symbol));
                //else
                //    CurrentClosure.DefineIdentifierType(fieldName, CodeType.From(node.Declaration.Type, SymbolKind.Field));

                var defaultValue = var.Initializer == null ? _global.GetDefaultValue(node.Declaration.Type, this) : null;

                bool useStaticPropertyFunction = false;
                //if ((node.Modifiers.IsStatic() || node.Modifiers.IsConst()) &&
                //    (defaultValue.EndsWith("()")/*eg T.default() ot Guid.default()*/ ||
                //    (var.Initializer?.Value != null && var.Initializer?.Value is not LiteralExpressionSyntax)))
                //{
                //    useStaticPropertyFunction = true;
                //    Writer.WriteLine(node, $"static $_{fieldMetadata.OverloadName ?? fieldName};", true);
                //}
                bool isLiteralInit = MemberIsLiteralInitialization(var.Initializer, type);
                bool isFieldLayout = _global.HasAttribute(symbol.ContainingType, typeof(StructLayoutAttribute).FullName!, this, false, out _);
                bool isInlineArray = _global.IsInlineArray(symbol.ContainingType, out var inlineArraySize);
                if (var.Initializer != null || isInlineArray || (type.IsValueType && !type.IsJsPrimitive())/*SpecialType == SpecialType.System_ValueType*/)
                {
                    //If we initialize a field from a primary constructor parameter, this is already handled in the primary constructor generator (WritePrimaryConstructor)
                    //We should skip it here
                    if (MemberWasInitializedByPrimaryConstructor(var, var.Initializer))
                    {
                    }
                    else if (isLiteralInit && !isFieldLayout)
                    {
                        //use inline init for literal init value
                    }
                    else
                    {
                        bool isStaticInit = node.Modifiers.IsStatic() || node.Modifiers.IsConst();
                        CurrentClosure.RegisterTypeInitializer(() =>
                        {
                            if (isInlineArray)
                            {
                                CurrentTypeWriter.WriteLine(node, $"for (let $i = 0; $i < {inlineArraySize}; $i++)", true);
                                CurrentTypeWriter.WriteLine(node, "{", true);
                                CurrentTypeWriter.WriteLine(node, $"this.SetField($i, {defaultValue});", true);
                                CurrentTypeWriter.WriteLine(node, "}", true);
                                //for (int i = 0; i < inlineArraySize; i++)
                                //{
                                //    CurrentTypeWriter.Write(node, $"{(isStaticInit ? "this"/*declaringSymbolMeta.InvocationName + "."*/ : "this")}", true);
                                //    CurrentTypeWriter.Write(node, ".");
                                //    CurrentTypeWriter.Write(node, fieldName);
                                //    CurrentTypeWriter.Write(node, "$");
                                //    CurrentTypeWriter.Write(node, (i + 1).ToString());
                                //    CurrentTypeWriter.Write(node, " = ");
                                //    //Visit(var.Initializer);
                                //    if (var.Initializer != null)
                                //    {
                                //        if (!TryWriteConstant(node, type, var.Initializer.Value))
                                //            WriteVariableAssignment(node, null, symbol, null, var.Initializer.Value, null);
                                //    }
                                //    else
                                //    {
                                //        if (defaultValue != null)
                                //            CurrentTypeWriter.Write(node, defaultValue);
                                //        else
                                //        {
                                //            CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.DefaultTypeName}({type.ComputeOutputTypeName(_global)})");
                                //        }
                                //    }
                                //    CurrentTypeWriter.WriteLine(node, ";");
                                //}
                            }
                            else
                            {
                                //If we are in a static initilizer, it is safe to use this as it reference the class prototype itself
                                CurrentTypeWriter.Write(node, $"{(isStaticInit ? "this"/*declaringSymbolMeta.InvocationName + "."*/ : "this")}", true);
                                CurrentTypeWriter.Write(node, ".");
                                CurrentTypeWriter.Write(node, fieldName);
                                CurrentTypeWriter.Write(node, " = ");
                                //Visit(var.Initializer);
                                if (var.Initializer != null)
                                {
                                    if (!TryWriteConstant(node, type, var.Initializer.Value))
                                        WriteVariableAssignment(node, null, symbol, null, var.Initializer.Value, null);
                                }
                                else
                                {
                                    if (defaultValue != null)
                                        CurrentTypeWriter.Write(node, defaultValue);
                                    else
                                    {
                                        CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.DefaultTypeName}({type.ComputeOutputTypeName(_global)})");
                                    }
                                }
                                CurrentTypeWriter.WriteLine(node, ";");
                            }
                        }, isStaticInit);
                    }
                }
                if ((isFieldLayout || isInlineArray) &&
                    TryWriteFieldLayout(var, fieldSymbol ?? (ISymbol)eventSymbol!, fieldSymbol?.Type ?? eventSymbol!.Type, fieldMetadata.OverloadName ?? fieldName, modifier, $"{node.Declaration.Type.ToFullString().Trim()}"))
                {
                }
                else
                {
                    CurrentTypeWriter.Write(node, $"/*{node.Declaration.Type.ToFullString().Trim()}*/ {modifier}{(useStaticPropertyFunction ? "get " : "")}{fieldMetadata.OverloadName ?? fieldName}", true);
                    //if (useStaticPropertyFunction)
                    //{
                    //    Writer.Write(node, $"() {{ return {(node.Modifiers.IsConst() || node.Modifiers.IsStatic() ? $"{declaringSymbolMeta.InvocationName}." : "")}$_{fieldMetadata.OverloadName ?? fieldName} ??= ");
                    //}
                    if (isLiteralInit)
                    {
                        CurrentTypeWriter.Write(node, " = ");
                        if (!TryWriteConstant(node, type, var.Initializer!.Value))
                            WriteVariableAssignment(node, null, symbol, null, var.Initializer!.Value, null);
                    }
                    else
                    {
                        if (defaultValue != null)
                        {
                            if (!useStaticPropertyFunction)
                                CurrentTypeWriter.Write(node, " = ");
                            CurrentTypeWriter.Write(node, defaultValue);
                        }
                    }
                    if (useStaticPropertyFunction)
                    {
                        CurrentTypeWriter.WriteLine(node, $"; }}");
                    }
                    else
                    {
                        CurrentTypeWriter.WriteLine(node, $";");
                    }
                }
            }
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            WriteField(node);
            //base.VisitFieldDeclaration(node);
        }

        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            WriteField(node);
            //base.VisitEventFieldDeclaration(node);
        }

    }
}
