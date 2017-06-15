using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class CheckBox : Static
    {
        /// <summary>
        /// The color of the checkbox check
        /// </summary>
        public Color CheckColor { get; set; } = Color.White;

        static int dividerWidth = 15;

        public bool IsChecked { get; set; } = false;

        public CheckBox(bool isChecked = false)
        {
            IsChecked = isChecked;
        }

        public override void AutoSize(float padding = 0)
        {
            base.AutoSize(padding);
            var checkboxSize = MathHelper.Min(VirtualBounds.Width, VirtualBounds.Height);
            Size += new Vector2(checkboxSize + dividerWidth, 0);
        }

        protected override void OnClick(ClickEventArgs args)
        {
            IsChecked ^= true;
            base.OnClick(args);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var checkboxSize = MathHelper.Min(VirtualBounds.Width, VirtualBounds.Height);
            var checkboxBounds = new Rectangle(VirtualBounds.X, VirtualBounds.Y, checkboxSize, checkboxSize);
            Graphics.Primitives2D.DrawRect(spriteBatch, HasFocus ? FocusOutlineColor : CheckColor, checkboxBounds);
            checkboxBounds.Inflate(-1, -1);
            Graphics.Primitives2D.DrawRect(spriteBatch, CheckColor, checkboxBounds);

            if (IsChecked)
            {
                checkboxBounds.Inflate(-4, -4);
                Graphics.Primitives2D.DrawFill(spriteBatch, CheckColor, checkboxBounds);
            }

            DrawText(spriteBatch, new Point(checkboxSize + dividerWidth, 0));
        }
    }
}
