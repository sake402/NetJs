using NetJs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeHelpers
    {
        public static Array CreateArray(Type type, object[]? jsArray, int[]? lengths = null, int[]? lowerBound = null)
        {
            if (lengths == null && jsArray == null)
                throw new InvalidOperationException("One of lenght or initializers required");
            unchecked
            {
                if (lengths != null && jsArray != null)
                {
                    for (int i = 0; i < lengths.Length; i++)
                    {
                        if (lengths[i] == -1) //ommited size
                        {
                            lengths[i] = lengths.Length == 1 ? jsArray.Length : jsArray[i].As<Array>().Length;
                        }
                    }
                }
                var arr = lowerBound != null ? Array.CreateInstance(type, lengths ?? NetJs.Script.CreateArrayFromValues<int>(jsArray!.Length), lowerBound) :
                    lengths != null ? Array.CreateInstance(type, lengths) :
                    Array.CreateInstance(type, jsArray!.Length);
                if (jsArray != null)
                {
                    for (int i = 0; i < jsArray.Length; i++)
                    {
                        arr[i] = jsArray[i];
                    }
                }
                return arr;
            }
        }

        public static T[] CreateArrayT<T>(T[]? jsArray, int[]? lengths = null, int[]? lowerBound = null)
        {
            return CreateArray(typeof(T), jsArray.As<object[]>(), lengths, lowerBound).As<T[]>();
        }

        /// <summary>
        /// Ensure the array is constructed from NetJS array type, not js array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsArray"></param>
        /// <returns></returns>
        public static T[] EnsureIsNetArray<T>(T[] jsArray)
        {
            if (NetJs.Script.InstanceOf(jsArray, typeof(Array)))
                return jsArray;
            return CreateArray(typeof(T), jsArray.As<object[]>(), null, null).As<T[]>();
        }

        public static IPromise TaskToPromise(Task task)
        {
            if (NetJs.Script.TypeOf(task).NativeEquals("Promise"))
                return task.As<IPromise>();
            return new Promise<object>((resolve, reject) =>
            {
                task.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        resolve(t.As<Task<object>>().Result);
                    }
                    else
                    {
                        reject(t.Exception);
                    }
                });
            });
        }

        public static Ref<T> CreateObjectReference<T>([NativeDelegate] Func<T> getValue, [NativeDelegate] Action<T>? setValue)
        {
            //int? a = 1;
            //var s = Global.IfNotNull(a, aa=>aa.ToString());
            //return new RefOrPointer<T?>((i) => getValue(), (v, i) => Global.IfNotNull(setValue, t => t.Invoke(v)));
            return new Ref<T>((i) => getValue(), (v, i) =>
            {
                if (setValue != null)
                    setValue(v);
            });
        }

        public static Ref<T?> CreateArrayReference<T>(T[] array, int? index = null, bool _checked = false)
        {
            Ref<T?> refs;
            if (_checked)
            {
                refs = new Ref<T?>((i) =>
               {
                   return array[(index ?? 0) + (i ?? 0)];
               }, (v, i) =>
               {
                   array[(index ?? 0) + (i ?? 0)] = v;
               });
            }
            else
            {
                refs = new Ref<T?>((i) =>
                {
                    unchecked
                    {
                        return array[(index ?? 0) + (i ?? 0)];
                    }
                }, (v, i) =>
                {
                    unchecked
                    {
                        array[(index ?? 0) + (i ?? 0)] = v;
                    }
                });
            }
            refs._array = array;
            return refs;
        }

        public static Ref<object?> CreateArrayReference(Array array, int? index = null, bool _checked = false)
        {
            Ref<object?> refs;
            if (_checked)
            {
                refs = new Ref<object?>((i) =>
                {
                    return array[(index ?? 0) + (i ?? 0)];
                }, (v, i) =>
                {
                    array[(index ?? 0) + (i ?? 0)] = v;
                });
            }
            else
            {
                refs = new Ref<object?>((i) =>
                {
                    unchecked
                    {
                        return array[(index ?? 0) + (i ?? 0)];
                    }
                }, (v, i) =>
                {
                    unchecked
                    {
                        array[(index ?? 0) + (i ?? 0)] = v;
                    }
                });
            }
            refs._array = array.As<object[]>();
            return refs;
        }

        public static Span<T> StackAllocSpan<T>(int? length = null, T[]? initialValues = null)
        {
            var ts = CreateArrayT<T>(initialValues, length != null ? NetJs.Script.CreateArrayFromValues(length.Value) : null);
            return ts;
        }

        public static ReadOnlySpan<T> StackAllocReadOnlySpan<T>(int? length = null, T[]? initialValues = null)
        {
            var ts = CreateArrayT<T>(initialValues, length != null ? NetJs.Script.CreateArrayFromValues(length.Value) : null);
            return ts;
        }


        public static unsafe T* StackAllocPointer<T>(int? length = null, T[]? initialValues = null)
        {
            var ts = CreateArrayT<T>(initialValues, length != null ? NetJs.Script.CreateArrayFromValues(length.Value) : null);
            return NetJs.Script.RefP(CreateArrayReference(ts, null))!;
        }

        static int StringToHashCode(string str)
        {
            int hash = 0;
            if (str.Length == 0)
            {
                return hash;
            }
            for (int i = 0; i < str.Length; i++)
            {
                char ch = str.NativeCharCodeAt(i);
                hash = ((hash << 5) - hash) + ch; // Simple bitwise operation
                hash = hash & hash; // Convert to 32bit integer
            }
            return hash;
        }

        //[NetJs.MemberReplace(nameof(InternalGetHashCode))]
        //private static int InternalGetHashCodeImpl(object? o)
        //{
        //    return NetJs.Script.Write<int>("$.$getHashCode(o)");
        //}

        [NetJs.MemberReplace(nameof(GetObjectValue))]
        public static object? GetObjectValueImpl(object? obj)
        {
            return obj;
        }

        [NetJs.MemberReplace(nameof(PrepareMethod))]
        private static unsafe void PrepareMethodImpl(IntPtr method, IntPtr* instantiations, int ninst)
        {

        }

        [NetJs.MemberReplace(nameof(GetUninitializedObjectInternal))]
        private static object GetUninitializedObjectInternalImpl(IntPtr type)
        {
            throw new NotImplementedException();
        }

        [NetJs.MemberReplace(nameof(InitializeArray))]
        private static void InitializeArrayImpl(Array array, IntPtr fldHandle)
        {

        }


        [NetJs.MemberReplace(nameof(GetSpanDataFrom))]
        private static unsafe ref byte GetSpanDataFromImpl(
            IntPtr fldHandle,
            IntPtr targetTypeHandle,
            IntPtr count)
        {
            throw new NotImplementedException();
        }

        [NetJs.MemberReplace(nameof(RunClassConstructor))]
        private static void RunClassConstructorImpl(IntPtr type)
        {

        }

        [NetJs.MemberReplace(nameof(RunModuleConstructor))]
        private static void RunModuleConstructorImpl(IntPtr module)
        {

        }

        [NetJs.MemberReplace(nameof(SufficientExecutionStack))]
        private static bool SufficientExecutionStackImpl()
        {
            return true;
        }

        [NetJs.MemberReplace(nameof(InternalBox))]
        private static object InternalBoxImpl(QCallTypeHandle type, ref byte target)
        {
            throw new NotImplementedException();
        }

        [NetJs.MemberReplace(nameof(SizeOf))]
        private static int SizeOfImpl(QCallTypeHandle handle)
        {
            throw new NotImplementedException();
        }

        [NetJs.IgnoreGeneric]
        public class Lazy<T>
        {
            bool hasValue;
            T value = default!;
            Func<T> get;

            public Lazy(Func<T> get)
            {
                this.get = get;
            }

            [NetJs.Name(NetJs.Constants.LazyVariableValueName)]
            public T Value
            {
                get
                {
                    if (hasValue)
                        return value;
                    value = get();
                    hasValue = true;
                    return value;
                }
            }
        }

        [NetJs.IgnoreGeneric]
        public static Lazy<T> LazyValue<T>(Func<T> getT)
        {
            return new Lazy<T>(getT);
        }

        static int StringHashCode(string str)
        {
            int hash = 0;
            if (str.Length == 0)
            {
                return hash;
            }
            for (int i = 0; i < str.Length; i++)
            {
                var c = str[i];
                hash = ((hash << 5) - hash) + c; // Simple bitwise operation
                hash = hash & hash; // Convert to 32bit integer
            }
            return hash;
        }

        [NetJs.MemberReplace(nameof(TryGetHashCode))]
        internal static int TryGetHashCodeImpl(object? o)
        {
            if (o == null)
                return 0;
            if (NetJs.Script.TypeOf(o).NativeEquals("number"))
            {
                return Math.Truncate(o.As<double>()).As<int>();
            }
            if (NetJs.Script.TypeOf(o).NativeEquals("boolean"))
                return o.As<bool>() ? 1 : 0;
            if (NetJs.Script.TypeOf(o).NativeEquals("string"))
                return StringToHashCode(o.As<string>());
            //if (a.ToString)
            //{
            //    var str = a.ToString();
            //    return stringHashCode(str);
            //}
            //if (a.$type && a.$type.ToString) {
            //    var str = a.$type.ToString();
            //    return stringHashCode(str);
            //}
            //const jsonString = JSON.stringify(a);
            //return stringHashCode(jsonString);

            if (NetJs.Script.TypeOf(o).NativeEquals("object"))
            {
                var existinghashCode = o[NetJs.Constants.HashCodeKey].As<int>();
                if (NetJs.Script.IsUndefinedOrNull(existinghashCode))
                {
                    existinghashCode = Random.Shared.Next();
                    o[NetJs.Constants.HashCodeKey] = existinghashCode.As<object>();
                }
                return existinghashCode.As<int>();
            }
            return 0;
        }

        [NetJs.MemberReplace(nameof(GetHashCode))]
        internal static int GetHashCodeImpl(object? o)
        {
            return TryGetHashCode(o);
        }

        [NetJs.Template("{handle}._ptr.$v")]
        internal static extern RuntimeAssembly QCallAssemblyHandleToRuntimeType(this QCallAssembly handle);
        [NetJs.Template("{handle}._ptr.$v")]
        internal static extern RuntimeModule QCallModuleHandleToRuntimeType(this QCallModule handle);
        [NetJs.Template("{handle}._ptr.$v")]
        internal static extern RuntimeType QCallTypeHandleToRuntimeType(this QCallTypeHandle handle);
        [NetJs.Template("{handle}._ptr")]
        internal static extern ref T GetObjectHandleOnStack<T>(this ObjectHandleOnStack handle);

        [NetJs.MemberReplace(nameof(IsReferenceOrContainsReferences) + "<>")]
        [NetJs.Template("false/*IsReferenceOrContainsReferencesImpl<T>()*/")]
        public static extern bool IsReferenceOrContainsReferencesImpl<T>() where T : allows ref struct;

        [NetJs.MemberReplace(nameof(IsBitwiseEquatable) + "<>")]
        [NetJs.Template("false/*IsBitwiseEquatable<T>()*/")]
        internal static extern bool IsBitwiseEquatableImpl<T>();

        [NetJs.MemberReplace(nameof(ObjectHasComponentSize) + "<>")]
        [NetJs.Template("false/*ObjectHasComponentSize<T>()*/")]
        internal static extern bool ObjectHasComponentSizeImpl(object obj);
    }
}
