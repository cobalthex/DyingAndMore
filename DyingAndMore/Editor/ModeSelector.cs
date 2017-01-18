using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Editor
{
    class ModeSelector : Takai.UI.Element
    {
        protected static readonly string[] editorModes = Enum.GetNames(typeof(EditorMode));

        public EditorMode Mode
        {
            get { return mode; }
            set
            {
                mode = value;

                for (int i = 0; i < Children.Count; ++i)
                {
                    if (i == (int)mode)
                    {
                        Children[i].Font = ActiveFont;
                        Children[i].Color = ActiveColor;
                    }
                    else
                    {
                        Children[i].Font = InactiveFont;
                        Children[i].Color = InactiveColor;
                    }
                }
            }
        }
        private EditorMode mode;

        public Takai.Graphics.BitmapFont ActiveFont { get; set; }
        public Takai.Graphics.BitmapFont InactiveFont { get; set; }

        public Color ActiveColor { get; set; } = Color.White;
        public Color InactiveColor { get; set; } = Color.LightGray;

        public ModeSelector(Takai.Graphics.BitmapFont ActiveFont, Takai.Graphics.BitmapFont InactiveFont)
        {
            this.ActiveFont = ActiveFont;
            this.InactiveFont = InactiveFont;

            float x = 0;
            for (int i = 0; i < editorModes.Length; ++i)
            {
                var child = new Takai.UI.Element()
                {
                    Text = editorModes[i],
                    Font = ActiveFont,
                    Color = InactiveColor
                };
                child.AutoSize(Padding: 20);
                child.Position += new Vector2(x, 0);
                x += child.Size.X;
                child.Font = InactiveFont;

                var n = i;
                child.OnClick += delegate (Takai.UI.Element sender, Takai.UI.ClickEventArgs args)
                {
                    Mode = (EditorMode)n;
                };

                AddChild(child);
            }

            AutoSize();

            Children[(int)Mode].Color = ActiveColor;
            Children[(int)Mode].Font = ActiveFont;
        }
    }
}
