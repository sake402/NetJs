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

        public static Array AsNetArray(this Array arr, Type elementType)
        {
            return RuntimeHelpers.EnsureIsNetArray(arr, elementType);
        }

        //public static Array AsNetArray(this Array arr)
        //{
        //    return RuntimeHelpers.EnsureIsNetArray(arr);
        //}

        [NetJs.Template("{array}.includes({item})")]
        public static extern bool contains<T>(this T[] array, T item);

        [NetJs.Template("{array}.every({callback})")]
        public static extern bool every<T>(this T[] array, [NativeDelegate] Func<T, int, T[], bool> callback);

        [NetJs.Template("{array}.every({callback})")]
        public static extern bool every<T>(this T[] array, [NativeDelegate] Func<T, bool> callback);

        [NetJs.Template("{array}.filter({callback})")]
        public static extern T[] filter<T>(this T[] array, [NativeDelegate] Func<T, int, T[], bool> callback);

        [NetJs.Template("{array}.filter({callback})")]
        public static extern T[] filter<T>(this T[] array, [NativeDelegate] Func<T, bool> callback);
        [NetJs.Template("[...new Set({array})]")]
        public static extern T[] unique<T>(this T[] array);

        [NetJs.Template("{array}.map({callback})")]
        public static extern TResult[] map<TSource, TResult>(this TSource[] array, [NativeDelegate] Func<TSource, int, TSource[], TResult> callback);

        [NetJs.Template("{array}.map({callback})")]
        public static extern TResult[] map<TSource, TResult>(this TSource[] array, [NativeDelegate] Func<TSource, TResult> callback);

        [NetJs.Template("{array}.some({callback})")]
        public static extern bool some<T>(this T[] array, [NativeDelegate] Func<T, int, T[], bool> callback);

        [NetJs.Template("{array}.some({callback})")]
        public static extern bool some<T>(this T[] array, [NativeDelegate] Func<T, bool> callback);

        [NetJs.Template("{source}.push({value})")]
        public static extern void push(this Array source, [Box(false)] object? value);
        [NetJs.Template("{source}.push({value})")]
        public static extern void push<T>(this T[] source, T value);
        [NetJs.Template("{source}.push( ...{values})")]
        public static extern void push<T>(this T[] source, params T[] values);

        [NetJs.Template("{array}.sort()")]
        public static extern void sort<T>(this T[] array);

        [NetJs.Template("{array}.sort({compareCallback})")]
        public static extern void sort<T>(this T[] array, [NativeDelegate] Func<T, T, int> compareCallback);

        [NetJs.Template("{array}.forEach({callback})")]
        public static extern void forEach<T>(this T[] array, [NativeDelegate] Action<T, int, T[]> callback);

        [NetJs.Template("{array}.forEach({callback})")]
        public static extern void forEach<T>(this T[] array, [NativeDelegate] Action<T> callback);

        [NetJs.Template("{array}.lastIndexOf({searchString}, {fromIndex})")]
        public static extern int lastIndexOf<T>(this T[] array, string searchString, int fromIndex);

        [NetJs.Template("{array}.join()")]
        public static extern string join<T>(this T[] array);

        [NetJs.Template("{array}.join({separator})")]
        public static extern string join<T>(this T[] array, string separator);

        [NetJs.Template("{array}.pop()")]
        public static extern T pop<T>(this T[] array);

        [NetJs.Template("{array}.reverse()")]
        public static extern void reverse<T>(this T[] array);

        [NetJs.Template("{array}.shift()")]
        public static extern object shift<T>(this T[] array);
        [NetJs.Template("{array}.indexOf({value})")]
        public static extern int indexOf<T>(this T[] array, T value);

        [NetJs.Template("{array}.slice()")]
        public static extern Array clone(this Array array);
        [NetJs.Template("{array}.slice({start})")]
        public static extern T[] slice<T>(this T[] array, int start);

        [NetJs.Template("{array}.slice({start}, {end})")]
        public static extern T[] slice<T>(this T[] array, int start, int end);

        [NetJs.Template("{array}.splice({start}, {deleteCount})")]
        public static extern T[] splice<T>(this T[] array, int start, int deleteCount);
        [NetJs.Template("{array}.splice({start}, {deleteCount}, {newItems})")]
        public static extern T[] splice<T>(this T[] array, int start, int deleteCount, [Spread] params T[] newItems);

        [NetJs.Template("{array}.unshift({items})")]
        public static extern void unshift<T>(this T[] array, [Spread] params T[] items);

        [NetJs.Template("{array1}.concat({array2})")]
        public static extern Array concat(this Array array1, Array array2);
        [NetJs.Template("{array1}.concat({array2})")]
        public static extern T[] concat<T>(this T[] array1, T[] array2);
        [NetJs.Template("{array}.indexOf({item})")]
        public static extern int indexOf(this Array array, [Box(false)] object item);

        public static T[] EnumerableToArray<T>(this IEnumerable<T> enumerable)
        {
            var arr = new T[0];
            foreach (var e in enumerable)
                arr.push(e);
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
                return arr.filter(filter)[0];
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
            var arr2 = arr.filter(filter);
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
            arr = arr.filter(filter);
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
            var arr2 = arr.filter(filter);
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
