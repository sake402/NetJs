using System;
using System.Collections.Generic;
using System.Text;

namespace System.Reflection.Emit
{
    public partial class CustomAttributeBuilder
    {
        [NetJs.MemberReplace(nameof(GetBlob))]
        private static byte[] GetBlobImpl(Assembly asmb, ConstructorInfo con, object?[] constructorArgs, PropertyInfo[] namedProperties, object[] propertyValues, FieldInfo[] namedFields, object[] fieldValues)
        {
            throw new NotImplementedException();
        }

    }
}
