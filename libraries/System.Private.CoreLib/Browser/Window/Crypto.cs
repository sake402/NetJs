using System;
using System.Threading.Tasks;

namespace Window
{
    /// <summary>
    /// Crypto and SubtleCrypto wrappers.
    /// </summary>
    [NetJs.External]
    public class Crypto
    {
        public extern void getRandomValues(byte[] array);
        public extern SubtleCrypto subtle { get; }
    }

    [NetJs.External]
    public class SubtleCrypto
    {
        public extern Promise<object> digest(string algorithm, object data);
        public extern Promise<object> encrypt(object algorithm, object key, object data);
        public extern Promise<object> decrypt(object algorithm, object key, object data);
        public extern Promise<object> importKey(string format, object keyData, object algorithm, bool extractable, string[] keyUsages);
        public extern Promise<object> deriveKey(object algorithm, object baseKey, object derivedKeyType, bool extractable, string[] keyUsages);
    }
}