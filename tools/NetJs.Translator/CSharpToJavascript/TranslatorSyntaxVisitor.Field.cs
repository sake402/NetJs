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
        public bool IsFieldStructLayout(CSharpSyntaxNode? member, ISymbol? field, out int fieldOffset, out int fieldSize)
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
                var layout = _global.StructLayout(field.ContainingType, isStatic);
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
                            var layout = _global.StructLayout(field.ContainingType, isStatic);
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
            var isFieldStructLayout = IsFieldStructLayout(member, field, out fieldOffset, out fieldSize);
            bool fieldClassIsInineArray = _global.IsInlineArray(field.ContainingType, out inlineSize, out _);
            bool isFixedArray = _global.IsFixedSizeField(field, out inlineSize, out _);
            bool fieldTypeIsInlineArrayStruct = _global.IsInlineArray(fieldType, out inlineSize, out _);
            //Dont use field layout for boot classes, as they dont inherit from System.Object really
            if (
                //!isBootClass &&
                !field.IsStatic &&
                (isFieldStructLayout || fieldClassIsInineArray || isFixedArray) &&
                !field.ContainingType.IsType("System.Exception")/*Exception inherit native JS error, not object*/)
            {
                if (fieldClassIsInineArray && inlineSize > 0)
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
                    var isPureStruct = _global.IsPureStructType(field.ContainingType);
                    if (isPureStruct || fieldTypeIsInlineArrayStruct)
                    {
                        CurrentTypeWriter.WriteLine(member, $"/*{comment}*/ {modifier} get {fieldName}() {{ return this.{(field.IsStatic ? Constants.ObjectGetStaticField : Constants.ObjectGetField)}({fieldOffset}, {fieldType.ComputeOutputTypeName(_global)}); }}", true);
                        CurrentTypeWriter.WriteLine(member, $"/*{comment}*/ {modifier} set {fieldName}(value) {{ this.{(field.IsStatic ? Constants.ObjectSetStaticField : Constants.ObjectSetField)}({fieldOffset}, {fieldType.ComputeOutputTypeName(_global)}, value); }}", true);
                    }
                    else
                    {
                        var knownType = _global.KnownTypeFrom(fieldType);
                        string? flagFieldOffsetPrefix = null;
                        string? flagFieldSizePrefix = null;
                        //if (isPureStruct && knownType != KnownTypeHandle.Unknown)
                        //{
                        //    flagFieldSizePrefix = "0x" + (((int)knownType) << (32 - 6)).ToString("X") + "|";
                        //}
                        //else 
                        if (isFixedArray)
                        {
                            flagFieldSizePrefix = "0x80000000|";
                        }
                        CurrentTypeWriter.WriteLine(member, $"/*{comment}{(isFixedArray ? $"[{fieldSize}]" : "")}*/ {modifier} get {fieldName}() {{ return this.{(field.IsStatic ? Constants.ObjectGetStaticField : Constants.ObjectGetField)}({flagFieldOffsetPrefix}{fieldOffset}, {flagFieldSizePrefix}{fieldSize}); }}", true);
                        CurrentTypeWriter.WriteLine(member, $"/*{comment}{(isFixedArray ? $"[{fieldSize}]" : "")}*/ {modifier} set {fieldName}(value) {{ this.{(field.IsStatic ? Constants.ObjectSetStaticField : Constants.ObjectSetField)}({flagFieldOffsetPrefix}{fieldOffset}, {flagFieldSizePrefix}{fieldSize}, value); }}", true);
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

                var refKind = symbol.GetRefKind() ?? RefKind.None;
                //if (refKind != RefKind.None)
                //{

                //}
                var defaultValue = _global.GetDefaultValue(node.Declaration.Type, this);
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
                bool isPureStructMember = _global.IsPureStructType(symbol.ContainingType);

                void RegisterMemberInitializer()
                {
                    if (MemberWasMarkedInitializedByPrimaryConstructor(node, var.Initializer))
                    {
                    }
                    else
                    {
                        bool isStaticInit = node.Modifiers.IsStatic() || node.Modifiers.IsConst();
                        var initLocation = isStaticInit ? TypeInitializerLocation.DefaultStaticConstructor : TypeInitializerLocation.DefaultInstanceConstructor;

                        // if class has primary constructor and the field being initialized needs a parameter from the constructor, we initialize that field in the primary constructor
                        if (var.Initializer?.Value != null && ((CurrentType is ClassDeclarationSyntax cls && cls.ParameterList != null) || (CurrentType is StructDeclarationSyntax str && str.ParameterList != null)))
                        {
                            var parameters = (CurrentType as ClassDeclarationSyntax)?.ParameterList ?? (CurrentType as StructDeclarationSyntax)!.ParameterList;
                            if (MemberReferencesPrimaryConstructorParameter(var.Initializer.Value, parameters!.Parameters))
                            {
                                initLocation = TypeInitializerLocation.PrimaryConstructor;
                            }
                        }

                        CurrentClosure.RegisterTypeInitializer(() =>
                        {
                            if (_global.IsFixedSizeField(symbol, out var fSize, out _))
                            {
                                CurrentTypeWriter.WriteLine(node, $"//FixedSizeArray({fieldType}, {fSize})", true);
                                var variableName = $"$t{++CurrentTypeWriter.CurrentClosure.NameManglingSeed}";
                                CurrentTypeWriter.Write(node, "let ", true);
                                CurrentTypeWriter.Write(node, variableName);
                                CurrentTypeWriter.Write(node, " = ");
                                CurrentTypeWriter.Write(node, $"{(isStaticInit ? "this"/*declaringSymbolMeta.InvocationName + "."*/ : "this")}");
                                CurrentTypeWriter.Write(node, ".");
                                CurrentTypeWriter.Write(node, fieldName);
                                CurrentTypeWriter.WriteLine(node, ";");
                                if (fieldType.IsPointer(out var pointedType))
                                {
                                    //Array.AddMetadata to the proxy serving this fixed array
                                    CurrentTypeWriter.Write(node, "", true);
                                    WriteMethodInvocation(node, "System.Array.AddMetadata", arguments: [
                                        new CodeNode(()=> CurrentTypeWriter.Write(node, variableName)),
                                    new CodeNode(()=> {
                                        CurrentTypeWriter.Write(node, pointedType.ComputeOutputTypeName(_global));
                                        CurrentTypeWriter.Write(node, ".");
                                        CurrentTypeWriter.Write(node, Constants.PrototypeTypeName);
                                    })
                                    ]);
                                    CurrentTypeWriter.WriteLine(node, ";");
                                }
                                CurrentTypeWriter.WriteLine(node, $"for (let $i = 0; $i < {fSize}; $i++)", true);
                                CurrentTypeWriter.WriteLine(node, "{", true);
                                CurrentTypeWriter.WriteLine(node, $"{variableName}[$i] = {defaultValue};", true);
                                CurrentTypeWriter.WriteLine(node, "}", true);
                            }
                            else
                            {
                                if (fieldContainingTypeIsInlineArray)
                                {
                                    var defaultValue = _global.GetDefaultValue(fieldType, true);
                                    CurrentTypeWriter.WriteLine(node, $"//InlineArray({fieldType}, {inlineArraySize})", true);
                                    CurrentTypeWriter.WriteLine(node, $"for (let $i = {inlineArraySize - 1}; $i >= 0; $i--)", true);
                                    CurrentTypeWriter.WriteLine(node, "{", true);
                                    if (isPureStructMember)
                                        CurrentTypeWriter.WriteLine(node, $"this.{Constants.ObjectSetField}($i, {fieldType.ComputeOutputTypeName(_global)}, {defaultValue});", true);
                                    else
                                        CurrentTypeWriter.WriteLine(node, $"this.{Constants.ObjectSetField}($i, 1, {defaultValue});", true);
                                    CurrentTypeWriter.WriteLine(node, "}", true);
                                    CurrentTypeWriter.Write(node, "", true);
                                    WriteMethodInvocation(node, "System.Array.AddMetadata", arguments: [
                                        new CodeNode(()=>
                                    {
                                        CurrentTypeWriter.Write(node, $"this.{Constants.StructFieldsLayoutName}");
                                    }),
                                    new CodeNode(()=>
                                    {
                                        CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.TypeOf}({fieldType.ComputeOutputTypeName(_global)})");
                                    })
                                    ]);
                                    CurrentTypeWriter.WriteLine(node, "");
                                }
                                else
                                {
                                    if (initLocation == TypeInitializerLocation.PrimaryConstructor)
                                    {
                                        CurrentTypeWriter.WriteLine(node, $"//depends on primary constructor parameter", true);
                                    }
                                    //If we are in a static initilizer, it is safe to use this as it reference the class prototype itself
                                    CurrentTypeWriter.Write(node, $"{(isStaticInit ? "this"/*declaringSymbolMeta.InvocationName + "."*/ : "this")}", true);
                                    CurrentTypeWriter.Write(node, ".");
                                    CurrentTypeWriter.Write(node, fieldName);
                                    CurrentTypeWriter.Write(node, " = ");
                                    //Visit(var.Initializer);
                                    if (var.Initializer != null)
                                    {
                                        if (!TryWriteConstant(node, fieldType, var.Initializer.Value))
                                        {
                                            ////We are inside js default constructor, if the node.Initializer.Value is a primary constructor parameter, we should not write it as that variable is undefined here
                                            //var symbol = _global.TryGetSymbol(var.Initializer.Value, this);
                                            //if (symbol != null && symbol.Kind == SymbolKind.Parameter)
                                            //{
                                            //    if (defaultValue != null)
                                            //        CurrentTypeWriter.Write(node, defaultValue);
                                            //    else
                                            //    {
                                            //        CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.DefaultTypeName}({fieldType.ComputeOutputTypeName(_global)})");
                                            //    }
                                            //}
                                            //else
                                            WriteVariableAssignment(node, null, symbol, null, var.Initializer.Value, null);
                                        }
                                    }
                                    else
                                    {
                                        if (defaultValue != null)
                                            CurrentTypeWriter.Write(node, defaultValue);
                                        else
                                        {
                                            if (refKind != RefKind.None)
                                            {
                                                WriteMethodInvocation(node, "System.Runtime.CompilerServices.Unsafe.NullRef", null, null, [fieldType]);
                                            }
                                            else
                                            {
                                                CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.DefaultTypeName}({fieldType.ComputeOutputTypeName(_global)})");
                                            }
                                        }
                                    }
                                    CurrentTypeWriter.WriteLine(node, ";");
                                }
                            }
                        }, initLocation);
                    }
                }

                bool memberInitialized = false;
                if (var.Initializer != null || (fieldType.IsValueType && !fieldType.IsJsPrimitive()))
                {
                    //If we initialize a field from a primary constructor parameter, this is already handled in the primary constructor generator (WritePrimaryConstructor)
                    //We should skip it here
                    if (MemberWasMarkedInitializedByPrimaryConstructor(var, var.Initializer))
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
                var symbol = (IEventSymbol)_global.GetSymbol(variable, this/*, out _, out _*/);
                var metadata = _global.GetRequiredMetadata(symbol);
                var addMetadata = symbol.AddMethod != null ? _global.GetMetadata(symbol.AddMethod) : null;
                var removeMetadata = symbol.RemoveMethod != null ? _global.GetMetadata(symbol.RemoveMethod) : null;

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

                CurrentTypeWriter.WriteLine(node, $"{modifier}{addMetadata?.OverloadName ?? ("add_" + (metadata?.OverloadName ?? symbol.Name))}(/*{node.Declaration.Type.ToString().Trim()}*/ value)", true);
                CurrentTypeWriter.WriteLine(node, "{", true);
                CurrentTypeWriter.Write(node, "", true);
                WriteDelegateCombine(node, left, right);
                CurrentTypeWriter.WriteLine(node, ";");
                CurrentTypeWriter.WriteLine(node, "}", true);

                CurrentTypeWriter.WriteLine(node, $"{modifier}{removeMetadata?.OverloadName ?? ("remove_" + (metadata?.OverloadName ?? symbol.Name))}(/*{node.Declaration.Type.ToString().Trim()}*/ value)", true);
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
            if (node.Parent.IsKind(SyntaxKind.InterfaceDeclaration))
                return;
            WriteField(node);
            WriteEventAddRemove(node);
            //base.VisitEventFieldDeclaration(node);
        }

    }
}
