using NetJs;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Window;

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
                //int[] inferedGridLenghts;
                //var isGridArray = false;
                var arr = lowerBound != null ? Array.CreateInstance(type, lengths ?? NetJs.Script.CreateArrayFromValues<int>(jsArray!.Length), lowerBound) :
                    lengths != null ? Array.CreateInstance(type, lengths) :
                    //isGridArray ? Array.CreateInstance(type, inferedGridLenghts) :
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
            if (NetJs.Script.InstanceOf<Array>(jsArray))
                return jsArray;
            return CreateArray(typeof(T), jsArray.As<object[]>(), null, null).As<T[]>();
        }

        /// <summary>
        /// Ensure the array is constructed from NetJS array type, not js array
        /// </summary>
        /// <param name="jsArray"></param>
        /// <param name="elementType"></param>
        /// <returns></returns>
        public static Array EnsureIsNetArray(Array jsArray, Type elementType)
        {
            if (NetJs.Script.InstanceOf<Array>(jsArray))
                return jsArray;
            return CreateArray(elementType, jsArray.As<object[]>(), null, null);
        }

        //TODO: remove this method and use the one in IAwaitable.cs
        //Make sure we always return task from a method that has Task return by wrapping the native promise into Task object
        public static TTaskType Async<TTaskType, TResult>(NativeFunction<Task> asyncCode)
        {
            var result = asyncCode();
            if (NetJs.Script.Write<bool>("result instanceof Promise"))
            {
                TaskCompletionSource? vResult = typeof(TResult) == typeof(void) ? new() : null;
                TaskCompletionSource<TResult>? tResult = typeof(TResult) != typeof(void) ? new() : null;
                result.As<IPromise>()
                   .Then((t) =>
                   {
                       vResult?.SetResult();
                       tResult?.SetResult(t.As<TResult>());
                   })
                   .Catch((e) =>
                   {
                       vResult?.SetException(e.As<Exception>());
                       tResult?.SetException(e.As<Exception>());
                   });
                if (typeof(TTaskType) == typeof(ValueTask))
                {
                    return new ValueTask(vResult!.Task).As<TTaskType>();
                }
                else if (typeof(TTaskType).As<RuntimeType>()._prototype.GenericArguments == 1 && typeof(TTaskType).As<RuntimeType>()._prototype.FullName.NativeStartsWith("System.Threading.Tasks.ValueTask<"))
                {
                    return new ValueTask<TResult>(tResult!.Task).As<TTaskType>();
                }
                else
                {
                    return (vResult?.Task ?? tResult!.Task).As<TTaskType>();
                }
            }
            return result.As<TTaskType>();
        }

        public static IPromise TaskToPromise(object taskLike)
        {
            if (NetJs.Script.TypeOf(taskLike).NativeEquals("Promise"))
                return taskLike.As<IPromise>();
            //if (taskLike is Task task)
            //{
            //    return new Promise<object>((resolve, reject) =>
            //    {
            //        task.ContinueWith(t =>
            //        {
            //            if (t.IsCompletedSuccessfully)
            //            {
            //                resolve(t.As<Task<object>>().Result);
            //            }
            //            else
            //            {
            //                reject(t.Exception);
            //            }
            //        });
            //    });
            //}
            var getAwaiterName = "GetAwaiter";
            var isCompletedName = "IsCompleted";
            var getResultName = "GetResult";
            var getAwaiter = taskLike[getAwaiterName].As<NativeFunction<INotifyCompletion>>();
            if (NetJs.Script.IsDefined(getAwaiter))
            {
                return new NetJs.Promise<object>((resolve, reject) =>
                {
                    var awaiter = getAwaiter.InvokeCall(taskLike);
                    awaiter.OnCompleted(() =>
                    {
                        try
                        {
                            var isCompleted = awaiter[isCompletedName].As<bool>();
                            if (isCompleted)
                            {
                                var getResult = awaiter[getResultName].As<NativeFunction<object>>();
                                var result = getResult.InvokeCall(awaiter);
                                resolve(result);
                            }
                            else
                            {
                                reject(new Exception("Awaiter is not completed"));
                            }
                        }
                        catch (Exception ex)
                        {
                            reject(ex);
                        }
                    });
                });
            }
            throw null!;
        }

        public static RefOrPointer<T> CreateObjectReferenceT<T>(NativeFunction<int?, T> getValue, NativeAction<T, int?>? setValue, int? _byteOffset = null, bool createRef = true)
        //where T:allows ref struct
        {
            if (NetJs.Script.IsUndefined(createRef))
            {
                createRef = true;
            }
            RefOrPointer<T> rref = default!;
            rref = createRef ? new Ref<T>((i) =>
            {
                var val = getValue(i);
                rref!._dataSource = val.As<object>();
                rref._dataSourceType = RefOrPointer.DataSourceType._object;
                return val;
            }, setValue ?? ((v, i) =>
            {
            })) : new Pointer<T>((i) =>
            {
                var val = getValue(i);
                rref!._dataSource = val.As<object>();
                rref._dataSourceType = RefOrPointer.DataSourceType._object;
                return val;
            }, setValue ?? ((v, i) =>
            {
            }));
            rref._byteOffset = _byteOffset;
            //It is a common pattern to create a variable on the stack uninitialized and then pass the ref of such(via out or ref) to a method to provide the value
            //By default in js the variable are undefined.
            //If however the ref type is struct, it is possible the method being called try to access the properties of the uninitialized object on stack
            //Make sure the ref variable is initialized to default here
            //TODO: We probably should make the transpiler initialize an uinit variable on stack always to their default
            var varValue = getValue(null);
            if (NetJs.Script.IsUndefined(varValue))
            {
                varValue = default(T);
                if (setValue != null)
                    setValue(varValue!, null); //make sure it is initialized
            }
            rref._dataSource = varValue.As<object>(); //dont box the dataSource
            rref._dataSourceType = RefOrPointer.DataSourceType._object;
            return rref;
        }

        public static RefOrPointer<T> CreateArrayReferenceT<T>(T[] array, int? index = null, bool _checked = false, bool createRef = true)
        {
            RefOrPointer<T>? refs = null;
            bool isArray = Array.Is(array, null);
            var knownType = typeof(T).As<RuntimeType>()._prototype.KnownType;
            if (_checked)
            {
                if (createRef)
                    refs = new Ref<T>((i) =>
                    {
                        if (isArray)
                            return array.As<T[]>()[(index ?? 0) + (i ?? 0)];
                        else
                            return RuntimeHelpers.ReadDataView<T>(array.As<DataView>(), knownType, (index ?? 0) + (i ?? 0));
                    }, (v, i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                            {
                                var lindex = (index ?? 0) + (i ?? 0);
                                array.As<T[]>()[lindex] = v;
                                //if (refs!._dataView != null)
                                //UpdateDataViewFromArray(array, refs.Type, refs._dataView, refs.SizeOfItem * lindex, 1);
                                ////changing array invalidates dataview
                                //refs!._dataView = null;
                            }
                            else
                                RuntimeHelpers.WriteDataView<T>(array.As<DataView>(), knownType, v, (index ?? 0) + (i ?? 0));
                        }
                    });
                else
                    refs = new Pointer<T>((i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                                return array.As<T[]>()[(index ?? 0) + (i ?? 0)];
                            else
                                return RuntimeHelpers.ReadDataView<T>(array.As<DataView>(), knownType, (index ?? 0) + (i ?? 0));
                        }
                    }, (v, i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                            {
                                var lindex = (index ?? 0) + (i ?? 0);
                                array.As<T[]>()[lindex] = v;
                                //if (refs!._dataView != null)
                                //UpdateDataViewFromArray(array, refs.Type, refs._dataView, refs.SizeOfItem * lindex, 1);
                                ////changing array invalidates dataview
                                //refs!._dataView = null;
                            }
                            else
                                RuntimeHelpers.WriteDataView<T>(array.As<DataView>(), knownType, v, (index ?? 0) + (i ?? 0));
                        }
                    });
            }
            else
            {
                if (createRef)
                    refs = new Ref<T>((i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                                return array.As<T[]>()[(index ?? 0) + (i ?? 0)];
                            else
                                return RuntimeHelpers.ReadDataView<T>(array.As<DataView>(), knownType, (index ?? 0) + (i ?? 0));
                        }
                    }, (v, i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                            {
                                var lindex = (index ?? 0) + (i ?? 0);
                                array.As<T[]>()[lindex] = v;
                                //if (refs!._dataView != null)
                                //UpdateDataViewFromArray(array, refs.Type, refs._dataView, refs.SizeOfItem * lindex, 1);
                                ////changing array invalidates dataview
                                //refs!._dataView = null;
                            }
                            else
                                RuntimeHelpers.WriteDataView<T>(array.As<DataView>(), knownType, v, (index ?? 0) + (i ?? 0));
                        }
                    });
                else
                    refs = new Pointer<T>((i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                                return array.As<T[]>()[(index ?? 0) + (i ?? 0)];
                            else
                                return RuntimeHelpers.ReadDataView<T>(array.As<DataView>(), knownType, (index ?? 0) + (i ?? 0));
                        }
                    }, (v, i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                            {
                                var lindex = (index ?? 0) + (i ?? 0);
                                array.As<T[]>()[lindex] = v;
                                //if (refs!._dataView != null)
                                //UpdateDataViewFromArray(array, refs.Type, refs._dataView, refs.SizeOfItem * lindex, 1);
                                ////changing array invalidates dataview
                                //refs!._dataView = null;
                            }
                            else
                                RuntimeHelpers.WriteDataView<T>(array.As<DataView>(), knownType, v, (index ?? 0) + (i ?? 0));
                        }
                    });
            }
            refs._dataSource = array.As<object[]>();
            refs._dataSourceType = RefOrPointer.DataSourceType._array;
            if (index > 0 && typeof(T).As<RuntimeType>()._prototype.KnownType.IsNumeric())
            {
                refs._byteOffset = index.Value * refs.SizeOfItem;
                index = null;
            }
            return refs;
        }

        public static RefOrPointer<T> CreateArrayReference<T>(Union<DataView, Array> array, int? index = null, bool _checked = false, bool createRef = true)
        {
            RefOrPointer<T>? refs = null;
            bool isArray = Array.Is(array, null);
            var knownType = typeof(T).As<RuntimeType>()._prototype.KnownType;
            var arrayType = isArray ? Array.GetArrayElementType(array.As<Array>()) : null;
            if (_checked)
            {
                if (createRef)
                    refs = new Ref<T>((i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                                return array.As<Array>()[(index ?? 0) + (i ?? 0)].As<T>();
                            else
                                return RuntimeHelpers.ReadDataView<T>(array.As<DataView>(), knownType, (index ?? 0) + (i ?? 0));
                        }
                    }, (v, i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                            {
                                var lindex = (index ?? 0) + (i ?? 0);
                                array.As<Array>()[lindex] = v;
                                //if (refs!._dataView != null)
                                //UpdateDataViewFromArray(array.As<Array>(), refs.Type, refs._dataView, refs.SizeOfItem * lindex, 1);
                                ////changing array invalidates dataview
                                //refs!._dataView = null;
                            }
                            else
                                RuntimeHelpers.WriteDataView<T>(array.As<DataView>(), knownType, v, (index ?? 0) + (i ?? 0));
                        }
                    });
                else
                    refs = new Pointer<T>((i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                                return array.As<Array>()[(index ?? 0) + (i ?? 0)].As<T>();
                            else
                                return RuntimeHelpers.ReadDataView<T>(array.As<DataView>(), knownType, (index ?? 0) + (i ?? 0));
                        }
                    }, (v, i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                            {
                                var lindex = (index ?? 0) + (i ?? 0);
                                array.As<Array>()[lindex] = v;
                                //if (refs!._dataView != null)
                                //UpdateDataViewFromArray(array.As<Array>(), refs.Type, refs._dataView, refs.SizeOfItem * lindex, 1);
                                ////changing array invalidates dataview
                                //refs!._dataView = null;
                            }
                            else
                                RuntimeHelpers.WriteDataView<T>(array.As<DataView>(), knownType, v, (index ?? 0) + (i ?? 0));
                        }
                    });
            }
            else
            {
                if (createRef)
                    refs = new Ref<T>((i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                                return array.As<Array>()[(index ?? 0) + (i ?? 0)].As<T>();
                            else
                                return RuntimeHelpers.ReadDataView<T>(array.As<DataView>(), knownType, (index ?? 0) + (i ?? 0));
                        }
                    }, (v, i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                            {
                                var lindex = (index ?? 0) + (i ?? 0);
                                array.As<Array>()[lindex] = v;
                                //if (refs!._dataView != null)
                                //UpdateDataViewFromArray(array.As<Array>(), refs.Type, refs._dataView, refs.SizeOfItem * lindex, 1);
                                ////changing array invalidates dataview
                                //refs!._dataView = null;
                            }
                            else
                                RuntimeHelpers.WriteDataView<T>(array.As<DataView>(), knownType, v, (index ?? 0) + (i ?? 0));
                        }
                    });
                else
                    refs = new Pointer<T>((i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                                return array.As<Array>()[(index ?? 0) + (i ?? 0)].As<T>();
                            else
                                return RuntimeHelpers.ReadDataView<T>(array.As<DataView>(), knownType, (index ?? 0) + (i ?? 0));
                        }
                    }, (v, i) =>
                    {
                        unchecked
                        {
                            if (isArray)
                            {
                                var lindex = (index ?? 0) + (i ?? 0);
                                array.As<Array>()[lindex] = v;
                                //if (refs!._dataView != null)
                                //UpdateDataViewFromArray(array.As<Array>(), refs.Type, refs._dataView, refs.SizeOfItem * lindex, 1);
                                ////changing array invalidates dataview
                                //refs!._dataView = null;
                            }
                            else
                                RuntimeHelpers.WriteDataView<T>(array.As<DataView>(), knownType, v, (index ?? 0) + (i ?? 0));
                        }
                    });
            }
            refs._dataSource = array.As<object[]>();
            refs._dataSourceType = Ref<T>.DataSourceType._array;
            refs._type = arrayType;
            if (index > 0 && arrayType.As<RuntimeType>()._prototype.KnownType.IsNumeric())
            {
                refs._byteOffset = index.Value * refs.SizeOfItem;
                index = null;
            }
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
            return NetJs.Script.RefP(CreateArrayReferenceT(ts))!;
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

        [NetJs.MemberReplace(nameof(GetRawData))]
        internal static ref byte GetRawDataImpl(this object obj)
        {
            if (obj == null)
                return ref Unsafe.NullRef<byte>();
            var type = obj.GetType().As<RuntimeType>();
            if (type._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsEnum) || type._prototype.KnownType.IsNumeric())
            {
                var evalue = NetJs.Script.Unbox(obj);
                var kt = type._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsEnum) ? type._prototype.As<EnumPrototype>().UnderlyingType.KnownType : type._prototype.KnownType;
                switch (kt)
                {
                    case KnownTypeHandle.SystemByte:
                    case KnownTypeHandle.SystemSByte:
                        return ref NetJs.Script.Ref<byte>(CreateObjectReferenceT<byte>((_) => evalue.As<byte>(), (v, i) => { throw null!; }));
                    case KnownTypeHandle.SystemChar:
                    case KnownTypeHandle.SystemInt16:
                    case KnownTypeHandle.SystemUInt16:
                        return ref Unsafe.As<ushort, byte>(ref NetJs.Script.Ref<ushort>(CreateObjectReferenceT<ushort>((_) => evalue.As<ushort>(), (v, i) => { throw null!; })));
                    case KnownTypeHandle.SystemInt32:
                    case KnownTypeHandle.SystemUInt32:
                    case KnownTypeHandle.SystemIntPtr:
                    case KnownTypeHandle.SystemUIntPtr:
                        return ref Unsafe.As<uint, byte>(ref NetJs.Script.Ref<uint>(CreateObjectReferenceT<uint>((_) => evalue.As<uint>(), (v, i) => { throw null!; })));
                    case KnownTypeHandle.SystemInt64:
                    case KnownTypeHandle.SystemUInt64:
                        return ref Unsafe.As<ulong, byte>(ref NetJs.Script.Ref<ulong>(CreateObjectReferenceT<ulong>((_) => evalue.As<ulong>(), (v, i) => { throw null!; })));
                    case KnownTypeHandle.SystemSingle:
                        return ref Unsafe.As<float, byte>(ref NetJs.Script.Ref<float>(CreateObjectReferenceT<float>((_) => evalue.As<float>(), (v, i) => { throw null!; })));
                    case KnownTypeHandle.SystemDouble:
                        return ref Unsafe.As<double, byte>(ref NetJs.Script.Ref<double>(CreateObjectReferenceT<double>((_) => evalue.As<double>(), (v, i) => { throw null!; })));
                }
            }
            ref byte result = ref NetJs.Script.Ref<byte>(CreateObjectReferenceT<object>((_) => obj, (v, i) => { }).As<Ref<byte>>());
            return ref result;
        }


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
            var ttype = type.QCallTypeHandleToRuntimeType();
            var rref = NetJs.Script.Ref<object>(target);
            return NetJs.Script.Box(rref.Value, ttype._prototype);
        }

        [NetJs.MemberReplace(nameof(SizeOf))]
        private static int SizeOfImpl(QCallTypeHandle handle)
        {
            var ttype = handle.QCallTypeHandleToRuntimeType();
            return Marshal.CalculateSizeOf(ttype);
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
            if (o is char ch)
                return ch.GetHashCode();
            if (o is sbyte sb)
                return sb.GetHashCode();
            if (o is byte b)
                return b.GetHashCode();
            if (o is short sh)
                return sh.GetHashCode();
            if (o is ushort us)
                return us.GetHashCode();
            if (o is int i)
                return i.GetHashCode();
            if (o is uint ui)
                return ui.GetHashCode();
            if (o is long l)
                return l.GetHashCode();
            if (o is ulong ul)
                return ul.GetHashCode();
            if (o is float f)
                return f.GetHashCode();
            if (o is double d)
                return d.GetHashCode();
            if (o is string s)
                return s.GetHashCode();
            var value = NetJs.Script.Unbox(o);
            var type = NetJs.Script.TypeOf(value);
            if (type.NativeEquals("number"))
                return value.As<double>().GetHashCode();
            if (type.NativeEquals("bigint"))
                return value.As<long>().GetHashCode();
            if (type.NativeEquals("boolean"))
                return value.As<bool>().GetHashCode();
            if (type.NativeEquals("string"))
                return value.As<string>().GetHashCode();
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

            if (type.NativeEquals("object"))
            {
                //var method = o["GetHashCode"];
                //if (NetJs.Script.IsDefined(method))
                //{
                //    var hashCode = NetJs.Script.Write<int>("method.call(o)");
                //    if (NetJs.Script.TypeOf(hashCode).NativeEquals("number"))
                //        return hashCode.As<int>();
                //}
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
        public static int GetHashCodeImpl(object? o)
        {
            return TryGetHashCode(o);
        }

        [NetJs.Template("{handle}")]
        //[NetJs.Template("{handle}._ptr?.$v")]
        internal static extern RuntimeAssembly QCallAssemblyHandleToRuntimeType(this QCallAssembly handle);
        [NetJs.Template("{handle}")]
        //[NetJs.Template("{handle}._ptr.$v")]
        internal static extern RuntimeModule QCallModuleHandleToRuntimeType(this QCallModule handle);
        [NetJs.Template("{handle}")]
        //[NetJs.Template("{handle}._ptr.$v")]
        internal static extern RuntimeType QCallTypeHandleToRuntimeType(this QCallTypeHandle handle);
        [NetJs.Template("{handle}.$v")]
        //[NetJs.Template("{handle}._ptr")]
        internal static extern ref T GetObjectHandleOnStack<T>(this ObjectHandleOnStack handle);

        [NetJs.MemberReplace(nameof(IsReferenceOrContainsReferences) + "<>")]
        [NetJs.Template("false/*IsReferenceOrContainsReferencesImpl<T>()*/")]
        public static extern bool IsReferenceOrContainsReferencesImpl<T>() where T : allows ref struct;

        [NetJs.MemberReplace(nameof(IsBitwiseEquatable) + "<>")]
        [NetJs.Template("false/*IsBitwiseEquatable<T>()*/")]
        internal static extern bool IsBitwiseEquatableImpl<T>();

        [NetJs.MemberReplace(nameof(ObjectHasComponentSize))]
        internal static bool ObjectHasComponentSizeImpl(object obj)
        {
            var type = NetJs.Script.TypeOf(obj);
            if (type.NativeEquals("string"))
                return true;
            if (Array.Is(obj, null))
                return true;
            return false;
        }

        internal static unsafe object[]? GetParametersFromPointer(IntPtr* args)
        {
            object[]? parameters = null;
            if (args != null)
            {
                var reff = NetJs.Script.Write<RefOrPointer<object>>(nameof(args));
                var paramContainer = reff?._object;
                if (NetJs.Script.IsDefined(paramContainer))
                {
                    if (reff!.Type.As<RuntimeType>()._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsInlineArray))
                    {
                        parameters = NetJs.Script.NewArray<object>();
                        unchecked
                        {
                            for (int i = 0; i < paramContainer!.fieldsAsArray.Length; i++)
                            {
                                var param = paramContainer!.fieldsAsArray[i];
                                if (NetJs.Script.IsUndefined(param))
                                    continue;
                                if (param.Is<RefOrPointer>())
                                {
                                    var refValue = param.As<Ref<object>>();
                                    ref byte b = ref NetJs.Script.Ref<byte>(param.As<Ref<byte>>());
                                    if (Unsafe.IsNullRef(ref b))
                                        param = null;
                                    else
                                    {
                                        if (refValue.Type.As<RuntimeType>()._prototype.KnownType == KnownTypeHandle.SystemByte)
                                        {
                                            param = refValue._dataSource;
                                            if (NetJs.Script.InstanceOf<RefOrPointer>(param))
                                            {
                                                param = param.As<RefOrPointer<object>>()._dataSource;
                                            }
                                        }
                                        else
                                            param = refValue.Value;
                                    }
                                }
                                else if (param.Is(typeof(ByReference)))
                                {
                                    var byreRefValueName = NetJs.Script.Write<string>("\"{nameof(System.ByReference.Value)}\"");
                                    ref byte b = ref NetJs.Script.Ref<byte>(param[byreRefValueName].As<Ref<byte>>());
                                    if (Unsafe.IsNullRef(ref b))
                                        param = null;
                                    else
                                    {
                                        var byRefValue = param[byreRefValueName].As<RefOrPointer<object>>();
                                        if (byRefValue.Type.As<RuntimeType>()._prototype.KnownType == KnownTypeHandle.SystemByte)
                                        {
                                            param = byRefValue._dataSource;
                                            if (NetJs.Script.InstanceOf<RefOrPointer>(param))
                                            {
                                                param = param.As<RefOrPointer<object>>()._dataSource;
                                            }
                                        }
                                        else
                                            param = byRefValue.Value;
                                    }
                                }
                                if (param != null && GetClassPrototypeOf(param).KnownType == KnownTypeHandle.SystemNullable)
                                {
                                    param = param.As<Nullable<int>>()!.Value.As<object>();
                                }
                                else
                                    param = NetJs.Script.Unbox(param);
                                parameters.push(param);
                            }
                        }
                    }
                }
            }
            return parameters;
        }

        internal static object? NativeFunctionDispatch(object? targetOrPrototype, string methodName, TypePrototype? returnType, params object?[]? parameters)
        {
            if (methodName.NativeStartsWith("set_")) //TODO: this wont work for indexer
            {
                methodName = methodName.NativeSubstring(4);
                unchecked
                {
                    object? value = parameters![0];
                    if (targetOrPrototype == null)
                    {
                        Debug.Assert(parameters.Length == 2);
                        targetOrPrototype = parameters![0];
                        value = parameters[1];
                    }
                    targetOrPrototype![methodName] = value;
                }
                return null;
            }
            else if (methodName.NativeStartsWith("get_"))  //TODO: this wont work for indexer
            {
                unchecked
                {
                    methodName = methodName.NativeSubstring(4);
                    if (targetOrPrototype == null)
                    {
                        Debug.Assert(parameters!.Length == 1);
                        targetOrPrototype = parameters![0];
                    }
                    var value = targetOrPrototype![methodName];
                    if (NetJs.Script.TypeOf(value).NativeEquals("object")) //dont attempt to box if value is object
                    {
                        return value;
                    }
                    return NetJs.Script.Box(value, returnType!);
                }
            }
            else
            {
                var method = targetOrPrototype![methodName];
                var value = NetJs.Script.Write<object>("method.apply(targetOrPrototype, parameters)");
                if (NetJs.Script.IsUndefined(value))
                    value = null;
                if (NetJs.Script.TypeOf(value).NativeEquals("object")) //dont attempt to box if value is object
                {
                    return value;
                }
                return NetJs.Script.Box(value, returnType!);
            }
        }

        internal static object? NativeFunctionDispatch(object? targetOrPrototype, MemberModel member, object?[]? parameters)
        {
            var methodName = member.GetOutputName();
            TypePrototype? returns = null;
            uint rt = 0;
            if (member.Flags.TypeHasFlag(MemberFlagsModel.IsMethod) && NetJs.Script.IsDefined(member.As<MethodModel>().ReturnType))
            {
                rt = member.As<MethodModel>().ReturnType.As<uint>();
            }
            else if (member.Flags.TypeHasFlag(MemberFlagsModel.IsProperty) && NetJs.Script.IsDefined(member.As<PropertyModel>().PropertyType))
            {
                rt = member.As<PropertyModel>().PropertyType.As<uint>();
            }
            else if (member.Flags.TypeHasFlag(MemberFlagsModel.IsField) && NetJs.Script.IsDefined(member.As<FieldModel>().FieldType))
            {
                rt = member.As<FieldModel>().FieldType.As<uint>();
            }
            if (rt != 0)
            {
                var tp = AppDomain.GetType(rt);
                returns = tp!._prototype;
            }
            if (member.Flags.TypeHasFlag(MemberFlagsModel.IsMethod) && member.Flags.TypeHasFlag(MemberFlagsModel.IsGeneric)) //a generic method expects the generic arguments to be passed as the first parameters
            {
                if (parameters == null)
                    parameters = NetJs.Script.NewArray<object>();
                var genericArgs = member.As<MethodModel>().GenericArguments.As<TypePrototype[]>();
                genericArgs.forEach((a) => parameters.unshift(a));
            }
            return NativeFunctionDispatch(targetOrPrototype, methodName, returns, parameters);
        }

        public static ArrayBuffer? ArrayBufferFrom(Array arr, KnownTypeHandle? knownType = null)
        {
            if (knownType == null)
            {
                var arrayType = Array.GetArrayElementType(arr).As<RuntimeType>();
                if (arrayType == null)
                    return null;
                knownType = arrayType._prototype.KnownType;
            }

            var nativeArr = arr;
            int len = nativeArr.Length;

            unchecked
            {
                switch (knownType)
                {
                    case KnownTypeHandle.SystemBool:
                    case KnownTypeHandle.SystemByte:
                        {
                            var typed = new Window.Uint8Array(len);
                            for (int i = 0; i < len; i++) { typed[i] = nativeArr[i].As<byte>(); }
                            return typed.buffer;
                        }
                    case KnownTypeHandle.SystemSByte:
                        {
                            var typed = new Window.Int8Array(len);
                            for (int i = 0; i < len; i++) { typed[i] = nativeArr[i].As<sbyte>(); }
                            return typed.buffer;
                        }
                    case KnownTypeHandle.SystemInt16:
                        {
                            var typed = new Window.Int16Array(len);
                            for (int i = 0; i < len; i++) { typed[i] = nativeArr[i].As<short>(); }
                            return typed.buffer;
                        }
                    case KnownTypeHandle.SystemUInt16:
                    case KnownTypeHandle.SystemChar:
                        {
                            var typed = new Window.Uint16Array(len);
                            for (int i = 0; i < len; i++) { typed[i] = nativeArr[i].As<ushort>(); }
                            return typed.buffer;
                        }
                    case KnownTypeHandle.SystemInt32:
                    case KnownTypeHandle.SystemIntPtr:
                        {
                            var typed = new Window.Int32Array(len);
                            for (int i = 0; i < len; i++) { typed[i] = nativeArr[i].As<int>(); }
                            return typed.buffer;
                        }
                    case KnownTypeHandle.SystemUInt32:
                    case KnownTypeHandle.SystemUIntPtr:
                        {
                            var typed = new Window.Uint32Array(len);
                            for (int i = 0; i < len; i++) { typed[i] = nativeArr[i].As<uint>(); }
                            return typed.buffer;
                        }
                    case KnownTypeHandle.SystemInt64:
                        {
                            var typed = new Window.BigInt64Array(len);
                            for (int i = 0; i < len; i++) { typed[i] = nativeArr[i].As<long>(); }
                            return typed.buffer;
                        }
                    case KnownTypeHandle.SystemUInt64:
                        {
                            var typed = new Window.BigUint64Array(len);
                            for (int i = 0; i < len; i++) { typed[i] = nativeArr[i].As<ulong>(); }
                            return typed.buffer;
                        }
                    case KnownTypeHandle.SystemSingle:
                        {
                            var typed = new Window.Float32Array(len);
                            for (int i = 0; i < len; i++) { typed[i] = nativeArr[i].As<float>(); }
                            return typed.buffer;
                        }
                    case KnownTypeHandle.SystemDouble:
                        {
                            var typed = new Window.Float64Array(len);
                            for (int i = 0; i < len; i++) { typed[i] = nativeArr[i].As<double>(); }
                            return typed.buffer;
                        }
                }
            }
            throw new NotSupportedException("Size not supported");
        }

        //public static ArrayBuffer? ArrayBufferFrom(Array arr, KnownTypeHandle? knownType = null)
        //{
        //    if (knownType == null)
        //    {
        //        var arrayType = Array.GetArrayElementType(arr).As<RuntimeType>();
        //        if (arrayType == null)
        //            return null;
        //        knownType = arrayType._prototype.KnownType;
        //    }
        //    switch (knownType)
        //    {
        //        case KnownTypeHandle.SystemBool: return new Window.Uint8Array(arr).buffer;
        //        case KnownTypeHandle.SystemSByte: return new Window.Int8Array(arr).buffer;
        //        case KnownTypeHandle.SystemByte: return new Window.Uint8Array(arr).buffer;
        //        case KnownTypeHandle.SystemInt16: return new Window.Int16Array(arr).buffer;
        //        case KnownTypeHandle.SystemUInt16: case KnownTypeHandle.SystemChar: return new Window.Uint16Array(arr).buffer;
        //        case KnownTypeHandle.SystemInt32: case KnownTypeHandle.SystemIntPtr: return new Window.Int32Array(arr).buffer;
        //        case KnownTypeHandle.SystemUint32: case KnownTypeHandle.SystemUIntPtr: return new Window.Uint32Array(arr).buffer;
        //        case KnownTypeHandle.SystemInt64: return new Window.BigInt64Array(arr).buffer;
        //        case KnownTypeHandle.SystemUint64: return new Window.BigUint64Array(arr).buffer;
        //        case KnownTypeHandle.SystemSingle: return new Window.Float32Array(arr).buffer;
        //        case KnownTypeHandle.SystemDouble: return new Window.Float64Array(arr).buffer;
        //    }
        //    throw new NotSupportedException("Size not supported");
        //}

        public static void WriteDataView(Type type, DataView dataView, KnownTypeHandle writeAsType, object value, int byteStartIndex = 0)
        {
            Debug.Assert(writeAsType.IsNumeric());
            Debug.Assert(type == typeof(object) ||
                type.As<RuntimeType>()._prototype.KnownType == writeAsType ||
                (type.As<RuntimeType>()._prototype.KnownType == KnownTypeHandle.SystemEnum && writeAsType.IsIntegerNumeric()));
            switch (writeAsType)
            {
                case KnownTypeHandle.SystemBool:
                    dataView.setUint8(byteStartIndex, value.As<bool>() ? 1.As<byte>() : 0.As<byte>());
                    break;
                case KnownTypeHandle.SystemByte:
                    dataView.setUint8(byteStartIndex, value.As<byte>());
                    break;
                case KnownTypeHandle.SystemSByte:
                    dataView.setInt8(byteStartIndex, value.As<sbyte>());
                    break;
                case KnownTypeHandle.SystemChar:
                case KnownTypeHandle.SystemUInt16:
                    dataView.setUint16(byteStartIndex, value.As<ushort>(), BitConverter.IsLittleEndian);
                    break;
                case KnownTypeHandle.SystemInt16:
                    dataView.setInt16(byteStartIndex, value.As<short>(), BitConverter.IsLittleEndian);
                    break;
                case KnownTypeHandle.SystemUInt32:
                case KnownTypeHandle.SystemUIntPtr:
                    dataView.setUint32(byteStartIndex, value.As<uint>(), BitConverter.IsLittleEndian);
                    break;
                case KnownTypeHandle.SystemInt32:
                case KnownTypeHandle.SystemIntPtr:
                    dataView.setInt32(byteStartIndex, value.As<int>(), BitConverter.IsLittleEndian);
                    break;
                case KnownTypeHandle.SystemUInt64:
                    dataView.setBigUint64(byteStartIndex, value.As<ulong>(), BitConverter.IsLittleEndian);
                    break;
                case KnownTypeHandle.SystemInt64:
                    dataView.setBigInt64(byteStartIndex, value.As<long>(), BitConverter.IsLittleEndian);
                    break;
                case KnownTypeHandle.SystemSingle:
                    dataView.setFloat32(byteStartIndex, value.As<float>(), BitConverter.IsLittleEndian);
                    break;
                case KnownTypeHandle.SystemDouble:
                    dataView.setFloat64(byteStartIndex, value.As<double>(), BitConverter.IsLittleEndian);
                    break;
                default:
                    throw new NotSupportedException("Size not supported");
            }
        }

        public static void WriteDataView<T>(DataView dataView, KnownTypeHandle writeAsType, T value, int byteStartIndex = 0)
            where T : allows ref struct
        {
            WriteDataView(typeof(T), dataView, writeAsType, value.As<T, object>(), byteStartIndex);
        }

        public static object ReadDataView(Type type, DataView dataView, KnownTypeHandle readAsType, int byteStartIndex = 0)
        {
            Debug.Assert(readAsType.IsNumeric());
            Debug.Assert(type == typeof(object) ||
                type.As<RuntimeType>()._prototype.KnownType == readAsType ||
                (type.As<RuntimeType>()._prototype.KnownType == KnownTypeHandle.SystemEnum && readAsType.IsIntegerNumeric()));
            return readAsType switch
            {
                KnownTypeHandle.SystemBool => (dataView.getInt8(byteStartIndex) != 0 ? true : false).As<object>(),
                KnownTypeHandle.SystemSByte => dataView.getInt8(byteStartIndex).As<object>(),
                KnownTypeHandle.SystemByte => dataView.getUint8(byteStartIndex).As<object>(),
                KnownTypeHandle.SystemInt16 => dataView.getInt16(byteStartIndex, BitConverter.IsLittleEndian).As<object>(),
                KnownTypeHandle.SystemUInt16 or KnownTypeHandle.SystemChar => dataView.getUint16(byteStartIndex, BitConverter.IsLittleEndian).As<object>(),
                KnownTypeHandle.SystemInt32 or KnownTypeHandle.SystemIntPtr => dataView.getInt32(byteStartIndex, BitConverter.IsLittleEndian).As<object>(),
                KnownTypeHandle.SystemUInt32 or KnownTypeHandle.SystemUIntPtr => dataView.getUint32(byteStartIndex, BitConverter.IsLittleEndian).As<object>(),
                KnownTypeHandle.SystemInt64 => dataView.getBigInt64(byteStartIndex, BitConverter.IsLittleEndian).As<object>(),
                KnownTypeHandle.SystemUInt64 => dataView.getBigUint64(byteStartIndex, BitConverter.IsLittleEndian).As<object>(),
                KnownTypeHandle.SystemSingle => dataView.getFloat32(byteStartIndex, BitConverter.IsLittleEndian).As<object>(),
                KnownTypeHandle.SystemDouble => dataView.getFloat64(byteStartIndex, BitConverter.IsLittleEndian).As<object>(),
                _ => throw new NotSupportedException("Size not supported")
            };
        }

        public static T ReadDataView<T>(DataView dataView, KnownTypeHandle readAsType, int byteStartIndex = 0)
            where T : allows ref struct
        {
            return ReadDataView(typeof(T), dataView, readAsType, byteStartIndex).As<T>();
        }

        static DataView? numericCastDataView;
        public static DataView GetReUsableDataView()
        {
            if (numericCastDataView == null)
            {
                var arrayBuffer = new ArrayBuffer(8);
                numericCastDataView = new DataView(arrayBuffer);
            }
            return numericCastDataView;
        }
        public static TTo BitCast<TFrom, TTo>(TFrom value)
            where TFrom : allows ref struct
            where TTo : allows ref struct
        {
            var from = typeof(TFrom).As<RuntimeType>()._prototype;
            var to = typeof(TTo).As<RuntimeType>()._prototype;
            if (from.KnownType == KnownTypeHandle.SystemEnum)
            {
                from = from.As<EnumPrototype>().UnderlyingType;
            }
            if (to.KnownType == KnownTypeHandle.SystemEnum)
            {
                to = to.As<EnumPrototype>().UnderlyingType;
            }
            if (from == to)
                return value.As<TFrom, TTo>();

            if (to.KnownType.IsNumeric() && from.KnownType.IsNumeric())
            {
                var dv = GetReUsableDataView();
                WriteDataView<TFrom>(dv, from.KnownType, value);
                return ReadDataView<TTo>(dv, to.KnownType);
            }

            throw null!;
        }

        public static void UpdateArrayFromDataView(Array _array, Type itemType, DataView _dataView, int byteStartIndex = 0, int? itemCount = null)
        {
            //copy it, this dataView could be invalidated, while modifying the underlying array
            var dataView = _dataView;
            var knownType = itemType.As<RuntimeType>()._prototype.KnownType;
            if (knownType == KnownTypeHandle.SystemEnum)
            {
                knownType = itemType.As<RuntimeType>()._prototype.As<EnumPrototype>().UnderlyingType.KnownType;
            }
            var sizeOfItem = Marshal.SizeOf(itemType);
            Array originalArray = _array!;
            //byte should start in the right place. eg for 2 sized item [0,1] => 0, [2,3]=>2
            byteStartIndex = (byteStartIndex / sizeOfItem) * sizeOfItem;
            var arrayStartIndex = byteStartIndex / sizeOfItem;
            int maxItems = itemCount ?? (originalArray.Length - arrayStartIndex);
            //if (maxItems <= 0) return;
            do
            {
                var value = ReadDataView<object>(dataView, knownType, byteStartIndex);
                unchecked
                {
                    originalArray[arrayStartIndex] = value;
                }
                byteStartIndex += sizeOfItem;
                if (byteStartIndex >= dataView.byteLength)
                    break;
                arrayStartIndex++;
            } while (--maxItems > 0);
        }

        public static void UpdateDataViewFromArray(Array _array, Type itemType, DataView _dataView, int byteStartIndex = 0, int? itemCount = null)
        {
            //copy it, this dataView could be invalidated, while modifying the underlying array
            var dataView = _dataView;
            var knownType = itemType.As<RuntimeType>()._prototype.KnownType;
            if (knownType == KnownTypeHandle.SystemEnum)
            {
                knownType = itemType.As<RuntimeType>()._prototype.As<EnumPrototype>().UnderlyingType.KnownType;
            }
            var sizeOfItem = Marshal.SizeOf(itemType);
            Array originalArray = _array!;
            //byte should start in the right place. eg for 2 sized item [0,1] => 0, [2,3]=>2
            byteStartIndex = (byteStartIndex / sizeOfItem) * sizeOfItem;
            var arrayStartIndex = byteStartIndex / sizeOfItem;
            int maxItems = itemCount ?? originalArray.Length;
            if (maxItems <= 0) return;
            do
            {
                unchecked
                {
                    var value = originalArray[arrayStartIndex];
                    WriteDataView<object>(dataView, knownType, value!, byteStartIndex);
                    byteStartIndex += sizeOfItem;
                    if (byteStartIndex >= dataView.byteLength)
                        break;
                    arrayStartIndex++;
                    if (arrayStartIndex >= originalArray.Length)
                        break;
                }
            } while (--maxItems > 0);
        }

        //public static byte[] StructToByteArray(object _object)
        //{
        //    if (_object.IsPureStruct)
        //    {
        //        return Array.from(new Uint8Array(_object.fieldsAsDataView.buffer)).As<byte[]>();
        //    }
        //    var jsArray = _object.fieldsAsArray;
        //    var fields = _object.GetType().As<RuntimeType>()._prototype.Metadata!.Fields!;
        //    //all fields are byte type, no need to rearrange
        //    if (fields.every(f => f.FieldType.As<int>() == (int)KnownTypeHandle.SystemByte || f.FieldType.As<int>() == (int)KnownTypeHandle.SystemSByte))
        //    {
        //        return jsArray.As<byte[]>();
        //    }
        //    byte[] bytes = [];
        //    RecursiveStructToByteArray(_object, bytes);
        //    return bytes;
        //}

        //static void RecursiveStructToByteArray(object _object, byte[] bytes)
        //{
        //    var jsArray = _object.fieldsAsArray;
        //    var fields = _object.GetType().As<RuntimeType>()._prototype.Metadata!.Fields!;
        //    int currentByteOffset = 0;
        //    for (int i = 0; i < fields.Length; i++)
        //    {
        //        var fieldType = AppDomain.GetType(fields[i].FieldType.As<uint>()).As<RuntimeType?>() ?? throw null!;
        //        var fieldOffset = fields[i].Offset;
        //        var fieldSize = fieldType._prototype.Size;
        //        var value = jsArray[fieldOffset ?? currentByteOffset];
        //        switch (fieldType._prototype.KnownType)
        //        {
        //            case KnownTypeHandle.SystemByte:
        //            case KnownTypeHandle.SystemSByte:
        //                Debug.Assert(fieldSize == 1);
        //                bytes.push(value);
        //                break;
        //            case KnownTypeHandle.SystemChar:
        //            case KnownTypeHandle.SystemInt16:
        //            case KnownTypeHandle.SystemUInt16:
        //                Debug.Assert(fieldSize == 2);
        //                bytes.push(value.As<short>() & 0xFF);
        //                bytes.push((value.As<short>() >> 8) & 0xFF);
        //                break;
        //            case KnownTypeHandle.SystemInt32:
        //            case KnownTypeHandle.SystemUInt32:
        //            case KnownTypeHandle.SystemIntPtr:
        //            case KnownTypeHandle.SystemUIntPtr:
        //                Debug.Assert(fieldSize == 4);
        //                bytes.push(value.As<uint>() & 0xFF);
        //                bytes.push((value.As<uint>() >> 8) & 0xFF);
        //                bytes.push((value.As<uint>() >> 16) & 0xFF);
        //                bytes.push((value.As<uint>() >> 24) & 0xFF);
        //                break;
        //            case KnownTypeHandle.SystemInt64:
        //            case KnownTypeHandle.SystemUInt64:
        //                Debug.Assert(fieldSize == 8);
        //                bytes.push((int)(value.As<ulong>() & 0xFF));
        //                bytes.push((int)((value.As<ulong>() >> 8) & 0xFF));
        //                bytes.push((int)((value.As<ulong>() >> 16) & 0xFF));
        //                bytes.push((int)((value.As<ulong>() >> 24) & 0xFF));
        //                bytes.push((int)((value.As<ulong>() >> 32) & 0xFF));
        //                bytes.push((int)((value.As<ulong>() >> 40) & 0xFF));
        //                bytes.push((int)((value.As<ulong>() >> 48) & 0xFF));
        //                bytes.push((int)((value.As<ulong>() >> 56) & 0xFF));
        //                break;
        //            default:
        //                if (fieldType._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsValueType))
        //                {
        //                    RecursiveStructToByteArray(value, bytes);
        //                }
        //                else //reference type, simply safe the js reference itself and skip 4(being our pointer size)
        //                {
        //                    bytes.push(value);
        //                    bytes.push(value);
        //                    bytes.push(value);
        //                    bytes.push(value);
        //                }
        //                break;
        //        }
        //        if (NetJs.Script.IsDefined(fieldOffset))
        //            currentByteOffset = fieldOffset.As<int>();
        //        else
        //            currentByteOffset += fieldSize;
        //    }
        //}

        [IgnoreGeneric]
        public static void AddSpreadToCollection<T>(NativeAction<T> addMethod, IEnumerable<T> spreadItems)
        {
            var enumerator = spreadItems.GetEnumerator();
            //var addMethod = collection[addMethodName].As<NativeAction<T>>();
            while (enumerator.MoveNext())
            {
                T current = enumerator.Current;
                addMethod(current);
            }
        }
    }
}
