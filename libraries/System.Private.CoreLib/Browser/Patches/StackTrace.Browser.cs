using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics
{
    public partial class StackTrace
    {
        [NetJs.MemberReplace(nameof(GetTrace))]
        internal static void GetTraceImpl(ObjectHandleOnStack ex, ObjectHandleOnStack res, int skipFrames, bool needFileInfo)
        {
            Exception e = ex.GetObjectHandleOnStack<Exception>();
            MonoStackFrame[] frames = [];
            var error = new Error();
            var stack = error.stack;

            if (stack != null)
            {

                // Split stack by line breaks
                var lines = stack.NativeSplit("\n");

                // Instantiate your custom JS RegExp classes with C# bridge templates
                // V8/Chrome: /^\s*at\s+(?:([^\s(]+)\s+)?\(?([^)]+?):(\d+):(\d+)\)?$/
                var v8Regex = new RegExp(@"^\s*at\s+(?:([^\s(]+)\s+)?\(?([^)]+?):(\d+):(\d+)\)?$");

                // Firefox/Safari fallback: /^(?:([^@]+)?@)?(.+):(\d+):(\d+)$/
                var ffSafariRegex = new RegExp(@"^(?:([^@]+)?@)?(.+):(\d+):(\d+)$");

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.Length == 0) continue;

                    // Execute the JS engine regex match wrapper
                    RegexMatch match = v8Regex.Exec(line);

                    if (match == null)
                    {
                        match = ffSafariRegex.Exec(line);
                    }

                    if (match != null)
                    {
                        // In JS Exec():
                        // match[0] = full matched string
                        // match[1] = Method Name (can be null/undefined if anonymous)
                        // match[2] = File Name
                        // match[3] = Line Number
                        // match[4] = Column Number

                        string methodName = !string.IsNullOrEmpty(match[1]) ? match[1] : "anonymous";
                        string fileName = match[2];
                        int lineNumber = int.Parse(match[3]);
                        int columnNumber = int.Parse(match[4]);

                        frames.Push(new MonoStackFrame
                        {
                            internalMethodName = methodName,
                            fileName = fileName,
                            lineNumber = lineNumber,
                            columnNumber = columnNumber
                        });
                    }
                }
            }
            res.GetObjectHandleOnStack<MonoStackFrame[]?>() = frames;
        }
    }
}
