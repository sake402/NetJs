using System;

[NetJs.Boot]
[NetJs.OutputOrder(int.MinValue)]
[NetJs.Reflectable(false)]
[NetJs.Name("$SD")]
public static class SimpleDictionaryExtension
{
    [IgnoreGeneric]
    public static void ForEach<T>(this SimpleDictionary<T> dic, NativeAction<string, T> action)
    {
        unchecked
        {
            var keys = dic.Keys;
            for (int i = 0; i < keys.Length; i++)
            {
                var value = dic[keys[i]];
                action(keys[i], value);
            }
        }
    }

    [Template("s.split({by})")]
    static extern string[] NativeSplit(this string s, string by);
    [IgnoreGeneric]
    public static void SetNested<T>(this SimpleDictionary<T> dic, string name, T value, bool throwIfExisting = true, NativeFunction<T, bool>? onAccess = null)
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
    public static T GetNested<T>(this SimpleDictionary<T> dic, string name, bool createMissingNode = true)
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
                        if (createMissingNode)
                        {
                            node = new SimpleDictionary<object>();
                            dic[nodeName] = node.As<T>();
                        }
                        else
                        {
                            return NetJs.Script.Undefined.As<T>();
                        }
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
