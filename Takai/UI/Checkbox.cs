using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class CheckBox : Static
    {
        static int dividerWidth = 15;

        public bool IsChecked { get; set; } = false;

        public CheckBox(bool isChecked = false)
        {
            IsChecked = isChecked;
        }

        public override void AutoSize(float padding = 0)
        {
            base.AutoSize(padding);
            var checkboxSize = MathHelper.Min(AbsoluteBounds.Width, AbsoluteBounds.Height);
            Size += new Vector2(checkboxSize + dividerWidth, 0);
        }

        protected override void BeforeClick(ClickEventArgs args)
        {
            IsChecked ^= true;
            base.BeforeClick(args);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var checkboxSize = MathHelper.Min(AbsoluteBounds.Width, AbsoluteBounds.Height);
            var checkboxBounds = new Rectangle(AbsoluteBounds.X, AbsoluteBounds.Y, checkboxSize, checkboxSize);
            Graphics.Primitives2D.DrawRect(spriteBatch, Color, checkboxBounds);
            checkboxBounds.Inflate(-1, -1);
            Graphics.Primitives2D.DrawRect(spriteBatch, Color, checkboxBounds);

            if (IsChecked)
            {
                checkboxBounds.Inflate(-4, -4);
                Graphics.Primitives2D.DrawFill(spriteBatch, Color, checkboxBounds);
            }

            Font?.Draw(
                spriteBatch,
                Text,
                CenterInRect(textSize, new Rectangle(
                    AbsoluteBounds.X + checkboxSize + dividerWidth,
                    AbsoluteBounds.Y,
                    AbsoluteBounds.Width - checkboxSize,
                    AbsoluteBounds.Height)),
                Color
            );
        }
    }
}
