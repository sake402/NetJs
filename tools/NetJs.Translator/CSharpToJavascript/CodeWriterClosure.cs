using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CodeLineWriter = System.IO.StringWriter;

namespace NetJs.Translator.CSharpToJavascript
{
    public class CodeWriterClosure
    {
        public CodeWriterClosure(SyntaxNode source, int nameSeedStart, LinkedListNode<CodeLineWriter> start)
        {
            Source = source;
            NameManglingSeed = nameSeedStart;
            Start = start;
        }

        public HashSet<string> LinesInserted { get; } = new();

        public SyntaxNode Source { get; }
        public LinkedListNode<CodeLineWriter> Start { get; }
        public int Inserts { get; set; }
        public int NameManglingSeed { get; set; }
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