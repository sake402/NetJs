namespace NetJs.Translator
{
    [Flags]
    public enum NetJsBuildFlags
    {
        None,
        Module = 1 << 0,
        Global = 1 << 1,
        SingleFile = 1 << 2,
        SingleHtmlFile = 1 << 3,
        NoReflection = 1 << 4,
        SeparateReflectionModule = 1 << 5,
        MinifyNamespaces = 1 << 6,
        MinifyTypeNames = 1 << 7,
        MinifyFieldNames = 1 << 8,
        MinifyPropertyNames = 1 << 9,
        MinifyMethodNames = 1 << 10,
        MinifyEventNames = 1 << 11,
        MinifyMemberNames = MinifyFieldNames | MinifyPropertyNames | MinifyMethodNames | MinifyEventNames,
        //ShortNames = 1 << 12,
        ShortNamesCreateFromCamelCase = 1 << 13,
        InlineConstants = 1 << 14,
        Default = Global | InlineConstants | SingleFile// | MinifyNamespaces | MinifyTypeNames | MinifyMemberNames //| ShortNamesCreateFromCamelCase
    }
}
