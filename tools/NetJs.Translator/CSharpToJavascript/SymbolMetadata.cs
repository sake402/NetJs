using Microsoft.CodeAnalysis;
using YamlDotNet.Core.Tokens;

namespace NetJs.Translator.CSharpToJavascript
{
    public record class SymbolMetadata
    {
        GlobalCompilationVisitor _global;
        public SymbolExport Export { get; set; }
        public SymbolMetadata(GlobalCompilationVisitor global, SymbolExport? exported)
        {
            _global = global;
            Export = exported ?? new SymbolExport();
        }

        public bool Preprocessed { get; set; }
        /// <summary>
        /// Name is only unique within the type. Declared symbol and overriden symbol will share this name within the type
        /// </summary>
        public string Signature { get; set; }
        //{
        //    get => Export.Signature;
        //    set => Export.Signature = value;
        //}
        /// <summary>
        /// Name is unique globally
        /// </summary>
        public string OriginalFullName { get; set; } = default!;
        /// <summary>
        /// Name is unique globally
        /// </summary>
        public string UniqueFullName { get; set; } = default!;

        string? TryUpdateInterfaceDispatchName(string? name)
        {
            if (name != null && Symbol.UseIndirectInterfaceMemberDispatch(_global, out var needRuntimeNameLookup, out var arguments))
            {
                Span<char> buffer = stackalloc char[1024];
                ValueStringBuilder sb = new(buffer);
                if (needRuntimeNameLookup)
                {
                    sb.Append("[`");
                }
                var memberNameIndex = name.LastIndexOf('$');
                var nameEnd = memberNameIndex;
                if (memberNameIndex > 0)
                {
                    while (name[nameEnd] == '$')
                        nameEnd--;
                    sb.Append(name, 0, nameEnd + 1);
                }
                else
                    sb.Append(name);
                void WriteTypeSymbol(ITypeSymbol type, ref ValueStringBuilder sb)
                {
                    string TypeString(ITypeSymbol type)
                    {
                        if (type.Kind == SymbolKind.TypeParameter)
                        {
                            return $"${{{type.Name}}}.{Constants.PrototypeFullNameWithSpecialSeperator}('$')";
                        }
                        return type.ToString()!.Split('<')[0].TrimEnd('?').ResolvePredefinedTypeName().Replace("+", "$").Replace(".", "$");
                    }
                    bool isArray = false;
                    ITypeSymbol? arrayElementType = null;
                    var fn = type.SpecialType == SpecialType.System_Array ? "System$Array" :
                        (isArray = type.IsArray(out arrayElementType)) ? $"System$Array" :
                        type.IsTupleType ? "System$ValueTuple" : TypeString(type);
                    sb.Append(fn);
                    if (isArray && arrayElementType != null)
                    {
                        WriteArguments([arrayElementType], ref sb);
                    }
                    if (type.Kind == SymbolKind.NamedType && type is INamedTypeSymbol nt && nt.IsGenericType)
                    {
                        WriteArguments(nt.TypeArguments, ref sb);
                    }
                }
                void WriteArguments(IEnumerable<ITypeSymbol> arguments, ref ValueStringBuilder sb)
                {
                    int ix = 0;
                    sb.Append("$");
                    foreach (var arg in arguments)
                    {
                        if (ix > 0)
                            sb.Append("$");
                        if (arg.Kind == SymbolKind.TypeParameter)
                        {
                            sb.Append("${");
                            sb.Append(arg.Name);
                            sb.Append(".");
                            sb.Append(Constants.PrototypeFullNameWithSpecialSeperator);
                            sb.Append("('$')}");
                        }
                        else
                        {
                            WriteTypeSymbol(arg, ref sb);
                        }
                        ix++;
                    }
                    sb.Append("$");
                }
                WriteArguments(arguments, ref sb);
                if (memberNameIndex > 0)
                    sb.Append(name, memberNameIndex + 1, name.Length - memberNameIndex - 1);
                if (needRuntimeNameLookup)
                {
                    sb.Append("`]");
                }
                name = sb.ToString();
                //if (!needRuntimeNameLookup)
                //{
                //    //Must be in sync with what NetJs.$iface(_global.GlobalName.Constants.IntefaceMemberMetadataName) does in NetJs.Boot.js
                //    name = $"{name}_{string.Join("_", arguments.Select(t => t.CreateSignature(_global, withTypeParameterNames: true, withGlobalNamespace: false, withAssemblySlugNamespace: false).Replace(".", "_")))}";
                //}
                //else
                //    name = $"[{_global.GlobalName}.{Constants.IntefaceMemberMetadataName}(\"{name}\", {string.Join(", ", arguments.Select(t => t.Kind == SymbolKind.TypeParameter ? t.Name : t.ComputeOutputTypeName(_global)))})]";
            }
            return name;
        }

        string? __originalOverloadName;
        public string? GivenOriginalOverloadName => __originalOverloadName;
        string? _originalOverloadName;
        /// <summary>
        /// If a name is shortened, this hold the original name before being shortened. Otherwise it is the same as the overload name
        /// </summary>
        public string? OriginalOverloadName
        {
            get => _originalOverloadName;
            set
            {
                if (value == null)
                    throw new InvalidOperationException("Original name cannot be null");
                __originalOverloadName = value;
                _originalOverloadName = TryUpdateInterfaceDispatchName(value);
            }
        }

        string? __overloadName;
        public string? GivenOverloadName => __overloadName;
        string? _overloadName;
        public string? OverloadName
        {
            get => _overloadName;
            set
            {
                //value.ValidateJsName(allowDot: true, allowComma: true, allowSpace: true);
                __overloadName = value;
                _overloadName = value;
                _overloadName = TryUpdateInterfaceDispatchName(value);
            }
        }

        void InitializeTypeName()
        {
            if (Symbol.Kind == SymbolKind.Namespace && Symbol is INamespaceSymbol nnamespace)
            {
                _originalInvocationName = OverloadName;
                _invocationName = OverloadName;
            }
            else if (Symbol is ITypeSymbol ttype)
            {
                bool isExtern = Symbol.IsExtern || _global.HasAttribute(Symbol, typeof(ExternalAttribute).FullName!, null, false, out _);
                if ((ttype is not INamedTypeSymbol nt || nt.IsExtension) && ttype.ContainingType != null)
                {
                    //type.OriginalInvocationName = (originalPrefixInvocationName != null ? originalPrefixInvocationName + "." : "") + overloadedName;
                    //type.InvocationName = ComputeInvocatioNameForType(ttype, type.OverloadName);
                    _originalInvocationName = ComputeInvocatioNameForType(ttype, _overloadName, _global);
                    _invocationName = _originalInvocationName;
                    //TODO: this will not work when shortname is actually enabled
                    //_invocationName = ShortName(_global, Signature, _originalInvocationName, _global.Symbols.Types, generate: !isExtern, export: false);
                    //type.InvocationName = ShortName(shortPrefixInvocationName, originalPrefixInvocationName, type.Signature, overloadedName, ShortNames.Types, generate: !isExtern);
                }
                else
                {
                    _originalInvocationName = ComputeInvocatioNameForType(ttype, OriginalOverloadName, _global);
                    _invocationName = ComputeInvocatioNameForType(ttype, OverloadName, _global);
                }
            }
            else if (Symbol.Kind == SymbolKind.Field && Symbol is IFieldSymbol ffield)
            {
                _originalInvocationName = ComputeInvocationNameForField(ffield, OriginalOverloadName, _global);
                _invocationName = ComputeInvocationNameForField(ffield, OverloadName, _global);
            }
            else if (Symbol.Kind == SymbolKind.Event && Symbol is IEventSymbol eevent)
            {
                _originalInvocationName = ComputeInvocationNameForEvent(eevent, OriginalOverloadName, _global);
                _invocationName = ComputeInvocationNameForEvent(eevent, OverloadName, _global);
            }
            else if (Symbol.Kind == SymbolKind.Property && Symbol is IPropertySymbol pproperty)
            {
                _originalInvocationName = ComputeInvocatioNameForProperty(pproperty, OriginalOverloadName, _global);
                _invocationName = ComputeInvocatioNameForProperty(pproperty, OverloadName, _global);
            }
            else if (Symbol.Kind == SymbolKind.Method && Symbol is IMethodSymbol mmethod)
            {
                _originalInvocationName = ComputeInvocatioNameForMethod(mmethod, OriginalOverloadName, _global);
                _invocationName = ComputeInvocatioNameForMethod(mmethod, OverloadName, _global);
                //for implemented interface members that may conflict in name:
                //eg if a class implement both IComparer<string> and IComparer<char>
                //the compare implementation methods for both implementation will conflictly be named System$Collections$Generic$IComparer$$$Compare
                //We need to discriminate these using their type arguments
                //if (mmethod.ContainingType.TypeKind == TypeKind.Interface && mmethod.ContainingType.TypeArguments.Any() && mmethod.ContainingType.TypeArguments.All(a => a is INamedTypeSymbol))
                //{
                //    var argNames = string.Join("$$", mmethod.ContainingType.TypeArguments.Select(t => t.ComputeOutputTypeName(_global).Replace(".", "$")));
                //    _originalInvocationName += "$$" + argNames;
                //    _invocationName += "$" + argNames;
                //}
            }
            //Handles primary constructor parameter created as field
            else if (Symbol.Kind == SymbolKind.TypeParameter && Symbol is IParameterSymbol pparameter)
            {
                _originalInvocationName = ComputeInvocationNameForParameterField(pparameter, OriginalOverloadName, _global);
                _invocationName = ComputeInvocationNameForParameterField(pparameter, OverloadName, _global);
            }
        }

        string? _originalInvocationName;
        /// <summary>
        /// If a name is shortened, this hold the original name before being shortened. Otherwise it is the same as the overload name
        /// </summary>
        public string? OriginalInvocationName
        {
            get
            {
                if (_originalInvocationName != null)
                    return _originalInvocationName;
                InitializeTypeName();
                return _originalInvocationName;
            }
        }

        string? _invocationName;
        //TODO: We should make invoation name take TranslatorVisitor,
        //If a static member is accesses within the same class, use this.Member, albeit static, instead of Namespace.Class.Member.
        //This way we can save some characters for static member access within the same class, which is more common
        public string? InvocationName
        {
            get
            {
                if (_invocationName != null)
                    return _invocationName;
                InitializeTypeName();
                return _invocationName;
            }
        }

        ISymbol _symbol = default!;
        public ISymbol Symbol
        {
            get => _symbol;
            set
            {
                _symbol = value;
                if (__originalOverloadName != null)
                    _originalOverloadName = TryUpdateInterfaceDispatchName(__originalOverloadName);
                if (__overloadName != null)
                    _overloadName = TryUpdateInterfaceDispatchName(__overloadName);
                _invocationName = null;
                _originalInvocationName = null;
            }
        }
        public IEnumerable<SyntaxReference> DeclaringReferences { get; set; } = default!;
        //public IEnumerable<SyntaxNode> Nodes { get; set; } = default!;

        public override string ToString()
        {
            return UniqueFullName;
        }

        //const string ShortenedNameIdentitfier = "\\";
        //public static string ShortName(
        //    GlobalCompilationVisitor _global,
        //    //string? shortPrefix, 
        //    //string? longPrefix, 
        //    string signature,
        //    string name, 
        //    Dictionary<string, SymbolValue> exportNames, 
        //    bool generate = true,
        //    bool export = true)
        //{
        //    if (!generate || !_global.BuildFlags.HasFlag(NetJsBuildFlags.ShortNames))
        //    {
        //        var resolvedName = /*(!string.IsNullOrEmpty(shortPrefix) ? shortPrefix + "." : "") +*/ name;
        //        if (export)
        //        {
        //            //var key = resolvedName;
        //            //if (resolvedName != name)
        //            //{
        //            //    key = name + "|" + resolvedName;
        //            //}
        //            //if (signature== "Ceiling($spc.System.Runtime.Intrinsics.Vector256<$spc.System.Double>)$$2")
        //            //{

        //            //}
        //            exportNames.Add(resolvedName, new SymbolValue { Signature = signature });
        //        }
        //        return resolvedName;
        //    }
        //    if (name.Length <= 3)
        //        return name;
        //    var shortenSegment = name;
        //    bool hasGlobal = false;
        //    if (name.StartsWith(_global.GlobalName) && name[_global.GlobalName.Length] == '.')
        //    {
        //        hasGlobal = true;
        //        shortenSegment = name.Substring(_global.GlobalName.Length + 1);
        //    }
        //    //string? keepSuffix = null;
        //    //if (shortenSegment.EndsWith("$") || (char.IsDigit(shortenSegment[shortenSegment.Length - 1]) && shortenSegment.Contains('$')))
        //    //{
        //    //    int l = shortenSegment.Length;
        //    //    while (char.IsDigit(shortenSegment[l - 1]))
        //    //    {
        //    //        l--;
        //    //    }
        //    //    while (shortenSegment[l - 1] == '$')
        //    //    {
        //    //        l--;
        //    //    }
        //    //    keepSuffix = shortenSegment.Substring(l);
        //    //    shortenSegment = shortenSegment.Substring(0, l);
        //    //}
        //    string shortName = "";
        //    bool startSingleCharacterCapture = true;
        //    List<char> possibleCamelCaseNameOverloadVariations = new List<char>();
        //    for (int i = 0; i < shortenSegment.Length; i++)
        //    {
        //        if (_global.BuildFlags.HasFlag(NetJsBuildFlags.ShortNamesTryUseCamelCase))
        //        {
        //            if (i > 0 && char.IsUpper(shortenSegment[i]) && char.IsLower(shortenSegment[i - 1]))
        //            {
        //                possibleCamelCaseNameOverloadVariations.Add(shortenSegment[i]);
        //            }
        //        }
        //        if (startSingleCharacterCapture)
        //        {
        //            if (shortenSegment[i] != ShortenedNameIdentitfier[0]) //this segment is already shortened, dont shorten it again
        //            {
        //                shortName += /*ShortenedNameIdentitfier +*/ shortenSegment[i];
        //            }
        //            startSingleCharacterCapture = false;
        //            possibleCamelCaseNameOverloadVariations.Clear();
        //        }
        //        else if (shortenSegment[i] == '.' || shortenSegment[i] == '_' || shortenSegment[i] == '$')
        //        {
        //            if (shortenSegment[i] != '_')
        //                shortName += /*ShortenedNameIdentitfier +*/ shortenSegment[i];
        //            if (i < shortenSegment.Length - 1 && shortenSegment[i + 1] == '$')
        //            {
        //                while (i < shortenSegment.Length - 1 && shortenSegment[i + 1] == '$') //keep generic argument $ marker
        //                {
        //                    shortName += '$';
        //                    i++;
        //                }
        //                //startSingleCharacterCapture = false;
        //            }
        //            //else
        //            startSingleCharacterCapture = true;
        //        }
        //    }
        //    //var splitted = shortenSegment.Split(['.','$'], StringSplitOptions.RemoveEmptyEntries);
        //    //var shortName = string.Join(".", splitted.Select(s => s[0]));
        //    //splitted = shortName.Split(['$'], StringSplitOptions.RemoveEmptyEntries);
        //    //shortName = string.Join("$", splitted.Select(s => s[0]));
        //    if (hasGlobal)
        //    {
        //        shortName = _global.GlobalName + "." + shortName;
        //    }
        //    //shortName = (shortPrefix != null ? shortPrefix + "." : "") + shortName;
        //    string? padded = null;
        //    //if we can form a unique name using its camel case pattern, the use that
        //    if (possibleCamelCaseNameOverloadVariations.Count > 0)
        //    {
        //        string sn = shortName;
        //        padded = "";
        //        int i = 0;
        //        while (exportNames.TryGetValue(sn, out _))
        //        {
        //            if (i >= possibleCamelCaseNameOverloadVariations.Count)
        //                break;
        //            sn += possibleCamelCaseNameOverloadVariations[i];
        //            i++;
        //        }
        //        if (!exportNames.TryGetValue(sn, out _))
        //        {
        //            shortName = sn;
        //        }
        //    }
        //    int nextTry = 1;
        //    padded = null;
        //    while (exportNames.TryGetValue(shortName, out _))
        //    {
        //        if (padded != null)
        //            shortName = shortName.Substring(0, shortName.Length - padded.Length);
        //        padded = nextTry.ToString();
        //        shortName += padded;
        //        nextTry++;
        //    }
        //    exportNames.Add(shortName, new SymbolValue { Signature = signature });
        //    //usedNames.Add(shortName, (longPrefix != null ? longPrefix + "." : "") + name + suffix);
        //    //shortName += keepSuffix;
        //    return shortName;
        //}

        static string ComputeInvocatioNameForType(ITypeSymbol type, string? overloadName, GlobalCompilationVisitor _global)
        {
            //if (type is INamedTypeSymbol ts && ts.IsExtension)
            //{

            //}
            if (type.IsAnonymousType)
            {
                return ComputeInvocatioNameForType(_global.SystemObject, null, _global);
            }
            //We are not testing for [External] because those types still exist, only JS world, do they are not deleted
            if (!type.IsNullable(out _) && _global.HasAnyAttribute(type, null, false, typeof(ObjectLiteralAttribute).FullName!, typeof(NonScriptableAttribute).FullName!))
            {
                return $"/*{overloadName?.Replace("/*", "").Replace("*/", "")}*/{_global.DeletedObject.ComputeOutputTypeName(_global)}";
            }
            var assembly = type.ContainingAssembly;
            if (type.Kind == SymbolKind.ErrorType)
                return "";
            if (type is INamedTypeSymbol tt && tt.IsNullable(out var ntt))
            {
                if (ntt!.Kind != SymbolKind.ErrorType && ntt.Kind != SymbolKind.TypeParameter && !ntt.IsValueType)
                {
                    type = ntt;
                }
            }
            if (type.IsArray(out var elementType, false))
            {
                return $"{_global.GlobalName}.{Constants.TypeArrayName}({ComputeInvocatioNameForType(elementType, null, _global)})";
            }
            if (type.IsPointer(out var pointedType))
            {
                return $"{_global.GlobalName}.{Constants.TypePointerName}({ComputeInvocatioNameForType(pointedType, null, _global)})";
            }
            if (type is ITypeParameterSymbol tp)
                return tp.Name;
            if (overloadName == null)
            {
                var typeMeta = _global.GetRequiredMetadata(type);
                overloadName = typeMeta.OverloadName ?? throw new InvalidOperationException("Containing type must be processed before contained type");
            }
            string invocationName = overloadName ?? type.Name;
            if (_global.BuildFlags.HasFlag(NetJsBuildFlags.Global))
            {
                if (type.ContainingSymbol is INamedTypeSymbol container && !_global.HasAttribute(type, typeof(ExternalAttribute).FullName!, null, false, out _))
                {
                    //inner type must always bear path to their parent type,
                    //only their name we need to concat to the parent name
                    overloadName = overloadName?.Split('.').Last();
                    //inner type
                    var containingType = _global.GetRequiredMetadata(container);
                    if (containingType.InvocationName == null)
                        throw new InvalidOperationException("Containing type must be processed before contained type");
                    if (type is INamedTypeSymbol nt2 && nt2.IsExtension)
                    {
                        invocationName = containingType.InvocationName;
                    }
                    else
                        invocationName = containingType.InvocationName + "." + (overloadName ?? type.Name);
                }
                else
                {
                    //if (overloadName != null && Constants.MinifyNamespace)
                    //{
                    //    overloadName = MinifyNamespace(overloadName);
                    //}
                    invocationName = overloadName ?? type.Name;
                }
            }
            if (type is INamedTypeSymbol nt && nt.Arity > 0)
            {
                invocationName += "(";
                if (nt.TypeArguments.Any(t => t.Kind != SymbolKind.ErrorType))
                {
                    invocationName += string.Join(", ", nt.TypeArguments.Select(c => ComputeInvocatioNameForType(c, null, _global)));
                }
                invocationName += ")";
            }
            return invocationName;
        }

        static string ComputeInvocationNameForField(IFieldSymbol field, string? overloadName, GlobalCompilationVisitor _global)
        {
            if (overloadName == null)
            {
                var typeMeta = _global.GetRequiredMetadata(field);
                overloadName = typeMeta.OverloadName ?? throw new InvalidOperationException("Containing type must be processed before contained type");
            }
            var invocationName = overloadName;
            if (field.IsStatic)
            {
                var declaringType = field.ContainingType;
                var declaringTypeMetadata = _global.GetRequiredMetadata(declaringType);
                invocationName = declaringTypeMetadata.InvocationName + "." + overloadName;
            }
            return invocationName;
        }

        static string ComputeInvocationNameForEvent(IEventSymbol eevent, string? overloadName, GlobalCompilationVisitor _global)
        {
            if (overloadName == null)
            {
                var typeMeta = _global.GetRequiredMetadata(eevent);
                overloadName = typeMeta.OverloadName ?? throw new InvalidOperationException("Containing type must be processed before contained type");
            }
            var invocationName = overloadName;
            if (eevent.IsStatic)
            {
                var declaringType = eevent.ContainingType;
                var declaringTypeMetadata = _global.GetRequiredMetadata(declaringType);
                bool isIndirectInterfaceCall = eevent.UseIndirectInterfaceMemberDispatch(_global, out var needRuntimeNameLookup, out _) && needRuntimeNameLookup;
                invocationName = declaringTypeMetadata.InvocationName + (!isIndirectInterfaceCall ? "." : "") + overloadName;
            }
            //if (eevent.ContainingType.TypeKind == TypeKind.Interface && eevent.ContainingType.IsGenericType)
            //{
            //    invocationName = $"[{_global.GlobalName}.{Constants.IntefaceMemberMetadataName}(\"{invocationName}\", {string.Join(", ", eevent.ContainingType.TypeArguments.Select(t => t.ComputeOutputTypeName(_global)))})]";
            //}
            return invocationName;
        }

        static string ComputeInvocatioNameForProperty(IPropertySymbol property, string? overloadName, GlobalCompilationVisitor _global)
        {
            if (overloadName == null)
            {
                var typeMeta = _global.GetRequiredMetadata(property);
                overloadName = typeMeta.OverloadName ?? throw new InvalidOperationException("Containing type must be processed before contained type");
            }
            var invocationName = overloadName;
            if (property.IsStatic || property.IsStaticCallConvention(_global))
            {
                var declaringType = property.ContainingType;
                var declaringTypeMetadata = _global.GetRequiredMetadata(declaringType);
                bool isIndirectInterfaceCall = property.UseIndirectInterfaceMemberDispatch(_global, out var needRuntimeNameLookup, out _) && needRuntimeNameLookup;
                invocationName = declaringTypeMetadata.InvocationName + (!isIndirectInterfaceCall ? "." : "") + invocationName;
            }
            //if (property.ContainingType.TypeKind == TypeKind.Interface && property.ContainingType.IsGenericType)
            //{
            //    invocationName = $"[{_global.GlobalName}.{Constants.IntefaceMemberMetadataName}(\"{invocationName}\", {string.Join(", ", property.ContainingType.TypeArguments.Select(t => t.ComputeOutputTypeName(_global)))})]";
            //}
            return invocationName;
        }

        static string ComputeInvocatioNameForMethod(IMethodSymbol method, string? overloadName, GlobalCompilationVisitor _global)
        {
            var invocationName = overloadName ?? method.Name;
            if (Constants.GenericMethodAsFactory && method.Arity > 0)
            {
                if (!_global.HasAttribute(method, typeof(IgnoreGenericAttribute).FullName, null, false, out _))
                {
                    invocationName += "(";
                    invocationName += string.Join(",", method.TypeArguments.Select(c => ComputeInvocatioNameForType(c, null, _global)));
                    invocationName += ")";
                }
            }
            /*if (method.AssociatedSymbol?.Kind == SymbolKind.Event)
            {
                var eevent = (IEventSymbol)method.AssociatedSymbol;
                if (SymbolEqualityComparer.Default.Equals(method, eevent.AddMethod))
                {
                    invocationName = "add_" + invocationName;
                }
                else
                {
                    invocationName = "remove_" + invocationName;
                }
            }
            else*/
            if (method.IsStatic || (method.IsStaticCallConvention(_global) && !method.IsVirtual))
            {
                var declaringType = method.ContainingType;
                var declaringTypeMetadata = _global.GetRequiredMetadata(declaringType);
                bool isIndirectInterfaceCall = method.UseIndirectInterfaceMemberDispatch(_global, out var needRuntimeNameLookup, out _) && needRuntimeNameLookup;
                invocationName = declaringTypeMetadata.InvocationName + (!isIndirectInterfaceCall ? "." : "") + invocationName;
            }
            else if (method.AssociatedSymbol?.Kind == SymbolKind.Property && method.AssociatedSymbol.IsExtensionPropertyMember(_global) && !method.AssociatedSymbol.IsStatic)
            {
                var declaringType = method.AssociatedSymbol.ContainingType;
                var declaringTypeMetadata = _global.GetRequiredMetadata(declaringType);
                bool isIndirectInterfaceCall = method.UseIndirectInterfaceMemberDispatch(_global, out var needRuntimeNameLookup, out _) && needRuntimeNameLookup;
                invocationName = declaringTypeMetadata.InvocationName + (!isIndirectInterfaceCall ? "." : "") + invocationName;
            }
            //if (method.ContainingType.TypeKind == TypeKind.Interface&& method.ContainingType.IsGenericType)
            //{
            //    invocationName = $"[{_global.GlobalName}.{Constants.IntefaceMemberMetadataName}(\"{invocationName}\", {string.Join(", ", method.ContainingType.TypeArguments.Select(t => t.ComputeOutputTypeName(_global)))})]";
            //}
            return invocationName;
        }


        static string ComputeInvocationNameForParameterField(IParameterSymbol field, string? overloadName, GlobalCompilationVisitor _global)
        {
            if (overloadName == null)
            {
                var typeMeta = _global.GetRequiredMetadata(field);
                overloadName = typeMeta.OverloadName ?? throw new InvalidOperationException("Containing type must be processed before contained type");
            }
            var invocationName = overloadName;
            if (field.IsStatic)
            {
                var declaringType = field.ContainingType;
                var declaringTypeMetadata = _global.GetRequiredMetadata(declaringType);
                invocationName = declaringTypeMetadata.InvocationName + "." + overloadName;
            }
            return invocationName;
        }

    }
}