using Microsoft.AspNetCore.Components;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BlazorJs.Core
{
    //public delegate void Fragment(IUIFrame context);
    //public delegate void Fragment<T>(IUIFrame context, T data);
    public delegate void FragmentDestructor();

    internal partial class UIFrame : UIFrameState, IUIFrame
    {
        //public readonly UIFrameState state;
        public RenderFragment ContentBuilder { get; internal set; }
        //public readonly FragmentDestructor TearDown;

        //public UIFrameState State => this;
        internal UIFrame(IRenderer renderer) : base(renderer, null, 0, null)
        {
            ContentBuilder = null;
            //TearDown = null;
        }

        public UIFrame(
            IUIFrame parent,
            RenderFragment contentBuilder,
            object key = null,
            [CallerLineNumber] int id = 0) : base(parent.State.Renderer, parent, id, key)
        {
            ContentBuilder = contentBuilder;
            //TearDown = tearDown;
            if (parent is UIElement && key == null)  //just so we dont create too much shadow region
            {
            }
            else
            {
                Renderer.CreateRegion(/*ref */this);
            }
        }

        public void Build(object key)
        {
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
        }

        public override string ToString()
        {
            return $"{string.Join("", Children.Select(v => v.ToString() ?? "") ?? Enumerable.Empty<string>())}";
        }
    }
}
