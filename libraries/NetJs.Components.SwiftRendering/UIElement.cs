using System.Runtime.CompilerServices;
using System;
using System.Linq;
using Microsoft.AspNetCore.Components;

namespace BlazorJs.Core
{

    public delegate void ElementAttributeBuilder(ref UIElementAttribute attribute);

    /// <summary>
    /// Represents an html element
    /// </summary>
    internal partial class UIElement : UIFrameState, IUIFrame
    {
        public readonly string Tag;
        public ElementAttributeBuilder AttributeBuilder { get; set; }
        public RenderFragment ContentBuilder { get; set; }
        //public FragmentDestructor TearDown { get; set; }

        //public UIFrameState State => this;

        internal UIElement(
            IRenderer platformRenderer,
            IUIFrame parent,
            int id,
            string tag,
            object key) : base(platformRenderer, parent, id, key)
        {
            Tag = tag;
            platformRenderer.CreateElement(/*ref */this);
        }

        public void Build(object key)
        {
            if (AttributeBuilder != null)
            {
                var attributes = new UIElementAttribute(this);
                AttributeBuilder.Invoke(ref attributes);
            }
            if (ContentBuilder != null)
            {
                TrackContents(() =>
                {
                    ContentBuilder.Invoke(this, key);
                });
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            //TearDown?.Invoke();
            Renderer.RemoveElement(/*ref */this);
        }

        public override string ToString()
        {
            return $"<{Tag}>{string.Join("", Children.Select(v => v.ToString() ?? "") ?? Enumerable.Empty<string>())}</{Tag}>";
        }
    }
}
