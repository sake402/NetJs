
namespace System
{
    [NetJs.ForcePartial(typeof(CharEnumerator))]
    public sealed partial class CharEnumerator_Partial : ForcedPartialBase<CharEnumerator>
    {
        [NetJs.MemberReplace(".ctor(string)")]
        public void Ctor(string str) //str passed to CharEnumerator may be a boxed string, if GetEnumerator is call through an interface
        {
            //THIS._str = str;
            NetJs.Script.Write($"this._str = {NetJs.Constants.GlobalName}.{NetJs.Constants.UnboxName}(str)");
        }
    }
}
