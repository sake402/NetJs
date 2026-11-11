using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    public partial struct Nullable<T>
    {
        [NetJs.MemberReplace(nameof(HasValue))]
        [NetJs.StaticCallConvention]
        public readonly bool HasValueOverride
        {
            get
            {
                var isNull = NetJs.Script.IsUndefinedOrNull(this);
                if (isNull)
                    return false;
                var isNullable = Object.HasOwnProperty(this, nameof(hasValue));
                if (isNullable)
                {
                    return hasValue;
                }
                return true;
            }
        }

        [NetJs.MemberReplace(nameof(value))]
        [NetJs.Template("{this}", "get")]
        [NetJs.Template("{this}.{nameof(value)}", "set")]
        public T valueImpl;

        [NetJs.MemberReplace(nameof(Value))]
        [NetJs.StaticCallConvention]
        public readonly T ValueOverride
        {
            get
            {
                var isNull = NetJs.Script.IsUndefinedOrNull(this);
                if (isNull)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_NoValue();
                var isNullable = NetJs.Script.TypeOf(this).NativeEquals("object") && Object.PropertyIn(this, nameof(hasValue));
                if (isNullable)
                {
                    if (!hasValue)
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_NoValue();
                    }
                    return NetJs.Script.Write<T>("this.{nameof(value)}");
                }
                return this.As<T>();
            }
        }

        [NetJs.MemberReplace(nameof(GetValueOrDefault))]
        [NetJs.StaticCallConvention]
        public readonly T GetValueOrDefaultImpl()
        {
            var isNull = NetJs.Script.IsUndefinedOrNull(this);
            if (isNull)
                return default(T);
            return Value;
        }

        [NetJs.MemberReplace(nameof(GetValueOrDefault) + "(T)")]
        [NetJs.StaticCallConvention]
        public readonly T GetValueOrDefaultImpl(T defaultValue)
        {
            var isNull = NetJs.Script.IsUndefinedOrNull(this);
            if (isNull)
                return defaultValue;
            return Value;
        }

        [NetJs.Name(Constants.IsTypeName)]
        public static bool Is(object? value)
        {
            if (value == null)
                return false;
            if (value is T t)
                return true;
            return false;
        }

        [NetJs.Name(Constants.DefaultTypeName)]
        public static object? Default()
        {
            return null;
        }

        //[NonVersionable]
        //public readonly T GetValueOrDefault(T defaultValue) =>
        //    hasValue ? value : defaultValue;

        //public override bool Equals(object? other)
        //{
        //    if (!hasValue) return other == null;
        //    if (other == null) return false;
        //    return value.Equals(other);
        //}

        //public override int GetHashCode() => hasValue ? value.GetHashCode() : 0;

        //public override string? ToString() => hasValue ? value.ToString() : "";

        //[NonVersionable]
        //public static implicit operator T?(T value) =>
        //    new T?(value);

        //[NonVersionable]
        //public static explicit operator T(T? value) => value!.Value;

    }
}
