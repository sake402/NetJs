using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.CompilerServices;

namespace NetJs.Translator.CSharpToJavascript
{
    public class CodeBlockClosure : IDisposable
    {
        GlobalCompilationVisitor _global;
        public CSharpSyntaxNode Syntax { get; }
        public ISymbol? Symbol { get; }

        public CodeBlockClosure(GlobalCompilationVisitor global, TranslatorSyntaxVisitor visitor, CSharpSyntaxNode syntax, ISymbol? symbol = null, CodeBlockClosure? parent = null)
        {
            _global = global;
            Syntax = syntax;
            Symbol = symbol ?? _global.TryGetSymbol(syntax, visitor/*, out _, out _*/);
            Parent = parent;
        }


        public CodeBlockClosure? FindHierachy<T>() where T : CSharpSyntaxNode
        {
            var current = this;
            while (current != null)
            {
                if (current.Syntax is T t)
                    return current;
                current = current.Parent;
            }
            return default;
        }

        public List<(Action Write, bool Static)> TypeInitializers = new();
        public void RegisterTypeInitializer(Action action, bool _static)
        {
            TypeInitializers.Add((action, _static));
        }


        Dictionary<string, (CodeSymbol, string?, string?, int)> _identifiers = new Dictionary<string, (CodeSymbol, string?, string?, int)>();
        //Dictionary<string, CodeType> _tempIdentifiers = new Dictionary<string, CodeType>();
        public IDisposable DefineIdentifierType(string identifier, TypeSyntax type, SymbolKind kind, [CallerFilePath] string? file = null, [CallerMemberName] string? memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            _identifiers[identifier] = (CodeSymbol.From(type, kind), file, memberName, lineNumber);
            return new DelegateDispose(() =>
            {
                _identifiers.Remove(identifier);
            });
        }
        //public void Define(string identifier, INamedTypeSymbol type)
        //{
        //    _identifiers[identifier] = CodeType.From(type);
        //}
        public IDisposable DefineIdentifierType(string identifier,
            CodeSymbol type,
            bool throwOnDuplicate = true,
            [CallerFilePath] string? file = null,
            [CallerMemberName] string? memberName = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            if (type.TypeSyntaxOrSymbol == null)
            {
                return new DelegateDispose(() =>
                {
                });
            }
            //if (!identifier.StartsWith("$") && type.TypeSyntaxOrSymbol is ITypeSymbol && type.TypeSyntaxOrSymbol is not ITypeParameterSymbol)
            //{

            //}
            if (_identifiers.TryGetValue(identifier, out var cd) && !cd.Item1.TypeSyntaxOrSymbol!.Equals(type.TypeSyntaxOrSymbol))
            {
                if (cd.Item1.TypeSyntaxOrSymbol is MemberSymbolOverload mo && mo.Overloads.Contains(type.TypeSyntaxOrSymbol))
                {
                    return new DelegateDispose(() =>
                    {
                    });
                }
                if (identifier != "_" && throwOnDuplicate) //discard can be redefined/reused in a scope
                    throw new InvalidOperationException($"Attempt to redefine an existing symbol {identifier} = {cd.Item1.TypeSyntaxOrSymbol} as {type.TypeSyntaxOrSymbol}. Initially defined at {cd.Item2}.{cd.Item3}:{cd.Item4}");
            }
            _identifiers[identifier] = (type, file, memberName, lineNumber);
            return new DelegateDispose(() =>
            {
                _identifiers.Remove(identifier);
            });
        }
        public CodeBlockClosure? Parent { get; }
        public string? Name { get; set; }
        public Dictionary<string, string> Tags { get; } = new Dictionary<string, string>();
        public string? JumpStartLabelName { get; set; }
        public string? JumpStateMachineVariableName { get; set; }
        public Dictionary<CSharpSyntaxNode, string> JumpLabels { get; } = new Dictionary<CSharpSyntaxNode, string>();
        public List<string> GotoJumpLabels { get; } = new List<string>();
        Stack<IEnumerable<ISymbol>> _anonymousMethodParameterTypes = new Stack<IEnumerable<ISymbol>>();
        public IDisposable DefineAnonymousMethodParameterTypes(IEnumerable<ISymbol> types)
        {
            _anonymousMethodParameterTypes.Push(types);
            int depth = _anonymousMethodParameterTypes.Count;
            return new DelegateDispose(() =>
            {
                if (_anonymousMethodParameterTypes.Count == depth)
                {
                    _anonymousMethodParameterTypes.Pop();
                }
            });
        }

        public IEnumerable<ISymbol>? GetAnonymousMethodParameterTypes()
        {
            _anonymousMethodParameterTypes.TryPop(out var v);
            return v;
        }

        //public IDisposable DefineTemporaty(string identifier, TypeSyntax type)
        //{
        //    _tempIdentifiers[identifier] = CodeType.From(type);
        //    return new DisposeTemporaryIdentifier(() => _tempIdentifiers.Remove(identifier));
        //}

        public CodeSymbol GetIdentifierType(string identifier)
        {
            //var v = _tempIdentifiers.GetValueOrDefault(identifier);
            //if (v.TypeSyntaxOrSymbol != null)
            //    return v;
            var v = _identifiers.GetValueOrDefault(identifier);
            if (v.Item1.TypeSyntaxOrSymbol != null)
                return v.Item1;
            if (Syntax is MemberDeclarationSyntax mmember)
            {
                var type = mmember.GetTypeIn(identifier);
                if (type != null)
                {
                    return CodeSymbol.From(type);
                }
            }
            if (Symbol is INamespaceOrTypeSymbol namespaceOrTypeSymbol)
            {
                var member = namespaceOrTypeSymbol.GetMembers(identifier, _global).ToList();
                if (member.Count() > 1)
                {
                    return CodeSymbol.From(new MemberSymbolOverload()
                    {
                        Overloads = member.ToList()
                    });
                }
                else
                {
                    return CodeSymbol.From(member.SingleOrDefault());
                }
            }
            return default;
        }

        public override string ToString()
        {
            return Symbol?.ToString() ?? Syntax.ToString();
        }

        public event EventHandler? OnClosing;
        public void Dispose()
        {
            OnClosing?.Invoke(this, EventArgs.Empty);
        }
    }
}
