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
using System.Diagnostics;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        bool IsFieldStructLayout(CSharpSyntaxNode? member, ISymbol? field, out int fieldOffset, out int fieldSize)
        {
            if (field == null && member == null)
                throw new InvalidOperationException("Expected one of member or field");
            field ??= _global.GetSymbol(member!, this);
            var isStatic = field.IsStatic;// ?? false;// member.IsSt
            if (field is IFieldSymbol fs && fs.IsConst)
            {
                fieldOffset = -1;
                fieldSize = -1;
                return false;
            }
            if (_global.HasAttribute(field, typeof(FieldOffsetAttribute).FullName!, this, false, out var fieldOffsetAttribute))
            {
                var layout = field.ContainingType.StructLayout(isStatic);
                var ol = layout[field];
                var offsetArg = (int)fieldOffsetAttribute![0]!;
                Debug.Assert(ol.Offset == offsetArg);
                fieldOffset = offsetArg;
                fieldSize = ol.Size;
                return true;
            }
            else
            {
                LayoutKind layoutKind = field.ContainingType.IsValueType ? LayoutKind.Sequential : LayoutKind.Auto;
                if (_global.HasAttribute(field.ContainingType, typeof(StructLayoutAttribute).FullName, this, false, out var structLayoutAttribute))
                {
                    layoutKind = (LayoutKind)(int)structLayoutAttribute![0]!;
                }
                //else
                //{
                //    fieldOffset = -1;
                //    fieldSize = -1;
                //    return false;
                //}
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
                        if (field != null)
                        {
                            var layout = field.ContainingType.StructLayout(isStatic);
                            var ol = layout[field];
                            fieldOffset = ol.Offset;
                            fieldSize = ol.Size;
                            //var fields = field.ContainingType.GetMembers().Where(m => m.IsStatic == isStatic).Where(m =>
                            //    (m.Kind == SymbolKind.Field && ((IFieldSymbol)m).AssociatedSymbol == null && !((IFieldSymbol)m).IsConst) ||
                            //    (m.Kind == SymbolKind.Event) ||
                            //    (m.Kind == SymbolKind.Property && ((IPropertySymbol)m).IsAutoProperty()))
                            //    .OrderBy(f =>
                            //    {
                            //        if (_global.HasAttribute(f, typeof(FieldOffsetAttribute).FullName!, this, false, out var fieldOffsetAttribute))
                            //        {
                            //            var offsetArg = fieldOffsetAttribute![0];
                            //            return (int)fieldOffsetAttribute[0];
                            //        }
                            //        if (f.Kind == SymbolKind.Field)
                            //            return int.MaxValue / 2;
                            //        return int.MaxValue;
                            //    })
                            //    .Select(f =>
                            //    {
                            //        var type = (f as IFieldSymbol)?.Type ?? (f as IEventSymbol)?.Type ?? (f as IPropertySymbol)?.Type;
                            //        if (_global.IsInlineArray(type!, out var iSize, out var iType))
                            //        {
                            //            return (iSize, iType, f);
                            //        }
                            //        return (1, null, f);
                            //    })
                            //    .ToArray();
                            //fieldOffset = Array.IndexOf(fields, field);
                        }
                        else if (member != null)
                        {
                            var type = member.FindClosestParent<TypeDeclarationSyntax>() ?? throw new InvalidOperationException();
                            var members = type.Members.Where(m => isStatic == m.Modifiers.IsStatic()).SelectMany(m =>
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
                            fieldSize = 1;
                        }
                        else
                        {
                            fieldOffset = -1;
                            fieldSize = -1;
                        }
                        if (fieldOffset == -1)
                            throw new InvalidOperationException();
                        break;
                    default:
                    case LayoutKind.Explicit:
                        fieldOffset = -1;
                        fieldSize = -1;
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
            int fieldSize = 0;
            int inlineSize = 0;
            //Dont use field layout for boot classes, as they dont inherit from System.Object really
            if (
                //!isBootClass &&
                !field.IsStatic &&
                (IsFieldStructLayout(member, field, out fieldOffset, out fieldSize) || _global.IsInlineArray(field.ContainingType, out inlineSize, out _)) &&
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
                    if (_global.IsInlineArray(fieldType, out inlineSize, out _))
                    {
                        CurrentTypeWriter.WriteLine(member, $"/*{comment}*/ {modifier} get {fieldName}() {{ return this.Get{(field.IsStatic ? "S" : "")}Field({fieldOffset}, {fieldType.ComputeOutputTypeName(_global)}); }}", true);
                        CurrentTypeWriter.WriteLine(member, $"/*{comment}*/ {modifier} set {fieldName}(value) {{ this.Set{(field.IsStatic ? "S" : "")}Field({fieldOffset}, {fieldType.ComputeOutputTypeName(_global)}, value); }}", true);
                    }
                    else
                    {
                        CurrentTypeWriter.WriteLine(member, $"/*{comment}*/ {modifier} get {fieldName}() {{ return this.Get{(field.IsStatic ? "S" : "")}Field({fieldOffset}, {fieldSize}); }}", true);
                        CurrentTypeWriter.WriteLine(member, $"/*{comment}*/ {modifier} set {fieldName}(value) {{ this.Set{(field.IsStatic ? "S" : "")}Field({fieldOffset}, {fieldSize}, value); }}", true);
                    }
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
                var symbol = _global.GetSymbol(var, this/*, out _, out _*/);
                fieldSymbol = symbol as IFieldSymbol;
                eventSymbol = symbol as IEventSymbol;
                if (fieldSymbol == null && eventSymbol == null)
                    throw new InvalidOperationException();
                ITypeSymbol fieldType = fieldSymbol?.Type ?? eventSymbol?.Type!;
                if (_global.HasAttribute(symbol, typeof(InlineConstAttribute).FullName!, this, false, out _))
                    continue;
                if (_global.HasAttribute(symbol, typeof(TemplateAttribute).FullName!, this, false, out _))
                    continue;
                var fieldMetadata = _global.GetRequiredMetadata(symbol);
                var declaringSymbolMeta = _global.GetRequiredMetadata(symbol.ContainingSymbol);
                var fieldName = fieldMetadata.OverloadName ?? var.Identifier.ResolveIdentifierName();
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
                bool isLiteralInit = MemberIsLiteralInitialization(var.Initializer, fieldType);
                bool isFieldLayout = _global.HasAttribute(symbol.ContainingType, typeof(StructLayoutAttribute).FullName!, this, false, out _);
                //bool fieldTypeIsInlineArray = _global.IsInlineArray(fieldType, out var inlineArraySize, out var inlineArrayFieldType);
                bool fieldContainingTypeIsInlineArray = _global.IsInlineArray(symbol.ContainingType, out var inlineArraySize, out var inlineArrayFieldType);
                void RegisterMemberInitializer()
                {
                    if (fieldContainingTypeIsInlineArray)
                        return;
                    bool isStaticInit = node.Modifiers.IsStatic() || node.Modifiers.IsConst();
                    CurrentClosure.RegisterTypeInitializer(() =>
                    {
                        //if (isInlineArray)
                        //{
                        //    //CurrentTypeWriter.Write(node, $"/*[InlineArray({inlineArraySize})] {type}*/");
                        //    CurrentTypeWriter.WriteLine(node, $"//InlineArray({inlineArraySize})", true);
                        //    CurrentTypeWriter.WriteLine(node, $"for (let $i = 0; $i < {inlineArraySize}; $i++)", true);
                        //    CurrentTypeWriter.WriteLine(node, "{", true);
                        //    CurrentTypeWriter.WriteLine(node, $"this.SetField($i, {defaultValue});", true);
                        //    CurrentTypeWriter.WriteLine(node, "}", true);

                        //    //CurrentTypeWriter.Write(node, _global.GlobalName);
                        //    //CurrentTypeWriter.Write(node, ".");
                        //    //CurrentTypeWriter.Write(node, Constants.CreateArray);
                        //    //CurrentTypeWriter.Write(node, "(");
                        //    //CurrentTypeWriter.Write(node, inlineArrayFieldType.ComputeOutputTypeName(_global));
                        //    //CurrentTypeWriter.Write(node, ", ");
                        //    //CurrentTypeWriter.Write(node, inlineArraySize.ToString());
                        //    //CurrentTypeWriter.Write(node, ")");
                        //}
                        //else
                        {
                            //If we are in a static initilizer, it is safe to use this as it reference the class prototype itself
                            CurrentTypeWriter.Write(node, $"{(isStaticInit ? "this"/*declaringSymbolMeta.InvocationName + "."*/ : "this")}", true);
                            CurrentTypeWriter.Write(node, ".");
                            CurrentTypeWriter.Write(node, fieldName);
                            CurrentTypeWriter.Write(node, " = ");
                            //Visit(var.Initializer);
                            if (var.Initializer != null)
                            {
                                if (!TryWriteConstant(node, fieldType, var.Initializer.Value))
                                    WriteVariableAssignment(node, null, symbol, null, var.Initializer.Value, null);
                            }
                            else
                            {
                                if (defaultValue != null)
                                    CurrentTypeWriter.Write(node, defaultValue);
                                else
                                {
                                    CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.DefaultTypeName}({fieldType.ComputeOutputTypeName(_global)})");
                                }
                            }
                            CurrentTypeWriter.WriteLine(node, ";");
                        }
                    }, isStaticInit);
                }
                bool memberInitialized = false;
                if (var.Initializer != null /*|| isInlineArray*/ || (fieldType.IsValueType && !fieldType.IsJsPrimitive()))
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
                        RegisterMemberInitializer();
                        memberInitialized = true;
                    }
                }
                if ((isFieldLayout /*|| isInlineArray*/ || (Constants.StructFieldAlwaysLayout && symbol.ContainingType.IsValueType)) &&
                    TryWriteFieldLayout(var, fieldSymbol ?? (ISymbol)eventSymbol!, fieldSymbol?.Type ?? eventSymbol!.Type, fieldMetadata.OverloadName ?? fieldName, modifier, $"{node.Declaration.Type.ToString().Trim()}"))
                {
                    if (!memberInitialized)
                        RegisterMemberInitializer();
                }
                else if (!fieldContainingTypeIsInlineArray)
                {
                    CurrentTypeWriter.Write(node, $"/*{node.Declaration.Type.ToString().Trim()}*/ {modifier}{(useStaticPropertyFunction ? "get " : "")}{fieldMetadata.OverloadName ?? fieldName}", true);
                    //if (useStaticPropertyFunction)
                    //{
                    //    Writer.Write(node, $"() {{ return {(node.Modifiers.IsConst() || node.Modifiers.IsStatic() ? $"{declaringSymbolMeta.InvocationName}." : "")}$_{fieldMetadata.OverloadName ?? fieldName} ??= ");
                    //}
                    if (isLiteralInit)
                    {
                        CurrentTypeWriter.Write(node, " = ");
                        if (!TryWriteConstant(node, fieldType, var.Initializer!.Value))
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

        public void WriteDelegateCombine(CSharpSyntaxNode node, CodeNode left, CodeNode right)
        {
            var delegateCombineMethod = _global.SystemDelegate
                .GetMembers("Combine")
                .OfType<IMethodSymbol>()
                .First(m => m.Parameters.Length == 2);
            VisitNode(left);
            CurrentTypeWriter.Write(node, " = ");
            WriteMethodInvocation(node, delegateCombineMethod, null, [left, right], null, null);
        }

        public void WriteDelegateRemove(CSharpSyntaxNode node, CodeNode left, CodeNode right)
        {
            var delegateRemoveMethod = _global.SystemDelegate
                .GetMembers("Remove")
                .OfType<IMethodSymbol>()
                .First(m => m.Parameters.Length == 2);
            VisitNode(left);
            CurrentTypeWriter.Write(node, " = ");
            WriteMethodInvocation(node, delegateRemoveMethod, null, [left, right], null, null);
        }

        void WriteEventAddRemove(EventFieldDeclarationSyntax node)
        {
            string? modifier = null;
            if (node.Modifiers.IsConst() || node.Modifiers.IsStatic())
            {
                modifier += "static ";
            }
            foreach (var variable in node.Declaration.Variables)
            {
                var symbol = _global.GetSymbol(variable, this/*, out _, out _*/);
                var metadata = _global.GetRequiredMetadata(symbol);

                CodeNode left = new CodeNode(() =>
                {
                    if (!symbol.IsStatic)
                    {
                        CurrentTypeWriter.Write(node, "this.");
                    }
                    CurrentTypeWriter.Write(node, metadata.InvocationName ?? symbol.Name);
                });
                CodeNode right = new CodeNode(() =>
                {
                    CurrentTypeWriter.Write(node, "value");
                });

                CurrentTypeWriter.WriteLine(node, $"{modifier} {metadata.OverloadName}$add(/*{node.Declaration.Type.ToString().Trim()}*/ value)", true);
                CurrentTypeWriter.WriteLine(node, "{", true);
                CurrentTypeWriter.Write(node, "", true);
                WriteDelegateCombine(node, left, right);
                CurrentTypeWriter.WriteLine(node, ";");
                CurrentTypeWriter.WriteLine(node, "}", true);

                CurrentTypeWriter.WriteLine(node, $"{modifier} {metadata.OverloadName}$remove(/*{node.Declaration.Type.ToString().Trim()}*/ value)", true);
                CurrentTypeWriter.WriteLine(node, "{", true);
                CurrentTypeWriter.Write(node, "", true);
                WriteDelegateRemove(node, left, right);
                CurrentTypeWriter.WriteLine(node, ";");
                CurrentTypeWriter.WriteLine(node, "}", true);
            }
        }

        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            if (node.Modifiers.IsPartial())
                return;
            WriteField(node);
            WriteEventAddRemove(node);
            //base.VisitEventFieldDeclaration(node);
        }

    }
}
