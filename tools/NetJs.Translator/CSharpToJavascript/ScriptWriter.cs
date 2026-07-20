using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
        public int ClosureDepth { get; set; }
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

        List<CodeWriterClosure> closures = new List<CodeWriterClosure>();
        public CodeWriterClosure CurrentClosure => closures[closures.Count - 1];
        Stack<CodeLineWriter> temporaryWriter = new Stack<CodeLineWriter>();

        public CodeWriterClosure? GetClosureOf(SyntaxNode node)
        {
            for (int i = closures.Count - 1; i >= 0; i--)
            {
                if (closures[i].Source == node)
                    return closures[i];
            }
            return null;
            //return closures.FirstOrDefault(c => c.Source == node);
        }

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

        public bool InsertAbove(SyntaxNode source, string line, bool withTabs, bool skipIfAlreadyInserted = false)
        {
            if (!CurrentClosure.LinesInserted.Add(line))
            {
                if (skipIfAlreadyInserted)
                {
                    return false;
                }
            }
            InsertAbove(source, () => temporaryWriter.Peek().Write(ProcessReplacement(line)), withTabs);
            return true;
        }

        public void InsertInCurrentClosure(SyntaxNode source, Action lineWriter, bool withTabs)
        {
            int closureDepth = ClosureDepth;
            int ic = closures.Count - 1;
            var useClosure = closures[ic];

            while (useClosure.ForbidsInsertion)
            {
                ic--;
                useClosure = closures[ic];
                closureDepth--;
            }

            var writer = new CodeLineWriter();
            if (withTabs)
            {
                string tabStr = ProcessReplacement("    ");
                for (int i = 0; i < closureDepth; i++)
                    writer.Write(tabStr);
            }
            temporaryWriter.Push(writer);
            lineWriter();
            temporaryWriter.Pop();

            var node = useClosure.Start;
            int inserts = useClosure.Inserts;
            for (int ix = 0; ix < inserts; ix++)
            {
                node = node.Next;
            }
            var lnode = lines.AddAfter(node!, writer);
            writer.Node = lnode;
            useClosure.Inserts++;
            //var useClosure = CurrentClosure;
            //var closureDepth = ClosureDepth;
            //int ic = 0;
            //useClosure = closures.ElementAt(ic);
            //while (useClosure.ForbidsInsertion)
            //{
            //    ic++;
            //    useClosure = closures.ElementAt(ic);
            //    closureDepth--;
            //}
            //var writer = new CodeLineWriter();
            //if (withTabs)
            //{
            //    for (int i = 0; i < closureDepth; i++)
            //        writer.Write(ProcessReplacement("    "));
            //}
            //temporaryWriter.Push(writer);
            //lineWriter();
            //temporaryWriter.Pop();
            ////writer.Write(line);
            //var node = useClosure.Start;
            //int ix = 0;
            //while (ix++ < useClosure.Inserts)
            //{
            //    node = node.Next;
            //}
            //var lnode = lines.AddAfter(node, writer);
            //writer.Node = lnode;
            //useClosure.Inserts++;
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
            //CodeWriterClosure? pendingClosedEvent = null;
            if (withTabs)
            {
                if (code.StartsWith("}"))
                {
                    //CurrentClosure.RaiseOnClosing();
                    //pendingClosedEvent = CurrentClosure;
                    closures.RemoveAt(closures.Count - 1); ;
                    ClosureDepth--;
                }
                WriteTabs();
                if (code == "{")
                {
                    closures.Add(new CodeWriterClosure(source, closures.Count > 0 ? CurrentClosure.NameManglingSeed : 0, lines.Last) { ForbidsInsertion = forbidInsertion });
                    ClosureDepth++;
                }
            }
            if (temporaryWriter.TryPeek(out var tpw))
            {
                tpw.Write(ProcessReplacement(code));
                //pendingClosedEvent?.RaiseOnClosed();
                return tpw;
            }
            else
                currentWriter.Write(ProcessReplacement(code));
            //pendingClosedEvent?.RaiseOnClosed();
            return currentWriter;
        }

        public CodeLineWriter WriteLine(CSharpSyntaxNode source, string code, bool withTabs = false, bool forbidInsertion = false)
        {
            bool hasTempWriter = temporaryWriter.TryPeek(out var tempWriter);
            var usedLineWriter = hasTempWriter ? tempWriter! : currentWriter;

            Write(source, code, withTabs, forbidInsertion: forbidInsertion);

            if (!hasTempWriter)
            {
                var writer = new CodeLineWriter();
                var node = lines.AddLast(writer);
                writer.Node = node;
            }
            else
            {
                Write(source, ProcessReplacement("\r\n"), withTabs, forbidInsertion: forbidInsertion);
            }
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

        public void TrimEnd()
        {
            if (temporaryWriter.TryPeek(out var tpw))
            {
                tpw.TrimEnd();
            }
            else
            {
                while (lines.Last.Value.Lenght == 0)
                {
                    lines.Remove(lines.Last);
                }
                lines.Last.Value.TrimEnd();
            }
        }

        public string Build(int formatTabs)
        {
            string tabs = formatTabs > 0 ? new string(' ', formatTabs * 4) : string.Empty;
            return string.Join("\r\n", lines.Select(l => tabs + l.ToString()));
        }

        public override string ToString()
        {
            return Build(0);
        }
    }
}