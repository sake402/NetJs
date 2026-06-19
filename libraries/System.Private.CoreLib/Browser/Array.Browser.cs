using NetJs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Window;

namespace System
{
    [NetJs.External]
    [NetJs.Name("window.Array")]
    public class NativeArray
    {
        //public extern NativeArray();
        //public extern NativeArray(int length);
    }
    //[NetJs.StaticCallConvention]
    //[NetJs.ExternalInterfaceImplementation(typeof(ArrayEnumerator))]
    public abstract partial class Array : NativeArray
    {
        //public Array(int length) : base(length)
        //{

        //}
        [NetJs.InlineConst]
        public const string InterfaceImplementationName = "$implements";
        [NetJs.InlineConst]
        public const string ElementTypeName = "$elementType";
        [NetJs.InlineConst]
        public const string SizesName = "$sizes";
        [NetJs.InlineConst]
        public const string LowerBoundsName = "$lb";

        public static extern Array from(Uint8Array arr);
        //Type ElementType
        //{
        //    get => this[ElementTypeName].As<TypePrototype>()?.Type;
        //}

        [NetJs.MemberReplace(nameof(Length))]
        //[NetJs.StaticCallConvention(false)]
        public extern int LengthImpl
        {
            [NetJs.Template("{this}.length")]
            get;
        }

        [NetJs.MemberReplace(nameof(NativeLength))]
        //[NetJs.StaticCallConvention(false)]
        [CLSCompliant(false)]
        public extern nuint NativeLengthImpl
        {
            [NetJs.Template("{this}.length")]
            get;
        }

        [NetJs.MemberReplace(nameof(LongLength))]
        //[NetJs.StaticCallConvention(false)]
        public extern int LongLengthImpl
        {
            [NetJs.Template("BigInt({this}.length)")]
            get;
        }

        [NetJs.MemberReplace(nameof(Rank))]
        [NetJs.StaticCallConvention]
        public int RankImpl
        {
            //[dotnetJs.Template("{assembly.}System.Array." + nameof(_GetRank) + "({this})")]
            //get;
            get
            {
                var sz = this[SizesName].As<int[]>();
                if (NetJs.Script.IsDefined(sz))
                    return sz.Length;
                return 1;
            }
        }

        [NetJs.MemberReplace(nameof(Clone))]
        public object CloneImpl()
        {
            var clone = CreateFinal(this[ElementTypeName].As<RuntimeType>(), this[SizesName].As<int[]>(), this[LowerBoundsName].As<int[]>(), this.As<object[]>(), 0);
            //if (NetJs.Script.IsDefined(this[ElementTypeName]))
            //    clone[ElementTypeName] = this[ElementTypeName];
            //if (NetJs.Script.IsDefined(this[SizesName]))
            //    clone[SizesName] = this[SizesName];
            //if (NetJs.Script.IsDefined(this[LowerBoundsName]))
            //    clone[LowerBoundsName] = this[LowerBoundsName];
            return clone;
        }

        //Most usage of array is one dimension, this doesnt make an array of the parameter, faster to just pass the parameter instead of creating an array for it
        [NetJs.StaticCallConvention]
        [NetJs.Name("$Read1")]
        internal object? Read1(int index)
        {
            var rank = Rank;
            if (rank != 1)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);
            if (NetJs.Script.HasValue(this[LowerBoundsName]))
            {
                //Use slow inter implementation if has lower bounds
                return InternalGetValue(GetFlattenedIndex([index]));
            }
            if (index < 0 || index >= Length)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
            unchecked
            {
                var elementType = this[ElementTypeName].As<RuntimeType>();
                var value = this[index];
                var boxValue = NetJs.Script.IsDefined(elementType) ? NetJs.Script.Write<object>("{global.}$box(value, elementType._prototype)") : value;
                return boxValue;
            }
        }

        [NetJs.StaticCallConvention]
        [NetJs.Name("$Write1")]
        protected void Write1(object? value, int index)
        {
            var rank = Rank;
            if (rank != 1)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);
            if (NetJs.Script.HasValue(this[LowerBoundsName]))
            {
                //Use slow internal implementation if has lower bounds
                InternalSetValue(value, GetFlattenedIndex([index]));
                return;
            }
            if (index < 0 || index >= Length)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
            unchecked
            {
                var elementType = this[ElementTypeName].As<RuntimeType>();
                var unBoxValue = NetJs.Script.IsDefined(elementType) ? NetJs.Script.Write<object>("{global.}$cast(value, elementType._prototype)") : value;
                this[index] = unBoxValue;
            }
        }

        [NetJs.StaticCallConvention]
        [NetJs.Name("$Read")]
        protected object? Read(params int[] indices)
        {
            if (indices == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
            var rank = Rank;
            if (rank != indices.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);
            if (rank == 1)
            {
                unchecked
                {
                    var index = indices[0];
                    var elementType = this[ElementTypeName].As<RuntimeType>();
                    var value = this[index];
                    var boxValue = NetJs.Script.IsDefined(elementType) ? NetJs.Script.Write<object>("{global.}$box(value, elementType._prototype)") : value;
                    return boxValue;
                }
            }
            return InternalGetValue(GetFlattenedIndex(indices));
        }

        [NetJs.StaticCallConvention]
        [NetJs.Name("$Write")]
        protected void Write(object? value, params int[] indices)
        {
            if (indices == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
            var rank = Rank;
            if (rank != indices.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);
            if (rank == 1)
            {
                unchecked
                {
                    var index = indices[0];
                    var elementType = this[ElementTypeName].As<RuntimeType>();
                    var unBoxValue = NetJs.Script.IsDefined(elementType) ? NetJs.Script.Write<object>("{global.}$cast(value, elementType._prototype)") : value;
                    this[index] = unBoxValue;
                }
            }
            else
                InternalSetValue(value, GetFlattenedIndex(indices));
        }

        [NetJs.StaticCallConvention]
        [NetJs.Name("$ReadT1")]
        protected object? ReadT1(int index)
        {
            var rank = Rank;
            if (rank != 1)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);
            if (NetJs.Script.HasValue(this[LowerBoundsName]))
            {
                //Use slow internal implementation if has lower bounds, but without boxing 
                unchecked
                {
                    return this[GetFlattenedIndex([index]).As<int>()];
                }
            }
            if (index < 0 || index >= Length)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
            unchecked
            {
                return this[index];
            }
        }

        [NetJs.StaticCallConvention]
        [NetJs.Name("$WriteT1")]
        protected void WriteT1(object? value, int index)
        {
            var rank = Rank;
            if (rank != 1)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);
            if (NetJs.Script.HasValue(this[LowerBoundsName]))
            {
                //Use slow internal implementation if has lower bounds, but without boxing 
                unchecked
                {
                    this[GetFlattenedIndex([index]).As<int>()] = value;
                }
                return;
            }
            if (index < 0 || index >= Length)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
            unchecked
            {
                this[index] = value;
            }
        }

        [NetJs.StaticCallConvention]
        [NetJs.Name("$ReadT")]
        internal object ReadT(params int[] indices)
        {
            if (indices == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
            var rank = Rank;
            if (rank != indices.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);
            if (rank == 1)
            {
                unchecked
                {
                    var index = indices[0];
                    var value = this[index];
                    return value!;
                }
            }
            unchecked
            {
                return this.As<object[]>()[GetFlattenedIndex(indices)];
            }
        }

        [NetJs.StaticCallConvention]
        [NetJs.Name("$WriteT")]
        internal void WriteT(object value, params int[] indices)
        {
            if (indices == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
            var rank = Rank;
            if (rank != indices.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);
            if (rank == 1)
            {
                unchecked
                {
                    var index = indices[0];
                    this[index] = value;
                }
            }
            else
            {
                unchecked
                {
                    this.As<object[]>()[GetFlattenedIndex(indices)] = value;
                }
            }
        }

        [NetJs.Unbox(false)]
        public extern object? this[int index]
        {
            [NetJs.Template("{assembly.}System.Array.$" + nameof(Read1) + ".call({this}, {index})")]
            [NetJs.Template("{this}[{index}]", "unchecked")]
            get;
            [NetJs.Template("{assembly.}System.Array.$" + nameof(Write1) + ".call({this}, {value}, {index})")]
            [NetJs.Template("{this}[{index}] = {value}", "unchecked")]
            set;
        }

        //public extern object this[Range range]
        //{
        //    [dotnetJs.External]
        //    [dotnetJs.Template("{assembly.}System.Array." + nameof(_Range) + "({this}, {range})")]
        //    get;
        //}

        //public extern object this[Index index]
        //{
        //    [dotnetJs.External]
        //    [dotnetJs.Template("{assembly.}System.Array." + nameof(_Index) + "({this}, {index})")]
        //    get;
        //}        

        [NetJs.Unbox(false)]
        public extern object? this[int index1, int index2]
        {
            [NetJs.Template("{assembly.}System.Array.$" + nameof(Read) + ".call({this}, [{index1}, {index2}])")]
            get;
            [NetJs.Template("{assembly.}System.Array.$" + nameof(Write) + ".call({this}, {value}, [{index1}, {index2}])")]
            set;
        }

        [NetJs.Unbox(false)]
        public extern object? this[int index1, int index2, int index3]
        {
            [NetJs.Template("{assembly.}System.Array.$" + nameof(Read) + ".call({this}, [{index1}, {index2}, {index3}])")]
            get;
            [NetJs.Template("{assembly.}System.Array.$" + nameof(Write) + ".call({this}, {value}, [{index1}, {index2}, {index3}])")]
            set;
        }

        [NetJs.Unbox(false)]
        public extern object? this[int index1, int index2, int index3, int index4]
        {
            [NetJs.Template("{assembly.}System.Array.$" + nameof(Read) + ".call({this}, [{index1}, {index2}, {index3}, {index4}])")]
            get;
            [NetJs.Template("{assembly.}System.Array.$" + nameof(Write) + ".call({this}, {value}, [{index1}, {index2}, {index3}, {index4}])")]
            set;
        }

        [NetJs.Unbox(false)]
        public extern object? this[int index1, int index2, int index3, int index4, int index5]
        {
            [NetJs.Template("{assembly.}System.Array.$" + nameof(Read) + ".call({this}, [{index1}, {index2}, {index3}, {index4}, {index5}])")]
            get;
            [NetJs.Template("{assembly.}System.Array.$" + nameof(Write) + ".call({this}, {value}, [{index1}, {index2}, {index3}, {index4}, {index5}])")]
            set;
        }

        internal static Type GetArrayType(Array array)
        {
            var et = array[ElementTypeName].As<RuntimeType?>();
            if (NetJs.Script.IsUndefined(et))
            {
                et = null;
            }
            var elementPrototype = et?._prototype ?? typeof(object).As<RuntimeType>()._prototype;
            //var prototype = elementType._prototype;
            return NetJs.Script.Write<Type>($"$.{NetJs.Constants.TypeOf}($.{NetJs.Constants.TypeArray}(elementPrototype))");
            //return typeof(Array<>).MakeGenericType(elementType);
        }

        public static void AddMetadata(Array arr, Type elementType, int[]? sizes = null, int[]? lowerBounds = null)
        {
            arr[SizesName] = sizes ?? NetJs.Script.CreateArrayFromValues(arr.Length);
            arr[ElementTypeName] = elementType;
            if (NetJs.Script.HasValue(lowerBounds))
            {
                arr[LowerBoundsName] = lowerBounds;
            }
        }

        public static void CopyMetadata(Array arr, Array source)
        {
            arr[SizesName] = source[SizesName];
            arr[ElementTypeName] = source[ElementTypeName];
            arr[LowerBoundsName] = source[LowerBoundsName];
        }

        internal static Array CreateFinal(RuntimeType type, int[] sizes, int[]? lowerBounds, NetJs.Union<object, object[]>? fill, int depth)
        {
            unchecked
            {
                Array arr = NetJs.Script.Write<Array>("new ({assembly.}System.Array$$(type._prototype))()");
                const bool createJaggedArray = false;
                if (!createJaggedArray || depth == 0)
                {
                    AddMetadata(arr, type, sizes, lowerBounds);
                }
                if (createJaggedArray && depth < sizes.Length - 1)
                {
                    for (int i = 0; i < sizes[depth]; i++)
                    {
                        var innerArray = CreateFinal(type, sizes, lowerBounds, fill, depth + 1);
                        arr.Push(innerArray);
                    }
                }
                else
                {
                    int len;
                    if (!createJaggedArray)
                    {
                        len = 1;
                        sizes.ForEach(s => len *= s);
                    }
                    else
                    {
                        len = sizes[depth];
                    }
                    var prototype = type._prototype;
                    //For struct, non primitive types, make sure we create different instance for each array item
                    var flags = prototype.Flags;
                    //if (NetJs.Script.IsUndefined(flags) && NetJs.Script.Write<bool>("prototype.bf"))
                    //{
                    //    flags = NetJs.Script.Write<TypeFlagsModel>("prototype.bf()");
                    //}
                    var defaultValue = flags.TypeHasFlag(TypeFlagsModel.IsValueType) && !flags.TypeHasFlag(TypeFlagsModel.IsPrimitive) ?
                        NetJs.Script.Undefined :
                        NetJs.Script.Write<object>($"$.{NetJs.Constants.DefaultTypeName}(prototype)");
                    if (NetJs.Script.IsDefined(fill))
                    {
                        if (fill.As<object>() is Array || NetJs.Script.Write<bool>("window.Array.isArray(fill)"))
                        {
                            var fillArr = fill.As<Array>();
                            unchecked
                            {
                                for (int i = 0; i < len; i++)
                                {
                                    if (i < fillArr.Length)
                                        arr.Push(fillArr[i]);
                                    else
                                        arr.Push(defaultValue ?? NetJs.Script.Write<object>($"$.{NetJs.Constants.DefaultTypeName}(prototype)"));
                                }
                            }
                            //NetJs.Script.Write<bool>("arr.length = {0}", len);
                            //fill.As<Array>().CopyTo(arr, 0);
                        }
                        else
                        {
                            for (int i = 0; i < len; i++)
                            {
                                arr.Push(fill);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < len; i++)
                        {
                            arr.Push(defaultValue ?? NetJs.Script.Write<object>($"$.{NetJs.Constants.DefaultTypeName}(prototype)"));
                        }
                    }
                }
                return arr;
            }
        }

        [NetJs.Name(NetJs.Constants.CreateArray)]
        internal static Array CreateFromScript(RuntimeType type, int len)
        {
            return CreateFinal(type, NetJs.Script.CreateArrayFromValues(len), null, null, 0);
        }

        [NetJs.MemberReplace(nameof(InternalCreate))]
        private static unsafe void InternalCreateImpl(ref Array? result, IntPtr elementType, int rank, int* lengths, int* lowerBounds)
        {
            unchecked
            {
                var sizes = NetJs.Script.Write<int[]>("window.Array(rank)");
                var lb = lowerBounds != null ? NetJs.Script.Write<int[]>("window.Array(rank)") : null;
                for (int i = 0; i < rank; i++)
                {
                    sizes[i] = lengths[i];
                    if (lowerBounds != null)
                    {
                        lb![i] = lowerBounds[i];
                    }
                }
                var type = AppDomain.GetType((uint)elementType) ?? throw new InvalidOperationException();
                var arr = CreateFinal(type, sizes, lb, null, 0);
                result = arr;
            }
        }

        [NetJs.MemberReplace(nameof(GetCorElementTypeOfElementTypeInternal))]
        private static CorElementType GetCorElementTypeOfElementTypeInternalImpl(ObjectHandleOnStack arr)
        {
            var marr = arr.GetObjectHandleOnStack<Array>();
            var elementType = marr[ElementTypeName].As<RuntimeType>();
            return RuntimeTypeHandle.GetCorElementType(new QCallTypeHandle(ref elementType));
        }

        [NetJs.MemberReplace(nameof(IsValueOfElementTypeInternal))]
        private static bool IsValueOfElementTypeInternalImpl(ObjectHandleOnStack arr, ObjectHandleOnStack obj)
        {
            var array = arr.GetObjectHandleOnStack<Array>();
            var value = obj.GetObjectHandleOnStack<object>();
            var elementType = array[ElementTypeName].As<RuntimeType>();
            return RuntimeTypeHandle.IsInstanceOfType(new QCallTypeHandle(ref elementType), value);
        }

        [NetJs.MemberReplace(nameof(CanChangePrimitive))]
        private static bool CanChangePrimitiveImpl(ObjectHandleOnStack srcType, ObjectHandleOnStack dstType, bool reliable)
        {
            var src = srcType.GetObjectHandleOnStack<RuntimeType>();
            var dst = dstType.GetObjectHandleOnStack<RuntimeType>();
            return src.IsPrimitive && dst.IsPrimitive;
        }

        [NetJs.MemberReplace(nameof(FastCopy))]
        internal static bool FastCopyImpl(ObjectHandleOnStack source, int source_idx, ObjectHandleOnStack dest, int dest_idx, int length)
        {
            var sourceArray = source.GetObjectHandleOnStack<Array>();
            var destinationArray = dest.GetObjectHandleOnStack<Array>();
            if (source_idx < dest_idx && sourceArray == destinationArray)
            {
                while (--length >= 0)
                {
                    destinationArray[dest_idx + length] = sourceArray[source_idx + length];
                }
            }
            else
            {
                for (var i = 0; i < length; i++)
                {
                    destinationArray[dest_idx + i] = sourceArray[source_idx + i];
                }
            }
            return true;
        }

        [NetJs.MemberReplace(nameof(GetLengthInternal))]
        private static int GetLengthInternalImpl(ObjectHandleOnStack arr, int dimension)
        {
            var marr = arr.GetObjectHandleOnStack<Array>();
            var sizes = marr[SizesName].As<int[]>();
            if (NetJs.Script.IsUndefinedOrNull(sizes) && dimension == 0)
                return marr.Length;
            unchecked
            {
                return sizes[dimension];
            }
        }

        [NetJs.MemberReplace(nameof(GetLowerBoundInternal))]
        private static int GetLowerBoundInternalImpl(ObjectHandleOnStack arr, int dimension)
        {
            var marr = arr.GetObjectHandleOnStack<Array>();
            var bounds = marr[LowerBoundsName].As<int[]?>();
            if (NetJs.Script.IsUndefinedOrNull(bounds))
                return 0;
            unchecked
            {
                return bounds![dimension];
            }
        }

        // CAUTION! No bounds checking!
        [NetJs.MemberReplace(nameof(GetValueImpl))]
        private static void GetValueImplImpl(ObjectHandleOnStack arr, ObjectHandleOnStack res, int pos)
        {
            var marr = arr.GetObjectHandleOnStack<Array>();
            unchecked
            {
                var value = marr[pos];
                var elementType = marr[ElementTypeName].As<RuntimeType>();
                var boxValue = NetJs.Script.Write<object>("{global.}$box(value, elementType._prototype)");
                res.GetObjectHandleOnStack<object?>() = boxValue;
            }
        }

        // CAUTION! No bounds checking!
        [NetJs.MemberReplace(nameof(SetValueImpl))]
        private static void SetValueImplImpl(ObjectHandleOnStack arr, ObjectHandleOnStack value, int pos)
        {
            var marr = arr.GetObjectHandleOnStack<Array>();
            unchecked
            {
                var dvalue = value.GetObjectHandleOnStack<object?>();
                var elementType = marr[ElementTypeName].As<RuntimeType>();
                var unBoxValue = NetJs.Script.Write<object>("{global.}$cast(dvalue, elementType._prototype)");
                marr[pos] = unBoxValue;
            }
        }

        // CAUTION! No bounds checking!
        [NetJs.MemberReplace(nameof(GetGenericValue_icall) + "<>")]
        private static void GetGenericValue_icallImpl<T>(ObjectHandleOnStack self, int pos, out T value)
        {
            var marr = self.GetObjectHandleOnStack<Array>();
            unchecked
            {
                value = marr[pos].As<T>();
            }
        }

        // CAUTION! No bounds checking!
        [NetJs.MemberReplace(nameof(SetGenericValue_icall) + "<>")]
        private static void SetGenericValue_icallImpl<T>(ObjectHandleOnStack arr, int pos, ref T value)
        {
            var marr = arr.GetObjectHandleOnStack<T[]>();
            unchecked
            {
                marr[pos] = value;
            }
        }

        [NetJs.MemberReplace(nameof(GetElementSize))]
        internal int GetElementSizeImpl()
        {
            var elementType = this[ElementTypeName].As<RuntimeType>();
            return System.Runtime.InteropServices.Marshal.SizeOf(elementType);
        }

        [NetJs.MemberReplace(nameof(Clear) + "(Array)")]
        public static unsafe void ClearImpl(Array array)
        {
            Clear(array, 0, array.Length);
        }

        [NetJs.MemberReplace(nameof(Clear) + "(Array, int, int)")]
        public static unsafe void ClearImpl2(Array array, int index, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            int lowerBound = array.GetLowerBound(0);
            int elementSize = array.GetElementSize();
            nuint numComponents = array.NativeLength;

            int offset = index - lowerBound;

            if (index < lowerBound || offset < 0 || length < 0 || (uint)(offset + length) > numComponents)
                ThrowHelper.ThrowIndexOutOfRangeException();

            var type = array[ElementTypeName].As<RuntimeType>();
            var prototype = type._prototype;
            //For struct, non primitive types, make sure we create different instance for each array item
            var flags = prototype.Flags;
            //if (NetJs.Script.IsUndefined(flags) && NetJs.Script.Write<bool>("prototype.bf"))
            //{
            //    flags = NetJs.Script.Write<TypeFlagsModel>("prototype.bf()");
            //}
            var defaultValue = flags.TypeHasFlag(TypeFlagsModel.IsValueType) && !flags.TypeHasFlag(TypeFlagsModel.IsPrimitive) ?
                NetJs.Script.Undefined :
                NetJs.Script.Write<object>($"$.{NetJs.Constants.DefaultTypeName}(prototype)");

            for (int i = 0; i < length; i++)
            {
                array[index + i] = defaultValue ?? NetJs.Script.Write<object>($"$.{NetJs.Constants.DefaultTypeName}(prototype)");
            }

            //ref byte ptr = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(array), (uint)offset * (nuint)elementSize);
            //nuint byteLength = (uint)length * (nuint)elementSize;

            //if (RuntimeHelpers.ObjectHasReferences(array))
            //    SpanHelpers.ClearWithReferences(ref Unsafe.As<byte, IntPtr>(ref ptr), byteLength / (uint)sizeof(IntPtr));
            //else
            //    SpanHelpers.ClearWithoutReferences(ref ptr, byteLength);
        }

        [NetJs.MemberReplace(nameof(InitializeInternal))]
        private static void InitializeInternalImpl(ObjectHandleOnStack arr)
        {

        }

        // CAUTION! No bounds checking!
        [NetJs.MemberReplace(nameof(SetValueRelaxedImpl))]
        private static void SetValueRelaxedImplImpl(ObjectHandleOnStack arr, ObjectHandleOnStack value, int pos)
        {
            var marr = arr.GetObjectHandleOnStack<Array>();
            unchecked
            {
                marr[pos] = value.GetObjectHandleOnStack<object?>();
            }
        }

        [NetJs.MemberReplace(nameof(GetEnumerator))]
        [NetJs.StaticCallConvention]
        public IEnumerator GetEnumeratorImpl()
        {
            return new ArrayEnumerator(this);
        }

        //[NetJs.StaticCallConvention]
        //protected T Get<T>(int[] indices)
        //{
        //    GetGenericValueImpl<T>(GetFlattenedIndex(indices).As<int>(), out var val);
        //    return val;
        //}

        //[NetJs.StaticCallConvention]
        //protected void Set<T>(int[] indices, T value)
        //{
        //    SetGenericValueImpl<T>(GetFlattenedIndex(indices).As<int>(), ref value);
        //}


        [NetJs.Name(NetJs.Constants.IsTypeName)]
        public static bool Is(object? instance)
        {
            if (instance == null)
                return false;
            if (NetJs.Script.Write<bool>("window.Array.isArray(instance)"))
                return true;
            if (NetJs.Script.InstanceOf(instance, typeof(Array)))
            {
                return true;
            }
            return false;
        }
    }

    //Class only defined for generator use
    //This class makes indexing a typed array work
    //[NetJs.External]
    public abstract class Array<T> : Array, ICollection<T>, IList<T>, IReadOnlyList<T>
    {
        //[NetJs.NativeConstructor]
        //public Array(int length) : base(length)
        //{

        //}


        [NetJs.Unbox(false)]
        public new extern T this[int index]
        {
            [NetJs.Template("{assembly.}System.Array.$" + nameof(ReadT1) + ".call({this}, {index})")]
            //[NetJs.Template("{this}.$" + nameof(ReadT) + "([{index}])")]
            [NetJs.Template("{this}[{index}]", "unchecked")]
            get;
            [NetJs.Template("{assembly.}System.Array.$" + nameof(WriteT1) + ".call({this}, {value}, {index})")]
            //[NetJs.Template("{this}.$" + nameof(WriteT) + "({value}, [{index}])")]
            [NetJs.Template("{this}[{index}] = {value}", "unchecked")]
            set;
        }

        [NetJs.Unbox(false)]
        public new extern T this[int index1, int index2]
        {
            //[NetJs.Template("{assembly.}System.Array.$" + nameof(ReadT) + ".call({this}, [{index1}, {index2}])")]
            [NetJs.Template("{this}.$" + nameof(ReadT) + "([{index1}, {index2}])")]
            get;
            //[NetJs.Template("{assembly.}System.Array.$" + nameof(WriteT) + ".call({this}, {value}, [{index1}, {index2}])")]
            [NetJs.Template("{this}.$" + nameof(WriteT) + "({value}, [{index1}, {index2}])")]
            set;
        }

        [NetJs.Unbox(false)]
        public new extern T this[int index1, int index2, int index3]
        {
            //[NetJs.Template("{assembly.}System.Array.$" + nameof(ReadT) + ".call({this}, [{index1}, {index2}, {index3}])")]
            [NetJs.Template("{this}.$" + nameof(ReadT) + "([{index1}, {index2}, {index3}])")]
            get;
            //[NetJs.Template("{assembly.}System.Array.$" + nameof(WriteT) + ".call({this}, {value}, [{index1}, {index2}, {index3}])")]
            [NetJs.Template("{this}.$" + nameof(WriteT) + "({value}, [{index1}, {index2}, {index3}])")]
            set;
        }

        [NetJs.Unbox(false)]
        public new extern T this[int index1, int index2, int index3, int index4]
        {
            //[NetJs.Template("{assembly.}System.Array.$" + nameof(ReadT) + ".call({this}, [{index1}, {index2}, {index3}, {index4}])")]
            [NetJs.Template("{this}.$" + nameof(ReadT) + "([{index1}, {index2}, {index3}, {index4}])")]
            get;
            //[NetJs.Template("{assembly.}System.Array.$" + nameof(WriteT) + ".call({this}, {value}, [{index1}, {index2}, {index3}, {index4}])")]
            [NetJs.Template("{this}.$" + nameof(WriteT) + "({value}, [{index1}, {index2}, {index3}, {index4}])")]
            set;
        }

        [NetJs.Unbox(false)]
        public new extern T this[int index1, int index2, int index3, int index4, int index5]
        {
            //[NetJs.Template("{assembly.}System.Array.$" + nameof(ReadT) + ".call({this}, [{index1}, {index2}, {index3}, {index4}, {index5}])")]
            [NetJs.Template("{this}.$" + nameof(ReadT) + "([{index1}, {index2}, {index3}, {index4}, {index5}])")]
            get;
            //[NetJs.Template("{assembly.}System.Array.$" + nameof(WriteT) + ".call({this}, {value}, [{index1}, {index2}, {index3}, {index4}, {index5}])")]
            [NetJs.Template("{this}.$" + nameof(WriteT) + "({value}, [{index1}, {index2}, {index3}, {index4}, {index5}])")]
            set;
        }

        public int Count => Length;

        public void Add(T item)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        public void Clear()
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        public bool Contains(T item)
        {
            return IndexOf(this.As<T[]>(), item, 0, Length) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Copy(this, GetLowerBound(0), array, arrayIndex, Length);
        }

        public int IndexOf(T item)
        {
            return IndexOf(this.As<T[]>(), item, 0, Length);
        }

        public void Insert(int index, T item)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        bool ICollection<T>.IsReadOnly => true;
        T IList<T>.this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Length)
                    ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
                return this[index];
                //T value;
                //// Do not change this to call GetGenericValue_icall directly, due to special casing in the runtime.
                //GetGenericValueImpl(index, out value);
                //return value;
            }
            set
            {
                if ((uint)index >= (uint)Length)
                    ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();

                //if (this is object?[] oarray)
                //{
                //    oarray[index] = value;
                //    return;
                //}
                this[index] = value;
                //// Do not change this to call SetGenericValue_icall directly, due to special casing in the runtime.
                //SetGenericValueImpl(index, ref value);
            }
        }

        T IReadOnlyList<T>.this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Length)
                    ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
                return this[index];
                //T value;
                //// Do not change this to call GetGenericValue_icall directly, due to special casing in the runtime.
                //GetGenericValueImpl(index, out value);
                //return value;
            }
        }

        int IReadOnlyCollection<T>.Count => Length;

        public bool Remove(T item)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
            throw null!;
        }

        public void RemoveAt(int index)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
            throw null!;
        }

        public new IEnumerator<T> GetEnumerator()
        {
            int length = Length;
            return length == 0 ? SZGenericArrayEnumerator<T>.Empty : new SZGenericArrayEnumerator<T>(Unsafe.As<T[]>(this), length);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        [NetJs.Name(NetJs.Constants.IsTypeName)]
        public static bool Is(object? instance)
        {
            if (instance == null)
                return false;
            if (NetJs.Script.Write<bool>("window.Array.isArray(instance)"))
                return true;
            if (NetJs.Script.InstanceOf(instance, typeof(Array)))
            {
                var elementType = (RuntimeType?)instance[ElementTypeName];
                if (!elementType!.IsValueType && !elementType!.IsPointer)
                    return typeof(T).IsAssignableFrom(elementType);
            }
            return false;
        }
    }
}
