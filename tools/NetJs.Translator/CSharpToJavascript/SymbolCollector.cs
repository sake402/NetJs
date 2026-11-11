using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using static NetJs.Translator.CSharpToJavascript.GlobalCompilationVisitor;
using static NetJs.Translator.CSharpToJavascript.SymbolCollector;

namespace NetJs.Translator.CSharpToJavascript
{
    public static class SymbolCollector
    {
        internal sealed class LocalFunctionFinder : CSharpSyntaxWalker
        {
            [ThreadStatic]
            private static LocalFunctionFinder _instance;
            private readonly List<LocalFunctionStatementSyntax> _nodes = new();

            public static List<LocalFunctionStatementSyntax> Find(SyntaxNode node)
            {
                var walker = _instance ??= new LocalFunctionFinder();
                walker._nodes.Clear();
                walker.Visit(node);
                return walker._nodes;
            }

            public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
            {
                _nodes.Add(node);
                base.VisitLocalFunctionStatement(node);
            }
        }


        static void GetAllInNamespaces(Compilation compilation, INamespaceSymbol @namespace, bool publicOnly, Func<ISymbol, bool> list, ConcurrentDictionary<SyntaxTree, SemanticModel> semanticModelCache)
        {
            if (@namespace.ContainingAssembly == null)
            {
                foreach (var n in @namespace.ConstituentNamespaces)
                    list(n);
            }
            else
                list(@namespace);

            foreach (var child in @namespace.GetNamespaceMembers())
            {
                GetAllInNamespaces(compilation, child, publicOnly, list, semanticModelCache);
            }

            foreach (var type in @namespace.GetTypeMembers())
            {
                GetMembersInType(compilation, type, publicOnly, list, semanticModelCache);
            }
        }

        static void GetMembersInType(Compilation compilation, ITypeSymbol type, bool publicOnly, Func<ISymbol, bool> list, ConcurrentDictionary<SyntaxTree, SemanticModel> semanticModelCache)
        {
            // Apply public-only filter immediately at the type tier if applicable
            if (publicOnly && type.DeclaredAccessibility != Accessibility.Public && type.DeclaredAccessibility != Accessibility.Protected)
            {
                return;
            }

            AddSymbolWithExtras(type, list);

            foreach (var nestedType in type.GetTypeMembers())
            {
                GetMembersInType(compilation, nestedType, publicOnly, list, semanticModelCache);
            }

            var members = type.GetMembers();
            for (int i = 0; i < members.Length; i++)
            {
                var child = members[i];

                if (publicOnly && child.DeclaredAccessibility != Accessibility.Public && child.DeclaredAccessibility != Accessibility.Protected)
                {
                    continue;
                }

                switch (child.Kind)
                {
                    case SymbolKind.Method:
                        var method = Unsafe.As<IMethodSymbol>(child);
                        AddSymbolWithExtras(method, list);

                        if (!publicOnly)
                        {
                            GetMethodsInMethod(compilation, method, list, semanticModelCache);
                        }
                        break;

                    case SymbolKind.Property:
                    case SymbolKind.Field:
                    case SymbolKind.Event:
                        AddSymbolWithExtras(child, list);
                        break;
                }
            }
        }

        static void GetMethodsInMethod(Compilation compilation, IMethodSymbol method, Func<ISymbol, bool> list, ConcurrentDictionary<SyntaxTree, SemanticModel> cache)
        {
            var decl = method.DeclaringSyntaxReferences;
            if (decl.Length == 0) return;

            for (int i = 0; i < decl.Length; i++)
            {
                var d = decl[i];
                var syntax = d.GetSyntax();
                var tree = syntax.SyntaxTree;

                var localFunctions = LocalFunctionFinder.Find(syntax);
                if (localFunctions.Count == 0) continue;

                if (!cache.TryGetValue(tree, out var semanticModel))
                {
                    semanticModel = compilation.GetSemanticModel(tree);
                    cache[tree] = semanticModel;
                }

                for (int j = 0; j < localFunctions.Count; j++)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(localFunctions[j]);
                    if (symbol != null)
                    {
                        AddSymbolWithExtras(symbol, list);
                    }
                }
            }
        }

        static void AddSymbolWithExtras(ISymbol symbol, Func<ISymbol, bool> list)
        {
            if (!list(symbol)) return;

            if (symbol.Kind == SymbolKind.Property)
            {
                var property = Unsafe.As<IPropertySymbol>(symbol);
                //_ = property.Type;
                if (property.GetMethod != null)
                {
                    list(property.GetMethod);
                    //_ = property.GetMethod.ReturnType;
                    //_ = property.GetMethod.Parameters;
                }
                if (property.SetMethod != null)
                {
                    list(property.SetMethod);
                    //_ = property.SetMethod.ReturnType;
                    //_ = property.SetMethod.Parameters;
                }
            }
            else if (symbol.Kind == SymbolKind.Event)
            {
                var _event = Unsafe.As<IEventSymbol>(symbol);
                //_ = _event.Type;
                if (_event.AddMethod != null)
                {
                    list(_event.AddMethod);
                    //_ = _event.AddMethod.ReturnType;
                    //_ = _event.AddMethod.Parameters;
                }
                if (_event.RemoveMethod != null)
                {
                    list(_event.RemoveMethod);
                    //_ = _event.RemoveMethod.ReturnType;
                    //_ = _event.RemoveMethod.Parameters;
                }
            }
            else if (symbol.Kind == SymbolKind.Field)
            {
                var field = Unsafe.As<IFieldSymbol>(symbol);
                //_ = field.Type;
            }
            else if (symbol.Kind == SymbolKind.Method)
            {
                var method = Unsafe.As<IMethodSymbol>(symbol);
                //_ = method.ReturnType;
                //_ = method.Parameters;
            }
            else if (symbol.Kind == SymbolKind.NamedType)
            {
                var t = Unsafe.As<ITypeSymbol>(symbol);
                var members = t.GetMembers();
                for (int i = 0; i < members.Length; i++)
                {
                    var m = members[i];
                    if (m.Kind == SymbolKind.Method && (m.Name == ".ctor" || m.Name == ".cctor"))
                    {
                        list(m);
                    }
                }
            }
        }

        public static void CollectSymbols(this Compilation compilation, Func<ISymbol, bool> processSymbol)
        {
            var semanticModelCache = new ConcurrentDictionary<SyntaxTree, SemanticModel>();

            //compilation.SourceModule
            //    .ReferencedAssemblySymbols
            //    //.AsParallel()
            //    .ForAll(a =>
            //    {
            //        var referenceVisitor = new SymbolCollectorVisitor(compilation, publicOnly: true, processSymbol, semanticModelCache);
            //        referenceVisitor.Visit(a.GlobalNamespace);
            //    });

            //compilation.GetSymbolsWithName(e => true, SymbolFilter.Namespace)
            //    //.AsParallel()
            //    .ForAll(s =>
            //    {
            //        if (s is INamespaceSymbol sourceNamespace)
            //        {
            //            var sourceVisitor = new SymbolCollectorVisitor(compilation, publicOnly: false, processSymbol, semanticModelCache);
            //            sourceVisitor.Visit(sourceNamespace);
            //        }
            //    });

            compilation.SourceModule
            .ReferencedAssemblySymbols
            //.AsParallel()
            .ForAll(a => GetAllInNamespaces(compilation, a.GlobalNamespace, true, processSymbol, semanticModelCache));

            compilation.GetSymbolsWithName(e => true, SymbolFilter.Namespace)
            //.AsParallel()
            .ForAll(a => GetAllInNamespaces(compilation, Unsafe.As<INamespaceSymbol>(a), false, processSymbol, semanticModelCache));
        }
    }

    public class SymbolCollectorVisitor : SymbolVisitor
    {
        private readonly bool _publicOnly;
        private readonly Func<ISymbol, bool> _processSymbol;
        private readonly ConcurrentDictionary<SyntaxTree, SemanticModel> _semanticModelCache;
        private readonly Compilation _compilation;

        public SymbolCollectorVisitor(
            Compilation compilation,
            bool publicOnly,
            Func<ISymbol, bool> processSymbol,
            ConcurrentDictionary<SyntaxTree, SemanticModel> semanticModelCache)
        {
            _compilation = compilation;
            _publicOnly = publicOnly;
            _processSymbol = processSymbol;
            _semanticModelCache = semanticModelCache;
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            if (symbol.ContainingAssembly == null)
            {
                foreach (var n in symbol.ConstituentNamespaces)
                {
                    _processSymbol(n);
                }
            }
            else
            {
                _processSymbol(symbol);
            }

            // Drill down into nested namespaces
            foreach (var childNamespace in symbol.GetNamespaceMembers())
            {
                childNamespace.Accept(this);
            }

            // Drill down into nested types
            foreach (var type in symbol.GetTypeMembers())
            {
                type.Accept(this);
            }
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            if (_publicOnly && !IsPublicOrProtected(symbol.DeclaredAccessibility))
            {
                return;
            }

            if (!_processSymbol(symbol)) return;

            // Collect instance/static constructors explicitly (.ctor and .cctor)
            foreach (var member in symbol.GetMembers())
            {
                if (member.Kind == SymbolKind.Method && (member.Name == ".ctor" || member.Name == ".cctor"))
                {
                    _processSymbol(member);
                }
            }

            // Drill down into nested types
            foreach (var nestedType in symbol.GetTypeMembers())
            {
                nestedType.Accept(this);
            }

            // Visit fields, properties, methods, events
            foreach (var member in symbol.GetMembers())
            {
                if (_publicOnly && !IsPublicOrProtected(member.DeclaredAccessibility))
                {
                    continue;
                }

                // Route execution using the compiler's native vtable dispatch
                member.Accept(this);
            }
        }

        public override void VisitMethod(IMethodSymbol symbol)
        {
            if (!_processSymbol(symbol)) return;

            if (!_publicOnly)
            {
                GetMethodsInMethod(symbol);
            }
        }

        public override void VisitProperty(IPropertySymbol symbol)
        {
            if (!_processSymbol(symbol)) return;

            if (symbol.GetMethod != null) _processSymbol(symbol.GetMethod);
            if (symbol.SetMethod != null) _processSymbol(symbol.SetMethod);
        }

        public override void VisitEvent(IEventSymbol symbol)
        {
            if (!_processSymbol(symbol)) return;

            if (symbol.AddMethod != null) _processSymbol(symbol.AddMethod);
            if (symbol.RemoveMethod != null) _processSymbol(symbol.RemoveMethod);
        }

        public override void VisitField(IFieldSymbol symbol)
        {
            _processSymbol(symbol);
        }

        private void GetMethodsInMethod(IMethodSymbol method)
        {
            var decl = method.DeclaringSyntaxReferences;
            if (decl.Length == 0) return;

            for (int i = 0; i < decl.Length; i++)
            {
                var syntax = decl[i].GetSyntax();
                var tree = syntax.SyntaxTree;

                var localFunctions = LocalFunctionFinder.Find(syntax);
                if (localFunctions.Count == 0) continue;

                var semanticModel = _semanticModelCache.GetOrAdd(tree, t => _compilation.GetSemanticModel(t));

                for (int j = 0; j < localFunctions.Count; j++)
                {
                    var localSymbol = semanticModel.GetDeclaredSymbol(localFunctions[j]);
                    if (localSymbol != null)
                    {
                        _processSymbol(localSymbol);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPublicOrProtected(Accessibility accessibility)
        {
            return accessibility == Accessibility.Public || accessibility == Accessibility.Protected;
        }
    }

}
