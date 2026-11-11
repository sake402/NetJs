using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUglify;
using CodeLineWriter = System.IO.StringWriter;

namespace NetJs.Translator.CSharpToJavascript
{
    public class CodeWriterClosure
    {
        public CodeWriterClosure(SyntaxNode source, CodeWriterClosure? parent, LinkedListNode<CodeLineWriter> start)
        {
            Source = source;
            Parent = parent;
            Start = start;
            nameManglingSeed = parent?.NameManglingSeed ?? 0;
        }

        public HashSet<string> LinesInserted { get; } = new();

        public SyntaxNode Source { get; }
        public CodeWriterClosure? Parent { get; }
        public LinkedListNode<CodeLineWriter> Start { get; }
        public int Inserts { get; set; }
        int nameManglingSeed;
        public int NameManglingSeed
        {
            get
            {
                //if (Source.IsKind(SyntaxKind.ConstructorDeclaration) ||
                //    Source.IsKind(SyntaxKind.IndexerDeclaration) ||
                //    Source.IsKind(SyntaxKind.MethodDeclaration) ||
                //    Source.IsKind(SyntaxKind.PropertyDeclaration) ||
                //    Source.IsKind(SyntaxKind.ConversionOperatorDeclaration) ||
                //    Source.IsKind(SyntaxKind.SetAccessorDeclaration) ||
                //    Source.IsKind(SyntaxKind.GetAccessorDeclaration) ||
                //    Source.IsKind(SyntaxKind.Block))
                    return nameManglingSeed;
                //return Parent?.NameManglingSeed ?? nameManglingSeed;
            }
            set
            {
                //if (Source.IsKind(SyntaxKind.ConstructorDeclaration) ||
                //    Source.IsKind(SyntaxKind.IndexerDeclaration) ||
                //    Source.IsKind(SyntaxKind.MethodDeclaration) ||
                //    Source.IsKind(SyntaxKind.PropertyDeclaration) ||
                //    Source.IsKind(SyntaxKind.ConversionOperatorDeclaration) ||
                //    Source.IsKind(SyntaxKind.SetAccessorDeclaration) ||
                //    Source.IsKind(SyntaxKind.GetAccessorDeclaration) ||
                //    Source.IsKind(SyntaxKind.Block))
                //    nameManglingSeed = value;
                //else if (Parent != null)
                //    Parent.NameManglingSeed = value;
                //else
                    nameManglingSeed = value;
                Parent?.NameManglingSeed = value;
            }
        }
        public bool ForbidsInsertion { get; set; }
        //public event EventHandler? OnBlockClosing;
        List<Action>? onBlockClosing;
        public void OnBlockClosing(Action action)
        {
            onBlockClosing ??= new();
            onBlockClosing.Add(action);
        }
        //public event EventHandler? OnClosing;
        //public event EventHandler? OnClosed;
        //bool onClosingRaised;
        public void RaiseOnBlockClosing()
        {
            if (onBlockClosing != null)
            {
                //Blaocks are LIFO
                foreach (var bc in ((IEnumerable<Action>)onBlockClosing).Reverse())
                    bc();
            }
        }
        //internal void RaiseOnClosing()
        //{
        //    if (onClosingRaised)
        //        return;
        //    onClosingRaised = true;
        //    OnClosing?.Invoke(this, EventArgs.Empty);
        //    onClosingRaised = false;
        //}
        //internal void RaiseOnClosed()
        //{
        //    OnClosed?.Invoke(this, EventArgs.Empty);
        //}
    }
}