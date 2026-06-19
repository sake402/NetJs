using NetJs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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
            if (NetJs.Script.InstanceOf(jsArray, typeof(Array)))
                return jsArray;
            return CreateArray(typeof(T), jsArray.As<object[]>(), null, null).As<T[]>();
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
            var getAwaiter = taskLike["GetAwaiter"].As<NativeFunction<INotifyCompletion>>();
            if (NetJs.Script.IsDefined(getAwaiter))
            {
                return new Promise<object>((resolve, reject) =>
                {
                    var awaiter = getAwaiter.Invoke(taskLike);
                    awaiter.OnCompleted(() =>
                    {
                        try
                        {
                            var isCompleted = awaiter["IsCompleted"].As<bool>();
                            if (isCompleted)
                            {
                                var getResult = awaiter["GetResult"].As<NativeFunction<object>>();
                                var result = getResult.Invoke(awaiter);
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

        public static Ref<T> CreateObjectReference<T>(NativeFunction<T> getValue, NativeAction<T>? setValue, int? _byteOffset = null)
        {
            Ref<T> rref = default!;
            rref = new Ref<T>((i) => rref!._object = getValue(), (v, i) =>
            {
                if (setValue != null)
                    setValue(v);
            });
            rref._byteOffset = _byteOffset??0;
            //It is a common pattern to create a variable on the stack uninitialized and then pass the ref of such(via out or ref) to a method to provide the value
            //By default in js the variable are undefined.
            //If however the ref type is struct, it is possible the method being called try to access the properties of the uninitialized object on stack
            //Make sure the ref variable is initialized to default here
            //TODO: We probably should make the transpiler initialize an uinit variable on stack always to their default
            var varValue = getValue();
            if (NetJs.Script.IsUndefined(varValue))
            {
                varValue = default(T);
                if (setValue != null)
                    setValue(varValue!); //make sure it is initialized
            }
            rref._object = varValue;
            return rref;
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

        [NetJs.MemberReplace(nameof(GetRawData))]
        internal static ref byte GetRawDataImpl(this object obj)
        {
            ref byte result = ref NetJs.Script.Ref<byte>(CreateObjectReference<byte>(() => obj.As<byte>(), (v) => { }));
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
            if (Array.Is(obj))
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
                        parameters = [];
                        unchecked
                        {
                            for (int i = 0; i < paramContainer!.fieldsAsArray.Length; i++)
                            {
                                var param = paramContainer!.fieldsAsArray[i];
                                if (param.Is(typeof(ByReference)))
                                {
                                    param = param[nameof(ByReference.Value)]![NetJs.Constants.RefValueName];
                                }
                                parameters.Push(param);
                            }
                        }
                    }
                }
            }
            return parameters;
        }

        internal static object NativeFunctionDispatch(object targetOrPrototype, string methodName, params object?[]? parameters)
        {
            var method = targetOrPrototype[methodName];
            return NetJs.Script.Write<object>("method.apply(targetOrPrototype, parameters)");
        }

        internal static object NativeFunctionDispatch(object targetOrPrototype, MemberModel member, params object?[]? parameters)
        {
            var methodName = NetJs.Script.IsDefined(member.OutputName) ? member.OutputName!.NativeReplace("@", member.Name) : member.Name;
            return NativeFunctionDispatch(targetOrPrototype, methodName, parameters);
        }

        public static byte[] StructToByteArray(object _object)
        {
            if (_object.IsPureStruct)
            {
                return Array.from(new Uint8Array(_object.fieldsAsDataView.buffer)).As<byte[]>();
            }
            var jsArray = _object.fieldsAsArray;
            var fields = _object.GetType().As<RuntimeType>()._prototype.Metadata!.Fields!;
            //all fields are byte type, no need to rearrange
            if (fields.Every(f => f.FieldType.As<int>() == (int)KnownTypeHandle.SystemByte || f.FieldType.As<int>() == (int)KnownTypeHandle.SystemSByte))
            {
                return jsArray.As<byte[]>();
            }
            byte[] bytes = [];
            RecursiveStructToByteArray(_object, bytes);
            return bytes;
        }

        static void RecursiveStructToByteArray(object _object, byte[] bytes)
        {
            var jsArray = _object.fieldsAsArray;
            var fields = _object.GetType().As<RuntimeType>()._prototype.Metadata!.Fields!;
            int currentByteOffset = 0;
            for (int i = 0; i < fields.Length; i++)
            {
                var fieldType = AppDomain.GetType(fields[i].FieldType.As<uint>()).As<RuntimeType?>() ?? throw null!;
                var fieldOffset = fields[i].Offset;
                var fieldSize = fieldType._prototype.Size;
                var value = jsArray[fieldOffset ?? currentByteOffset];
                switch (fieldType._prototype.KnownType)
                {
                    case KnownTypeHandle.SystemByte:
                    case KnownTypeHandle.SystemSByte:
                        Debug.Assert(fieldSize == 1);
                        bytes.Push(value);
                        break;
                    case KnownTypeHandle.SystemChar:
                    case KnownTypeHandle.SystemInt16:
                    case KnownTypeHandle.SystemUInt16:
                        Debug.Assert(fieldSize == 2);
                        bytes.Push(value.As<short>() & 0xFF);
                        bytes.Push((value.As<short>() >> 8) & 0xFF);
                        break;
                    case KnownTypeHandle.SystemInt32:
                    case KnownTypeHandle.SystemUint32:
                    case KnownTypeHandle.SystemIntPtr:
                    case KnownTypeHandle.SystemUintPtr:
                        Debug.Assert(fieldSize == 4);
                        bytes.Push(value.As<uint>() & 0xFF);
                        bytes.Push((value.As<uint>() >> 8) & 0xFF);
                        bytes.Push((value.As<uint>() >> 16) & 0xFF);
                        bytes.Push((value.As<uint>() >> 24) & 0xFF);
                        break;
                    case KnownTypeHandle.SystemInt64:
                    case KnownTypeHandle.SystemUint64:
                        Debug.Assert(fieldSize == 8);
                        bytes.Push((int)(value.As<ulong>() & 0xFF));
                        bytes.Push((int)((value.As<ulong>() >> 8) & 0xFF));
                        bytes.Push((int)((value.As<ulong>() >> 16) & 0xFF));
                        bytes.Push((int)((value.As<ulong>() >> 24) & 0xFF));
                        bytes.Push((int)((value.As<ulong>() >> 32) & 0xFF));
                        bytes.Push((int)((value.As<ulong>() >> 40) & 0xFF));
                        bytes.Push((int)((value.As<ulong>() >> 48) & 0xFF));
                        bytes.Push((int)((value.As<ulong>() >> 56) & 0xFF));
                        break;
                    default:
                        if (fieldType._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsValueType))
                        {
                            RecursiveStructToByteArray(value, bytes);
                        }
                        else //reference type, simply safe the js reference itself and skip 4(being our pointer size)
                        {
                            bytes.Push(value);
                            bytes.Push(value);
                            bytes.Push(value);
                            bytes.Push(value);
                        }
                        break;
                }
                if (NetJs.Script.IsDefined(fieldOffset))
                    currentByteOffset = fieldOffset.As<int>();
                else
                    currentByteOffset += fieldSize;
            }
        }
    }
}
