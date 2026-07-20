using System;

namespace BlazorJs.Core
{
    /// <summary>
    /// Represents a literal text
    /// </summary>
    public partial class UIText : UIFrameState, IUIContent
    {
        object oldText;
        object text;

        public object Text
        {
            get => text;
            set
            {
                if (value == null && oldText == null)
                    return;
                if (!Utility.Equal(value, oldText))
                {
                    oldText = text;
                    text = value;
                }
            }
        }

        //public UIFrameState State => this;

        internal UIText(IRenderer platformRenderer, IUIFrame parent, int id, object key) : base(platformRenderer, parent, id, key)
        {
            platformRenderer.CreateText(/*ref */this);
        }

        public void Build(object key)
        {
            if (!Utility.Equal(text, oldText))
            {
                Renderer.UpdateText(/*ref */this);
                oldText = text;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            Renderer.RemoveText(/*ref */this);
        }

        public override string ToString()
        {
            return $"{Text}";
        }
    }
}
