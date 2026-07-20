namespace BlazorJs.Core
{
    public partial class UIMarkup : UIFrameState, IUIFrame
    {
        string markup;
        string oldMarkup;
        public string Markup
        {
            get => markup;
            set
            {
                if (value != oldMarkup)
                {
                    oldMarkup = markup;
                    markup = value;
                }
            }
        }
        //public UIFrameState State => this;

        internal UIMarkup(
            IRenderer platformRenderer,
            IUIFrame parent,
            int id,
            string markup,
            object key) : base(platformRenderer, parent, id, key)
        {
            Markup = markup;
            platformRenderer.CreateMarkup(this);
        }

        public void Build(object key)
        {
            if (markup != oldMarkup)
            {
                Renderer.UpdateMarkup(this);
                oldMarkup = markup;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            Renderer.RemoveMarkup(/*ref */this);
        }

        public override string ToString()
        {
            return $"{Markup}";
        }
    }
}
