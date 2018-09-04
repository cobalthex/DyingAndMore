using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    /// <summary>
    /// A checkbox element.Text is drawn to the right of the checkbox
    /// </summary>
    public class CheckBox : Static
    {
        /// <summary>
        /// The color of the checkbox check
        /// </summary>
        public Color CheckColor { get; set; } = Color.White;

        static int dividerWidth = 15;

        public bool IsChecked { get; set; } = false;

        public CheckBox() : this(false) { }

        public CheckBox(bool isChecked)
        {
            IsChecked = isChecked;
        }

        public override void AutoSize(float padding = 0)
        {
            base.AutoSize(padding);
            var checkboxSize = System.Math.Min(VirtualBounds.Width, VirtualBounds.Height);
            Size += new Vector2(checkboxSize + dividerWidth, 0);
        }

        protected override void OnClick(ClickEventArgs args)
        {
            IsChecked ^= true;
            base.OnClick(args);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var checkboxSize = System.Math.Min(VirtualBounds.Width, VirtualBounds.Height);
            var checkBounds = new Rectangle(VirtualBounds.X, VirtualBounds.Y, checkboxSize, checkboxSize);
            var checkboxBounds = Rectangle.Intersect(checkBounds, VisibleBounds);
            Graphics.Primitives2D.DrawRect(spriteBatch, HasFocus ? FocusedBorderColor : CheckColor, checkboxBounds);
            checkboxBounds.Inflate(-1, -1);
            Graphics.Primitives2D.DrawRect(spriteBatch, CheckColor, checkboxBounds);

            if (IsChecked)
            {
                checkBounds.Inflate(-4, -4);
                Graphics.Primitives2D.DrawFill(spriteBatch, CheckColor, Rectangle.Intersect(checkBounds, VisibleBounds));
            }

            DrawText(spriteBatch, new Point(checkboxSize + dividerWidth, 0));
        }
    }
}
