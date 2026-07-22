using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetJs;
using NetJs.Translator;
using NetJs.Translator.CSharpToJavascript;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Array;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Delegate;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Indexer;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Number;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Numbers;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.QCall;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Ref;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.StaticConvention;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.String;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.SystemIndex;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Tuples;
using NetJs.Translator.RazorToCSharp;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using YamlDotNet.Core;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor : CSharpSyntaxWalker
    {
        GlobalCompilationVisitor _global;
        SyntaxTree _tree;
        List<SemanticModel> _semanticModels = new List<SemanticModel>();
        public IEnumerable<SemanticModel> SemanticModels => _semanticModels;
        CodeSymbol memberAccesChainCurrentType;
        string? currentTypeNamespace;

        public GlobalCompilationVisitor Global => _global;
        public string? CurrentTypeNamespace => currentTypeNamespace;
        public Dictionary<INamedTypeSymbol, ScriptWriter> TypeWriters { get; private set; } = new Dictionary<INamedTypeSymbol, ScriptWriter>(SymbolEqualityComparer.Default);
        public Dictionary<string, object> States { get; private set; } = new();

        //Stack<ScriptWriter> writers = new Stack<ScriptWriter>();
        //ScriptWriter Writer => writers.Peek();
        public ScriptWriter CurrentTypeWriter { get; set; } = new ScriptWriter();


        static ISyntaxEmitter[] emitters =
        [
            new ImplicitConversionSyntaxEmitter(),
            new ImplicitTrueFalseOperatorSyntaxEmitter(),
            new RefTypeDereferenceOnAccessSyntaxEmitter(),
            new UnneccessaryUnsafeAddSyntaxEmitter(),

            new StaticConventionPropertySetterSyntaxEmitter(),

            new BoxPrimitiveAssignmentSyntaxEmitter(),

            new StringPlusNumberSyntaxEmitter(),
            new StringConstructorSyntaxEmitter(),
            new StringFirstCharSyntaxEmitter(),
            new RefToStringFirstCharSyntaxEmitter(),
            new RefArgumentToStringFirstCharSyntaxEmitter(),
            new AddressOfStringFirstCharSyntaxEmitter(),
            new MaterializeFastAllocatedStringOnReturnSyntaxEmitter(),
            new MaterializeFastAllocatedStringOnAssignmentSyntaxEmitter(),

            new ObjectCastToIntPtrSyntaxEmitter(),
            new IntPtrCastToObjectSyntaxEmitter(),

            new PointerCreateSyntaxEmitter(),
            new PointerArrayElementGetAccessSyntaxEmitter(),
            new PointerArrayElementSetAccessSyntaxEmitter(),
            new PointerDereferenceSyntaxEmitter(),
            new PointerPreIncrementDecrementSyntaxEmitter(),
            new PointerPostIncrementDecrementSyntaxEmitter(),
            new PointerAddSubtractIntegerToSelfSyntaxEmitter(),
            new PointerAddSubtractIntegerSyntaxEmitter(),
            new PointerSubtractPointerToIntegerSyntaxEmitter(),
            new PointerComparisionSyntaxEmitter(),
            new PointerMemberAccessSyntaxEmitter(),

            new CreateIndexSyntaxEmitter(),
            new ArrayRangeToSubArraySyntaxEmitter(),
            new ArrayForEachSyntaxEmitter(),
            new InlineArrayIndexingSyntaxEmitter(),
            new InlineArrayCastToSpanSyntaxEmitter(),

            new IndexerPostIncrementDecrementSyntaxEmitter(),
            new IndexerPreIncrementDecrementSyntaxEmitter(),
            new RangeToSliceMethodSyntaxEmitter(),
            new SystemIndexToSetElementSyntaxEmitter(),
            new SystemIndexToGetElementSyntaxEmitter(),
            new IndexerGetItemSyntaxEmitter(),
            new IndexerSetItemSyntaxEmitter(),
            new ThisAssignmentSyntaxEmitter(),
            new Utf8StringLiteralConcatSyntaxEmitter(),
            new Utf8StringLiteralToReadOnlySpanOfByteSyntaxEmitter(),
            new RecursiveOperatorSyntaxEmitter(),

            new NumericShiftSyntaxEmitter(),
            new NotOfBigIntShiftSyntaxEmitter(),

            new UnneccesaryNumericCastSyntaxEmitter(),
            new UnwrapRefOfPointerDereferenceSyntaxEmitter(),
            new UnwrapRefOfPointerDerefereceFromArgumentSyntaxEmitter(),
            new FixedVariableDeclarationSyntaxEmitter(),

            new TruncateIntegerDivisionSyntaxEmitter(),
            new WrapIntegerOperationsSyntaxEmitter(),
            new UnsignedNumberComparisonSyntaxEmitter(),

            new MethodDelegateCreateSyntaxEmitter(),
            new DelegateAddSyntaxEmitter(),
            new DelegateRemoveSyntaxEmitter(),

            new TupleEqualSyntaxEmitter(),
            new IsLiteralSyntaxEmitter(),

            new SkipCreateQCallSyntaxEmitter(),
            new SimpleObjectHandleOnStackCreateSyntaxEmitter(),

            new BindLocalFunctionIdentifierToAvailableThisSyntaxEmitter(),

            new PointerFromAddressOfStructCastToNumericSyntaxEmitter(),
            new RefOfSequentialStructFieldAsNumericSyntaxEmitter(),
            new InOfSequentialStructFieldAsNumericSyntaxEmitter(),

            new AsyncMethodWrapperSyntaxEmitter(),
            new PrimitiveMethodSyntaxEmitter(),
            new EnumMethodSyntaxEmitter(),
            new GenericTypeGetHashCodeSyntaxEmitter(),

            new StructCloneOnAssignmentSyntaxEmitter()
            //new SpanGetItemSyntaxEmitter(),
            //new SpanSetItemSyntaxEmitter(),
        ];

        // Maps a concrete concrete runtime Type to a flattened, pre-filtered list of matching emitters
        static readonly Dictionary<Type, List<ISyntaxEmitter>> typeToEmittersCache = new();

        public TranslatorSyntaxVisitor(GlobalCompilationVisitor global, SyntaxTree tree)
        {
            _global = global;
            _tree = tree;
            _semanticModels.Add(global.Compilation.GetSemanticModel(tree));
            //writers.Push(new ScriptWriter());
        }

        public void VisitNode(CodeNode? node)
        {
            if (node != null)
            {
                if (node.IsT0)
                    Visit(node.AsT0);
                else
                    node.AsT1();
            }
        }

        List<ISyntaxEmitter> ResolveEmittersForType(Type nodeType)
        {
            var matched = new List<ISyntaxEmitter>();

            // Runs exactly once per unique concrete Type encountered in the AST
            for (int i = 0; i < emitters.Length; i++)
            {
                var emitter = emitters[i];

                Type assignedType = emitter.SyntaxType;

                // This handles everything perfectly:
                // 1. Exact matches (nodeType == assignedType)
                // 2. Abstract/Base matches (assignedType is parent of nodeType)
                if (assignedType.IsAssignableFrom(nodeType))
                {
                    matched.Add(emitter);
                }
            }

            return matched;
        }

        public override void Visit(SyntaxNode? node)
        {
            if (node != null)
            {
                //if (node.ToString().StartsWith("val is not JsonConstants.Space and"))
                //{

                //}
                Type nodeType = node.GetType();

                List<ISyntaxEmitter> matchingEmitters;

                lock (typeToEmittersCache)
                {
                    // O(1) Lookup for subsequent nodes
                    if (!typeToEmittersCache.TryGetValue(nodeType, out matchingEmitters))
                    {
                        matchingEmitters = ResolveEmittersForType(nodeType);
                        typeToEmittersCache[nodeType] = matchingEmitters;
                    }
                }

                int count = matchingEmitters.Count;
                for (int i = 0; i < count; i++)
                {
                    if (matchingEmitters[i].TryEmit(node, this))
                    {
                        return;
                    }
                }

                //var nodeType = node.GetType();
                //for (int i = 0; i < s_Emitters.Length; i++)
                //{
                //    var emitter = s_Emitters[i];
                //    if (emitter.SyntaxType.IsAbstract && emitter.SyntaxType.IsAssignableFrom(nodeType))
                //    {
                //        if (emitter.TryEmit(node, this))
                //            return;
                //    }
                //    else if (nodeType == emitter.SyntaxType)
                //    {
                //        if (emitter.TryEmit(node, this))
                //            return;
                //    }
                //}
            }
            base.Visit(node);
        }

        //string CollectStatement(Action _continue)
        //{
        //    var sb = new ScriptWriter();
        //    writers.Push(sb);
        //    _continue();
        //    writers.Pop();
        //    return sb.ToString();
        //}

        //More oftern when we rewrite a node using SYntaxFactory and later we try to do _semanticModel.Get....,
        //We can't do that because the new node is detached not part of the current semantic model
        //This let us replace the said node and put it in a new SyntaxTree with its own dedicated semantic model
        //We have to create its own visitor as well with the new semantic model
        public void ReplaceAndVisit(CSharpSyntaxNode target, CSharpSyntaxNode newNode)
        {
            var mnewNode = (ExpressionStatementSyntax)target.Parent!.ReplaceNode(target, newNode)!;
            Visit(mnewNode.Expression);
            return;

            //var syntaxAnnotation = new SyntaxAnnotation("NewNodeTracker");
            //newNode = newNode.WithAdditionalAnnotations(syntaxAnnotation);
            //var rewriter = new SingleNodeReplacer(target, newNode);
            //var result = rewriter.Visit(_tree.GetRoot());
            //var replacedNode = result!.DescendantNodes().Where(n => n.HasAnnotation(syntaxAnnotation)).Single();
            //if (replacedNode.SyntaxTree != result.SyntaxTree) //did not replace
            //{

            //}
            //var newCompilationUnit = _global.Compilation.AddSyntaxTrees(result.SyntaxTree);
            //var newGlobal = _global with { Compilation = newCompilationUnit };
            //var newVisitor = new TranslatorSyntaxVisitor(newGlobal, result.SyntaxTree)
            //{
            //    CurrentTypeWriter = CurrentTypeWriter,
            //    TypeWriters = TypeWriters,
            //    alreadyTriedImport = alreadyTriedImport,
            //    Dependencies = Dependencies,
            //    closures = closures,
            //    currentExpressionNamespace = currentExpressionNamespace,
            //    currentTypeNamespace = currentTypeNamespace,
            //    importedNamespace = importedNamespace,
            //    imports = imports,

            //};
            //newVisitor.Visit(replacedNode);
        }

        public override void VisitCompilationUnit(CompilationUnitSyntax node)
        {
            if (node.ChildNodes().Any(e => e.IsKind(SyntaxKind.GlobalStatement)))
            {
                var mainEntry = _global.MainEntry ?? throw new InvalidOperationException("Expected a main entry for global statements");
                var typeSymbol = mainEntry.ContainingType;
                var classMetadata = _global.GetRequiredMetadata(typeSymbol);
                var methodMetadata = _global.GetRequiredMetadata(mainEntry);
                CurrentTypeWriter = new ScriptWriter();
                TypeWriters.Add(typeSymbol, CurrentTypeWriter);
                _global.TypeVisitors.Add(typeSymbol, this);
                _global.TypeWriters.Add(typeSymbol, CurrentTypeWriter);
                OpenClosure(node);
                CurrentTypeWriter.WriteLine(node, $"{Constants.AssemblyRegistryName}.{Constants.AssemblyDefineClassName}(\"{typeSymbol.CreateSignature(_global, withGlobalNamespace: false, withAssemblySlugNamespace: true)}\", ($self) => class {typeSymbol.Name}", true);
                CurrentTypeWriter.WriteLine(node, "{", true);
                CurrentTypeWriter.WriteLine(node, $"static{(mainEntry.IsAsync ? " async" : "")} {methodMetadata.OverloadName}(args)", true);
                CurrentTypeWriter.WriteLine(node, "{", true);
                base.VisitCompilationUnit(node);
                CurrentTypeWriter.WriteLine(node, "}", true);
                WriteTypeMetadata(node, typeSymbol);
                CurrentTypeWriter.WriteLine(node, "});", true);
                CloseClosure(node);
            }
            else
                base.VisitCompilationUnit(node);
        }

        public override void VisitGlobalStatement(GlobalStatementSyntax node)
        {
            base.VisitGlobalStatement(node);
        }

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.Alias != null)
            {
                var name = node.NamespaceOrType!.ToString().Trim();
                if (aliasNamespace.TryGetValue(node.Alias.Name.Identifier.ValueText, out var existingAlias))
                {
                    if (existingAlias == name)
                    {
                        return;
                    }
                }
                aliasNamespace.Add(node.Alias.Name.Identifier.ValueText, name);
            }
            else
            {
                var name = node.Name!.ToString().Trim();
                if (node.Parent is NamespaceDeclarationSyntax ns)
                {
                    if (!importedNamespace.Contains(name))
                        importedNamespace.Add(name);
                    name = ns.Name.ToString().Trim() + "." + name;
                }
                if (!importedNamespace.Contains(name))
                    importedNamespace.Add(name);
            }
            //we are exporting every module by theri namespace
            //var targetNamespace = global.AllNodes.Where(e => e is NamespaceDeclarationSyntax ns && global.ResolveFullNamespace(ns) == name);
            //var types = targetNamespace.SelectMany(c => c.ChildNodes().OfType<TypeDeclarationSyntax>());
            //Writer.WriteLine(node, $"import {{ {string.Join(",\r\n", types.Select(t => t.Identifier.ValueText))} }} from \"/{name}.js\"");
            //base.VisitUsingDirective(node);
        }

        public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            currentTypeNamespace = node.Name.ToString();
            VisitChildren(node.Members);
            //base.VisitFileScopedNamespaceDeclaration(node);
        }

        public override void VisitUsingStatement(UsingStatementSyntax node)
        {
            ITypeSymbol? expressionType = null;
            string? disposableVariableName = null;
            if (node.Declaration != null)
            {
                foreach (var variable in node.Declaration.Variables)
                {
                    CurrentTypeWriter.WriteLine(node, $"let {variable.Identifier.ResolveIdentifierName()} = null;", true);
                }
            }
            else if (node.Expression != null)
            {
                disposableVariableName = $"$disposable{++CurrentTypeWriter.CurrentClosure.NameManglingSeed}";
                CurrentTypeWriter.WriteLine(node, $"let {disposableVariableName} = null;", true);
                expressionType = _global.TryGetTypeSymbol(node.Expression, this);
            }
            CurrentTypeWriter.WriteLine(node, "try", true);
            WriteBlock(node.Statement, new CodeNode(() =>
            {
                if (node.Expression != null)
                {
                    CurrentTypeWriter.Write(node, $"{disposableVariableName} = ", true);
                    Visit(node.Expression);
                    CurrentTypeWriter.WriteLine(node, ";");
                }
                else if (node.Declaration != null)
                {
                    CurrentTypeWriter.Write(node, "", true);
                    VisitChildren(node.Declaration.Variables);
                    CurrentTypeWriter.WriteLine(node, ";");
                }
                if (node.Statement.IsKind(SyntaxKind.Block))
                    VisitChildren(node.Statement.ChildNodes());
                else
                    Visit(node.Statement);
            }));
            CurrentTypeWriter.WriteLine(node, "finally", true);
            WriteBlock(node, new CodeNode(() =>
            {
                if (node.Expression != null)
                {
                    if (expressionType != null && expressionType.AllInterfaces.Any(it => SymbolEqualityComparer.Default.Equals(it, _global.SystemIDisposable)))
                    {
                        WriteMethodInvocation(node, "System.IDisposable.Dispose", lhsExpression: new CodeNode(() =>
                        {
                            CurrentTypeWriter.Write(node, $"{disposableVariableName}?", true);
                        }));
                        CurrentTypeWriter.WriteLine(node, ";");
                    }
                    else if (expressionType != null && expressionType.AllInterfaces.Any(it => SymbolEqualityComparer.Default.Equals(it, _global.SystemIAsyncDisposable)))
                    {
                        CurrentTypeWriter.WriteLine(node, $"if ({disposableVariableName} !== null)", true);
                        CurrentTypeWriter.WriteLine(node, "{", true);
                        CurrentTypeWriter.Write(node, "await ", true);
                        WriteMethodInvocation(node, "System.IAsyncDisposable.DisposeAsync", lhsExpression: new CodeNode(() =>
                        {
                            CurrentTypeWriter.Write(node, $"{disposableVariableName}");
                        }));
                        CurrentTypeWriter.WriteLine(node, ";");
                        CurrentTypeWriter.WriteLine(node, "}", true);
                    }
                    else
                    {
                        CurrentTypeWriter.WriteLine(node, $"{disposableVariableName}?.Dispose();", true);
                    }
                }
                else if (node.Declaration != null)
                {
                    foreach (var variable in node.Declaration.Variables)
                    {
                        var declarationType = _global.TryGetTypeSymbol(variable, this);
                        if (declarationType != null && declarationType.AllInterfaces.Any(it => SymbolEqualityComparer.Default.Equals(it, _global.SystemIDisposable)))
                        {
                            //Writer.WriteLine(node, $"{variable.Identifier.ValueText}?.System$IDisposable$Dispose();", true);
                            WriteMethodInvocation(node, "System.IDisposable.Dispose", lhsExpression: new CodeNode(() =>
                            {
                                CurrentTypeWriter.Write(node, $"{variable.Identifier.ResolveIdentifierName()}?", true);
                            }));
                            CurrentTypeWriter.WriteLine(node, ";");
                        }
                        else
                        {
                            CurrentTypeWriter.WriteLine(node, $"{variable.Identifier.ResolveIdentifierName()}?.Dispose()", true);
                        }
                    }
                }
            }));
        }

        public void VisitChildren(IEnumerable<SyntaxNode> nodes, string? separator = null)
        {
            int ix = 0;
            foreach (var node in nodes)
            {
                if (separator != null && ix > 0)
                    CurrentTypeWriter.Write(node, separator, false);
                Visit(node);
                ix++;
            }
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var addedName = node.Name!.ToString().Trim();
            if (addedName?.Length > 0 && currentTypeNamespace?.Length > 0)
            {
                addedName = "." + addedName;
            }
            currentTypeNamespace += addedName;
            //VisitChildren(node.ChildNodes().Where(e => e is not QualifiedNameSyntax && e is not IdentifierNameSyntax));
            VisitChildren(node.ChildNodes().Where(e => !e.IsKind(SyntaxKind.QualifiedName) && !e.IsKind(SyntaxKind.IdentifierName)));
            //base.VisitNamespaceDeclaration(node);
            currentTypeNamespace = currentTypeNamespace.Substring(0, currentTypeNamespace.Length - (addedName?.Length ?? 0));
        }

        public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            base.VisitAccessorDeclaration(node);
        }

        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            CurrentTypeWriter.Write(node, "", true);
            base.VisitExpressionStatement(node);
            CurrentTypeWriter.WriteLine(node, ";");
        }

        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.Token.Text.EndsWith("m"))
            {
                if (TryWriteConstant(node, (ITypeSymbol)_global.GetSymbol("System.Decimal", this/*, out _, out _*/), node))
                    return;
            }
            else if (node.Token.Text.EndsWith("UL"))
            {
                if (TryWriteConstant(node, (ITypeSymbol)_global.GetSymbol("System.UInt64", this/*, out _, out _*/), node))
                    return;
            }
            else if (node.Token.Text.EndsWith("L"))
            {
                if (TryWriteConstant(node, (ITypeSymbol)_global.GetSymbol("System.Int64", this/*, out _, out _*/), node))
                    return;
            }
            else if (node.IsKind(SyntaxKind.DefaultLiteralExpression))
            {
                var type = GetExpressionReturnSymbol(node);
                if (type.TypeSyntaxOrSymbol is ITypeSymbol typeSymbol)
                {
                    var defaultValue = _global.GetDefaultValue(typeSymbol, true);
                    CurrentTypeWriter.Write(node, defaultValue ?? "null");
                    return;
                }
            }
            var txt = node.GetLiteralString(_global);
            CurrentTypeWriter.Write(node, txt);
            base.VisitLiteralExpression(node);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            if (TryInvokeMethodOperator(node, node.OperatorToken.ValueText, null, node.Operand, null, [node.Operand]))
                return;
            CurrentTypeWriter.Write(node, $"{node.OperatorToken.ValueText}");
            Visit(node.Operand);
            //DereferenceIfReference(node.Operand);
            //base.VisitPrefixUnaryExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            if (!node.IsKind(SyntaxKind.SuppressNullableWarningExpression))
                if (TryInvokeMethodOperator(node, node.OperatorToken.ValueText, null, node.Operand, null, [node.Operand]))
                    return;
            Visit(node.Operand);
            //DereferenceIfReference(node.Operand);
            if (!node.IsKind(SyntaxKind.SuppressNullableWarningExpression))//remove shebang after null and default
                CurrentTypeWriter.Write(node, $"{node.OperatorToken.ValueText}");
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (TryWriteMathBinaryExpression(node, node.OperatorToken.ValueText, node.Left, node.Right))
                return;
            if (TryInvokeMethodOperator(node, node.OperatorToken.ValueText, null, node.Left, null, [node.Left, node.Right]))
                return;
            var op = node.OperatorToken.ValueText.Trim();
            var rightSymbol = _global.GetSymbol(node.Right, this);
            if (op == "??" && node.Right.IsKind(SyntaxKind.ThrowExpression)/* is ThrowExpressionSyntax*/)
            {
                CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.FirstOf}(");
                Visit(node.Left);
                CurrentTypeWriter.Write(node, ", () => { ");
                Visit(node.Right);
                CurrentTypeWriter.Write(node, " })");
            }
            else if (op == "as")
            {
                CurrentTypeWriter.Write(node, $"{_global.GlobalName}.$as(");
                Visit(node.Left);
                CurrentTypeWriter.Write(node, ", ");
                Visit(node.Right);
                CurrentTypeWriter.Write(node, ")");
            }
            else if (op == "is" && node.Right is TypeSyntax && rightSymbol.Kind != SymbolKind.Field/*resolve ambiguity on maybeColor is Color.Blue when Color.Bule is an enum*/)
            {
                var type = _global.GetTypeSymbol(rightSymbol);
                CurrentTypeWriter.Write(node, $"{_global.GlobalName}.$is(");
                Visit(node.Left);
                CurrentTypeWriter.Write(node, ", ");
                //Visit(node.Right);
                CurrentTypeWriter.Write(node, type.ComputeOutputTypeName(_global));
                CurrentTypeWriter.Write(node, ")");
            }
            else
            {
                //var leftType = _global.TryGetTypeSymbol(node.Left, this)?.GetTypeSymbol();
                //var rightType = _global.TryGetTypeSymbol(node.Right, this)?.GetTypeSymbol();
                //if (leftType != null && rightType != null)
                //{
                //    if (leftType.Equals(_global.SystemBoolean, SymbolEqualityComparer.Default) && rightType.Equals(_global.SystemBoolean, SymbolEqualityComparer.Default))
                //    {
                //        //Rewrite boolean logical & to &&, | to || and ^ to != as js interpret this differently from c# 
                //        //if (op == "&")
                //        //    op = "&&";
                //        //else if (op == "|")
                //        //    op = "||";
                //        //else 
                //        if (op == "^")
                //            op = "!==";
                //    }
                //}
                //Writer.Write(node, $"(");
                Visit(node.Left);
                //bool KeepOperator()
                //{
                //    //If the left is a bitwise or, js will not return a bool but an int 1 or 0, we should keep the == in this scenario
                //    bool keep = false;
                //    if (leftType != null && rightType != null && leftType.Equals(_global.SystemBoolean, SymbolEqualityComparer.Default) && rightType.Equals(_global.SystemBoolean, SymbolEqualityComparer.Default))
                //    {
                //        keep = true;
                //    }
                //    if ((node.Left is BinaryExpressionSyntax be && (be.OperatorToken.ValueText == "&" || be.OperatorToken.ValueText == "|"))
                //        ||
                //        (node.Left is ParenthesizedExpressionSyntax pe && pe.Expression is BinaryExpressionSyntax be2 && (be2.OperatorToken.ValueText == "&" || be2.OperatorToken.ValueText == "|")))
                //    {
                //        keep = true;
                //    }
                //    return keep;
                //}
                if (op == "is")
                {
                    if (node.Right is LiteralExpressionSyntax || _global.GetSymbol(node.Right, this).Kind == SymbolKind.Field/*resolve ambiguity on maybeColor is Color.Blue when Color.Bule is an enum*/)
                    {
                        op = "===";
                    }
                    else
                    {
                        op = "instanceof";
                    }
                }
                else if (op == "==")
                {

                    //Left type of a == operator may be a bool & bool
                    //if (!KeepOperator())
                    op = "===";
                }
                else if (op == "!=")
                {
                    //if (!KeepOperator())
                    op = "!==";
                }
                CurrentTypeWriter.Write(node, $" {op} ");
                Visit(node.Right);
                //Writer.Write(node, $")");
            }
            //base.VisitBinaryExpression(node);
        }

        public override void VisitAwaitExpression(AwaitExpressionSyntax node)
        {
            CurrentTypeWriter.Write(node, $"await ");
            Visit(node.Expression);
            //WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.TaskToPromise", arguments: [node.Expression]);
            //base.VisitAwaitExpression(node);
        }

        public override void VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {
            CurrentTypeWriter.Write(node, "(");
            base.VisitParenthesizedExpression(node);
            CurrentTypeWriter.TrimEnd();
            CurrentTypeWriter.Write(node, ")");
        }

        public void WriteBlock(CSharpSyntaxNode node, CodeNode code)
        {
            CurrentTypeWriter.WriteLine(node, "{", true);
            var blockClosure = CurrentTypeWriter.CurrentClosure;
            OpenClosure(node);
            VisitNode(code);
            CloseClosure(node);
            blockClosure.RaiseOnBlockClosing();
            CurrentTypeWriter.WriteLine(node, "}", true);
        }

        List<BlockSyntax> _blockBraceWritten = new();
        void MarkBlockBraceWritten(BlockSyntax block)
        {
            _blockBraceWritten.Add(block);
        }
        public override void VisitBlock(BlockSyntax node)
        {
            if (_blockBraceWritten.Contains(node))
            {
                _blockBraceWritten.Remove(node);
                if (!BlockTryHandleJumpLabels(node))
                    base.VisitBlock(node);
            }
            else
            {
                WriteBlock(node, new CodeNode(() =>
                {
                    if (!BlockTryHandleJumpLabels(node))
                        base.VisitBlock(node);
                }));
            }
        }

        //public override void VisitDeclarationPattern(DeclarationPatternSyntax node)
        //{
        //    base.VisitDeclarationPattern(node);
        //    if (node.Designation != null)
        //    {
        //        Writer.Write($", ");
        //        Visit(node.Designation);
        //    }
        //}

        public IParameterSymbol? GetParameterSymbol(ArgumentSyntax argumentSyntax)
        {
            IParameterSymbol? parameterSymbol = null;
            if (!argumentSyntax.Expression.IsKind(SyntaxKind.ThisExpression))
            {
                parameterSymbol = _global.TryGetSymbol(argumentSyntax, this) as IParameterSymbol;
                if (parameterSymbol != null)
                    return parameterSymbol;
            }
            var invocation = argumentSyntax.FirstAncestorOrSelf<InvocationExpressionSyntax>();

            if (invocation != null)
            {
                var methodSymbol = _global.TryGetSymbol(invocation, this) as IMethodSymbol;

                if (methodSymbol != null)
                {
                    if (argumentSyntax.NameColon != null)
                    {
                        string name = argumentSyntax.NameColon.Name.Identifier.ValueText;

                        parameterSymbol = methodSymbol.Parameters
                            .FirstOrDefault(p => p.Name == name);
                    }
                    else
                    {
                        int argumentIndex = invocation.ArgumentList.Arguments.IndexOf(argumentSyntax);

                        if (argumentIndex >= 0 && argumentIndex < methodSymbol.Parameters.Length)
                        {
                            parameterSymbol = methodSymbol.Parameters[argumentIndex];
                        }
                    }
                }
            }
            return parameterSymbol;
        }

        public override void VisitArgument(ArgumentSyntax node)
        {
            var parameter = GetParameterSymbol(node);
            if (node.RefKindKeyword.IsKind(SyntaxKind.OutKeyword) ||
                node.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) ||
                node.RefKindKeyword.IsKind(SyntaxKind.InKeyword) ||
                parameter?.RefKind == RefKind.In)
            {
                if (node.Expression.IsKind(SyntaxKind.DeclarationExpression) && ((DeclarationExpressionSyntax)node.Expression).Designation.IsKind(SyntaxKind.DiscardDesignation))
                {
                    CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.DiscardRefName}()");
                }
                else if (node.Expression.IsKind(SyntaxKind.IdentifierName) && ((IdentifierNameSyntax)node.Expression).Identifier.ValueText == "_")
                {
                    CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.DiscardRefName}()");
                }
                else
                {
                    var iNameMangling = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                    var arg = _global.TryGetSymbol(node.Expression, this/*, out var rhs, out var rhsKind*/);
                    var argKind = arg?.Kind;
                    var argRefKind = arg?.GetRefKind();
                    if (argRefKind != null && argRefKind != RefKind.None) //the referenced field is already a ref itself. No need to create a new ref
                    {
                        if (!node.Expression.IsKind(SyntaxKind.ThisExpression))
                        {
                            Visit(node.Expression);
                            return;
                        }
                    }
                    var expression = node.Expression;
                    bool useSimpleRef = false;
                    var refType = arg != null ? _global.GetTypeSymbol(arg) : null;
                    if (!expression.IsKind(SyntaxKind.ElementAccessExpression)) //element access must use full array ref system, else indexing fails
                    {
                        if (parameter?.RefKind == RefKind.In)
                        {
                            useSimpleRef = true;
                        }
                        else if (refType == null ||
                                refType.IsNumericType() ||
                                refType.IsLongNumericType() ||
                                !refType.IsValueType ||
                                node.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
                        {
                            useSimpleRef = true;
                        }
                    }
                    IDisposable? dispose1 = null;
                    IDisposable? dispose2 = null;
                    if (expression is DeclarationExpressionSyntax decl && decl.Designation is SingleVariableDesignationSyntax svd)
                    {
                        if (GotoHasDefinedVariable(decl))
                        {

                        }
                        else
                        {
                            CurrentTypeWriter.InsertAbove(node, $"/*{decl.Type}*/ let {svd.Identifier.ResolveIdentifierName()};", true);
                        }
                        dispose1 = CurrentTypeWriter.SetReplacement("let ", "");
                        dispose2 = CurrentTypeWriter.SetReplacement($"/*{decl.Type}*/ ", "");
                        //if we are going to use simple ref, we need to make sure the variable is initialized to its default using Unsafe.SkipInit, as the runtime cannot do it for us in that mode
                        //if (useSimpleRef && refType != null && argKind == SymbolKind.Local)
                        //{
                        //    CurrentTypeWriter.InsertAbove(node, () =>
                        //    {
                        //        WriteMethodInvocation(node, "System.Runtime.CompilerServices.Unsafe.SkipInit", methodGenericTypes: [refType], arguments: [new CodeNode(() => {
                        //            WriteCreateSimpleRef(node, expression);
                        //        })]);
                        //        CurrentTypeWriter.Write(node, ";");
                        //    }, true);
                        //}
                    }
                    if (useSimpleRef)
                        WriteCreateSimpleRef(node, expression, refType, _readOnly: node.RefKindKeyword.IsKind(SyntaxKind.InKeyword) || parameter?.RefKind == RefKind.In);
                    else
                        WriteCreateRef(node, expression, refType); //We could have use simpleref across board, but we wanto to have the opportunity to ref the backing fields of a struct(especially pure ones), in case wee need to lay it over another type via casting
                    dispose1?.Dispose();
                    dispose2?.Dispose();
                    return;
                    //string? boundIdentifierName = null;
                    //string? bindToThis = null;
                    //if (node.Expression is DeclarationExpressionSyntax dec && dec.Designation is SingleVariableDesignationSyntax sv)
                    //{
                    //    var boundLocalField = _global.TryGetTypeSymbol(sv, this/*, out _, out _*/);
                    //    boundIdentifierName = sv.Identifier.ValueText;
                    //    CurrentTypeWriter.InsertInCurrentClosure(node, $"let {boundIdentifierName} = null;", true);
                    //    if (boundLocalField != null)
                    //    {
                    //        CurrentClosure.DefineIdentifierType(boundIdentifierName, CodeSymbol.From(boundLocalField));
                    //    }
                    //    else if (node.RefKindKeyword.ValueText == "out" && !dec.Type.IsVar)
                    //    {
                    //        CurrentClosure.DefineIdentifierType(boundIdentifierName, CodeSymbol.From(dec.Type, SymbolKind.Local));
                    //    }
                    //}
                    //else if (node.Expression is IdentifierNameSyntax id)
                    //{
                    //    if (rhsKind == SymbolKind.Field || rhsKind == SymbolKind.Local || rhsKind == SymbolKind.Parameter)
                    //    {
                    //        if (rhsKind == SymbolKind.Field)
                    //        {
                    //            //While we could be cheking if the accessed field is static.
                    //            //The "this" in the static method is most likely the prototype of the class itself though
                    //            //So we expect it to work
                    //            if (!rhs!.IsStatic)
                    //                bindToThis = $"$this{iNameMangling}";
                    //            var metadata = _global.GetRequiredMetadata(rhs!);
                    //            boundIdentifierName = metadata.InvocationName ?? rhs!.Name;
                    //            if (!rhs.IsStatic)
                    //            {
                    //                CurrentTypeWriter.InsertInCurrentClosure(node, $"const {bindToThis} = this;", true);
                    //                bindToThis += ".";
                    //            }
                    //        }
                    //        else
                    //        {
                    //            boundIdentifierName = rhs!.Name;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        boundIdentifierName = id.Identifier.ValueText;
                    //    }
                    //}
                    //else if (node.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    //{
                    //    var identifierName = $"${node.RefKindKeyword.ValueText}_{node.Expression.ToString().Replace(" ", "_").Replace(".", "_").Replace("[", "_").Replace("]", "_").Replace("!", "_").Replace("(", "_").Replace(")", "_")}{iNameMangling}";
                    //    CurrentTypeWriter.InsertAbove(node, () =>
                    //    {
                    //        var argType = _global.GetTypeSymbol(node.Expression, this).GetTypeSymbol();
                    //        //var _thisCache = $"const $this{iNameMangling} = this;";
                    //        //var line = CurrentTypeWriter.WriteLine(node, _thisCache, true);
                    //        CurrentTypeWriter.Write(node, $"const {identifierName} = ", true);
                    //        //var replaceThis = CurrentTypeWriter.SetReplacement("this", $"$this{iNameMangling}");
                    //        WriteCreateRef(node, node.Expression, argType);
                    //        //if (replaceThis.Hit == 0) //no this replacement was made, remove the redundant this assignment
                    //        //{
                    //        //    line.Remove(_thisCache);
                    //        //}
                    //        //replaceThis.Dispose();
                    //        CurrentTypeWriter.Write(node, $";");
                    //    }, true);
                    //    CurrentTypeWriter.Write(node, identifierName);
                    //    return;
                    //}
                    //else if (node.Expression.IsKind(SyntaxKind.FieldExpression))
                    //{
                    //    var containigType = node.FindClosestParent<BaseTypeDeclarationSyntax>() ?? throw new InvalidOperationException("field must be inside a property");
                    //    var typeSymbol = _global.GetTypeSymbol(containigType, this);
                    //    var typeMetadata = _global.GetRequiredMetadata(typeSymbol);
                    //    var containigProperty = node.FindClosestParent<PropertyDeclarationSyntax>() ?? throw new InvalidOperationException("field must be inside a property");
                    //    var propertyName = containigProperty.Identifier.ValueText;
                    //    bool isStatic = containigProperty.Modifiers.IsStatic();
                    //    boundIdentifierName = $"{(!isStatic ? "this" : typeMetadata.InvocationName ?? typeSymbol.Name)}.{propertyName}$";
                    //}
                    //else
                    //{
                    //    Visit(node.Expression);
                    //    return;
                    //}
                    //var fieldName = $"{bindToThis}{boundIdentifierName}";
                    //if (boundIdentifierName == "_")//discard
                    //{
                    //    CurrentTypeWriter.Write(node, $"$.{Constants.DiscardRefName}");
                    //}
                    //else
                    //{
                    //    var argType = _global.GetTypeSymbol(node, this).GetTypeSymbol();
                    //    var simpleBoundIdentifierName = boundIdentifierName.Split('.').Last();
                    //    var ix = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                    //    WriteCreateRef(node, argType, fieldName, $"/*{node.RefKindKeyword.ValueText} {boundIdentifierName}*/ const ${node.RefKindKeyword.ValueText}_{simpleBoundIdentifierName}{ix} = ", ";", _readOnly: node.RefKindKeyword.ValueText == "in");
                    //    CurrentTypeWriter.Write(node, $"${node.RefKindKeyword.ValueText}_{simpleBoundIdentifierName}{ix}");
                    //}
                    ////Writer.InsertInCurrentClosure($"/*{node.RefKindKeyword.ValueText} {boundIdentifierName}*/ const ${node.RefKindKeyword.ValueText}{iNameMangling} = {{ get value(){{ return {bindToThis}{boundIdentifierName}; }}, set value(v){{ {bindToThis}{boundIdentifierName} = v; }} }};", true);
                }
            }
            //else if (node.RefKindKeyword.ValueText == "in")
            //{
            //}
            else
            {
                //skip namecolon
                //base.VisitArgument(node);
                Visit(node.Expression);
            }
        }

        public override void VisitParenthesizedVariableDesignation(ParenthesizedVariableDesignationSyntax node)
        {
            CurrentTypeWriter.Write(node, " [ ");
            int i = 0;
            foreach (var v in node.Variables)
            {
                if (i > 0)
                    CurrentTypeWriter.Write(node, ", ");
                Visit(v);
                i++;
            }
            CurrentTypeWriter.Write(node, " ]");
            //base.VisitParenthesizedVariableDesignation(node);
        }

        public override void VisitBaseExpression(BaseExpressionSyntax node)
        {
            //if (node.Parent is InvocationExpressionSyntax)//dont insert super keyword into method calls. Can only be used as a dispatch prefix
            //    Writer.Write(node, "this");
            //else
            if (node.FindClosestParent<LocalFunctionStatementSyntax>() != null) //cant access super in a local function
            {
                CurrentTypeWriter.Write(node, "this.");
                CurrentTypeWriter.Write(node, Constants.SuperClassAccessName);
            }
            else
            {
                CurrentTypeWriter.Write(node, "super");
            }
            base.VisitBaseExpression(node);
        }

        public override void VisitNameColon(NameColonSyntax node)
        {
            throw new InvalidOperationException("Should not get here. Javascript doesnt do named colon");
            //base.VisitNameColon(node);
            //Writer.Write(node, $" {node.ColonToken.ValueText} ");
        }

        public override void VisitInitializerExpression(InitializerExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.ArrayInitializerExpression))
            {
                var arrayType = (IArrayTypeSymbol?)_global.TryGetTypeSymbol(node, this);
                if (arrayType != null)
                {
                    bool isMultiDimensionalArray = arrayType.Rank > 1;
                    WriteCreateArray(node, arrayType.ElementType, lengths: arrayType.Rank > 1 ? new CodeNode(() =>
                    {
                        var lenghts = new int[arrayType.Rank];
                        var exp = node;
                        int i = 0;
                        while (exp != null)
                        {
                            lenghts[i++] = exp.Expressions.Count;
                            exp = exp.Expressions[0] as InitializerExpressionSyntax;
                        }
                        int ix = 0;
                        CurrentTypeWriter.Write(node, "[");
                        foreach (var l in lenghts)
                        {
                            if (ix > 0)
                                CurrentTypeWriter.Write(node, ", ");
                            CurrentTypeWriter.Write(node, l.ToString());
                            ix++;
                        }
                        CurrentTypeWriter.Write(node, "]");
                    }) : null, bounds: null, values: new CodeNode(() =>
                    {
                        CurrentTypeWriter.Write(node, "[");
                        int ix = 0;
                        foreach (var i in node.Expressions)
                        {
                            if (!isMultiDimensionalArray)
                            {
                                if (ix > 0)
                                    CurrentTypeWriter.Write(node, ", ");
                            }
                            Visit(i);
                            ix++;
                        }
                        CurrentTypeWriter.Write(node, "]");
                    }));
                }
                else
                {
                    bool isMultiDimensionalArray = node.Parent.IsKind(SyntaxKind.ArrayInitializerExpression);
                    if (!isMultiDimensionalArray) //multidimensional array?, flatten the inner arrays
                        CurrentTypeWriter.Write(node, "[");
                    int i = 0;
                    if (isMultiDimensionalArray)
                    {
                        if (States.TryGetValue(nameof(SyntaxKind.ArrayInitializerExpression), out var v))
                        {
                            i = (int)v;
                        }
                    }
                    foreach (var n in node.Expressions)
                    {
                        if (i > 0)
                            CurrentTypeWriter.Write(node, ", ");
                        Visit(n);
                        i++;
                    }
                    if (!isMultiDimensionalArray)
                        CurrentTypeWriter.Write(node, "]");
                    if (isMultiDimensionalArray)
                    {
                        States[nameof(SyntaxKind.ArrayInitializerExpression)] = i;
                    }
                }
            }
            else
            {
                int i = 0;
                foreach (var n in node.Expressions)
                {
                    if (i > 0)
                        CurrentTypeWriter.Write(node, ", ");
                    Visit(n);
                    i++;
                }
            }
            //base.VisitInitializerExpression(node);
        }

        public override void VisitThrowExpression(ThrowExpressionSyntax node)
        {
            CurrentTypeWriter.Write(node, $"throw ");
            base.VisitThrowExpression(node);
            if (node.Expression == null) //we must have being inside a catch if throw has no expression
            {
                var _catch = node.FindClosestParent<CatchClauseSyntax>();
                CurrentTypeWriter.Write(node, !string.IsNullOrEmpty(_catch?.Declaration?.Identifier.ValueText) ? _catch!.Declaration!.Identifier.ValueText : "$e");
            }
        }


        public override void VisitThrowStatement(ThrowStatementSyntax node)
        {
            CurrentTypeWriter.Write(node, $"throw ", true);
            base.VisitThrowStatement(node);
            if (node.Expression == null) //we must have being inside a catch if throw has no expression
            {
                var _catch = node.FindClosestParent<CatchClauseSyntax>();
                CurrentTypeWriter.Write(node, !string.IsNullOrEmpty(_catch?.Declaration?.Identifier.ValueText) ? _catch!.Declaration!.Identifier.ValueText : "$e");
            }
            CurrentTypeWriter.WriteLine(node, $";");
        }

        public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            //var type = _global.ResolveSymbol(GetExpressionReturnSymbol(node.Expression), this/*, out _, out _*/)?.GetTypeSymbol();
            //if (type != null)
            //{
            //    var propertyIndexers = type.GetMembers("get_Item", _global).Where(e => e is IMethodSymbol p && p.Parameters.Count() == node.ArgumentList.Arguments.Count).Cast<IMethodSymbol>().ToList();
            //    //var propertyIndexers = nt.GetMembers("get_Item", _global).Where(e => e is IPropertySymbol p && p.IsIndexer && p.Parameters.Count() == node.ArgumentList.Arguments.Count && p.GetMethod != null).Cast<IPropertySymbol>().ToList();
            //    var bestIndexer = GetBestOverloadMethod(type, propertyIndexers, null, node.ArgumentList.Arguments, null, out _);
            //    if (bestIndexer != null)
            //    {
            //        bool isExtern = bestIndexer.IsExtern || _global.HasAttribute(bestIndexer, typeof(ExternalAttribute).FullName!, this, false, out _) ||
            //             (bestIndexer.AssociatedSymbol?.IsExtern ?? false) || (bestIndexer.AssociatedSymbol != null && _global.HasAttribute(bestIndexer.AssociatedSymbol, typeof(ExternalAttribute).FullName!, this, false, out _));
            //        bool hasTemplate = bestIndexer.GetTemplateAttribute(_global) != null;
            //        if (!isExtern || hasTemplate)
            //        {
            //            if (WriteMethodInvocation(node, bestIndexer, null, null, node.ArgumentList.Arguments, node.Expression, type, false))
            //                return;
            //        }
            //    }
            //}
            Visit(node.Expression);
            CurrentTypeWriter.Write(node, "[");
            foreach (var a in node.ArgumentList.Arguments)
            {
                Visit(a);
            }
            CurrentTypeWriter.Write(node, "]");
            //base.VisitElementAccessExpression(node);
        }

        public override void VisitThisExpression(ThisExpressionSyntax node)
        {
            CurrentTypeWriter.Write(node, "this");
            //base.VisitThisExpression(node);
        }

        public override void VisitDefaultExpression(DefaultExpressionSyntax node)
        {
            EnsureImported(node.Type);
            //if (node.Type != null)
            //{
            var defaultValue = _global.GetDefaultValue(node.Type, this);
            if (defaultValue != null)
            {
                CurrentTypeWriter.Write(node, defaultValue);
            }
            else
            {
                var typeSymbol = _global.TryGetSymbol(node.Type, this) as INamedTypeSymbol;
                CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.DefaultTypeName}(");
                if (typeSymbol != null)
                {
                    CurrentTypeWriter.Write(node, typeSymbol.ComputeOutputTypeName(_global));
                }
                else
                {
                    Visit(node.Type);
                }
                CurrentTypeWriter.Write(node, $")");
            }
            //}
            //else
            //{
            //    Writer.Write(node, "null");
            //}
            //base.VisitDefaultExpression(node);
        }

        public override void VisitCheckedExpression(CheckedExpressionSyntax node)
        {
            var dispose = DefinePragma(node.Keyword.ValueText);
            base.VisitCheckedExpression(node);
            dispose.Dispose();
        }

        public override void VisitCheckedStatement(CheckedStatementSyntax node)
        {
            CurrentTypeWriter.WriteLine(node, $"//{node.Keyword.ValueText}", true);
            var dispose = DefinePragma(node.Keyword.ValueText);
            base.VisitCheckedStatement(node);
            dispose.Dispose();
        }

        public override void VisitSizeOfExpression(SizeOfExpressionSyntax node)
        {
            var type = _global.GetTypeSymbol(node.Type, this);
            if (type.Kind == SymbolKind.TypeParameter)
                CurrentTypeWriter.Write(node, $"(");
            //Visit(node.Type);
            CurrentTypeWriter.Write(node, type.ComputeOutputTypeName(_global));
            CurrentTypeWriter.Write(node, $".");
            CurrentTypeWriter.Write(node, Constants.PrototypeStructSize);
            if (type.Kind == SymbolKind.TypeParameter)
                CurrentTypeWriter.Write(node, $"??4)");//ref type doesnt have their size exported
            //CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.SizeOf}(");
            //Visit(node.Type);
            //CurrentTypeWriter.Write(node, $")");
            //base.VisitSizeOfExpression(node);
        }

        bool IsRewiteCandidate(ConditionalAccessExpressionSyntax node)
        {
            //if (node.WhenNotNull.IsKind(SyntaxKind.ConditionalAccessExpression))
            //    return true;
            //var rhsSymbol = _global.GetSymbol(node.WhenNotNull, this);
            //if (rhsSymbol is IMethodSymbol m && (m.IsExtensionMethod /*|| m.IsStaticCallConvention(_global)*/))
            //{
            //    //We only rewite for extension method
            //    return true;
            //}
            //return false;
            if (node.WhenNotNull.IsKind(SyntaxKind.ConditionalAccessExpression))
                return true;
            var rhsExpression = node.WhenNotNull;
            if (rhsExpression.IsKind(SyntaxKind.ElementAccessExpression) && rhsExpression is ElementAccessExpressionSyntax el)
            {
                rhsExpression = el.Expression;
            }
            var invoke = rhsExpression;
            while (invoke.IsKind(SyntaxKind.InvocationExpression))
            {
                var rhsSymbol = _global.GetSymbol(invoke, this);
                if (rhsSymbol is IMethodSymbol m && (m.IsExtensionMethod/* || m.IsStaticCallConvention()*/))
                {
                    //We only rewite for extension method and static call convensions
                    return true;
                }
                //Dealing with something like oldBytes?.AsSpan(0, _offset).Clear();
                //The clear is not an extension method, but AsSpan is
                if (((InvocationExpressionSyntax)invoke).Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                {
                    var sm = (MemberAccessExpressionSyntax)((InvocationExpressionSyntax)invoke).Expression;
                    if (sm.Expression.IsKind(SyntaxKind.InvocationExpression))
                    {
                        invoke = sm.Expression;
                        continue;
                    }
                }
                break;
            }
            return false;
        }

        public bool ConditionalAccessUseIfNotNull(ConditionalAccessExpressionSyntax node, out ISymbol rhs)
        {
            var rhsExpression = node.WhenNotNull;
            bool useIfNotNull = false;
            int depth = 0;
            void CheckNode(ExpressionSyntax node)
            {
                var nodeType = _global.GetSymbol(node, this);
                if (nodeType is IMethodSymbol ms && ms.IsStaticCallConvention(_global))
                {
                    useIfNotNull |= true;
                }
                if (nodeType.GetTemplateAttribute(_global, this, checkPropertyAccessors: true) != null)
                {
                    useIfNotNull |= true;
                }
                if (node.IsKind(SyntaxKind.ElementBindingExpression) && node.Parent.IsKind(SyntaxKind.ConditionalAccessExpression))
                {
                    var indexer = GetGetIndexer((ElementBindingExpressionSyntax)node);
                    useIfNotNull |= indexer != null;
                }
            }
            while (true)
            {
                if (depth == 0)
                {
                    CheckNode(rhsExpression);
                }
                if (rhsExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression) && rhsExpression is MemberAccessExpressionSyntax me)
                {
                    rhsExpression = me.Expression;
                }
                else if (rhsExpression.IsKind(SyntaxKind.InvocationExpression) && rhsExpression is InvocationExpressionSyntax inv)
                {
                    rhsExpression = inv.Expression;
                }
                else break;
                depth++;
            }
            rhs = _global.GetSymbol(rhsExpression, this);
            if (node.WhenNotNull.IsKind(SyntaxKind.SimpleAssignmentExpression))
                useIfNotNull = true;
            CheckNode(rhsExpression);
            return useIfNotNull;
        }

        public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            Debug.Assert(!IsRewiteCandidate(node));
            var useIfNotNull = ConditionalAccessUseIfNotNull(node, out var rhs);
            if (useIfNotNull)
            {
                var iNameMangling = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                var localTemporaryIdentifierName = $"{Constants.IfNotNullParameterName}{iNameMangling}";
                CurrentTypeWriter.InsertAbove(node, () =>
                {
                    CurrentTypeWriter.Write(node, $"const {localTemporaryIdentifierName} = {(node.Expression.IsKind(SyntaxKind.IdentifierName) ? "" : "() => ")}");
                    Visit(node.Expression);
                    CurrentTypeWriter.Write(node, ";");
                }, true);
                CurrentTypeWriter.Write(node, _global.GlobalName);
                CurrentTypeWriter.Write(node, ".");
                CurrentTypeWriter.Write(node, Constants.IfNotNull);
                CurrentTypeWriter.Write(node, "(");
                CurrentTypeWriter.Write(node, localTemporaryIdentifierName);
                CurrentTypeWriter.Write(node, ", (");
                CurrentTypeWriter.Write(node, Constants.IfNotNullParameterName);
                CurrentTypeWriter.Write(node, ") => ");
                //if (node.WhenNotNull.IsKind(SyntaxKind.SimpleAssignmentExpression))
                //CurrentTypeWriter.Write(node, Constants.IfNotNullParameterName);
                Visit(node.WhenNotNull);
                CurrentTypeWriter.Write(node, ")");
            }
            else
            {
                if (rhs != null)
                {
                    var lhs = _global.GetTypeSymbol(node.Expression, this);
                    if (IsGenericDispatch(lhs, rhs))
                    {
                        Visit(node.WhenNotNull);
                        return;
                    }
                }
                if (!(node.Parent is StatementSyntax))
                    CurrentTypeWriter.Write(node, "(");
                Visit(node.Expression);
                CurrentTypeWriter.Write(node, node.OperatorToken.ValueText);

                //javascript doesnt support ?[] conditional array access notation, rewrite as ?.[
                var expressionType = _global.GetTypeSymbol(node.Expression, this);
                var whenNotNullArrayGetter = node.WhenNotNull.IsKind(SyntaxKind.ElementBindingExpression) ? GetGetIndexer((ElementBindingExpressionSyntax)node.WhenNotNull) : null;
                if ((expressionType.IsArray(out _) && node.WhenNotNull.ToString().StartsWith("[")) || whenNotNullArrayGetter != null)
                {
                    CurrentTypeWriter.Write(node, ".");
                }

                Visit(node.WhenNotNull);
                if (node.Parent is not StatementSyntax)
                    CurrentTypeWriter.Write(node, " ?? null"); //js null?.member is undefined, we need to convert it to null to be consistent with c#
                if (!(node.Parent is StatementSyntax))
                    CurrentTypeWriter.Write(node, ")");
            }
            return;
            ////This is rewritten, should not get called at all
            ////Debug.Assert(false);
            ////invocation visit will handle the conditional invocation
            ////if (node.WhenNotNull is InvocationExpressionSyntax conditionalInvoke)
            //{
            //    var i = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
            //    var temporaryIdentifierName = $"$t{i}";
            //    CurrentTypeWriter.InsertInCurrentClosure(node, $"let {temporaryIdentifierName};", true);
            //    var lhsType = GetExpressionReturnSymbol(node.Expression);
            //    //var lhsSymbol = GetTypeSymbol(lhsType, out _);
            //    //var lhsSymbol = GetTypeSymbol(lhsType, out _);
            //    //VariableDeclarationSyntax variableDeclaration = SyntaxFactory.VariableDeclaration(
            //    //    SyntaxFactory.ParseTypeName(lhsSymbol!.Name), // Type of the variable
            //    //    SyntaxFactory.SingletonSeparatedList(
            //    //        SyntaxFactory.VariableDeclarator(
            //    //            SyntaxFactory.Identifier(temporaryIdentifierName) // Name of the variable
            //    //        )
            //    //    )
            //    //);
            //    //LocalDeclarationStatementSyntax localDeclarationStatement = SyntaxFactory.LocalDeclarationStatement(variableDeclaration);
            //    //var block = node.FindClosest<BlockSyntax>();
            //    //var newBlock = block.InsertNodesBefore(block.ChildNodes().FirstOrDefault()!, [localDeclarationStatement]);
            //    ////node.InsertNodesBefore(node, [localDeclarationStatement]);
            //    //var localField = semanticModel.GetDeclaredSymbol(localDeclarationStatement);
            //    IDisposable? disposeTemporatyVariable = null;
            //    //if (lhsSymbol != null)
            //    //{
            //    //    var field = SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(lhsSymbol.Name), SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(temporaryIdentifierName))));
            //    //    var block = node.FindClosest<BlockSyntax>();
            //    //    node = (ConditionalAccessExpressionSyntax)node.Parent.InsertNodesBefore(node, [field]);
            //    //    var localField = GetTypeSymbol(field);
            //    //    disposeTemporatyVariable = CurrentClosure.DefineIdentifierType(temporaryIdentifierName, CodeType.From(localField));
            //    //}
            //    //else
            //    //{
            //    disposeTemporatyVariable = CurrentClosure.DefineIdentifierType(temporaryIdentifierName, lhsType with { Kind = SymbolKind.Local });
            //    //}
            //    if (false)
            //    {
            //        CurrentTypeWriter.WriteLine(node, $"{_global.GlobalName}.{Constants.Expression}(function()");
            //        CurrentTypeWriter.WriteLine(node, $"{{", true);
            //        CurrentTypeWriter.Write(node, $"let {temporaryIdentifierName} = ", true);
            //        Visit(node.Expression);
            //        CurrentTypeWriter.WriteLine(node, $";");
            //        CurrentTypeWriter.WriteLine(node, $"if ({temporaryIdentifierName} != null)", true);
            //        CurrentTypeWriter.WriteLine(node, $"{{", true);
            //        CurrentTypeWriter.Write(node, "return ", true);
            //    }
            //    else
            //    {
            //        CurrentTypeWriter.Write(node, $"(({temporaryIdentifierName} = ");
            //        Visit(node.Expression);
            //        CurrentTypeWriter.Write(node, $") && ");
            //    }
            //    ExpressionSyntax Combine(ExpressionSyntax lhs, ExpressionSyntax rhs)
            //    {
            //        if (rhs is InvocationExpressionSyntax conditionalInvoke)
            //        {
            //            if (conditionalInvoke.Expression is MemberBindingExpressionSyntax mb)
            //            {
            //                var memberAccess = SyntaxFactory.MemberAccessExpression(
            //                    SyntaxKind.SimpleMemberAccessExpression,
            //                    lhs,
            //                    SyntaxFactory.Token(SyntaxKind.DotToken), mb.Name);
            //                return SyntaxFactory.InvocationExpression(memberAccess, conditionalInvoke.ArgumentList);
            //            }
            //            else if (conditionalInvoke.Expression is MemberAccessExpressionSyntax ma)
            //            {
            //                var memberAccess = SyntaxFactory.MemberAccessExpression(
            //                    SyntaxKind.SimpleMemberAccessExpression,
            //                    Combine(lhs, ma.Expression),
            //                    SyntaxFactory.Token(SyntaxKind.DotToken), ma.Name);
            //                return SyntaxFactory.InvocationExpression(memberAccess, conditionalInvoke.ArgumentList);
            //            }
            //        }
            //        else if (rhs is MemberBindingExpressionSyntax member)
            //        {
            //            return SyntaxFactory.MemberAccessExpression(
            //                SyntaxKind.SimpleMemberAccessExpression,
            //                lhs,
            //                SyntaxFactory.Token(SyntaxKind.DotToken), member.Name);
            //        }
            //        else if (rhs is MemberAccessExpressionSyntax ma)
            //        {
            //            return SyntaxFactory.MemberAccessExpression(
            //                    SyntaxKind.SimpleMemberAccessExpression,
            //                    Combine(lhs, ma.Expression),
            //                    SyntaxFactory.Token(SyntaxKind.DotToken), ma.Name);
            //        }
            //        else if (rhs is ConditionalAccessExpressionSyntax cd)
            //        {
            //            var m = Combine(lhs, cd.Expression);
            //            return cd.ReplaceNode(cd.Expression, m);
            //        }
            //        else if (rhs is ElementAccessExpressionSyntax ae)
            //        {
            //            var newNode = Combine(lhs, ae.Expression);
            //            return ae.ReplaceNode(ae.Expression, newNode);
            //        }
            //        else if (rhs is ElementBindingExpressionSyntax ab)
            //        {
            //            var m = SyntaxFactory.ElementAccessExpression(lhs, ab.ArgumentList);
            //            return m;
            //        }
            //        else if (rhs is AssignmentExpressionSyntax asm)
            //        {
            //            var newNode = Combine(lhs, asm.Left);
            //            return asm.ReplaceNode(asm.Left, newNode);
            //        }
            //        return null;
            //    }
            //    ExpressionSyntax next = Combine(SyntaxFactory.IdentifierName($"{temporaryIdentifierName}"), node.WhenNotNull);

            //    //if (node.WhenNotNull is InvocationExpressionSyntax conditionalInvoke)
            //    //{
            //    //    next = Combine(SyntaxFactory.IdentifierName($"{temporaryIdentifierName}"), node.WhenNotNull);
            //    //    //var memberAccess = SyntaxFactory.MemberAccessExpression(
            //    //    //    SyntaxKind.SimpleMemberAccessExpression,
            //    //    //    SyntaxFactory.IdentifierName($"{temporaryIdentifierName}"),
            //    //    //    SyntaxFactory.Token(SyntaxKind.DotToken), ((MemberBindingExpressionSyntax)conditionalInvoke.Expression).Name);
            //    //    //next = SyntaxFactory.InvocationExpression(memberAccess, conditionalInvoke.ArgumentList);
            //    //}
            //    //else if (node.WhenNotNull is MemberBindingExpressionSyntax member)
            //    //{
            //    //    next = Combine(SyntaxFactory.IdentifierName($"{temporaryIdentifierName}"), node.WhenNotNull);
            //    //    //next = SyntaxFactory.MemberAccessExpression(
            //    //    //    SyntaxKind.SimpleMemberAccessExpression,
            //    //    //    SyntaxFactory.IdentifierName($"{temporaryIdentifierName}"),
            //    //    //    SyntaxFactory.Token(SyntaxKind.DotToken), member.Name);
            //    //}
            //    //else if (node.WhenNotNull is ConditionalAccessExpressionSyntax cd)
            //    //{
            //    //    var m = Combine(SyntaxFactory.IdentifierName($"{temporaryIdentifierName}"), cd.Expression);
            //    //    next = cd.ReplaceNode(cd.Expression, m);
            //    //}
            //    //node.ReplaceToken(node.OperatorToken, SyntaxFactory.ope($"$loc"));
            //    Visit(next);
            //    if (false)
            //    {
            //        CurrentTypeWriter.WriteLine(node, $";");
            //        CurrentTypeWriter.WriteLine(node, $"}}", true);
            //        CurrentTypeWriter.WriteLine(node, $"return null;", true);
            //        CurrentTypeWriter.Write(node, $"}}.bind(this))", true);
            //    }
            //    else
            //    {
            //        CurrentTypeWriter.Write(node, $")");
            //    }
            //    disposeTemporatyVariable.Dispose();
            //}
            ////else
            ////{
            ////    Visit(node.Expression);
            ////    Writer.Write(node, node.OperatorToken.ValueText/*.ToFullString()*/);
            ////    Visit(node.WhenNotNull);
            ////}
            ////base.VisitConditionalAccessExpression(node);
        }

        public override void VisitLockStatement(LockStatementSyntax node)
        {
            CurrentTypeWriter.WriteLine(node, "//lock", true);
            CurrentTypeWriter.WriteLine(node, "try", true);
            WriteBlock(node, new CodeNode(() =>
            {
                CurrentTypeWriter.Write(node, "", true);
                WriteMethodInvocation(node, "System.Threading.Monitor.Enter", methodFilter: (m) => m.Parameters.Length == 1, arguments: [node.Expression]);
                CurrentTypeWriter.WriteLine(node, "");
                Visit(node.Statement);
            }));
            CurrentTypeWriter.WriteLine(node, "finally", true);
            WriteBlock(node, new CodeNode(() =>
            {
                CurrentTypeWriter.Write(node, "", true);
                WriteMethodInvocation(node, "System.Threading.Monitor.Exit", methodFilter: (m) => m.Parameters.Length == 1, arguments: [node.Expression]);
                CurrentTypeWriter.WriteLine(node, "");
            }));
            //base.VisitLockStatement(node);
        }

        public override void VisitBracketedArgumentList(BracketedArgumentListSyntax node)
        {
            if (node.Arguments.Count > 1)
            {
                CurrentTypeWriter.Write(node, "get_Item(");
                int i = 0;
                foreach (var a in node.Arguments)
                {
                    if (i > 0)
                        CurrentTypeWriter.Write(node, ", ");
                    Visit(a);
                    i++;
                }
                CurrentTypeWriter.Write(node, ")");
            }
            else
            {
                CurrentTypeWriter.Write(node, node.OpenBracketToken.ValueText);
                int i = 0;
                foreach (var a in node.Arguments)
                {
                    if (i > 0)
                        CurrentTypeWriter.Write(node, ", ");
                    Visit(a);
                    i++;
                }
                CurrentTypeWriter.Write(node, node.CloseBracketToken.ValueText);
            }
            //base.VisitBracketedArgumentList(node);
        }

        public override void VisitMemberBindingExpression(MemberBindingExpressionSyntax node)
        {
            var rhs = _global.GetSymbol(node.Name, this);
            if (rhs.GetTemplateAttribute(_global, this, checkPropertyAccessors: true) != null)
            {
                //dont write the dot, if we will be writing a template
            }
            else
            {
                ConditionalAccessExpressionSyntax? ce = null;
                if ((ce = node.FindClosestParent<ConditionalAccessExpressionSyntax>()) != null)
                {
                    if (ConditionalAccessUseIfNotNull(ce, out _))
                    {
                        CurrentTypeWriter.Write(node, Constants.IfNotNullParameterName);
                    }
                }
                CurrentTypeWriter.Write(node, node.OperatorToken.ValueText);
            }
            Visit(node.Name);
            //base.VisitMemberBindingExpression(node);
        }

        public override void VisitElementBindingExpression(ElementBindingExpressionSyntax node)
        {
            if (node.Parent.IsKind(SyntaxKind.ConditionalAccessExpression))
            {
                var getter = GetGetIndexer(node);
                if (getter != null)
                {
                    WriteMethodInvocation(node, getter, null, node.ArgumentList.Arguments.Select(a => new CodeNode(a)), new CodeNode(() =>
                    {
                        if (node.Parent.IsKind(SyntaxKind.ConditionalAccessExpression))
                        {
                            CurrentTypeWriter.Write(node, Constants.IfNotNullParameterName);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }), null);
                    return;
                }
            }
            //javascript doesnt support ?[] conditional array access notation, rewrite as ?.[
            //if (node.Parent.IsKind(SyntaxKind.ConditionalAccessExpression))
            //{
            //    CurrentTypeWriter.Write(node, ".");
            //}
            base.VisitElementBindingExpression(node);
            ////If the lhs of the ConditionalAccessExpression is null, null?.[0] returns undefined, make it null with null?.[0]??null
            //if (node.Parent.IsKind(SyntaxKind.ConditionalAccessExpression))
            //{
            //    CurrentTypeWriter.Write(node, " ?? null");
            //}
        }

        void WriteTypeOf(CSharpSyntaxNode node, CodeNode typePrototype)
        {
            var typeSymbol = typePrototype.IsT0 ? _global.TryGetTypeSymbol(typePrototype.AsT0, this) : null;
            if (typeSymbol != null)
                CurrentTypeWriter.Write(node, typeSymbol.ComputeOutputTypeName(_global));
            else
                VisitNode(typePrototype);
            CurrentTypeWriter.Write(node, ".");
            CurrentTypeWriter.Write(node, Constants.PrototypeTypeName);
            //CurrentTypeWriter.Write(node, $"$.{Constants.TypeOf}(");
            //VisitNode(typePrototype);
            //CurrentTypeWriter.Write(node, ")");
        }

        public override void VisitTypeOfExpression(TypeOfExpressionSyntax node)
        {
            WriteTypeOf(node, node.Type);
            //Writer.Write(node, $"$.{Constants.TypeOf}(");
            //Visit(node.Type);
            //Writer.Write(node, ")");
            //base.VisitTypeOfExpression(node);
        }

        public override void VisitFixedStatement(FixedStatementSyntax node)
        {
            OpenClosure(node);
            //Keep all fixed in a separate block, lest we have variable names clasing
            //eg 
            // fixed(str = ...)
            //{
            //}
            // fixed(str = ...)
            //{
            //}
            //if (!node.Statement.IsKind(SyntaxKind.Block))
            CurrentTypeWriter.WriteLine(node, $"/*fixed*/ ", true);
            WriteBlock(node.Statement, new CodeNode(() =>
            {
                CurrentTypeWriter.Write(node, "", true);
                Visit(node.Declaration);
                CurrentTypeWriter.WriteLine(node, ";");
                //Make the block to know it its opening brace is already manually written and so skip another brace
                if (node.Statement.IsKind(SyntaxKind.Block))
                    MarkBlockBraceWritten((BlockSyntax)node.Statement);
                Visit(node.Statement);
            }));
            CloseClosure(node);
            //base.VisitFixedStatement(node);
        }

        public override void VisitWithExpression(WithExpressionSyntax node)
        {
            var type = _global.GetTypeSymbol(node.Expression, this);
            CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.With}(");
            Visit(node.Expression);
            CurrentTypeWriter.WriteLine(node, ", ($clone) =>");
            CurrentTypeWriter.WriteLine(node, "{", true);
            WriteInitializer(node, "$clone", type, node.Initializer.Expressions);
            CurrentTypeWriter.Write(node, "})", true);
            //base.VisitWithExpression(node);
        }

        public override void VisitIfDirectiveTrivia(IfDirectiveTriviaSyntax node)
        {
            //base.VisitIfDirectiveTrivia(node);
        }

        public override void VisitElifDirectiveTrivia(ElifDirectiveTriviaSyntax node)
        {
            //base.VisitElifDirectiveTrivia(node);
        }

        public override void VisitElseDirectiveTrivia(ElseDirectiveTriviaSyntax node)
        {
            //base.VisitElseDirectiveTrivia(node);
        }

        public override void VisitEndIfDirectiveTrivia(EndIfDirectiveTriviaSyntax node)
        {
            //base.VisitEndIfDirectiveTrivia(node);
        }

        public override void VisitTypeConstraint(TypeConstraintSyntax node)
        {
            //base.VisitTypeConstraint(node);
        }

        public override void VisitAttribute(AttributeSyntax node)
        {
            //base.VisitAttribute(node);
        }

        public void WrapStatementsInExpression(CSharpSyntaxNode node, Action statementsWriter)
        {
            CurrentTypeWriter.WriteLine(node, $"(() =>");
            CurrentTypeWriter.WriteLine(node, $"{{", true);
            statementsWriter();
            CurrentTypeWriter.Write(node, $"}})()", true);


            //CurrentTypeWriter.WriteLine(node, $"{_global.GlobalName}.{Constants.Expression}(() =>");
            //CurrentTypeWriter.WriteLine(node, $"{{", true);
            //statementsWriter();
            //CurrentTypeWriter.Write(node, $"}})", true);

            //CurrentTypeWriter.WriteLine(node, $"{_global.GlobalName}.{Constants.Expression}(function()");
            //CurrentTypeWriter.WriteLine(node, $"{{", true);
            //statementsWriter();
            //CurrentTypeWriter.Write(node, $"}}.bind(this))", true);
        }

        public string Build(int formatTabs)
        {
            var importsFromSource = _global.OutputMode.HasFlag(OutputMode.Module) ?
            string.Join("\r\n", imports.Where(e => e.Key.EndsWith(".cs")).Select(i => $"import {{ {string.Join(", ", i.Value)} }} from \"/{_global.Project.GetName()}/{Path.ChangeExtension(Utility.GetRelativePath(_global.Project.GetFolder(), i.Key), "js").Replace("\\", "/")}\"")) : null;
            var importsFromModule = _global.OutputMode.HasFlag(OutputMode.Module) ?
                string.Join("\r\n", imports.Where(e => e.Key.Contains(".dll")).Select(i => $"import {{ {string.Join(", ", i.Value)} }} from \"/{Path.GetFileNameWithoutExtension(i.Key)}.js\"")) : null;
            return (importsFromSource + "\r\n" + importsFromModule + "\r\n" + string.Join("\r\n\r\n", TypeWriters.Values.Select(w => w.Build(formatTabs)))).Trim();
        }

        public override string ToString()
        {
            return Build(0);
        }
    }
}