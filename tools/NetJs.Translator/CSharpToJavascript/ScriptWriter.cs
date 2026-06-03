using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
//using CodeLineWriter = System.IO.StringWriter;

namespace NetJs.Translator.CSharpToJavascript
{
    public class ScriptWriter
    {
        public class Replacement : IDisposable
        {
            public string Token { get; }
            Action dispose;
            public int Hit { get; set; }
            public Replacement(string token, Action dispose)
            {
                Token = token;
                this.dispose = dispose;
            }

            public void Dispose()
            {
                dispose();
            }
        }
        LinkedList<CodeLineWriter> lines = new LinkedList<CodeLineWriter>([new CodeLineWriter()]);
        //StringWriter writer = new StringWriter();
        public int ClosureDepth { get; set; }
        //LinkedListNode<CodeLineWriter> currentWriterNode => lines.Last;
        CodeLineWriter currentWriter => lines.Last!.Value;
        Dictionary<string, Replacement> _replaceToken = new();
        public Replacement SetReplacement(string token, string replacement)
        {
            var rep = new Replacement(replacement, () => _replaceToken.Remove(token));
            _replaceToken.Add(token, rep);
            return rep;
        }

        string ProcessReplacement(string token)
        {
            if (_replaceToken.TryGetValue(token, out var replacement))
            {
                replacement.Hit++;
                return replacement.Token;
            }
            return token;
        }

        void WriteTabs()
        {
            for (int i = 0; i < ClosureDepth; i++)
            {
                if (temporaryWriter.TryPeek(out var tpw))
                    tpw.Write(ProcessReplacement("    "));
                else
                    currentWriter.Write(ProcessReplacement("    "));
            }
        }

        Stack<CodeWriterClosure> closures = new Stack<CodeWriterClosure>();
        public CodeWriterClosure CurrentClosure => closures.Peek();
        Stack<CodeLineWriter> temporaryWriter = new Stack<CodeLineWriter>();
        LinkedListNode<CodeLineWriter> EnsureCanInsertAbove(LinkedListNode<CodeLineWriter> node)
        {
            if (node.Value.RedirectInsertBefore != null)
            {
                var toRet = node.Value.RedirectInsertBefore.Node;
                while (toRet.Value.RedirectInsertBefore != null)
                {
                    toRet = toRet.Value.RedirectInsertBefore.Node;
                }
                return toRet;
            }
            //cannot insert before this block
            //if (before.Value.StartsWith("else"))
            //{
            //    while (!before.Value.StartsWith("if "))
            //    {
            //        before = before.Previous;
            //    }
            //}
            //else if (before.Value.StartsWith("while"))
            //{
            //    if (before.Previous.Value.StartsWith("}"))
            //    {
            //        while (!before.Value.StartsWith("do"))
            //        {
            //            before = before.Previous;
            //        }
            //    }
            //}
            return node;
        }

        public void InsertAbove(SyntaxNode source, Action lineWriter, bool withTabs)
        {
            var closureDepth = ClosureDepth;
            var writer = new CodeLineWriter();
            if (withTabs)
            {
                for (int i = 0; i < closureDepth; i++)
                    writer.Write(ProcessReplacement("    "));
            }
            temporaryWriter.Push(writer);
            lineWriter();
            temporaryWriter.Pop();
            var before = EnsureCanInsertAbove(lines.Last);
            var node = lines.AddBefore(before, writer);
            writer.Node = node;
        }

        public void InsertAbove(SyntaxNode source, string line, bool withTabs)
        {
            InsertAbove(source, () => temporaryWriter.Peek().Write(ProcessReplacement(line)), withTabs);
        }

        public void InsertInCurrentClosure(SyntaxNode source, Action lineWriter, bool withTabs)
        {
            var useClosure = CurrentClosure;
            var closureDepth = ClosureDepth;
            int ic = 0;
            useClosure = closures.ElementAt(ic);
            while (useClosure.ForbidsInsertion)
            {
                ic++;
                useClosure = closures.ElementAt(ic);
                closureDepth--;
            }
            var writer = new CodeLineWriter();
            if (withTabs)
            {
                for (int i = 0; i < closureDepth; i++)
                    writer.Write(ProcessReplacement("    "));
            }
            temporaryWriter.Push(writer);
            lineWriter();
            temporaryWriter.Pop();
            //writer.Write(line);
            var node = useClosure.Start;
            int ix = 0;
            while (ix++ < useClosure.Inserts)
            {
                node = node.Next;
            }
            var lnode = lines.AddAfter(node, writer);
            writer.Node = lnode;
            useClosure.Inserts++;
        }


        public void InsertInCurrentClosure(SyntaxNode source, string line, bool withTabs)
        {
            InsertInCurrentClosure(source, () => temporaryWriter.Peek().Write(ProcessReplacement(line)), withTabs);
        }

        public CodeLineWriter Write(SyntaxNode source, char code)
        {
            if (temporaryWriter.TryPeek(out var tpw))
            {
                tpw.Write(code);
                return tpw;
            }
            else
                currentWriter.Write(code);
            return currentWriter;
        }

        public CodeLineWriter Write(SyntaxNode source, string code, bool withTabs = false, bool forbidInsertion = false)
        {
            if (withTabs)
            {
                if (code.StartsWith("}"))
                {
                    CurrentClosure.RaiseOnClosing();
                    closures.Pop();
                    ClosureDepth--;
                }
                WriteTabs();
                if (code == "{")
                {
                    closures.Push(new CodeWriterClosure(closures.Count > 0 ? CurrentClosure.NameManglingSeed : 0, lines.Last) { ForbidsInsertion = forbidInsertion });
                    ClosureDepth++;
                }
            }
            if (temporaryWriter.TryPeek(out var tpw))
            {
                tpw.Write(ProcessReplacement(code));
                return tpw;
            }
            else
                currentWriter.Write(ProcessReplacement(code));
            return currentWriter;
        }

        public CodeLineWriter WriteLine(CSharpSyntaxNode source, string code, bool withTabs = false, bool forbidInsertion = false)
        {
            var usedLineWriter = temporaryWriter.Count > 0 ? temporaryWriter.Peek() : currentWriter;
            Write(source, code, withTabs, forbidInsertion: forbidInsertion);
            if (temporaryWriter.Count == 0)
            {
                var writer = new CodeLineWriter();
                var node = lines.AddLast(writer);
                writer.Node = node;
            }
            else
                return Write(source, ProcessReplacement("\r\n"), withTabs, forbidInsertion: forbidInsertion);
            return usedLineWriter;
        }

        public void EnsureNewLine()
        {
            if (currentWriter.ToString().Length > 0)
            {
                var writer = new CodeLineWriter();
                var node = lines.AddLast(writer);
                writer.Node = node;
            }
        }

        public bool EndsWith(string token)
        {
            if (temporaryWriter.TryPeek(out var tpw))
                return tpw.EndsWith(token);
            return lines.Last.Value.EndsWith(token);
        }

        public string Build(int formatTabs)
        {
            string tabs = "";
            for (int i = 0; i < formatTabs; i++)
                tabs += "    ";
            return string.Join("\r\n", lines.Select(l => tabs + l.ToString()));
        }

        public override string ToString()
        {
            return Build(0);
        }
    }
}