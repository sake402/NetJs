using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetJs.Translator
{
    public static class Utility
    {
        internal static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key)
        {
            TValue? value = default!;
            dic.TryGetValue(key, out value);
            return value;
        }

        internal static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, TValue value)
        {
            if (!dic.ContainsKey(key))
            {
                dic[key] = value;
            }
            return false;
        }

        internal static bool TryPop<TValue>(this Stack<TValue> stack, out TValue value)
        {
            if (stack.Count > 0)
            {
                value = stack.Pop();
                return true;
            }
            value = default!;
            return false;
        }

        internal static bool TryPeek<TValue>(this Stack<TValue> stack, out TValue value)
        {
            if (stack.Count > 0)
            {
                value = stack.ElementAt(0);
                return true;
            }
            value = default!;
            return false;
        }

        internal static IEnumerable<TSource> DistinctBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static string GetFolder(this IProject project)
        {
            return System.IO.Path.GetDirectoryName(project.FullPath)!;
        }
        public static string GetFolderName(this IProject project)
        {
            return System.IO.Path.GetDirectoryName(project.FullPath)!.Split('/', '\\').Last();
        }

        public static string GetName(this IProject project, bool useNetJsFormat = true)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(project.FullPath);
            if (useNetJsFormat && !name.StartsWith(Constants.ProjectName + "."))
                name = Constants.ProjectName + "." + name;
            return name;
        }

        public static string GetRelativePath(this string fromPath, string toPath)
        {
            if (!fromPath.EndsWith("\\"))
                fromPath += "\\";
            if (string.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (string.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        public static IEnumerable<TResult> FastCast<TResult>(this IEnumerable source) where TResult : class
        {
            foreach (object obj in source)
            {
                yield return Unsafe.As<TResult>(obj);
            }
        }

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence contains no elements.");
                }

                TSource maxElement = enumerator.Current;
                TKey maxKey = keySelector(maxElement);
                IComparer<TKey> comparer = Comparer<TKey>.Default; // Uses the default comparer for TKey

                while (enumerator.MoveNext())
                {
                    TSource currentElement = enumerator.Current;
                    TKey currentKey = keySelector(currentElement);

                    if (comparer.Compare(currentKey, maxKey) > 0)
                    {
                        maxElement = currentElement;
                        maxKey = currentKey;
                    }
                }
                return maxElement;
            }
        }

        //static TextWriter logTo = Console.Out;

        //public static void LogTo(this TextWriter? writer)
        //{
        //    logTo = writer ?? Console.Out;
        //    Console.Tex. = logTo;
        //}

        static AsyncLocal<int> depth = new AsyncLocal<int>();
        public static void Profile(this string message, Action action)
        {
            Console.WriteLine();
            Console.Write(string.Join("", Enumerable.Range(1, depth.Value).Select(i => "    ")) + message + "...");
            Stopwatch sw = new();
            sw.Start();
            depth.Value++;
            try
            {
                action();
            }
            finally
            {
                depth.Value--;
            }
            sw.Stop();
            Console.Write("  " + sw.ElapsedMilliseconds + "ms");
        }

        public static async Task ProfileAsync(this string message, Func<Task> action)
        {
            Console.WriteLine();
            Console.Write(string.Join("", Enumerable.Range(1, depth.Value).Select(i => "    ")) + message + "...");
            Stopwatch sw = new();
            sw.Start();
            depth.Value++;
            try
            {
                await action();
            }
            finally
            {
                depth.Value--;
            }
            sw.Stop();
            Console.Write("  " + sw.ElapsedMilliseconds + "ms");
        }

        public static string RemoveComments(this string str)
        {
            StringBuilder sb = new StringBuilder();
            bool inComment = false;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '/' && i + 1 < str.Length && str[i + 1] == '*')
                {
                    i++;
                    inComment = true;
                }
                else if (str[i] == '*' && i + 1 < str.Length && str[i + 1] == '/')
                {
                    i++;
                    inComment = false;
                }
                else if (!inComment)
                {
                    sb.Append(str[i]);
                }
            }
            return sb.ToString();
        }

        public static string EscapeString(this string input)
        {

            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Allocate slightly larger buffer to handle expanded escape sequences
            var sb = new StringBuilder(input.Length * 4);

            foreach (char c in input)
            {
                // Keep standard readable printable ASCII literal characters as-is
                if (c >= 32 && c <= 126 && c != '"' && c != '\\')
                {
                    sb.Append(c);
                }
                else if (c == '"') sb.Append("\\\""); // Escape literal quotes for JS
                else if (c == '\\') sb.Append("\\\\"); // Escape literal backslashes
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else
                {
                    // Force the character into the literal string format: \uXXXX
                    sb.Append("\\u");
                    sb.Append(((int)c).ToString("X4")); // "X4" outputs exactly 4 hex digits (padded)
                }
            }

            return sb.ToString();
            //return str.Replace(@"\", @"\\")
            //    .Replace("\"", "\\\"")
            //    .Replace("\r", "\\r")
            //    .Replace("\n", "\\n")
            //    .Replace("\t", "\\t")
            //    .Replace("\b", "\\b")
            //    .Replace("\f", "\\f")
            //    .Replace("\v", "\\v")/*.Replace("\0", "\\0")*/;
        }

    }
}
