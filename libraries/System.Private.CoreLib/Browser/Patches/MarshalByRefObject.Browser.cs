namespace System
{
    [NetJs.ForcePartial(typeof(MarshalByRefObject))]
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    [NetJs.OutputOrder(int.MinValue+3)] 
    public abstract partial class MarshalByRefObject_Partial
    {
    }
}
