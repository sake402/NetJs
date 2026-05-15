using NetJs;


[NetJs.External]
public static class ObjectExtensions
{
    [NetJs.Template("{0}")]
    [NetJs.Unbox(true)]
    public static extern T As<T>([Box(false)] this object? obj) where T : allows ref struct;
    [NetJs.Template("{0}")]
    [NetJs.Unbox(true)]
    public static extern TTo As<TFrom, TTo>(this TFrom obj) where TFrom : allows ref struct where TTo : allows ref struct;

    [NetJs.Template("{global.}" + Constants.CastName + "({obj}, {T})")]
    public static extern T CastType<T>(this object obj);

    [NetJs.Template("{global.}" + Constants.TryCastName + "({obj}, {T})")]
    public static extern T TryCast<T>(this object obj);

    [NetJs.Template("{global.}" + Constants.IsTypeName + "({obj}, {T})")]
    public static extern bool Is<T>(this object obj);
    [NetJs.Template("{global.}" + Constants.IsTypeName + "({obj}, {type})")]
    public static extern bool Is(this object obj, System.Type type);
}
