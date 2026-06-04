using System.Runtime.CompilerServices;

namespace System
{
    [NetJs.ForcePartial(typeof(Buffer))]
    public static partial class Buffer_Partial
    {
        [NetJs.MemberReplace(nameof(Buffer.Memmove) + "<>")]
        public static unsafe void MemmoveImpl<T>(ref T destination, ref T source, nuint elementCount)
        {
            var sizeOfT = sizeof(T);
            if (NetJs.Script.IsDefined(sizeOfT) && typeof(T).As<RuntimeType>()._prototype!.Metadata!.KnownType.IsNumeric())
            {
                SpanHelpers.Memmove(
                        ref Unsafe.As<T, byte>(ref destination),
                        ref Unsafe.As<T, byte>(ref source),
                        elementCount * sizeOfT.As<nuint>());
            }
            else
            {
                for (nuint i = 0; i < elementCount; i++)
                {
                    Unsafe.Add(ref destination, i) = Unsafe.Add(ref source, i);
                }
            }
        }
    }
}