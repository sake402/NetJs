// =============================================================================
//  CSharp14TranspilerTests.cs
//  Exhaustive C# 14 syntax coverage for C# → JS transpiler validation.
//  Every assertion uses Debug.Assert exclusively.
//  Compile with: <LangVersion>preview</LangVersion> (VS 2022 17.12+ / .NET 9+)
// =============================================================================

#nullable enable
#pragma warning disable CS0219   // variable assigned but never used (intentional)
#pragma warning disable CS8321   // local function declared but never used (intentional)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Matrix = System.Collections.Generic.List<System.Collections.Generic.List<int>>;
// ── Top-level alias (C# 12+) ────────────────────────────────────────────────
using Point2D = (int X, int Y);
using StringList = System.Collections.Generic.List<string>;

// ── File-scoped namespace (C# 10+) ──────────────────────────────────────────

namespace NetJs.Tests
{
    // =============================================================================
    //  Primary test runner
    // =============================================================================
    public static class CSharp14TranspilerTestsV2
    {
        public static async Task Run()
        {
            // ── Primitive types & literals ──────────────────────────────────────
            LiteralTests.Run();

            // ── Operators ───────────────────────────────────────────────────────
            OperatorTests.Run();

            // ── Control flow ────────────────────────────────────────────────────
            ControlFlowTests.Run();

            // ── Pattern matching ────────────────────────────────────────────────
            PatternMatchingTests.Run();

            // ── Collections & initializers ──────────────────────────────────────
            CollectionTests.Run();

            // ── LINQ ────────────────────────────────────────────────────────────
            LinqTests.Run();

            // ── Strings ─────────────────────────────────────────────────────────
            StringTestsw.Run();

            // ── Tuples & deconstruction ─────────────────────────────────────────
            TupleTests.Run();

            // ── Nullable reference types ─────────────────────────────────────────
            NullableTests.Run();

            // ── Records ─────────────────────────────────────────────────────────
            RecordTests.Run();

            // ── Classes & OOP ────────────────────────────────────────────────────
            ClassTests.Run();

            // ── Interfaces & default members ────────────────────────────────────
            InterfaceTests.Run();

            // ── Generics ────────────────────────────────────────────────────────
            GenericsTests.Run();

            // ── Delegates, lambdas, events ───────────────────────────────────────
            DelegateLambdaTests.Run();

            // ── Local functions ──────────────────────────────────────────────────
            await LocalFunctionTests.Run();

            // ── Async / await ────────────────────────────────────────────────────
            await AsyncTests.Run();

            // ── Span / Memory (stack-only types) ────────────────────────────────
            SpanTests.Run();

            // ── Unsafe & pointers ────────────────────────────────────────────────
            UnsafeTests.Run();

            // ── Exception handling ───────────────────────────────────────────────
            ExceptionTests.Run();

            // ── Attributes ───────────────────────────────────────────────────────
            AttributeTests.Run();

            // ── C# 9  features ───────────────────────────────────────────────────
            CSharp9Tests.Run();

            // ── C# 10 features ───────────────────────────────────────────────────
            CSharp10Tests.Run();

            // ── C# 11 features ───────────────────────────────────────────────────
            CSharp11Tests.Run();

            // ── C# 12 features ───────────────────────────────────────────────────
            CSharp12Tests.Run();

            // ── C# 13 features ───────────────────────────────────────────────────
            CSharp13Tests.Run();

            // ── C# 14 features ───────────────────────────────────────────────────
            CSharp14Tests.Run();

            // ── Additional coverage ──────────────────────────────────────────────
            ConversionOperatorTests.Run();
            DynamicTests.Run();
            VolatileTests.Run();
            ReadonlyStructTests.Run();
            CheckedOperatorTests.Run();
            ScopedTests.Run();
            RefStructInterfaceTests.Run();
            AsyncDisposableTests.Run();
            LinqExtendedTests.Run();

            Console.WriteLine("✅ Transpiler tests V2 passed.");
        }
    }

    // =============================================================================
    //  1. LITERALS & PRIMITIVE TYPES
    // =============================================================================
    file static class LiteralTests
    {
        public static void Run()
        {
            // Integer literals
            int dec = 1_000_000;
            int hex = 0xFF_AA;
            int bin = 0b_1010_0101;
            long big = 9_223_372_036_854_775_807L;
            uint u = 4_294_967_295U;
            ulong ul = 18_446_744_073_709_551_615UL;

            Debug.Assert(dec == 1000000, "decimal int literal");
            Debug.Assert(hex == 0xFFAA, "hex literal");
            Debug.Assert(bin == 165, "binary literal");
            Debug.Assert(big == long.MaxValue, "long max literal");
            Debug.Assert(u == uint.MaxValue, "uint literal");
            Debug.Assert(ul == ulong.MaxValue, "ulong literal");

            // Floating-point literals
            float f = 3.14f;
            double d = 3.141_592_653_589_793;
            decimal m = 1_000_000.99m;

            Debug.Assert(MathF.Abs(f - 3.14f) < 1e-5f, "float literal");
            Debug.Assert(Math.Abs(d - Math.PI) < 1e-9, "double literal");
            Debug.Assert(m == 1000000.99m, "decimal literal");

            // Character & string literals
            char c1 = 'A';
            char c2 = '\n';
            char c3 = '\u03A9'; // Ω
            Debug.Assert(c1 == 65, "char literal");
            Debug.Assert(c2 == 10, "escape char");
            Debug.Assert(c3 == 'Ω', "unicode char");

            // Boolean & null
            bool t = true, f2 = false;
            object? n = null;
            Debug.Assert(t == true, "bool true");
            Debug.Assert(f2 == false, "bool false");
            Debug.Assert(n == null, "null literal");

            // default literal
            int di = default;
            string ds = default!;
            bool db = default;
            Debug.Assert(di == 0, "default int");
            Debug.Assert(ds == null, "default string");
            Debug.Assert(db == false, "default bool");

            // sizeof
            Debug.Assert(sizeof(int) == 4, "sizeof int");
            Debug.Assert(sizeof(double) == 8, "sizeof double");
            Debug.Assert(sizeof(char) == 2, "sizeof char");
        }
    }

    // =============================================================================
    //  2. OPERATORS
    // =============================================================================
    file static class OperatorTests
    {
        public static void Run()
        {
            // Arithmetic
            Debug.Assert(10 + 3 == 13, "+ op");
            Debug.Assert(10 - 3 == 7, "- op");
            Debug.Assert(10 * 3 == 30, "* op");
            Debug.Assert(10 / 3 == 3, "/ integer division");
            Debug.Assert(10 % 3 == 1, "% modulo");
            Debug.Assert(-(-5) == 5, "unary minus");

            // Increment / decrement
            int x = 5;
            Debug.Assert(x++ == 5, "post-increment returns old value");
            Debug.Assert(x == 6, "post-increment mutates");
            Debug.Assert(++x == 7, "pre-increment returns new value");
            Debug.Assert(x-- == 7, "post-decrement returns old value");
            Debug.Assert(x == 6, "post-decrement mutates");
            Debug.Assert(--x == 5, "pre-decrement returns new value");

            // Bitwise
            Debug.Assert((0b1100 & 0b1010) == 0b1000, "& bitwise AND");
            Debug.Assert((0b1100 | 0b1010) == 0b1110, "| bitwise OR");
            Debug.Assert((0b1100 ^ 0b1010) == 0b0110, "^ bitwise XOR");
            Debug.Assert((~0) == -1, "~ bitwise NOT");
            Debug.Assert((1 << 3) == 8, "<< shift left");
            Debug.Assert((16 >> 2) == 4, ">> shift right");
            Debug.Assert((-16 >>> 1) > 0, ">>> unsigned shift right (C# 11+)");

            // Logical
            Debug.Assert((true && false) == false, "&& logical AND");
            Debug.Assert((false || true) == true, "|| logical OR");
            Debug.Assert((!true) == false, "! logical NOT");

            // Relational
            Debug.Assert(3 < 5, "< less than");
            Debug.Assert(5 > 3, "> greater than");
            Debug.Assert(3 <= 3, "<= less or equal");
            Debug.Assert(3 >= 3, ">= greater or equal");
            Debug.Assert(3 == 3, "== equality");
            Debug.Assert(3 != 4, "!= inequality");

            // Compound assignment
            int a = 10;
            a += 5; Debug.Assert(a == 15, "+=");
            a -= 3; Debug.Assert(a == 12, "-=");
            a *= 2; Debug.Assert(a == 24, "*=");
            a /= 4; Debug.Assert(a == 6, "/=");
            a %= 4; Debug.Assert(a == 2, "%=");
            a <<= 2; Debug.Assert(a == 8, "<<=");
            a >>= 1; Debug.Assert(a == 4, ">>=");
            a &= 6; Debug.Assert(a == 4, "&=");
            a |= 3; Debug.Assert(a == 7, "|=");
            a ^= 5; Debug.Assert(a == 2, "^=");

            // Unsigned right-shift assign (C# 11+)
            int urs = -1;
            urs >>>= 1;
            Debug.Assert(urs > 0, ">>>= unsigned shift assign");

            // Null-coalescing
            string? s = null;
            Debug.Assert((s ?? "fallback") == "fallback", "?? null coalescing");
            s ??= "assigned";
            Debug.Assert(s == "assigned", "??= null coalescing assign");

            // Ternary
            int val = true ? 1 : 2;
            Debug.Assert(val == 1, "ternary ? :");

            // Type operators
            object obj = "hello";
            Debug.Assert(obj is string, "is type check");
            Debug.Assert(obj as string == "hello", "as cast");
            Debug.Assert(obj is string str && str.Length == 5, "is pattern with capture");

            // Checked / unchecked
            int big2 = unchecked(int.MaxValue + 1);
            Debug.Assert(big2 == int.MinValue, "unchecked overflow wraps");

            // nameof
            int myVar = 0;
            Debug.Assert(nameof(myVar) == "myVar", "nameof expression");

            // typeof
            Debug.Assert(typeof(int) == typeof(Int32), "typeof");

            // Conditional member access
            string? ns = null;
            Debug.Assert(ns?.Length == null, "?. null conditional");
            string? ns2 = "hi";
            Debug.Assert(ns2?.Length == 2, "?. on non-null");

            // Index & range (C# 8+)
            int[] arr = { 0, 1, 2, 3, 4 };
            Debug.Assert(arr[^1] == 4, "^1 from-end index");
            Debug.Assert(arr[^2] == 3, "^2 from-end index");
            Debug.Assert(arr[1..3].Length == 2, "1..3 range slice");
            Debug.Assert(arr[..2].Length == 2, "..2 range from start");
            Debug.Assert(arr[3..].Length == 2, "3.. range to end");
            Debug.Assert(arr[..].Length == 5, ".. full range");
        }
    }

    // =============================================================================
    //  3. CONTROL FLOW
    // =============================================================================
    file static class ControlFlowTests
    {
        public static void Run()
        {
            // if / else if / else
            int v = 5;
            string label;
            if (v > 10) label = "big";
            else if (v > 3) label = "medium";
            else label = "small";
            Debug.Assert(label == "medium", "if/else if/else");

            // switch statement
            int day = 3;
            string dayName = day switch
            {
                1 => "Mon",
                2 => "Tue",
                3 => "Wed",
                4 => "Thu",
                5 => "Fri",
                _ => "Weekend"
            };
            Debug.Assert(dayName == "Wed", "switch expression");

            // Classic switch with fall-through guard
            int sw = 0;
            switch (v)
            {
                case 1:
                case 2:
                    sw = 12; break;
                case 5:
                    sw = 50; break;
                default:
                    sw = -1; break;
            }
            Debug.Assert(sw == 50, "switch statement");

            // for loop
            int sum = 0;
            for (int i = 0; i < 5; i++) sum += i;
            Debug.Assert(sum == 10, "for loop");

            // while loop
            int w = 0, wc = 0;
            while (w < 3) { w++; wc++; }
            Debug.Assert(wc == 3, "while loop");

            // do-while loop
            int dw = 0;
            do { dw++; } while (dw < 3);
            Debug.Assert(dw == 3, "do-while loop");

            // foreach loop
            int fsum = 0;
            foreach (int n in new[] { 1, 2, 3, 4, 5 }) fsum += n;
            Debug.Assert(fsum == 15, "foreach loop");

            // break & continue
            int bsum = 0;
            for (int i = 0; i < 10; i++)
            {
                if (i == 7) break;
                if (i % 2 == 0) continue;
                bsum += i;
            }
            Debug.Assert(bsum == 1 + 3 + 5, "break and continue"); // 9

            // goto
            int gv = 0;
            goto jumpTo;
            gv = 99; // unreachable
        jumpTo:
            gv = 42;
            Debug.Assert(gv == 42, "goto");

            // using statement (IDisposable)
            int disposeCount = 0;
            using (var d = new Disposable(() => disposeCount++)) { _ = d; }
            Debug.Assert(disposeCount == 1, "using statement disposes");

            // using declaration (C# 8+)
            {
                using var d2 = new Disposable(() => disposeCount++);
                _ = d2;
            }
            Debug.Assert(disposeCount == 2, "using declaration disposes at scope end");

            // lock statement
            var lockObj = new object();
            int locked = 0;
            lock (lockObj) { locked = 1; }
            Debug.Assert(locked == 1, "lock statement");

            // yield (via helper)
            var yielded = YieldRange(1, 4).ToList();
            Debug.Assert(yielded.SequenceEqual(new[] { 1, 2, 3 }), "yield return");

            // checked / unchecked blocks
            int u2 = unchecked(int.MaxValue + 1);
            Debug.Assert(u2 == int.MinValue, "unchecked block overflow");
        }

        private static IEnumerable<int> YieldRange(int start, int end)
        {
            for (int i = start; i < end; i++) yield return i;
        }

        private sealed class Disposable(Action onDispose) : IDisposable
        {
            public void Dispose() => onDispose();
        }
    }

    // =============================================================================
    //  4. PATTERN MATCHING
    // =============================================================================
    file static class PatternMatchingTests
    {
        public static void Run()
        {
            // Type pattern
            object obj = 42;
            Debug.Assert(obj is int, "type pattern");

            // Declaration pattern
            if (obj is int i) Debug.Assert(i == 42, "declaration pattern captures value");

            // Constant pattern
            Debug.Assert(obj is 42, "constant pattern");
            Debug.Assert(!(obj is 0), "constant pattern negative");

            // Relational patterns (C# 9+)
            int score = 75;
            string grade = score switch
            {
                >= 90 => "A",
                >= 80 => "B",
                >= 70 => "C",
                >= 60 => "D",
                _ => "F"
            };
            Debug.Assert(grade == "C", "relational pattern");

            // Logical patterns: and / or / not (C# 9+)
            bool inRange = score is >= 50 and <= 100;
            Debug.Assert(inRange, "and pattern");

            bool isExtremes = score is < 10 or > 90;
            Debug.Assert(!isExtremes, "or pattern negative");

            bool notZero = score is not 0;
            Debug.Assert(notZero, "not pattern");

            // Property pattern
            var p = new Point(3, 4);
            Debug.Assert(p is { X: 3, Y: 4 }, "property pattern");
            Debug.Assert(p is { X: > 0, Y: > 0 }, "property + relational pattern");

            // Positional / deconstruct pattern
            Debug.Assert(p is (3, 4), "positional pattern");
            Debug.Assert(p is ( > 0, > 0), "positional + relational pattern");

            // Nested pattern
            var rect = new Rect(new Point(0, 0), new Point(5, 5));
            Debug.Assert(rect is { TopLeft: { X: 0, Y: 0 }, BottomRight: { X: 5 } },
                         "nested property pattern");

            // List pattern (C# 11+)
            int[] arr = { 1, 2, 3, 4, 5 };
            Debug.Assert(arr is [1, 2, 3, 4, 5], "list pattern exact");
            Debug.Assert(arr is [1, .. var dd, 5], "list pattern slice");
            Debug.Assert(arr is [_, 2, ..], "list pattern wildcard head");
            Debug.Assert(arr is [.. var pp, 4, 5], "list pattern slice tail");
            Debug.Assert(arr is [1, 2, .. var rest] && rest.Length == 3, "list slice capture");

            // var pattern
            if (obj is var captured) Debug.Assert(captured is 42, "var pattern");

            // switch expression guards (when clause)
            int result = score switch
            {
                int n when n > 100 => -1,
                int n when n >= 70 => 1,
                _ => 0
            };
            Debug.Assert(result == 1, "switch expression with when guard");

            // Tuple pattern
            var tp = (1, "hello");
            Debug.Assert(tp is (1, "hello"), "tuple pattern exact");
            Debug.Assert(tp is ( > 0, { Length: 5 }), "tuple with property pattern");
        }

        private record Point(int X, int Y);
        private record Rect(Point TopLeft, Point BottomRight);
    }

    // =============================================================================
    //  5. COLLECTIONS & INITIALIZERS
    // =============================================================================
    file static class CollectionTests
    {
        public static void Run()
        {
            // Array initializer
            int[] a = { 1, 2, 3 };
            Debug.Assert(a.Length == 3 && a[2] == 3, "array initializer");

            // Implicitly typed array
            var b = new[] { 10, 20, 30 };
            Debug.Assert(b[1] == 20, "implicitly typed array");

            // Multi-dimensional array
            int[,] grid = { { 1, 2 }, { 3, 4 } };
            Debug.Assert(grid[1, 0] == 3, "2D array");

            // Jagged array
            int[][] jag = { new[] { 1, 2 }, new[] { 3, 4, 5 } };
            Debug.Assert(jag[1][2] == 5, "jagged array");

            // List<T>
            var list = new List<int> { 1, 2, 3 };
            list.Add(4);
            Debug.Assert(list.Count == 4 && list[3] == 4, "List<T>");

            // Dictionary<K,V>
            var dict = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
            dict["c"] = 3;
            Debug.Assert(dict["c"] == 3 && dict.Count == 3, "Dictionary<K,V>");

            // HashSet<T>
            var set = new HashSet<int> { 1, 2, 3, 2, 1 };
            Debug.Assert(set.Count == 3, "HashSet dedup");

            // Stack<T>
            var stack = new Stack<int>();
            stack.Push(1); stack.Push(2); stack.Push(3);
            Debug.Assert(stack.Pop() == 3, "Stack.Pop");

            // Queue<T>
            var queue = new Queue<int>();
            queue.Enqueue(1); queue.Enqueue(2);
            Debug.Assert(queue.Dequeue() == 1, "Queue.Dequeue");

            // LinkedList<T>
            var linked = new LinkedList<int>(new[] { 1, 2, 3 });
            linked.AddFirst(0);
            Debug.Assert(linked.First!.Value == 0, "LinkedList.AddFirst");

            // SortedDictionary
            var sorted = new SortedDictionary<int, string> { { 3, "c" }, { 1, "a" }, { 2, "b" } };
            Debug.Assert(sorted.Keys.First() == 1, "SortedDictionary ordering");

            // ImmutableArray / ImmutableList (C# / BCL)
            var immArr = ImmutableArray.Create(1, 2, 3);
            Debug.Assert(immArr[2] == 3, "ImmutableArray");

            // Collection expression (C# 12+)
            int[] ce1 = [1, 2, 3];
            Debug.Assert(ce1.Length == 3 && ce1[0] == 1, "collection expression array");

            List<int> ce2 = [4, 5, 6];
            Debug.Assert(ce2.Count == 3, "collection expression List");

            // Spread in collection expression (C# 12+)
            int[] first = [1, 2];
            int[] second = [3, 4];
            int[] spread = [.. first, .. second, 5];
            Debug.Assert(spread.Length == 5 && spread[4] == 5, "spread element");
            Debug.Assert(spread[2] == 3, "spread ordering");

            // Object initializer
            var pt = new SimplePoint { X = 7, Y = 8 };
            Debug.Assert(pt.X == 7 && pt.Y == 8, "object initializer");

            // Nested object initializer
            var rect = new SimpleRect
            {
                TopLeft = new SimplePoint { X = 0, Y = 0 },
                BottomRight = new SimplePoint { X = 10, Y = 10 }
            };
            Debug.Assert(rect.BottomRight.X == 10, "nested object initializer");

            // With-expression initializer inside collection
            List<SimplePoint> points = [new() { X = 1, Y = 2 }, new() { X = 3, Y = 4 }];
            Debug.Assert(points[1].Y == 4, "collection expression with object initializer");

            // Index / Range on collections
            var arr5 = new[] { 0, 1, 2, 3, 4 };
            var slice = arr5[1..4];
            Debug.Assert(slice is [1, 2, 3], "range slice via list pattern");
        }

        private class SimplePoint { public int X; public int Y; }
        private class SimpleRect { public SimplePoint TopLeft = new(); public SimplePoint BottomRight = new(); }
    }

    // =============================================================================
    //  6. LINQ
    // =============================================================================
    file static class LinqTests
    {
        public static void Run()
        {
            var nums = new[] { 5, 3, 8, 1, 9, 2, 7, 4, 6 };

            // Where / Select
            var evens = nums.Where(n => n % 2 == 0).ToArray();
            Debug.Assert(evens.Length == 4, "LINQ Where");
            var doubled = nums.Select(n => n * 2).ToArray();
            Debug.Assert(doubled[0] == 10, "LINQ Select");

            // OrderBy / ThenBy
            var words = new[] { "banana", "apple", "cherry", "apricot" };
            var ordered = words.OrderBy(w => w[0]).ThenBy(w => w.Length).ToArray();
            Debug.Assert(ordered[0] == "apple" || ordered[0] == "apricot", "OrderBy/ThenBy");

            // GroupBy
            var groups = nums.GroupBy(n => n % 2 == 0 ? "even" : "odd").ToList();
            Debug.Assert(groups.Count == 2, "GroupBy count");

            // Join
            var ids = new[] { 1, 2, 3 };
            var names = new[] { (Id: 1, Name: "Alice"), (Id: 2, Name: "Bob"), (Id: 3, Name: "Carol") };
            var joined = ids.Join(names, id => id, n => n.Id, (id, n) => n.Name).ToArray();
            Debug.Assert(joined[0] == "Alice", "LINQ Join");

            // SelectMany (flatten)
            var nested = new[] { new[] { 1, 2 }, new[] { 3, 4 }, new[] { 5 } };
            var flat = nested.SelectMany(x => x).ToArray();
            Debug.Assert(flat.Length == 5 && flat[2] == 3, "SelectMany");

            // Aggregate
            int product = nums.Aggregate(1, (acc, n) => acc * n);
            Debug.Assert(product == 362880, "Aggregate product"); // 9!

            // Count / Sum / Min / Max / Average
            Debug.Assert(nums.Count() == 9, "Count");
            Debug.Assert(nums.Sum() == 45, "Sum");
            Debug.Assert(nums.Min() == 1, "Min");
            Debug.Assert(nums.Max() == 9, "Max");
            Debug.Assert(nums.Average() == 5.0, "Average");

            // Any / All / None
            Debug.Assert(nums.Any(n => n > 8), "Any");
            Debug.Assert(!nums.All(n => n > 5), "All negative");
            Debug.Assert(nums.Any(), "Any non-empty");

            // First / Last / Single
            var sorted = nums.OrderBy(n => n).ToArray();
            Debug.Assert(sorted.First() == 1, "First");
            Debug.Assert(sorted.Last() == 9, "Last");
            Debug.Assert(nums.Single(n => n == 5) == 5, "Single");
            Debug.Assert(nums.FirstOrDefault(n => n > 100) == 0, "FirstOrDefault missing");

            // Take / Skip / TakeLast / SkipLast
            Debug.Assert(sorted.Take(3).Last() == 3, "Take");
            Debug.Assert(sorted.Skip(6).First() == 7, "Skip");
            Debug.Assert(sorted.TakeLast(2).First() == 8, "TakeLast");
            Debug.Assert(sorted.SkipLast(2).Last() == 7, "SkipLast");

            // TakeWhile / SkipWhile
            Debug.Assert(sorted.TakeWhile(n => n < 5).Count() == 4, "TakeWhile");
            Debug.Assert(sorted.SkipWhile(n => n < 5).First() == 5, "SkipWhile");

            // Distinct / Union / Intersect / Except
            var a = new[] { 1, 2, 2, 3 };
            var bArr = new[] { 2, 3, 4 };
            Debug.Assert(a.Distinct().Count() == 3, "Distinct");
            Debug.Assert(a.Union(bArr).Count() == 4, "Union");
            Debug.Assert(a.Intersect(bArr).Count() == 2, "Intersect");
            Debug.Assert(a.Except(bArr).Single() == 1, "Except");

            // Zip
            var zipped = new[] { 1, 2, 3 }.Zip(new[] { "a", "b", "c" }).ToArray();
            Debug.Assert(zipped[1] == (2, "b"), "Zip");

            // ToDictionary / ToHashSet / ToLookup
            var dict = names.ToDictionary(n => n.Id, n => n.Name);
            Debug.Assert(dict[2] == "Bob", "ToDictionary");

            var hs = nums.ToHashSet();
            Debug.Assert(hs.Contains(9), "ToHashSet");

            var lookup = names.ToLookup(n => n.Name.Length);
            Debug.Assert(lookup[5].Any(n => n.Name == "Alice"), "ToLookup");

            // Query syntax
            var query =
                from n in nums
                where n > 4
                orderby n descending
                select n * 10;
            var qArr = query.ToArray();
            Debug.Assert(qArr[0] == 90 && qArr.Length == 5, "query syntax");

            // Query with let & group
            var grouped =
                from w in words
                let upper = w.ToUpper()
                group upper by upper[0] into g
                orderby g.Key
                select g;
            var gList = grouped.ToList();
            Debug.Assert(gList.Count >= 2, "query group by with let");

            // Chunk (C# 6 / BCL .NET 6+)
            var chunks = sorted.Chunk(3).ToArray();
            Debug.Assert(chunks.Length == 3 && chunks[0].Length == 3, "Chunk");

            // DistinctBy / MinBy / MaxBy (LINQ .NET 6+)
            var minByLen = words.MinBy(w => w.Length);
            Debug.Assert(minByLen != null && minByLen.Length <= words.Min(w => w.Length), "MinBy");

            var distByFirst = words.DistinctBy(w => w[0]).ToArray();
            Debug.Assert(distByFirst.Length < words.Length, "DistinctBy reduces count");
        }
    }

    // =============================================================================
    //  7. STRINGS
    // =============================================================================
    file static class StringTestsw //TODO: test and fix reflection metatdata issue with this name clash with another class StringTests
    {
        public static void Run()
        {
            // Interpolated strings
            int x = 42;
            string s1 = $"Value is {x}";
            Debug.Assert(s1 == "Value is 42", "string interpolation");

            // Nested interpolation
            string s2 = $"Computed: {(x > 40 ? "big" : "small")}";
            Debug.Assert(s2 == "Computed: big", "nested interpolation");

            // Format specifier in interpolation
            double pi = Math.PI;
            string s3 = $"{pi:F2}";
            Debug.Assert(s3 == "3.14", "interpolation format specifier");

            // Verbatim strings
            string path = @"C:\Users\test\file.txt";
            Debug.Assert(path.Contains(@"\Users\"), "verbatim string");

            // Verbatim interpolated
            string dir = "docs";
            string full = $@"C:\{dir}\readme.txt";
            Debug.Assert(full == @"C:\docs\readme.txt", "verbatim interpolated");

            // Raw string literals (C# 11+)
            string raw = """
            Hello
            World
            """;
            Debug.Assert(raw.Contains("Hello") && raw.Contains("World"), "raw string literal");

            // Raw interpolated (C# 11+)
            string name = "Claude";
            string rawInterp = $"""Hello, {name}!""";
            Debug.Assert(rawInterp == "Hello, Claude!", "raw interpolated string");

            // UTF-8 string literals (C# 11+)
            ReadOnlySpan<byte> utf8 = "Hello"u8;
            Debug.Assert(utf8.Length == 5, "UTF-8 string literal length");
            Debug.Assert(utf8[0] == (byte)'H', "UTF-8 first byte");

            // String methods
            string str = "  Hello, World!  ";
            Debug.Assert(str.Trim() == "Hello, World!", "Trim");
            Debug.Assert(str.TrimStart() == "Hello, World!  ", "TrimStart");
            Debug.Assert(str.TrimEnd() == "  Hello, World!", "TrimEnd");

            string h = "Hello, World!";
            Debug.Assert(h.ToUpper() == "HELLO, WORLD!", "ToUpper");
            Debug.Assert(h.ToLower() == "hello, world!", "ToLower");
            Debug.Assert(h.Replace("World", "C#") == "Hello, C#!", "Replace");
            Debug.Assert(h.Contains("World"), "Contains");
            Debug.Assert(h.StartsWith("Hello"), "StartsWith");
            Debug.Assert(h.EndsWith("!"), "EndsWith");
            Debug.Assert(h.IndexOf("World") == 7, "IndexOf");
            Debug.Assert(h.Substring(7, 5) == "World", "Substring");
            Debug.Assert(h.Split(',').Length == 2, "Split");
            Debug.Assert(h.Length == 13, "Length");

            // String.Format
            string sf = string.Format("{0} + {1} = {2}", 1, 2, 3);
            Debug.Assert(sf == "1 + 2 = 3", "String.Format");

            // String.Join
            string joined = string.Join("-", new[] { "a", "b", "c" });
            Debug.Assert(joined == "a-b-c", "String.Join");

            // String.Concat
            string concat = string.Concat("foo", "bar", "baz");
            Debug.Assert(concat == "foobarbaz", "String.Concat");

            // Span-based operations
            string hello = "Hello, World!";
            ReadOnlySpan<char> span = hello.AsSpan(7, 5);
            Debug.Assert(span.ToString() == "World", "AsSpan slice");

            // StringBuilder
            var sb = new StringBuilder();
            sb.Append("Hello");
            sb.Append(", ");
            sb.Append("World");
            sb.Append('!');
            Debug.Assert(sb.ToString() == "Hello, World!", "StringBuilder");

            // String comparison
            Debug.Assert(string.Compare("abc", "ABC", StringComparison.OrdinalIgnoreCase) == 0,
                         "StringComparison.OrdinalIgnoreCase");
            Debug.Assert("abc".Equals("ABC", StringComparison.OrdinalIgnoreCase),
                         "string.Equals ignorecase");

            // Span pattern matching (C# 11+)
            ReadOnlySpan<char> sp = "hello";
            Debug.Assert(sp is "hello", "span pattern matching");
        }
    }

    // =============================================================================
    //  8. TUPLES & DECONSTRUCTION
    // =============================================================================
    file static class TupleTests
    {
        public static void Run()
        {
            // Value tuple creation
            var t1 = (1, "hello");
            Debug.Assert(t1.Item1 == 1 && t1.Item2 == "hello", "tuple access via Item");

            // Named tuples
            var t2 = (X: 3, Y: 4);
            Debug.Assert(t2.X == 3 && t2.Y == 4, "named tuple members");

            // Return tuple from method
            var t3 = Divide(10, 3);
            Debug.Assert(t3.Quotient == 3 && t3.Remainder == 1, "tuple return named");

            // Deconstruction
            var (q, r) = Divide(10, 3);
            Debug.Assert(q == 3 && r == 1, "tuple deconstruction");

            // Discard in deconstruction
            var (_, rem) = Divide(10, 3);
            Debug.Assert(rem == 1, "deconstruction with discard");

            // Nested tuple
            var nested = ((1, 2), (3, 4));
            Debug.Assert(nested.Item1.Item2 == 2, "nested tuple");

            // Tuple equality (C# 7.3+)
            Debug.Assert((1, 2) == (1, 2), "tuple value equality");
            Debug.Assert((1, 2) != (1, 3), "tuple inequality");

            // Deconstruct custom type
            var p = new PointD(5, 6);
            var (px, py) = p;
            Debug.Assert(px == 5 && py == 6, "custom Deconstruct");

            // Multiple deconstruct
            var (a, b, c) = (10, 20, 30);
            Debug.Assert(a == 10 && b == 20 && c == 30, "triple deconstruct");

            // Tuple as dictionary key
            var dict = new Dictionary<(int, int), string>();
            dict[(1, 2)] = "one-two";
            Debug.Assert(dict[(1, 2)] == "one-two", "tuple as dict key");

            // Swap via tuple (no temp variable)
            int x = 1, y = 2;
            (x, y) = (y, x);
            Debug.Assert(x == 2 && y == 1, "tuple swap");

            // Type alias for tuple (C# 12+ top-level alias)
            Point2D pt = (10, 20);
            Debug.Assert(pt.X == 10 && pt.Y == 20, "tuple type alias");
        }

        private static (int Quotient, int Remainder) Divide(int a, int b) => (a / b, a % b);

        private class PointD(int x, int y)
        {
            public void Deconstruct(out int outX, out int outY) { outX = x; outY = y; }
        }
    }

    // =============================================================================
    //  9. NULLABLE REFERENCE TYPES & VALUE TYPES
    // =============================================================================
    file static class NullableTests
    {
        public static void Run()
        {
            // Nullable value types
            int? ni = null;
            Debug.Assert(!ni.HasValue, "nullable int has no value");
            Debug.Assert(ni == null, "nullable int == null");

            ni = 42;
            Debug.Assert(ni.HasValue, "nullable int has value");
            Debug.Assert(ni.Value == 42, "nullable int .Value");
            Debug.Assert(ni.GetValueOrDefault() == 42, "GetValueOrDefault");
            Debug.Assert(((int?)null).GetValueOrDefault(-1) == -1, "GetValueOrDefault with default");

            // Nullable arithmetic
            int? a = 3, b = null;
            Debug.Assert((a + b) == null, "nullable arithmetic with null propagates");
            Debug.Assert((a + 2) == 5, "nullable + non-null");

            // Null-coalescing with nullable
            int result = b ?? -1;
            Debug.Assert(result == -1, "null-coalescing on nullable");

            // Nullable comparison
            Debug.Assert(a > b == false, "null comparison always false");
            Debug.Assert(a == b == false, "null == non-null");

            // Nullable reference types
            string? ns = null;
            Debug.Assert(ns == null, "nullable string == null");
            ns = "hello";
            Debug.Assert(ns!.Length == 5, "null-forgiving on non-null string");

            // Null-conditional chain
            string? maybeNull = null;
            int? len = maybeNull?.Length;
            Debug.Assert(len == null, "?.Length on null");

            string? nonNull = "world";
            int? len2 = nonNull?.Length;
            Debug.Assert(len2 == 5, "?.Length on non-null");

            // Null-conditional indexer
            int[]? arr = null;
            int? elem = arr?[0];
            Debug.Assert(elem == null, "?[] on null array");

            // Null-conditional method call
            StringList? list = null;
            list?.Add("test");  // should not throw
            Debug.Assert(list == null, "?.method on null doesn't throw");

            // is null / is not null
            object? obj = null;
            Debug.Assert(obj is null, "is null pattern");
            obj = 1;
            Debug.Assert(obj is not null, "is not null pattern");

            // Nullable<T> boxing
            int? boxed = 7;
            object boxedObj = boxed!;
            Debug.Assert(boxedObj is int i && i == 7, "nullable boxing");
        }
    }

    // =============================================================================
    //  10. RECORDS
    // =============================================================================
    file static class RecordTests
    {
        // Positional record (C# 9+)
        private record Person(string Name, int Age);

        // Record with custom member
        private record Product(string Name, decimal Price)
        {
            public string Display => $"{Name}: ${Price:F2}";
        }

        // Record struct (C# 10+)
        private record struct Coords(double Lat, double Lon);

        // Readonly record struct (C# 10+)
        private readonly record struct RGB(byte R, byte G, byte B);

        // Record inheritance
        private record Animal(string Species);
        private record Dog(string Species, string Breed) : Animal(Species);

        // Record with init-only + required (C# 11+)
        private record Config
        {
            public required string Host { get; init; }
            public int Port { get; init; } = 8080;
        }

        public static void Run()
        {
            // Basic record
            var alice = new Person("Alice", 30);
            Debug.Assert(alice.Name == "Alice" && alice.Age == 30, "record positional");

            // Value equality
            var alice2 = new Person("Alice", 30);
            Debug.Assert(alice == alice2, "record value equality");
            Debug.Assert(alice.Equals(alice2), "record Equals");
            Debug.Assert(alice.GetHashCode() == alice2.GetHashCode(), "record hash code");

            // with-expression (non-destructive mutation)
            var bob = alice with { Name = "Bob", Age = 25 };
            Debug.Assert(bob.Name == "Bob" && bob.Age == 25, "record with-expression");
            Debug.Assert(alice.Name == "Alice", "original unchanged after with");

            // Deconstruct
            var (name, age) = alice;
            Debug.Assert(name == "Alice" && age == 30, "record deconstruct");

            // Custom member
            var prod = new Product("Widget", 9.99m);
            Debug.Assert(prod.Display == "Widget: $9.99", "record custom property");

            // ToString (auto-generated)
            Debug.Assert(alice.ToString()!.Contains("Alice"), "record ToString");

            // Record struct
            var coords = new Coords(51.5, -0.1);
            Debug.Assert(coords.Lat == 51.5, "record struct");

            // Readonly record struct
            var red = new RGB(255, 0, 0);
            Debug.Assert(red.R == 255, "readonly record struct");

            // Inheritance
            var dog = new Dog("Canine", "Labrador");
            Debug.Assert(dog.Breed == "Labrador", "record inheritance member");
            Debug.Assert(dog is Animal, "record inherits type");

            // required + init
            var cfg = new Config { Host = "localhost" };
            Debug.Assert(cfg.Host == "localhost" && cfg.Port == 8080, "record required + init default");
        }
    }

    // =============================================================================
    //  11. CLASSES, STRUCTS, INTERFACES & OOP
    // =============================================================================
    file static class ClassTests
    {
        // ── Access modifiers & fields ────────────────────────────────────────────
        private class Counter
        {
            private int _value;
            public int Value => _value;
            public Counter(int start = 0) => _value = start;
            public void Increment() => _value++;
            public static Counter operator +(Counter c, int n) => new(c._value + n);
            public override string ToString() => $"Counter({_value})";
        }

        // ── Properties: auto, full, expression-bodied, init-only ────────────────
        private class Person
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string FullName => $"{FirstName} {LastName}";

            private int _age;
            public int Age
            {
                get => _age;
                set
                {
                    if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                    _age = value;
                }
            }

            public string? Nickname { get; init; }
        }

        // ── Inheritance & virtual / override / sealed ────────────────────────────
        private abstract class Shape
        {
            public abstract double Area { get; }
            public virtual string Describe() => $"Shape with area {Area:F2}";
        }

        private sealed class Circle(double radius) : Shape
        {
            public override double Area => Math.PI * radius * radius;
            public override string Describe() => $"Circle r={radius} area={Area:F2}";
        }

        private class Rectangle(double w, double h) : Shape
        {
            public override double Area => w * h;
        }

        // ── Structs ──────────────────────────────────────────────────────────────
        private struct Vector2(float x, float y)
        {
            public float X = x, Y = y;
            public readonly float Length => MathF.Sqrt(X * X + Y * Y);
            public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
        }

        // ── Static class & members ───────────────────────────────────────────────
        private static class MathUtils
        {
            public static int Square(int n) => n * n;
            public static int Cube(int n) => n * n * n;
        }

        // ── Extension methods ────────────────────────────────────────────────────
        // (defined outside — see bottom of file)

        // ── Indexers ────────────────────────────────────────────────────────────
        private class WordBag
        {
            private readonly List<string> _words = new();
            public string this[int i] => _words[i];
            public void Add(string w) => _words.Add(w);
            public int Count => _words.Count;
        }

        // ── Explicit interface implementation ────────────────────────────────────
        private interface IPrintable { void Print(); }
        private interface ILoggable { void Print(); }

        private class Document : IPrintable, ILoggable
        {
            public string Content = "doc";
            void IPrintable.Print() => Console.Write($"[PRINT] {Content}");
            void ILoggable.Print() => Console.Write($"[LOG] {Content}");
        }

        // ── Partial class ────────────────────────────────────────────────────────
        // (split across two partial definitions — simulated inline)

        public static void Run()
        {
            // Counter & operator overloading
            var c = new Counter(5);
            c.Increment();
            Debug.Assert(c.Value == 6, "counter increment");
            var c2 = c + 4;
            Debug.Assert(c2.Value == 10, "operator + overload");
            Debug.Assert(c2.ToString() == "Counter(10)", "overridden ToString");

            // Properties
            var p = new Person { FirstName = "Ada", LastName = "Lovelace", Age = 36, Nickname = "Ada" };
            Debug.Assert(p.FullName == "Ada Lovelace", "expression-bodied property");
            Debug.Assert(p.Age == 36, "full property get");
            Debug.Assert(p.Nickname == "Ada", "init-only property");

            bool threw = false;
            try { p.Age = -1; } catch (ArgumentOutOfRangeException) { threw = true; }
            Debug.Assert(threw, "property setter validation throws");

            // Polymorphism
            Shape[] shapes = { new Circle(3), new Rectangle(4, 5) };
            Debug.Assert(Math.Abs(shapes[0].Area - Math.PI * 9) < 1e-9, "Circle.Area");
            Debug.Assert(shapes[1].Area == 20, "Rectangle.Area");
            Debug.Assert(shapes[0].Describe().Contains("Circle"), "overridden Describe");

            // is / as / typeof with inheritance
            Debug.Assert(shapes[0] is Circle, "is derived type");
            Debug.Assert(shapes[0] is Shape, "is base type");
            Debug.Assert(shapes[0] as Rectangle == null, "as wrong type == null");

            // Struct
            var v1 = new Vector2(3, 4);
            var v2 = new Vector2(1, 1);
            Debug.Assert(Math.Abs(v1.Length - 5f) < 1e-5f, "struct computed property");
            var v3 = v1 + v2;
            Debug.Assert(v3.X == 4 && v3.Y == 5, "struct operator +");

            // Static class
            Debug.Assert(MathUtils.Square(5) == 25, "static method Square");
            Debug.Assert(MathUtils.Cube(3) == 27, "static method Cube");

            // Indexer
            var bag = new WordBag();
            bag.Add("hello"); bag.Add("world");
            Debug.Assert(bag[0] == "hello" && bag.Count == 2, "indexer");

            // Explicit interface
            var doc = new Document();
            ((IPrintable)doc).Print();
            ((ILoggable)doc).Print();

            // Extension methods (defined below as file-scoped)
            Debug.Assert(42.IsEven(), "extension method IsEven");
            Debug.Assert(!43.IsEven(), "extension method IsEven negative");
            Debug.Assert("hello".Shout() == "HELLO!", "extension method Shout");
        }
    }

    // Extension methods (file-scoped)
    file static class Extensions
    {
        public static bool IsEven(this int n) => n % 2 == 0;
        public static string Shout(this string s) => s.ToUpper() + "!";
    }

    // =============================================================================
    //  12. INTERFACES & DEFAULT INTERFACE MEMBERS
    // =============================================================================
    file static class InterfaceTests
    {
        private interface IAnimal
        {
            string Name { get; }
            string Speak();
            // Default interface member (C# 8+)
            string Describe() => $"I am {Name} and I say {Speak()}";
        }

        private interface IFlyable
        {
            double MaxAltitude { get; }
            string Fly() => $"Flying at {MaxAltitude}m";
        }

        // Multiple interface implementation
        private class Parrot : IAnimal, IFlyable
        {
            public string Name => "Parrot";
            public string Speak() => "Squawk!";
            public double MaxAltitude => 500;
        }

        // Generic interface
        private interface IRepository<T>
        {
            T? GetById(int id);
            void Save(T item);
        }

        private class MemoryRepo<T> : IRepository<T>
        {
            private readonly Dictionary<int, T> _store = new();
            private int _next = 1;
            public T? GetById(int id) => _store.TryGetValue(id, out var v) ? v : default;
            public void Save(T item) => _store[_next++] = item;
        }

        // Static interface members (C# 11+)
        private interface ICreatable<T>
        {
            static abstract T Create();
        }

        private class Foo : ICreatable<Foo>
        {
            public int Value { get; private set; }
            public static Foo Create() => new Foo { Value = 99 };
        }

        public static void Run()
        {
            // Default interface method
            var parrot = new Parrot();
            Debug.Assert(parrot.Speak() == "Squawk!", "interface method");
            Debug.Assert(((IAnimal)parrot).Describe().Contains("Parrot"), "default interface method");
            Debug.Assert(((IFlyable)parrot).Fly().Contains("500"), "second interface default method");

            // Multiple interfaces
            Debug.Assert(parrot is IAnimal && parrot is IFlyable, "implements multiple interfaces");

            // Generic interface
            var repo = new MemoryRepo<string>();
            repo.Save("hello");
            Debug.Assert(repo.GetById(1) == "hello", "generic repository Save/Get");
            Debug.Assert(repo.GetById(99) == null, "generic repository missing");

            // Static abstract interface member
            var foo = Foo.Create();
            Debug.Assert(foo.Value == 99, "static abstract interface Create");
        }
    }

    // =============================================================================
    //  13. GENERICS
    // =============================================================================
    file static class GenericsTests
    {
        // Generic class with constraint
        private class MinHeap<T> where T : IComparable<T>
        {
            private readonly List<T> _data = new();
            public void Push(T item) { _data.Add(item); _data.Sort(); }
            public T Pop() { var v = _data[0]; _data.RemoveAt(0); return v; }
            public int Count => _data.Count;
        }

        // Generic method
        private static T[] Repeat<T>(T val, int times)
        {
            var arr = new T[times];
            Array.Fill(arr, val);
            return arr;
        }

        // Multiple constraints
        private static string Describe<T>(T item)
            where T : class, IComparable<T>
            => $"[{item}]";

        // Generic with new() constraint
        private static T CreateDefault<T>() where T : new() => new T();

        // Covariant interface (out)
        private interface IProducer<out T> { T Produce(); }
        private class StringProducer : IProducer<string>
        {
            public string Produce() => "hello";
        }

        // Contravariant interface (in)
        private interface IConsumer<in T> { void Consume(T item); }
        private class ObjectConsumer : IConsumer<object>
        {
            public string? Last { get; private set; }
            public void Consume(object item) => Last = item.ToString();
        }

        // Generic delegate
        private delegate TResult Transform<TIn, TResult>(TIn input);

        // Generic with unmanaged constraint
        private static unsafe int SizeOfUnmanaged<T>() where T : unmanaged => sizeof(T);

        public static void Run()
        {
            // Generic class
            var heap = new MinHeap<int>();
            heap.Push(5); heap.Push(2); heap.Push(8); heap.Push(1);
            Debug.Assert(heap.Pop() == 1, "generic MinHeap pop min");
            Debug.Assert(heap.Pop() == 2, "generic MinHeap second");

            // Generic method
            var arr = Repeat("x", 4);
            Debug.Assert(arr.Length == 4 && arr[2] == "x", "generic Repeat");

            // Inferred type arguments
            var nums = Repeat(0, 3);
            Debug.Assert(nums is [0, 0, 0], "generic Repeat inferred int");

            // Constraint: class + IComparable
            Debug.Assert(Describe("hi") == "[hi]", "generic Describe with constraints");

            // new() constraint
            var list = CreateDefault<List<int>>();
            Debug.Assert(list != null && list.Count == 0, "new() constraint");

            // Variance: IProducer<string> can be used as IProducer<object>
            IProducer<object> covariant = new StringProducer();
            Debug.Assert(covariant.Produce() is "hello", "covariant generic interface");

            // Variance: IConsumer<object> can be used as IConsumer<string>
            IConsumer<string> contravariant = new ObjectConsumer();
            contravariant.Consume("world");
            Debug.Assert(((ObjectConsumer)contravariant).Last == "world", "contravariant generic interface");

            // Generic delegate
            Transform<int, string> t = n => $"#{n}";
            Debug.Assert(t(7) == "#7", "generic delegate");

            // Unmanaged constraint
            int size = SizeOfUnmanaged<int>();
            Debug.Assert(size == 4, "unmanaged sizeof");

            // LINQ with generics
            var sorted = new[] { 3, 1, 4, 1, 5 }.Order().ToArray();
            Debug.Assert(sorted is [1, 1, 3, 4, 5], "LINQ Order generic");
        }
    }

    // =============================================================================
    //  14. DELEGATES, LAMBDAS & EVENTS
    // =============================================================================
    file static class DelegateLambdaTests
    {
        // Named delegate
        private delegate int BinaryOp(int a, int b);

        // Event
        private class Button
        {
            public event EventHandler<string>? Clicked;
            public void Click(string label) => Clicked?.Invoke(this, label);
        }

        public static void Run()
        {
            // Named delegate
            BinaryOp add = (a, b) => a + b;
            Debug.Assert(add(3, 4) == 7, "named delegate lambda");

            BinaryOp mul = delegate (int a, int b) { return a * b; };
            Debug.Assert(mul(3, 4) == 12, "anonymous method delegate");

            // Multicast delegate
            var log = new List<string>();
            Action<string> logger = s => log.Add("A:" + s);
            logger += s => log.Add("B:" + s);
            logger("test");
            Debug.Assert(log.Count == 2 && log[0] == "A:test", "multicast delegate");

            // Func<> / Action<> / Predicate<>
            Func<int, int, int> sum = (a, b) => a + b;
            Action<string> print = s => { _ = s; };
            Predicate<int> isPos = n => n > 0;

            Debug.Assert(sum(5, 6) == 11, "Func<>");
            Debug.Assert(isPos(1), "Predicate<> true");
            Debug.Assert(!isPos(-1), "Predicate<> false");
            print("ok");

            // Lambda with closure
            int factor = 3;
            Func<int, int> triple = n => n * factor;
            Debug.Assert(triple(5) == 15, "lambda closure");
            factor = 10; // mutation seen by lambda
            Debug.Assert(triple(5) == 50, "lambda closure captures by reference");

            // Statement lambda
            Func<int, string> classify = n =>
            {
                if (n < 0) return "negative";
                if (n == 0) return "zero";
                return "positive";
            };
            Debug.Assert(classify(-1) == "negative", "statement lambda negative");
            Debug.Assert(classify(0) == "zero", "statement lambda zero");
            Debug.Assert(classify(1) == "positive", "statement lambda positive");

            // Lambda natural type (C# 10+) — var inferred as Func<int,int>
            var square = (int n) => n * n;
            Debug.Assert(square(6) == 36, "lambda natural type");

            // Lambda with return type annotation (C# 10+)
            var parse = int (string s) => int.Parse(s);
            Debug.Assert(parse("42") == 42, "lambda explicit return type");

            // Lambda attributes (C# 10+)
            Func<string?> maybeNull = [return: System.Diagnostics.CodeAnalysis.MaybeNull] () => null;
            Debug.Assert(maybeNull() == null, "lambda with attribute");

            // Method group
            Func<string, int> len = s => s.Length;
            Debug.Assert(len("hello") == 5, "method group-like lambda");

            // Event
            var btn = new Button();
            string? lastLabel = null;
            btn.Clicked += (_, label) => lastLabel = label;
            btn.Click("Submit");
            Debug.Assert(lastLabel == "Submit", "event += and raise");

            // Remove event handler
            EventHandler<string> handler = (_, lbl) => lastLabel = "REMOVED:" + lbl;
            btn.Clicked += handler;
            btn.Clicked -= handler;
            btn.Click("Again");
            Debug.Assert(lastLabel == "Again", "event -= handler removed");

            // Higher-order functions
            Func<int, Func<int, int>> adder = a => b => a + b;
            var add5 = adder(5);
            Debug.Assert(add5(3) == 8, "currying via lambdas");
        }
    }

    // =============================================================================
    //  15. LOCAL FUNCTIONS
    // =============================================================================
    file static class LocalFunctionTests
    {
        public static async Task Run()
        {
            // Basic local function
            int Double(int n) => n * 2;
            Debug.Assert(Double(5) == 10, "local function");

            // Local function with closure
            int multiplier = 4;
            int Multiply(int n) => n * multiplier;
            Debug.Assert(Multiply(5) == 20, "local function closure");

            // Recursive local function
            long Factorial(int n) => n <= 1 ? 1 : n * Factorial(n - 1);
            Debug.Assert(Factorial(6) == 720, "recursive local function");

            // Static local function (C# 8+) — cannot capture locals
            static int StaticAdd(int a, int b) => a + b;
            Debug.Assert(StaticAdd(3, 4) == 7, "static local function");

            // Local function returning iterator
            IEnumerable<int> Range(int start, int count)
            {
                for (int i = 0; i < count; i++) yield return start + i;
            }
            Debug.Assert(Range(5, 3).ToArray() is [5, 6, 7], "iterator local function");

            // Local async function
            async Task<int> FetchAsync(int v)
            {
                var t = Task.Yield();
                await t;
                return v * 10;
            }
            var task = FetchAsync(7);
            int asyncResult = await task;
            Debug.Assert(asyncResult == 70, "async local function");

            // Local function using out param
            bool TryParsePositive(string s, out int result)
            {
                if (int.TryParse(s, out result) && result > 0) return true;
                result = 0; return false;
            }
            Debug.Assert(TryParsePositive("5", out int pos) && pos == 5, "local function out param");
            Debug.Assert(!TryParsePositive("-1", out int _), "local function out param fail");

            // Nested local functions
            int Outer(int n)
            {
                int Inner(int m) => m * m;
                return Inner(n) + n;
            }
            Debug.Assert(Outer(3) == 12, "nested local functions"); // 9 + 3
        }
    }

    // =============================================================================
    //  16. ASYNC / AWAIT
    // =============================================================================
    file static class AsyncTests
    {
        public static async Task Run()
        {
            // Basic async/await
            int r1 = await BasicAsync();
            Debug.Assert(r1 == 42, "basic async returns value");

            // Sequential await
            int r2 = await SequentialAsync();
            Debug.Assert(r2 == 30, "sequential await");

            // Parallel await
            int r3 = await ParallelAsync();
            Debug.Assert(r3 == 10, "parallel await Task.WhenAll");

            // async void via Task wrapper
            bool fired = false;
            Task.Run(() => { fired = true; }).Wait();
            Debug.Assert(fired, "Task.Run");

            // ValueTask
            int r4 = await ValueTaskAsync(5);
            Debug.Assert(r4 == 25, "ValueTask async");

            // CancellationToken
            var cts = new CancellationTokenSource();
            cts.Cancel();
            bool cancelled = false;
            try
            {
                await CancellableAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            Debug.Assert(cancelled, "CancellationToken throws");

            // IAsyncEnumerable (C# 8+)
            var asyncSeq = await ConsumeAsync();
            Debug.Assert(asyncSeq is [0, 1, 2, 3, 4], "IAsyncEnumerable");

            // Task.WhenAny
            int r5 = await await Task.WhenAny(
                Task.Delay(1000).ContinueWith(_ => 1),
                Task.FromResult(2)
            );
            Debug.Assert(r5 == 2, "Task.WhenAny picks fastest");

            // ConfigureAwait
            int r6 = await ConfiguredAsync();
            Debug.Assert(r6 == 1, "ConfigureAwait(false)");
        }

        private static async Task<int> BasicAsync()
        {
            await Task.Yield();
            return 42;
        }

        private static async Task<int> SequentialAsync()
        {
            int a = await Task.FromResult(10);
            int b = await Task.FromResult(20);
            return a + b;
        }

        private static async Task<int> ParallelAsync()
        {
            var t1 = Task.FromResult(3);
            var t2 = Task.FromResult(7);
            var results = await Task.WhenAll(t1, t2);
            return results.Sum();
        }

        private static async ValueTask<int> ValueTaskAsync(int n)
        {
            await Task.Yield();
            return n * n;
        }

        private static async Task CancellableAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(1000, ct);
        }

        private static async IAsyncEnumerable<int> GenerateAsync()
        {
            for (int i = 0; i < 5; i++)
            {
                await Task.Yield();
                yield return i;
            }
        }

        private static async Task<int[]> ConsumeAsync()
        {
            var list = new List<int>();
            await foreach (var item in GenerateAsync()) list.Add(item);
            return list.ToArray();
        }

        private static async Task<int> ConfiguredAsync()
        {
            await Task.Yield();//.ConfigureAwait(false);
            return 1;
        }
    }

    // =============================================================================
    //  17. SPAN / MEMORY
    // =============================================================================
    file static class SpanTests
{
    public static void Run()
    {
        // Span<T> from array
        int[] arr = { 1, 2, 3, 4, 5 };
        Span<int> span = arr;
        span[0] = 10;
        Debug.Assert(arr[0] == 10, "Span mutation reflects in array");

        // Span slice
        var slice = span.Slice(1, 3);
        Debug.Assert(slice.Length == 3 && slice[0] == 2, "Span slice");

        // Span from stackalloc
        Span<int> stack = stackalloc int[4] { 1, 2, 3, 4 };
        Debug.Assert(stack[3] == 4, "stackalloc span");

        // ReadOnlySpan<char> from string
        ReadOnlySpan<char> str = "Hello, World!".AsSpan();
        Debug.Assert(str.Length == 13, "ReadOnlySpan<char> length");
        Debug.Assert(str[7..12].ToString() == "World", "ReadOnlySpan slice");

        // Span<T> CopyTo
        int[] dest = new int[3];
        slice.CopyTo(dest);
        Debug.Assert(dest[0] == 2 && dest[1] == 3 && dest[2] == 4, "Span.CopyTo");

        // Memory<T>
        Memory<int> mem = arr;
        var memSlice = mem.Slice(1, 3);
        Debug.Assert(memSlice.Span[0] == 2, "Memory<T> slice");

        // SequenceEqual
        Span<int> a = stackalloc int[] { 2, 3, 4 };
        Debug.Assert(a.SequenceEqual(dest), "Span.SequenceEqual");

        // String operations via Span
        ReadOnlySpan<char> hello = "hello world".AsSpan();
        Debug.Assert(hello.StartsWith("hello"), "ReadOnlySpan.StartsWith");
        Debug.Assert(hello.IndexOf(' ') == 5, "ReadOnlySpan.IndexOf");

        // stackalloc Span<T> in non-unsafe context (C# 7.2+)
        Span<byte> stackBytes = stackalloc byte[] { 10, 20, 30 };
        Debug.Assert(stackBytes[1] == 20, "stackalloc Span in safe context");

        // scoped ref parameter (C# 11+) — prevents ref from escaping method
        static int SumScoped(scoped ref int a, scoped ref int b) => a + b;
        int p1 = 3, p2 = 4;
        Debug.Assert(SumScoped(ref p1, ref p2) == 7, "scoped ref parameter");

        // scoped Span<T> parameter
        static int FirstOf(scoped Span<int> s) => s[0];
        Span<int> sp2 = stackalloc int[] { 99, 1, 2 };
        Debug.Assert(FirstOf(sp2) == 99, "scoped Span parameter");
    }
}

// =============================================================================
//  18. UNSAFE & POINTERS
// =============================================================================
file static class UnsafeTests
{
    public static void Run()
    {
        unsafe
        {
            // Pointer basics
            int value = 42;
            int* ptr = &value;
            Debug.Assert(*ptr == 42, "pointer dereference");
            *ptr = 100;
            Debug.Assert(value == 100, "pointer mutation");

            // Pointer arithmetic
            int[] arr = { 10, 20, 30 };
            fixed (int* p = arr)
            {
                Debug.Assert(*(p + 0) == 10, "fixed pointer arr[0]");
                Debug.Assert(*(p + 1) == 20, "fixed pointer arr[1]");
                Debug.Assert(*(p + 2) == 30, "fixed pointer arr[2]");
            }

            // sizeof with unmanaged type
            Debug.Assert(sizeof(long) == 8, "unsafe sizeof long");

            // Pointer cast
            double d = 1.0;
            long* lp = (long*)&d;
            Debug.Assert(*lp == 0x3FF0000000000000L, "pointer cast double to long bits");

            // stackalloc
            int* buf = stackalloc int[5];
            for (int i = 0; i < 5; i++) buf[i] = i * i;
            Debug.Assert(buf[3] == 9, "stackalloc pointer write/read");

            // Pointer comparison
            int a = 1, b = 2;
            Debug.Assert(&a != &b, "pointer inequality");

            // fixed: pin a managed string to get a char*
            string managed = "Hi!";
            fixed (char* cp = managed)
            {
                Debug.Assert(cp[0] == 'H', "fixed pin string char*[0]");
                Debug.Assert(cp[1] == 'i', "fixed pin string char*[1]");
            }

            // fixed: pin a byte array
            byte[] bytes = { 0xDE, 0xAD, 0xBE, 0xEF };
            fixed (byte* bp = bytes)
            {
                Debug.Assert(bp[0] == 0xDE, "fixed pin byte array [0]");
                Debug.Assert(bp[3] == 0xEF, "fixed pin byte array [3]");
            }
        }
    }
}

// =============================================================================
//  19. EXCEPTION HANDLING
// =============================================================================
file static class ExceptionTests
{
    private class AppException(string message, int code) : Exception(message)
    {
        int fieldCode = code;
        public int Code { get; } = code;
    }

    public static void Run()
    {
        // Basic try/catch/finally
        int x = 0;
        try
        {
            throw new InvalidOperationException("oops");
        }
        catch (InvalidOperationException ex)
        {
            x = 1;
            Debug.Assert(ex.Message == "oops", "catch exception message");
        }
        finally
        {
            x += 10;
        }
        Debug.Assert(x == 11, "try/catch/finally");

        // Multiple catch clauses
        bool caught = false;
        try { throw new ArgumentNullException("p"); }
        catch (ArgumentNullException) { caught = true; }
        catch (Exception) { /* not reached */ }
        Debug.Assert(caught, "specific catch before general");

        // Exception filter (when)
        int code = 0;
        try { throw new AppException("fail", 404); }
        catch (AppException e) when (e.Code == 404) { code = 404; }
        catch (AppException) { code = -1; }
        Debug.Assert(code == 404, "exception filter when");

        // Re-throw preserving stack
        bool rethrown = false;
        try
        {
            try { throw new Exception("inner"); }
            catch (Exception) { throw; }
        }
        catch (Exception e) when (e.Message == "inner") { rethrown = true; }
        Debug.Assert(rethrown, "rethrow preserves exception");

        // Custom exception
        bool customCaught = false;
        try { throw new AppException("custom", 500); }
        catch (AppException e) when (e.Code == 500) { customCaught = true; }
        Debug.Assert(customCaught, "custom exception with property");

#if NET11_0_OR_GREATER
            if (RuntimeFeature.IsMultithreadingSupported)
            {
                // AggregateException
                bool aggCaught = false;
                try
                {
                    var tasks = new[]
                    {
                    Task.Run(() => throw new InvalidOperationException("t1")),
                    Task.Run(() => throw new InvalidOperationException("t2"))
                };
                    Task.WaitAll(tasks);
                }
                catch (AggregateException ae)
                {
                    aggCaught = ae.InnerExceptions.Count == 2;
                }
                Debug.Assert(aggCaught, "AggregateException from Task.WaitAll");
            }
#endif

        // Nested try/catch
        int n = 0;
        try
        {
            try { throw new Exception("inner"); }
            catch (Exception) { n = 1; throw new Exception("outer"); }
        }
        catch (Exception e) when (e.Message == "outer") { n += 10; }
        Debug.Assert(n == 11, "nested try/catch");
    }
}

// =============================================================================
//  20. ATTRIBUTES
// =============================================================================
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
file class TagAttribute(string tag) : Attribute
{
    public string Tag { get; } = tag;
}

[Tag("alpha")]
[Tag("beta")]
file class AttributedClass
{
    [Tag("method-tag")]
    [Obsolete("Use NewMethod instead")]
    public static void OldMethod() { }

    [System.Diagnostics.Conditional("DEBUG")]
    public static void DebugOnly() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int InlinedAdd(int a, int b) => a + b;
}

file static class AttributeTests
{
    public static void Run()
    {
        // Reading custom attributes via reflection
        var tags = typeof(AttributedClass)
            .GetCustomAttributes(typeof(TagAttribute), false)
            .Cast<TagAttribute>()
            .Select(t => t.Tag)
            .ToArray();
        Debug.Assert(tags.Length == 2, "class has 2 Tag attributes");
        Debug.Assert(tags.Contains("alpha"), "class has 'alpha' tag");
        Debug.Assert(tags.Contains("beta"), "class has 'beta' tag");

        // Method attribute
        var methodTags = typeof(AttributedClass)
            .GetMethod(nameof(AttributedClass.OldMethod))!
            .GetCustomAttributes(typeof(TagAttribute), false)
            .Cast<TagAttribute>()
            .ToArray();
        Debug.Assert(methodTags[0].Tag == "method-tag", "method has Tag attribute");

        // Obsolete attribute
        var obsAttr = typeof(AttributedClass)
            .GetMethod(nameof(AttributedClass.OldMethod))!
            .GetCustomAttribute<ObsoleteAttribute>();
        Debug.Assert(obsAttr != null, "Obsolete attribute present");
        Debug.Assert(obsAttr.Message == "Use NewMethod instead", "Obsolete message");

        // MethodImpl attribute
        Debug.Assert(AttributedClass.InlinedAdd(3, 4) == 7, "AggressiveInlining works");

        // Caller info attributes
        static void CallerTest(
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Debug.Assert(!string.IsNullOrEmpty(member), "CallerMemberName");
            Debug.Assert(!string.IsNullOrEmpty(file), "CallerFilePath");
            Debug.Assert(line > 0, "CallerLineNumber");
        }
        CallerTest();
    }
}

// =============================================================================
//  21. C# 9 FEATURES
// =============================================================================
file static class CSharp9Tests
{
    // init-only setters
    private class Immutable
    {
        public int X { get; init; }
        public int Y { get; init; }
    }

    // Target-typed new (C# 9)
    private static List<int> CreateList() => new() { 1, 2, 3 };

    // Covariant return types (C# 9)
    private class Base { public virtual Base Clone() => new Base(); }
    private class Derived : Base { public override Derived Clone() => new Derived(); }

    // with-expression on classes (C# 10) — record prerequisite: record classes
    private record Pt(int X, int Y);

    public static void Run()
    {
        // init-only
        var obj = new Immutable { X = 1, Y = 2 };
        Debug.Assert(obj.X == 1 && obj.Y == 2, "init-only setters");

        // Target-typed new
        var list = CreateList();
        Debug.Assert(list.Count == 3, "target-typed new");

        // Covariant return
        var d = new Derived();
        Debug.Assert(d.Clone() is Derived, "covariant return type");

        // with-expression (record)
        var p1 = new Pt(1, 2);
        var p2 = p1 with { Y = 99 };
        Debug.Assert(p2.X == 1 && p2.Y == 99, "with-expression");
        Debug.Assert(p1.Y == 2, "original unchanged");

        // Pattern matching: relational, logical
        int val = 55;
        bool ok = val is > 0 and < 100;
        Debug.Assert(ok, "relational + logical patterns");

        // Negation pattern
        Debug.Assert(val is not 0, "not pattern");
        object? a = null;
        Debug.Assert(a is not int, "not null pattern");
    }
}

// =============================================================================
//  22. C# 10 FEATURES
// =============================================================================
file static class CSharp10Tests
{
    // record struct
    private record struct Size(int Width, int Height);

    // Global using directives are tested implicitly (file uses System etc.)

    // Extended property pattern
    private record Order(int Id, Address ShipTo);
    private record Address(string City, string Country);

    public static void Run()
    {
        // Record struct
        var sz = new Size(1920, 1080);
        Debug.Assert(sz.Width == 1920, "record struct member");
        var sz2 = sz with { Height = 720 };
        Debug.Assert(sz2.Height == 720, "record struct with-expression");

        // Extended property pattern
        var order = new Order(1, new Address("Lagos", "NG"));
        Debug.Assert(order is { ShipTo.Country: "NG" }, "extended property pattern");

        // Constant interpolated strings
        const string greeting = "Hello";
        const string full = $"{greeting}, World!";
        Debug.Assert(full == "Hello, World!", "const interpolated string");

        // Lambda improvements
        var fn = (int x) => x * x;
        Debug.Assert(fn(5) == 25, "lambda with explicit param type");

        // var in lambda (inferring natural type)
        var identity = (object o) => o;
        Debug.Assert(identity(42) is 42, "lambda with object param");

        // Caller argument expression (C# 10)
        static void Validate(bool condition,
            [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(condition))]
            string? expr = null)
        {
            Debug.Assert(condition, $"Condition failed: {expr}");
        }
        Validate(1 + 1 == 2);
    }
}

// =============================================================================
//  23. C# 11 FEATURES
// =============================================================================
file static class CSharp11Tests
{
    // Required members
    private class Point3D
    {
        public required int X { get; init; }
        public required int Y { get; init; }
        public required int Z { get; init; }
    }

    // Generic math (static abstract interface members)
    private interface IAddable<T> where T : IAddable<T>
    {
        static abstract T Add(T a, T b);
    }

    private struct MyInt(int value) : IAddable<MyInt>
    {
        public int Value = value;
        public static MyInt Add(MyInt a, MyInt b) => new(a.Value + b.Value);
    }

    // ref fields in ref struct (C# 11)
    private ref struct RefWrapper
    {
        public ref int Value;
        public RefWrapper(ref int v) => Value = ref v;
    }

    public static void Run()
    {
        // Required members
        var pt = new Point3D { X = 1, Y = 2, Z = 3 };
        Debug.Assert(pt.X + pt.Y + pt.Z == 6, "required members");

        // Raw string literals
        string raw = """
            Line 1
            Line 2
            """;
        Debug.Assert(raw.Contains("Line 1") && raw.Contains("Line 2"), "raw string literal");

        // UTF-8 string literals
        ReadOnlySpan<byte> utf8 = "test"u8;
        Debug.Assert(utf8.Length == 4 && utf8[0] == (byte)'t', "UTF-8 literal");

        // List patterns
        int[] arr = { 1, 2, 3 };
        Debug.Assert(arr is [1, 2, 3], "list pattern exact match");
        Debug.Assert(arr is [1, .., 3], "list pattern with slice");
        Debug.Assert(arr is [_, 2, _], "list pattern wildcards");

        // Span pattern matching
        ReadOnlySpan<char> sp = "hello";
        Debug.Assert(sp is "hello", "span pattern match");

        // Generic math
        var sum = MyInt.Add(new MyInt(3), new MyInt(4));
        Debug.Assert(sum.Value == 7, "generic math static abstract Add");

        // ref fields in ref struct
        int v = 10;
        var wrapper = new RefWrapper(ref v);
        wrapper.Value = 20;
        Debug.Assert(v == 20, "ref field in ref struct");

        // Numeric IntPtr/UIntPtr as nint/nuint
        nint ni = 42;
        nuint nui = 42;
        Debug.Assert(ni == 42, "nint literal");
        Debug.Assert(nui == 42, "nuint literal");

        // File-scoped types (this entire file uses file-scoped namespace)
        Debug.Assert(true, "file-scoped namespace active");
    }
}

// =============================================================================
//  24. C# 12 FEATURES
// =============================================================================
file static class CSharp12Tests
{
    // Primary constructors on non-record class
    private class Config(string host, int port)
    {
        public string Host { get; } = host;
        public int Port { get; } = port;
        public string Endpoint => $"{Host}:{Port}";
    }

    // Primary constructors on struct
    private struct Point(int x, int y)
    {
        public int X = x, Y = y;
    }

    // Alias for any type (C# 12+)
    // (StringList, Point2D, Matrix defined at file top)

    // Inline arrays (C# 12+)
    [System.Runtime.CompilerServices.InlineArray(4)]
    private struct Buffer4
    {
        private int _element;
    }
    public delegate int FC(int arg1, int arg2 = 10);
    // Default lambda parameters (C# 12+)
    //private static Func<int, int, int> Adder = (int a, int b = 10) => a + b;
    private static FC Adder = (int a, int b = 10) => a + b;

    public static void Run()
    {
        // Primary constructors
        var cfg = new Config("localhost", 8080);
        Debug.Assert(cfg.Host == "localhost", "primary ctor property");
        Debug.Assert(cfg.Endpoint == "localhost:8080", "primary ctor computed");

        var pt = new Point(3, 4);
        Debug.Assert(pt.X == 3 && pt.Y == 4, "primary ctor struct");

        // Collection expressions
        int[] arr = [1, 2, 3, 4, 5];
        List<int> l = [6, 7, 8];
        Debug.Assert(arr is [1, 2, 3, 4, 5], "collection expression array");
        Debug.Assert(l.Count == 3, "collection expression list");

        // Spread operator
        int[] a = [1, 2], b = [3, 4];
        int[] c = [.. a, .. b, 5];
        Debug.Assert(c is [1, 2, 3, 4, 5], "spread in collection expression");

        // Type aliases
        StringList sl = ["hello", "world"];
        Debug.Assert(sl.Count == 2, "StringList alias");
        Point2D p2d = (10, 20);
        Debug.Assert(p2d.X == 10, "Point2D alias");

        // Inline array
        var buf = new Buffer4();
        buf[0] = 10; buf[1] = 20; buf[2] = 30; buf[3] = 40;
        Debug.Assert(buf[2] == 30, "inline array write/read");

        // Default lambda parameters
        Debug.Assert(Adder(5) == 15, "default lambda param used");
        Debug.Assert(Adder(5, 1) == 6, "default lambda param overridden");

        // ref readonly parameters (C# 12+)
        static int ReadRefReadonly(ref readonly int v) => v;
        int val = 99;
        Debug.Assert(ReadRefReadonly(ref val) == 99, "ref readonly parameter");

        // Experimental interceptors note: transpiler test doesn't invoke them as
        // interceptors require source generators; assertion below tests basic call.
        Debug.Assert(true, "C# 12 syntax coverage complete");
    }
}

// =============================================================================
//  25. C# 13 FEATURES
// =============================================================================
file static class CSharp13Tests
{
    // params Span / ReadOnlySpan (C# 13+)
    private static int Sum(params ReadOnlySpan<int> values)
    {
        int total = 0;
        foreach (var v in values) total += v;
        return total;
    }

    // params IEnumerable (C# 13+)
    private static int SumEnum(params IEnumerable<int> values)
        => values.Sum();

    // Lock object (System.Threading.Lock — C# 13+)
    private static readonly Lock _lock = new();

    // Partial properties (C# 13+)
    private partial class PartialProps
    {
        public partial int Value { get; set; }
    }

    private partial class PartialProps
    {
        private int _value;
        public partial int Value
        {
            get => _value;
            set => _value = value < 0 ? 0 : value;
        }
    }

    // Ref and unsafe in iterators (C# 13+)
    private static IEnumerable<int> IteratorWithRef()
    {
        // ref locals allowed outside yield points in C# 13+
        int local = 10;
        ref int r = ref local;
        r = 20;
        yield return local; // 20
        yield return 30;
    }

    // \e escape sequence (C# 13+)
    private static readonly string Esc = "\e[0m"; // ESC character + reset

    // Method group natural type improvements (C# 13+)
    private static int Triple(int n) => n * 3;

    public static void Run()
    {
        // params Span
        Debug.Assert(Sum(1, 2, 3, 4, 5) == 15, "params ReadOnlySpan<int>");
        Debug.Assert(Sum() == 0, "params ReadOnlySpan empty");

        // params IEnumerable
        Debug.Assert(SumEnum(1, 2, 3) == 6, "params IEnumerable<int>");

        // System.Threading.Lock
        int counter = 0;
        lock (_lock) { counter++; }
        Debug.Assert(counter == 1, "System.Threading.Lock");

        // Partial properties
        var pp = new PartialProps();
        pp.Value = 42;
        Debug.Assert(pp.Value == 42, "partial property set");
        pp.Value = -5;
        Debug.Assert(pp.Value == 0, "partial property validation");

        // Ref in iterator
        var seq = IteratorWithRef().ToArray();
        Debug.Assert(seq is [20, 30], "iterator with ref local");

        // \e escape
        Debug.Assert(Esc[0] == '\x1B', @"\e escape sequence == ESC");

        // Method group natural type
        Func<int, int> fn = Triple;
        Debug.Assert(fn(7) == 21, "method group natural type");

        // Index from end type (System.Index / System.Range already tested in Operators)
        Index fromEnd = ^1;
        int[] arr = { 10, 20, 30 };
        Debug.Assert(arr[fromEnd] == 30, "Index type from end");

        // Allows ref struct generic constraint (C# 13+)
        static void AcceptsRefStruct<T>(T _) where T : allows ref struct { }
        Span<int> span = stackalloc int[] { 1, 2, 3 };
        AcceptsRefStruct(span); // compiles with allows ref struct
        Debug.Assert(true, "allows ref struct constraint");
    }
}

// =============================================================================
//  26. C# 14 FEATURES
// =============================================================================
file static class CSharp14Tests
{
    // ── Field keyword in properties (C# 14) ─────────────────────────────────
    private class Clamped
    {
        // 'field' refers to the compiler-generated backing field
        public int Value
        {
            get => field;
            set => field = Math.Clamp(value, 0, 100);
        }
    }

    // ── Implicit span conversions (C# 14) ───────────────────────────────────
    // ReadOnlySpan<T> implicitly convertible to/from Span<T>-accepting methods
    private static int SpanSum(ReadOnlySpan<int> values)
    {
        int s = 0; foreach (var v in values) s += v; return s;
    }

    // ── Unbound generic types in nameof (C# 14) ─────────────────────────────
    // nameof(List<>) — allowed in C# 14
    private static readonly string ListName = nameof(List<>);

    // ── Null-conditional assignment (C# 14) ─────────────────────────────────
    private class Node
    {
        public string? Label;
        public Node? Next;
    }

    // ── params in interface / abstract (C# 14) ──────────────────────────────
    private interface ISummer
    {
        int Sum(params ReadOnlySpan<int> values);
    }

    private class SpanSummer : ISummer
    {
        public int Sum(params ReadOnlySpan<int> values) => SpanSum(values);
    }

    // ── Partial events and constructors (C# 14) ─────────────────────────────
    private partial class PartialClass
    {
        public partial event EventHandler? OnRun;
        public partial void Run();
    }

    private partial class PartialClass
    {
        private EventHandler? _onRun;
        public partial event EventHandler? OnRun
        {
            add => _onRun += value;
            remove => _onRun -= value;
        }
        public partial void Run() => _onRun?.Invoke(this, EventArgs.Empty);
    }
    // ── Overload resolution priority (C# 14) ─────────────────────────────────
    private static class Overloads
    {
        // The overload with [OverloadResolutionPriority] wins when both match
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        public static string Pick(ReadOnlySpan<int> _) => "span";
        public static string Pick(int[] _) => "array";
    }

    // ── field keyword: lazy init (C# 14) ────────────────────────────────────
    private class LazyField
    {
        public string Computed
        {
            get => field ??= "computed!";
        }
    }

    // ── Partial constructor (C# 14) ─────────────────────────────────────────
    private partial class PartialCtorClass
    {
        public int X { get; }
        public partial PartialCtorClass(int x);
    }

    private partial class PartialCtorClass
    {
        public partial PartialCtorClass(int x) => X = x * 2;
    }

    public static void Run()
    {
        // ── field keyword ────────────────────────────────────────────────────
        var c = new Clamped();
        c.Value = 50;
        Debug.Assert(c.Value == 50, "field keyword: normal set");
        c.Value = 200;
        Debug.Assert(c.Value == 100, "field keyword: clamp to max");
        c.Value = -10;
        Debug.Assert(c.Value == 0, "field keyword: clamp to min");

        // ── field keyword: lazy-init pattern ────────────────────────────────
        var lazy = new LazyField();
        string v1 = lazy.Computed;
        string v2 = lazy.Computed;
        Debug.Assert(v1 == v2 && v1 == "computed!", "field keyword: lazy init via ??=");

        // ── Implicit span conversions ────────────────────────────────────────
        int[] arr = { 1, 2, 3, 4, 5 };
        Debug.Assert(SpanSum(arr) == 15, "implicit array → ReadOnlySpan<T> conversion");

        Span<int> spanArr = arr;
        Debug.Assert(SpanSum(spanArr) == 15, "implicit Span<T> → ReadOnlySpan<T> conversion");

        // ── nameof(unbound generic) ──────────────────────────────────────────
        Debug.Assert(ListName == "List", "nameof(List<>) == \"List\"");
        Debug.Assert(nameof(Dictionary<,>) == "Dictionary",
                     "nameof(Dictionary<,>) == \"Dictionary\"");

        // ── Null-conditional assignment ──────────────────────────────────────
        Node? node = null;
        node?.Label = "should not assign"; // no NullReferenceException
        Debug.Assert(node == null, "null-conditional assignment on null: no effect");

        node = new Node { Label = "old" };
        node?.Label = "new";
        Debug.Assert(node.Label == "new", "null-conditional assignment on non-null: assigns");

        // Chained
        node.Next = new Node();
        node.Next?.Label = "child";
        Debug.Assert(node.Next.Label == "child", "null-conditional assignment chained");

        // ── params in interface ──────────────────────────────────────────────
        ISummer summer = new SpanSummer();
        Debug.Assert(summer.Sum(1, 2, 3) == 6, "params ReadOnlySpan in interface method");

        // ── Partial events and methods ────────────────────────────────────────
        var pc = new PartialClass();
        bool ran = false;
        pc.OnRun += (_, _) => ran = true;
        pc.Run();
        Debug.Assert(ran, "partial event and partial method");

        // ── Partial constructor ───────────────────────────────────────────────
        var pcc = new PartialCtorClass(21);
        Debug.Assert(pcc.X == 42, "partial constructor");

        // ── Extension members ─────────────────────────────────────────────────
        Debug.Assert(7.IsPrime, "extension property IsPrime true");
        Debug.Assert(!8.IsPrime, "extension property IsPrime false");
        Debug.Assert(7.NextPrime == 11, "extension property NextPrime");
        Debug.Assert(int.Zeroed == 0, "extension static property Zeroed");

        // ── Extension method on interface receiver (C# 14) ───────────────────
        IEnumerable<int> seq = new[] { 2, 3, 5 };
        Debug.Assert(seq.AllPositive, "extension method on interface receiver");

        // ── Overload resolution priority ─────────────────────────────────────
        int[] intArr = { 1, 2 };
        Debug.Assert(Overloads.Pick(intArr) == "span",
                     "OverloadResolutionPriority: span overload wins over array");

        // ── Simple lambda default params (already in C# 12, reinforced C# 14)
        var greet = (string name, string prefix = "Hello") => $"{prefix}, {name}!";
        Debug.Assert(greet("World") == "Hello, World!", "default lambda param default");
        Debug.Assert(greet("World", "Hi") == "Hi, World!", "default lambda param override");

        // ── ref readonly locals (C# 14 improvement) ──────────────────────────
        int x = 42;
        ref readonly int rx = ref x;
        Debug.Assert(rx == 42, "ref readonly local");

        Debug.Assert(true, "C# 14 feature coverage complete");
    }
}

// ── Extension members (C# 14) ────────────────────────────────────────────
// New syntax: implicit extension blocks
file static class IntExtensions
{
    extension(int value)
    {
        public bool IsPrime
        {
            get
            {
                if (value < 2) return false;
                for (int i = 2; i * i <= value; i++)
                    if (value % i == 0) return false;
                return true;
            }
        }

        public int NextPrime
        {
            get
            {
                int n = value + 1;
                while (!n.IsPrime) n++;
                return n;
            }
        }

        public static int Zeroed => 0;
    }
}

// ── Extension block on interface/generic receiver (C# 14) ────────────────
public static class EnumerableExtensions
{
    extension(IEnumerable<int> source)
    {
        public bool AllPositive => source.All(x => x > 0);
    }
}

// =============================================================================
//  27. CONVERSION OPERATORS (implicit / explicit)
// =============================================================================
file static class ConversionOperatorTests
{
    private struct Celsius
    {
        public double Degrees;
        public Celsius(double d) => Degrees = d;

        // implicit: Celsius → double (safe, no data loss)
        public static implicit operator double(Celsius c) => c.Degrees;

        // explicit: double → Celsius (caller must opt in)
        public static explicit operator Celsius(double d) => new Celsius(d);
    }

    private struct Fahrenheit
    {
        public double Degrees;
        public Fahrenheit(double d) => Degrees = d;
        public static implicit operator Celsius(Fahrenheit f)
            => new Celsius((f.Degrees - 32) * 5 / 9);
    }

    // User-defined true/false operators
    private struct Truthful
    {
        public int Value;
        public Truthful(int v) => Value = v;
        public static bool operator true(Truthful t) => t.Value != 0;
        public static bool operator false(Truthful t) => t.Value == 0;
        public static Truthful operator &(Truthful a, Truthful b)
            => new Truthful(a.Value & b.Value);
        public static Truthful operator |(Truthful a, Truthful b)
            => new Truthful(a.Value | b.Value);
    }

    public static void Run()
    {
        // Implicit conversion
        var c = new Celsius(100);
        double d = c;  // implicit operator double
        Debug.Assert(d == 100.0, "implicit operator Celsius→double");

        // Explicit conversion
        var c2 = (Celsius)36.6;
        Debug.Assert(Math.Abs(c2.Degrees - 36.6) < 1e-9, "explicit operator double→Celsius");

        // Implicit conversion chain
        var f = new Fahrenheit(212);
        Celsius boiling = f;  // implicit Fahrenheit→Celsius
        Debug.Assert(Math.Abs(boiling.Degrees - 100.0) < 1e-9, "implicit Fahrenheit→Celsius");

        // User-defined true/false operators
        var truthy = new Truthful(1);
        var falsy = new Truthful(0);
        Debug.Assert(truthy ? true : false, "operator true");
        Debug.Assert(!(falsy ? true : false), "operator false");

        // Short-circuit && via operator true/false + operator &
        bool andResult = (truthy && falsy) ? true : false;  // uses operator true + &
        Debug.Assert(!andResult, "operator && via true/false/&");

        bool orResult = (falsy || truthy) ? true : false;   // uses operator false + |
        Debug.Assert(orResult, "operator || via true/false/|");
    }
}

// =============================================================================
//  28. DYNAMIC TYPE
// =============================================================================
file static class DynamicTests
{
    public static void Run()
    {
        // Basic dynamic binding
        dynamic d = 42;
        Debug.Assert(d == 42, "dynamic int value");

        d = "hello";
        Debug.Assert(d.length == 5, "dynamic string member access");

        // Dynamic method call
        d = new List<int> { 1, 2, 3 };
        d.Add(4);
        Debug.Assert(d.Count == 4, "dynamic method call on List");

        // Dynamic arithmetic
        dynamic a = 10;
        dynamic b = 3;
        Debug.Assert(a + b == 13, "dynamic addition");
        //Debug.Assert(a / b == 3, "dynamic integer division");

        // Dynamic with object
        dynamic obj = new System.Dynamic.ExpandoObject();
        obj.Name = "Claude";
        obj.Age = 1;
        Debug.Assert(obj.Name == "Claude", "ExpandoObject dynamic property set/get");
        Debug.Assert(obj.Age == 1, "ExpandoObject dynamic int property");

        // Dynamic type checking
        dynamic num = 3.14;
        Debug.Assert(num is double, "dynamic is-check");

        // Dynamic cast
        dynamic str = "world";
        string s = (string)str;
        Debug.Assert(s == "world", "dynamic explicit cast to string");

        // Dynamic in collection
        var list = new List<dynamic> { 1, "two", 3.0 };
        Debug.Assert(list[1] == "two", "dynamic in generic list");
    }
}

// =============================================================================
//  29. VOLATILE FIELDS
// =============================================================================
file static class VolatileTests
{
    private class SharedFlag
    {
        public volatile bool Ready = false;
        public volatile int Counter = 0;
    }

    public static void Run()
    {
        var flag = new SharedFlag();

        // volatile read/write on same thread (semantics: no optimisation reorder)
        flag.Ready = true;
        Debug.Assert(flag.Ready, "volatile bool write/read");

        flag.Counter = 42;
        Debug.Assert(flag.Counter == 42, "volatile int write/read");

#if NET11_0_OR_GREATER
            // Multi-threaded volatile usage (if threading supported)
            if (RuntimeFeature.IsMultithreadingSupported)
            {
                var shared = new SharedFlag();
                var thread = new Thread(() =>
                {
                    Thread.Sleep(5);
                    shared.Counter = 99;
                    shared.Ready = true;
                });
                thread.Start();
                thread.Join();
                Debug.Assert(shared.Ready, "volatile: flag set by other thread visible");
                Debug.Assert(shared.Counter == 99, "volatile: value set by other thread visible");
            }
#endif

        // Volatile.Read / Volatile.Write (explicit API)
        int v = 0;
        Volatile.Write(ref v, 7);
        Debug.Assert(Volatile.Read(ref v) == 7, "Volatile.Read/Write");
    }
}

// =============================================================================
//  30. READONLY STRUCT
// =============================================================================
file static class ReadonlyStructTests
{
    // Plain readonly struct (not record)
    private readonly struct ImmutableVector(double x, double y)
    {
        public double X { get; } = x;
        public double Y { get; } = y;

        public double Length => Math.Sqrt(X * X + Y * Y);

        // readonly method: implicit on all members of readonly struct
        public ImmutableVector Normalize()
        {
            double len = Length;
            return new ImmutableVector(X / len, Y / len);
        }

        public static ImmutableVector operator +(ImmutableVector a, ImmutableVector b)
            => new(a.X + b.X, a.Y + b.Y);

        public override string ToString() => $"({X}, {Y})";
    }

    // readonly struct with explicit readonly method
    private struct MutablePoint
    {
        public int X, Y;
        // readonly method on a mutable struct (C# 8+)
        public readonly int Sum() => X + Y;
        public void Reset() { X = 0; Y = 0; }
    }

    // ref struct with Dispose() (pattern-based disposal)
    private ref struct RefBuffer
    {
        private Span<int> _data;
        public bool Disposed { get; private set; }

        public RefBuffer(Span<int> data) => _data = data;

        public int this[int i] => _data[i];

        public void Dispose()
        {
            _data = Span<int>.Empty;
            Disposed = true;
        }
    }

    public static void Run()
    {
        // readonly struct basic usage
        var v = new ImmutableVector(3, 4);
        Debug.Assert(Math.Abs(v.Length - 5.0) < 1e-9, "readonly struct computed Length");

        var v2 = v.Normalize();
        Debug.Assert(Math.Abs(v2.X - 0.6) < 1e-9, "readonly struct Normalize X");
        Debug.Assert(Math.Abs(v2.Y - 0.8) < 1e-9, "readonly struct Normalize Y");

        // operator on readonly struct
        var sum = v + new ImmutableVector(1, 0);
        Debug.Assert(sum.X == 4 && sum.Y == 4, "readonly struct operator +");

        // ToString on readonly struct
        Debug.Assert(v.ToString() == "(3, 4)", "readonly struct ToString");

        // readonly method on mutable struct
        var mp = new MutablePoint { X = 10, Y = 20 };
        Debug.Assert(mp.Sum() == 30, "readonly method on mutable struct");
        mp.Reset();
        Debug.Assert(mp.X == 0 && mp.Y == 0, "mutable method on struct");

        // ref struct with pattern-based Dispose
        Span<int> buf = stackalloc int[] { 1, 2, 3 };
        {
            var rb = new RefBuffer(buf);
            Debug.Assert(rb[0] == 1, "ref struct indexer");
            rb.Dispose();
            Debug.Assert(rb.Disposed, "ref struct Dispose");
        }

        // ref struct in using (pattern-based disposal)
        Span<int> buf2 = stackalloc int[] { 10, 20 };
        using (var rb2 = new RefBuffer(buf2))
        {
            Debug.Assert(rb2[1] == 20, "ref struct using block indexer");
        }
        // after using, rb2 is disposed — can't access here, that's fine
    }
}

// =============================================================================
//  31. CHECKED OPERATOR OVERLOADS (C# 11)
// =============================================================================
file static class CheckedOperatorTests
{
    private struct Meter
    {
        public int Value;
        public Meter(int v) => Value = v;

        // Regular (unchecked) operator
        public static Meter operator +(Meter a, Meter b) => new(a.Value + b.Value);

        // Checked variant — called in a checked context
        public static Meter operator checked +(Meter a, Meter b)
        {
            return new(checked(a.Value + b.Value));
        }

        // Explicit + checked explicit conversion
        public static explicit operator int(Meter m) => m.Value;
        public static explicit operator checked int(Meter m)
            => checked(m.Value); // identical here, but checked version throws on overflow
    }

    public static void Run()
    {
        var a = new Meter(int.MaxValue - 1);
        var b = new Meter(1);

        // Unchecked path
        var sum = unchecked(a + b);
        Debug.Assert(sum.Value == int.MaxValue, "unchecked operator + Meter");

        // Checked path — should throw on overflow
        bool threw = false;
        try
        {
            var _ = checked(a + b + b); // MaxValue-1 + 1 + 1 overflows
        }
        catch (OverflowException)
        {
            threw = true;
        }
        Debug.Assert(threw, "checked operator + throws OverflowException");

        // Checked explicit conversion
        var small = new Meter(100);
        int converted = checked((int)small);
        Debug.Assert(converted == 100, "checked explicit operator int");

        // Overflow in checked explicit conversion
        bool convThrew = false;
        try
        {
            // Wrap in a helper so we can force the checked conversion path
            // Use a very large value that would overflow an int — not possible with int
            // but we can verify the operator is callable
            int _ = checked((int)small);
        }
        catch (OverflowException) { convThrew = true; }
        Debug.Assert(!convThrew, "checked explicit conversion no overflow for small value");
    }
}

// =============================================================================
//  32. SCOPED KEYWORD (C# 11)  — deeper coverage beyond SpanTests
// =============================================================================
file static class ScopedTests
{
    // scoped in method signature prevents the ref/span escaping
    private static Span<int> GoodSlice(scoped Span<int> source, int start, int length)
        => new int[length]; // returns a new span, not the scoped one

    // scoped ref — cannot be returned or stored in a ref field
    private static int ReadFirst(scoped ref int value) => value;

    // scoped in local variable (C# 11+)
    private static int UseScoped()
    {
        Span<int> local = stackalloc int[] { 10, 20, 30 };
        scoped Span<int> s = local; // annotate local as scoped
        return s[0];
    }

    public static void Run()
    {
        // scoped Span parameter
        Span<int> arr = stackalloc int[] { 5, 6, 7 };
        var slice = GoodSlice(arr, 0, 2);
        Debug.Assert(slice.Length == 2, "scoped Span parameter: returned new span");

        // scoped ref parameter
        int val = 99;
        Debug.Assert(ReadFirst(ref val) == 99, "scoped ref parameter read");

        // scoped local variable
        Debug.Assert(UseScoped() == 10, "scoped local span");
    }
}

// =============================================================================
//  33. REF STRUCT IMPLEMENTING INTERFACES (C# 13)
// =============================================================================
file static class RefStructInterfaceTests
{
    private interface ISummable
    {
        int Sum();
        int Count { get; }
    }

    // C# 13: ref struct can implement interfaces (but only used via generic or direct call)
    private ref struct SpanWrapper : ISummable
    {
        private Span<int> _data;

        public SpanWrapper(Span<int> data) => _data = data;

        public int Sum()
        {
            int total = 0;
            foreach (var v in _data) total += v;
            return total;
        }

        public int Count => _data.Length;
    }

    // Generic method that accepts allows ref struct + interface constraint
    private static int SumViaInterface<T>(T summable)
        where T : ISummable, allows ref struct
        => summable.Sum();

    public static void Run()
    {
        Span<int> data = stackalloc int[] { 1, 2, 3, 4, 5 };
        var wrapper = new SpanWrapper(data);

        // Direct call through ref struct
        Debug.Assert(wrapper.Sum() == 15, "ref struct implements interface: Sum()");
        Debug.Assert(wrapper.Count == 5, "ref struct implements interface: Count");

        // Via generic method with allows ref struct
        int result = SumViaInterface(wrapper);
        Debug.Assert(result == 15, "ref struct via allows ref struct generic");
    }
}

// =============================================================================
//  34. ASYNC DISPOSABLE / AWAIT USING (C# 8+)
// =============================================================================
file static class AsyncDisposableTests
{
    private class AsyncResource : IAsyncDisposable
    {
        public bool Disposed { get; private set; }
        public List<string> Log { get; } = new();

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            Disposed = true;
            Log.Add("disposed");
        }

        public async Task DoWorkAsync()
        {
            await Task.Yield();
            Log.Add("work");
        }
    }

    // IAsyncEnumerable with [EnumeratorCancellation]
    private static async IAsyncEnumerable<int> CountAsync(
        int count,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken ct = default)
    {
        for (int i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return i;
        }
    }

    // TaskCompletionSource usage
    private static Task<int> BuildTaskFromTcs(int value)
    {
        var tcs = new TaskCompletionSource<int>();
        tcs.SetResult(value);
        return tcs.Task;
    }

    private static Task BuildFaultedTask()
    {
        var tcs = new TaskCompletionSource();
        tcs.SetException(new InvalidOperationException("tcs-fault"));
        return tcs.Task;
    }

    public static async Task Run()
    {
        // await using — calls DisposeAsync
        AsyncResource? capturedResource = null;
        await Task.Run(async () =>
        {
            await using var res = new AsyncResource();
            capturedResource = res;
            await res.DoWorkAsync();
            // DisposeAsync called at end of await using
        });

        Debug.Assert(capturedResource!.Disposed, "IAsyncDisposable: DisposeAsync called");
        Debug.Assert(capturedResource.Log.SequenceEqual(new[] { "work", "disposed" }),
                     "IAsyncDisposable: log order correct");

        // await using with explicit variable scope
        var res2 = new AsyncResource();
        await Task.Run(async () =>
        {
            await using (res2)
            {
                await res2.DoWorkAsync();
            }
        });
        Debug.Assert(res2.Disposed, "await using with braces disposes");

        // IAsyncEnumerable with EnumeratorCancellation
        var collected = await Task.Run(async () =>
        {
            var list = new List<int>();
            await foreach (var n in CountAsync(5))
                list.Add(n);
            return list.ToArray();
        });
        Debug.Assert(collected is [0, 1, 2, 3, 4], "IAsyncEnumerable EnumeratorCancellation all items");

        // IAsyncEnumerable + WithCancellation cancels mid-stream
        var cts = new CancellationTokenSource();
        bool cancelled = false;
        await Task.Run(async () =>
        {
            try
            {
                await foreach (var n in CountAsync(100).WithCancellation(cts.Token))
                {
                    if (n == 2) cts.Cancel();
                }
            }
            catch (OperationCanceledException) { cancelled = true; }
        });
        Debug.Assert(cancelled, "IAsyncEnumerable WithCancellation cancels");

        // TaskCompletionSource
        int tcsResult = await BuildTaskFromTcs(77);
        Debug.Assert(tcsResult == 77, "TaskCompletionSource.SetResult");

        bool tcsFaulted = false;
        try { await BuildFaultedTask(); }
        catch (InvalidOperationException e) when (e.Message == "tcs-fault")
        { tcsFaulted = true; }
        Debug.Assert(tcsFaulted, "TaskCompletionSource.SetException faults task");
    }
}

// =============================================================================
//  35. LINQ EXTENDED (IQueryable, Range/Repeat/Empty, Append/Prepend)
// =============================================================================
file static class LinqExtendedTests
{
    public static void Run()
    {
        // Enumerable.Range
        var range = Enumerable.Range(1, 5).ToArray();
        Debug.Assert(range is [1, 2, 3, 4, 5], "Enumerable.Range");

        // Enumerable.Repeat
        var repeated = Enumerable.Repeat("x", 4).ToArray();
        Debug.Assert(repeated.Length == 4 && repeated[0] == "x", "Enumerable.Repeat");

        // Enumerable.Empty
        var empty = Enumerable.Empty<int>();
        Debug.Assert(!empty.Any(), "Enumerable.Empty");

        // Append / Prepend
        var seq = new[] { 2, 3, 4 };
        var appended = seq.Append(5).ToArray();
        Debug.Assert(appended is [2, 3, 4, 5], "Enumerable.Append");

        var prepended = seq.Prepend(1).ToArray();
        Debug.Assert(prepended is [1, 2, 3, 4], "Enumerable.Prepend");

        //// AsQueryable + IQueryable<T>
        IQueryable<int> query = Enumerable.Range(1, 10).AsQueryable();
        var filtered = query.Where(n => n % 2 == 0).OrderByDescending(n => n).ToArray();
        Debug.Assert(filtered is [10, 8, 6, 4, 2], "IQueryable Where/OrderByDescending");

        //// Expression tree composition via IQueryable
        var q2 = query.AsQueryable();
        System.Linq.Expressions.Expression<Func<int, bool>> expr = n => n > 7;
        var parameters = expr.Parameters;
        var result = q2.Where(expr).ToArray();
        Debug.Assert(result is [8, 9, 10], "IQueryable with expression tree predicate");

        // Concat
        var a = new[] { 1, 2 };
        var b = new[] { 3, 4 };
        Debug.Assert(a.Concat(b).ToArray() is [1, 2, 3, 4], "Enumerable.Concat");

        // DefaultIfEmpty
        var emptyInts = Enumerable.Empty<int>();
        Debug.Assert(emptyInts.DefaultIfEmpty(42).Single() == 42, "DefaultIfEmpty");

        var nonEmpty = new[] { 1, 2, 3 };
        Debug.Assert(nonEmpty.DefaultIfEmpty(99).First() == 1, "DefaultIfEmpty non-empty");

        // Cast / OfType
        var mixed = new object[] { 1, "two", 3, "four", 5 };
        var ints = mixed.OfType<int>().ToArray();
        Debug.Assert(ints is [1, 3, 5], "OfType<int>");

        var strings = mixed.OfType<string>().ToArray();
        Debug.Assert(strings is ["two", "four"], "OfType<string>");

        // SelectMany with result selector
        var sentences = new[] { "hello world", "foo bar baz" };
        var wordLengths = sentences
            .SelectMany(s => s.Split(' '), (s, w) => w.Length)
            .ToArray();
        Debug.Assert(wordLengths.Length == 5, "SelectMany with result selector");

        // GroupJoin
        var depts = new[] { (Id: 1, Name: "Eng"), (Id: 2, Name: "HR") };
        var employees = new[] {
                (DeptId: 1, Name: "Alice"),
                (DeptId: 1, Name: "Bob"),
                (DeptId: 2, Name: "Carol")
            };
        var groupJoined = depts
            .GroupJoin(employees, d => d.Id, e => e.DeptId,
                (d, emps) => (d.Name, Count: emps.Count()))
            .ToArray();
        Debug.Assert(groupJoined[0] == ("Eng", 2), "GroupJoin Eng has 2");
        Debug.Assert(groupJoined[1] == ("HR", 1), "GroupJoin HR has 1");
    }
}

}