using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CodeLineWriter = System.IO.StringWriter;

namespace NetJs.Translator.CSharpToJavascript
{
    public class CodeWriterClosure
    {
        public CodeWriterClosure(int nameSeedStart, LinkedListNode<CodeLineWriter> start)
        {
            NameManglingSeed = nameSeedStart;
            Start = start;
        }

        public LinkedListNode<CodeLineWriter> Start { get; }
        public int Inserts { get; set; }
        public int NameManglingSeed { get; set; }
        public bool ForbidsInsertion { get; set; }
        public event EventHandler? OnClosing;
        bool onClosingRaised;
        internal void RaiseOnClosing()
        {
            if (onClosingRaised)
                return;
            onClosingRaised = true;
            OnClosing?.Invoke(this, EventArgs.Empty);
        }
    }
}