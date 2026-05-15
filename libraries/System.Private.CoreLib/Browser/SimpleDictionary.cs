using NetJs;
using System;

namespace System
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

    [Boot]
    [OutputOrder(int.MinValue)]
    [Reflectable(false)]
    public static class SimpleDictionaryExtension
    {
        [Template("s.split({by})")]
        static extern string[] NativeSplit(this string s, string by);
        [IgnoreGeneric]
        public static void SetNested<T>(this SimpleDictionary<T> dic, string name, T value, bool throwIfExisting = true, [NativeDelegate] Func<T, bool>? onAccess = null)
        {
            unchecked
            {
                //runtime methods not available in boot code, use native code
                var names = name.NativeSplit(".");
                //var names = Script.Write<string[]>("fullTypeName.Split('.')");
                if (name.Length > 0)
                {
                    for (var i = 0; i < names.Length - 1; i++)
                    {
                        var nodeName = names[i];
                        var node = dic[nodeName].As<SimpleDictionary<object>>();
                        if (Script.IsUndefined(node))
                        {
                            node = new SimpleDictionary<object>();
                            dic[nodeName] = node.As<T>();
                        }
                        dic = node.As<SimpleDictionary<T>>();
                    }
                }
                var typeName = names[names.Length - 1];
                if (throwIfExisting && dic.ContainsKey(typeName))
                    throw new InvalidOperationException();
                if (onAccess != null)
                {
                    // this is a bit hacky, but it allows us to call onAccess when the value is accessed, without having to create a wrapper object
                    Script.Write("Object.defineProperty(dic, typeName, {{ configurable:true, get:function(){{ let done = onAccess(value); if (done){{ Object.defineProperty(dic, typeName, {{ value:value }}); }} return value; }} }})");
                    //dic[typeName] = Script.Write<T>("{{ get {{ onAccess(value); return value; }} }}");
                }
                else
                {
                    // Let there be an exception if we try to overwrite an existing property,
                    // as that would be a bug in the code using this method, and we want to catch it early.
                    //Script.Write("Object.defineProperty(dic, typeName, value)");
                    dic[typeName] = value;
                }
            }
        }

        [IgnoreGeneric]
        public static T GetNested<T>(this SimpleDictionary<T> dic, string name)
        {
            unchecked
            {
                //runtime methods not available in boot code, use native code
                var names = name.NativeSplit(".");
                //var names = Script.Write<string[]>("fullTypeName.Split('.')");
                if (name.Length > 0)
                {
                    for (var i = 0; i < names.Length - 1; i++)
                    {
                        var nodeName = names[i];
                        var node = dic[nodeName].As<SimpleDictionary<object>>();
                        if (Script.IsUndefined(node))
                        {
                            node = new SimpleDictionary<object>();
                            dic[nodeName] = node.As<T>();
                        }
                        dic = node.As<SimpleDictionary<T>>();
                    }
                }
                var typeName = names[names.Length - 1];
                return dic[typeName];
            }
        }


        [IgnoreGeneric]
        public static T RemoveNested<T>(this SimpleDictionary<T> dic, string name)
        {
            unchecked
            {
                //runtime methods not available in boot code, use native code
                var names = name.NativeSplit(".");
                //var names = Script.Write<string[]>("fullTypeName.Split('.')");
                if (name.Length > 0)
                {
                    for (var i = 0; i < names.Length - 1; i++)
                    {
                        var nodeName = names[i];
                        var node = dic[nodeName].As<SimpleDictionary<object>>();
                        if (Script.IsUndefined(node))
                        {
                            node = new SimpleDictionary<object>();
                            dic[nodeName] = node.As<T>();
                        }
                        dic = node.As<SimpleDictionary<T>>();
                    }
                }
                var typeName = names[names.Length - 1];
                var result = dic[typeName];
                Script.Delete(dic[typeName]);
                return result;
            }
        }
    }
}