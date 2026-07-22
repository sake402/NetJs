using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using YamlDotNet.Core.Tokens;

namespace NetJs.Compiler
{
    public class DuplicityWriter : TextWriter
    {
        private readonly TextWriter _primaryOutput;
        private readonly TextWriter _secondaryOutput;
       public AsyncLocal<TextWriter> AsyncLocalWriter { get; set; } = new AsyncLocal<TextWriter>();
        public DuplicityWriter(TextWriter primaryOutput, TextWriter secondaryOutput)
        {
            _primaryOutput = primaryOutput;
            _secondaryOutput = secondaryOutput;
        }

        public override Encoding Encoding => _primaryOutput.Encoding;

        public override void Write(char value)
        {
            _primaryOutput.Write(value);
            _secondaryOutput.Write(value);
            if (AsyncLocalWriter.Value != null)
            {
                AsyncLocalWriter.Value.Write(value);
            }
        }

        public override void Write(string? value)
        {
            _primaryOutput.Write(value);
            _secondaryOutput.Write(value);
            if (AsyncLocalWriter.Value != null)
            {
                AsyncLocalWriter.Value.Write(value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _secondaryOutput.Dispose();
                if (AsyncLocalWriter.Value != null)
                {
                    AsyncLocalWriter.Value.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }

}
