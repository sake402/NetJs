using LivingThing.Core.Frameworks.Common.OneOf;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using NuGet.Protocol;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.SymbolStore;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static NetJs.Translator.OneOf.Types.TrueFalseOrNull;

namespace NetJs.Translator.CSharpToJavascript
{

    public static class Utilities
    {
        public static bool IsExtern(this SyntaxTokenList modifiers)
        {
            return modifiers.Any(e => e.ValueText == "extern");
        }

        public static bool IsStatic(this SyntaxTokenList modifiers)
        {
            return modifiers.Any(e => e.ValueText == "static");
        }

        public static bool IsConst(this SyntaxTokenList modifiers)
        {
            return modifiers.Any(e => e.ValueText == "const");
        }

        public static bool IsReadOnly(this SyntaxTokenList modifiers)
        {
            return modifiers.Any(e => e.ValueText == "readonly");
        }

        public static bool IsRef(this SyntaxTokenList modifiers)
        {
            return modifiers.Any(e => e.ValueText == "ref");
        }

        public static bool IsIn(this SyntaxTokenList modifiers)
        {
            return modifiers.Any(e => e.ValueText == "in");
        }

        public static bool IsOut(this SyntaxTokenList modifiers)
        {
            return modifiers.Any(e => e.ValueText == "out");
        }

        public static bool IsFixed(this SyntaxTokenList modifiers)
        {
            return modifiers.Any(e => e.ValueText == "fixed");
        }

        public static bool IsPartial(this SyntaxTokenList modifiers)
        {
            return modifiers.Any(e => e.ValueText == "partial");
        }

        public static bool IsPrivate(this SyntaxTokenList modifiers)
        {
            return modifiers.Any(e => e.ValueText == "private");
        }

        public static bool IsPublic(this SyntaxTokenList modifiers)
        {
            return modifiers.Any(e => e.ValueText == "public");
        }

        public static bool IsAsync(this SyntaxTokenList modifiers)
        {
            return modifiers.Any(e => e.ValueText == "async");
        }

        public static bool IsAbstract(this SyntaxTokenList modifiers)
        {
            return modifiers.Any(e => e.ValueText == "abstract");
        }

        public static bool IsVirtual(this SyntaxTokenList modifiers)
        {
            return modifiers.Any(e => e.ValueText == "virtual");
        }

        //get a type defined within another type/method
        public static BaseTypeDeclarationSyntax? GetTypeIn(this MemberDeclarationSyntax member, string typeName)
        {
            return (BaseTypeDeclarationSyntax?)member.ChildNodes().FirstOrDefault(c => c is BaseTypeDeclarationSyntax t && t.Identifier.ValueText == typeName);
        }

        public static T? FindClosestParent<T>(this SyntaxNode source, Func<T, bool>? isCandidate = null)
        {
            var current = source;
            while (current != null)
            {
                if (current is T t && (isCandidate?.Invoke(t) ?? true))
                    return t;
                current = current.Parent;
            }
            return default;
        }

        public static T? FindClosestParent<T>(this IOperation source, Func<T, bool>? isCandidate = null)
        {
            var current = source;
            while (current != null)
            {
                if (current is T t && (isCandidate?.Invoke(t) ?? true))
                    return t;
                current = current.Parent;
            }
            return default;
        }

        public static IEnumerable<T> FindDescendant<T>(this SyntaxNode source, Func<T, bool>? isCandidate = null, Func<SyntaxNode, bool>? continueDescendant = null)
        {
            if (source == null) yield break;

            var stack = new Stack<SyntaxNode>();

            // Push initial children in reverse order to evaluate left-to-right
            var initialChildren = source.ChildNodes().Reverse();
            foreach (var child in initialChildren)
            {
                stack.Push(child);
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                // 1. Check if the current node matches the criteria and yield it
                if (current is T t && (isCandidate?.Invoke(t) ?? true))
                {
                    yield return t;
                }

                // 2. Decide whether to dive deeper into this node's children
                if (continueDescendant?.Invoke(current) ?? true)
                {
                    var children = current.ChildNodes().Reverse();
                    foreach (var child in children)
                    {
                        stack.Push(child);
                    }
                }
            }
            //var children = source.ChildNodes();
            //foreach (var c in children)
            //{
            //    if (c is T t && (isCandidate?.Invoke(t) ?? true))
            //        yield return t;
            //    if (continueDescendant?.Invoke(c) ?? true)
            //    {
            //        foreach (var v in FindDescendant<T>(c, isCandidate, continueDescendant))
            //        {
            //            yield return v;
            //        }
            //    }
            //}
        }

        public static void VisitHierachy(this SyntaxNode source, Func<SyntaxNode, int, bool> visitor, int depth = 0)
        {
            if (source == null) return;

            var stack = new Stack<(SyntaxNode Node, int Depth)>();
            stack.Push((source, depth));

            while (stack.Count > 0)
            {
                var (currentNode, currentDepth) = stack.Pop();

                var @continue = visitor(currentNode, currentDepth);
                if (!@continue)
                {
                    continue;
                }

                var children = currentNode.ChildNodes().Reverse();
                foreach (var child in children)
                {
                    stack.Push((child, currentDepth + 1));
                }
            }
            //var @continue = visitor(source, depth);
            //if (@continue)
            //{
            //    var children = source.ChildNodes();
            //    foreach (var c in children)
            //    {
            //        VisitHierachy(c, visitor, depth++);
            //    }
            //}
        }

        public static void VisitParentHierachy(this SyntaxNode source, Func<SyntaxNode, int, bool> visitor, int depth = 0)
        {
            var current = source;
            var currentDepth = depth;

            while (current != null)
            {
                // 1. Run visitor on the current parent node
                var @continue = visitor(current, currentDepth);

                // 2. Break early if visitor returns false, or if there is no parent left
                if (!@continue || current.Parent == null)
                {
                    break;
                }

                // 3. Move up to the next parent level and increment depth
                current = current.Parent;
                currentDepth++;
            }
            //var @continue = visitor(source, depth);
            //if (@continue && source.Parent != null)
            //{
            //    VisitParentHierachy(source.Parent, visitor, depth++);
            //}
        }

        public static SyntaxNode? NextSibling(this SyntaxNode node)
        {
            if (node?.Parent == null) return null;

            // 1. Get the root tree to perform a highly optimized positional search
            var root = node.SyntaxTree.GetRoot();

            // 2. Locate the node starting exactly where the current node ends
            var nextNode = root.FindNode(new Microsoft.CodeAnalysis.Text.TextSpan(node.Span.End, 0));

            // 3. Ensure the found node is a true sibling sharing the exact same parent
            return nextNode?.Parent == node.Parent ? nextNode : null;

            //if (node?.Parent == null) return null;

            //var siblings = node.Parent.ChildNodes();
            //using var enumerator = siblings.GetEnumerator();

            //while (enumerator.MoveNext())
            //{
            //    if (enumerator.Current == node)
            //    {
            //        if (enumerator.MoveNext())
            //        {
            //            return enumerator.Current;
            //        }
            //        break;
            //    }
            //}
            //return null;
        }

        public static bool ChildIsBlock(this SyntaxNode node)
        {
            return node.ChildNodes().Count() == 1 && node.ChildNodes().Single() is BlockSyntax;
        }

        private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
        {
             // --- Original Keywords & Core Logic ---
            "constructor", "arguments",                           // OOP internals & Function implicitly scoped variables
            "function", "this", "super",                          // Function declarations & Context pointers
            "class", "extends", "static",                         // Object-Oriented programming structures

            // --- Variable Declarations & Modules ---
            "var", "let", "const",                                // Variable declarations
            "import", "export", "from", "as",                     // ES6 Module syntax bindings

            // --- Control Flow & Loops ---
            "if", "else", "switch", "case", "default",            // Conditional branching structures
            "for", "while", "do", "in", "of",                     // Iteration & Loop evaluation expressions
            "break", "continue", "goto", "return",                          // Code execution jumps and interruptions

            // --- Error Handling & Debugging ---
            "try", "catch", "finally", "throw",                   // Exception handling patterns
            "debugger",                                           // Native development breakpoints

            // --- Asynchronous & Generator Flow ---
            "async", "await", "yield",                            // Promises, concurrency, and iterator yielding

            // --- Operators & Evaluation ---
            "typeof", "instanceof", "delete", "void",             // Unary, type checking, and mutation operators

            // --- Literals & Primitive Values ---
            "true", "false", "null", "undefined",                 // Fundamental values & Variable default states

            // --- Strict Mode & Future Reserved Words ---
            "package", "private", "protected", "public",          // Visibility modifiers (Strict mode restrictions)
            "interface", "implements", "enum", "with"             // Legacy structures & Future proof compliance
        };

        public static string ResolveIdentifierName(this SyntaxToken token)
        {
            string text = token.Text;

            if (string.IsNullOrEmpty(text))
                return text;

            // Handle @ prefix case
            if (text[0] == '@')
            {
                // Slice out the text after '@'
                string subText = text.Substring(1);

                // If it was a keyword (like @if), return $if, else return clean text
                return Keywords.Contains(subText) ? "$" + subText : subText;
            }

            // Handle plain keyword case
            if (Keywords.Contains(text))
            {
                return "$" + text;
            }

            return text;
        }

        //public static string ResolveIdentifierName(this SyntaxToken token)
        //{
        //    if (token.Text == "constructor" || token.Text == "@constructor")
        //        return "$constructor";
        //    if (token.Text == "function" || token.Text == "@function")
        //        return "$function";
        //    if (token.Text == "arguments" || token.Text == "@arguments")
        //        return "$arguments";
        //    if (token.Text == "break" || token.Text == "@break")
        //        return "$break";
        //    if (token.Text == "continue" || token.Text == "@continue")
        //        return "$continue";
        //    if (token.Text == "extends" || token.Text == "@extends")
        //        return "$extends";
        //    if (token.Text == "switch" || token.Text == "@switch")
        //        return "$switch";
        //    if (token.Text == "case" || token.Text == "@case")
        //        return "$case";
        //    if (token.Text == "try" || token.Text == "@try")
        //        return "$try";
        //    if (token.Text == "catch" || token.Text == "@catch")
        //        return "$catch";
        //    if (token.Text == "finally" || token.Text == "@finally")
        //        return "$finally";
        //    if (token.Text == "if" || token.Text == "@if")
        //        return "$if";
        //    if (token.Text == "do" || token.Text == "@do")
        //        return "$do";
        //    if (token.Text == "while" || token.Text == "@while")
        //        return "$while";
        //    if (token.Text == "goto" || token.Text == "@goto")
        //        return "$goto";
        //    if (token.Text == "this" || token.Text == "@this")
        //        return "$this";
        //    if (token.Text == "class" || token.Text == "@class")
        //        return "$class";
        //    if (token.Text == "var" || token.Text == "@var")
        //        return "$var";
        //    if (token.Text == "else" || token.Text == "@else")
        //        return "$else";
        //    if (token.Text == "default" || token.Text == "@default")
        //        return "$default";
        //    if (token.Text == "return" || token.Text == "@return")
        //        return "$return";
        //    if (token.Text == "new" || token.Text == "@new")
        //        return "$new";
        //    if (token.Text == "import" || token.Text == "@import")
        //        return "$import";
        //    if (token.Text == "super" || token.Text == "@super")
        //        return "$super";
        //    if (token.Text == "debugger" || token.Text == "@debugger")
        //        return "$debugger";
        //    if (token.Text.StartsWith("@"))
        //        return token.Text.Substring(1);
        //    return token.Text;
        //}

        public static string ResolveTypeName(SyntaxToken type)
        {
            var t = type.ToString().Trim().TrimEnd('?').Replace("@", "$");
            if (t.EndsWith("[]"))
            {
                return $"dotnetJs.TypeArray({t.Substring(0, t.Length - 2)})";
            }
            return t;
        }

        public static string SimplifyName(this TypeSyntax type, out GenericNameSyntax? genericName)
        {
            string simpleName;
            if (type is QualifiedNameSyntax qn1)
            {
                GenericNameSyntax? left = null;
                GenericNameSyntax? right = null;
                simpleName = $"{SimplifyName(qn1.Left, out left)}{qn1.DotToken}{SimplifyName(qn1.Right, out right)}";
                genericName = left ?? right;
            }
            else if (type is GenericNameSyntax g1)
            {
                simpleName = $"{g1.Identifier.ValueText}<{string.Join(",", Enumerable.Range(1, g1.Arity).Select(e => ""))}>";
                genericName = g1;
            }
            else
            {
                genericName = null;
                simpleName = type.ToString();
            }
            return simpleName;
        }

        public static (List<string> Types, List<string>? Names)? ResolveTupleTypes(this string name)
        {
            if (name.StartsWith("(") && name.EndsWith(")"))
            {
                var chars = name.ToArray();
                //int cLen = 0;
                //var newChars = new char[chars.Length];
                int genericDepth = 0;
                int tupleDepth = 0;
                string currentTupleTypeName = "";
                string currentTupleName = "";
                bool isCollectingName = false;
                var tupleTypesList = new List<string>();
                List<string>? tupleNameList = null; ;
                int collectedTypeIndex = -1;
                void Collect(int i)
                {
                    if (isCollectingName)
                        currentTupleName += chars[i];
                    else
                        currentTupleTypeName += chars[i];
                }
                void CollectType()
                {
                    Debug.Assert(currentTupleTypeName.Length > 0);
                    collectedTypeIndex = tupleTypesList.Count;
                    tupleTypesList.Add(currentTupleTypeName.Trim());
                    currentTupleTypeName = "";
                }
                void CollectName()
                {
                    Debug.Assert(collectedTypeIndex >= 0);
                    while (tupleTypesList.Count < collectedTypeIndex)
                    {
                        tupleTypesList.Add("");
                    }
                    tupleNameList ??= new List<string>();
                    tupleNameList.Add(currentTupleName.Trim());
                    currentTupleName = "";
                    isCollectingName = false;
                    collectedTypeIndex = -1;
                }
                for (int i = 0; i < chars.Length; i++)
                {
                    if (chars[i] == '(')
                    {
                        if (tupleDepth > 0)
                            Collect(i);
                        tupleDepth++;
                    }
                    else if (chars[i] == ')')
                    {
                        tupleDepth--;
                        if (tupleDepth > 0)
                            Collect(i);
                        if (tupleDepth == 0)
                        {
                            if (isCollectingName)
                                CollectName();
                            else
                                CollectType();
                        }
                    }
                    else
                    {
                        if (tupleDepth == 1 && chars[i] == ' ' && currentTupleTypeName.Length > 0 && genericDepth == 0)
                        {
                            CollectType();
                            isCollectingName = true;
                        }
                        else if (tupleDepth == 1 && chars[i] == ',' && genericDepth == 0)
                        {
                            if (isCollectingName)
                                CollectName();
                            else
                                CollectType();
                        }
                        else
                        {
                            Collect(i);
                        }
                        if (chars[i] == '<')
                        {
                            genericDepth++;
                        }
                        else if (chars[i] == '>')
                        {
                            genericDepth--;
                        }
                    }
                }
                return (tupleTypesList, tupleNameList);
            }
            return null;
        }


        public static string RemoveGenericParameterNames(this string name, out string[]? genericTypes)
        {
            if (string.IsNullOrEmpty(name))
            {
                genericTypes = null;
                return name;
            }

            // Fast-path: If there are no generic symbols, immediately return without allocating anything
            int firstBracket = name.IndexOf('<');
            if (firstBracket < 0)
            {
                genericTypes = null;
                return name;
            }

            ReadOnlySpan<char> source = name.AsSpan();

            var newNameBuilder = new StringBuilder(name.Length);

            // Copy the pre-bracket substring without allocating a new string object
            newNameBuilder.Append(name, 0, firstBracket);

            List<string>? genericTypesList = null;
            int currentGenericStart = -1;
            int genericDepth = 0;
            int tupleDepth = 0;

            for (int i = firstBracket; i < source.Length; i++)
            {
                char c = source[i];

                if (c == '<')
                {
                    if (genericDepth == 0)
                    {
                        newNameBuilder.Append(c);
                        currentGenericStart = i + 1;
                    }
                    genericDepth++;
                }
                else if (c == '>')
                {
                    genericDepth--;
                    if (genericDepth == 0)
                    {
                        newNameBuilder.Append(c);
                        if (currentGenericStart >= 0 && i > currentGenericStart)
                        {
                            genericTypesList ??= new List<string>(4);

                            // NetStandard 2.0 compatible slicing to string conversion
                            var argument = source.Slice(currentGenericStart, i - currentGenericStart).ToString().Trim();
                            genericTypesList.Add(argument);
                        }
                        currentGenericStart = -1;
                    }
                }
                else
                {
                    if (genericDepth == 0)
                    {
                        newNameBuilder.Append(c);
                    }
                    else if (genericDepth == 1)
                    {
                        if (tupleDepth == 0 && c == ',')
                        {
                            newNameBuilder.Append(c);
                            if (currentGenericStart >= 0 && i > currentGenericStart)
                            {
                                genericTypesList ??= new List<string>(4);
                                var argument = source.Slice(currentGenericStart, i - currentGenericStart).ToString().Trim();
                                genericTypesList.Add(argument);
                            }
                            currentGenericStart = i + 1;
                        }
                    }

                    if (c == '(')
                    {
                        tupleDepth++;
                    }
                    else if (c == ')')
                    {
                        tupleDepth--;
                    }
                }
            }

            genericTypes = genericTypesList?.ToArray();
            return newNameBuilder.ToString();
        }


        //public static string RemoveGenericParameterNames(this string name, out string[]? genericTypes)
        //{
        //    genericTypes = null;
        //    var chars = name.ToArray();
        //    int cLen = 0;
        //    var newChars = new char[chars.Length];
        //    int genericDepth = 0;
        //    int tupleDepth = 0;
        //    string currentGenericName = "";
        //    var genericTypesList = new List<string>();
        //    for (int i = 0; i < chars.Length; i++)
        //    {
        //        if (chars[i] == '<')
        //        {
        //            if (genericDepth == 0)
        //                newChars[cLen++] = chars[i];
        //            else
        //                currentGenericName += chars[i];
        //            genericDepth++;
        //        }
        //        else if (chars[i] == '>')
        //        {
        //            genericDepth--;
        //            if (genericDepth == 0)
        //                newChars[cLen++] = chars[i];
        //            else
        //                currentGenericName += chars[i];
        //            if (genericDepth == 0)
        //            {
        //                genericTypesList.Add(currentGenericName.Trim());
        //                currentGenericName = "";
        //            }
        //        }
        //        else
        //        {
        //            if (genericDepth == 0)
        //            {
        //                newChars[cLen++] = chars[i];
        //            }
        //            else if (genericDepth == 1)
        //            {
        //                if (tupleDepth == 0 && chars[i] == ',')
        //                {
        //                    newChars[cLen++] = chars[i];
        //                    genericTypesList.Add(currentGenericName.Trim());
        //                    currentGenericName = "";
        //                }
        //                else
        //                {
        //                    currentGenericName += chars[i];
        //                }
        //            }
        //            else
        //            {
        //                currentGenericName += chars[i];
        //            }
        //            if (chars[i] == '(')
        //            {
        //                tupleDepth++;
        //            }
        //            else if (chars[i] == ')')
        //            {
        //                tupleDepth--;
        //            }
        //        }
        //    }
        //    if (genericTypesList.Count > 0)
        //    {
        //        genericTypes = genericTypesList.ToArray();
        //    }
        //    return new string(newChars, 0, cLen);
        //}

        static StringBuilder _discardTypeParameter = new StringBuilder();
        private static readonly Regex FileScopeRegex = new Regex(@"^<(?<Assembly>[^>]+)>F(?<FileHash>[0-9A-F]+)__(?<TypeName>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static void __CreateSignatures((StringBuilder WithTypeParameter, StringBuilder WithoutTypeParameter) builder, ISymbol current, GlobalCompilationVisitor global, bool withGlobalNamespace = true)
        {
            //if (current is ITypeParameterSymbol)
            if (current.Kind == SymbolKind.TypeParameter/* is ITypeParameterSymbol*/)
            {
                builder.WithTypeParameter.Append(current.Name);
                //builder.WithoutTypeParameter.Append(current.Name);
                return;
            }
            bool isArray = false;
            bool isPointer = false;
            //if (current is IArrayTypeSymbol tt)
            if (current.Kind == SymbolKind.ArrayType/* is IArrayTypeSymbol tt*/)
            {
                isArray = true;
                var tt = Unsafe.As<IArrayTypeSymbol>(current);
                current = tt.ElementType;
            }
            if (current.Kind == SymbolKind.PointerType)
            {
                isPointer = true;
                current = ((IPointerTypeSymbol)current).PointedAtType;
            }
            if (global.FullTypeNameCache.TryGetValue(current, out var nms))
            {
                if (withGlobalNamespace && current.ContainingAssembly != null)
                {
                    var slug = global.GetAssemblyGlobalSlug(current.ContainingAssembly);
                    builder.WithTypeParameter.Append(slug);
                    builder.WithoutTypeParameter.Append(slug);
                    builder.WithTypeParameter.Append(".");
                    builder.WithoutTypeParameter.Append(".");
                }
                builder.WithTypeParameter.Append(nms.WithTypeParameter);
                builder.WithoutTypeParameter.Append(nms.WithoutTypeParameter);
            }
            else
            {
                void TryWriteFileScopedNameHash(ISymbol lcurrent)
                {
                    if (lcurrent.Kind == SymbolKind.NamedType && ((INamedTypeSymbol)lcurrent).IsFileLocal)
                    {
                        var match = FileScopeRegex.Match(((INamedTypeSymbol)lcurrent).MetadataName);
                        if (match.Success)
                        {
                            builder.WithTypeParameter.Append("_");
                            builder.WithoutTypeParameter.Append("_");
                            builder.WithTypeParameter.Append(match.Groups["FileHash"].Value);
                            builder.WithoutTypeParameter.Append(match.Groups["FileHash"].Value);
                        }
                        else
                        {
                            builder.WithTypeParameter.Append(Constants.FileScopedTypeNameMangling);
                            builder.WithoutTypeParameter.Append(Constants.FileScopedTypeNameMangling);
                        }
                    }
                }
                bool isExtensionType = current.Kind == SymbolKind.NamedType && ((INamedTypeSymbol)current).IsExtension;
                bool TryWriteExtensionType(ISymbol lcurrent)
                {
                    if (lcurrent.Kind == SymbolKind.NamedType)
                    {
                        var ts = (INamedTypeSymbol)lcurrent;
                        if (ts.IsExtension)
                        {
                            builder.WithTypeParameter.Append("extension");
                            builder.WithoutTypeParameter.Append("extension");
                            if (ts.Arity > 0)
                            {
                                builder.WithTypeParameter.Append("<");
                                builder.WithoutTypeParameter.Append("<");
                                var tps = ts.TypeParameters;
                                unchecked
                                {
                                    for (int i = 0; i < tps.Length; i++)
                                    {
                                        if (i > 0)
                                        {
                                            builder.WithTypeParameter.Append(",");
                                            builder.WithoutTypeParameter.Append(",");
                                        }
                                        _discardTypeParameter.Clear();
                                        __CreateSignatures(builder with { WithoutTypeParameter = _discardTypeParameter }, tps[i], global);
                                        //Different extension types with same signature can have discriminating constraint
                                        //Use the constraint as part of the signature
                                        if (tps[i].ConstraintTypes.Length > 0)
                                        {
                                            builder.WithTypeParameter.Append(":");
                                            int ix = 0;
                                            foreach (var t in tps[i].ConstraintTypes)
                                            {
                                                if (ix > 0)
                                                {
                                                    builder.WithTypeParameter.Append(",");
                                                }
                                                _discardTypeParameter.Clear();
                                                __CreateSignatures(builder with { WithoutTypeParameter = _discardTypeParameter }, t, global);
                                                ix++;
                                            }
                                        }
                                    }
                                }
                                builder.WithTypeParameter.Append(">");
                                builder.WithoutTypeParameter.Append(">");
                            }
                            builder.WithTypeParameter.Append("(");
                            builder.WithoutTypeParameter.Append("(");
                            __CreateSignatures(builder, ts.ExtensionParameter!.Type, global);
                            builder.WithTypeParameter.Append(")");
                            builder.WithoutTypeParameter.Append(")");
                            return true;
                        }
                    }
                    return false;
                }
                void TryWriteExplicitMethodReturn(ISymbol lcurrent)
                {
                    if (lcurrent.Kind == SymbolKind.Method && (lcurrent.Name == "op_Implicit" || lcurrent.Name == "op_Explicit" || lcurrent.Name == "op_CheckedExplicit"))
                    {
                        __CreateSignatures(builder, ((IMethodSymbol)lcurrent).ReturnType, global);
                        builder.WithTypeParameter.Append(" ");
                        builder.WithoutTypeParameter.Append(" ");
                    }
                }
                TryWriteExplicitMethodReturn(current);
                var nName = current.Name.Replace(".", "$");
                var parent = current.ContainingType ?? (ISymbol)current.ContainingNamespace;
                if (parent != null /*&& parent.Name.Length > 0 && !ReferenceEquals(parent, global.Compilation.GlobalNamespace)*/)
                {
                    //__CreateFullTypeName(builder, parent, global);
                    //    if (!ReferenceEquals(parent, global.Compilation.GlobalNamespace))
                    //    {
                    //        builder.WithTypeParameter.Append(".");
                    //        builder.WithoutTypeParameter.Append(".");
                    //    }
                    var assembly = current.ContainingAssembly;
                    ISymbol[] pathToRoot = ArrayPool<ISymbol>.Shared.Rent(64);
                    int i = 0;
                    while (parent != null)
                    {
                        if (parent.Kind == SymbolKind.Assembly)
                            break;
                        if (ReferenceEquals(parent, global.Compilation.GlobalNamespace))
                        {
                            break;
                        }
                        if (ReferenceEquals(parent, assembly))
                        {
                            break;
                        }
                        if (ReferenceEquals(parent, assembly?.GlobalNamespace))
                        {
                            break;
                        }
                        pathToRoot[i++] = parent;
                        parent = parent.ContainingSymbol;
                    }
                    if (withGlobalNamespace && assembly != null)
                        pathToRoot[i++] = assembly;
                    int done = 0;
                    while (i > 0)
                    {
                        var lcurrent = pathToRoot[i - 1];
                        var lName = lcurrent.Kind == SymbolKind.Assembly ? global.GetAssemblyGlobalSlug(Unsafe.As<IAssemblySymbol>(lcurrent)) : lcurrent.Name.Replace(".", "$");
                        builder.WithTypeParameter.Append(lName);
                        builder.WithoutTypeParameter.Append(lName);
                        TryWriteFileScopedNameHash(lcurrent);
                        if (TryWriteExtensionType(lcurrent))
                        {

                        }
                        else if (lcurrent.Kind == SymbolKind.NamedType && lcurrent is INamedTypeSymbol nt && nt.Arity > 0)
                        {
                            builder.WithTypeParameter.Append("<");
                            builder.WithoutTypeParameter.Append("<");
                            int ix = 0;
                            foreach (var t in nt.TypeArguments)
                            {
                                if (ix > 0)
                                {
                                    builder.WithTypeParameter.Append(",");
                                    builder.WithoutTypeParameter.Append(",");
                                }
                                __CreateSignatures(builder, t, global, withGlobalNamespace: withGlobalNamespace);
                                ix++;
                            }
                            builder.WithTypeParameter.Append(">");
                            builder.WithoutTypeParameter.Append(">");
                        }
                        i--;
                        done++;
                        if (i > 0 ||
                            !string.IsNullOrEmpty(nName) ||
                            isExtensionType)
                        {
                            builder.WithTypeParameter.Append(".");
                            builder.WithoutTypeParameter.Append(".");
                        }
                    }
                    ArrayPool<ISymbol>.Shared.Return(pathToRoot);
                }
                //if (ReferenceEquals(current, global.Compilation.GlobalNamespace))
                //{
                //    nName = "$";
                //}
                builder.WithTypeParameter.Append(nName);
                builder.WithoutTypeParameter.Append(nName);
                if (isExtensionType)
                    TryWriteExtensionType(current);
                //}
                //if (current is INamedTypeSymbol ts && ts.Arity > 0)
                if (current.Kind == SymbolKind.NamedType/* is INamedTypeSymbol ts && ts.Arity > 0*/)
                {
                    TryWriteFileScopedNameHash(current);
                    var ts = Unsafe.As<INamedTypeSymbol>(current);
                    if (ts.Arity > 0 && !isExtensionType)
                    {
                        builder.WithTypeParameter.Append("<");
                        builder.WithoutTypeParameter.Append("<");
                        var tps = ts.TypeArguments;
                        unchecked
                        {
                            for (int i = 0; i < tps.Length; i++)
                            {
                                if (i > 0)
                                {
                                    builder.WithTypeParameter.Append(",");
                                    builder.WithoutTypeParameter.Append(",");
                                }
                                _discardTypeParameter.Clear();
                                __CreateSignatures(builder with { WithoutTypeParameter = _discardTypeParameter }, tps[i], global);
                            }
                        }
                        builder.WithTypeParameter.Append(">");
                        builder.WithoutTypeParameter.Append(">");
                    }
                }
                else if (current.Kind == SymbolKind.Method/* is IMethodSymbol ms*/)
                {
                    var ms = Unsafe.As<IMethodSymbol>(current);
                    if (ms.Arity > 0)
                    {
                        builder.WithTypeParameter.Append("<");
                        builder.WithoutTypeParameter.Append("<");
                        var tps = ms.TypeParameters;
                        unchecked
                        {
                            for (int i = 0; i < tps.Length; i++)
                            {
                                if (i > 0)
                                {
                                    builder.WithTypeParameter.Append(",");
                                    builder.WithoutTypeParameter.Append(",");
                                }
                                _discardTypeParameter.Clear();
                                __CreateSignatures(builder with { WithoutTypeParameter = _discardTypeParameter }, tps[i], global);
                            }
                        }
                        builder.WithTypeParameter.Append(">");
                        builder.WithoutTypeParameter.Append(">");
                    }
                    builder.WithTypeParameter.Append("(");
                    builder.WithoutTypeParameter.Append("(");
                    var msp = ms.Parameters;
                    unchecked
                    {
                        for (int ix = 0; ix < msp.Length; ix++)
                        {
                            if (ix > 0)
                            {
                                builder.WithTypeParameter.Append(",");
                                builder.WithoutTypeParameter.Append(",");
                            }
                            var p = msp[ix];
                            if (p.RefKind != RefKind.None)
                            {
                                builder.WithTypeParameter.Append(p.RefKind.ToString().ToLower());
                                builder.WithoutTypeParameter.Append(p.RefKind.ToString().ToLower());
                                builder.WithTypeParameter.Append(" ");
                                builder.WithoutTypeParameter.Append(" ");
                            }
                            __CreateSignatures(builder, p.Type, global);
                        }
                    }
                    builder.WithTypeParameter.Append(")");
                    builder.WithoutTypeParameter.Append(")");
                }
                else if (current.Kind == SymbolKind.Property/* is IPropertySymbol property && property.IsIndexer*/)
                {
                    var property = Unsafe.As<IPropertySymbol>(current);
                    if (property.IsIndexer)
                    {
                        builder.WithTypeParameter.Append("(");
                        builder.WithoutTypeParameter.Append("(");
                        var msp = property.Parameters;
                        unchecked
                        {
                            for (int ix = 0; ix < msp.Length; ix++)
                            {
                                if (ix > 0)
                                {
                                    builder.WithTypeParameter.Append(",");
                                    builder.WithoutTypeParameter.Append(",");
                                }
                                var p = msp[ix];
                                __CreateSignatures(builder, p.Type, global);
                            }
                        }
                        builder.WithTypeParameter.Append(")");
                        builder.WithoutTypeParameter.Append(")");
                    }
                }
            }
            if (isPointer)
            {
                builder.WithTypeParameter.Append("*");
                builder.WithoutTypeParameter.Append("*");
            }
            if (isArray)
            {
                builder.WithTypeParameter.Append("[]");
                builder.WithoutTypeParameter.Append("[]");
            }
        }

        //static Dictionary<ISymbol, (string WithTypeParameter, string WithoutTypeParameter)> cacheFullName = new Dictionary<ISymbol, (string, string)>(SymbolEqualityComparer.Default);
        //public static (string WithTypeParameter, string WithoutTypeParameter) CreateSignatures(this ISymbol type, GlobalCompilationVisitor global)
        //{
        //    if (type.Kind == SymbolKind.NamedType)
        //    {
        //        var tt = Unsafe.As<INamedTypeSymbol>(type);
        //        if (/*type is INamedTypeSymbol tt &&*/ tt.IsNullable(out var nt))
        //        {
        //            if (!nt!.IsValueType)
        //            {
        //                type = nt;
        //            }
        //        }
        //    }
        //    StringBuilder withTypeParameterBuilder = new StringBuilder(1024);
        //    StringBuilder withoutTypeParameterBuilder = new StringBuilder(1024);
        //    __CreateSignatures((withTypeParameterBuilder, withoutTypeParameterBuilder), type, global);
        //    (string WithTypeParameter, string WithoutTypeParameter) values = (withTypeParameterBuilder.ToString(), withoutTypeParameterBuilder.ToString());
        //    return values;
        //}

        public static string CreateSignature(this ISymbol symbol, GlobalCompilationVisitor global, bool withTypeParameterNames = false, bool withGlobalNamespace = true, bool withAssemblySlugNamespace = false)
        {
            //if (global.HasAttribute(symbol, typeof(SignatureAttribute).FullName, null, false, out var pa))
            //{
            //    return (string)pa[0]!;
            //}
            string? prefix = null;
            if (withGlobalNamespace && symbol.ContainingAssembly != null)
            {
                prefix = global.GlobalName + "." + global.GetAssemblyGlobalSlug(symbol.ContainingAssembly) + ".";
            }
            else if (withAssemblySlugNamespace && symbol.ContainingAssembly != null)
            {
                prefix = global.GetAssemblyGlobalSlug(symbol.ContainingAssembly) + ".";
            }
            if (global.FullTypeNameCache.TryGetValue(symbol, out var s))
            {
                if (withTypeParameterNames)
                    return prefix + s.WithTypeParameter;
                return prefix + s.WithoutTypeParameter;
            }
            if (symbol.Kind == SymbolKind.NamedType && symbol is INamedTypeSymbol tt && tt.IsNullable(out var nt))
            {
                if (!nt!.IsValueType)
                {
                    symbol = nt;
                }
            }
            StringBuilder withTypeParameterBuilder = new StringBuilder(256);
            StringBuilder withoutTypeParameterBuilder = new StringBuilder(256);
            __CreateSignatures((withTypeParameterBuilder, withoutTypeParameterBuilder), symbol, global, /*withGlobalNamespace*/false);
            (string WithTypeParameter, string WithoutTypeParameter) values = (withTypeParameterBuilder.ToString(), withoutTypeParameterBuilder.ToString());
            global.FullTypeNameCache[symbol] = values;
            if (withTypeParameterNames)
                return prefix + values.WithTypeParameter;
            return prefix + values.WithoutTypeParameter;
        }

        //public static string _CreateFullTypeName(this ISymbol type, GlobalCompilationVisitor global, bool withTypeParameterNames = false)
        //{
        //    StringBuilder? previousName = null;
        //    ISymbol current = type;
        //    while (!string.IsNullOrEmpty(current?.Name))
        //    {
        //        StringBuilder currentName = new StringBuilder(1024);
        //        //if (current is IPropertySymbol pt && pt.ExplicitInterfaceImplementations.Any())
        //        //{
        //        //    name = pt.ExplicitInterfaceImplementations.First().Name + "$" + pt.Name;
        //        //}
        //        //else if (current is IMethodSymbol mms && mms.ExplicitInterfaceImplementations.Any())
        //        //{
        //        //    name = mms.ExplicitInterfaceImplementations.First().Name + "$" + mms.Name;
        //        //}
        //        //else if (current is IFieldSymbol fs && fs.ExplicitInterfaceImplementations.Any())
        //        //{
        //        //    name = fs.ExplicitInterfaceImplementations.First().Name + "$" + fs.Name;
        //        //}
        //        //else
        //        //{
        //        currentName.Append(current.Name.Replace(".", "$"));
        //        //}
        //        if (current is INamedTypeSymbol ts && ts.Arity > 0)
        //        {
        //            //if (!checkIgnoreAttribute || !global.HasAttribute(type, typeof(IgnoreGenericAttribute).FullName, null, false, out _))
        //            //{
        //            if (withTypeParameterNames)
        //            {
        //                currentName.Append("<");
        //                var tps = ts.TypeParameters;
        //                unchecked
        //                {
        //                    for (int i = 0; i < tps.Length; i++)
        //                    {
        //                        if (i > 0)
        //                            currentName.Append(",");
        //                        currentName.Append(CreateFullTypeName(tps[i], global, true/*, checkIgnoreAttribute*/));
        //                    }
        //                }
        //                currentName.Append(">");
        //                //name.Append($"<{string.Join(",", ts.TypeParameters.Select(e => CreateFullTypeName(e, global, true/*, checkIgnoreAttribute*/)))}>");
        //            }
        //            else
        //            {
        //                currentName.Append("<");
        //                var tps = ts.TypeParameters;
        //                for (int i = 0; i < tps.Length; i++)
        //                {
        //                    if (i > 0)
        //                        currentName.Append(",");
        //                }
        //                currentName.Append(">");
        //                //name.Append($"<{string.Join(",", Enumerable.Range(1, ts.Arity).Select(e => ""))}>");
        //            }
        //            //name += "$$" + ts.Arity;
        //            //}
        //        }
        //        else if (current is IMethodSymbol ms)
        //        {
        //            if (ms.Arity > 0)
        //            {
        //                //if (!checkIgnoreAttribute || !global.HasAttribute(type, typeof(IgnoreGenericAttribute).FullName, null, false, out _))
        //                //{
        //                if (withTypeParameterNames)
        //                {
        //                    currentName.Append("<");
        //                    var tps = ms.TypeParameters;
        //                    unchecked
        //                    {
        //                        for (int i = 0; i < tps.Length; i++)
        //                        {
        //                            if (i > 0)
        //                                currentName.Append(",");
        //                            currentName.Append(CreateFullTypeName(tps[i], global, true/*, checkIgnoreAttribute*/));
        //                        }
        //                    }
        //                    currentName.Append(">");
        //                    //name.Append($"<{string.Join(",", ms.TypeParameters.Select(e => CreateFullTypeName(e, global, true)))}>");
        //                }
        //                else
        //                {
        //                    currentName.Append("<");
        //                    var tps = ms.TypeParameters;
        //                    for (int i = 0; i < tps.Length; i++)
        //                    {
        //                        if (i > 0)
        //                            currentName.Append(",");
        //                    }
        //                    currentName.Append(">");
        //                    //name.Append($"<{string.Join(",", Enumerable.Range(1, ms.Arity).Select(e => ""))}>");
        //                }
        //                //}
        //            }
        //            currentName.Append("(");
        //            var msp = ms.Parameters;
        //            unchecked
        //            {
        //                for (int ix = 0; ix < msp.Length; ix++)
        //                {
        //                    if (ix > 0)
        //                        currentName.Append(", ");
        //                    var p = msp[ix];
        //                    currentName.Append(p.Type.CreateFullTypeName(global));
        //                }
        //            }
        //            currentName.Append(")");
        //        }
        //        //var newBuilder = new StringBuilder();
        //        //newBuilder.Append(name);
        //        if (previousName != null)
        //        {
        //            currentName.Append(".");
        //            currentName.Append(previousName);
        //        }
        //        previousName = currentName;
        //        //ret = name + (!string.IsNullOrEmpty(ret) ? "." + ret : "");
        //        if (type is ITypeParameterSymbol) //type parameters a denoted by placeholders, expected to be replaced when used
        //            return previousName.ToString();
        //        current = current.ContainingType ?? (ISymbol)current.ContainingNamespace;
        //    }
        //    return previousName?.ToString().ResolvePredefinedTypeName() ?? "";
        //}

        public static string CreateFullNamespace(this NamespaceDeclarationSyntax type)
        {
            string? parent = null;
            if (type.Parent is NamespaceDeclarationSyntax ns)
            {
                parent = CreateFullNamespace(ns);
            }
            var ret = parent + type.Name.ToString().Trim();
            return ret;
        }

        public static bool IsReadOnlyOperation(this CSharpSyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.IdentifierName))
            {
                if (node.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    var ass = (AssignmentExpressionSyntax)node.Parent;
                    return ass.Right == node;
                }
                else if (!node.Parent.IsKind(SyntaxKind.RefExpression))
                    return true;
            }
            if (node.Parent is AssignmentExpressionSyntax ass2)
            {
                if (ass2.Right == node)
                {
                    return true;
                }
            }
            else if (node.Parent.IsKind(SyntaxKind.EqualsValueClause))
            {
                if (((EqualsValueClauseSyntax)node.Parent).Value == node)
                {
                    return true;
                }
            }
            else if (node.Parent.IsKind(SyntaxKind.IsPatternExpression))
            {
                if (((IsPatternExpressionSyntax)node.Parent).Expression == node)
                {
                    return true;
                }
            }
            //else if (node.Parent.IsKind(SyntaxKind.ElementAccessExpression))
            //{
            //    if (node.Parent.IsKind(SyntaxKind.Argument))
            //    {
            //        return true;
            //    }
            //}
            else if (node.Parent.IsKind(SyntaxKind.ReturnStatement) ||
                node.Parent.IsKind(SyntaxKind.ConditionalAccessExpression) ||
                node.Parent.IsKind(SyntaxKind.PointerMemberAccessExpression) ||
                node.Parent.IsKind(SyntaxKind.PointerIndirectionExpression) ||
                node.Parent.IsKind(SyntaxKind.ParenthesizedExpression) ||
                node.Parent.IsKind(SyntaxKind.CastExpression) ||
                node.Parent.IsKind(SyntaxKind.Argument) ||
                node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression) ||
                node.Parent.IsKind(SyntaxKind.EqualsExpression) ||
                node.Parent.IsKind(SyntaxKind.NotEqualsExpression) ||
                node.Parent.IsKind(SyntaxKind.BitwiseAndExpression) ||
                node.Parent.IsKind(SyntaxKind.BitwiseOrExpression) ||
                node.Parent.IsKind(SyntaxKind.BitwiseNotExpression) ||
                node.Parent.IsKind(SyntaxKind.AddExpression) ||
                node.Parent.IsKind(SyntaxKind.SubtractExpression) ||
                node.Parent.IsKind(SyntaxKind.MultiplyExpression) ||
                node.Parent.IsKind(SyntaxKind.DivideExpression) ||
                node.Parent.IsKind(SyntaxKind.LeftShiftExpression) ||
                node.Parent.IsKind(SyntaxKind.RightShiftExpression) ||
                node.Parent.IsKind(SyntaxKind.ExclusiveOrExpression) ||
                node.Parent.IsKind(SyntaxKind.LessThanExpression) ||
                node.Parent.IsKind(SyntaxKind.GreaterThanExpression) ||
                node.Parent.IsKind(SyntaxKind.LessThanOrEqualExpression) ||
                node.Parent.IsKind(SyntaxKind.GreaterThanOrEqualExpression) ||
                node.Parent.IsKind(SyntaxKind.UnaryMinusExpression) ||
                node.Parent.IsKind(SyntaxKind.SwitchExpressionArm))
            {
                return true;
            }
            return false;
        }

        public static string? CreateFullMemberName(this MemberDeclarationSyntax type)
        {
            string? parent = null;
            if (type.Parent is BaseTypeDeclarationSyntax ts)
            {
                parent = CreateFullMemberName(ts) + ".";
            }
            else if (type.Parent is NamespaceDeclarationSyntax ns)
            {
                parent = CreateFullNamespace(ns) + ".";
            }
            string? name = null;
            switch (type)
            {
                case NamespaceDeclarationSyntax ns:
                    {
                        name = CreateFullNamespace(ns);
                        break;
                    }
                case BaseTypeDeclarationSyntax bt:
                    {
                        name = bt.Identifier.ValueText.TrimEnd('?');
                        if (bt.HasAnyAttribute([typeof(ForcePartialAttribute).FullName], out var atts2))
                        {
                            var att = atts2.Values.Single().Single();
                            var typeOf = (TypeOfExpressionSyntax)att.ArgumentList!.Arguments[0].Expression;
                            var typeName = typeOf.Type.ToString();
                            name = typeName;
                        }
                        break;
                    }
                case MethodDeclarationSyntax mt:
                    {
                        if (mt.ExplicitInterfaceSpecifier != null)
                        {
                            name = mt.ExplicitInterfaceSpecifier.Name + "$" + mt.Identifier.ValueText;
                        }
                        else
                        {
                            name = mt.Identifier.ValueText;
                        }
                        break;
                    }
                case ConstructorDeclarationSyntax ctor:
                    {
                        name = ".ctor";
                        break;
                    }
                case PropertyDeclarationSyntax pt:
                    {
                        if (pt.ExplicitInterfaceSpecifier != null)
                        {
                            name = pt.ExplicitInterfaceSpecifier.Name + "$" + pt.Identifier.ValueText;
                        }
                        else
                        {
                            name = pt.Identifier.ValueText;
                        }
                        break;
                    }
                case EnumMemberDeclarationSyntax:
                    return null;
                case FieldDeclarationSyntax:
                    return null;
                case DelegateDeclarationSyntax:
                    return null;
                case IndexerDeclarationSyntax:
                    return null;
                case EventFieldDeclarationSyntax:
                    return null;
                case OperatorDeclarationSyntax:
                    return null;
                case ConversionOperatorDeclarationSyntax:
                    return null;
                //case FieldDeclarationSyntax fd:
                //    {
                //        name = fd.Declaration.Variables.Identifier.ValueText.Trim().TrimEnd('?');
                //        break;
                //    }
                default:
                    return null;
            }
            if (type is TypeDeclarationSyntax t && t.Arity > 0)
            {
                if (!name.EndsWith(">"))//skip double <>, from ForcedPartial
                    name += $"<{string.Join(",", Enumerable.Range(1, t.Arity).Select(e => ""))}>";
            }
            else if (type is MethodDeclarationSyntax m)
            {
                if (m.Arity > 0)
                {
                    name += $"<{string.Join(",", Enumerable.Range(1, m.Arity).Select(e => ""))}>";
                }
                name += "(";
                int i = 0;
                foreach (var p in m.ParameterList.Parameters)
                {
                    if (i > 0)
                        name += ", ";
                    name += p.Type?.ToString();
                    i++;
                }
                name += ")";
            }
            var ret = parent + name;
            return ret;
        }

        public static string ComputeOutputTypeName(this ITypeSymbol type, GlobalCompilationVisitor global)
        {
            if (global.IsAnonymousType(type))
            {
                return ComputeOutputTypeName(global.SystemObject, global);
            }
            if (type is ITypeParameterSymbol tp)
            {
                return tp.Name;
            }
            if (type.Kind == SymbolKind.FunctionPointerType)
            {
                return ComputeOutputTypeName(global.SystemObject, global);
            }
            if (type.IsArray(out var elementType))
                return $"{global.GlobalName}.{Constants.TypeArray}({ComputeOutputTypeName(elementType, global)})";
            if (type.IsPointer(out var pointedType))
                return $"{global.GlobalName}.{Constants.TypePointer}({ComputeOutputTypeName(pointedType, global)})";
            if (type.IsNullable(out var nt) && nt!.IsValueType)
                return $"{global.GlobalName}.{Constants.NullableType}({ComputeOutputTypeName(nt, global)})";
            var sym = global.GetMetadata(type);
            if (sym != null)
            {
                if (type is INamedTypeSymbol)
                    return sym.InvocationName ?? type.Name;
                return sym.OverloadName ?? type.Name;
            }
            //if (type is INamedTypeSymbol nt && nt.Arity > 0)
            //{
            //    if (!global.HasAttribute(type, typeof(IgnoreGenericAttribute).FullName, null, false, out _))
            //    {
            //        var original = nt.OriginalDefinition;
            //        var originalName = original.CreateFullTypeName(global).Trim().Split('<')[0].ResolvePredefinedTypeName();// ComputeOutputTypeName(original, global);
            //        return $"{originalName}({string.Join(", ", nt.TypeArguments.Select(t => ComputeOutputTypeName(t, global)))})";
            //    }
            //}
            return type.CreateSignature(global).ResolvePredefinedTypeName();
        }

        /// <summary>
        /// Calculates a tiered dependency score for an INamedTypeSymbol.
        /// Guarantees concrete/declared types outrank pure interfaces, even with huge cyclic loops.
        /// </summary>
        public static int GetDependencyScore(this INamedTypeSymbol symbol)
        {
            if (symbol.Name == "StringSearchValuesBase")
            {

            }
            if (symbol.Name == "SearchValues")
            {

            }
            // 1. Calculate Major Tier Priority (High-order bits)
            // Classes, Structs, and Records sit in the highest tier. Interfaces sit below them.
            int tierPriority = 0;
            if (symbol.TypeKind == TypeKind.Class || symbol.TypeKind == TypeKind.Struct)
            {
                if (symbol.IsAbstract)
                    tierPriority = 1 << 23; // A large bitwise offset ensuring 100% isolation from structural counts
                else
                    tierPriority = 1 << 24; // A large bitwise offset ensuring 100% isolation from structural counts
            }
            else if (symbol.TypeKind == TypeKind.Delegate)
            {
                tierPriority = 1 << 25; // delegate ranks more than its abstract base class
            }
            else if (symbol.TypeKind == TypeKind.Enum)
            {
                tierPriority = 1 << 25; // enum ranks more than its abtract base class
            }

            // 2. Calculate Minor Structural Hierarchy Weight (Low-order bits)
            var visited = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            int structuralWeight = CalculateStructuralScore(symbol, visited);

            // Combine both layers into a single deterministic int for OrderBy
            return tierPriority + structuralWeight;
        }

        private static int CalculateStructuralScore(ITypeSymbol symbol, HashSet<ITypeSymbol> visited)
        {
            symbol = symbol.OriginalDefinition;

            // Infinite loop/recursion safety switch
            if (!visited.Add(symbol))
            {
                return 0;
            }

            int baseWeight = 1;

            if (symbol is ITypeParameterSymbol)
            {
                baseWeight = 1;
            }
            else if (symbol is INamedTypeSymbol namedType)
            {
                if (!namedType.IsGenericType || IsBoundSerializableType(namedType))
                {
                    baseWeight = 2;
                }
            }

            int cumulativeScore = baseWeight;

            if (symbol is INamedTypeSymbol currentType)
            {
                // 1. Accumulate structural weight from Generic Type Arguments
                if (currentType.IsGenericType && !currentType.IsUnboundGenericType)
                {
                    foreach (var typeArg in currentType.TypeArguments)
                    {
                        cumulativeScore += CalculateStructuralScore(typeArg, visited);
                    }
                }

                // Normalize back to original definition for structural hierarchy checks
                var definition = currentType.IsGenericType ? currentType.OriginalDefinition : currentType;

                // 2. Accumulate structural weight from Base Class Hierarchy
                if (definition.BaseType != null)
                {
                    cumulativeScore += CalculateStructuralScore(definition.BaseType, visited);
                }

                // 3. Accumulate structural weight from Implemented Interfaces
                foreach (var @interface in definition.Interfaces)
                {
                    cumulativeScore += CalculateStructuralScore(@interface, visited);
                }
            }
            else if (symbol is IArrayTypeSymbol arrayType)
            {
                cumulativeScore += CalculateStructuralScore(arrayType.ElementType, visited);
            }

            // Unwind the track set for other sibling branches
            visited.Remove(symbol);

            return cumulativeScore;
        }

        private static bool IsBoundSerializableType(INamedTypeSymbol namedType)
        {
            return namedType.TypeArguments.Length > 0 && !(namedType.TypeArguments[0] is ITypeParameterSymbol);
        }

        static long RecursiveOutputRank(INamedTypeSymbol symbol, int depth, HashSet<INamedTypeSymbol> found)
        {
            if (!found.Add(symbol))
                return 0;
            if (depth > 50)
                return 0;
            var parentRank = symbol.ContainingType != null ? RecursiveOutputRank(symbol.ContainingType, depth + 1, found) : 0;
            var baseRank = symbol.BaseType != null ? RecursiveOutputRank(symbol.BaseType, depth + 1, found) : 0;
            return
                (symbol.TypeKind == TypeKind.Interface ? 100 : symbol.IsAbstract ? 10000 : 1000000) + //self rank
                                                                                                      //symbol.Arity +
                 symbol.TypeArguments.Where(t => t is INamedTypeSymbol).Sum(t => RecursiveOutputRank((INamedTypeSymbol)t, depth + 1, found)) / 10 +
                 parentRank +
                 baseRank +
                 symbol.Interfaces.Sum(i => RecursiveOutputRank(i, depth + 1, found)); //interfaces rank
        }

        public static long OutputRank(this INamedTypeSymbol symbol, int depth)
        {
            //return GetDependencyScore(symbol);
            var found = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            return RecursiveOutputRank(symbol, depth, found);
        }
        public static IEnumerable<INamedTypeSymbol> SortByDependencies(this IEnumerable<INamedTypeSymbol> types)
        {
            var typeSet = new HashSet<INamedTypeSymbol>(types, SymbolEqualityComparer.Default);
            var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var visiting = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var sortedList = new List<INamedTypeSymbol>();

            foreach (var type in typeSet)
            {
                Visit(type, typeSet, visiting, visited, sortedList);
            }

            return sortedList;
        }

        private static void Visit(
            INamedTypeSymbol type,
            HashSet<INamedTypeSymbol> typeSet,
            HashSet<INamedTypeSymbol> visiting,
            HashSet<INamedTypeSymbol> visited,
            List<INamedTypeSymbol> sortedList)
        {
            // Already fully processed? Skip.
            if (visited.Contains(type)) return;

            // Loop/Infinite recursion detected in the graph!
            // Instead of throwing an error, we gracefully return. This resolves the loop.
            if (visiting.Contains(type)) return;

            // Mark as actively processing on the current stack branch
            visiting.Add(type);

            // Gather all valid type dependencies
            var dependencies = new List<INamedTypeSymbol>();

            if (type.BaseType != null) dependencies.Add(type.BaseType);
            dependencies.AddRange(type.Interfaces);

            // OPTIONAL: Uncomment if you also sort by property/field types
            /*
            foreach (var member in type.GetMembers())
            {
                if (member is IPropertySymbol prop && prop.Type is INamedTypeSymbol propType) dependencies.Add(propType);
                if (member is IFieldSymbol field && field.Type is INamedTypeSymbol fieldType) dependencies.Add(fieldType);
            }
            */

            foreach (var dependency in dependencies)
            {
                // Normalize generics (e.g., Node<T> instead of Node<int>)
                var targetDependency = dependency.IsGenericType ? dependency.OriginalDefinition : dependency;

                if (typeSet.Contains(targetDependency))
                {
                    Visit(targetDependency, typeSet, visiting, visited, sortedList);
                }
            }

            // Backtrack: Remove from active stack, mark as completely processed
            visiting.Remove(type);
            visited.Add(type);

            // Add to the sorted result
            sortedList.Add(type);
        }

        public static bool IsPredefinedTypeName(this string? name)
        {
            switch (name)
            {
                case "void":
                    return true;
                case "object":
                    return true;
                case "bool":
                    return true;
                case "char":
                    return true;
                case "byte":
                    return true;
                case "sbyte":
                    return true;
                case "double":
                    return true;
                case "float":
                    return true;
                case "short":
                    return true;
                case "ushort":
                    return true;
                case "int":
                    return true;
                case "uint":
                    return true;
                case "long":
                    return true;
                case "ulong":
                    return true;
                case "decimal":
                    return true;
                case "string":
                    return true;
                default:
                    break;
            }
            return false;
        }

        public static string ResolvePredefinedTypeName(this string name)
        {
            switch (name)
            {
                case "void":
                    name = "System.Void";
                    break;
                case "object":
                    name = "System.Object";
                    break;
                case "bool":
                    name = "System.Boolean";
                    break;
                case "char":
                    name = "System.Char";
                    break;
                case "byte":
                    name = "System.Byte";
                    break;
                case "sbyte":
                    name = "System.SByte";
                    break;
                case "double":
                    name = "System.Double";
                    break;
                case "float":
                    name = "System.Single";
                    break;
                case "short":
                    name = "System.Int16";
                    break;
                case "ushort":
                    name = "System.UInt16";
                    break;
                case "nint":
                case "int":
                    name = "System.Int32";
                    break;
                case "nuint":
                case "uint":
                    name = "System.UInt32";
                    break;
                case "long":
                    name = "System.Int64";
                    break;
                case "ulong":
                    name = "System.UInt64";
                    break;
                case "decimal":
                    name = "System.Decimal";
                    break;
                case "string":
                    name = "System.String";
                    break;
                default:

                    break;
            }
            return name;
        }

        public static string ResolvePredefinedTypeName(this PredefinedTypeSyntax type)
        {
            return type.Keyword.ValueText.ResolvePredefinedTypeName();
        }

        public static string ResolveTypeName(this TypeSyntax type, GlobalCompilationVisitor _global, bool stripGenericName = false)
        {
            if (type is PredefinedTypeSyntax pt)
            {
                return pt.ResolvePredefinedTypeName();
            }
            if (type is TupleTypeSyntax tuple)
            {

            }
            string t;
            if (type is GenericNameSyntax g && !stripGenericName)
            {
                t = g.Identifier.ValueText + $"({string.Join(", ", g.TypeArgumentList.Arguments.Select(a => a.ResolveTypeName(_global, stripGenericName)))})";
            }
            else
            {
                if (stripGenericName)
                {
                    t = type.ToString().Split('<')[0].Trim().Replace("?", "");
                }
                else
                {
                    t = type.ToString().Replace("<", "(").Replace(">", ")").Trim().Replace("?", "");
                }
            }
            if (t.EndsWith("[]"))
            {
                return $"$.typearray({t.Substring(0, t.Length - 2)})";
            }
            return t;
        }

        public static string ResolveTypeName(this TypeParameterSyntax type)
        {
            return type.Identifier.ToString().Replace("<", "(").Replace(">", ")");
        }

        public static string ResolveMethodName(MethodDeclarationSyntax node)
        {
            var name = node.Identifier.ValueText;
            string? methodOverload = null;
            if (node.Parent is TypeDeclarationSyntax type)
            {
                var overloads = type.Members.Where(m => m is MethodDeclarationSyntax).Cast<MethodDeclarationSyntax>().Where(e => e.Identifier.ValueText == name);
                if (overloads.Count() > 1)
                {
                    var index = Array.IndexOf(overloads.ToArray(), node);
                    if (index > 0)
                        methodOverload = "$$" + index;
                }
            }
            return $"{node.Identifier.Text.Trim()}{methodOverload}";
        }

        public static bool IsType(this ITypeSymbol type, string fullName, bool matchGenerics = false)
        {
            if (type.ContainingNamespace == null)
                return false;
            var name = $"{type.ContainingNamespace}.{type.Name}{(matchGenerics && (type as INamedTypeSymbol)?.Arity > 0 ? $"<{(string.Join(",", Enumerable.Range(1, (type as INamedTypeSymbol)!.Arity).Select(e => "")))}>" : "")}";
            return fullName == name;
        }

        public static bool IsArray(this ITypeSymbol symbol, out ITypeSymbol elementType)
        {
            if (symbol is IArrayTypeSymbol arr)
            {
                elementType = arr.ElementType;
                return true;
            }
            //if (symbol.IsType("System.Array", true))
            //{
            //    elementType = ((INamedTypeSymbol)symbol).TypeArguments[0];
            //    return true;
            //}
            if (symbol.IsType("System.Array<>", true))
            {
                elementType = ((INamedTypeSymbol)symbol).TypeArguments[0];
                return true;
            }
            elementType = null!;
            return false;
        }

        public static bool IsPointer(this ITypeSymbol symbol, out ITypeSymbol pointedType)
        {
            if (symbol.Kind == SymbolKind.PointerType)
            {
                var pointer = (IPointerTypeSymbol)symbol;
                pointedType = pointer.PointedAtType;
                return true;
            }
            pointedType = null!;
            return false;
        }

        //public static bool IsGenericType(this ITypeSymbol type, string fullName, out IEnumerable<ITypeSymbol> genericArgs)
        //{
        //    genericArgs = [];
        //    if (type.ContainingNamespace == null)
        //    {
        //        return false;
        //    }
        //    bool ret = fullName.StartsWith(type.ContainingNamespace.Name) && fullName.EndsWith(type.Name);
        //    if (ret)
        //    {
        //        genericArgs = ((INamedTypeSymbol)type).TypeArguments;
        //    }
        //    return ret;
        //}

        public static ITypeSymbol GetOriginalRootDefinition(this ITypeSymbol type)
        {
#pragma warning disable RS1024 // Symbols should be compared for equality
            while (type.OriginalDefinition != type)
            {
                if (type.OriginalDefinition is INamedTypeSymbol nt && nt.Arity > 0 && nt.TypeArguments.All(a => a.Name == ""))
                {
                    break;
                }
                type = type.OriginalDefinition;
            }
#pragma warning restore RS1024 // Symbols should be compared for equality
            return type;
        }

        //[return: MemberNotNullWhen(true, nameof(argumentTypes))]
        public static bool IsDelegate(this ITypeSymbol type, out ITypeSymbol? returnType, out IEnumerable<ITypeSymbol>? argumentTypes)
        {
            if (type.TypeKind == TypeKind.Delegate)
            {
                returnType = ((INamedTypeSymbol)type).DelegateInvokeMethod!.ReturnType;
                argumentTypes = ((INamedTypeSymbol)type).DelegateInvokeMethod!.Parameters.Select(t => t.Type).ToList();
                return true;
            }
            returnType = null;
            argumentTypes = null;
            return false;
        }

        //[return: MemberNotNullWhen(true, nameof(argumentTypes))]
        public static bool IsFunction(this ITypeSymbol type, out ITypeSymbol? returnType, out IEnumerable<ITypeSymbol>? argumentTypes)
        {
            if (type.IsType("dotnetJs.Function"))
            {
                var targsCount = ((INamedTypeSymbol)type).TypeArguments.Count();
                returnType = ((INamedTypeSymbol)type).TypeArguments.Last();
                argumentTypes = ((INamedTypeSymbol)type).TypeArguments.Take(targsCount - 1).ToList();
                return true;
            }
            returnType = null;
            argumentTypes = null;
            return false;
        }

        public static bool IsUnion(this ITypeSymbol type, out IEnumerable<ITypeSymbol>? argumentTypes)
        {
            if (type.IsType("dotnetJs.Union"))
            {
                argumentTypes = ((INamedTypeSymbol)type).TypeArguments;
                return true;
            }
            argumentTypes = null;
            return false;
        }

        public static bool IsAction(this ITypeSymbol type, out IEnumerable<ITypeSymbol>? argumentTypes)
        {
            if (type.IsType("dotnetJs.Action"))
            {
                argumentTypes = ((INamedTypeSymbol)type).TypeArguments;
                return true;
            }
            argumentTypes = null;
            return false;
        }

        public static bool IsNullable(this ITypeSymbol type, out ITypeSymbol? argumentTypes)
        {
            if (type.IsType("System.Nullable<>", true))
            {
                argumentTypes = ((INamedTypeSymbol)type).TypeArguments[0];
                return true;
            }
            argumentTypes = null;
            return false;
        }

        public static bool IsNullableReferenceType(this ITypeSymbol type, out ITypeSymbol? argumentTypes)
        {
            if (type.IsType("System.Nullable<>", true) && !((INamedTypeSymbol)type).TypeArguments[0].IsValueType)
            {
                argumentTypes = ((INamedTypeSymbol)type).TypeArguments[0];
                return true;
            }
            argumentTypes = null;
            return false;
        }
        public static bool IsNullableValueType(this ITypeSymbol type, out ITypeSymbol? argumentTypes)
        {
            if (type.IsType("System.Nullable<>", true) && ((INamedTypeSymbol)type).TypeArguments[0].IsValueType)
            {
                argumentTypes = ((INamedTypeSymbol)type).TypeArguments[0];
                return true;
            }
            argumentTypes = null;
            return false;
        }

        public static bool IsEnumerable(this ITypeSymbol type, out ITypeSymbol? argumentType)
        {
            if (type.IsType("System.Collections.Generic.IEnumerable<>", true))
            {
                argumentType = ((INamedTypeSymbol)type).TypeArguments.Single();
                return true;
            }
            argumentType = null;
            return false;
        }

        public static bool IsAsyncEnumerable(this ITypeSymbol type, out ITypeSymbol? argumentType)
        {
            if (type.IsType("System.Collections.Generic.IAsyncEnumerable<>", true))
            {
                argumentType = ((INamedTypeSymbol)type).TypeArguments.Single();
                return true;
            }
            argumentType = null;
            return false;
        }

        public static bool IsEnumerator(this ITypeSymbol type, out ITypeSymbol? argumentType)
        {
            if (type.IsType("System.Collections.Generic.IEnumerator<>", true))
            {
                argumentType = ((INamedTypeSymbol)type).TypeArguments.Single();
                return true;
            }
            argumentType = null;
            return false;
        }

        public static bool IsAsyncEnumerator(this ITypeSymbol type, out ITypeSymbol? argumentType)
        {
            if (type.IsType("System.Collections.Generic.IAsyncEnumerator<>", true))
            {
                argumentType = ((INamedTypeSymbol)type).TypeArguments.Single();
                return true;
            }
            argumentType = null;
            return false;
        }

        public static bool IsEnumerable(this ITypeSymbol type)
        {
            if (type.IsType("System.Collections.IEnumerable"))
            {
                return true;
            }
            return false;
        }

        public static bool IsEnumerator(this ITypeSymbol type)
        {
            if (type.IsType("System.Collections.IEnumerator"))
            {
                return true;
            }
            return false;
        }

        public static bool IsRef(this ITypeSymbol type, out ITypeSymbol? argumentType)
        {
            if (type.IsType(Constants.RefClassFullName))
            {
                argumentType = ((INamedTypeSymbol)type).TypeArguments.Single();
                return true;
            }
            argumentType = null;
            return false;
        }

        /// <summary>
        /// Compare two type if they are convertible. returns 0 if they are not
        /// </summary>
        /// <param name="fromType"></param>
        /// <param name="toType"></param>
        /// <param name="global"></param>
        /// <param name="genericTypeSubstitutions"></param>
        /// <returns>How closely match they are. If they are exactly the same type, return a higher number</returns>
        public static int CanConvertTo(
            this ISymbol fromType,
            ISymbol toType,
            GlobalCompilationVisitor global,
            Dictionary<ITypeParameterSymbol, ITypeSymbol>? genericTypeSubstitutions,
            out ITypeSymbol? unionItemSelected,
            ExpressionSyntax? fromExpressionHint = null,
            TranslatorSyntaxVisitor? visitor = null)
        {
            //if (fromType is ITypeSymbol ts1 && ts1.IsNullable(out var nullableType1) && !nullableType1!.IsValueType)
            //{
            //    fromType = nullableType1;
            //}
            //if (toType is ITypeSymbol ts2 && ts2.IsNullable(out var nullableType2) && !nullableType2!.IsValueType)
            //{
            //    toType = nullableType2;
            //}
            const int defaultTrue = 10;
            const int defaultFalse = -30000;
            unionItemSelected = null;
            if (fromType.Equals(toType, SymbolEqualityComparer.Default))
                return defaultTrue * 3;
            ITypeSymbol? typeFromType = fromType as ITypeSymbol;
            ITypeSymbol? typeToType = toType as ITypeSymbol;
            INamedTypeSymbol? namedFromType = fromType as INamedTypeSymbol;
            INamedTypeSymbol? namedToType = toType as INamedTypeSymbol;

            if (fromExpressionHint is LiteralExpressionSyntax && visitor != null)
            {
                if (typeFromType != null && typeToType != null && typeFromType.IsNumericType() && typeToType.IsNumericType())
                {
                    //if we are trying to convert something like int to ulong (ulong a = 1), CanConvertTo will return a falsy as expected
                    //But there is an exception for when the int value is a literal constant whose value fit within the ulong. C# allows this
                    var fromConstantValue = global.EvaluateConstant(fromExpressionHint, visitor);
                    if (fromConstantValue.HasValue && fromConstantValue.Value is int)
                    {
                        object? minValue = null;
                        object? maxValue = null;
                        var f = typeToType.GetMembers("MinValue").FirstOrDefault();
                        if (f is IFieldSymbol fs)
                        {
                            minValue = fs.ConstantValue;
                        }
                        else
                        {
                            //for long and ulong, we couldn't declare the Min and Max as field, but rather property
                            if (typeToType.Name == "Int64")
                            {
                                minValue = long.MinValue;
                            }
                            if (typeToType.Name == "UInt64")
                            {
                                minValue = ulong.MinValue;
                            }
                        }
                        f = typeToType.GetMembers("MaxValue").FirstOrDefault();
                        if (f is IFieldSymbol mfs)
                        {
                            maxValue = mfs.ConstantValue;
                        }
                        else
                        {
                            //for long and ulong, we couldn't declare the Min and Max as field, but rather property
                            if (typeToType.Name == "Int64")
                            {
                                maxValue = long.MaxValue;
                            }
                            if (typeToType.Name == "UInt64")
                            {
                                maxValue = ulong.MaxValue;
                            }
                        }
                        if (minValue != null && maxValue != null)
                        {
                            bool isWithin = false;
                            int value = (int)fromConstantValue.Value!;
                            if (minValue is char minC && maxValue is char maxC)
                            {
                                isWithin = value >= minC && value <= maxC;
                            }
                            else if (minValue is byte minB && maxValue is byte maxB)
                            {
                                isWithin = value >= minB && value <= maxB;
                            }
                            else if (minValue is sbyte minSB && maxValue is sbyte maxSB)
                            {
                                isWithin = value >= minSB && value <= maxSB;
                            }
                            else if (minValue is ushort minSh && maxValue is ushort maxSh)
                            {
                                isWithin = value >= minSh && value <= maxSh;
                            }
                            else if (minValue is int minI && maxValue is int maxI)
                            {
                                isWithin = value >= minI && value <= maxI;
                            }
                            else if (minValue is uint minUI && maxValue is uint maxUI)
                            {
                                isWithin = value >= minUI && value <= maxUI;
                            }
                            else if (minValue is long minL && maxValue is long maxL)
                            {
                                isWithin = value >= minL && value <= maxL;
                            }
                            else if (minValue is ulong minUL && maxValue is ulong maxUL)
                            {
                                isWithin = (ulong)value >= minUL && (ulong)value <= maxUL;
                            }
                            if (isWithin)
                            {
                                return defaultTrue;
                            }
                        }
                    }
                }
            }

            if ((typeFromType?.IsNumericType() ?? false) && (typeToType?.IsNumericType() ?? false))
            {
                var fromRank = typeFromType.GetNumericRangeRank();
                var toRank = typeToType.GetNumericRangeRank();
                if (fromRank <= toRank)
                    return defaultTrue;
            }

            if (typeToType?.IsType("System.Object") ?? false)
            {
                return defaultTrue;
            }
            if (typeToType?.IsNullable(out var nType) ?? false)
            {
                if (nType!.IsType("System.Object"))
                {
                    return defaultTrue;
                }
            }
            if ((typeFromType?.IsNullable(out var gFromType) ?? false) && (typeToType?.IsNullable(out var gToType) ?? false))
            {
                return gFromType!.CanConvertTo(gToType!, global, genericTypeSubstitutions, out unionItemSelected);
            }
            if (namedFromType != null && namedFromType.IsType("dotnetJs.Union"))
            {
                var types = namedFromType.TypeArguments;
                foreach (var type in types)
                {
                    var w = CanConvertTo(type, toType, global, genericTypeSubstitutions, out _);
                    if (w > 0)
                    {
                        unionItemSelected = type;
                        return w;
                    }
                }
                return -30000;
            }
            else if (namedToType != null && namedToType.IsType("dotnetJs.Union"))
            {
                var types = namedToType.TypeArguments;
                return types.Sum(t => CanConvertTo(fromType, t, global, genericTypeSubstitutions, out _));
            }
            else if (namedFromType != null && namedFromType.IsType("dotnetJs.Null"))
            {
                //null can be assigned to any value type
                return !((ITypeSymbol)toType).IsValueType ? defaultTrue : defaultFalse;
            }
            else if (typeFromType != null && typeFromType.IsType("dotnetJs.Default"))
            {
                //default can be assigned to any type
                return defaultTrue;
            }

            if (toType is ITypeParameterSymbol genericParameter)
            {
                if (genericParameter.ConstraintTypes.Count() == 0)
                {
                    if (genericTypeSubstitutions != null)
                    {
                        if (!genericTypeSubstitutions.TryAdd(genericParameter, typeFromType))
                        {

                        }
                    }
                    return defaultTrue * 2;
                }
                var ret = genericParameter.ConstraintTypes.Sum(constraint =>
                {
                    return fromType.CanConvertTo(constraint, global, genericTypeSubstitutions, out _);
                });
                if (ret > 0)
                {
                    if (genericTypeSubstitutions != null)
                    {
                        if (!genericTypeSubstitutions.TryAdd(genericParameter, typeFromType))
                        {

                        }
                    }
                }
                return ret;
            }
            if (typeFromType?.IsArray(out var fromArrayElementType) ?? false)
            {
                if (typeToType?.IsArray(out var toArrayElementType) ?? false)
                    return fromArrayElementType.CanConvertTo(toArrayElementType, global, genericTypeSubstitutions, out _);
                else
                {
                    var baseArray = typeFromType.BaseType;
                    var w = baseArray!.CanConvertTo(toType, global, genericTypeSubstitutions, out unionItemSelected);
                    if (w >= 0)
                        return w / 3; //matching to a base type should have lesser weight
                    if (namedToType?.IsType("System.Collections.Generic.IList<>", true) ?? false)
                        return defaultTrue;
                    if (namedToType?.IsType("System.Collections.Generic.IEnumerable<>", true) ?? false)
                        return defaultTrue;
                    if (namedToType?.IsType("System.Collections.Generic.ICollection<>", true) ?? false)
                        return defaultTrue;
                }
            }
            if ((typeFromType?.IsArray(out var fromArray2ElementType) ?? false) && (namedToType?.IsEnumerable(out var eargs) ?? false))
            {
                return fromArray2ElementType.CanConvertTo(eargs!, global, genericTypeSubstitutions, out _);
            }
            if (typeFromType != null && typeToType != null)
            {
                if (global.Compilation.HasImplicitConversion(typeFromType, typeToType))
                    return defaultTrue;
                if (new ITypeSymbol[] { typeFromType }.Concat(typeFromType.AllInterfaces).Any(i => i.OriginalDefinition.Equals(typeToType, SymbolEqualityComparer.Default)))
                    return defaultTrue;
            }
            if (namedFromType != null && namedToType != null && namedToType.Arity > 0)
            {
                var openNamedToType = namedToType.ConstructUnboundGenericType();
                foreach (var i in new INamedTypeSymbol[] { namedFromType }.Concat(namedFromType.AllInterfaces))
                {
                    if (!i.IsGenericType)
                        continue;
                    var iOpen = i.ConstructUnboundGenericType();
                    if (iOpen.Equals(openNamedToType, SymbolEqualityComparer.Default))
                    {
                        if (genericTypeSubstitutions != null)
                        {
                            int ii = 0;
                            foreach (var g in i.TypeParameters)
                            {
                                genericTypeSubstitutions[g] = i.TypeArguments.ElementAt(ii);
                                ii++;
                            }
                        }
                        return defaultTrue;
                    }
                }
            }
            if ((typeFromType?.IsAction(out var fromArgs) ?? false) && (typeToType?.IsDelegate(out var rType, out var toArgs) ?? false))
            {
                if (rType == null || rType.Name == "Void")
                    if (fromArgs.Count() == toArgs.Count() && fromArgs.Select((f, i) => (f, i)).All(farg => farg.f.CanConvertTo(toArgs.ElementAt(farg.i), global, genericTypeSubstitutions, out _) > 0))
                        return defaultTrue;
            }
            if ((typeFromType?.IsFunction(out var fRType, out var fromArgs2) ?? false) && (typeToType?.IsDelegate(out var dRType, out var toArgs2) ?? false))
            {
                if (fromArgs2.Count() == toArgs2.Count() &&
                    fRType!.CanConvertTo(dRType!, global, genericTypeSubstitutions, out _) > 0 &&
                    fromArgs2.Select((f, i) => (f, i)).All(farg => farg.f.CanConvertTo(toArgs2.ElementAt(farg.i), global, genericTypeSubstitutions, out _) > 0))
                    return defaultTrue;
            }
            if (fromType is IMethodSymbol fromMethod && namedToType != null)
            {
                IEnumerable<ITypeSymbol>? aargs = null;
                IEnumerable<ITypeSymbol>? fargs = null;
                ITypeSymbol? fRetType = null;
                if (namedToType.DelegateInvokeMethod != null || namedToType.IsFunction(out fRetType, out fargs) || namedToType.IsAction(out aargs))
                {
                    if ((namedToType.DelegateInvokeMethod?.Parameters.Count() == fromMethod.Parameters.Count() && fromMethod.ReturnType.CanConvertTo(namedToType.DelegateInvokeMethod.ReturnType, global, genericTypeSubstitutions, out _) > 0) ||
                        (fargs?.Count() == fromMethod.Parameters.Count() && fromMethod.ReturnType.CanConvertTo(fRetType!, global, genericTypeSubstitutions, out _) > 0) ||
                        (aargs?.Count() == fromMethod.Parameters.Count()))
                    {
                        if (fromMethod.Parameters.Select((fromMethodParameter, i) => (parameter: fromMethodParameter, i)).Sum(i =>
                        {
                            var toDelegateParameter = ((IEnumerable<ISymbol>?)namedToType.DelegateInvokeMethod?.Parameters.Select(e => e.Type) ?? fargs ?? aargs)!.ElementAt(i.i);
                            return i.parameter.Type.CanConvertTo(toDelegateParameter, global, genericTypeSubstitutions, out _);
                        }) > 0)
                        {
                            return defaultTrue;
                        }
                    }
                }
            }
            return -300000;
        }

        public static ISymbol SubstituteGenericType(this ISymbol sourceType, Dictionary<ITypeParameterSymbol, ISymbol> genericTypeSubstitutions, GlobalCompilationVisitor global)
        {
            if (genericTypeSubstitutions.Count == 0)
                return sourceType;
            if (!sourceType.OriginalDefinition.Equals(sourceType, SymbolEqualityComparer.Default)) //already substituded
                return sourceType;
            if (sourceType is ITypeParameterSymbol genericParameter)
            {
                return genericTypeSubstitutions.GetValueOrDefault(genericParameter) ?? sourceType;
            }
            if (sourceType is ITypeSymbol tp && tp.IsArray(out var elementType))
            {
                var replaced = elementType.SubstituteGenericType(genericTypeSubstitutions, global);
                return global.Compilation.CreateArrayTypeSymbol((ITypeSymbol)replaced);
            }
            if (sourceType is IMethodSymbol fromMethod && fromMethod.IsGenericMethod)
            {
                var replacements = fromMethod.TypeParameters.Select(t => (ITypeSymbol)t.SubstituteGenericType(genericTypeSubstitutions, global)).ToArray();
                return fromMethod.Construct(replacements);
            }
            if (sourceType is INamedTypeSymbol fromType && fromType.IsGenericType)
            {
                var replacements = fromType.TypeParameters.Select(t => (ITypeSymbol)t.SubstituteGenericType(genericTypeSubstitutions, global)).ToArray();
                return fromType.Construct(replacements);
            }
            return sourceType;
        }

        //static IEnumerable<ISymbol> RecursivelyGetMembers(this INamespaceOrTypeSymbol type, string? name, GlobalCompilationVisitor global, HashSet<string>? found, bool deep)
        //{
        //    bool ShouldReturn(ISymbol symbol)
        //    {
        //        if (symbol.Name == "IsNegative")
        //        {

        //        }
        //        if (found == null) //inner getmember always return
        //            return true;
        //        var signature = global.GetRequiredMetadata(symbol).Signature;
        //        if (found.Contains(signature + "!")) //overriden member already returned
        //            return false;
        //        if (symbol.IsOverride) //mark it such that this symbol name will not be retuurned again
        //        {
        //            found.Add(signature + "!"); // marks a final symbol with this name
        //        }
        //        if (found.Add(signature)) //member with this signature has not been returned yet
        //            return true;
        //        return false;
        //    }
        //    if (type is IArrayTypeSymbol arr)
        //    {
        //        type = (INamespaceOrTypeSymbol)global.AdjustConcreteArrayType(arr);
        //    }
        //    var members = string.IsNullOrEmpty(name) ? type.GetMembers() : type.GetMembers(name!);
        //    foreach (var t in members)
        //    {
        //        if (ShouldReturn(t))
        //            yield return t;
        //    }
        //    //IndexerName attribute may have been applied to the member we are looking for. 
        //    //In which case it isn't get_Item any longer
        //    if (name == "get_Item" || name == "set_Item")
        //    {
        //        var allNamedIndexers = type.GetMembers().Where(m =>
        //        {
        //            if (m is IPropertySymbol ps && ps.IsIndexer/* && global.HasAttribute(ps, typeof(IndexerNameAttribute).FullName, null, false, out var args)*/)
        //            {
        //                //var name = args[0].ToString();
        //                return true;
        //            }
        //            return false;
        //        }).Cast<IPropertySymbol>();
        //        foreach (var t in allNamedIndexers)
        //        {
        //            if (name == "get_Item" && t.GetMethod != null)
        //                if (ShouldReturn(t.GetMethod))
        //                    yield return t.GetMethod;
        //            if (name == "set_Item" && t.SetMethod != null)
        //                if (ShouldReturn(t.SetMethod))
        //                    yield return t.SetMethod;
        //        }
        //    }
        //    if (deep && type is INamedTypeSymbol nt)
        //    {
        //        if (nt.BaseType != null)
        //        {
        //            foreach (var m in RecursivelyGetMembers(nt.BaseType, name, global, null, deep))
        //                if (ShouldReturn(m))
        //                    yield return m;
        //        }
        //        //if the symbol is an interface, then its interfaces members are public within this interface
        //        //If the symbol is a class, its interface are not directly public, unless the class implement them publicly,
        //        //in which case we already found the member on the class itself
        //        if (nt.TypeKind == TypeKind.Interface)
        //        {
        //            foreach (var i in nt.Interfaces)
        //            {
        //                foreach (var m in RecursivelyGetMembers(i, name, global, null, deep))
        //                    if (ShouldReturn(m))
        //                        yield return m;
        //            }
        //        }
        //    }
        //    if (/*deep && */type is ITypeParameterSymbol tp)
        //    {
        //        foreach (var c in tp.ConstraintTypes)
        //        {
        //            var searchCandidates = (ITypeSymbol[])[c, .. c.AllInterfaces];
        //            foreach (var m in RecursivelyGetMembers(c, name, global, null, deep))
        //                if (ShouldReturn(m))
        //                    yield return m;
        //        }
        //    }
        //    if (deep && found != null)
        //    {
        //        //every type inherits from object and has ToString and GetHashCode, even if not explicitly defined
        //        var obj = (ITypeSymbol)global.GetSymbol("System.Object", null);
        //        foreach (var m in string.IsNullOrEmpty(name) ? obj.GetMembers() : obj.GetMembers(name!))
        //            if (ShouldReturn(m))
        //                yield return m;
        //    }
        //}

        static List<ISymbol> RecursivelyGetMembers(this INamespaceOrTypeSymbol initialType, string? name, GlobalCompilationVisitor global, bool deep)
        {
            List<ISymbol> resultList = new();

            if (initialType == null)
                return resultList;

            // Highly optimized, allocation-free evaluation utility
            static bool ShouldReturn(ISymbol symbol, GlobalCompilationVisitor global, HashSet<string> foundSignatures)
            {
                // Extracted target metadata signature calculation exactly once
                var signature = global.GetRequiredMetadata(symbol).Signature;

                // Check if an overridden method variant has already taken precedence down the chain
                if (foundSignatures.Contains(signature + "!"))
                {
                    return false;
                }

                if (symbol.IsOverride)
                {
                    foundSignatures.Add(signature + "!"); // Seal this signature layout permanently
                }

                // Adds item to tracking dictionary on-the-fly; returns true if it didn't exist yet
                return foundSignatures.Add(signature);
            }

            // Use a localized hashset for on-the-fly override tracking and deduplication
            var foundSignatures = new HashSet<string>(StringComparer.Ordinal);

            // A processing queue replaces structural method recursion safely
            var typeQueue = new Queue<INamespaceOrTypeSymbol>();

            // Helper tracker to avoid infinite loops if interface structures form cyclic references
            var visitedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

            typeQueue.Enqueue(initialType);

            // Track if we need to fall back to appending System.Object members at the very end
            bool isTypeSearch = false;
            bool hasItemNameSearch = name == "get_Item" || name == "set_Item";

            while (typeQueue.Count > 0)
            {
                var type = typeQueue.Dequeue();

                // Standardize concrete array wrapper targets cleanly
                if (type is IArrayTypeSymbol arr)
                {
                    type = (INamespaceOrTypeSymbol)global.AdjustConcreteArrayType(arr);
                }

                if (type is ITypeSymbol ts)
                {
                    isTypeSearch = true;
                    if (!visitedTypes.Add(ts)) continue; // Skip if already completely processed
                }

                // 1. Fetch direct matching members at this current inheritance tier
                var members = string.IsNullOrEmpty(name) ? type.GetMembers() : type.GetMembers(name!);
                for (int i = 0; i < members.Length; i++)
                {
                    var m = members[i];
                    if (ShouldReturn(m, global, foundSignatures))
                    {
                        resultList.Add(m);
                    }
                }

                // 2. Handle Custom Indexers (get_Item / set_Item fallback overrides)
                if (hasItemNameSearch)
                {
                    var allMembers = type.GetMembers();
                    for (int i = 0; i < allMembers.Length; i++)
                    {
                        if (allMembers[i] is IPropertySymbol ps && ps.IsIndexer)
                        {
                            if (name == "get_Item" && ps.GetMethod != null)
                            {
                                if (ShouldReturn(ps.GetMethod, global, foundSignatures))
                                    resultList.Add(ps.GetMethod);
                            }
                            else if (name == "set_Item" && ps.SetMethod != null)
                            {
                                if (ShouldReturn(ps.SetMethod, global, foundSignatures))
                                    resultList.Add(ps.SetMethod);
                            }
                        }
                    }
                }

                // 3. Queue up inheritance dependencies if deep scanning is active
                if (deep && type is INamedTypeSymbol nt)
                {
                    if (nt.BaseType != null)
                    {
                        typeQueue.Enqueue(nt.BaseType);
                    }

                    if (nt.TypeKind == TypeKind.Interface)
                    {
                        var interfaces = nt.Interfaces;
                        for (int i = 0; i < interfaces.Length; i++)
                        {
                            typeQueue.Enqueue(interfaces[i]);
                        }
                    }
                }

                // 4. Queue up type constraints for type parameter symbols (like generic T constraints)
                if (type is ITypeParameterSymbol tp)
                {
                    var constraints = tp.ConstraintTypes;
                    for (int i = 0; i < constraints.Length; i++)
                    {
                        var c = constraints[i];
                        typeQueue.Enqueue(c);

                        // Queue up implicit sub-interface networks mapping from the constraints
                        var allInterfaces = c.AllInterfaces;
                        for (int j = 0; j < allInterfaces.Length; j++)
                        {
                            typeQueue.Enqueue(allInterfaces[j]);
                        }
                    }
                }
            }

            // 5. Append mandatory System.Object fallback items directly into the accumulator 
            if (deep && isTypeSearch)
            {
                var obj = (ITypeSymbol)global.GetSymbol("System.Object", null);
                var objMembers = string.IsNullOrEmpty(name) ? obj.GetMembers() : obj.GetMembers(name!);
                for (int i = 0; i < objMembers.Length; i++)
                {
                    if (ShouldReturn(objMembers[i], global, foundSignatures))
                    {
                        resultList.Add(objMembers[i]);
                    }
                }
            }

            return resultList;
        }


        public static IEnumerable<ISymbol> GetMembers(this INamespaceOrTypeSymbol type, string? name, GlobalCompilationVisitor global, bool deep = true)
        {
            //HashSet<string> found = new HashSet<string>();
            return RecursivelyGetMembers(type, name, global, deep);
        }
        internal static bool IsBooleanType(this ITypeSymbol type)
        {
            return type.SpecialType == SpecialType.System_Boolean;
        }
        internal static bool IsJsPrimitive(this ITypeSymbol type)
        {
            if (type.IsNullable(out var it))
                type = it!;
            if (type.TypeKind == TypeKind.Enum)
                return true;
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Char:
                case SpecialType.System_Enum:
                case SpecialType.System_String when Constants.HandleStringAsValueTypePrimitive:
                    return true;
            }
            if (type.IsType("System.Utf16Char") || type.IsType("System.Utf8Char"))
                return true;
            return false;
        }

        internal static bool IsUnsignedNumericType(this ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_UIntPtr:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                    return true;
            }
            if (type.IsType("System.Utf16Char") || type.IsType("System.Utf8Char"))
                return true;
            return false;
        }

        internal static bool IsSignedNumericType(this ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                    return true;
            }
            return false;
        }

        internal static bool IsNumericType(this ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_UInt64:
                case SpecialType.System_Int64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                    return true;
            }
            if (type.IsType("System.Utf16Char") || type.IsType("System.Utf8Char"))
                return true;
            return false;
        }

        internal static bool IsNumberNumericType(this ITypeSymbol type)
        {

            switch (type.SpecialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    return true;
            }
            if (type.IsType("System.Utf16Char") || type.IsType("System.Utf8Char"))
                return true;
            return false;
        }

        internal static bool IsLongNumericType(this ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_UInt64:
                case SpecialType.System_Int64:
                    return true;
            }
            return false;
        }

        internal static bool IsIntegerNumericType(this ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                    return true;
            }
            if (type.IsType("System.Utf16Char") || type.IsType("System.Utf8Char"))
                return true;
            return false;
        }

        internal static bool IsFloatingNumericType(this ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Double:
                case SpecialType.System_Single:
                    return true;
            }
            return false;
        }

        public static int GetNumericRangeRank(this ITypeSymbol type)
        {
            if (!type.IsNumericType())
            {
                throw new InvalidOperationException("Not a number");
            }
            int DefaultRank()
            {
                if (type.IsType("System.Utf16Char"))
                    return 1;
                if (type.IsType("System.Utf8Char"))
                    return 0;
                return 7;
            }
            return type.SpecialType switch
            {
                SpecialType.System_Byte => 0,
                SpecialType.System_SByte => 0,
                SpecialType.System_Char => 1,
                SpecialType.System_Int16 => 1,
                SpecialType.System_UInt16 => 1,
                SpecialType.System_Int32 => 2,
                SpecialType.System_UInt32 => 2,
                SpecialType.System_IntPtr => 2,
                SpecialType.System_UIntPtr => 2,
                SpecialType.System_Int64 => 3,
                SpecialType.System_UInt64 => 3,
                SpecialType.System_Single => 4,
                SpecialType.System_Double => 5,
                SpecialType.System_Decimal => 6,
                _ => DefaultRank()
            };

        }

        public static int GetNumericBits(this ITypeSymbol type)
        {
            if (!type.IsIntegerNumericType())
            {
                throw new InvalidOperationException("Not an integer number");
            }
            int DefaultBits()
            {
                if (type.IsType("System.Utf16Char") || type.IsType("System.Utf8Char"))
                    return 16;
                return 0;
            }
            return type.SpecialType switch
            {
                SpecialType.System_Byte => 8,
                SpecialType.System_SByte => 8,
                SpecialType.System_Char => 16,
                SpecialType.System_Int16 => 16,
                SpecialType.System_UInt16 => 16,
                SpecialType.System_Int32 => 32,
                SpecialType.System_UInt32 => 32,
                SpecialType.System_IntPtr => 32,
                SpecialType.System_UIntPtr => 32,
                SpecialType.System_Int64 => 64,
                SpecialType.System_UInt64 => 64,
                _ => DefaultBits()
            };
        }

        public static ulong GetNumericMask(this ITypeSymbol type)
        {
            if (!type.IsIntegerNumericType())
            {
                throw new InvalidOperationException("Not an integer number");
            }
            ulong DefaultMask()
            {
                if (type.IsType("System.Utf16Char"))
                    return 0xFFFF;
                if (type.IsType("System.Utf8Char"))
                    return 0xFF;
                return 0;
            }
            return type.SpecialType switch
            {
                SpecialType.System_Byte => 0xFF,
                SpecialType.System_SByte => 0xFF,
                SpecialType.System_Char => 0xFFFF,
                SpecialType.System_Int16 => 0xFFFF,
                SpecialType.System_UInt16 => 0xFFFF,
                SpecialType.System_Int32 => 0xFFFFFFFF,
                SpecialType.System_UInt32 => 0xFFFFFFFF,
                SpecialType.System_IntPtr => 0xFFFFFFFF,
                SpecialType.System_UIntPtr => 0xFFFFFFFF,
                SpecialType.System_Int64 => 0xFFFFFFFFFFFFFFFF,
                SpecialType.System_UInt64 => 0xFFFFFFFFFFFFFFFF,
                _ => DefaultMask()
            };
        }
        public static IEnumerable<INamedTypeSymbol> GetInterfaces(this INamedTypeSymbol _interface)
        {
            foreach (var _innerInterface in _interface.Interfaces)
            {
                yield return _innerInterface;
                foreach (var _minnerInterface in GetInterfaces(_innerInterface))
                {
                    yield return _minnerInterface;
                }
            }
        }

        public static bool IsValidateJsName(this string? name, bool allowDot = false, bool allowComma = false, bool allowSpace = false)
        {
            if (name == null)
                return false;
            if (name.IndexOfAny(['<', '>', !allowSpace ? ' ' : '\0', !allowComma ? ',' : '\0', !allowDot ? '.' : '\0']) >= 0)
            {
                return false;
            }
            if (name.EndsWith("."))
                return false;
            return true;
        }

        public static void ValidateJsName(this string? name, bool allowDot = false, bool allowComma = false, bool allowSpace = false)
        {
            if (!IsValidateJsName(name, allowDot, allowComma, allowSpace))
                throw new InvalidOperationException($"Invalid name identifier \"{name}\".");
        }

        public static string ResolveOperatorMethodName(this string _operator, int parametersCount, bool @checked = false)
        {
            if (_operator.StartsWith("op_"))
                return _operator;
            string operatorName = "Unknown";
            switch (_operator)
            {
                case "==":
                    operatorName = "Equality";
                    break;
                case "!=":
                    operatorName = "Inequality";
                    break;
                case ">":
                    operatorName = "GreaterThan";
                    break;
                case ">=":
                    operatorName = "GreaterThanOrEqual";
                    break;
                case "<":
                    operatorName = "LessThan";
                    break;
                case "<=":
                    operatorName = "LessThanOrEqual";
                    break;
                case "+=":
                    if (@checked)
                        operatorName = "CheckedAddition";
                    else
                        operatorName = "Addition";
                    break;
                case "-=":
                    if (@checked)
                        operatorName = "CheckedSubtraction";
                    else
                        operatorName = "Subtraction";
                    break;
                case "*":
                    if (@checked)
                        operatorName = "CheckedMultiply";
                    else
                        operatorName = "Multiply";
                    break;
                case "/":
                    if (@checked)
                        operatorName = "CheckedDivision";
                    else
                        operatorName = "Division";
                    break;
                case "%":
                    operatorName = "Modulus";
                    break;
                case "++":
                    if (@checked)
                        operatorName = "CheckedIncrement";
                    else
                        operatorName = "Increment";
                    break;
                case "--":
                    if (@checked)
                        operatorName = "CheckedDecrement";
                    else
                        operatorName = "Decrement";
                    break;
                case "+":
                    if (parametersCount == 1)
                        operatorName = "UnaryPlus";
                    else
                    {
                        if (@checked)
                            operatorName = "CheckedAddition";
                        else
                            operatorName = "Addition";
                    }
                    break;
                case "-":
                    if (parametersCount == 1)
                        operatorName = "UnaryNegation";
                    else
                    {
                        if (@checked)
                            operatorName = "CheckedSubtraction";
                        else
                            operatorName = "Subtraction";
                    }
                    break;
                case "|":
                    operatorName = "BitwiseOr";
                    break;
                case "&":
                    operatorName = "BitwiseAnd";
                    break;
                case "^":
                    operatorName = "ExclusiveOr";
                    break;
                case ">>":
                    operatorName = "RightShift";
                    break;
                case "<<":
                    operatorName = "LeftShift";
                    break;
                case "!":
                    operatorName = "LogicalNot";
                    break;
                case "~":
                    operatorName = "OnesComplement";
                    break;
                case "true":
                    operatorName = "True";
                    break;
                case "false":
                    operatorName = "False";
                    break;
                default:
                    break;
            }
            return "op_" + operatorName;
        }


        public static bool HasAnyAttribute(this MemberDeclarationSyntax node, string[] attributeNames, out Dictionary<string, List<AttributeSyntax>> atts)
        {
            atts = new Dictionary<string, List<AttributeSyntax>>();
            foreach (var attributes in node.AttributeLists)
            {
                foreach (var attribute in attributes.Attributes)
                {
                    foreach (var attributeName in attributeNames)
                    {
                        if (attributeName.StartsWith(attribute.Name.ToString()))
                        {
                            if (!atts.TryGetValue(attributeName, out var ats))
                            {
                                ats = new List<AttributeSyntax>();
                                atts[attributeName] = ats;
                            }
                            ats.Add(attribute);
                        }
                    }
                }
            }
            return atts.Count > 0;
            //if (node.AttributeLists.Any(a => a.Attributes.Any(aa => attributeNames.Any(an => an.StartsWith(aa.Name.ToString())))))
            //    return true;
            //return false;
        }

        //public static bool HasAttribute(MemberDeclarationSyntax node, string attributeName)
        //{
        //    if (HasAttachedAttribute())
        //        attributeName = attributeName.Substring(0, attributeName.Length - 9);
        //    if (node.AttributeLists.SelectMany(a => a.Attributes).Any(a => attributeName == a.Name.GetText().ToString()))
        //        return true;
        //    return false;
        //}

        public static AttributeData? GetTemplateAttribute(this ISymbol symbol, GlobalCompilationVisitor _global, TranslatorSyntaxVisitor? visitor, bool checkPropertyAccessors = false)
        {
            var templateAttributeSymbol = _global.GetSymbol(typeof(TemplateAttribute).FullName!, null/*, out _, out _*/);
            var attributes = symbol.OriginalDefinition.GetAttributes().Where(a => a.AttributeClass?.Equals(templateAttributeSymbol, SymbolEqualityComparer.Default) ?? false).ToList();
            AttributeData? attribute = null;
            if (attributes?.Count > 1)
            {
                //chose one of the attribute based on condition
                foreach (var att in attributes)
                {
                    if (att.ConstructorArguments.Length == 2)
                    {
                        var condition = att.ConstructorArguments[1].Value?.ToString();
                        if (condition != null)
                        {
                            if (_global.Evaluate(condition, visitor) != null)
                            {
                                attribute = att;
                            }
                        }
                    }
                }
                if (attribute == null)
                    attribute = attributes.FirstOrDefault();
            }
            else
            {
                attribute = attributes?.FirstOrDefault(a => a.AttributeClass?.Equals(templateAttributeSymbol, SymbolEqualityComparer.Default) ?? false);
            }
            if (attribute == null && checkPropertyAccessors && symbol is IPropertySymbol ps)
            {
                return ps.GetMethod?.GetTemplateAttribute(_global, visitor) ?? ps.SetMethod?.GetTemplateAttribute(_global, visitor);
            }
            return attribute;
        }

        public static AttributeData? GetJsImportAttribute(this ISymbol symbol, GlobalCompilationVisitor _global, TranslatorSyntaxVisitor? visitor, bool checkPropertyAccessors = false)
        {
            var importAttributeSymbol = _global.TryGetSymbol("System.Runtime.InteropServices.JavaScript.JSImportAttribute", null/*, out _, out _*/);
            if (importAttributeSymbol == null)
                return null;
            var allAttributes = symbol.OriginalDefinition.GetAttributes();
            var attributes = allAttributes.Where(a => a.AttributeClass?.Equals(importAttributeSymbol, SymbolEqualityComparer.Default) ?? false);
            AttributeData? attribute = attributes?.FirstOrDefault();
            if (attribute != null)
            {
                var noImportAttributeSymbol = _global.TryGetSymbol("NetJs.NoJSImportAttribute", null/*, out _, out _*/);
                if (noImportAttributeSymbol != null)
                {
                    var mattributes = allAttributes.Where(a => a.AttributeClass?.Equals(noImportAttributeSymbol, SymbolEqualityComparer.Default) ?? false);
                    AttributeData? mattribute = mattributes?.FirstOrDefault();
                    if (mattribute != null)
                        return null;
                }
            }
            return attribute;
        }

        public static bool IsInvokable(this IMethodSymbol method, GlobalCompilationVisitor _global)
        {
            bool isExtern = method.IsExtern || _global.HasAttribute(method, typeof(ExternalAttribute).FullName!, null, false, out _) ||
                 (method.AssociatedSymbol?.IsExtern ?? false) || (method.AssociatedSymbol != null && _global.HasAttribute(method.AssociatedSymbol, typeof(ExternalAttribute).FullName!, null, false, out _));
            bool hasTemplate = method.GetTemplateAttribute(_global, null) != null;
            if (!isExtern || hasTemplate)
            {
                return true;
            }
            return false;
        }

        //public static bool HasAnyAttribute(this ISymbol symbol, bool inherits, string[] attributeNames, out Dictionary<string, Dictionary<object, object?>>? constructorArgs)
        //{
        //    if (symbol.Kind == SymbolKind.Namespace)
        //    {
        //        constructorArgs = null;
        //        return false;
        //    }
        //    if (symbol is ITypeSymbol ts && ts.IsNullable(out var it))
        //        symbol = it!;
        //    //var symbols = attributeNames.Select(s => GetTypeSymbol(s, visitor/*, out _, out _*/)).ToList();
        //    constructorArgs = null;
        //    Dictionary<string, Dictionary<object, object?>>? mconstructorArgs = null;
        //    if (symbol.GetAttributes().Where(e => e.AttributeClass != null).Any(a =>
        //    {
        //        var aName = a.AttributeClass!.ToString();// !.CreateFullTypeName(this, withGlobalNamespace: false)!;
        //        if (!aName.EndsWith("Attribute"))
        //            aName += "Attribute";
        //        if (attributeNames.Contains(aName))
        //        {
        //            mconstructorArgs ??= new Dictionary<string, Dictionary<object, object?>>();
        //            mconstructorArgs[aName] = a.ConstructorArguments.Select((c, i) => ((object)i, c.Value))
        //            .Concat(a.NamedArguments.Select(a => ((object)a.Key, a.Value.Value)))
        //            .ToDictionary(kv => kv.Item1, kv => kv.Value);
        //            return true;
        //        }
        //        return false;
        //        //var aSymbol = TryGetTypeSymbol(aName, visitor/*, out _, out _*/);
        //        //if (aSymbol ==null)
        //        //    return false;
        //        //return symbols.Contains(aSymbol);
        //    }))
        //    {
        //        constructorArgs = mconstructorArgs;
        //        return true;
        //    }
        //    if (inherits && symbol is ITypeSymbol ns && ns.BaseType != null)
        //    {
        //        return HasAnyAttribute(ns.BaseType, inherits, attributeNames, out constructorArgs);
        //    }
        //    if (inherits && symbol is IMethodSymbol ms && ms.OverriddenMethod != null)
        //    {
        //        return HasAnyAttribute(ms.OverriddenMethod, inherits, attributeNames, out constructorArgs);
        //    }
        //    if (inherits && symbol is IPropertySymbol ps && ps.OverriddenProperty != null)
        //    {
        //        return HasAnyAttribute(ps.OverriddenProperty, inherits, attributeNames, out constructorArgs);
        //    }
        //    return false;
        //}

        //public static bool HasAttribute(this ISymbol symbol, string attributeName, bool inherits, out Dictionary<object, object?> constructorArgs)
        //{
        //    constructorArgs = null!;
        //    return HasAnyAttribute(symbol, inherits, [attributeName], out var margs) && margs != null && margs.TryGetValue(attributeName, out constructorArgs);
        //}

        //public static bool IsStaticCallConvention(this ISymbol symbol, bool? inherits = null)
        //{
        //    if (symbol.IsStatic)
        //        return false;
        //    //field access cannot use static convention
        //    if (symbol.Kind == SymbolKind.Field)
        //        return false;
        //    //An explicit implementation cannot use static convention
        //    if (symbol is IMethodSymbol method && method.ExplicitInterfaceImplementations.Any())
        //        return false;
        //    ////A method tht overrides must conform to its overriden convention
        //    //if (symbol is IMethodSymbol method2 && method2.IsOverride)
        //    //{
        //    //    return method2.OverriddenMethod!.IsStaticCallConvention(_global);
        //    //}
        //    var attributeName = typeof(StaticCallConventionAttribute).FullName;
        //    if (HasAnyAttribute(symbol, inherits ?? true, [attributeName], out var margs))
        //    {
        //        var args = margs.Single().Value;
        //        if (args.Count == 1)
        //        {
        //            return (bool)args[0]!;
        //        }
        //        return true;
        //    }
        //    if (symbol.ContainingType != null)
        //    {
        //        return IsStaticCallConvention(symbol.ContainingType, inherits ?? true);
        //    }
        //    return false;
        //    //if (symbol.GetAttributes().Select(a => (a, a.AttributeClass)).Where(e => e.AttributeClass != null).Any(a =>
        //    //{
        //    //    var aName = a.AttributeClass!.ToString();
        //    //    if (!aName.EndsWith("Attribute"))
        //    //        aName += "Attribute";
        //    //    if (aName != attributeName)
        //    //        return false;
        //    //    return true;
        //    //}))
        //    //{
        //    //    return true;
        //    //}
        //    //if (symbol.ContainingType != null)
        //    //{
        //    //    return IsStaticCallConvention(symbol.ContainingType);
        //    //}
        //    //return false;
        //}


        public static bool IsExtensionPropertyMember(this ISymbol symbol, GlobalCompilationVisitor _global)
        {
            if (symbol.Kind == SymbolKind.Property && symbol.ContainingType.IsExtension)
            {
                return true;
            }
            return false;
        }

        public static bool IsStaticCallConvention(this ISymbol symbol, GlobalCompilationVisitor _global, bool? inherits = null)
        {
            if (symbol.IsStatic)
                return false;
            //field access cannot use static convention
            if (symbol.Kind == SymbolKind.Field)
                return false;
            //An explicit implementation cannot use static convention
            if (symbol is IMethodSymbol method && method.ExplicitInterfaceImplementations.Any())
                return false;
            ////A method tht overrides must conform to its overriden convention
            //if (symbol is IMethodSymbol method2 && method2.IsOverride)
            //{
            //    return method2.OverriddenMethod!.IsStaticCallConvention(_global);
            //}
            if (_global.HasAttribute(symbol, typeof(StaticCallConventionAttribute).FullName!, null, inherits ?? true, out var args))
            {
                if (args != null && args.Count > 0)
                {
                    return (bool)args[0];
                }
                return true;
            }
            if (symbol.ContainingType != null && _global.HasAttribute(symbol.ContainingType, typeof(StaticCallConventionAttribute).FullName!, null, inherits ?? true, out args))
            {
                if (args != null && args.Count > 0)
                {
                    return (bool)args[0];
                }
                return true;
            }
            return false;
        }

        //public static bool IsStaticCallConvention(this IPropertySymbol property, GlobalCompilationVisitor _global)
        //{
        //    if (_global.HasAttribute(property, typeof(StaticCallConventionAttribute).FullName!, null, false, out _))
        //        return true;
        //    if (_global.HasAttribute(property.ContainingType, typeof(StaticCallConventionAttribute).FullName!, null, false, out _))
        //        return true;
        //    return false;
        //}

        //TODO: This doesnt seem to work well, especially for Symbol generated from metadata
        //Method.IsImplicitlyDeclared is always false
        //We tried not calling this method for now from the caller by cheking the containing symbol of the parameters to the primary constructor
        public static bool IsPrimaryConstructor(this IMethodSymbol methodSymbol, GlobalCompilationVisitor _global)
        {
            if (methodSymbol.MethodKind != MethodKind.Constructor)
            {
                return false;
            }
            var definingSyntax = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

            if (definingSyntax is ClassDeclarationSyntax classDeclaration)
            {
                return classDeclaration.ParameterList != null && classDeclaration.ParameterList.Parameters.Any();
            }
            else if (definingSyntax is StructDeclarationSyntax structDeclaration)
            {
                return structDeclaration.ParameterList != null && structDeclaration.ParameterList.Parameters.Any();
            }
            else if (definingSyntax is RecordDeclarationSyntax recordDeclaration)
            {
                return recordDeclaration.ParameterList != null && recordDeclaration.ParameterList.Parameters.Any();
            }
            return false;
            //return method.MethodKind == MethodKind.Constructor && method.IsImplicitlyDeclared;
        }

        public static string GetLiteralString(this LiteralExpressionSyntax node, GlobalCompilationVisitor global)
        {
            //if (node.ToString().Contains("\\uFB00\\uFB50"))
            //{

            //}
            //if (node.ToString().StartsWith("\"\"\""))
            //{

            //}
            return GetLiteralString(node.Token.ValueText, (SyntaxKind)node.RawKind, global);
        }
        public static string GetLiteralString(this string txt, SyntaxKind kind, GlobalCompilationVisitor global)
        {
            if (txt == "default")
            {
                txt = $"{global.GlobalName}.$default()";
            }
            else if (kind == SyntaxKind.StringLiteralExpression) //handless @"jsdd" string 
            {
                /*if (txt.StartsWith("@") && txt.Length > 1 && txt[1] == '\"' && txt.EndsWith("\""))
                {
                    txt = "\"" + txt.Substring(2, txt.Length - 3).EscapeString() + "\"";
                }
                else if (txt.StartsWith("\"\"\""))
                {
                    int startingQuotes = 0;
                    for (int i = 0; i < txt.Length; i++)
                    {
                        if (txt[i] == '\"')
                            startingQuotes++;
                        else
                            break;
                    }
                    txt = "\"" + txt.Substring(startingQuotes, txt.Length - startingQuotes - startingQuotes)
                        .EscapeString() + "\"";
                }
                else*/
                if (txt.Contains("\0"))//In js a string like "[G\003" is invalid in strict mode. We split as "[G\0" + "03"
                {
                    var indexOfZero = txt.IndexOf('\0');
                    var charAfterZero = indexOfZero + 1 < txt.Length ? txt[indexOfZero + 1] : '\0';
                    if (char.IsNumber(charAfterZero))
                    {
                        var pre = txt.Substring(0, indexOfZero + 1);
                        var post = txt.Substring(indexOfZero + 1);
                        txt = $"\"{pre.EscapeString()}\" + \"{post.EscapeString()}\"";
                    }
                    else
                    {
                        txt = $"\"{txt.EscapeString()}\"";
                    }
                }
                else
                {
                    txt = $"\"{txt.EscapeString()}\"";
                }
            }
            else if (kind == SyntaxKind.MultiLineRawStringLiteralToken) //handless """jsdd""" string 
            {
                int startingQuotes = 0;
                for (int i = 0; i < txt.Length; i++)
                {
                    if (txt[i] == '\"')
                        startingQuotes++;
                    else
                        break;
                }
                txt = "\"" + txt.Substring(startingQuotes, txt.Length - startingQuotes - startingQuotes)
                    .EscapeString();
                //.Replace("\r", "\\r")
                //.Replace("\n", "\\n") + "\"";
            }
            //Char literal written as plain number
            else if (kind == SyntaxKind.CharacterLiteralExpression) //handless ''
            {
                var originalChar = txt;
                if (txt.StartsWith("'") && txt.EndsWith("'") && txt.Length == 3)
                {
                    txt = txt.Substring(1, txt.Length - 2);
                }
                if (txt.StartsWith("\\x") || txt.StartsWith("\\u"))
                {
                    int HexToInt(char c)
                    {
                        c = char.ToUpper(c);
                        if (c <= '9')
                            return c - '0';
                        return (c - 'A') + 10;
                    }
                    int value = 0;
                    for (int i = 2; i < txt.Length; i++)
                    {
                        value *= 16;
                        value += HexToInt(txt[i]);
                    }
                    txt = value.ToString();
                }
                else if (txt.StartsWith("\\") && txt.Length == 2)
                {
                    string AsInt(char c)
                    {
                        if (c >= '0' && c <= '9')
                            return ((int)(c - '0')).ToString();
                        return ((int)c).ToString();
                    }
                    switch (txt[1])
                    {
                        case 'r':
                            {
                                txt = AsInt('\r');
                                break;
                            }
                        case 'n':
                            {
                                txt = AsInt('\n');
                                break;
                            }
                        case 't':
                            {
                                txt = AsInt('\t');
                                break;
                            }
                        case 'v':
                            {
                                txt = AsInt('\v');
                                break;
                            }
                        case 'f':
                            {
                                txt = AsInt('\f');
                                break;
                            }
                        case '\\':
                            {
                                txt = AsInt('\\');
                                break;
                            }
                        default:
                            {
                                txt = AsInt(txt[1]);
                                break;
                            }
                    }
                }
                else
                {
                    txt = ((int)txt[0]).ToString();
                }
                txt = $"/*{originalChar.EscapeString()}*/ {txt}";
            }
            //else if (node.IsKind(SyntaxKind.NumericLiteralExpression) && txt.StartsWith("0b", StringComparison.InvariantCultureIgnoreCase)) //0b10101
            //{
            //    txt = txt.Substring(2);
            //    int value = 0;
            //    for (int i = 0; i < txt.Length; i++)
            //    {
            //        if (txt[i] == '_')
            //            continue;
            //        value <<= 1;
            //        value += txt[i] == '1' ? 1 : 0;
            //    }
            //    txt = value.ToString();
            //}
            else if (kind == SyntaxKind.NumericLiteralExpression && txt.EndsWith("U", StringComparison.InvariantCultureIgnoreCase)) //handle 10u
            {
                txt = txt.Substring(0, txt.Length - 1).Replace("_", "");
            }
            else if (kind == SyntaxKind.NumericLiteralExpression && txt.EndsWith("UL", StringComparison.InvariantCultureIgnoreCase)) //handle 10UL
            {
                txt = txt.Substring(0, txt.Length - 2).Replace("_", "");
            }
            else if (kind == SyntaxKind.NumericLiteralExpression && txt.EndsWith("L", StringComparison.InvariantCultureIgnoreCase)) //handle 10L
            {
                txt = txt.Substring(0, txt.Length - 1).Replace("_", "");
            }
            else if (kind == SyntaxKind.NumericLiteralExpression && txt.EndsWith("f", StringComparison.InvariantCultureIgnoreCase) && !txt.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase)) //handle 10.0f
            {
                txt = txt.Substring(0, txt.Length - 1).Replace("_", "");
            }
            else if (kind == SyntaxKind.NumericLiteralExpression && txt.EndsWith("D", StringComparison.InvariantCultureIgnoreCase) && !txt.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase)) //handle 10D
            {
                txt = txt.Substring(0, txt.Length - 1).Replace("_", "");
            }
            else if (kind == SyntaxKind.NumericLiteralExpression && txt.EndsWith("m", StringComparison.InvariantCultureIgnoreCase)) //handle decimal with m suffix
            {
                txt = txt.Substring(0, txt.Length - 1).Replace("_", "");
            }
            else if (kind == SyntaxKind.NumericLiteralExpression)
            {
                if (txt.Length > 1 &&
                    txt[0] == '0' &&
                    !txt.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase) &&
                    !txt.StartsWith("0b", StringComparison.InvariantCultureIgnoreCase) &&
                    !txt.Contains("."))
                    txt = txt.Substring(1); //js would interprete leading zero in literal number as octal
                txt = txt.Replace("_", "");
            }
            else if (kind == SyntaxKind.NullLiteralExpression)
            {
                txt = "null";
            }
            return txt;
        }

        public static bool IsAutoProperty(this IPropertySymbol propertySymbol)
        {
            // Get fields declared in the same type as the property
            var fields = propertySymbol.ContainingType.GetMembers().OfType<IFieldSymbol>();
            // Check if one field is associated to
            return fields.Any(field => SymbolEqualityComparer.Default.Equals(field.AssociatedSymbol, propertySymbol));
        }

        public static RefKind? GetRefKind(this ISymbol lhs)
        {
            return (lhs as IParameterSymbol)?.RefKind ??
                (lhs as IFieldSymbol)?.RefKind ??
                (lhs as ILocalSymbol)?.RefKind ??
                (lhs as IPropertySymbol)?.RefKind ??
                (lhs as IMethodSymbol)?.RefKind ??
                (lhs is ITypeSymbol ? RefKind.None : null);
        }

        public static ITypeSymbol GetTypeSymbol(this ISymbol symbol)
        {
            ITypeSymbol TryUnwrapInlineArray(ITypeSymbol type)
            {
                //if (type.IsInlineArray(out int sz))
                //{
                //    var field = (IFieldSymbol?)type.GetMembers().FirstOrDefault();
                //    if (field != null)
                //    {
                //        //symbol.
                //    }
                //}
                return type;
            }
            if ((symbol.Kind == SymbolKind.NamedType || symbol.Kind == SymbolKind.ArrayType || symbol.Kind == SymbolKind.PointerType || symbol.Kind == SymbolKind.FunctionPointerType) &&
                symbol is ITypeSymbol type)
            {
                return TryUnwrapInlineArray(type);
            }
            if (symbol.Kind == SymbolKind.Property && symbol is IPropertySymbol property)
            {
                return TryUnwrapInlineArray(property.Type);
            }
            if (symbol.Kind == SymbolKind.Field && symbol is IFieldSymbol field)
            {
                return TryUnwrapInlineArray(field.Type);
            }
            if (symbol.Kind == SymbolKind.Local && symbol is ILocalSymbol local)
            {
                return TryUnwrapInlineArray(local.Type);
            }
            if (symbol.Kind == SymbolKind.Parameter && symbol is IParameterSymbol parameter)
            {
                return TryUnwrapInlineArray(parameter.Type);
            }
            if (symbol.Kind == SymbolKind.TypeParameter && symbol is ITypeParameterSymbol tparameter)
            {
                return TryUnwrapInlineArray(tparameter);
            }
            if (symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol method)
            {
                if (method.Name == "op_Implicit")
                    return TryUnwrapInlineArray(method.Parameters.First().Type);
                if (method.MethodKind == MethodKind.Constructor)
                    return method.ContainingType;
                return TryUnwrapInlineArray(method.ReturnType);
            }
            if (symbol.Kind == SymbolKind.Discard && symbol is IDiscardSymbol discard)
            {
                return TryUnwrapInlineArray(discard.Type);
            }
            if (symbol.Kind == SymbolKind.Event && symbol is IEventSymbol ev)
            {
                return TryUnwrapInlineArray(ev.Type);
            }
            if (symbol.Kind == SymbolKind.DynamicType)
            {
                return (ITypeSymbol)symbol;
            }
            throw new InvalidOperationException($"Cannot evaluate type from {symbol}");
        }
        public static MemberFlagsModel GetSymbolFlags(this ISymbol symbol)
        {
            var flags = MemberFlagsModel.None;

            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public: flags |= MemberFlagsModel.IsPublic; break;
                case Accessibility.Private: flags |= MemberFlagsModel.IsPrivate; break;
                case Accessibility.Protected: flags |= MemberFlagsModel.IsFamily; break;
                case Accessibility.Internal: flags |= MemberFlagsModel.IsAssembly; break;
                case Accessibility.ProtectedOrInternal: flags |= MemberFlagsModel.IsFamilyOrAssembly; break;
            }

            if (symbol.IsStatic) flags |= MemberFlagsModel.IsStatic;
            if (symbol.IsAbstract) flags |= MemberFlagsModel.IsAbstract;
            if (symbol.IsVirtual) flags |= MemberFlagsModel.IsVirtual;
            if (symbol.IsOverride) flags |= MemberFlagsModel.IsOverride;
            if (symbol is IMethodSymbol method)
            {
                if (method.MethodKind == MethodKind.AnonymousFunction) flags |= MemberFlagsModel.IsAnonymous;
                if (method.IsAsync) flags |= MemberFlagsModel.IsAsync;
                if (method.IsGenericMethod) flags |= MemberFlagsModel.IsGeneric;
                if (method.ReturnType.Kind == SymbolKind.TypeParameter && method.ReturnType is ITypeParameterSymbol tp && tp.Variance == VarianceKind.Out)
                    flags |= MemberFlagsModel.ReturnTypeIsCovariantOut;
            }
            if (symbol.IsSealed) flags |= MemberFlagsModel.IsSealed;

            return flags;
        }
        public static bool IsAwaitable(this ITypeSymbol type)
        {
            bool IsINotifyCompletionOrICriticalNotifyCompletion(ITypeSymbol type)
            {
                if (type.IsType("System.Runtime.CompilerServices.INotifyCompletion"))
                {
                    return true;
                }
                if (type.IsType("System.Runtime.CompilerServices.ICriticalNotifyCompletion"))
                {
                    return true;
                }
                return false;
            }
            bool HasIsCompleted(ITypeSymbol type)
            {
                var isCompleted = type.GetMembers("IsCompleted").Where(e => e is IPropertySymbol ps && ps.GetMethod != null && ps.Type.IsType("System.Boolean"));
                var getResult = type.GetMembers("GetResult").Where(e => e is IMethodSymbol ms && ms.Parameters.Length == 0);
                return isCompleted.Any() && getResult.Any();
            }
            var getAwaiter = type.GetMembers("GetAwaiter")
                .Where(e => e is IMethodSymbol ms &&
                        ms.DeclaredAccessibility == Accessibility.Public &&
                        ms.ReturnType.AllInterfaces.Any(i => IsINotifyCompletionOrICriticalNotifyCompletion(i)) &&
                        HasIsCompleted(ms.ReturnType));
            var ret = getAwaiter.Any();
            return ret;
        }

        /// <summary>
        /// Extension method to determine if a type symbol is structurally immutable.
        /// Safely handles Case A (RuntimeTypeHandle: all fields readonly, allows methods)
        /// and Case B (QCallTypeHandle: has mutable fields, but zero mutating methods).
        /// </summary>
        public static bool IsStructurallyImmutable(this ITypeSymbol? type)
        {
            if (type == null) return false;

            if (type is INamedTypeSymbol namedType)
            {
                // 1. Explicit baseline structural shortcuts
                if (namedType.IsReadOnly) return true; // Explicitly declared 'readonly struct'
                if (namedType.IsAnonymousType) return true; // Roslyn anonymous types are inherently immutable

                // Interfaces, abstract types, and static blocks cannot be verified this way
                if (namedType.TypeKind == TypeKind.Interface || namedType.IsAbstract || namedType.IsStatic)
                    return false;

                // Track state variations across the type members
                bool hasMutableFields = false;
                bool hasRegularMethods = false;

                var members = namedType.GetMembers();

                for (int i = 0; i < members.Length; i++)
                {
                    var member = members[i];
                    if (member.IsStatic) continue; // Static state doesn't impact instance immutability

                    switch (member)
                    {
                        case IFieldSymbol field:
                            if (!field.IsReadOnly)
                            {
                                // RULE A: If a field is mutable, it MUST be private to ensure external encapsulation
                                if (field.DeclaredAccessibility != Accessibility.Private)
                                {
                                    return false;
                                }
                                hasMutableFields = true;
                            }
                            break;

                        case IPropertySymbol property:
                            var setMethod = property.SetMethod;
                            if (setMethod != null)
                            {
                                // RULE B: Allow standard 'init;' auto-properties, but reject manual 'init { }' blocks
                                if (!setMethod.IsInitOnly || setMethod.DeclaringSyntaxReferences.Length > 0)
                                {
                                    return false;
                                }
                            }
                            break;

                        case IMethodSymbol method:
                            // Filter out boilerplate accessors (constructors, getters, and automatic init setters)
                            if (method.MethodKind is not (MethodKind.Constructor or MethodKind.PropertyGet or MethodKind.PropertySet))
                            {
                                hasRegularMethods = true;
                            }

                            // Strict protection: If an explicit setter or init accessor contains custom body code, reject it
                            if (method.MethodKind == MethodKind.PropertySet && method.DeclaringSyntaxReferences.Length > 0)
                            {
                                return false;
                            }
                            break;
                    }
                }

                // 3. FINAL EVALUATION PARADIGM MATCHING:
                // Case A (RuntimeTypeHandle): All fields are readonly -> Safe regardless of how many methods exist.
                // Case B (QCallTypeHandle): Contains mutable private fields -> Only safe if it has ZERO regular methods.
                if (hasMutableFields && hasRegularMethods)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private static readonly string TargetAttributeFullName = typeof(ConventionAttribute).FullName!;
        public static ConventionAttribute? GetConvention(this ISymbol symbol, GlobalCompilationVisitor global)
        {
            var currentSymbol = symbol;
            while (currentSymbol != null)
            {
                var attributes = currentSymbol.GetAttributes();
                for (int i = 0; i < attributes.Length; i++)
                {
                    var a = attributes[i];
                    var attributeClass = a.AttributeClass;
                    if (attributeClass == null) continue;

                    var aName = attributeClass.CreateSignature(global, withGlobalNamespace: false);
                    if (aName != null)
                    {
                        bool isMatch = aName.Equals(TargetAttributeFullName, StringComparison.Ordinal) ||
                                      (!aName.EndsWith("Attribute", StringComparison.Ordinal) &&
                                       (aName.Length + 9 == TargetAttributeFullName.Length) &&
                                       TargetAttributeFullName.StartsWith(aName, StringComparison.Ordinal));

                        if (isMatch)
                        {
                            object? notation = null;
                            object? target = null;
                            object? member = null;

                            var constructorArgs = a.ConstructorArguments;
                            var namedArgs = a.NamedArguments;

                            if (constructorArgs.Length > 0)
                            {
                                notation = constructorArgs[0].Value;
                                if (constructorArgs.Length > 1)
                                {
                                    target = constructorArgs[1].Value;
                                }
                            }

                            for (int j = 0; j < namedArgs.Length; j++)
                            {
                                var namedPair = namedArgs[j];
                                switch (namedPair.Key)
                                {
                                    case nameof(ConventionAttribute.Notation):
                                        notation ??= namedPair.Value.Value;
                                        break;
                                    case nameof(ConventionAttribute.Target):
                                        target ??= namedPair.Value.Value;
                                        break;
                                    case nameof(ConventionAttribute.Member):
                                        member ??= namedPair.Value.Value;
                                        break;
                                }
                            }

                            return new ConventionAttribute
                            {
                                Notation = notation != null ? (Notation)Convert.ToInt32(notation) : Notation.None,
                                Target = target != null ? (ConventionTarget)Convert.ToInt32(target) : ConventionTarget.All,
                                Member = member != null ? (ConventionMember)Convert.ToInt32(member) : ConventionMember.All,
                            };
                        }
                    }
                }

                var container = currentSymbol.ContainingSymbol;
                if (container == null) break;

                if (currentSymbol is INamedTypeSymbol && container is INamedTypeSymbol)
                {
                    break;
                }

                currentSymbol = container;
            }

            return null;
        }

        //ConventionAttribute? GetConvention(ISymbol symbol)
        //{
        //    foreach (var a in symbol.GetAttributes().Select(a => (a, a.AttributeClass)).Where(e => e.AttributeClass != null))
        //    {
        //        var aName = a.AttributeClass!.CreateSignature(this, withGlobalNamespace: false)!;
        //        if (!aName.EndsWith("Attribute"))
        //            aName += "Attribute";
        //        if (aName == typeof(ConventionAttribute).FullName)
        //        {
        //            var notation = a.a.ConstructorArguments.Count() > 0 ? a.a.ConstructorArguments.ElementAt(0).Value : a.a.NamedArguments.FirstOrDefault(c => c.Key == nameof(ConventionAttribute.Notation)).Value.Value;
        //            var target = a.a.ConstructorArguments.Count() > 1 ? a.a.ConstructorArguments.ElementAt(1).Value : a.a.NamedArguments.FirstOrDefault(c => c.Key == nameof(ConventionAttribute.Target)).Value.Value;
        //            var member = a.a.NamedArguments.FirstOrDefault(c => c.Key == nameof(ConventionAttribute.Member)).Value.Value;
        //            return new ConventionAttribute
        //            {
        //                Notation = notation != null ? (Notation)int.Parse(notation.ToString()!) : Notation.None,
        //                Target = target != null ? (ConventionTarget)int.Parse(target.ToString()!) : ConventionTarget.All,
        //                Member = member != null ? (ConventionMember)int.Parse(member.ToString()!) : ConventionMember.All,
        //            };
        //        }
        //    }
        //    if (symbol.ContainingSymbol != null)
        //    {
        //        if (symbol is INamedTypeSymbol && symbol.ContainingSymbol is INamedTypeSymbol) //dont inherit conventions for inner class
        //        {

        //        }
        //        else
        //            return GetConvention(symbol.ContainingSymbol);
        //    }
        //    return null;
        //}

    }
}