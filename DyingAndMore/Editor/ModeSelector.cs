using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Editor
{
    class ModeSelector : Takai.UI.Element
    {
        private System.Collections.Generic.List<EditorMode> modes;

        public EditorMode Mode
        {
            get { return modes[mode]; }
            set
            {
                ModeIndex = modes.IndexOf(value);
            }
        }
        public int ModeIndex
        {
            get { return mode; }
            set
            {
                mode = value;

                for (int i = 0; i < Children.Count; ++i)
                {
                    if (i == mode)
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
        private int mode;

        public Takai.Graphics.BitmapFont ActiveFont { get; set; }
        public Takai.Graphics.BitmapFont InactiveFont { get; set; }

        public Color ActiveColor { get; set; } = Color.White;
        public Color InactiveColor { get; set; } = Color.LightGray;

        public ModeSelector(Takai.Graphics.BitmapFont ActiveFont,
                            Takai.Graphics.BitmapFont InactiveFont)
        {
            this.ActiveFont = ActiveFont;
            this.InactiveFont = InactiveFont;
        }

        void AddMode(EditorMode Mode)
        {
            modes.Add(Mode);

            var child = new Takai.UI.Element()
            {
                Text = Mode.Name,
                Font = ActiveFont,
                Color = InactiveColor
            };
            child.AutoSize(Padding: 20);
            child.Font = InactiveFont;

            if (children.Count > 0)
                child.Position += new Vector2(children[children.Count - 1].Bounds.Right, 0);

            child.OnClick += delegate (Takai.UI.Element sender, Takai.UI.ClickEventArgs args)
            {
                this.Mode = Mode;
            };
            AddChild(child);

            AutoSize();
        }

        public override bool Update(TimeSpan DeltaTime)
        {
            for (int i = 0; i < editorModes.Count; ++i)
            {
                //set editor mode
                if (InputState.IsPress(Keys.D1 + i) || InputState.IsPress(Keys.NumPad1 + i))
                {
                    modes.ModeIndex = i;
                }
            }

            return base.Update(DeltaTime);
        }
    }
}
