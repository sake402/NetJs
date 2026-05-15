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
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Ref;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.StaticConvention;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.String;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.SystemIndex;
using NetJs.Translator.RazorToCSharp;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

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


        static ISyntaxEmitter[] s_Emitters =
        [
            new ImplicitConversionSyntaxEmitter(),
            new RefTypeDereferenceOnAccessSyntaxEmitter(),
            new UnneccessaryUnsafeAddSyntaxEmitter(),

            new StaticConventionPropertySetterSyntaxEmitter(),

            new BoxPrimitiveAssignmentSyntaxEmitter(),

            new StringConstructorSyntaxEmitter(),
            new StringFirstCharSyntaxEmitter(),
            new RefToStringFirstCharSyntaxEmitter(),
            new RefArgumentToStringFirstCharSyntaxEmitter(),
            new AddressOfStringFirstCharSyntaxEmitter(),
            new MaterializeFastAllocatedStringOnReturnSyntaxEmitter(),
            new MaterializeFastAllocatedStringOnAssignmentSyntaxEmitter(),

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
            new IndexerGetItemSyntaxEmitter(),
            new IndexerSetItemSyntaxEmitter(),
            new SystemIndexToSetElementSyntaxEmitter(),
            new SystemIndexToGetElementSyntaxEmitter(),
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
            new DelegateRemoveSyntaxEmitter()
        ];
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

        public override void Visit(SyntaxNode? node)
        {
            if (node != null)
            {
                if (node.ToString().Contains("GroupBy"))
                {

                }
                var nodeType = node.GetType();
                for (int i = 0; i < s_Emitters.Length; i++)
                {
                    var emitter = s_Emitters[i];
                    if (emitter.SyntaxType.IsAbstract && emitter.SyntaxType.IsAssignableFrom(nodeType))
                    {
                        if (emitter.TryEmit(node, this))
                            return;
                    }
                    else if (nodeType == emitter.SyntaxType)
                    {
                        if (emitter.TryEmit(node, this))
                            return;
                    }
                }
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
            base.VisitCompilationUnit(node);
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
            if (node.ToString().Contains("ExecutionContext.SuppressFlow()"))
            {

            }
            if (node.Declaration != null)
            {
                foreach (var variable in node.Declaration.Variables)
                {
                    CurrentTypeWriter.WriteLine(node, $"const {variable.Identifier.ValueText} = null;", true);
                }
            }
            else if (node.Expression != null)
            {
                CurrentTypeWriter.WriteLine(node, "const $disposable = null;", true);
            }
            CurrentTypeWriter.WriteLine(node, "try", true);
            CurrentTypeWriter.WriteLine(node, "{", true);
            if (node.Expression != null)
            {
                CurrentTypeWriter.Write(node, "$disposable = ", true);
                Visit(node.Expression);
                CurrentTypeWriter.WriteLine(node, ";");
            }
            else if (node.Declaration != null)
            {
                CurrentTypeWriter.Write(node, "", true);
                Visit(node.Declaration);
                CurrentTypeWriter.WriteLine(node, ";");
            }
            //if (node.Expression != null)
            //{
            //    VisitChildren(node.Expression.ChildNodes());
            //}
            if (node.Statement != null)
            {
                //if (node.Statement is BlockSyntax block)
                if (node.Statement.IsKind(SyntaxKind.Block))
                    VisitChildren(node.Statement.ChildNodes());
                else
                    Visit(node.Statement);
            }
            //base.VisitUsingStatement(node);
            CurrentTypeWriter.WriteLine(node, "}", true);
            CurrentTypeWriter.WriteLine(node, "finally", true);
            CurrentTypeWriter.WriteLine(node, "{", true);
            if (node.Expression != null)
            {
                //Writer.WriteLine(node, "$disposable?.System$IDisposable$Dispose();", true);
                WriteMethodInvocation(node, "System.IDisposable.Dispose", lhsExpression: new CodeNode(() =>
                {
                    CurrentTypeWriter.Write(node, "$disposable?", true);
                }));
                CurrentTypeWriter.WriteLine(node, ";");
            }
            else if (node.Declaration != null)
            {
                foreach (var variable in node.Declaration.Variables)
                {
                    //Writer.WriteLine(node, $"{variable.Identifier.ValueText}?.System$IDisposable$Dispose();", true);
                    WriteMethodInvocation(node, "System.IDisposable.Dispose", lhsExpression: new CodeNode(() =>
                    {
                        CurrentTypeWriter.Write(node, $"{variable.Identifier.ValueText}?", true);
                    }));
                    CurrentTypeWriter.WriteLine(node, ";");
                }
            }
            CurrentTypeWriter.WriteLine(node, "}", true);
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
            if (TryInvokeMethodOperator(node, node.OperatorToken.ValueText, null, node.Operand, [node.Operand]))
                return;
            CurrentTypeWriter.Write(node, $"{node.OperatorToken.ValueText}");
            Visit(node.Operand);
            DereferenceIfReference(node.Operand);
            //base.VisitPrefixUnaryExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            if (!node.IsKind(SyntaxKind.SuppressNullableWarningExpression))
                if (TryInvokeMethodOperator(node, node.OperatorToken.ValueText, null, node.Operand, [node.Operand]))
                    return;
            Visit(node.Operand);
            DereferenceIfReference(node.Operand);
            if (!node.IsKind(SyntaxKind.SuppressNullableWarningExpression))//remove shebang after null and default
                CurrentTypeWriter.Write(node, $"{node.OperatorToken.ValueText}");
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (TryWriteMathBinaryExpression(node, node.OperatorToken.ValueText, node.Left, node.Right))
                return;
            if (TryInvokeMethodOperator(node, node.OperatorToken.ValueText, null, node.Left, [node.Left, node.Right]))
                return;
            var op = node.OperatorToken.ValueText.Trim();
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
            else if (op == "is" && node.Right is not LiteralExpressionSyntax)
            {
                CurrentTypeWriter.Write(node, $"{_global.GlobalName}.$is(");
                Visit(node.Left);
                CurrentTypeWriter.Write(node, ", ");
                Visit(node.Right);
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
                    if (node.Right is LiteralExpressionSyntax)
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
            WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.TaskToPromise", arguments: [node.Expression]);
            //base.VisitAwaitExpression(node);
        }

        public override void VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {
            CurrentTypeWriter.Write(node, "(");
            base.VisitParenthesizedExpression(node);
            CurrentTypeWriter.Write(node, ")");
        }

        public override void VisitBlock(BlockSyntax node)
        {
            CurrentTypeWriter.WriteLine(node, "{", true);
            OpenClosure(node);
            if (!BlockTryHandleJumpLabels(node))
                base.VisitBlock(node);
            CloseClosure();
            CurrentTypeWriter.WriteLine(node, "}", true);
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

        public override void VisitArgument(ArgumentSyntax node)
        {
            if (node.RefKindKeyword.ValueText == "out" || node.RefKindKeyword.ValueText == "ref" || node.RefKindKeyword.ValueText == "in")
            {
                var iNameMangling = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                var rhs = _global.TryGetSymbol(node.Expression, this/*, out var rhs, out var rhsKind*/);
                var rhsKind = rhs?.Kind;
                //ISymbol? rhsRefTarget = (declaringType as IParameterSymbol) ??
                //    (declaringType as IFieldSymbol) ??
                //    (ISymbol?)(declaringType as ILocalSymbol);
                //var rhsType = (declaringType as IParameterSymbol)?.Type ??
                //    (declaringType as ILocalSymbol)?.Type ??
                //    (declaringType as IFieldSymbol)?.Type ??
                //    (declaringType as IPropertySymbol)?.Type ?? declaringType as ITypeSymbol;
                var rhsRefKind = rhs?.GetRefKind();
                //if (rhsKind == SymbolKind.Field || rhsKind == SymbolKind.Local || rhsKind == SymbolKind.Parameter)
                //{
                if (rhsRefKind != null && rhsRefKind != RefKind.None) //the referenced field is already a ref itself. No need to create a new ref
                {
                    if (!node.Expression.IsKind(SyntaxKind.ThisExpression))
                    {
                        Visit(node.Expression);
                        return;
                    }
                }
                //}
                var expression = node.Expression;
                IDisposable? dispose1 = null;
                IDisposable? dispose2 = null;
                if (expression is DeclarationExpressionSyntax decl && decl.Designation is SingleVariableDesignationSyntax svd)
                {
                    CurrentTypeWriter.InsertInCurrentClosure(node, $"/*{decl.Type}*/ let {svd.Identifier.ValueText} = null;", true);
                    dispose1 = CurrentTypeWriter.SetReplacement("let ", "");
                    dispose2 = CurrentTypeWriter.SetReplacement($"/*{decl.Type}*/ ", "");
                }
                WriteCreateRef(node, expression, rhs != null ? _global.GetTypeSymbol(rhs) : null);
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
            CurrentTypeWriter.Write(node, "super");
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
                    WriteCreateArray(node, arrayType.ElementType, lengths: null, bounds: null, values: new CodeNode(() =>
                    {
                        CurrentTypeWriter.Write(node, "[");
                        int ix = 0;
                        foreach (var i in node.Expressions)
                        {
                            if (ix > 0)
                                CurrentTypeWriter.Write(node, ", ");
                            Visit(i);
                            ix++;
                        }
                        CurrentTypeWriter.Write(node, "]");
                    }));
                }
                else
                {
                    CurrentTypeWriter.Write(node, "[");
                    int i = 0;
                    foreach (var n in node.Expressions)
                    {
                        if (i > 0)
                            CurrentTypeWriter.Write(node, ", ");
                        Visit(n);
                        i++;
                    }
                    CurrentTypeWriter.Write(node, "]");
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
                CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.DefaultTypeName}(");
                Visit(node.Type);
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
            var dispose = _global.DefinePragma(node.Keyword.ValueText);
            base.VisitCheckedExpression(node);
            dispose.Dispose();
        }

        public override void VisitCheckedStatement(CheckedStatementSyntax node)
        {
            CurrentTypeWriter.WriteLine(node, $"//{node.Keyword.ValueText}", true);
            var dispose = _global.DefinePragma(node.Keyword.ValueText);
            base.VisitCheckedStatement(node);
            dispose.Dispose();
        }

        public override void VisitSizeOfExpression(SizeOfExpressionSyntax node)
        {
            CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.SizeOf}(");
            Visit(node.Type);
            CurrentTypeWriter.Write(node, $")");
            //base.VisitSizeOfExpression(node);
        }

        bool IsRewiteCandidate(ConditionalAccessExpressionSyntax node)
        {
            if (node.WhenNotNull.IsKind(SyntaxKind.ConditionalAccessExpression))
                return true;
            var rhsSymbol = _global.GetSymbol(node.WhenNotNull, this);
            if (rhsSymbol is IMethodSymbol m && (m.IsExtensionMethod /*|| m.IsStaticCallConvention(_global)*/))
            {
                //We only rewite for extension method
                return true;
            }
            return false;
        }

        public bool ConditionalAccessUseIfNotNull(ConditionalAccessExpressionSyntax node, out ISymbol rhs)
        {
            if (node.ToString().Contains("sb?.ToString()"))
            {

            }
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
                if (nodeType.GetTemplateAttribute(_global, checkPropertyAccessors: true) != null)
                {
                    useIfNotNull |= true;
                }
                if (node.IsKind(SyntaxKind.ElementBindingExpression))
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
                if (node.WhenNotNull.IsKind(SyntaxKind.SimpleAssignmentExpression))
                    CurrentTypeWriter.Write(node, Constants.IfNotNullParameterName);
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
                Visit(node.WhenNotNull);
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

        public override void VisitTupleElement(TupleElementSyntax node)
        {
            base.VisitTupleElement(node);
        }

        public override void VisitTupleExpression(TupleExpressionSyntax node)
        {
            if (node.Parent is AssignmentExpressionSyntax assignment)
            {
                if (assignment.Right == node)
                {
                    //assigning tuple to tuple in an expression like (T start, T end) = (_start, _endExclusive);
                    //should create a simple object desctructured back to the lhs a simple
                    //no need to instantiate a tuple type
                    CurrentTypeWriter.Write(node, "{ ");
                    int i = 0;
                    foreach (var e in node.Arguments)
                    {
                        if (i > 0)
                            CurrentTypeWriter.Write(node, ", ");
                        CurrentTypeWriter.Write(node, "Item");
                        CurrentTypeWriter.Write(node, (i + 1).ToString());
                        CurrentTypeWriter.Write(node, ": ");
                        Visit(e.Expression);
                        i++;
                    }
                    CurrentTypeWriter.Write(node, " }");
                }
                else
                {
                    if (node.Arguments.All(a => a.Expression.IsKind(SyntaxKind.DeclarationExpression)))
                    {
                        CurrentTypeWriter.Write(node, "const { ");
                        int i = 0;
                        foreach (var e in node.Arguments)
                        {
                            if (i > 0)
                                CurrentTypeWriter.Write(node, ", ");
                            CurrentTypeWriter.Write(node, "Item");
                            CurrentTypeWriter.Write(node, (i + 1).ToString());
                            CurrentTypeWriter.Write(node, ": ");
                            if (e.Expression is DeclarationExpressionSyntax de)
                            {
                                Visit(de.Designation);
                            }
                            else
                            {
                                Visit(e.Expression);
                            }
                            i++;
                        }
                        CurrentTypeWriter.Write(node, " }");
                    }
                    else
                    {
                        foreach (var e in node.Arguments)
                        {
                            if (e.Expression is DeclarationExpressionSyntax de)
                            {
                                Visit(de);
                                CurrentTypeWriter.WriteLine(node, ";");
                            }
                        }
                        CurrentTypeWriter.WriteLine(node, $"{_global.GlobalName}.{Constants.TupleUnPack}(($tp) =>");
                        CurrentTypeWriter.WriteLine(node, "{", true);
                        int ix = 0;
                        foreach (var arg in node.Arguments)
                        {
                            CurrentTypeWriter.Write(node, "", true);
                            WriteVariableAssignment(node, arg.Expression is DeclarationExpressionSyntax de ? de.Designation : arg.Expression, null, "=", new CodeNode(() =>
                            {
                                CurrentTypeWriter.Write(node, $"$tp.Item{(ix + 1)}");
                            }), rhs: _global.TryGetTypeSymbol(arg.Expression, this));
                            //if (arg.Expression is DeclarationExpressionSyntax de)
                            //{
                            //    Visit(de.Designation);
                            //}
                            //else
                            //{
                            //    Visit(arg.Expression);
                            //}
                            //Writer.Write(node, " = ");
                            //Writer.Write(node, "$tp.Item");
                            //Writer.Write(node, ix.ToString());
                            CurrentTypeWriter.WriteLine(node, ";");
                            ix++;
                        }
                        CurrentTypeWriter.Write(node, "}).$v", true);
                    }
                }
            }
            else
            {
                var tupleStruct = (INamedTypeSymbol)_global.GetSymbol($"System.ValueTuple<{string.Join(",", Enumerable.Range(1, node.Arguments.Count).Select(s => ""))}>", this/*, out _, out _*/);
                var argTypes = node.Arguments.Select(a => _global.TryGetTypeSymbol(a, this) ?? throw new InvalidOperationException("Cannot result tuple generic argument")).ToArray();
                tupleStruct = tupleStruct.Construct(argTypes);
                var tupleConstructor = (IMethodSymbol)tupleStruct.GetMembers(".ctor").Where(e => ((IMethodSymbol)e).Parameters.Count() == node.Arguments.Count).Single();
                WriteConstructorCall(node, tupleStruct, tupleConstructor, null, node.Arguments.Select(e => new CodeNode(e)), default);
                //Writer.Write(node, "{ ");
                //int i = 0;
                //foreach (var e in node.Arguments)
                //{
                //    if (i > 0)
                //        Writer.Write(node, ", ");
                //    //if (e.NameColon == null)
                //    //{
                //    Writer.Write(node, $"Item{i + 1}: ");
                //    //}
                //    //The namecolon are syntatic sugar, we still reference them in runtime as ItemX
                //    Visit(e.Expression);
                //    i++;
                //}
                //Writer.Write(node, " }");
            }
            //base.VisitTupleExpression(node);
        }

        public override void VisitTupleType(TupleTypeSyntax node)
        {
            CurrentTypeWriter.Write(node, $"System.ValueTuple(");
            int i = 0;
            foreach (var e in node.Elements)
            {
                if (i > 0)
                    CurrentTypeWriter.Write(node, ", ");
                Visit(e);
                i++;
            }
            CurrentTypeWriter.Write(node, $")");
            //base.VisitTupleType(node);
        }

        public override void VisitLockStatement(LockStatementSyntax node)
        {
            CurrentTypeWriter.WriteLine(node, "//lock", true);
            CurrentTypeWriter.WriteLine(node, "try", true);
            CurrentTypeWriter.WriteLine(node, "{", true);
            CurrentTypeWriter.Write(node, "", true);
            WriteMethodInvocation(node, "System.Threading.Monitor.Enter", methodFilter: (m) => m.Parameters.Length == 1, arguments: [node.Expression]);
            CurrentTypeWriter.WriteLine(node, "");
            Visit(node.Statement);
            CurrentTypeWriter.WriteLine(node, "}", true);
            CurrentTypeWriter.WriteLine(node, "finally", true);
            CurrentTypeWriter.WriteLine(node, "{", true);
            CurrentTypeWriter.Write(node, "", true);
            WriteMethodInvocation(node, "System.Threading.Monitor.Exit", methodFilter: (m) => m.Parameters.Length == 1, arguments: [node.Expression]);
            CurrentTypeWriter.WriteLine(node, "");
            CurrentTypeWriter.WriteLine(node, "}", true);
            //base.VisitLockStatement(node);
        }

        public override void VisitBracketedArgumentList(BracketedArgumentListSyntax node)
        {
            CurrentTypeWriter.Write(node, node.OpenBracketToken.ValueText);
            if (node.Arguments.Count > 1)
            {
                CurrentTypeWriter.Write(node, "getItem(");
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
                int i = 0;
                foreach (var a in node.Arguments)
                {
                    if (i > 0)
                        CurrentTypeWriter.Write(node, ", ");
                    Visit(a);
                    i++;
                }
            }
            CurrentTypeWriter.Write(node, node.CloseBracketToken.ValueText);
            //base.VisitBracketedArgumentList(node);
        }

        public override void VisitMemberBindingExpression(MemberBindingExpressionSyntax node)
        {
            var rhs = _global.GetSymbol(node.Name, this);
            if (rhs.GetTemplateAttribute(_global, checkPropertyAccessors: true) != null)
            {
                //dont write the dot, if we will be writing a template
            }
            else
            {
                CurrentTypeWriter.Write(node, node.OperatorToken.ValueText);
            }
            Visit(node.Name);
            //base.VisitMemberBindingExpression(node);
        }

        public override void VisitElementBindingExpression(ElementBindingExpressionSyntax node)
        {
            //javascript doesnt support ?[] conditional array access notation, rewrite as ?.[
            if (node.Parent.IsKind(SyntaxKind.ConditionalAccessExpression))
            {
                CurrentTypeWriter.Write(node, ".");
            }
            base.VisitElementBindingExpression(node);
            ////If the lhs of the ConditionalAccessExpression is null, null?.[0] returns undefined, make it null with null?.[0]??null
            //if (node.Parent.IsKind(SyntaxKind.ConditionalAccessExpression))
            //{
            //    CurrentTypeWriter.Write(node, " ?? null");
            //}
        }

        void WriteTypeOf(CSharpSyntaxNode node, CodeNode typePrototype)
        {
            CurrentTypeWriter.Write(node, $"$.{Constants.TypeOf}(");
            VisitNode(typePrototype);
            CurrentTypeWriter.Write(node, ")");
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
            if (!node.Statement.IsKind(SyntaxKind.Block))
                CurrentTypeWriter.WriteLine(node, "{", true);
            CurrentTypeWriter.Write(node, "/*fixed*/ ", true);
            //CurrentTypeWriter.Write(node, "", true);
            Visit(node.Declaration);
            CurrentTypeWriter.WriteLine(node, ";");
            Visit(node.Statement);
            if (!node.Statement.IsKind(SyntaxKind.Block))
                CurrentTypeWriter.WriteLine(node, "}", true);
            CloseClosure();
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
            CurrentTypeWriter.WriteLine(node, $"{_global.GlobalName}.{Constants.Expression}(() =>");
            CurrentTypeWriter.WriteLine(node, $"{{", true);
            statementsWriter();
            CurrentTypeWriter.Write(node, $"}})", true);

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