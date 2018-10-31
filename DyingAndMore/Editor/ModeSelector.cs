using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor
{
    //todo: rewrite as tab control

    class ModeSelector : Static
    {
        public System.Collections.ObjectModel.ReadOnlyCollection<EditorMode> Modes { get; private set; }
        private List<EditorMode> modes = new List<EditorMode>();
        protected List tabs;

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

                for (int i = 0; i < tabs.Children.Count; ++i)
                {
                    if (!tabs.Children[i].IsEnabled)
                        continue;

                    if (i == mode)
                    {
                        tabs.Children[i].Font = ActiveFont;
                        tabs.Children[i].Color = ActiveColor;
                        modes[i].Start();
                        InsertChild(modes[i]);
                    }
                    else
                    {
                        tabs.Children[i].Font = InactiveFont;
                        tabs.Children[i].Color = InactiveColor;

                        if (i == lastMode)
                        {
                            RemoveChild(modes[lastMode]);
                            modes[lastMode].End();
                        }
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
            ActiveFont = activeFont;
            InactiveFont = inactiveFont;

            AddChild(tabs = new List()
            {
                Position = new Vector2(0, 40),
                VerticalAlignment = Alignment.Start,
                HorizontalAlignment = Alignment.Middle,
                Direction = Direction.Horizontal
            });

            Modes = modes.AsReadOnly();
        }

        public void AddMode(EditorMode mode)
        {
            modes.Add(mode);

            var tab = new Static()
            {
                Text = mode.Name,
                Font = ActiveFont,
                Color = InactiveColor,
                Padding = new Vector2(20, 10),
                VerticalAlignment = Alignment.Middle
            };
            tab.Font = InactiveFont;

            tab.Click += delegate
            {
                Mode = mode;
                return UIEventResult.Handled;
            };
            tabs.AddChild(tab);
        }

        protected override void OnChildReflow(Static child)
        {
            Reflow();
            base.OnChildReflow(child);
        }

        protected override bool HandleInput(GameTime time)
        {
            for (int i = 0; i < System.Math.Min(10, modes.Count); ++i)
            {
                //set editor mode
                if (InputState.IsPress(Keys.D1 + i) || InputState.IsPress(Keys.NumPad1 + i))
                {
                    ModeIndex = i;
                    return false;
                }
            }

            return base.HandleInput(time);
        }
    }
}
