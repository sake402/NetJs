using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Tests
{
    internal class StringTests
    {
        public static void Run()
        {
            {
                // --- Construction & Basic Equality ---
                string a = "hello";
                string b = "hello";
                string c = new string(new[] { 'h', 'e', 'l', 'l', 'o' });

                Debug.Assert(a == b);
                Debug.Assert(a.Equals(b));
                Debug.Assert(a.Equals(c));
                //Debug.Assert(ReferenceEquals(a, b));
                Debug.Assert(!ReferenceEquals(a, c)); // c is a new instance

                // --- Length ---
                Debug.Assert(a.Length == 5);
                
                // --- Indexing ---
                Debug.Assert(a[0] == 'h');
                Debug.Assert(a[^1] == 'o');

                // --- Concatenation ---
                string concat = a + " world";
                Debug.Assert(concat == "hello world");
                concat = string.Concat(a, " ", "world");
                Debug.Assert(concat == "hello world");

                // --- Interpolation & Formatting ---
                string formatted = $"{a} world";
                Debug.Assert(formatted == "hello world");
                string fmt = string.Format("{0} {1}!", "hello", "world");
                Debug.Assert(fmt == "hello world!");

                // --- Comparison ---
                string x = "Apple";
                string y = "apple";
                Debug.Assert(string.Compare(x, y, true, CultureInfo.InvariantCulture) == 0);
                Debug.Assert(string.CompareOrdinal(x, y) < 0);
                Debug.Assert(x.Equals(y, StringComparison.OrdinalIgnoreCase));

                // --- Contains, StartsWith, EndsWith ---
                Debug.Assert(a.Contains("ell"));
                Debug.Assert(a.StartsWith("he"));
                Debug.Assert(a.EndsWith("lo"));
                Debug.Assert(!a.Contains("xyz"));

                // --- IndexOf / LastIndexOf ---
                Debug.Assert(a.IndexOf('l') == 2);
                Debug.Assert(a.LastIndexOf('l') == 3);
                Debug.Assert(a.IndexOf("ll") == 2);
                Debug.Assert(a.LastIndexOf("ll") == 2);

                // --- Substring ---
                Debug.Assert(a.Substring(1, 3) == "ell");
                Debug.Assert(a.Substring(0) == "hello");
                Debug.Assert(a[..3] == "hel");
                Debug.Assert(a[2..] == "llo");

                // --- Replace --- 
                string r = a.Replace('l', 'x');
                Debug.Assert(r == "hexxo");
                Debug.Assert(a == "hello"); // immutability

                r = a.Replace("ll", "yy");
                Debug.Assert(r == "heyyo");

                // --- Split ---
                string[] parts = "one,two,three".Split(',');
                Debug.Assert(parts.Length == 3);
                Debug.Assert(parts[0] == "one" && parts[1] == "two" && parts[2] == "three");

                // --- Join ---
                string joined = string.Join("-", parts);
                Debug.Assert(joined == "one-two-three");

                // --- Trim / Pad ---
                string t = "   padded   ";
                Debug.Assert(t.Trim() == "padded");
                Debug.Assert(t.TrimStart() == "padded   ");
                Debug.Assert(t.TrimEnd() == "   padded");
                Debug.Assert("a".PadLeft(3, 'x') == "xxa");
                Debug.Assert("a".PadRight(3, 'x') == "axx");

                // --- ToUpper / ToLower ---
                Debug.Assert(a.ToUpperInvariant() == "HELLO");
                Debug.Assert(a.ToLowerInvariant() == "hello");

                // --- IsNullOrEmpty / IsNullOrWhiteSpace ---
                Debug.Assert(string.IsNullOrEmpty("") == true);
                Debug.Assert(string.IsNullOrWhiteSpace("   ") == true);
                Debug.Assert(string.IsNullOrEmpty("x") == false);

                // --- Empty constant ---
                Debug.Assert(string.Empty == "");

                // --- Interning behavior ---
                //string inter1 = string.Intern("sample");
                //string inter2 = string.Intern("sample");
                //Debug.Assert(ReferenceEquals(inter1, inter2));

                // --- CompareOrdinal and Equals variants ---
                Debug.Assert(string.Equals("abc", "ABC", StringComparison.OrdinalIgnoreCase));
                Debug.Assert(!string.Equals("abc", "ABD", StringComparison.Ordinal));
                Debug.Assert(string.CompareOrdinal("abc", "abc") == 0);
                Debug.Assert(string.CompareOrdinal("abc", "abd") < 0);

                // --- Join with empty separator ---
                string joined2 = string.Join("", new[] { "a", "b", "c" });
                Debug.Assert(joined2 == "abc");

                // --- Format variations ---
                string s1 = string.Format(CultureInfo.InvariantCulture, "{0:N2}", 1234.5);
                Debug.Assert(s1.Contains("1,234.50") || s1.Contains("1 234,50")); // culture invariant tolerance

                // --- String.CompareTo ---
                Debug.Assert("abc".CompareTo("abc") == 0);
                Debug.Assert("abc".CompareTo("abd") < 0);
                Debug.Assert("abd".CompareTo("abc") > 0);

                // --- Contains with StringComparison ---
                Debug.Assert("abc".Contains("A", StringComparison.OrdinalIgnoreCase));

                // --- Null coalescing & empty ops ---
                string? maybe = null;
                string safe = maybe ?? "fallback";
                Debug.Assert(safe == "fallback");

                // --- Immutability check ---
                string original = "immutable";
                string copy = original;
                copy = copy.Replace("i", "I");
                Debug.Assert(original == "immutable"); // original unchanged

                // --- Raw string literals and escaped sequences ---
                string raw = @"C:\path\to\file";
                Debug.Assert(raw.Contains(@"\path"));
                string multi = """
                       line1
                       line2
                       """;
                Debug.Assert(multi.Contains("line1"));
                Debug.Assert(multi.Contains("line2"));

                // --- Spans and memory ---
                ReadOnlySpan<char> span = "abcdef".AsSpan(2, 3);
                Debug.Assert(span.SequenceEqual("cde".AsSpan()));

                // --- Equality across casing with culture ---
                //var turkish = new CultureInfo("tr-TR");
                //Debug.Assert("i".ToUpper(turkish) != "I".ToUpperInvariant());
            }

            {
                // --- Basic construction ---
                string s1 = "Hello";
                string s2 = "Hello";
                string s3 = "World";
                string empty = string.Empty;

                Debug.Assert(s1 == s2);
                Debug.Assert(s1 != s3);
                Debug.Assert(empty.Length == 0);

                // --- Equality and comparison ---
                Debug.Assert(string.Equals(s1, s2));
                Debug.Assert(!string.Equals(s1, s3));
                Debug.Assert(s1.CompareTo(s2) == 0);
                Debug.Assert(s1.CompareTo(s3) < 0);
                Debug.Assert(s3.CompareTo(s1) > 0);

                // --- Indexing ---
                Debug.Assert(s1[0] == 'H');
                Debug.Assert(s1[^1] == 'o');
                
                // --- Enumerable behavior ---
                int count = 0;
                foreach (char ch in s1)
                {
                    Debug.Assert(s1.Contains(ch.ToString()));
                    count++;
                }
                Debug.Assert(count == s1.Length);

                // --- IEnumerable via non-generic interface ---
                IEnumerable enumerable = s1;
                int manualCount = 0;
                foreach (object c in enumerable)
                {
                    Debug.Assert(c is char);
                    manualCount++;
                }
                Debug.Assert(manualCount == s1.Length);

                // --- String.Concat & Join ---
                Debug.Assert(string.Concat("A", "B", "C") == "ABC");
                string joined = string.Join(",", new[] { "A", "B", "C" });
                Debug.Assert(joined == "A,B,C");

                // --- Contains, StartsWith, EndsWith ---
                Debug.Assert(s1.Contains("ell"));
                Debug.Assert(s1.StartsWith("He"));
                Debug.Assert(s1.EndsWith("lo"));
                Debug.Assert(!s1.Contains("xyz"));

                // --- Substring & Range ---
                Debug.Assert(s1.Substring(1, 3) == "ell");
                Debug.Assert(s1[..2] == "He");
                Debug.Assert(s1[1..^1] == "ell");

                // --- IndexOf / LastIndexOf ---
                Debug.Assert(s1.IndexOf('l') == 2);
                Debug.Assert(s1.LastIndexOf('l') == 3);
                Debug.Assert(s1.IndexOf("lo") == 3);

                // --- Replace ---
                string replaced = s1.Replace("l", "L");
                Debug.Assert(replaced == "HeLLo");

                // --- Insert & Remove ---
                string inserted = s1.Insert(5, "!");
                Debug.Assert(inserted == "Hello!");
                string removed = inserted.Remove(5);
                Debug.Assert(removed == "Hello");

                // --- Trim ---
                string padded = "  Hi  ";
                Debug.Assert(padded.Trim() == "Hi");
                Debug.Assert(padded.TrimStart() == "Hi  ");
                Debug.Assert(padded.TrimEnd() == "  Hi");

                // --- Split ---
                string csv = "A,B,C";
                var parts = csv.Split(',');
                Debug.Assert(parts.Length == 3 && parts[0] == "A" && parts[2] == "C");

                // --- Case conversion ---
                Debug.Assert("abc".ToUpper() == "ABC");
                Debug.Assert("ABC".ToLower() == "abc");
                //Debug.Assert("ß".ToUpperInvariant() == "SS" || "ß".ToUpperInvariant() == "ẞ"); // locale variation

                // --- Format ---
                string formatted = string.Format("Hello {0}!", "Sam");
                Debug.Assert(formatted == "Hello Sam!");

                // --- Interpolation ---
                string name = "ChatGPT";
                Debug.Assert($"Hello {name}" == "Hello ChatGPT");

                // --- Join with char separator ---
                string joinedChar = string.Join('-', new[] { "one", "two", "three" });
                Debug.Assert(joinedChar == "one-two-three");

                // --- ToCharArray ---
                char[] arr = s1.ToCharArray();
                Debug.Assert(arr.Length == s1.Length);
                Debug.Assert(arr[0] == 'H' && arr[^1] == 'o');

                // --- Clone ---
                object clone = s1.Clone();
                Debug.Assert(clone is string && (string)clone == s1);

                // --- CompareOrdinal ---
                Debug.Assert(string.CompareOrdinal("abc", "abc") == 0);
                Debug.Assert(string.CompareOrdinal("abc", "abd") < 0);

                // --- Empty and null tests ---
                Debug.Assert(string.IsNullOrEmpty(""));
                Debug.Assert(string.IsNullOrWhiteSpace(" "));
                Debug.Assert(!string.IsNullOrWhiteSpace("x"));

                // --- PadLeft / PadRight ---
                Debug.Assert("Hi".PadLeft(4, '_') == "__Hi");
                Debug.Assert("Hi".PadRight(4, '_') == "Hi__");

                // --- Concat and Interpolation tests ---
                string concat = s1 + " " + s3;
                Debug.Assert(concat == "Hello World");

                // --- Compare (Culture aware) ---
                Debug.Assert(string.Compare("a", "A", true, CultureInfo.InvariantCulture) == 0);
                Debug.Assert(string.Compare("a", "B", true, CultureInfo.InvariantCulture) < 0);

                // --- Interning ---
                //string interned = string.Intern(new string("Hello"));
                //Debug.Assert(object.ReferenceEquals(interned, string.Intern(s1)));

                // --- CopyTo ---
                char[] buffer = new char[5];
                s1.CopyTo(0, buffer, 0, 5);
                Debug.Assert(new string(buffer) == "Hello");

                // --- Enumeration behavior vs indexing ---
                for (int i = 0; i < s1.Length; i++)
                    Debug.Assert(s1[i] == arr[i]);

                // --- CompareOrdinalIgnoreCase simulation ---
                Debug.Assert(string.Equals("abc", "ABC", StringComparison.OrdinalIgnoreCase));

                // --- EndsWith with comparison ---
                Debug.Assert("HELLO".EndsWith("lo", StringComparison.OrdinalIgnoreCase));

                // --- StartsWith with comparison ---
                Debug.Assert("HELLO".StartsWith("he", StringComparison.OrdinalIgnoreCase));

                // --- Format with multiple arguments ---
                string fmt2 = string.Format("{0}-{1}-{2}", 1, 2, 3);
                Debug.Assert(fmt2 == "1-2-3");

                // --- Split with count ---
                var limited = "a,b,c,d".Split(',', 2);
                Debug.Assert(limited.Length == 2);

                // --- Enumerator manual check ---
                IEnumerator<char> enumerator = ((IEnumerable<char>)s1).GetEnumerator();
                int charCount = 0;
                while (enumerator.MoveNext())
                {
                    Debug.Assert(s1.Contains(enumerator.Current.ToString()));
                    charCount++;
                }
                Debug.Assert(charCount == s1.Length);

                // --- Comparison and collation ---
                //CultureInfo fr = new("fr-FR");
                //Debug.Assert(string.Compare("é", "e", false, fr) > 0);
            }

            Console.WriteLine("✅ String Tests passed.");
        }
    }
}

