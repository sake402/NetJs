using NetJs;

namespace System
{
    [NetJs.Reflectable(false)]
    public class StringProxyHandler : IJsProxyHandler
    {
        public StringProxyHandler(string str)
        {
            _chars = str.ToCharArray();
            //Add a null terminator to this char, some pointer/ref usage requires this
            _chars.Push('\0');
            reff = new Ref<char>((i) =>
            {
                unchecked
                {
                    return _chars[(i ?? 0)];
                }
            }, (v, i) =>
            {
                unchecked
                {
                    _chars[(i ?? 0)] = v;
                    strDirty = true;
                }
            });
            reff._array = _chars;
        }

        public StringProxyHandler(int length)
        {
            //Add a null terminator to this char, some pointer/ref usage requires this
            _chars = new char[length + 1];
            var handler = new ArrayMutationProxyHandler(_chars);
            var _proxyChars = JSProxy.Create<char[]>(handler);
            reff = new Ref<char>((i) =>
            {
                unchecked
                {
                    return _chars[(i ?? 0)];
                }
            }, (v, i) =>
            {
                unchecked
                {
                    _chars[(i ?? 0)] = v;
                    strDirty = true;
                }
            });
            //If someone mutate this array behind the scene, we need to know and update our dirty flag
            //Which is why it is a proxy exposed
            reff._array = _proxyChars;
            handler.OnMutated += (s, e) =>
            {
                if (reff._dataView != null)
                {
                    var index = NetJs.Script.ParseInt(e.Property);
                    if (!NetJs.Script.IsNaN(index))
                    {
                        if (e.OldValue != e.Value)
                        {
                            //The backing array is mutated directly, if the reff dataView exists, it is no longer valid, not in sync with the backing array
                            reff._dataView = null;
                        }
                    }
                }
                strDirty = true;
            };
        }
        string str = "";
        internal char[] _chars;
        //internal char[] _proxyChars;
        Ref<char> reff;
        bool strDirty;
        public Ref<char> Reference => reff;
        public string Collect
        {
            get
            {
                if (strDirty || str.Length == 0)
                {
                    str = string.NativeFromCharCode(_chars, 0, _chars.Length - 1);
                    strDirty = false;
                }
                return str;
            }
        }
        public object? Get(object target, string property, object receiver)
        {
            if (property.NativeEquals(Constants.IsProxy))
            {
                return true.As<object>();
            }
            if (property.NativeEquals(Constants.ProxyType))
            {
                return typeof(string);
            }
            if (property.NativeEquals("_firstChar"))
            {
                return reff;
            }
            if (property.NativeEquals("length"))
            {
                return (_chars.Length-1).As<object>();
            }
            if (property.NativeEquals(nameof(Reference)))
            {
                return reff;
            }
            if (strDirty)
            {
                str = string.NativeFromCharCode(_chars, 0, _chars.Length-1);
                strDirty = false;
            }
            if (property.NativeEquals(nameof(Collect)))
            {
                return Collect.As<object>();
            }
            return str[property];
        }

        public bool Set(object target, string property, object value, object receiver)
        {
            if (property.NativeEquals("_firstChar"))
            {
                unchecked
                {
                    _chars[0] = value.As<char>();
                }
                strDirty = true;
                return true;
            }
            if (strDirty)
            {
                str = string.NativeFromCharCode(_chars, 0, _chars.Length - 1);
                strDirty = false;
            }
            str[property] = value;
            return true;
        }
    }
}
