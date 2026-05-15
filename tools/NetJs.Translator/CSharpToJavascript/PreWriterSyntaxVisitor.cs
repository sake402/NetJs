using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class PreWriterSyntaxVisitor : CSharpSyntaxRewriter
    {
        CSharpCompilation _compilation;
        SemanticModel _semanticModel;
        SyntaxTree _tree;
        Dictionary<string, List<TypeDeclarationSyntax>> _partialClassGroupings;
        //List<SyntaxNode> _pendingVisits;
        public PreWriterSyntaxVisitor(
            CSharpCompilation compilation,
            SyntaxTree tree,
            Dictionary<string, List<TypeDeclarationSyntax>> partialClassGroupings)
        {
            _compilation = compilation;
            _tree = tree;
            _partialClassGroupings = partialClassGroupings;
            //_pendingVisits = pendingVisits;
            _semanticModel = compilation.GetSemanticModel(tree);
        }

        Dictionary<SyntaxNode, SyntaxNode> replacements = new Dictionary<SyntaxNode, SyntaxNode>();
        IDisposable AssociateSyntaxFactoryNode(SyntaxNode original, SyntaxNode newNode)
        {
            if (original != newNode && original is CSharpSyntaxNode originalCs)
            {
                Debug.Assert(original.GetType() == newNode.GetType());
                if (original.GetType() == newNode.GetType())
                {
                    var visitor = new AssociateSyntaxFactoryNewNodeVisitor(replacements, original, newNode);
                    originalCs.Accept(visitor);
                    return visitor;
                }
                else
                {
                    replacements.Add(newNode, original);
                    return new DelegateDispose(() => { replacements.Remove(newNode); });
                }
            }
            return new DelegateDispose(() => { });
        }

        bool IsRewiteCandidate(ConditionalAccessExpressionSyntax node)
        {
            if (node.WhenNotNull.IsKind(SyntaxKind.ConditionalAccessExpression))
                return true;
            var rhsExpression = node.WhenNotNull;
            if (rhsExpression.IsKind(SyntaxKind.ElementAccessExpression) && rhsExpression is ElementAccessExpressionSyntax el)
            {
                rhsExpression = el.Expression;
            }
            var rhsSymbol = GetSymbol(rhsExpression);
            if (rhsSymbol is IMethodSymbol m && (m.IsExtensionMethod/* || m.IsStaticCallConvention()*/))
            {
                //We only rewite for extension method and static call convensions
                return true;
            }
            return false;
        }

        BlockSyntax WrapInBlock(StatementSyntax expression)
        {
            return SyntaxFactory.Block(expression.WithLeadingTrivia(SyntaxFactory.LineFeed)).WithLeadingTrivia(SyntaxFactory.LineFeed).WithTrailingTrivia(SyntaxFactory.LineFeed);
        }
        //StatementSyntax? currentStatement;
        //StatementSyntax? previousStatement;
        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            //if (node is StatementSyntax ss)
            //    currentStatement = ss;
            var newNode = base.Visit(node);
            //if (node != newNode && node != null && newNode != null && newNode.GetType() == node.GetType() && node.SyntaxTree == _tree)
            //{
            //    //if (node is BaseTypeDeclarationSyntax) { }
            //    //else
            //    //    //if (replacements.TryGetValue(newNode, out var nd) && nd == node)
            //    //    //{

            //    //    //}
            //    //    //else
            //    //    //replacements.Add(newNode, node);
            //    //    AssociateSyntaxFactoryNode(node, newNode);
            //    ////replacements[newNode] = node;
            //}
            //if (newNode is StatementSyntax sss)
            //    previousStatement = sss;
            return newNode;
        }

        ISymbol? GetSymbolInfo(SyntaxNode node)
        {
            var t = node.SyntaxTree == _tree ? _semanticModel.GetSymbolInfo(node) :
                node.SyntaxTree.HasCompilationUnitRoot ? _compilation.GetSemanticModel(node.SyntaxTree).GetSymbolInfo(node) :
                default;
            if (t.Symbol == null)
            {
                if (replacements.TryGetValue(node, out var originalNode))
                {
                    t = _semanticModel.GetSymbolInfo(originalNode);
                }
            }
            return t.Symbol;// ?? throw new InvalidOperationException("Cannot obtain type");
        }

        ITypeSymbol? GetTypeSymbol(SyntaxNode node)
        {
            var t = node.SyntaxTree == _tree ? _semanticModel.GetTypeInfo(node).Type :
                node.SyntaxTree.HasCompilationUnitRoot ? _compilation.GetSemanticModel(node.SyntaxTree).GetTypeInfo(node).Type :
                null;
            if (t == null)
            {
                if (replacements.TryGetValue(node, out var originalNode))
                {
                    t = _semanticModel.GetTypeInfo(originalNode).Type;
                }
            }
            return t;// ?? throw new InvalidOperationException("Cannot obtain type");
        }

        ISymbol? GetSymbol(SyntaxNode node)
        {
            var t = node.SyntaxTree == _tree ? _semanticModel.GetSymbolInfo(node).Symbol :
                node.SyntaxTree.HasCompilationUnitRoot ? _compilation.GetSemanticModel(node.SyntaxTree).GetSymbolInfo(node).Symbol :
                null;
            if (t == null)
            {
                if (replacements.TryGetValue(node, out var originalNode))
                {
                    t = _semanticModel.GetSymbolInfo(originalNode).Symbol;
                }
            }
            return t;// ?? throw new InvalidOperationException("Cannot obtain type");
        }

        //public ISymbol? GetExpressionBoundMember(CSharpSyntaxNode expression)
        //{
        //    if (expression is RefExpressionSyntax rref)
        //        expression = rref.Expression;
        //    if (expression is ArgumentSyntax arg)
        //        expression = arg.Expression;
        //    if (expression is CastExpressionSyntax cast)
        //        expression = cast.Expression;
        //    if (expression is ParenthesizedExpressionSyntax par)
        //        expression = par.Expression;
        //    var sinfo = _semanticModel.GetSymbolInfo(expression);
        //    if (sinfo.Symbol != null)
        //    {
        //        var symbol = sinfo.Symbol;
        //        if (symbol is IMethodSymbol ms && ms.ReducedFrom != null)
        //        {
        //            if (ms.IsGenericMethod)
        //            {
        //                symbol = ms.ReducedFrom.Construct(ms.TypeArguments.ToArray());
        //            }
        //            else
        //            {
        //                symbol = ms.ReducedFrom;
        //            }
        //        }
        //        return symbol;
        //    }
        //    return null;
        //}

        bool NeedsWapInBLock(StatementSyntax statement)
        {
            if (statement.Parent is IfStatementSyntax ifs)
            {
                return ifs.Statement is not BlockSyntax;
            }
            else if (statement.Parent is ElseClauseSyntax els)
            {
                return els.Statement is not BlockSyntax;
            }
            else if (statement.Parent is ForStatementSyntax fors)
            {
                return fors.Statement is not BlockSyntax;
            }
            else if (statement.Parent is ForEachStatementSyntax fe)
            {
                return fe.Statement is not BlockSyntax;
            }
            else if (statement.Parent is WhileStatementSyntax wh)
            {
                return wh.Statement is not BlockSyntax;
            }
            else if (statement.Parent is DoStatementSyntax dos)
            {
                return dos.Statement is not BlockSyntax;
            }
            return false;
        }

        public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
        {
            node = (ForStatementSyntax)base.VisitForStatement(node)!;
            if (node.Statement is not BlockSyntax)
            {
                var block = WrapInBlock(node.Statement);
                node = node.ReplaceNode(node.Statement, block);
            }
            return node;
        }

        public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
        {
            node = (ForEachStatementSyntax)base.VisitForEachStatement(node)!;
            if (node.Statement is not BlockSyntax)
            {
                var block = WrapInBlock(node.Statement);
                node = node.ReplaceNode(node.Statement, block);
            }
            return node;
        }

        public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
        {
            node = (IfStatementSyntax)base.VisitIfStatement(node)!;
            //Keep statement in block so that other auto variables we need to define for the statement can remain in the block
            if (node.Statement is not BlockSyntax)
            {
                var block = WrapInBlock(node.Statement);
                node = node.ReplaceNode(node.Statement, block);
            }
            return node;
        }

        public override SyntaxNode? VisitElseClause(ElseClauseSyntax node)
        {
            node = (ElseClauseSyntax)base.VisitElseClause(node)!;
            if (node.Statement is not BlockSyntax && node.Statement is not IfStatementSyntax)
            {
                var block = WrapInBlock(node.Statement);
                node = node.ReplaceNode(node.Statement, block);
            }
            return node;
        }

        public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
        {
            node = (WhileStatementSyntax)base.VisitWhileStatement(node)!;
            if (node.Statement is not BlockSyntax)
            {
                var block = WrapInBlock(node.Statement);
                node = node.ReplaceNode(node.Statement, block);
            }
            return node;
        }

        SyntaxNode? TryRemovePartial(IdentifierNameSyntax node)
        {
            if (node.Identifier.ValueText.EndsWith("_Partial"))
            {
                return SyntaxFactory.IdentifierName(node.Identifier.ValueText.Substring(0, node.Identifier.ValueText.Length - "_Partial".Length))
                    .WithLeadingTrivia(node.GetLeadingTrivia())
                    .WithTrailingTrivia(node.GetTrailingTrivia());
            }
            return null;
        }
        SyntaxNode? TryRewriteTHIS(IdentifierNameSyntax node)
        {
            if (node.Identifier.ValueText.Equals("THIS"))
            {
                //var @class = node.FindClosestParent<BaseTypeDeclarationSyntax>();
                //if (@class?.BaseList?.Types.Any(b => b.Type.ToString().Contains("ForcedPartialBase")) ?? false)
                {
                    return SyntaxFactory.IdentifierName("this")
                    .WithLeadingTrivia(node.GetLeadingTrivia())
                    .WithTrailingTrivia(node.GetTrailingTrivia());
                }
            }
            return null;
        }
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            return TryRemovePartial(node) ?? 
                TryRewriteTHIS(node) ?? 
                TryGetQueryMemberAccessForVariable(node) ?? 
                base.VisitIdentifierName(node);
            //if (node.Identifier.ValueText.EndsWith("_Partial"))
            //{
            //    return SyntaxFactory.IdentifierName(node.Identifier.ValueText.Substring(0, node.Identifier.ValueText.Length - "_Partial".Length))
            //        .WithLeadingTrivia(node.GetLeadingTrivia())
            //        .WithTrailingTrivia(node.GetTrailingTrivia());
            //}
            ////if (node.Identifier.ValueText.Equals("THIS"))
            ////{
            ////    return SyntaxFactory.IdentifierName("this")
            ////        .WithLeadingTrivia(node.GetLeadingTrivia())
            ////        .WithTrailingTrivia(node.GetTrailingTrivia());
            ////}
            //if (node.Identifier.ValueText.Equals("THIS"))
            //{
            //    //var @class = node.FindClosestParent<BaseTypeDeclarationSyntax>();
            //    //if (@class?.BaseList?.Types.Any(b => b.Type.ToString().Contains("ForcedPartialBase")) ?? false)
            //    {
            //        return SyntaxFactory.IdentifierName("this")
            //        .WithLeadingTrivia(node.GetLeadingTrivia())
            //        .WithTrailingTrivia(node.GetTrailingTrivia());
            //    }
            //}
            //return base.VisitIdentifierName(node);
        }
        BaseTypeDeclarationSyntax? TryBruteForcePartialTypes(BaseTypeDeclarationSyntax node, string fullName)
        {
            //Rename forced partial types to the requested types
            if (node.HasAnyAttribute([typeof(ForcePartialAttribute).FullName], out var atts2))
            {
                var att = atts2.Values.Single().Single();
                var type = (TypeOfExpressionSyntax)att.ArgumentList!.Arguments[0].Expression;
                var typeName = type.Type.ToString().TrimEnd('<', ',', '>');
                var newNode = node.WithIdentifier(SyntaxFactory.Identifier(typeName)).WithBaseList(null);
                if (!newNode.Modifiers.Any(e => e.IsKind(SyntaxKind.PartialKeyword)))
                {
                    SyntaxTokenList newModifiers = newNode.Modifiers.Add(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.PartialKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)));
                    newNode = newNode.WithModifiers(newModifiers).WithTrailingTrivia(SyntaxFactory.Space);
                }
                return newNode;
            }
            //var fullName = node.CreateFullMemberName()!;
            var partialClasses = _partialClassGroupings[fullName];
            Dictionary<string, List<AttributeSyntax>>? atts = null;
            TypeDeclarationSyntax? partialPart = null;
            //Add partial modifiers to the existing types beign forced on
            if (partialClasses.Any(part =>
            {
                if (part.HasAnyAttribute([typeof(ForcePartialAttribute).FullName], out atts))
                {
                    partialPart = part;
                    return true;
                }
                return false;
            }))
            {
                var att = atts!.Values.Single().Single();
                var type = (TypeOfExpressionSyntax)att.ArgumentList!.Arguments[0].Expression;
                var typeName = type.Type.ToString();
                var newNode = node.WithModifiers(partialPart!.Modifiers);
                if (!newNode.Modifiers.Any(e => e.IsKind(SyntaxKind.PartialKeyword)))
                {
                    SyntaxTokenList newModifiers = newNode.Modifiers.Add(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.PartialKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)));
                    newNode = newNode.WithModifiers(newModifiers).WithTrailingTrivia(SyntaxFactory.Space);
                }
                return newNode;
            }
            return null;
        }

        public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var fullName = node.CreateFullMemberName()!;
            node = (InterfaceDeclarationSyntax?)base.VisitInterfaceDeclaration(node) ?? node;
            var newNode = TryBruteForcePartialTypes(node, fullName);
            return newNode ?? node;
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var fullName = node.CreateFullMemberName()!;
            node = (ClassDeclarationSyntax?)base.VisitClassDeclaration(node) ?? node;
            var newNode = TryBruteForcePartialTypes(node, fullName);
            return newNode ?? node;
        }

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var fullName = node.CreateFullMemberName()!;
            node = (StructDeclarationSyntax?)base.VisitStructDeclaration(node) ?? node;
            var newNode = TryBruteForcePartialTypes(node, fullName);
            return newNode ?? node;
        }

        public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            var fullName = node.CreateFullMemberName()!;
            node = (RecordDeclarationSyntax?)base.VisitRecordDeclaration(node) ?? node;
            var newNode = TryBruteForcePartialTypes(node, fullName);
            return newNode ?? node;
        }

        string ResolveMemberReplaceExpressionSignature(AttributeArgumentSyntax argValue)
        {
            var signature = argValue.Expression.ToString();
            int nameOf = signature.IndexOf("nameof(");
            while (nameOf >= 0)
            {
                var closingBrace = signature.IndexOf(")", nameOf);
                var extract = signature.Substring("nameof(".Length, closingBrace - "nameof(".Length);
                var name = extract.Split('.').Last();
                signature = signature.Substring(0, closingBrace) + "\"" + signature.Substring(closingBrace + 1);
                signature = signature.Replace("nameof(" + extract, "nameof(" + name);
                signature = signature.Replace("nameof(", "\"");
                nameOf = signature.IndexOf("nameof(");
            }
            var signatures = signature.Split(['+']).Select(s => s.Trim().Trim('"'));
            signature = string.Join("", signatures);
            return signature;
        }

        string GetMemberIdentifier(MemberDeclarationSyntax node, bool throws = true)
        {
            var identifier =
                node is MethodDeclarationSyntax method ? method.Identifier.ValueText + (method.Arity > 0 ? $"<{string.Join(",", Enumerable.Range(1, method.Arity).Select(s => ""))}>" : "") :
                node is ConstructorDeclarationSyntax ctor ? ".ctor" :
                node is PropertyDeclarationSyntax property ? property.Identifier.ValueText :
                node is IndexerDeclarationSyntax idx ? "this" :
                node is FieldDeclarationSyntax field && field.Declaration.Variables.Count == 1 ? field.Declaration.Variables.Single().Identifier.ValueText :
                !throws ? "" :
                throw new InvalidOperationException();
            return identifier;
        }

        string? GetMemberSignature(MemberDeclarationSyntax node, bool throws = true)
        {
            if (node is MethodDeclarationSyntax mm && mm.FindClosestParent<TypeDeclarationSyntax>()?.Identifier.ValueText == "Unsafe" && mm.Identifier.ValueText == "Add")
            {

            }
            string ModifierString(ParameterSyntax p)
            {
                return p.Modifiers.ToFullString();
                //return $"{(p.Modifiers.IsRef() ? "ref " : p.Modifiers.IsOut() ? "out " : p.Modifiers.IsIn() ? "in " : "")}";
            }
            var methodSignature =
                node is MethodDeclarationSyntax method1 ? $"{method1.Identifier.ValueText}{(method1.Arity > 0 ? $"<{string.Join(",", Enumerable.Range(1, method1.Arity).Select(s => ""))}>" : "")}({string.Join(", ", method1.ParameterList.Parameters.Select(p => $"{ModifierString(p)}{p.Type}"))})" :
                node is ConstructorDeclarationSyntax ctor1 ? $".ctor({string.Join(", ", ctor1.ParameterList.Parameters.Select(p => p.Type?.ToString()))})" :
                node is PropertyDeclarationSyntax property1 ? $"{property1.Identifier.ValueText}" :
                node is IndexerDeclarationSyntax idx ? $"this[{string.Join(", ", idx.ParameterList.Parameters.Select(p => p.Type?.ToString()))}]" :
                node is FieldDeclarationSyntax field1 ? $"{field1.Declaration.Variables.Single().Identifier.ValueText}" :
                !throws ? null :
                throw new InvalidOperationException();
            return methodSignature;
        }

        bool CanOverride(MemberDeclarationSyntax originalMember, MemberDeclarationSyntax overrideMemberCandidate, out string? getSet, bool throws = true)
        {
            getSet = null;
            var originalType = originalMember.GetType();
            var overrideType = overrideMemberCandidate.GetType();
            if (originalType != overrideType)
            {
                //we allow method to override a constructor though
                if (originalType == typeof(ConstructorDeclarationSyntax) && overrideType == typeof(MethodDeclarationSyntax)) { }
                //we also allow method to override a indexer
                else if (originalType == typeof(IndexerDeclarationSyntax) && overrideType == typeof(MethodDeclarationSyntax)) { }
                //we also allow property to override a field
                else if (originalType == typeof(FieldDeclarationSyntax) && overrideType == typeof(PropertyDeclarationSyntax)) { }
                else
                {
                    return false;
                }
            }
            var originalParameters =
                originalMember is MethodDeclarationSyntax method ? method.ParameterList.Parameters :
                originalMember is ConstructorDeclarationSyntax ctor ? ctor.ParameterList.Parameters :
                originalMember is IndexerDeclarationSyntax idx ? idx.ParameterList.Parameters :
                originalMember is PropertyDeclarationSyntax property ? [] :
                originalMember is FieldDeclarationSyntax field ? [] :
                !throws ? [] :
                throw new InvalidOperationException();
            var overrideCandidateParameters =
                overrideMemberCandidate is MethodDeclarationSyntax method1 ? method1.ParameterList.Parameters :
                overrideMemberCandidate is ConstructorDeclarationSyntax ctor1 ? ctor1.ParameterList.Parameters :
                overrideMemberCandidate is IndexerDeclarationSyntax idx2 ? idx2.ParameterList.Parameters :
                overrideMemberCandidate is PropertyDeclarationSyntax property1 ? [] :
                overrideMemberCandidate is FieldDeclarationSyntax field1 ? [] :
                !throws ? [] :
                throw new InvalidOperationException();
            if (!overrideMemberCandidate.HasAnyAttribute([typeof(MemberParameterCountMayNotMatch).FullName], out _))
            {
                if (originalParameters.Count != overrideCandidateParameters.Count)
                {
                    return false;
                }
            }
            if (!overrideMemberCandidate.HasAnyAttribute([typeof(MemberParameterTypesMayNotMatch).FullName], out _))
            {
                if (!originalParameters.Select((p, i) => (p, i)).All(p =>
                {
                    var originalParameter = p.p;
                    var overrideParameter = overrideCandidateParameters.ElementAt(p.i);
                    if (originalParameter.Type == overrideParameter.Type)
                        return true;
                    return originalParameter.Type?.ToString() == overrideParameter.Type?.ToString();
                    //return originalParameter.Type?.Equals(overrideParameter.Type) ?? false;
                }))
                {
                    return false;
                }
            }
            if (overrideMemberCandidate.HasAnyAttribute([typeof(MemberReplaceAttribute).FullName], out var args))
            {
                //if (originalMember is FieldDeclarationSyntax && overrideMemberCandidate is PropertyDeclarationSyntax m && (m.Identifier.ValueText == "GetMValue" || m.Identifier.ValueText == "SetMValue"))
                //{

                //}
                var identifier = GetMemberIdentifier(originalMember, throws: throws);
                var originalMemberSignature = GetMemberSignature(originalMember, throws: throws);
                if (originalMemberSignature == null)
                    return false;
                var arg = args.Single().Value[0].ArgumentList?.Arguments;
                if ((arg?.Count ?? 0) == 0)
                {
                    var nameIndentifier = overrideMemberCandidate is PropertyDeclarationSyntax p ? p.Identifier.ValueText :
                         overrideMemberCandidate is FieldDeclarationSyntax f ? f.Declaration.Variables.Single().Identifier.ValueText :
                        //overrideMemberCandidate is IndexerDeclarationSyntax idx3 ? "this[]" :
                        ((MethodDeclarationSyntax)overrideMemberCandidate).Identifier.ValueText;
                    return nameIndentifier == identifier;
                }
                else if (arg?.Count == 1)
                {
                    var argValue = arg.Value[0];
                    var attributedSignature = ResolveMemberReplaceExpressionSignature(argValue);
                    if (attributedSignature.EndsWith(")") || attributedSignature.EndsWith("]") || attributedSignature.EndsWith(".get") || attributedSignature.EndsWith(".set"))
                    {
                        if (attributedSignature == originalMemberSignature)
                            return true;
                        if (attributedSignature == originalMemberSignature + ".get")
                        {
                            getSet = "get";
                            return true;
                        }
                        if (attributedSignature == originalMemberSignature + ".set")
                        {
                            getSet = "set";
                            return true;
                        }
                        return false;
                    }
                    return attributedSignature == identifier;
                }
            }
            return false;
        }

        bool isMemberVisitFromGetMemberOverride;
        Dictionary<string, MemberDeclarationSyntax>? GetMemberOverride(MemberDeclarationSyntax node)
        {
            var @class = node.FindClosestParent<TypeDeclarationSyntax>();
            if (@class != null)
            {
                var fullName = @class.CreateFullMemberName()!;
                var partialClasses = _partialClassGroupings[fullName].Except([@class]);
                var identifier = GetMemberIdentifier(node);
                //var methodSignature = GetMemberSignature(node);
                var memberOverride = partialClasses
                    .SelectMany(c => c.Members)
                    .Where(e => node is PropertyDeclarationSyntax ? e is PropertyDeclarationSyntax :
                                node is FieldDeclarationSyntax ? e is PropertyDeclarationSyntax || e is FieldDeclarationSyntax :
                                //node is IndexerDeclarationSyntax ? e is IndexerDeclarationSyntax :
                                e is MethodDeclarationSyntax)
                    //.OfType<MethodDeclarationSyntax>()
                    //.FirstOrDefault(m => m.AttributeLists.Any(a => a.Attributes.Any(aa => aa.Name.ToString().Contains("dotnetJs.MemberReplace") && (aa.ArgumentList?.Arguments.Any(arg => arg.Expression.ToString().Contains(node.Identifier.ValueText)) ?? false))));
                    .Select(m =>
                    {
                        return (m, CanOverride(node, m, out var getSet), getSet);
                    }).Where(e => e.Item2)
                    .Select(e =>
                    {
                        var attName = nameof(MemberReplaceAttribute).Replace("Attribute", "");
                        var attributes = e.Item1?.AttributeLists.Select(a =>
                        {
                            var atts = a.Attributes.Where(e => !e.Name.ToString().Contains(attName));
                            return SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(atts));
                        });
                        isMemberVisitFromGetMemberOverride = true;
                        var mem = (MemberDeclarationSyntax)(Visit(e.Item1) ?? e.Item1)!;
                        isMemberVisitFromGetMemberOverride = false;
                        return (e.Item3 ?? "", mem.WithAttributeLists(SyntaxFactory.List(attributes!)));
                    })
                    .ToDictionary(e => e.Item1, e => e.Item2);
                return memberOverride;
            }
            return null;
        }

        void EnsureUnambiguity(MemberDeclarationSyntax node, AttributeArgumentSyntax? arg)
        {
            var @class = node.FindClosestParent<TypeDeclarationSyntax>();
            if (@class != null)
            {
                var fullName = @class.CreateFullMemberName()!;
                var partialClasses = _partialClassGroupings[fullName].Except([@class]);
                var members = partialClasses.SelectMany(c => c.Members).Except([node]);
                var matchingMembers = members
                    .Where(member => CanOverride(member, node, out _, false))
                    .Select(member => (GetMemberSignature(member, false), member))
                    //.Where(m => m.Item2 == providedSignature)
                    .ToList();
                if (matchingMembers.Count > 1)
                {
                    var signatures = GetMemberSignature(node);
                    var identifier = GetMemberIdentifier(node);
                    var providedSignature = arg != null ? ResolveMemberReplaceExpressionSignature(arg) : identifier;
                    throw new AmbiguousMatchException($"From \"{providedSignature}\" defined on {identifier}. Ambiguity between members of signatures {(string.Join(", ", matchingMembers.Select(m => m.Item1)))}. You must fully qualifut the intended member to override");
                }
            }
        }

        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var @class = node.FindClosestParent<BaseTypeDeclarationSyntax>();
            var memberOverride = (MethodDeclarationSyntax?)GetMemberOverride(node)?.SingleOrDefault().Value;

            node = (ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node)!;
            //Rename constructors defined in a forcedpartial
            if (@class != null)
            {
                if (@class.HasAnyAttribute([typeof(ForcePartialAttribute).FullName], out var atts))
                {
                    var att = atts!.Values.Single().Single();
                    var type = (TypeOfExpressionSyntax)att.ArgumentList!.Arguments[0].Expression;
                    var typeName = type.Type.ToString();
                    node = node.WithIdentifier(SyntaxFactory.Identifier(typeName));
                }
            }

            MemberReplaceType replacementType = MemberReplaceType.All;
            if (memberOverride != null)
            {
                var atts = replacementType.HasFlag(MemberReplaceType.Attributes) ? SyntaxFactory.List([SyntaxFactory.AttributeList(SeparatedSyntaxList.Create([.. memberOverride.AttributeLists.SelectMany(a => a.Attributes).Concat(node.AttributeLists.SelectMany(a => a.Attributes))]))]) : node.AttributeLists;
                var newNode = node
                    .WithBody(replacementType.HasFlag(MemberReplaceType.Body) ? memberOverride.Body : node.Body)
                    .WithExpressionBody(replacementType.HasFlag(MemberReplaceType.Body) ? memberOverride.ExpressionBody : node.ExpressionBody)
                    .WithModifiers(replacementType.HasFlag(MemberReplaceType.Body) ? memberOverride.Modifiers : node.Modifiers);
                if (newNode.Body != null || newNode.ExpressionBody != null)
                {
                    newNode = newNode.ReplaceToken(newNode.SemicolonToken, SyntaxFactory.MissingToken(SyntaxKind.None));
                }
                if (atts.Any(a => a.Attributes.Count > 0))
                    newNode = newNode.WithAttributeLists(atts);
                newNode = newNode.WithLeadingTrivia(node.GetLeadingTrivia())
                   .WithTrailingTrivia(node.GetTrailingTrivia());
                //_pendingVisits.Add(newNode);
                return newNode;
            }
            return node;
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var memberOverride = (MethodDeclarationSyntax?)GetMemberOverride(node)?.SingleOrDefault().Value;
            // Check if the property is an expression-bodied property (has an arrow clause)
            // and there is a conditional expression in the body.
            // Convet to a block so we can define a local variable in it
            if (node.ExpressionBody != null && node.ExpressionBody.FindDescendant<ConditionalAccessExpressionSyntax>().Any(e => IsRewiteCandidate(e)))
            {
                BeginBlockVariables();
                var expression = Visit(node.ExpressionBody.Expression)!;

                //StatementSyntax statement;

                //if (expression is StatementSyntax ss)
                //{
                //    statement = ss;
                //}
                //// If the method returns void, the expression becomes an expression statement
                //else if (node.ReturnType.Kind() == SyntaxKind.VoidKeyword)
                //{
                //    statement = SyntaxFactory.ExpressionStatement((ExpressionSyntax)expression);
                //}
                //// Otherwise, it becomes a return statement
                //else
                //{
                //    statement = SyntaxFactory.ReturnStatement((ExpressionSyntax)expression);
                //}

                var newBlock = (BlockSyntax)EndBlockVariables((CSharpSyntaxNode)expression, true, node.ReturnType.ToString() != "void");

                // Replace the expression body and the arrow token with the new block
                // Also, remove the semicolon token that was part of the expression body
                var newNode = node
                    .WithBody(newBlock)
                    .WithExpressionBody(null) // Remove the expression body
                    .WithSemicolonToken(SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken)); // Remove the standalone semicolon

                // Use the Formatter annotation to let Roslyn handle the indentation/formatting of the new block
                node = newNode;//.WithAdditionalAnnotations(Formatter.Annotation);
            }
            else
                node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
            if (isMemberVisitFromGetMemberOverride)
                return node;
            //A method decorated with MemberOverride is removed. Only its content is used to replace the overriden member
            if (node.HasAnyAttribute([typeof(MemberReplaceAttribute).FullName], out var args))
            {
                //We want to check for ambiguity though before proceeding
                EnsureUnambiguity(node, args.Single().Value.First().ArgumentList?.Arguments.First());
                return null;
            }
            MemberReplaceType replacementType = MemberReplaceType.All;// & ~MemberReplaceType.Modifiers;
            if (memberOverride != null)
            {
                SyntaxList<AttributeListSyntax> atts = default;
                if (replacementType.HasFlag(MemberReplaceType.Modifiers))
                {
                    atts = node.AttributeLists.AddRange(memberOverride.AttributeLists.Where(e => e.Attributes.Count > 0));
                    //var attWithSpecifier = memberOverride.AttributeLists.Where(e => e.Target != null);
                    //List<(AttributeSyntax, AttributeTargetSpecifierSyntax)> attsyn = new(memberOverride.AttributeLists.SelectMany(a => a.Attributes));
                    //attsyn.AddRange(node.AttributeLists.SelectMany(a => a.Attributes));
                    //atts = SyntaxFactory.List([SyntaxFactory.AttributeList(SeparatedSyntaxList.Create(attsyn.ToArray())).WithTrailingTrivia(SyntaxFactory.LineFeed)]);
                }
                else
                {
                    atts = node.AttributeLists;
                }
                //var expressionBody = replacementType.HasFlag(MemberReplaceType.Body) ? memberOverride.ExpressionBody : node.ExpressionBody;
                SyntaxTokenList modifiers = default;
                if (replacementType.HasFlag(MemberReplaceType.Modifiers))
                {
                    //if (!memberOverride.Modifiers.Any(e => e.IsKind(SyntaxKind.ExternKeyword)) || expressionBody != null)
                    {
                        List<SyntaxToken> tokens = new List<SyntaxToken>(memberOverride.Modifiers/*.Where(e => !e.IsKind(SyntaxKind.ExternKeyword))*/);
                        if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword)))
                        {
                            tokens.Add(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.OverrideKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)));
                        }
                        if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.NewKeyword)))
                        {
                            tokens.Add(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.NewKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)));
                        }
                        modifiers = SyntaxFactory.TokenList(tokens);
                    }
                    //else
                    //{
                    //    modifiers = node.Modifiers;
                    //}
                }
                else
                {
                    modifiers = node.Modifiers;
                }
                var newNode = node
                    .WithBody(memberOverride.Body)
                    .WithExpressionBody(memberOverride.ExpressionBody)
                    .WithSemicolonToken(memberOverride.ExpressionBody != null || (memberOverride.ExpressionBody == null && memberOverride.Body == null) ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken))
                    .WithModifiers(modifiers);
                if (atts.Any(a => a.Attributes.Count > 0))
                    newNode = newNode.WithAttributeLists(atts);
                //if (newNode.Body != null || newNode.ExpressionBody != null)
                //{
                //    newNode = newNode.ReplaceToken(newNode.SemicolonToken, SyntaxFactory.MissingToken(SyntaxKind.None));
                //}
                //else if (newNode.Body == null && newNode.ExpressionBody == null)
                //{
                //    newNode = newNode.ReplaceToken(newNode.SemicolonToken, SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                //}
                newNode = newNode.WithLeadingTrivia(node.GetLeadingTrivia())
                   .WithTrailingTrivia(node.GetTrailingTrivia());
                //_pendingVisits.Add(newNode);
                return newNode;
            }
            return node;
        }

        public override SyntaxNode? VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            if (node.ExpressionBody != null && node.ExpressionBody.FindDescendant<ConditionalAccessExpressionSyntax>().Any(e => IsRewiteCandidate(e)))
            {
                BeginBlockVariables();
                var expression = (ExpressionSyntax)Visit(node.ExpressionBody.Expression)!;
                var block = (BlockSyntax)EndBlockVariables(expression, true, node.IsKind(SyntaxKind.GetAccessorDeclaration));
                var modifiers = node.Modifiers;
                var getAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithBody(block)
                    .WithAttributeLists(node.AttributeLists);
                return getAccessor.WithTriviaFrom(node);
            }
            else
                return base.VisitAccessorDeclaration(node);
        }

        AccessorListSyntax? OverrideAccessor(MemberDeclarationSyntax node, Dictionary<string, MemberDeclarationSyntax> memberOverrides, AccessorListSyntax? accessors, ref SyntaxList<AttributeListSyntax> atts, ref SyntaxTokenList modifiers)
        {
            MemberReplaceType replacementType = MemberReplaceType.All;// & ~MemberReplaceType.Modifiers;
            //var accessors = node.AccessorList;
            foreach (var kv in memberOverrides)
            {
                var memberOverride = kv.Value;
                //SyntaxTokenList modifiers = default;
                //SyntaxList<AttributeListSyntax> atts = default;
                if (replacementType.HasFlag(MemberReplaceType.Modifiers))
                {
                    atts = atts.AddRange(memberOverride.AttributeLists.Where(e => e.Attributes.Count > 0));
                }
                //else
                //{
                //    atts = node.AttributeLists;
                //}
                //if (replacementType.HasFlag(MemberReplaceType.Modifiers))
                //{
                //    List<SyntaxToken> tokens = new List<SyntaxToken>(memberOverride.Modifiers/*.Where(e => !e.IsKind(SyntaxKind.ExternKeyword))*/);
                //    if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword)))
                //    {
                //        tokens.Add(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.OverrideKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)));
                //    }
                //    if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.NewKeyword)))
                //    {
                //        tokens.Add(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.NewKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)));
                //    }
                //    modifiers = SyntaxFactory.TokenList(tokens);
                //}
                //else
                //{
                //modifiers = node.Modifiers.Remove(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                //    .Remove(SyntaxFactory.Token(SyntaxKind.InternalKeyword))
                //    .Remove(SyntaxFactory.Token(SyntaxKind.ExternKeyword))
                //    .Remove(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
                //}
                var getSet = kv.Key;
                var methodBody = (memberOverride as MethodDeclarationSyntax)?.Body;
                var getAttr = (memberOverride as MethodDeclarationSyntax)?.AttributeLists;
                var propertyGetBody = (memberOverride as PropertyDeclarationSyntax)?.AccessorList?.Accessors.FirstOrDefault(e => e.IsKind(SyntaxKind.GetAccessorDeclaration))?.Body;
                var propertySetBody = (memberOverride as PropertyDeclarationSyntax)?.AccessorList?.Accessors.FirstOrDefault(e => e.IsKind(SyntaxKind.SetAccessorDeclaration))?.Body;
                var propertyGetExpressionBody = (memberOverride as PropertyDeclarationSyntax)?.ExpressionBody ?? (memberOverride as PropertyDeclarationSyntax)?.AccessorList?.Accessors.FirstOrDefault(e => e.IsKind(SyntaxKind.GetAccessorDeclaration))?.ExpressionBody;
                var propertySetExpressionBody = (memberOverride as PropertyDeclarationSyntax)?.AccessorList?.Accessors.FirstOrDefault(e => e.IsKind(SyntaxKind.SetAccessorDeclaration))?.ExpressionBody;

                var propertyGetAttr = (memberOverride as PropertyDeclarationSyntax)?.AccessorList?.Accessors.FirstOrDefault(e => e.IsKind(SyntaxKind.GetAccessorDeclaration))?.AttributeLists ??
                    accessors?.Accessors.FirstOrDefault(e => e.IsKind(SyntaxKind.GetAccessorDeclaration))?.AttributeLists ??
                    default;
                var propertySetAttr = (memberOverride as PropertyDeclarationSyntax)?.AccessorList?.Accessors.FirstOrDefault(e => e.IsKind(SyntaxKind.SetAccessorDeclaration))?.AttributeLists ??
                    accessors?.Accessors.FirstOrDefault(e => e.IsKind(SyntaxKind.SetAccessorDeclaration))?.AttributeLists ??
                    default;

                AccessorDeclarationSyntax? getAccessor = null;
                if (propertyGetExpressionBody != null)
                {
                    getAccessor = getSet == "" || getSet == "get" || propertyGetExpressionBody != null ? SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, default, default, propertyGetExpressionBody)
                        .WithAttributeLists(propertyGetAttr)
                        //.WithModifiers(modifiers)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)) :
                        accessors?.Accessors.FirstOrDefault(e => e.IsKind(SyntaxKind.GetAccessorDeclaration));
                }
                else
                {
                    getAccessor = getSet == "" || getSet == "get" || (methodBody ?? propertySetBody) != null ? SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, methodBody ?? propertyGetBody)
                        .WithAttributeLists(propertyGetAttr)
                        //.WithModifiers(modifiers)
                        .WithSemicolonToken((methodBody ?? propertyGetBody) == null ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken)) :
                        accessors?.Accessors.FirstOrDefault(e => e.IsKind(SyntaxKind.GetAccessorDeclaration));
                }
                AccessorDeclarationSyntax? setAccessor = null;
                if (propertySetExpressionBody != null)
                {
                    setAccessor = getSet == "set" || propertySetExpressionBody != null ? SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, default, default, propertySetExpressionBody)
                        .WithAttributeLists(propertySetAttr)
                        //.WithModifiers(modifiers)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)) :
                        accessors?.Accessors.FirstOrDefault(e => e.IsKind(SyntaxKind.SetAccessorDeclaration));
                }
                else
                {
                    setAccessor = getSet == "set" || propertySetBody != null ? SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, methodBody ?? propertySetBody)
                        .WithAttributeLists(propertySetAttr)
                        //.WithModifiers(modifiers)
                        .WithSemicolonToken((methodBody ?? propertySetBody) == null ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken)) :
                        accessors?.Accessors.FirstOrDefault(e => e.IsKind(SyntaxKind.SetAccessorDeclaration));
                }
                if (getAccessor?.Body != null || getAccessor?.ExpressionBody != null || setAccessor?.Body != null || setAccessor?.ExpressionBody != null)
                {
                    modifiers = modifiers.Remove(modifiers.FirstOrDefault(e => e.IsKind(SyntaxKind.ExternKeyword)));
                }
                if ((getAccessor?.Body != null || getAccessor?.ExpressionBody != null) && (setAccessor?.Body != null || setAccessor?.ExpressionBody != null))
                {
                    modifiers = modifiers.Remove(modifiers.FirstOrDefault(e => e.IsKind(SyntaxKind.ReadOnlyKeyword)));
                }
                accessors = SyntaxFactory.AccessorList(
                    SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                    SyntaxFactory.List<AccessorDeclarationSyntax>(new[] { getAccessor, setAccessor }.Where(e => e != null)!),
                    SyntaxFactory.Token(SyntaxKind.CloseBraceToken));
            }
            return accessors;
        }

        public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var memberOverrides = GetMemberOverride(node);
            // Check if the property is an expression-bodied property (has an arrow clause)
            // and there is a conditional expression in the body.
            // Convert to a block so we can define a local variable in it
            if (node.ExpressionBody != null && node.ExpressionBody.FindDescendant<ConditionalAccessExpressionSyntax>().Any(e => IsRewiteCandidate(e)))
            {
                BeginBlockVariables();
                var expression = (ExpressionSyntax)Visit(node.ExpressionBody.Expression)!;
                var block = (BlockSyntax)EndBlockVariables(expression, true, true);
                var propertyName = node.Identifier.Text;
                var propertyType = node.Type;
                var modifiers = node.Modifiers;
                var getAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithBody(block);
                var accessorList = SyntaxFactory.AccessorList(
                    SyntaxFactory.List(new AccessorDeclarationSyntax[] { getAccessor }));
                var newPropertyDeclaration = SyntaxFactory.PropertyDeclaration(
                    attributeLists: node.AttributeLists,
                    modifiers: modifiers,
                    type: propertyType,
                    explicitInterfaceSpecifier: node.ExplicitInterfaceSpecifier,
                    identifier: node.Identifier,
                    accessorList: accessorList, // Use the new accessor list
                    expressionBody: null,       // Remove the expression body
                    initializer: node.Initializer
                );
                // Copy trivia (whitespace, comments) from the original node
                node = newPropertyDeclaration.WithTriviaFrom(node);
            }
            else
                node = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node)!;
            if (isMemberVisitFromGetMemberOverride)
                return node;
            //A property decorated with MemberOverride is removed. Only its content is used to replace the overriden member
            if (node.HasAnyAttribute([typeof(MemberReplaceAttribute).FullName], out _))
                return null;
            if (memberOverrides?.Any() ?? false)
            {
                SyntaxList<AttributeListSyntax> atts = node.AttributeLists;
                SyntaxTokenList modifiers = node.Modifiers;
                var accessors = OverrideAccessor(node, memberOverrides, node.AccessorList, ref atts, ref modifiers);
                var newNode = node.WithAccessorList(accessors)
                    .WithExpressionBody(accessors != null ? null : node.ExpressionBody)
                    .WithSemicolonToken(accessors != null ? SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken) : node.SemicolonToken)
                    .WithAttributeLists(atts)
                    .WithModifiers(modifiers);
                //if (atts.Any(a => a.Attributes.Count > 0))
                //newNode = newNode.WithAttributeLists(atts);
                newNode = newNode.WithLeadingTrivia(node.GetLeadingTrivia())
                   .WithTrailingTrivia(node.GetTrailingTrivia());
                return newNode;
            }
            return node;
        }

        public override SyntaxNode? VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            var memberOverrides = GetMemberOverride(node);
            // Check if the property is an expression-bodied property (has an arrow clause)
            // and there is a conditional expression in the body.
            // Convert to a block so we can define a local variable in it
            if (node.ExpressionBody != null && node.ExpressionBody.FindDescendant<ConditionalAccessExpressionSyntax>().Any(e => IsRewiteCandidate(e)))
            {
                BeginBlockVariables();
                var expression = (ExpressionSyntax)Visit(node.ExpressionBody.Expression)!;
                var block = (BlockSyntax)EndBlockVariables(expression, true, true);
                //var propertyName = node.Identifier.Text;
                var propertyType = node.Type;
                var modifiers = node.Modifiers;
                var getAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithBody(block);
                var accessorList = SyntaxFactory.AccessorList(
                    SyntaxFactory.List(new AccessorDeclarationSyntax[] { getAccessor }));
                var newPropertyDeclaration = SyntaxFactory.IndexerDeclaration(
                    attributeLists: node.AttributeLists,
                    modifiers: modifiers,
                    type: propertyType,
                    explicitInterfaceSpecifier: node.ExplicitInterfaceSpecifier,
                    parameterList: node.ParameterList,
                    //identifier: node.Identifier,
                    accessorList: accessorList, // Use the new accessor list
                    expressionBody: null       // Remove the expression body
                                               //initializer: node.Initializer
                );
                // Copy trivia (whitespace, comments) from the original node
                node = newPropertyDeclaration.WithTriviaFrom(node);
            }
            else
                node = (IndexerDeclarationSyntax)base.VisitIndexerDeclaration(node)!;
            if (isMemberVisitFromGetMemberOverride)
                return node;
            //A property decorated with MemberOverride is removed. Only its content is used to replace the overriden member
            if (node.HasAnyAttribute([typeof(MemberReplaceAttribute).FullName], out _))
                return null;
            if (memberOverrides?.Any() ?? false)
            {
                SyntaxList<AttributeListSyntax> atts = node.AttributeLists;
                SyntaxTokenList modifiers = node.Modifiers;
                var accessors = OverrideAccessor(node, memberOverrides, node.AccessorList, ref atts, ref modifiers);
                var newNode = node.WithAccessorList(accessors)
                    //.WithExpressionBody(expressionBody)
                    .WithAttributeLists(atts)
                    .WithModifiers(modifiers);
                //if (atts.Any(a => a.Attributes.Count > 0))
                //newNode = newNode.WithAttributeLists(atts);
                newNode = newNode.WithLeadingTrivia(node.GetLeadingTrivia())
                   .WithTrailingTrivia(node.GetTrailingTrivia());
                return newNode;
            }
            return node;
        }

        public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (node.ToFullString().Contains("public static readonly string Empty"))
            {

            }
            //A field decorated with MemberOverride is removed. Only its content is used to replace the overriden member
            if (node.HasAnyAttribute([typeof(MemberReplaceAttribute).FullName], out _))
                return null;
            if (node.Declaration.Variables.Count == 1)
            {
                var memberOverrides = GetMemberOverride(node);
                //MemberReplaceType replacementType = MemberReplaceType.All;// & ~MemberReplaceType.Modifiers;
                if (memberOverrides?.Any() ?? false)
                {
                    var moverride = memberOverrides.Single().Value;
                    if (moverride.IsKind(SyntaxKind.FieldDeclaration))
                    {
                        var mov = (FieldDeclarationSyntax)moverride;
                        var originalVariable = node.Declaration.Variables.Single();
                        var overrideVariable = mov.Declaration.Variables.Single();
                        originalVariable = originalVariable.WithInitializer(overrideVariable.Initializer);
                        var newDeclaration = SyntaxFactory.VariableDeclaration(node.Declaration.Type, SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>([originalVariable]));
                        var newNode = node.WithDeclaration(newDeclaration);
                        SyntaxList<AttributeListSyntax> atts = node.AttributeLists;
                        if (moverride.AttributeLists.Count > 0)
                        {
                            atts = atts.AddRange(moverride.AttributeLists.Where(e => e.Attributes.Count > 0));
                        }
                        newNode = newNode.WithAttributeLists(atts);
                        return newNode;
                    }
                    else
                    {
                        var property = SyntaxFactory.PropertyDeclaration(
                            node.AttributeLists,
                            node.Modifiers,
                            node.Declaration.Type,
                            null,
                            node.Declaration.Variables.Single().Identifier,
                            null, null,
                            node.Declaration.Variables.Single().Initializer);
                        SyntaxList<AttributeListSyntax> atts = node.AttributeLists;
                        SyntaxTokenList modifiers = node.Modifiers;
                        var accessors = OverrideAccessor(node, memberOverrides, SyntaxFactory.AccessorList(default), ref atts, ref modifiers);
                        var newNode = property.WithAccessorList(accessors)
                            //.WithExpressionBody(expressionBody)
                            .WithAttributeLists(atts)
                            .WithModifiers(modifiers);
                        //if (atts.Any(a => a.Attributes.Count > 0))
                        //newNode = newNode.WithAttributeLists(atts);
                        newNode = newNode.WithLeadingTrivia(node.GetLeadingTrivia())
                           .WithTrailingTrivia(node.GetTrailingTrivia());
                        return newNode;
                    }
                    //    var atts = replacementType.HasFlag(MemberReplaceType.Attributes) ? SyntaxFactory.List([SyntaxFactory.AttributeList(SeparatedSyntaxList.Create([.. memberOverride.AttributeLists.SelectMany(a => a.Attributes).Concat(node.AttributeLists.SelectMany(a => a.Attributes))]))]) : node.AttributeLists;
                    //var newNode = node.WithDeclaration(memberOverride.Declaration)
                    //    .WithModifiers(replacementType.HasFlag(MemberReplaceType.Modifiers) ? (!memberOverride.Modifiers.Any(e => e.IsKind(SyntaxKind.ExternKeyword)) ? SyntaxFactory.TokenList(memberOverride.Modifiers.Where(e => !e.IsKind(SyntaxKind.ExternKeyword))) : memberOverride.Modifiers) : node.Modifiers);
                    //if (atts.Any(a => a.Attributes.Count > 0))
                    //    newNode = newNode.WithAttributeLists(atts);
                    //newNode = newNode.WithLeadingTrivia(node.GetLeadingTrivia())
                    //   .WithTrailingTrivia(node.GetTrailingTrivia());
                    //_pendingVisits.Add(newNode);
                    //return newNode;
                }
            }
            return base.VisitFieldDeclaration(node);
        }

        public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            var exp = Visit(node.Expression);
            if (exp == null)
                return null;
            if (exp is StatementSyntax)
                return exp.WithTriviaFrom(node);
            return SyntaxFactory.ExpressionStatement((ExpressionSyntax)exp).WithTriviaFrom(node);
            //return base.VisitExpressionStatement(node);
        }

        public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            //Optimize/Concantenate all u8 literals to new giant u8
            //if (node.Left.IsKind(SyntaxKind.Utf8StringLiteralExpression) && node.Right.IsKind(SyntaxKind.Utf8StringLiteralExpression) && node.IsKind(SyntaxKind.AddExpression))
            //{
            //    var leftLiteral = (LiteralExpressionSyntax)node.Left;
            //    var rightLiteral = (LiteralExpressionSyntax)node.Right;
            //    var concated = leftLiteral.Token.ValueText + rightLiteral.Token.ValueText;
            //    var newNode = SyntaxFactory.LiteralExpression(SyntaxKind.Utf8StringLiteralExpression, SyntaxFactory.Literal(concated, concated));
            //    return newNode;
            //}

            //VisitConditionalAccessExpression will handle both ConditionalAccessExpression and its CoalesceExpression
            //eg value?.GetHashCode() ?? 0
            if (node.IsKind(SyntaxKind.CoalesceExpression) && node.Left.IsKind(SyntaxKind.ConditionalAccessExpression) && node.Left is ConditionalAccessExpressionSyntax ce && IsRewiteCandidate(ce))
            {
                return Visit(node.Left);
            }
            return base.VisitBinaryExpression(node);
        }

        //Dictionary<ConditionalAccessExpressionSyntax, string> conditionalVariables = new Dictionary<ConditionalAccessExpressionSyntax, string>();
        struct DefineBlockVariable
        {
            public ITypeSymbol? Type;
            public ExpressionSyntax? Initializer;
            public bool InitializerTypeInferenceOnly;
            public int? InsertionIndex;
        }
        Stack<Dictionary<string, DefineBlockVariable>> defineBlockVariables = new();
        int nextBlockVariableIndex = 1;
        void BeginBlockVariables()
        {
            defineBlockVariables.Push(new Dictionary<string, DefineBlockVariable>());
        }

        CSharpSyntaxNode EndBlockVariables(CSharpSyntaxNode node,
            bool convertToBlock = false,
            bool blockHasReturn = false,
            ITypeSymbol? lamdaReturnType = null,
            StatementSyntax? lamdaBlockStatement = null)
        {
            var variables = defineBlockVariables.Pop();
            if (variables.Count > 0)
            {
                List<(string, int, LocalDeclarationStatementSyntax)> declarations = new();
                foreach (var variable in variables)
                {
                    LocalDeclarationStatementSyntax _var_;
                    if (variable.Value.Type != null)
                    {
                        _var_ = SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(
                                SyntaxFactory.IdentifierName(variable.Value.Type.ToString()))
                            .WithVariables(
                                SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                    SyntaxFactory.VariableDeclarator(
                                        SyntaxFactory.Identifier(variable.Key).WithTrailingTrivia(SyntaxFactory.Space))
                                    .WithInitializer(
                                        SyntaxFactory.EqualsValueClause(
                                            (variable.Value.Initializer == null ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression) :
                                            variable.Value.Initializer).WithLeadingTrivia(SyntaxFactory.Space)
                                        )
                                    )
                                    .WithLeadingTrivia(SyntaxFactory.Space))))
                        .WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Tab, SyntaxFactory.Tab, SyntaxFactory.Tab))
                        .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                    }
                    else
                    {
                        _var_ = SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(
                                SyntaxFactory.IdentifierName(
                                    SyntaxFactory.Identifier(
                                        SyntaxFactory.TriviaList(),
                                        SyntaxKind.VarKeyword,
                                        "var",
                                        "var",
                                        SyntaxFactory.TriviaList()
                                    )
                                ).WithTrailingTrivia(SyntaxFactory.Space)
                            )
                            .WithVariables(
                                SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                    SyntaxFactory.VariableDeclarator(
                                        SyntaxFactory.Identifier(variable.Key)
                                    ).WithTrailingTrivia(SyntaxFactory.Space)
                                    .WithInitializer(
                                        SyntaxFactory.EqualsValueClause(
                                            variable.Value.Initializer == null ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression) :
                                            variable.Value.InitializerTypeInferenceOnly ? SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName("Global"),
                                                    SyntaxFactory.IdentifierName("TypeInference"))
                                                .WithLeadingTrivia(SyntaxFactory.Space))
                                            .WithArgumentList(
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                                        SyntaxFactory.Argument(
                                                            variable.Value.Initializer.WithoutTrivia())
                                                    )
                                                )
                                            ) : variable.Value.Initializer
                                        )
                                    )
                                )
                            )
                        )
                        .WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Tab, SyntaxFactory.Tab, SyntaxFactory.Tab))
                        .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                    }
                    declarations.Add((variable.Key, variable.Value.InsertionIndex ?? -1, _var_));
                }
                if (convertToBlock)
                {
                    node = SyntaxFactory.Block((StatementSyntax[])[
                            .. declarations.Select(s => s.Item3),
                            blockHasReturn ? SyntaxFactory.ReturnStatement((ExpressionSyntax)node.WithLeadingTrivia(SyntaxFactory.Space)): node is StatementSyntax ss ? ss : SyntaxFactory.ExpressionStatement((ExpressionSyntax)node.WithLeadingTrivia(SyntaxFactory.Space))
                        ]);
                }
                else if (node.IsKind(SyntaxKind.ArrowExpressionClause))
                {
                    node = SyntaxFactory.Block((StatementSyntax[])[.. declarations.Select(s => s.Item3), SyntaxFactory.ReturnStatement(((ArrowExpressionClauseSyntax)node).Expression.WithLeadingTrivia(SyntaxFactory.Space))]);
                }
                else if (node.IsKind(SyntaxKind.SimpleLambdaExpression))
                {
                    var lamda = (SimpleLambdaExpressionSyntax)node;
                    bool hasReturnType = lamdaReturnType != null && lamdaReturnType.SpecialType != SpecialType.System_Void;
                    var prt = lamda.WithExpressionBody(null).WithBlock(
                                        SyntaxFactory.Block((StatementSyntax[])[.. declarations.Select(s => s.Item3), hasReturnType ? SyntaxFactory.ReturnStatement(lamda.ExpressionBody!) : lamdaBlockStatement ?? SyntaxFactory.ExpressionStatement(lamda.ExpressionBody!)])
                                        .WithOpenBraceToken(
                                            SyntaxFactory.Token(
                                                SyntaxFactory.TriviaList(
                                                    SyntaxFactory.Whitespace("    ")),
                                                SyntaxKind.OpenBraceToken,
                                                SyntaxFactory.TriviaList(
                                                    SyntaxFactory.CarriageReturnLineFeed)))
                                        .WithCloseBraceToken(
                                            SyntaxFactory.Token(
                                                SyntaxFactory.TriviaList(
                                                    new[]{
                                                        SyntaxFactory.Whitespace("        "),
                                                        SyntaxFactory.CarriageReturnLineFeed,
                                                        SyntaxFactory.Whitespace("    ")}),
                                                SyntaxKind.CloseBraceToken,
                                                SyntaxFactory.TriviaList())));
                    node = prt;
                }
                else if (node.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
                {
                    var lamda = (ParenthesizedLambdaExpressionSyntax)node;
                    bool hasReturnType = lamdaReturnType != null && lamdaReturnType.SpecialType != SpecialType.System_Void;
                    var prt = lamda.WithExpressionBody(null).WithBlock(
                                        SyntaxFactory.Block((StatementSyntax[])[.. declarations.Select(s => s.Item3), hasReturnType ? SyntaxFactory.ReturnStatement(lamda.ExpressionBody!) : lamdaBlockStatement ?? SyntaxFactory.ExpressionStatement(lamda.ExpressionBody!)])
                                        .WithOpenBraceToken(
                                            SyntaxFactory.Token(
                                                SyntaxFactory.TriviaList(
                                                    SyntaxFactory.Whitespace("    ")),
                                                SyntaxKind.OpenBraceToken,
                                                SyntaxFactory.TriviaList(
                                                    SyntaxFactory.CarriageReturnLineFeed)))
                                        .WithCloseBraceToken(
                                            SyntaxFactory.Token(
                                                SyntaxFactory.TriviaList(
                                                    new[]{
                                                        SyntaxFactory.Whitespace("        "),
                                                        SyntaxFactory.CarriageReturnLineFeed,
                                                        SyntaxFactory.Whitespace("    ")}),
                                                SyntaxKind.CloseBraceToken,
                                                SyntaxFactory.TriviaList())));
                    node = prt;
                }
                else if (node.IsKind(SyntaxKind.Argument))
                {
                    var arg = ((ArgumentSyntax)node);
                    var argument = SyntaxFactory.Argument(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName("Global"),
                                                    SyntaxFactory.IdentifierName("DelegateTypeInference")))
                                            .WithArgumentList(
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.ParenthesizedLambdaExpression()
                                                            .WithBlock(
                                                                SyntaxFactory.Block(
                                                                    (StatementSyntax[])[
                                                                        ..declarations.Select(s => s.Item3),
                                                                        SyntaxFactory.ReturnStatement(arg.Expression.WithLeadingTrivia(SyntaxFactory.Space))
                                                                        ]
                                                                    )
                                                                )
                                                            )
                                                        )
                                                    )
                                                )
                                            )
                                       );
                    node = argument;
                }
                else
                {
                    //make sure we insert right above the statement that needs the local variable
                    if (node.IsKind(SyntaxKind.Block))
                    {
                        foreach (var v in declarations.Where(d => d.Item2 >= 0).OrderByDescending(d => d.Item2).ThenByDescending(d => d.Item1))
                        {
                            var reference = node!.ChildNodes().ElementAtOrDefault(v.Item2);
                            if (reference == null)
                            {
                                node = node.InsertNodesBefore(node!.ChildNodes().First(), [v.Item3]);
                            }
                            else
                                node = node.InsertNodesBefore(reference, [v.Item3]);
                        }
                        node = node.InsertNodesBefore(node!.ChildNodes().First(), declarations.Where(d => d.Item2 < 0).Select(s => s.Item3));
                    }
                    else
                        node = node.InsertNodesBefore(node!.ChildNodes().First(), declarations.Select(s => s.Item3));
                }
            }
            return node;
        }

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            BeginBlockVariables();
            var newNode = base.VisitBlock(node);
            return EndBlockVariables((CSharpSyntaxNode)newNode!);
        }

        public override SyntaxNode? VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            if (node.FindDescendant<ConditionalAccessExpressionSyntax>().Any(e => IsRewiteCandidate(e)))
            {
                BeginBlockVariables();
                var newNode = base.VisitArrowExpressionClause(node);
                return EndBlockVariables((CSharpSyntaxNode)newNode!, true);
            }
            return base.VisitArrowExpressionClause(node);
        }

        public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            if (node.ExpressionBody != null && node.FindDescendant<ConditionalAccessExpressionSyntax>().Any(e => IsRewiteCandidate(e)))
            {
                var lamdaSymbol = GetSymbolInfo(node);
                BeginBlockVariables();
                var newExpression = Visit(node.ExpressionBody);
                CSharpSyntaxNode newNode;
                StatementSyntax? statement = null;
                if (newExpression is StatementSyntax ss) //ConditionalAccessExpressionSyntax converted to IfStatement
                {
                    statement = ss;
                    newNode = node.Update(
                        VisitList(node.AttributeLists),
                        VisitList(node.Modifiers),
                        (ParameterSyntax?)Visit(node.Parameter) ?? throw new ArgumentNullException("parameter"),
                        VisitToken(node.ArrowToken),
                        (BlockSyntax?)Visit(node.Block),
                        null);
                }
                else
                {
                    newNode = node.Update(
                        VisitList(node.AttributeLists),
                        VisitList(node.Modifiers),
                        (ParameterSyntax?)Visit(node.Parameter) ?? throw new ArgumentNullException("parameter"),
                        VisitToken(node.ArrowToken),
                        (BlockSyntax?)Visit(node.Block),
                        (ExpressionSyntax?)newExpression);
                }
                //var newNode = base.VisitSimpleLambdaExpression(node);
                return EndBlockVariables((CSharpSyntaxNode)newNode!, lamdaReturnType: ((IMethodSymbol?)lamdaSymbol)?.ReturnType);
            }
            return base.VisitSimpleLambdaExpression(node);
        }

        public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            if (node.ExpressionBody != null && node.FindDescendant<ConditionalAccessExpressionSyntax>().Any())
            {
                var lamdaSymbol = GetSymbolInfo(node);
                BeginBlockVariables();
                var newExpression = Visit(node.ExpressionBody);
                CSharpSyntaxNode newNode;
                StatementSyntax? statement = null;
                if (newExpression is StatementSyntax ss) //ConditionalAccessExpressionSyntax converted to IfStatement
                {
                    statement = ss;
                    newNode = node.Update(
                       VisitList(node.AttributeLists),
                       VisitList(node.Modifiers),
                       (TypeSyntax?)Visit(node.ReturnType),
                       ((ParameterListSyntax)Visit(node.ParameterList)!) ?? throw new ArgumentNullException("parameterList"),
                       VisitToken(node.ArrowToken),
                       (BlockSyntax?)Visit(node.Block),
                       null);
                }
                else
                {
                    newNode = node.Update(
                       VisitList(node.AttributeLists),
                       VisitList(node.Modifiers),
                       (TypeSyntax?)Visit(node.ReturnType),
                       ((ParameterListSyntax)Visit(node.ParameterList)!) ?? throw new ArgumentNullException("parameterList"),
                       VisitToken(node.ArrowToken),
                       (BlockSyntax?)Visit(node.Block),
                       (ExpressionSyntax?)newExpression!);
                }
                //var newNode = base.VisitParenthesizedLambdaExpression(node);
                return EndBlockVariables(newNode, lamdaReturnType: ((IMethodSymbol?)lamdaSymbol)?.ReturnType, lamdaBlockStatement: statement);
            }
            return base.VisitParenthesizedLambdaExpression(node);
        }
        //public override SyntaxNode? VisitConstructorInitializer(ConstructorInitializerSyntax node)
        //{
        //    if (node.FindDescendant<ConditionalAccessExpressionSyntax>().Any())
        //    {
        //        BeginBlockVariables();
        //        var newNode = base.VisitConstructorInitializer(node);
        //        var retNode = EndBlockVariables((CSharpSyntaxNode)newNode!, out var _var_);
        //        if (_var_.Count > 0)
        //        {

        //        }
        //        return retNode;

        //    }
        //    return base.VisitConstructorInitializer(node);
        //}

        public override SyntaxNode? VisitArgument(ArgumentSyntax node)
        {
            if (node.FindClosestParent<ConstructorInitializerSyntax>() != null && node.FindDescendant<ConditionalAccessExpressionSyntax>().Any(e => IsRewiteCandidate(e)))
            {
                BeginBlockVariables();
                var newNode = base.VisitArgument(node);
                var retNode = EndBlockVariables((CSharpSyntaxNode)newNode!);
                return retNode;

            }
            return base.VisitArgument(node);
        }

        public override SyntaxNode? VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            return VisitConditionalAccessExpression(node, null);
        }

        public SyntaxNode? VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node, ConditionalAccessExpressionSyntax? parentCondition = null)
        {
            if (parentCondition == null && !IsRewiteCandidate(node))
                return base.VisitConditionalAccessExpression(node);
            //no block to define temp variable in
            if (defineBlockVariables.Count == 0)
                return node;
            //if (node.Parent is ConditionalAccessExpressionSyntax)
            //{
            //    return base.VisitConditionalAccessExpression(node);
            //}
            //var temporaryIdentifierName = $"t";
            //bool hasResult = true;
            ExpressionSyntax Combine(ExpressionSyntax lhs, ExpressionSyntax rhs)
            {
                if (rhs is InvocationExpressionSyntax conditionalInvoke)
                {
                    if (conditionalInvoke.Expression is MemberBindingExpressionSyntax mb)
                    {
                        var memberAccess = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            lhs,
                            SyntaxFactory.Token(SyntaxKind.DotToken), mb.Name);
                        var ret = SyntaxFactory.InvocationExpression(memberAccess, conditionalInvoke.ArgumentList);
                        AssociateSyntaxFactoryNode(conditionalInvoke.ArgumentList, ret.ArgumentList);
                        return ret;
                    }
                    else if (conditionalInvoke.Expression is MemberAccessExpressionSyntax ma)
                    {
                        var memberAccess = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            Combine(lhs, ma.Expression),
                            SyntaxFactory.Token(SyntaxKind.DotToken), ma.Name);
                        var ret = SyntaxFactory.InvocationExpression(memberAccess, conditionalInvoke.ArgumentList);
                        AssociateSyntaxFactoryNode(conditionalInvoke.ArgumentList, ret.ArgumentList);
                        return ret;
                    }
                }
                else if (rhs is MemberBindingExpressionSyntax member)
                {
                    var ret = SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        lhs,
                        SyntaxFactory.Token(SyntaxKind.DotToken), member.Name);
                    AssociateSyntaxFactoryNode(ret.Name, member.Name);
                    return ret;
                }
                else if (rhs is MemberAccessExpressionSyntax ma)
                {
                    var ret = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            Combine(lhs, ma.Expression),
                            SyntaxFactory.Token(SyntaxKind.DotToken), ma.Name);
                    AssociateSyntaxFactoryNode(ret.Name, ma.Name);
                    return ret;
                }
                else if (rhs is ConditionalAccessExpressionSyntax cd)
                {
                    var m = Combine(lhs, cd.Expression);
                    var ret = cd.ReplaceNode(cd.Expression, m);
                    //AssociateSyntaxFactoryNode(ret.Expression, cd.Expression);
                    return ret;
                }
                else if (rhs is ElementAccessExpressionSyntax ae)
                {
                    var newNode = Combine(lhs, ae.Expression);
                    var ret = ae.ReplaceNode(ae.Expression, newNode);
                    //AssociateSyntaxFactoryNode(ret.Expression, ae.Expression);
                    return ret;
                }
                else if (rhs is ElementBindingExpressionSyntax ab)
                {
                    var m = SyntaxFactory.ElementAccessExpression(lhs, ab.ArgumentList);
                    AssociateSyntaxFactoryNode(m.ArgumentList, ab.ArgumentList);
                    return m;
                }
                else if (rhs is AssignmentExpressionSyntax asm)
                {
                    //hasResult = false;
                    var newNode = Combine(lhs, asm.Left);
                    var ret = asm.ReplaceNode(asm.Left, newNode);
                    AssociateSyntaxFactoryNode(ret.Right, asm.Right);
                    return ret;
                }
                throw null!;
                //return null;
            }
            //if (node.ToString().Contains("waiter?.TryStart()"))
            //{

            //}
            //if (node.ToFullString().Contains("type?.IsTypeBuilder()"))
            //{

            //}
            var lhsType = GetTypeSymbol(node.Expression);
            var type = GetTypeSymbol(parentCondition ?? node);
            bool typeIsVoid = type != null && type.SpecialType == SpecialType.System_Void;
            ExpressionSyntax? whenNull = null;
            if (node.Parent.IsKind(SyntaxKind.CoalesceExpression))
            {
                var binary = (BinaryExpressionSyntax)node.Parent;
                whenNull = binary.Right;
            }
            else
            {
                if (type != null)
                {
                    if (type.SpecialType != SpecialType.System_Void)
                    {
                        //if we have an expression like this
                        //if (cachedData._systemTimeZones?.TryGetValue(id, out value) is true)
                        //We typically rewrite as 
                        //if (((tempVariable = cachedData._systemTimeZones) != null ? tempVariable.TryGetValue(id, out value) : null) is true) {}
                        //But dotnet is unable to keeptrack of the fact that the out value was assigned if the condition is true and stil complains of unassigned value
                        //Unless we replace the ": null" with ": false"
                        if (type.IsNullable(out var primitiveType) &&
                            primitiveType!.IsType("System.Boolean") &&
                            ((parentCondition ?? node).Parent is BinaryExpressionSyntax || (parentCondition ?? node).Parent is IsPatternExpressionSyntax) &&
                            (parentCondition ?? node).FindClosestParent<IfStatementSyntax>() != null/* &&
                            (parentCondition ?? node).FindDescendant<ArgumentSyntax>(isCandidate: e => e.RefKindKeyword.ValueText == "out").Any()*/)
                        {
                            whenNull = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                        }
                        else
                        {
                            whenNull = type.IsValueType ? SyntaxFactory.DefaultExpression(SyntaxFactory.IdentifierName(type.ToString())) :
                                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
                        }
                    }
                }
            }
            //if (false)
            //{
            //    ExpressionSyntax whenNotNull = (ExpressionSyntax)Visit(Combine(SyntaxFactory.IdentifierName($"{temporaryIdentifierName}"), node.WhenNotNull));
            //    var delegateWhenNotNull = SyntaxFactory.ParenthesizedLambdaExpression(
            //        SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList((SyntaxFactory.Parameter(SyntaxFactory.Identifier($"{temporaryIdentifierName}"))))),
            //        whenNotNull
            //    );
            //    var execute = SyntaxFactory.InvocationExpression(
            //        SyntaxFactory.MemberAccessExpression(
            //            SyntaxKind.SimpleMemberAccessExpression,
            //            SyntaxFactory.IdentifierName("Global"),
            //            SyntaxFactory.IdentifierName("IfNotNull")
            //            //SyntaxFactory.IdentifierName($"IfNotNull{(!hasResult ? "Void" : "")}")
            //            //SyntaxFactory.GenericName(SyntaxFactory.Identifier("IfNotNull"), SyntaxFactory.TypeArgumentList())
            //            ),
            //        SyntaxFactory.ArgumentList(
            //            SyntaxFactory.SeparatedList<ArgumentSyntax>(new[]
            //                {
            //                SyntaxFactory.Argument((ExpressionSyntax)Visit(node.Expression.WithoutLeadingTrivia().WithoutTrailingTrivia())),
            //                SyntaxFactory.Argument(delegateWhenNotNull),
            //                whenNull == null ? null : SyntaxFactory.Argument(whenNull)
            //                }.Where(e => e != null).ToArray()!
            //                )))
            //        .WithLeadingTrivia(node.Expression.GetLeadingTrivia())
            //        .WithTrailingTrivia(node.Expression.GetTrailingTrivia());
            //    return execute;
            //}
            //else
            //{
            var varName = $"__ca{nextBlockVariableIndex++}__";
            var statementContainer = node.FindClosestParent<StatementSyntax>() ?? parentCondition?.FindClosestParent<StatementSyntax>();
            if (statementContainer == null)
            {
                if (replacements.TryGetValue(node, out var originalNode))
                {
                    statementContainer = originalNode.FindClosestParent<StatementSyntax>();
                }
            }
            if (statementContainer != null)
            {
                if (!statementContainer.Parent.IsKind(SyntaxKind.Block))
                {
                    while (statementContainer.Parent is StatementSyntax st)
                    {
                        statementContainer = st;
                        if (statementContainer.Parent.IsKind(SyntaxKind.Block))
                            break;
                    }
                    if (!statementContainer.Parent.IsKind(SyntaxKind.Block))
                        statementContainer = null;
                }
            }
            int? insertAt = statementContainer != null ? Array.IndexOf(statementContainer.Parent!.ChildNodes().ToArray(), statementContainer) : null;
            var currentBlockVariables = defineBlockVariables.Peek();
            if (insertAt == null)
            {
                if (currentBlockVariables.Count > 0)
                {
                    insertAt = currentBlockVariables.Last().Value.InsertionIndex;
                }
                else
                {

                }
            }
            currentBlockVariables.Add(varName, new DefineBlockVariable
            {
                Type = lhsType,
                Initializer = node.Expression,
                InitializerTypeInferenceOnly = true,
                InsertionIndex = insertAt
            });

            CSharpSyntaxNode whenNotNull;
            var derefVariableName = varName;
            if (lhsType != null && lhsType.IsValueType)
            {
                derefVariableName += ".Value";
            }
            if (node.WhenNotNull.IsKind(SyntaxKind.ConditionalAccessExpression))
            {
                whenNotNull = (CSharpSyntaxNode)VisitConditionalAccessExpression((ConditionalAccessExpressionSyntax)Combine(SyntaxFactory.IdentifierName(derefVariableName), node.WhenNotNull)!, parentCondition ?? node)!;
            }
            else
            {
                whenNotNull = Combine(SyntaxFactory.IdentifierName(derefVariableName), (ExpressionSyntax)Visit(node.WhenNotNull)!)!;
            }
            bool isIfStatement = node.Parent is ExpressionStatementSyntax;
            bool hasReturn = false;
            ITypeSymbol? conditionalType = GetTypeSymbol(node.WhenNotNull);
            if (node.Parent == null && parentCondition != null)
            {
                isIfStatement = parentCondition.Parent is ExpressionStatementSyntax;
            }
            if (node.Parent.IsKind(SyntaxKind.ArrowExpressionClause))
            {
                if (node.Parent.Parent.IsKind(SyntaxKind.MethodDeclaration))
                {
                    var method = (MethodDeclarationSyntax)node.Parent.Parent;
                    if (method.ReturnType.ToString() == "void")
                    {
                        isIfStatement = true;
                    }
                }
            }
            //else if (node.Parent.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
            //{
            //    if (node.Parent.Parent.IsKind(SyntaxKind.Argument))
            //    {
            //        var argument = (ArgumentSyntax)node.Parent.Parent;
            //        var argInfo = GetSymbolInfo(argument.Expression);
            //        var invocation = (InvocationExpressionSyntax?)argument.Parent.Parent;

            //    }
            //}
            CSharpSyntaxNode newNode;
            if (typeIsVoid || whenNotNull is StatementSyntax statement || isIfStatement)
            {
                var blockedStatement = SyntaxFactory.SingletonList<StatementSyntax>(whenNotNull is StatementSyntax ss ? ss : SyntaxFactory.ExpressionStatement((ExpressionSyntax)whenNotNull));
                if (hasReturn)
                {
                    //blockedStatement = SyntaxFactory.ReturnStatement(blockedStatement.Single().Ex);
                }
                var if_ = SyntaxFactory.IfStatement(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.NotEqualsExpression,
                        SyntaxFactory.ParenthesizedExpression(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.IdentifierName(varName),
                                (ExpressionSyntax)Visit(node.Expression.WithoutLeadingTrivia().WithoutTrailingTrivia())!
                            )
                        ),
                        SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                    ),
                    SyntaxFactory.Block(blockedStatement));
                newNode = if_;
            }
            else
            {
                var execute = SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.ConditionalExpression(
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.NotEqualsExpression,
                            SyntaxFactory.ParenthesizedExpression(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName(varName).WithTrailingTrivia(SyntaxFactory.Space),
                                    (ExpressionSyntax)Visit(node.Expression.WithoutLeadingTrivia().WithoutTrailingTrivia().WithLeadingTrivia(SyntaxFactory.Space))!
                                )
                            ).WithTrailingTrivia(SyntaxFactory.Space),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression).WithLeadingTrivia(SyntaxFactory.Space)
                        ).WithTrailingTrivia(SyntaxFactory.Space),
                        (ExpressionSyntax)whenNotNull.WithLeadingTrivia(SyntaxFactory.Space),
                        (whenNull ?? (ExpressionSyntax)SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)).WithLeadingTrivia(SyntaxFactory.Space)
                    )
                );
                if (node.Parent is StatementSyntax || node.Parent.IsKind(SyntaxKind.ArrowExpressionClause) || node.Parent.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
                {
                    newNode = SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(
                            SyntaxFactory.Identifier(
                                SyntaxFactory.TriviaList(),
                                SyntaxKind.UnderscoreToken,
                                "_",
                                "_",
                                SyntaxFactory.TriviaList())),
                        execute);
                }
                else
                {
                    newNode = execute;
                }
            }
            return newNode;
            //}
        }

        //public /*override*/ SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node)
        //{
        //    var rhsType = GetExpressionBoundMember(node.Right);
        //    var lhsType = GetExpressionBoundMember(node.Left);
        //    var assignmentType =
        //        (rhsType as ILocalSymbol)?.Type ??
        //        (rhsType as IFieldSymbol)?.Type ??
        //        (rhsType as IPropertySymbol)?.Type ??
        //        (rhsType as IMethodSymbol)?.ReturnType ??
        //        (rhsType as ITypeSymbol) ??

        //        (lhsType as ILocalSymbol)?.Type ??
        //        (lhsType as IFieldSymbol)?.Type ??
        //        (lhsType as IPropertySymbol)?.Type ??
        //        (lhsType as IMethodSymbol)?.ReturnType ??
        //        (lhsType as ITypeSymbol);

        //    if (assignmentType != null &&
        //            assignmentType.IsJsNativeNumeric() &&
        //            !node.Left.IsKind(SyntaxKind.IdentifierName) &&
        //            !node.OperatorToken.IsKind(SyntaxKind.EqualsToken) &&
        //            !node.OperatorToken.IsKind(SyntaxKind.QuestionQuestionEqualsToken))
        //    {
        //        var kind = node.OperatorToken.ValueText switch
        //        {
        //            "+=" => SyntaxKind.AddExpression,
        //            "-=" => SyntaxKind.SubtractExpression,
        //            "*=" => SyntaxKind.MultiplyExpression,
        //            "=" => SyntaxKind.DivideExpression,
        //            "%=" => SyntaxKind.ModuloExpression,
        //            "|=" => SyntaxKind.BitwiseOrExpression,
        //            "&=" => SyntaxKind.BitwiseAndExpression,
        //            "^=" => SyntaxKind.ExclusiveOrExpression,
        //            ">>=" => SyntaxKind.RightShiftExpression,
        //            "<<=" => SyntaxKind.LeftShiftExpression,
        //            _ => SyntaxKind.None
        //        };
        //        if (kind != SyntaxKind.None)
        //        {
        //            var newNode = SyntaxFactory.AssignmentExpression(
        //                SyntaxKind.SimpleAssignmentExpression,
        //                node.Left.WithoutLeadingTrivia().WithoutTrailingTrivia(),
        //                SyntaxFactory.BinaryExpression(
        //                    kind,
        //                    node.Left.WithoutLeadingTrivia().WithoutTrailingTrivia(),
        //                    node.Left.WithoutLeadingTrivia().WithoutTrailingTrivia())
        //                );
        //            return newNode;
        //        }
        //    }
        //    return base.VisitAssignmentExpression(node);
        //}
    }
}