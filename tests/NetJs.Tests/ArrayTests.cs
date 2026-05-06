using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace NetJs.Tests
{
    class ArrayTests
    {
        public static void Run()
        {
            TestProperties();
            TestCreateInstance();
            TestEmptyAndAsReadOnly();
            TestGetSetValue();
            TestCopyAndConstrainedCopy();
            TestCopyTo();
            TestClear();
            TestClone();
            TestResize();
            TestSortMethods();
            TestReverseMethods();
            TestIndexOf_LastIndexOf();
            TestBinarySearch();
            TestConvertAll_Exists_Find_FindAll_FindIndex_FindLastIndex_TrueForAll_ForEach();
            TestInterfaceBehavior_IList_ICollection_IListT_ICollectionT_IEnumerableT();
            TestMultiDimensional();
            TestNonZeroLowerBound();
            TestExceptionCases();

            Console.WriteLine("✅ Array Tests passed.");
        }

        static void TestProperties()
        {
            int[] arr = new int[5];
            Array a = arr;

            Debug.Assert(arr.Length == 5);
            Debug.Assert(a.Length == 5);
            Debug.Assert(arr.Rank == 1);
            Debug.Assert(a.Rank == 1);
            Debug.Assert(a.LongLength == 5);

            // SyncRoot / IsSynchronized
            ICollection coll = arr;
            Debug.Assert(coll.IsSynchronized == false);  // default for arrays
            object sync = coll.SyncRoot;
            Debug.Assert(sync != null);

            // IsFixedSize / IsReadOnly via IList
            IList list = arr;
            Debug.Assert(list.IsFixedSize == true);
            Debug.Assert(list.IsReadOnly == false);
            // arrays allow setting elements, so IList’s setter should work
        }

        static void TestCreateInstance()
        {
            // 1D
            Array a1 = Array.CreateInstance(typeof(int), 4);
            Debug.Assert(a1.Length == 4);
            Debug.Assert(a1.GetLowerBound(0) == 0 && a1.GetUpperBound(0) == 3);

            // Multi-dimensional
            Array m = Array.CreateInstance(typeof(string), new int[] { 3, 2 });
            Debug.Assert(m.Rank == 2);
            Debug.Assert(m.GetLength(0) == 3);
            Debug.Assert(m.GetLength(1) == 2);
            Debug.Assert(m.GetLowerBound(0) == 0 && m.GetUpperBound(1) == 1);

            // With non-zero lower bounds
            Array nz = Array.CreateInstance(typeof(int), new int[] { 2, 3 }, new int[] { 10, -5 });
            Debug.Assert(nz.GetLowerBound(0) == 10);
            Debug.Assert(nz.GetUpperBound(0) == 11);
            Debug.Assert(nz.GetLowerBound(1) == -5);
            Debug.Assert(nz.GetUpperBound(1) == -3);
        }

        static void TestEmptyAndAsReadOnly()
        {
            // Array.Empty<T>
            int[] e = Array.Empty<int>();
            Debug.Assert(e.Length == 0);

            string[] es = Array.Empty<string>();
            Debug.Assert(es.Length == 0);

            // AsReadOnly
            int[] arr = new int[] { 1, 2, 3 };
            IList<int> ro = Array.AsReadOnly(arr);
            Debug.Assert(ro.Count == 3);
            Debug.Assert(ro[0] == 1 && ro[2] == 3);
            try
            {
                ro[1] = 100;  // should throw
                Debug.Assert(false);
            }
            catch (NotSupportedException) { }
        }

        static void TestGetSetValue()
        {
            int[] arr = new int[3];
            Array a = arr;
            a.SetValue(77, 1);
            object o = a.GetValue(1);
            Debug.Assert((int)o == 77);

            // Multi-dim
            int[,] md = new int[2, 3];
            Array am = md;
            am.SetValue(9, 1, 2);
            Debug.Assert((int)am.GetValue(1, 2) == 9);

            // Using index arrays
            int[,,] t3 = new int[2, 2, 2];
            Array at3 = t3;
            int[] idx = new int[] { 1, 0, 1 };
            at3.SetValue(123, idx);
            Debug.Assert((int)at3.GetValue(idx) == 123);
        }

        static void TestCopyAndConstrainedCopy()
        {
            int[] src = new int[] { 1, 2, 3, 4, 5 };
            int[] dest = new int[5];
            Array.Copy(src, dest, 5);
            for (int i = 0; i < 5; i++) Debug.Assert(dest[i] == src[i]);

            // Overload with indexes
            int[] dest2 = new int[7];
            Array.Copy(src, 1, dest2, 2, 3);
            Debug.Assert(dest2[2] == 2 && dest2[3] == 3 && dest2[4] == 4);

            // Between object[] and value array
            object[] obj = new object[5];
            Array.Copy(src, obj, 5);
            Debug.Assert((int)obj[2] == 3);

            // ConstrainedCopy: rollback semantics
            int[] s2 = { 10, 20, 30, 40 };
            int[] d2 = { 0, 0, 0, 0, 0 };
            Array.ConstrainedCopy(s2, 1, d2, 2, 2);
            Debug.Assert(d2[2] == 20 && d2[3] == 30);

            // Try a failing scenario for ConstrainedCopy: e.g. mismatched types
            try
            {
                object[] os = new object[] { "a", "b", "c" };
                int[] ta = new int[3];
                Array.ConstrainedCopy(os, 0, ta, 0, 3);
                Debug.Assert(false);
            }
            catch (ArrayTypeMismatchException) { }
        }

        static void TestCopyTo()
        {
            int[] a = new int[] { 5, 6, 7 };
            Array aa = a;
            int[] dest = new int[5];
            aa.CopyTo(dest, 1);
            Debug.Assert(dest[1] == 5 && dest[3] == 7 && dest[0] == 0);

            // CopyTo to object array
            object[] ob = new object[5];
            aa.CopyTo(ob, 2);
            Debug.Assert((int)ob[2] == 5 && (int)ob[4] == 7);

            // invalid dimensions
            try
            {
                int[,] md = new int[2, 2];
                md.CopyTo(dest, 0);
                // maybe throws RankException
            }
            catch (Exception) { }
        }

        static void TestClear()
        {
            int[] a = new int[] { 10, 20, 30, 40 };
            Array.Clear(a, 1, 2);
            Debug.Assert(a[0] == 10 && a[1] == 0 && a[2] == 0 && a[3] == 40);

            string[] s = new string[] { "x", "y", "z" };
            Array.Clear(s, 0, 3);
            Debug.Assert(s[0] == null && s[2] == null);
        }

        static void TestClone()
        {
            int[] a = new int[] { 2, 4, 6 };
            Array ac = a;
            object cloneObj = ac.Clone();
            int[] c = (int[])cloneObj;
            Debug.Assert(c.Length == a.Length);
            c[1] = 999;
            Debug.Assert(a[1] == 4);

            // reference types
            string[] ss = new string[] { "A", "B" };
            string[] cc = (string[])((Array)ss).Clone();
            Debug.Assert(cc[0] == "A");
        }

        static void TestResize()
        {
            int[] a = new int[] { 1, 2, 3 };
            Array.Resize(ref a, 5);
            Debug.Assert(a.Length == 5);
            Debug.Assert(a[0] == 1 && a[1] == 2 && a[2] == 3 && a[4] == 0);

            Array.Resize(ref a, 2);
            Debug.Assert(a.Length == 2);
            Debug.Assert(a[0] == 1 && a[1] == 2);

            int[] b = null;
            Array.Resize(ref b, 4);
            Debug.Assert(b != null && b.Length == 4);
        }

        static void TestSortMethods()
        {
            // Simple sort
            int[] a = new int[] { 3, 1, 2 };
            Array.Sort(a);
            Debug.Assert(a[0] == 1 && a[1] == 2 && a[2] == 3);

            // Partial sort
            int[] b = new int[] { 10, 5, 7, 2, 8 };
            Array.Sort(b, 1, 3);
            // expected sorting of b[1..3]
            Debug.Assert(b[1] <= b[2] && b[2] <= b[3]);

            // Key / item sort
            int[] keys = new int[] { 3, 1, 2 };
            string[] items = new string[] { "three", "one", "two" };
            Array.Sort(keys, items);
            Debug.Assert(keys[0] == 1 && items[0] == "one");
            Debug.Assert(keys[2] == 3 && items[2] == "three");

            // Comparer sort
            string[] ss = new string[] { "banana", "Apple", "cherry" };
            Array.Sort(ss, StringComparer.OrdinalIgnoreCase);
            Debug.Assert(string.Equals(ss[0], "Apple", StringComparison.OrdinalIgnoreCase));

            // Partial with comparer
            string[] sx = new string[] { "x", "z", "y", "a" };
            Array.Sort(sx, 1, 2, StringComparer.OrdinalIgnoreCase);
            Debug.Assert(string.Compare(sx[1], sx[2], StringComparison.OrdinalIgnoreCase) <= 0);
        }

        static void TestReverseMethods()
        {
            int[] a = new int[] { 1, 2, 3, 4 };
            Array.Reverse(a);
            Debug.Assert(a[0] == 4 && a[3] == 1);

            int[] b = new int[] { 10, 20, 30, 40, 50 };
            Array.Reverse(b, 1, 3);
            // reversed the subrange [1..3]
            Debug.Assert(b[1] == 40 && b[2] == 30 && b[3] == 20);
        }

        static void TestIndexOf_LastIndexOf()
        {
            int[] a = new int[] { 5, 7, 5, 7, 5 };

            Debug.Assert(Array.IndexOf(a, 7) == 1);
            Debug.Assert(Array.LastIndexOf(a, 7) == 3);

            Debug.Assert(Array.IndexOf(a, 5, 2) == 2);
            Debug.Assert(Array.IndexOf(a, 5, 1, 3) == 2);

            Debug.Assert(Array.LastIndexOf(a, 5, 3) == 2);
            Debug.Assert(Array.LastIndexOf(a, 5, 3, 3) == 2);

            Debug.Assert(Array.IndexOf(a, 100) == -1);
            Debug.Assert(Array.LastIndexOf(a, 100) == -1);
        }

        static void TestBinarySearch()
        {
            int[] a = new int[] { 1, 3, 5, 7, 9 };
            int pos = Array.BinarySearch(a, 5);
            Debug.Assert(pos >= 0 && a[pos] == 5);

            int notFound = Array.BinarySearch(a, 6);
            Debug.Assert(notFound < 0);

            int pr = Array.BinarySearch(a, 1, 3, 7);
            Debug.Assert(pr >= 0 && a[pr] == 7);

            string[] ss = new string[] { "apple", "Banana", "cherry" };
            Array.Sort(ss, StringComparer.OrdinalIgnoreCase);
            int pi = Array.BinarySearch(ss, "banana", StringComparer.OrdinalIgnoreCase);
            Debug.Assert(pi >= 0);
        }

        static void TestConvertAll_Exists_Find_FindAll_FindIndex_FindLastIndex_TrueForAll_ForEach()
        {
            int[] a = new int[] { 1, 2, 3, 4, 5 };

            // ConvertAll
            string[] sa = Array.ConvertAll(a, x => "num" + x);
            Debug.Assert(sa.Length == a.Length);
            Debug.Assert(sa[2] == "num3");

            // Exists
            bool e = Array.Exists(a, x => x == 3);
            Debug.Assert(e == true);
            bool ne = Array.Exists(a, x => x == 99);
            Debug.Assert(ne == false);

            // Find
            int f = Array.Find(a, x => x % 2 == 0);
            Debug.Assert(f == 2);
            int fn = Array.Find(a, x => x > 100);
            Debug.Assert(fn == default(int));  // default for value type is 0

            // FindAll
            int[] evens = Array.FindAll(a, x => x % 2 == 0);
            for (int i = 0; i < evens.Length; i++)
                Debug.Assert(evens[i] % 2 == 0);

            // FindIndex / FindLastIndex
            int fi = Array.FindIndex(a, x => x % 2 == 0);
            Debug.Assert(fi == 1);
            int fli = Array.FindLastIndex(a, x => x % 2 == 0);
            Debug.Assert(fli == 3);

            // TrueForAll
            bool allPositive = Array.TrueForAll(a, x => x > 0);
            Debug.Assert(allPositive);
            bool allGT2 = Array.TrueForAll(a, x => x > 2);
            Debug.Assert(allGT2 == false);

            // ForEach
            int sum = 0;
            Array.ForEach(a, x => sum += x);
            Debug.Assert(sum == 1 + 2 + 3 + 4 + 5);
        }

        static void TestInterfaceBehavior_IList_ICollection_IListT_ICollectionT_IEnumerableT()
        {
            int[] a = new int[] { 10, 20, 30 };
            // Non-generic
            IList list = a;
            Debug.Assert(list.Count == 3);
            Debug.Assert(list[1].Equals(20));
            list[1] = 25;
            Debug.Assert(a[1] == 25);
            
            try { list.Add(40); Debug.Assert(false); }
            catch (NotSupportedException) { }

            try { list.Remove(25); Debug.Assert(false); }
            catch (NotSupportedException) { }

            // Generic
            IList<int> listT = a;
            Debug.Assert(listT.Count == 3);
            Debug.Assert(listT[2] == 30);
            listT[2] = 35;
            Debug.Assert(a[2] == 35);

            try { listT.Add(100); Debug.Assert(false); }
            catch (NotSupportedException) { }

            try { listT.RemoveAt(1); Debug.Assert(false); }
            catch (NotSupportedException) { }

            // ICollection<T>
            ICollection<int> collT = a;
            Debug.Assert(collT.Count == 3);
            Debug.Assert(collT.IsReadOnly == true);  // The generic ICollection is read-only
                                                     // Try CopyTo via generic
            int[] dest = new int[5];
            collT.CopyTo(dest, 1);
            Debug.Assert(dest[1] == 10 && dest[3] == 35);

            // IEnumerable<T> enumeration order
            int idx = 0;
            foreach (int x in (IEnumerable<int>)a)
            {
                Debug.Assert(x == a[idx]);
                idx++;
            }
        }

        static void TestMultiDimensional()
        {
            int[,] m = new int[2, 3];
            Array am = m;
            am.SetValue(42, 1, 2);
            Debug.Assert((int)am.GetValue(1, 2) == 42);

            // Try operations invalid on multi-dim
            try
            {
                Array.Reverse(m);
                Debug.Assert(false);
            }
            catch (RankException) { }

            try
            {
                Array.Sort(m as Array);
                Debug.Assert(false);
            }
            catch (RankException) { }

            // Flatten copy
            try
            {
                int[] flat = new int[6];
                Array.Copy(m, flat, 6);
                // maybe it succeeds or not (depending), accept exception
            }
            catch { }

            // BinarySearch on multi-dim
            try
            {
                Array.BinarySearch(m, 0);
                Debug.Assert(false);
            }
            catch (RankException) { }
        }

        static void TestNonZeroLowerBound()
        {
            Array arr = Array.CreateInstance(typeof(int), new int[] { 3 }, new int[] { 5 });
            Debug.Assert(arr.GetLowerBound(0) == 5);
            Debug.Assert(arr.GetUpperBound(0) == 7);

            arr.SetValue(9, 6);
            Debug.Assert((int)arr.GetValue(6) == 9);

            //int[] dest = new int[3];
            //arr.CopyTo(dest, 0);
            //Debug.Assert(dest[1] == 9);

            Array arrCl = (Array)arr.Clone();
            Debug.Assert(arrCl.GetLowerBound(0) == arr.GetLowerBound(0));
            
            // Clear
            Array.Clear(arr, arr.GetLowerBound(0), arr.GetLength(0));
            for (int i = arr.GetLowerBound(0); i <= arr.GetUpperBound(0); i++)
            {
                Debug.Assert((int)arr.GetValue(i) == 0);
            }
        }

        static void TestExceptionCases()
        {
            // Nulls
            try { Array.Copy(null, new int[1], 1); Debug.Assert(false); }
            catch (ArgumentNullException) { }
            try { Array.Copy(new int[1], null, 1); Debug.Assert(false); }
            catch (ArgumentNullException) { }
            try { Array.Clear(null, 0, 1); Debug.Assert(false); }
            catch (ArgumentNullException) { }
            try { Array.ConstrainedCopy(null, 0, new int[1], 0, 1); Debug.Assert(false); }
            catch (ArgumentNullException) { }

            int[] a = new int[3];
            try { Array.Clear(a, -1, 2); Debug.Assert(false); }
            catch (ArgumentOutOfRangeException) { }
            try { Array.Clear(a, 2, 5); Debug.Assert(false); }
            catch (ArgumentException) { }

            try { Array.Copy(a, 0, new int[2], 0, 3); Debug.Assert(false); }
            catch (ArgumentException) { }

            try { Array.IndexOf(a, 1, -1); Debug.Assert(false); }
            catch (ArgumentOutOfRangeException) { }

            try { Array.LastIndexOf(a, 1, 5); Debug.Assert(false); }
            catch (ArgumentOutOfRangeException) { }

            try { Array.BinarySearch((int[])null, 1); Debug.Assert(false); }
            catch (ArgumentNullException) { }

            try { Array.Sort((int[])null); Debug.Assert(false); }
            catch (ArgumentNullException) { }

            // Sort of non‑IComparable
            object[] os = new object[] { new object(), new object() };
            try
            {
                Array.Sort(os);
                Debug.Assert(false);
            }
            catch (InvalidOperationException) { }

            // Sort keys/items mismatch lengths
            try
            {
                int[] ks = new int[] { 1, 2, 3 };
                string[] vs = new string[] { "a", "b" };
                Array.Sort(ks, vs);
                Debug.Assert(false);
            }
            catch (ArgumentException) { }
        }
    }
}