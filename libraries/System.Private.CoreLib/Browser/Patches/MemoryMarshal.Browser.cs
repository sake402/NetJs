using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    public static unsafe partial class MemoryMarshal
    {
        [NetJs.MemberReplace(nameof(GetArrayDataReference) + "<>")]
        public static ref T GetArrayDataReferenceImpl<T>(T[] array)
        {
            var reff = RuntimeHelpers.CreateArrayReference(array);
            NetJs.Script.Write("return reff");
            throw new NotImplementedException();
        }

        [NetJs.MemberReplace(nameof(GetArrayDataReference))]
        public static ref byte GetArrayDataReferenceImpl(Array array)
        {
            var reff = RuntimeHelpers.CreateArrayReference(array);
            NetJs.Script.Write("return reff");
            throw new NotImplementedException();
        }
    }
}
