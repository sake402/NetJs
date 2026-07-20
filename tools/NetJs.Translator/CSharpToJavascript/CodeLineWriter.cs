using System.IO;
//using CodeLineWriter = System.IO.StringWriter;

namespace NetJs.Translator.CSharpToJavascript
{
    public class CodeLineWriter
    {
        public CodeLineWriter()
        {
            internalWriter = new StringWriter(sb);
        }
#if DEBUG
        string line = "";
#endif
        private readonly StringBuilder sb = new StringBuilder(512);
        private readonly StringWriter internalWriter;
        char lastChar;
        string? lastWord;
        public LinkedListNode<CodeLineWriter> Node { get; set; } = default!;
        public CodeLineWriter? RedirectInsertBefore { get; set; }
        void ValidateChar(char firstChar, string? fromString)
        {
            if (lastChar == '(' && firstChar == ',')
                throw new InvalidOperationException("Syntax would not be valid");
            if (lastChar == '(' && firstChar == '=')
                throw new InvalidOperationException("Syntax would not be valid");
            if (lastChar == '(' && (firstChar == '>' || firstChar == '<' || firstChar == '='))
                throw new InvalidOperationException("Syntax would not be valid");
            if ((lastChar == '(' || lastChar == ',') && firstChar == '.' && fromString != "...")
                throw new InvalidOperationException("Syntax would not be valid");
        }

        void ValidateWord(ReadOnlySpan<char> word)
        {
            if (lastWord == "return" && word == "throw")
                throw new InvalidOperationException("Syntax would not be valid");
            if (lastWord == "throw" && word == ";")
                throw new InvalidOperationException("Syntax would not be valid");
#if DEBUG
            if (line.EndsWith(Constants.RefValueName + ".") && word .SequenceEqual( Constants.RefValueName))
                throw new InvalidOperationException("Double dereference would fail");
#endif
            //if (lastWord == Constants.RefValueName && lastChar == '.' && word == Constants.RefValueName)
            //throw new InvalidOperationException("Double dereference would fail");
            //if (lastChar == '.' && word == "this")
            //throw new InvalidOperationException("Syntax would not be valid");
        }

        public void Write(char value)
        {
            ValidateChar(value, null);
#if DEBUG
            line += value;
            //if (line.Contains("$.$mac.$."))
            //{
            
            //}
#endif
            internalWriter.Write(value);
            lastChar = value;

            // Incrementally capture the last word if it's alphanumeric
            if (char.IsLetterOrDigit(value) || value == '_')
            {
                lastWord = (lastWord == null) ? value.ToString() : lastWord + value;
            }
            else if (!char.IsWhiteSpace(value))
            {
                lastWord = null;
            }
        }
        public void Write(string value)
        {
            if (value.Length == 0)
                return;
            if (value.Length == 1)
            {
                Write(value[0]);
                return;
            }
            ValidateChar(value[0], value);
            //ValidateWord(value.Trim().Split([' '], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault());
            // Zero-allocation word identification via ReadOnlySpan
            ReadOnlySpan<char> span = value.AsSpan().Trim();
            if (span.Length > 0)
            {
                int firstSpace = span.IndexOf(' ');
                ReadOnlySpan<char> firstWord = firstSpace == -1 ? span : span.Slice(0, firstSpace);
                ValidateWord(firstWord);
            }
            internalWriter.Write(value);
#if DEBUG
            line += value;
            //if (line.Contains("$.$mac.$."))
            //{

            //}
#endif
            if (span.Length > 0)
            {
                lastChar = span[span.Length - 1];
                int lastSpace = span.LastIndexOf(' ');
                lastWord = lastSpace == -1 ? span.ToString() : span.Slice(lastSpace + 1).ToString();
            }
            //var trimmedValue = value.Trim();
            //if (trimmedValue.Length > 0)
            //{
            //    lastChar = trimmedValue[trimmedValue.Length - 1];
            //    lastWord = trimmedValue.Split([' '], StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            //}
        }

        public bool StartsWith(string value)
        {
            //return ToString().TrimStart().StartsWith(value);
            if (string.IsNullOrEmpty(value) || sb.Length < value.Length) return false;

            int startIdx = 0;
            while (startIdx < sb.Length && char.IsWhiteSpace(sb[startIdx]))
            {
                startIdx++;
            }

            if (sb.Length - startIdx < value.Length) return false;

            for (int i = 0; i < value.Length; i++)
            {
                if (sb[startIdx + i] != value[i]) return false;
            }
            return true;
        }

        public bool EndsWith(string value)
        {
            //return ToString().TrimEnd().EndsWith(value);
            if (string.IsNullOrEmpty(value) || sb.Length < value.Length) return false;

            int endIdx = sb.Length - 1;
            while (endIdx >= 0 && char.IsWhiteSpace(sb[endIdx]))
            {
                endIdx--;
            }

            if (endIdx + 1 < value.Length) return false;

            int matchStart = endIdx - value.Length + 1;
            for (int i = 0; i < value.Length; i++)
            {
                if (sb[matchStart + i] != value[i]) return false;
            }
            return true;
        }

        public void Remove(string token)
        {
            //var newContents = internalWriter.ToString().Replace(token, "");
            //internalWriter = new();
            //internalWriter.Write(newContents);
            if (string.IsNullOrEmpty(token)) return;

            // Modifies the original buffer text in place without garbage generation
            sb.Replace(token, string.Empty);
        }

        public void TrimEnd()
        {
            while (sb.Length > 0 && char.IsWhiteSpace(sb[sb.Length - 1]))
            {
                sb.Length--;
            }
        }
        public int Lenght => sb.Length;
        public override string ToString()
        {
            return internalWriter.ToString();
        }
    }
}