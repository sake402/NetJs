using System.IO;
//using CodeLineWriter = System.IO.StringWriter;

namespace NetJs.Translator.CSharpToJavascript
{
    public class CodeLineWriter
    {
#if DEBUG
        string line = "";
#endif
        StringWriter internalWriter = new StringWriter(new StringBuilder(512));
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

        void ValidateWord(string word)
        {
            if (lastWord == "return" && word == "throw")
                throw new InvalidOperationException("Syntax would not be valid");
            if (lastWord == "throw" && word == ";")
                throw new InvalidOperationException("Syntax would not be valid");
#if DEBUG
            if (line.EndsWith(Constants.RefValueName + ".") && word == Constants.RefValueName)
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
#endif
            internalWriter.Write(value);
            lastChar = value;
        }
        public void Write(string value)
        {
            if (value==".Invoke" && lastWord == ".Invoke")
            {

            }
            if (value.Length == 0)
                return;
            if (value.Length == 1)
            {
                Write(value[0]);
                return;
            }
            ValidateChar(value[0], value);
            ValidateWord(value.Trim().Split([' '], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault());
            internalWriter.Write(value);
#if DEBUG
            line += value;
#endif
            var trimmedValue = value.Trim();
            if (trimmedValue.Length > 0)
            {
                lastChar = trimmedValue[trimmedValue.Length - 1];
                lastWord = trimmedValue.Split([' '], StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            }
        }

        public bool StartsWith(string value)
        {
            return ToString().TrimStart().StartsWith(value);
        }

        public bool EndsWith(string value)
        {
            return ToString().TrimEnd().EndsWith(value);
        }

        public void Remove(string token)
        {
            var newContents = internalWriter.ToString().Replace(token, "");
            internalWriter = new();
            internalWriter.Write(newContents);
        }

        public override string ToString()
        {
            return internalWriter.ToString();
        }
    }
}