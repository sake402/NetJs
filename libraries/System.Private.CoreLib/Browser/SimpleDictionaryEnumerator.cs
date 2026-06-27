using System.Collections;

namespace NetJs
{
    public class SimpleDictionaryEnumerator<T> : IDictionaryEnumerator
    {
        SimpleDictionary<T> dic;

        public SimpleDictionaryEnumerator(SimpleDictionary<T> dic)
        {
            this.dic = dic;
        }

        public DictionaryEntry Entry { get; private set; }
        public object Key => Entry.Key;
        public object? Value => Entry.Value;
        public object Current => Entry;

        int key_i;
        string[]? keys;
        public bool MoveNext()
        {
            if (keys == null)
            {
                keys = dic.Keys;
                key_i = 0;
            }
            if (key_i < keys.Length)
            {
                var key = keys[key_i];
                var value = dic[key];
                Entry = new DictionaryEntry(key, value);
                return true;
            }
            return false;
        }

        public void Reset()
        {
        }
    }
}