using System;
using System.Linq;
using System.Collections.Generic;
using static H5.Core.dom;
using H5;
using Microsoft.AspNetCore.Components;

namespace BlazorJs.Core
{
    partial class UIKeyedGroup
    {
        public Dictionary<object, IUIContent> Members { get; } = new Dictionary<object, IUIContent>();
    }

    public partial class UIFrameState : IDisposable,
        IUIFrame// used by component
    {
        //Dictionary<string, object> contents;
        object contents = null;
        internal IRenderer Renderer { get; }
        IUIFrame parentFrame;
        int id;
        object key;
        internal Type ComponentType { get; set; }
        internal IComponent Component { get; set; }
        internal Node.Interface[] Elements { get; set; }
        internal int TrackedId { get; set; }

        internal UIFrameState(IRenderer renderer, IUIFrame parent, int id, object key)
        {
            Renderer = renderer;
            this.parentFrame = parent;
            this.id = id;
            this.key = key;
        }

        internal int Id => id;
        internal object Key => key;
        internal IUIFrame ParentFrame => parentFrame;
        internal IEnumerable<IUIContent> Children => contents == null ? null : object.GetOwnPropertyNames(contents)
            .SelectMany<string, IUIContent>(pn =>
            {
                var o = contents[pn];
                if (o is UIKeyedGroup g)
                {
                    return g.Members.Values;
                }
                return new[] { (IUIContent)o };
            })
            .OrderBy(o => o.State?.id ?? 0) ?? Enumerable.Empty<IUIContent>();

        public UIFrameState State => this;

        int trackingChildrenId = -1;

        T SetContentTracked<T>(T content) where T : IUIContent
        {
            if (trackingChildrenId > 0 && content != null)
            {
                content.State.TrackedId = trackingChildrenId;
            }
            return content;
        }

        //TODO use key to identify same id frame, perhaps by creating a group that has the is and the items inside the group identified by key
        internal T GetOrCreate<T>(int sequenceNumber, Func<int, T> create, object key = null) where T : IUIContent
        {
            if (sequenceNumber == 0)
                throw new InvalidOperationException("Sequence number cannot be zero. Must be a unique number in a frame.");
            var sid = sequenceNumber.ToString();
            if (contents == null)
                contents = new object();
            if (contents.TryGetValue(sid, out var t))
            {
                if (key != null)
                {
                    var group = (UIKeyedGroup)t;
                    if (!group.Members.TryGetValue(key, out var member))
                    {
                        member = create(sequenceNumber);
                        group.Members[key] = member;
                    }
                    return SetContentTracked((T)member);
                }
                else
                {
                    return SetContentTracked((T)t);
                }
            }
            var tt = create(sequenceNumber);
            if (key != null)
            {
                var group = new UIKeyedGroup();
                group.Members[key] = tt;
                contents[sid] = group;
            }
            else
            {
                contents[sid] = tt;
            }
            return SetContentTracked((T)tt);
        }

        internal void Remove(IUIContent child)
        {
            if (child.State.Key != null && contents.TryGetValue(child.State.id, out var group) && group is UIKeyedGroup kgroup)
            {
                kgroup.Members.Remove(child.State.Key);
            }
            else
            {
                contents.Remove(child.State.id);
            }
        }

        internal void TrackContents(Action action)
        {
            trackingChildrenId++;
            try
            {
                action();
            }
            finally
            {
                if (Children != null)
                {
                    var disposedChildren = Children.Where(c => c.State.TrackedId != trackingChildrenId);
                    foreach (var child in disposedChildren)
                    {
                        if (child != null)
                        {
                            child.Dispose();
                            contents.Remove(child.State.id);
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"{string.Join("", Children?.Select(v => v.ToString() ?? "") ?? Enumerable.Empty<string>())}";
        }

        const string CascadingValueKey = "__CascadingValueKey__";

        [ObjectLiteral]
        partial class CascadingValueData
        {
            public object Value { get; set; }
            public bool IsFixed { get; set; }
            public string Name { get; set; }
            public EventHandler<object> Handlers { get; set; }
        }

        internal void SetCascadingValue<T>(T value, bool isFixed, string name)
        {
            var valueKey = CascadingValueKey + (name != null ? "." + name : "");
            var cascadeValue = this[valueKey].As<CascadingValueData>() ?? new CascadingValueData
            {
                Name = name,
                Value = value,
                IsFixed = isFixed
            };
            this[valueKey] = cascadeValue;
            if (!isFixed)
            {
                if (cascadeValue.Handlers != null && !Utility.Equal(cascadeValue, value))
                {
                    cascadeValue.Handlers.Invoke(this, cascadeValue.Value);
                }
            }
        }

        bool GetCascadingValueImpl<T>(string valueKey, out CascadingValueData value)
        {
            var val = this[valueKey].As<CascadingValueData>();
            if (val != null && val.Value is T t)
            {
                value = val;
                return true;
            }
            if (parentFrame != null)
            {
                return parentFrame.State.GetCascadingValueImpl<T>(valueKey, out value);
            }
            value = default;
            return false;
        }

        internal IDisposable SubscribeCascadingValue<T>(EventHandler<T> callback, string name = null)
        {
            var valueKey = CascadingValueKey + (name != null ? "." + name : "");
            bool gotCurrentValue = GetCascadingValueImpl<T>(valueKey, out var value);
            IDisposable dispose = null;
            if (gotCurrentValue)
            {
                if (!value.IsFixed)
                {
                    EventHandler<object> call = (s, e) => callback(s, (T)e);
                    value.Handlers += call;
                    dispose = new DisposableDelegate(() => value.Handlers -= call);
                }
                callback.Invoke(this, (T)value.Value);
            }
            return dispose;
        }

        public virtual void Dispose()
        {
            if (Component is IDisposable disposable)
            {
                disposable.Dispose();
            }
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    child.Dispose();
                }
            }
            contents = null;
            ParentFrame.State.Remove(this);
            ((BrowserNativeRenderer)Renderer).Remove(id);
        }

        //public void Build(object key = null)
        //{
        //    //((BrowserNativeRenderer)Renderer).AddToRenderQueue(id);
        //    throw new NotImplementedException();
        //}
    }
}
