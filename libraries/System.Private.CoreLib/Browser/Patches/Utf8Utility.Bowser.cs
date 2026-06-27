namespace System.Text.Unicode
{
    [NetJs.VerbatimReplacement(
        //Cast to void* while not neccessary here breaks our runtime as an arbitrary number returned by Unsafe.ByteOffset cannot be converted to a pointer
        "if ((nint)(void*)Unsafe.ByteOffset(ref *pInputBuffer, ref *pFinalPosWhereCanReadDWordFromInputBuffer) >= 4 * sizeof(uint))",
        "if (Unsafe.ByteOffset(ref *pInputBuffer, ref *pFinalPosWhereCanReadDWordFromInputBuffer) >= 4 * sizeof(uint))")]
    [NetJs.VerbatimReplacement(
        //Cast to void* while not neccessary here breaks our runtime as an arbitrary number returned by Unsafe.ByteOffset cannot be converted to a pointer
        "(nuint)(void*)Unsafe.ByteOffset(ref *pInputBuffer, ref *pFinalPosWhereCanReadDWordFromInputBuffer) + 4;",
        "(nuint)Unsafe.ByteOffset(ref *pInputBuffer, ref *pFinalPosWhereCanReadDWordFromInputBuffer) + 4;")]
    internal static unsafe partial class Utf8Utility
    {
    }
}