namespace NetJs
{
    [NonScriptable]
    public interface IGeneratorIteratorResult<out T>
        where T : allows ref struct
    {
        [Name("value")]
        public T Value { get; }
        [Name("done")]
        public bool Done { get; }
    }

    [ObjectLiteral]
    [IgnoreGeneric]
    public class GeneratorIteratorResult<T> : IGeneratorIteratorResult<T>
        //where T : allows ref struct
    {
        [Name("value")]
        public T Value { get; set; } = default!;
        [Name("done")]
        public bool Done { get; set; }
    }

    [NonScriptable]
    public interface IGenerator<out T>
        where T : allows ref struct
    {
        [Name("next")]
        IGeneratorIteratorResult<T> Next();
    }
}