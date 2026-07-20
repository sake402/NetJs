using NetJs;

namespace NetJs
{
    [External]
    [ObjectLiteral]
    public class SimpleDictionary<TValue>
    {
        [Template("{}")]
        public extern SimpleDictionary();

        public extern new TValue this[string key]
        {
            [Template("{this}[{key}]")]
            get;
            [Template("{this}[{key}] = {value}")]
            set;
        }

        public extern TValue this[int key]
        {
            [Template("{this}[{key}]")]
            get;
            [Template("{this}[{key}] = {value}")]
            set;
        }

        public extern TValue this[uint key]
        {
            [Template("{this}[{key}]")]
            get;
            [Template("{this}[{key}] = {value}")]
            set;
        }

        [Template("delete {this}[{key}]")]
        public extern void Remove(int key);
        [Template("Object.getOwnPropertyNames({this}).some(e => e == {key}.toString())")]
        public extern bool ContainsKey(int key);
        [Template("delete {this}[{key}]")]
        public extern void Remove(uint key);
        [Template("Object.getOwnPropertyNames({this}).some(e => e == {key}.toString())")]
        public extern bool ContainsKey(uint key);
        [Template("delete {this}[{key}]")]
        public extern void Remove(string key);
        [Template("Object.getOwnPropertyNames({this}).some(e => e == {key})")]
        public extern bool ContainsKey(string key);
        [Template("Object.getOwnPropertyNames({this}).some(e => {this}[e] == {value})")]
        public extern bool ContainsValue(object value);
        public extern string[] Keys
        {
            [Template("Object.getOwnPropertyNames({this})")]
            get;
        }
        public extern TValue[] Values
        {
            [Template("Object.getOwnPropertyNames({this}).map(e => {this}[e])")]
            get;
        }
    }
}
