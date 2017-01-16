using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Editor
{
    class ModeSelector : Takai.UI.Element
    {
        protected static readonly string[] editorModes = Enum.GetNames(typeof(EditorMode));

        public EditorMode Mode { get; set; } = EditorMode.Tiles;

        public Takai.Graphics.BitmapFont ActiveFont { get; set; }
        public Takai.Graphics.BitmapFont InactiveFont { get; set; }

        public ModeSelector(Takai.Graphics.BitmapFont ActiveFont, Takai.Graphics.BitmapFont InactiveFont)
        {
            this.ActiveFont = ActiveFont;
            this.InactiveFont = InactiveFont;

            float x = 0;
            foreach (var mode in editorModes)
            {
                var child = new Takai.UI.Element()
                {
                    Text = mode,
                    Font = ActiveFont,
                    Color = Color.LightGray,
                };
                child.AutoSize(20);
                child.Position += new Vector2(x, 0);
                x += child.Size.X;
                child.Font = InactiveFont;
                AddChild(child);
            }

            AutoSize(20);

            Children[(int)Mode].Color = Color.White;
            Children[(int)Mode].Font = ActiveFont;
        }
    }
}
