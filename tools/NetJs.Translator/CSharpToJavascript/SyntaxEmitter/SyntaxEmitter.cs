using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    public abstract class SyntaxEmitter<TSyntax> : ISyntaxEmitter<TSyntax> where TSyntax : SyntaxNode
    {
        protected ThreadLocal<Stack<CSharpSyntaxNode>> _processing = new(() => new Stack<CSharpSyntaxNode>());
        public Type SyntaxType => typeof(TSyntax);
        public abstract bool TryEmit(TSyntax node, TranslatorSyntaxVisitor visitor);
        public bool TryEmit(SyntaxNode node, TranslatorSyntaxVisitor visitor) => TryEmit((TSyntax)node, visitor);
    }
}
