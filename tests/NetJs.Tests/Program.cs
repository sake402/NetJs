using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NetJs.Tests
{
    public static class Program
    {
        [ModuleInitializer]
        internal static void MInit()
        {

        }
        //public struct Foo
        //{
        //    public static implicit operator Foo(int x)
        //    {
        //        return default(Foo);
        //    }
        //}

        //internal static T[] _Empty<T>()
        //{
        //    return _EmptyArray<T>.Value;
        //}
        
        //static class _EmptyArray<T>
        //{
        //    internal static readonly T[] Value = new T[0];
        //    static long RR
        //    {
        //        get
        //        {
        //            return 0;
        //        }
        //    }

        //    static long R => 0;

        //    static long RRR()=> 0;
        //    static long RRRR()
        //    {
        //        return 0;
        //    }
        //}
        //static void M<TValue>()
        //{
        //    var eq = EqualityComparer<TValue>.Default;

        //}

        public static void Main()
        {
            int start = Environment.TickCount;
            BooleanTests.Run();
            CharTests.Run();
            NumericTests.Run();
            ArrayTests.Run();
            StringTests.Run();
            DateTimeTests.Run();
            DateTimeExhaustiveTests.Run();
            DateTimeOffsetTests.Run();
            DateTimeOffsetExhaustiveTests.Run();
            CSharp14TranspilerTests.Run();
            CSharp14TranspilerTestsV2.Run();
            int end = Environment.TickCount;
            Debug.WriteLine($"✅ All Tests completed in {end - start}ms.");
            //int[] aaa = [1,2,3];
            //List<int> abc = [1, 2, 3, ..aaa];
            //int[] a = [1, 2, 3];
            //Array.Resize(ref a, 5);
            //Foo foo = 1;
            //var eq = EqualityComparer<int>.Default;
            //int[] avf = [1, 2];
            //avf[0] -= avf[1];
            //var Zero = new TimeSpan(0);
            ////var z1 = Zero;
            ////char abc = (char)0x1111;
            ////char bde = (abc & 0x3FF).As<char>();
            //long lg = 1;
            //var lgh = 2 + lg;
            //int div = 10;
            //var llll = lg / div;
            //var lll = lg / 10;
            //lg += 2;
            //bool ok = lg == 3;
            //ok = lg != 0;
            //ok = lg > 3;
            //ok = lg < 3;
            //long hh = lg >> 3;
            //var ll = lg / 1000.0;
            //long lg2 = lg;
            //object avdgd = null;
            //avdgd["ab"] = 1;
            //int[] arr = [];
            //arr[0] = arr[1];
            //string acv = "aa";
            //var ab = acv[0];
            //SortedList<int, int>.IsCompatibleKey(null);
            //Script.StrictNotEqual(null, null);
            //var st = Stream.Null;
            //ReadOnlyMemory<char> memory = default;
            //memory.Span.Trim();
            //object o = (ReadOnlySpan<char>?)null;
            //((Dictionary<string, string>)o)[""] = null;
            //IList<int> items = default;
            //items[0] = 1;
            //var A = new Dictionary<string, string>();
            ////var x = A[""];
            //A[""] = "";
            //if (A.TryGetValue("a", out var abc))
            //{
            //    abc.ToString()?.ToString();
            //}
            ////o?.ToString().Split(';');

            //int i = 0;
            //i.ToString();
            //var b = i.ToString()?.ToString();
            //var bb = i.ToString()?.ToString()?.ToString()?.ToString()?.ToString()?.Split(';')?.LastOrDefault()?.ToString()?.ToString()?.ToString()?.ToString()?.ToString();
            //int[] abcd = [1, 2, 3, 4];
            ////var descriptors = abcd.Select(s => s>0);
            //int[] s = [1];

            //s.Where(e => true).Select((o, i) => (o, i)).Select(o => o.Item2.ToString()).Select(o => o).ToArray();
            //static string Reverse(string s)
            //{
            //    var t = s.ToCharArray();
            //    t.Reverse();
            //    return new string(t);
            //}

            //string format = "abcdef";

            //format = Reverse(Reverse(format.Replace(Script.Write<RegExp>("/\\{\\{/g"), static (m) =>
            //{
            //    return new string([1, 1]);
            //})).Replace(Script.Write<RegExp>("/\\}\\}/g"), static (m) =>
            //{
            //    return new string([2, 2]);
            //}));

            //object aa = null!;
            //char[] a = ['a'];
            //var arr = new char[] { 'A', 'B' };
            //new String(arr);
            //Console.WriteLine("Hello World from C#!".ToUpper());
            //var _char = "ABC"[2];
            //var len = "ABC".Length;
        }
    }
}
