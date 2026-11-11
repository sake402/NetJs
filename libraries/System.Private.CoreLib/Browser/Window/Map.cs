namespace Window
{
    [NetJs.External]
    public class Map<TKey, TValue>
    {
        public class MapIterator
        {
            public extern MapIterator();
            //public extern bool next(out KeyValuePair<TKey, TValue> result);
        }
        public extern Map();
        public extern void set(TKey key, TValue value);
        public extern TValue get(TKey key);
        public extern bool has(TKey key);
        public extern void delete(TKey key);
        public extern MapIterator keys();
        public extern void forEach(NativeAction<TValue, TKey> callback);
        public extern int size { get; }
    }
}