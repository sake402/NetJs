using NetJs;

namespace System.Reflection
{
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    [NetJs.OutputOrder(int.MinValue + 6)]
    public abstract partial class MemberInfo
    {
        internal MemberModel _model;

        protected MemberInfo(MemberModel model)
        {
            _model = model;
        }

        //public abstract MemberTypes MemberType { get; }

        //public string Name => _miMetadata.Name;

        //public Type DeclaringType => Type.GetTypeFromHandle(_miMetadata.DeclaringType) ?? throw new InvalidOperationException();

        public bool IsStatic => _model.Flags.TypeHasFlag(MemberFlagsModel.IsStatic);

        public bool IsOverride => _model.Flags.TypeHasFlag(MemberFlagsModel.IsOverride);

        public bool IsVirtual => _model.Flags.TypeHasFlag(MemberFlagsModel.IsVirtual);

        public bool IsFinal => _model.Flags.TypeHasFlag(MemberFlagsModel.IsFinal);

        public bool IsAbstract => _model.Flags.TypeHasFlag(MemberFlagsModel.IsAbstract);

        public bool IsSealed => _model.Flags.TypeHasFlag(MemberFlagsModel.IsSealed);

        public bool IsSpecialName => _model.Flags.TypeHasFlag(MemberFlagsModel.IsSpecialName);

        public bool IsFamily => _model.Flags.TypeHasFlag(MemberFlagsModel.IsFamily);

        public bool IsFamilyOrAssembly => _model.Flags.TypeHasFlag(MemberFlagsModel.IsFamilyOrAssembly);

        public bool IsFamilyAndAssembly => _model.Flags.TypeHasFlag(MemberFlagsModel.IsFamilyAndAssembly);

        public bool IsPrivate => _model.Flags.TypeHasFlag(MemberFlagsModel.IsPrivate);

        public bool IsPublic => _model.Flags.TypeHasFlag(MemberFlagsModel.IsPublic);

        public bool IsAssembly => _model.Flags.TypeHasFlag(MemberFlagsModel.IsAssembly);

        ///// <summary>
        ///// Returns an array of all custom attributes applied to this member.
        ///// </summary>
        ///// <param name="inherit">Ignored for members. Base members will never be considered.</param>
        ///// <returns>An array that contains all the custom attributes applied to this member, or an array with zero elements if no attributes are defined. </returns>
        //public object[] GetCustomAttributes(bool inherit)
        //{
        //    return _miMetadata.Attributes ?? Array.Empty<object>();
        //}

        ///// <summary>
        ///// Returns an array of custom attributes applied to this member and identified by <see cref="T:System.Type"/>.
        ///// </summary>
        ///// <param name="attributeType">The type of attribute to search for. Only attributes that are assignable to this type are returned. </param>
        ///// <param name="inherit">Ignored for members. Base members will never be considered.</param>
        ///// <returns>An array that contains all the custom attributes applied to this member, or an array with zero elements if no attributes are defined.</returns>
        //public object[] GetCustomAttributes(Type attributeType, bool inherit)
        //{
        //    return GetCustomAttributes(inherit).Filter(f => attributeType.IsInstanceOfType(f));
        //}

        ///// <summary>
        ///// Returns an array of all custom attributes applied to this member.
        ///// </summary>
        ///// <returns>An array that contains all the custom attributes applied to this member, or an array with zero elements if no attributes are defined. </returns>
        //public object[] GetCustomAttributes()
        //{
        //    return _miMetadata.Attributes ?? Array.Empty<object>();
        //}

        ///// <summary>
        ///// Returns an array of custom attributes applied to this member and identified by <see cref="T:System.Type"/>.
        ///// </summary>
        ///// <param name="attributeType">The type of attribute to search for. Only attributes that are assignable to this type are returned. </param>
        ///// <returns>An array that contains all the custom attributes applied to this member, or an array with zero elements if no attributes are defined.</returns>
        //public object[] GetCustomAttributes(Type attributeType)
        //{
        //    return GetCustomAttributes().Filter(f => attributeType.IsInstanceOfType(f));
        //}

        //public bool IsDefined(Type attributeType, bool inherit = false)
        //{
        //    return GetCustomAttributes().Some(f => attributeType.IsInstanceOfType(f));
        //}

        public bool ContainsGenericParameters => _model.Flags.TypeHasFlag(MemberFlagsModel.IsGeneric);
    }
}