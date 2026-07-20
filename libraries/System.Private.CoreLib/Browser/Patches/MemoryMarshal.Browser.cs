using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    public static unsafe partial class MemoryMarshal
    {
        [NetJs.MemberReplace(nameof(GetArrayDataReference) + "<>")]
        public static ref T GetArrayDataReferenceImpl<T>(T[] array)
        {
            var reff = RuntimeHelpers.CreateArrayReferenceT(array);
            NetJs.Script.Write("return reff");
            throw null!;
        }

        [NetJs.MemberReplace(nameof(GetArrayDataReference))]
        public static ref byte GetArrayDataReferenceImpl(Array array)
        {
            var reff = RuntimeHelpers.CreateArrayReference<object>(array);
            NetJs.Script.Write("return reff");
            throw null!;
        }
    }
}
