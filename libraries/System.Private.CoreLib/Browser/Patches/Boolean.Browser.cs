using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    [NetJs.ForcePartial(typeof(Boolean))]
    [NetJs.StaticCallConvention]
    public readonly partial struct Boolean_Partial
    {
        public static bool operator |(Boolean_Partial a, Boolean_Partial b)
        {
            return a.As<bool>() || b.As<bool>();
        }

        public static bool? operator |(Boolean_Partial a, Boolean_Partial? b)
        {
            if (a.As<bool>() == true && b.As<bool?>() == null)
                return true;
            if (a.As<bool>() == false && b.As<bool?>() == null)
                return null;
            return a.As<bool>() || b.As<bool?>()!.Value;
        }

        public static bool operator &(Boolean_Partial a, Boolean_Partial b)
        {
            return a.As<bool>() && b.As<bool>();
        }

        public static bool? operator &(Boolean_Partial? a, Boolean_Partial? b)
        {
            if (a != null && b != null)
                return a.As<bool?>()!.Value && b.As<bool?>()!.Value;
            if (a.As<bool>() == false && b.As<bool?>() == null)
                return false;
            if (b.As<bool>() == false && a == null)
                return false;
            return null;
        }

        public static bool? operator |(Boolean_Partial? a, Boolean_Partial? b)
        {
            if (a != null && b != null)
                return a.As<bool?>()!.Value || b.As<bool?>()!.Value;
            if (a.As<bool>() == true && b == null)
                return true;
            if (b.As<bool>() == true && a == null)
                return true;
            return null;
        }

        public static bool? operator ^(Boolean_Partial? a, Boolean_Partial? b)
        {
            if (a != null && b != null)
                return a.As<bool?>()!.Value != b.As<bool?>()!.Value;
            return null;
        }

        //public static bool? operator !(bool? a)
        //{
        //    if ( a == null)
        //        return null;
        //    return !a.Value;
        //}

        public static bool? operator &(Boolean_Partial a, Boolean_Partial? b)
        {
            if (a.As<bool>() == true && b == null)
                return null;
            if (a.As<bool>() == false && b == null)
                return false;
            return a.As<bool>() && b.As<bool?>()!.Value;
        }

        public static bool operator ^(Boolean_Partial a, Boolean_Partial b)
        {
            return a.As<bool>() != b.As<bool>();
        }

        public static bool? operator ^(Boolean_Partial a, Boolean_Partial? b)
        {
            if (a.As<bool>() == true && b == null)
                return null;
            if (a.As<bool>() == false && b == null)
                return null;
            return a ^ b!.Value;
        }

        [NetJs.MemberReplace]
        internal static bool IsTrueStringIgnoreCase(ReadOnlySpan<char> value)
        {
            return value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        [NetJs.MemberReplace]
        internal static bool IsFalseStringIgnoreCase(ReadOnlySpan<char> value)
        {
            return value.Equals("false", StringComparison.OrdinalIgnoreCase);
        }

        readonly bool _m_value;
        [NetJs.MemberReplace("m_value")]
        internal bool MValue
        {
            get
            {
                if (NetJs.Script.TypeOf(this).NativeEquals("boolean"))
                    return this.As<bool>();
                return _m_value;
            }
            set
            {
                NetJs.Script.Write("this._m_value = value");
            }
        }

        [NetJs.Name(NetJs.Constants.IsTypeName)]
        public static bool Is(object? value)
        {
            if (value == null)
                return false;
            return NetJs.Script.TypeOf(value).NativeEquals("boolean");
        }

        [NetJs.Name(NetJs.Constants.DefaultTypeName)]
        public static bool Default()
        {
            return false;
        }
    }
}
