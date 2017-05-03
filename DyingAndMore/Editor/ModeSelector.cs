using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class ModeSelector : Takai.UI.List
    {
        private List<EditorMode> modes = new List<EditorMode>();

        public EditorMode Mode
        {
            get { return (mode >= 0 && mode < modes.Count ? modes[mode] : null); }
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
                int lastMode = mode;

                mode = value;

                for (int i = 0; i < Children.Count; ++i)
                {
                    if (i == mode)
                    {
                        Children[i].Font = ActiveFont;
                        Children[i].Color = ActiveColor;
                        modes[i].Start();
                    }
                    else
                    {
                        Children[i].Font = InactiveFont;
                        Children[i].Color = InactiveColor;

                        if (i == lastMode)
                            modes[lastMode].End();
                    }
                }
            }
        }
        private int mode = -1;

        public Takai.Graphics.BitmapFont ActiveFont { get; set; }
        public Takai.Graphics.BitmapFont InactiveFont { get; set; }

        public Color ActiveColor { get; set; } = Color.White;
        public Color InactiveColor { get; set; } = Color.LightGray;

        public ModeSelector(Takai.Graphics.BitmapFont activeFont,
                            Takai.Graphics.BitmapFont inactiveFont)
        {
            Direction = Takai.UI.Direction.Horizontal;

            ActiveFont = activeFont;
            InactiveFont = inactiveFont;
        }

        public void AddMode(EditorMode mode)
        {
            modes.Add(mode);

            var child = new Takai.UI.Static()
            {
                Text = mode.Name,
                Font = ActiveFont,
                Color = InactiveColor
            };
            child.AutoSize(padding: 20);
            child.Font = InactiveFont;

            child.OnClick += delegate (Takai.UI.Static sender, Takai.UI.ClickEventArgs args)
            {
                Mode = mode;
            };
            AddChild(child);

            AutoSize();
        }

        protected override bool UpdateSelf(GameTime time)
        {
            Mode?.Update(time);

            for (int i = 0; i < MathHelper.Min(10, modes.Count); ++i)
            {
                //set editor mode
                if (InputState.IsPress(Keys.D1 + i) || InputState.IsPress(Keys.NumPad1 + i))
                {
                    ModeIndex = i;
                    return false;
                }
            }

            return true;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Mode?.Draw(spriteBatch);
        }
    }
}
