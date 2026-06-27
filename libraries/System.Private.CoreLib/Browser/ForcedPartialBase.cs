namespace NetJs
{
    //[NetJs.NonScriptable]
    public class ForcedPartialBase<T>
    {
        protected extern T THIS
        {
            [Name("this")]
            get;
        }
        //protected extern dynamic DynamicThis
        //{
        //    [dotnetJs.Name("this")]
        //    get;
        //}
    }

}
