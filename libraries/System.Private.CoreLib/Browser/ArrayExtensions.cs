using NetJs;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System
{
    //[dotnetJs.Convention(Member = dotnetJs.ConventionMember.Field | dotnetJs.ConventionMember.Method, Notation = dotnetJs.Notation.CamelCase)]
    //[dotnetJs.External]
    public static class ArrayExtensions
    {
        public static T[] AsNetArray<T>(this T[] arr)
        {
            return RuntimeHelpers.EnsureIsNetArray(arr);
        }

        //public static Array AsNetArray(this Array arr)
        //{
        //    return RuntimeHelpers.EnsureIsNetArray(arr);
        //}

        [NetJs.Template("{array}.includes({item})")]
        public static extern bool ArrayContains<T>(this T[] array, T item);

        [NetJs.Template("{array}.every({callback})")]
        public static extern bool Every<T>(this T[] array, [NativeDelegate] Func<T, int, T[], bool> callback);

        [NetJs.Template("{array}.every({callback})")]
        public static extern bool Every<T>(this T[] array, [NativeDelegate] Func<T, bool> callback);

        [NetJs.Template("{array}.filter({callback})")]
        public static extern T[] Filter<T>(this T[] array, [NativeDelegate] Func<T, int, T[], bool> callback);

        [NetJs.Template("{array}.filter({callback})")]
        public static extern T[] Filter<T>(this T[] array, [NativeDelegate] Func<T, bool> callback);
        [NetJs.Template("[...new Set({array})]")]
        public static extern T[] Unique<T>(this T[] array);

        [NetJs.Template("{array}.map({callback})")]
        public static extern TResult[] Map<TSource, TResult>(this TSource[] array, [NativeDelegate] Func<TSource, int, TSource[], TResult> callback);

        [NetJs.Template("{array}.map({callback})")]
        public static extern TResult[] Map<TSource, TResult>(this TSource[] array, [NativeDelegate] Func<TSource, TResult> callback);

        [NetJs.Template("{array}.some({callback})")]
        public static extern bool Some<T>(this T[] array, [NativeDelegate] Func<T, int, T[], bool> callback);

        [NetJs.Template("{array}.some({callback})")]
        public static extern bool Some<T>(this T[] array, [NativeDelegate] Func<T, bool> callback);

        [NetJs.Template("{source}.push({value})")]
        public static extern void Push(this Array source, [Box(false)] object? value);
        [NetJs.Template("{source}.push({value})")]
        public static extern void Push<T>(this T[] source, T value);
        [NetJs.Template("{source}.push( ...{values})")]
        public static extern void Push<T>(this T[] source, params T[] values);

        [NetJs.Template("{array}.sort()")]
        public static extern void Sort<T>(this T[] array);

        [NetJs.Template("{array}.sort({compareCallback})")]
        public static extern void Sort<T>(this T[] array, [NativeDelegate] Func<T, T, int> compareCallback);

        [NetJs.Template("{array}.forEach({callback})")]
        public static extern void ForEach<T>(this T[] array, [NativeDelegate] Action<T, int, T[]> callback);

        [NetJs.Template("{array}.forEach({callback})")]
        public static extern void ForEach<T>(this T[] array, [NativeDelegate] Action<T> callback);

        [NetJs.Template("{array}.lastIndexOf({searchString}, {fromIndex})")]
        public static extern int LastIndexOf<T>(this T[] array, string searchString, int fromIndex);

        [NetJs.Template("{array}.join()")]
        public static extern string Join<T>(this T[] array);

        [NetJs.Template("{array}.join({separator})")]
        public static extern string Join<T>(this T[] array, string separator);

        [NetJs.Template("{array}.pop()")]
        public static extern T Pop<T>(this T[] array);

        [NetJs.Template("{array}.reverse()")]
        public static extern void Reverse<T>(this T[] array);

        [NetJs.Template("{array}.shift()")]
        public static extern object Shift<T>(this T[] array);

        [NetJs.Template("{array}.slice()")]
        public static extern Array ArrayClone(this Array array);
        [NetJs.Template("{array}.slice({start})")]
        public static extern T[] ArraySlice<T>(this T[] array, int start);

        [NetJs.Template("{array}.slice({start}, {end})")]
        public static extern T[] ArraySlice<T>(this T[] array, int start, int end);

        [NetJs.Template("{array}.splice({start}, {deleteCount})")]
        public static extern T[] Splice<T>(this T[] array, int start, int deleteCount);
        [NetJs.Template("{array}.splice({start}, {deleteCount}, ...{newItems})")]
        public static extern T[] Splice<T>(this T[] array, int start, int deleteCount, params T[] newItems);

        [NetJs.Template("{array}.splice({items})")]
        public static extern void Unshift<T>(this T[] array, params T[] items);

        [NetJs.Template("{array1}.concat({array2})")]
        public static extern Array ArrayConcat(this Array array1, Array array2);
        [NetJs.Template("{array}.indexOf({item})")]
        public static extern int NativeIndexOf(this Array array, [Box(false)] object item);

        public static T[] EnumerableToArray<T>(this IEnumerable<T> enumerable)
        {
            var arr = new T[0];
            foreach (var e in enumerable)
                arr.Push(e);
            return arr;
        }

        public static T ArraySingle<T>(this T[] arr)
        {
            if (arr.Length == 1)
            {
                unchecked
                {
                    return arr[0];
                }
            }
            if (arr.Length > 1)
                throw new ArrayTypeMismatchException();
            throw new InvalidOperationException();
        }

        public static T? ArraySingleOrDefault<T>(this T[] arr)
        {
            if (arr.Length == 1)
            {
                unchecked
                {
                    return arr[0];
                }
            }
            if (arr.Length > 1)
                throw new ArrayTypeMismatchException();
            return default(T);
        }

        public static T ArrayFirst<T>(this T[] arr)
        {
            if (arr.Length == 0)
                throw new InvalidOperationException();
            unchecked
            {
                return arr[0];
            }
        }

        public static T ArrayFirst<T>(this T[] arr, [NativeDelegate] Func<T, bool> filter)
        {
            if (arr.Length == 0)
                throw new InvalidOperationException();
            unchecked
            {
                return arr.Filter(filter)[0];
            }
        }

        public static T? ArrayFirstOrDefault<T>(this T[] arr)
        {
            if (arr.Length == 0)
                return default(T);
            return arr[0];
        }

        public static T? ArrayFirstOrDefault<T>(this T[] arr, [NativeDelegate] Func<T, bool> filter)
        {
            if (arr.Length == 0)
                return default(T);
            var arr2 = arr.Filter(filter);
            if (arr2.Length == 0)
                return default(T);
            unchecked
            {
                return arr2[0];
            }
        }

        public static T ArrayLast<T>(this T[] arr)
        {
            if (arr.Length == 0)
                throw new InvalidOperationException();
            unchecked
            {
                return arr[arr.Length - 1];
            }
        }

        public static T ArrayLast<T>(this T[] arr, [NativeDelegate] Func<T, bool> filter)
        {
            if (arr.Length == 0)
                throw new InvalidOperationException();
            arr = arr.Filter(filter);
            unchecked
            {
                return arr[arr.Length - 1];
            }
        }

        public static T? ArrayLastOrDefault<T>(this T[] arr)
        {
            if (arr.Length == 0)
                return default(T);
            unchecked
            {
                return arr[arr.Length - 1];
            }
        }

        public static T? ArrayLastOrDefault<T>(this T[] arr, [NativeDelegate] Func<T, bool> filter)
        {
            if (arr.Length == 0)
                return default(T);
            var arr2 = arr.Filter(filter);
            if (arr2.Length == 0)
                return default(T);
            unchecked
            {
                return arr2[arr2.Length - 1];
            }
        }

        public static bool ArrayAny<T>(this T[] arr)
        {
            return arr.Length > 0;
        }

    }
}
