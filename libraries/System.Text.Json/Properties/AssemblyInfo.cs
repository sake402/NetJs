
using NetJs;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;

//Even though DynamicDependencyAttribute is not allowed on a Type, but we need to keep these converters constructor away from trimming, we apply it anyway to the type itself
//We are saying that "a reference to these types, must keep their constructor"
[assembly: Attached(typeof(ArrayConverter<,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(ListOfTConverter<,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(DictionaryOfTKeyTValueConverter<,,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(ImmutableDictionaryOfTKeyTValueConverterWithReflection<,,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(IDictionaryOfTKeyTValueConverter<,,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(IReadOnlyDictionaryOfTKeyTValueConverter<,,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(ImmutableEnumerableOfTConverterWithReflection<,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(IListOfTConverter<,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(ISetOfTConverter<,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(ICollectionOfTConverter<,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(StackOfTConverter<,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(QueueOfTConverter<,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(ConcurrentStackOfTConverter<,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(ConcurrentQueueOfTConverter<,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(IEnumerableOfTConverter<,>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(IDictionaryConverter<>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(IListConverter<>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(StackOrQueueConverterWithReflection<>), typeof(DynamicDependencyAttribute), "$ctor()")]
[assembly: Attached(typeof(IEnumerableConverter<>), typeof(DynamicDependencyAttribute), "$ctor()")]
