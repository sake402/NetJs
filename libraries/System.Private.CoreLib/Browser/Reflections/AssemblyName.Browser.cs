using Mono;
using NetJs;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection
{
    public partial class AssemblyName
    {
        [NetJs.MemberReplace(nameof(FreeAssemblyName))]
        internal static void FreeAssemblyNameImpl(ref MonoAssemblyName name, bool freeStruct)
        {
            Marshal.Remove(name.name);
        }

        static ref byte GetRawStringDataAsByte(string s)
        {
            var array = NetJs.Script.Write<char[]>("Array.from(s, char => char.charCodeAt(0))");
            Array.AddMetadata(array, typeof(char));
            var bArray = new byte[array.Length + 1]; //add a null terminator at the end of the array to make it compatible with C string functions
            unchecked
            {
                for (int i = 0; i < array.Length; i++)
                {
                    bArray[i] = (array[i] & 0xFF).As<byte>();
                }
            }
            var rref = RuntimeHelpers.CreateArrayReference(bArray);
            NetJs.Script.Write("return rref");
            throw null!;
        }

        [NetJs.MemberReplace(nameof(GetNativeName))]
        private static unsafe MonoAssemblyName* GetNativeNameImpl(IntPtr assemblyPtr)
        {
            var assembly = AppDomain.GetAssembly((uint)assemblyPtr);
            MonoAssemblyName name = new MonoAssemblyName();
            var model = assembly.As<RuntimeAssembly_Partial>()._model;
            name.major = 1;
            name.minor = 0;
            name.build = -1;
            var reff = NetJs.Script.Ref(in GetRawStringDataAsByte(model.FullName));
            name.name = InteropUtility.castPtr2Address(reff.As<RefOrPointer<object>>()).As<nint>();
            var reff2 = NetJs.Script.Ref(in GetRawStringDataAsByte(""));
            NetJs.Script.Write("name.public_key_token = reff2");
            //name.public_key_token[0] =  NetJs.Script.RefP<byte>(reff2);
            return &name;
        }
    }
}
