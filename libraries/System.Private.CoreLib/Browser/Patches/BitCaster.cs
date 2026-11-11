namespace NetJs
{
    using System;
    public static partial class BitCaster
    {
        public static bool CanCast(Type type)
        {
            var prototype = type.As<RuntimeType>()._prototype;
            return prototype.KnownType.IsNumeric() ||
                   prototype.Flags.TypeHasFlag(TypeFlagsModel.IsValueType | TypeFlagsModel.IsPureStruct) ||
                   (prototype.Flags.TypeHasFlag(TypeFlagsModel.IsValueType | TypeFlagsModel.IsInlineArray) && NetJs.Script.IsDefined(prototype.InlineArrayType));
        }
        public static bool CanCast<T>()
            where T : allows ref struct
        {
            return CanCast(typeof(T));
        }
        /// <summary>
        /// Reads a single value of TTo from an array of TFrom starting at a specific byte offset.
        /// Performs single-pass batch extractions to completely avoid byte-by-byte loop method overhead.
        /// </summary>
        public static object ReInterpretRead(Type TFrom, Type TTo, Array source, int byteOffset)
        {
            unchecked
            {
                if (source == null) throw new ArgumentNullException(nameof(source));

                int sourceSize = TFrom.As<RuntimeType>()._prototype.Size;
                int resultSize = TTo.As<RuntimeType>()._prototype.Size;

                int totalBytesAvailable = source.Length * sourceSize;
                if (byteOffset < 0 || byteOffset + resultSize > totalBytesAvailable)
                {
                    throw new ArgumentOutOfRangeException(nameof(byteOffset), "Reading past the bounds of the source array storage.");
                }

                if (TFrom == TTo)
                {
                    var arrayOffset = byteOffset / resultSize;
                    return source[arrayOffset]!;
                }

                int startElementIndex = byteOffset / sourceSize;
                int endElementIndex = (byteOffset + resultSize - 1) / sourceSize;
                int totalElementsNeeded = (endElementIndex - startElementIndex) + 1;

                int scratchSize = totalElementsNeeded * sourceSize;
                byte[] scratchBuffer = NetJs.Script.NewArray<byte>(scratchSize);

                for (int i = 0; i < totalElementsNeeded; i++)
                {
                    GetBytesNonAlloc(TFrom, source[startElementIndex + i]!, scratchBuffer, i * sourceSize);
                }

                int internalScratchOffset = byteOffset % sourceSize;
                return AssembleBytesNonAlloc(TTo, scratchBuffer, internalScratchOffset);
            }
        }

        public static TTo ReInterpretRead<TFrom, TTo>(TFrom[] source, int byteOffset)
            where TTo : allows ref struct
        {
            return ReInterpretRead(typeof(TFrom), typeof(TTo), source, byteOffset).As<TTo>();
        }

        private static int SingleToInt32BitsTranspilerSafe(float value)
        {
            return BitConverter.SingleToInt32Bits(value);
            //if (value == 0) return 0;
            //if (float.IsNaN(value)) return 0x7FC00000;
            //if (float.IsPositiveInfinity(value)) return 0x7F800000;
            //if (float.IsNegativeInfinity(value)) return unchecked((int)0xFF800000);

            //int sign = value < 0 ? 1 : 0;
            //value = Math.Abs(value);
            //int exponent = (int)Math.Floor(Math.Log(value) / Math.Log(2));
            //double mantissa = (value / Math.Pow(2, exponent)) - 1.0;

            //int expField = (exponent + 127) & 0xFF;
            //int mantissaField = (int)Math.Round(mantissa * Math.Pow(2, 23)) & 0x7FFFFF;

            //return (sign << 31) | (expField << 23) | mantissaField;
        }

        private static float Int32BitsToSingleTranspilerSafe(int bits)
        {
            return BitConverter.Int32BitsToSingle(bits);
            //if (bits == 0) return 0f;
            //int sign = (bits >> 31) != 0 ? -1 : 1;
            //int exp = (bits >> 23) & 0xFF;
            //int frac = bits & 0x7FFFFF;

            //if (exp == 0xFF) return frac == 0 ? (sign == 1 ? float.PositiveInfinity : float.NegativeInfinity) : float.NaN;

            //double mantissa = 1.0 + ((double)frac / Math.Pow(2, 23));
            //return (float)(sign * mantissa * Math.Pow(2, exp - 127));
        }

        private static long DoubleToInt64BitsTranspilerSafe(double value)
        {
            return BitConverter.DoubleToInt64Bits(value);
            //if (value == 0) return 0L;
            //if (double.IsNaN(value)) return 0x7FF8000000000000L;
            //if (double.IsPositiveInfinity(value)) return 0x7FF0000000000000L;
            //if (double.IsNegativeInfinity(value)) return unchecked((long)0xFFF0000000000000L);

            //long sign = value < 0 ? 1L : 0L;
            //value = Math.Abs(value);
            //int exponent = (int)Math.Floor(Math.Log(value) / Math.Log(2));
            //double mantissa = (value / Math.Pow(2, exponent)) - 1.0;

            //long expField = (long)(exponent + 1023) & 0x7FFL;
            //long mantissaField = (long)Math.Round(mantissa * Math.Pow(2, 52)) & 0xFFFFFFFFFFFFFL;

            //return (sign << 63) | (expField << 52) | mantissaField;
        }

        private static double Int64BitsToDoubleTranspilerSafe(long bits)
        {
            return BitConverter.Int64BitsToDouble(bits);
            //if (bits == 0) return 0.0;
            //int sign = (bits >> 63) != 0 ? -1 : 1;
            //int exp = (int)((bits >> 52) & 0x7FFL);
            //long frac = bits & 0xFFFFFFFFFFFFFL;

            //if (exp == 0x7FF) return frac == 0 ? (sign == 1 ? double.PositiveInfinity : double.NegativeInfinity) : double.NaN;

            //double mantissa = 1.0 + ((double)frac / Math.Pow(2, 52));
            //return sign * mantissa * Math.Pow(2, exp - 1023);
        }

        /// <summary>
        /// Writes a single value of TValue into an array of TTarget starting at a specific byte offset.
        /// Extracted into contiguous chunks to minimize method-call bottlenecks.
        /// </summary>
        public static void ReInterpretWrite(Type TTarget, Type TValue, Array target, [NetJs.Box(false)] object value, int byteOffset)
        {
            unchecked
            {
                if (target == null) throw new ArgumentNullException(nameof(target));

                int targetSize = TTarget.As<RuntimeType>()._prototype.Size;
                int valueSize = TValue.As<RuntimeType>()._prototype.Size;

                int totalBytesAvailable = target.Length * targetSize;
                if (byteOffset < 0 || byteOffset + valueSize > totalBytesAvailable)
                {
                    throw new ArgumentOutOfRangeException(nameof(byteOffset), "Writing past the bounds of the target array storage.");
                }
                if (TTarget == TValue)
                {
                    var arrayOffset = byteOffset / valueSize;
                    target[arrayOffset] = value;
                    return;
                }
                int startElementIndex = byteOffset / targetSize;
                int endElementIndex = (byteOffset + valueSize - 1) / targetSize;
                int totalElementsAffected = (endElementIndex - startElementIndex) + 1;

                int scratchSize = totalElementsAffected * targetSize;
                byte[] scratchBuffer = NetJs.Script.NewArray<byte>(scratchSize);

                for (int i = 0; i < totalElementsAffected; i++)
                {
                    GetBytesNonAlloc(TTarget, target[startElementIndex + i]!, scratchBuffer, i * targetSize);
                }

                int internalScratchOffset = byteOffset % targetSize;
                GetBytesNonAlloc(TValue, value, scratchBuffer, internalScratchOffset);

                for (int i = 0; i < totalElementsAffected; i++)
                {
                    target[startElementIndex + i] = AssembleBytesNonAlloc(TTarget, scratchBuffer, i * targetSize);
                }
            }
        }

        public static void ReInterpretWrite<TTarget, TValue>(TTarget[] target, TValue value, int byteOffset)
            where TValue : allows ref struct
        {
            ReInterpretWrite(typeof(TTarget), typeof(TValue), target, value!.As<TValue, object>(), byteOffset);
        }

        private static object AssembleBytesNonAlloc(Type T, byte[] bytes, int offset)
        {
            unchecked
            {
                var prototype = T.As<RuntimeType>()._prototype;
                var knownType = prototype.KnownType;
                if (prototype.Kind == TypeKindModel.Enum)
                {
                    knownType = T.As<RuntimeType>()._prototype.As<EnumPrototype>().UnderlyingType.KnownType;
                }

                if (knownType == KnownTypeHandle.SystemBool)
                {
                    return (bytes[offset] == 0 ? false : true).As<object>();
                }
                // 1-Byte Types
                if (knownType == KnownTypeHandle.SystemByte)
                {
                    return bytes[offset].As<object>();
                }
                if (knownType == KnownTypeHandle.SystemSByte)
                {
                    int val = bytes[offset];
                    if ((val & 0x80) != 0)
                        val = (val | 0xFF000000.As<int>()).As<int>();
                    return val.As<object>();
                }

                // 2-Byte Types
                if (knownType == KnownTypeHandle.SystemInt16 || knownType == KnownTypeHandle.SystemUInt16 || knownType == KnownTypeHandle.SystemChar)
                {
                    int val = bytes[offset] | (bytes[offset + 1] << 8);
                    if (knownType == KnownTypeHandle.SystemInt16 && (val & 0x8000) != 0)
                        val = (val | 0xFFFF0000.As<int>()).As<int>();
                    return val.As<object>();
                }

                // 4-Byte Types
                if (knownType == KnownTypeHandle.SystemInt32 || knownType == KnownTypeHandle.SystemUInt32 ||
                    knownType == KnownTypeHandle.SystemIntPtr || knownType == KnownTypeHandle.SystemUIntPtr)
                {
                    int val = bytes[offset] |
                              (bytes[offset + 1] << 8) |
                              (bytes[offset + 2] << 16) |
                              (bytes[offset + 3] << 24);

                    // Use native C# >>> operator to force zero-extension in the generated JS
                    if (knownType == KnownTypeHandle.SystemUInt32 || knownType == KnownTypeHandle.SystemUIntPtr)
                    {
                        val = val >>> 0;
                    }

                    return val.As<object>();
                }

                // 8-Byte Long Types
                if (knownType == KnownTypeHandle.SystemInt64 || knownType == KnownTypeHandle.SystemUInt64)
                {
                    uint low32 = bytes[offset].As<uint>() | (bytes[offset + 1] << 8).As<uint>() | (bytes[offset + 2] << 16).As<uint>() | (bytes[offset + 3] << 24).As<uint>();
                    uint high32 = bytes[offset + 4].As<uint>() | (bytes[offset + 5] << 8).As<uint>() | (bytes[offset + 6] << 16).As<uint>() | (bytes[offset + 7] << 24).As<uint>();

                    // Native zero-extension forces fields to act as pure positive 32-bit unsigned primitives
                    uint uLow = (low32 >>> 0).As<uint>();
                    uint uHigh = (high32 >>> 0).As<uint>();

                    if (knownType == KnownTypeHandle.SystemInt64)
                        return ((long)uLow | ((long)uHigh << 32)).As<object>();
                    return ((ulong)uLow | ((ulong)uHigh << 32)).As<object>();
                }

                // Floating Point Types
                if (knownType == KnownTypeHandle.SystemSingle)
                {
                    int val = bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24);
                    float fVal = Int32BitsToSingleTranspilerSafe(val);
                    return fVal.As<object>();
                }

                if (knownType == KnownTypeHandle.SystemDouble)
                {
                    int dLow32 = bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24);
                    int dHigh32 = bytes[offset + 4] | (bytes[offset + 5] << 8) | (bytes[offset + 6] << 16) | (bytes[offset + 7] << 24);

                    int uLow = dLow32 >>> 0;
                    int uHigh = dHigh32 >>> 0;

                    long dBits = (uLow + (uHigh * 4294967296)).As<long>();

                    double dVal = Int64BitsToDoubleTranspilerSafe(dBits);
                    return dVal.As<object>();
                }

                if (prototype.Flags.TypeHasFlag(TypeFlagsModel.IsValueType | TypeFlagsModel.IsPureStruct))
                {
                    var value = prototype.NewWithDefaultConstructor();
                    var fields = value!.fieldsAsDataView;
                    for (int i = 0; i < prototype.Size; i++)
                    {
                        fields.setUint8(i, bytes[offset + i]);
                    }
                    return value;
                }
                if (prototype.Flags.TypeHasFlag(TypeFlagsModel.IsValueType | TypeFlagsModel.IsInlineArray) &&
                    NetJs.Script.IsDefined(prototype.InlineArrayType))
                {
                    var value = prototype.NewWithDefaultConstructor();
                    var fields = value!.fieldsAsArray;
                    for (int i = 0; i < prototype.InlineArraySize; i++)
                    {
                        var field = AssembleBytesNonAlloc(prototype.InlineArrayType!.Type!, bytes, offset + i * prototype.InlineArrayType.Size);
                        fields[i] = field;
                    }
                    return value;
                }
            }
            throw new NotSupportedException();
        }

        private static void GetBytesNonAlloc(Type T, object value, byte[] buffer, int offset)
        {
            unchecked
            {
                var prototype = T.As<RuntimeType>()._prototype;
                var knownType = prototype.KnownType;
                if (prototype.Kind == TypeKindModel.Enum)
                {
                    knownType = T.As<RuntimeType>()._prototype.As<EnumPrototype>().UnderlyingType.KnownType;
                }

                switch (knownType)
                {
                    case KnownTypeHandle.SystemBool:
                        buffer[offset] = value.As<bool>() ? 1.As<byte>() : 0.As<byte>();
                        break;

                    case KnownTypeHandle.SystemByte:
                        buffer[offset] = value.As<byte>();
                        break;

                    case KnownTypeHandle.SystemSByte:
                        buffer[offset] = (value.As<int>() & 0xFF).As<byte>();
                        break;

                    case KnownTypeHandle.SystemInt16:
                    case KnownTypeHandle.SystemUInt16:
                    case KnownTypeHandle.SystemChar:
                        int v16 = value.As<int>();
                        buffer[offset] = (v16 & 0xFF).As<byte>();
                        buffer[offset + 1] = ((v16 >>> 8) & 0xFF).As<byte>();
                        break;

                    case KnownTypeHandle.SystemInt32:
                    case KnownTypeHandle.SystemUInt32:
                    case KnownTypeHandle.SystemIntPtr:
                    case KnownTypeHandle.SystemUIntPtr:
                        int v32 = value.As<int>();
                        buffer[offset] = (v32 & 0xFF).As<byte>();
                        buffer[offset + 1] = ((v32 >>> 8) & 0xFF).As<byte>();
                        buffer[offset + 2] = ((v32 >>> 16) & 0xFF).As<byte>();
                        buffer[offset + 3] = ((v32 >>> 24) & 0xFF).As<byte>();
                        break;

                    case KnownTypeHandle.SystemInt64:
                    case KnownTypeHandle.SystemUInt64:
                        long longVal = value.As<long>();
                        int low32 = (int)(longVal & 0xFFFFFFFF);
                        int high32 = (int)(longVal >> 32);

                        buffer[offset] = (low32 & 0xFF).As<byte>();
                        buffer[offset + 1] = ((low32 >>> 8) & 0xFF).As<byte>();
                        buffer[offset + 2] = ((low32 >>> 16) & 0xFF).As<byte>();
                        buffer[offset + 3] = ((low32 >>> 24) & 0xFF).As<byte>();
                        buffer[offset + 4] = (high32 & 0xFF).As<byte>();
                        buffer[offset + 5] = ((high32 >>> 8) & 0xFF).As<byte>();
                        buffer[offset + 6] = ((high32 >>> 16) & 0xFF).As<byte>();
                        buffer[offset + 7] = ((high32 >>> 24) & 0xFF).As<byte>();
                        break;

                    case KnownTypeHandle.SystemSingle:
                        int fBits = SingleToInt32BitsTranspilerSafe(value.As<float>());
                        buffer[offset] = (fBits & 0xFF).As<byte>();
                        buffer[offset + 1] = ((fBits >>> 8) & 0xFF).As<byte>();
                        buffer[offset + 2] = ((fBits >>> 16) & 0xFF).As<byte>();
                        buffer[offset + 3] = ((fBits >>> 24) & 0xFF).As<byte>();
                        break;

                    case KnownTypeHandle.SystemDouble:
                        long dBits = DoubleToInt64BitsTranspilerSafe(value.As<double>());
                        uint dLow32 = (uint)(dBits & 0xFFFFFFFF);
                        uint dHigh32 = (uint)(dBits >> 32);

                        buffer[offset] = (dLow32 & 0xFF).As<byte>();
                        buffer[offset + 1] = ((dLow32 >>> 8) & 0xFF).As<byte>();
                        buffer[offset + 2] = ((dLow32 >>> 16) & 0xFF).As<byte>();
                        buffer[offset + 3] = ((dLow32 >>> 24) & 0xFF).As<byte>();
                        buffer[offset + 4] = (dHigh32 & 0xFF).As<byte>();
                        buffer[offset + 5] = ((dHigh32 >>> 8) & 0xFF).As<byte>();
                        buffer[offset + 6] = ((dHigh32 >>> 16) & 0xFF).As<byte>();
                        buffer[offset + 7] = ((dHigh32 >>> 24) & 0xFF).As<byte>();
                        break;

                    default:
                        if (prototype.Flags.TypeHasFlag(TypeFlagsModel.IsValueType | TypeFlagsModel.IsPureStruct))
                        {
                            var fields = value!.fieldsAsDataView;
                            for (int i = 0; i < prototype.Size; i++)
                            {
                                buffer[offset + i] = fields.getUint8(i);
                            }
                            return;
                        }
                        if (prototype.Flags.TypeHasFlag(TypeFlagsModel.IsValueType | TypeFlagsModel.IsInlineArray) &&
                            NetJs.Script.IsDefined(prototype.InlineArrayType))
                        {
                            var fields = value!.fieldsAsArray;
                            for (int i = 0; i < prototype.InlineArraySize; i++)
                            {
                                GetBytesNonAlloc(prototype.InlineArrayType!.Type!, fields[i], buffer, offset + i * prototype.InlineArrayType.Size);
                            }
                            return;
                        }
                        throw new NotSupportedException();
                }
            }
        }
    }
}
