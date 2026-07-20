using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BlazorJs.Core
{
    public partial struct UIElementAttribute
    {
        UIElement element;

        internal UIElementAttribute(UIElement _element)
        {
            this.element = _element;
        }

        public new string this[string key]
        {
            set
            {
                element.State.Renderer.SetElementAttribute(element, key, value);
            }
        }

        public void Set(string key, object value)
        {
            if (key == "@attributes")
            {
                if (value is IReadOnlyDictionary<string, object> dic)
                {
                    foreach (var kv in dic)
                    {
                        element.State.Renderer.SetElementAttribute(element, kv.Key, kv.Value);
                    }
                }
                else
                {
                    foreach (var mkey in object.GetOwnPropertyNames(value))
                    {
                        if (mkey.Length > 0 && char.IsLower(mkey[0]))
                        {
                            var val = value[mkey];
                            element.State.Renderer.SetElementAttribute(element, mkey, val);
                        }
                    }
                }
            }
            else
            {
                element.State.Renderer.SetElementAttribute(element, key, value);
            }
        }

        public void Set<T>(string key, Func<T> value)
        {
            element.State.Renderer.SetElementAttribute(element, key, value());
        }
    }
}
