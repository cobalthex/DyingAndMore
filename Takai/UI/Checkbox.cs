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

        /// <summary>
        /// The margin between the checkbox and text
        /// </summary>
        static int Margin { get; set; } = 15;

        /// <summary>
        /// Is this checkbox currently checked?
        /// </summary>
        public bool IsChecked { get; set; } = false;

        public CheckBox() : this(false) { }

        public CheckBox(bool isChecked)
        {
            IsChecked = isChecked;
        }

        public override void AutoSize()
        {
            base.AutoSize();
            var checkboxSize = System.Math.Min(Dimensions.Width, Dimensions.Height);
            Size += new Vector2(checkboxSize + Margin, 0);
        }

        protected override void OnClick(ClickEventArgs args)
        {
            IsChecked ^= true;
            base.OnClick(args);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var checkboxSize = System.Math.Min(AbsoluteDimensions.Width, AbsoluteDimensions.Height);
            var checkBounds = new Rectangle(AbsoluteDimensions.X, AbsoluteDimensions.Y, checkboxSize, checkboxSize);
            var checkboxBounds = Rectangle.Intersect(checkBounds, VisibleBounds);
            Graphics.Primitives2D.DrawRect(spriteBatch, HasFocus ? FocusedBorderColor : CheckColor, checkboxBounds);
            checkboxBounds.Inflate(-1, -1);
            Graphics.Primitives2D.DrawRect(spriteBatch, CheckColor, checkboxBounds);

            if (IsChecked)
            {
                checkBounds.Inflate(-4, -4);
                Graphics.Primitives2D.DrawFill(spriteBatch, CheckColor, Rectangle.Intersect(checkBounds, VisibleBounds));
            }

            DrawText(spriteBatch, new Point(checkboxSize + Margin, 0));
        }
    }
}
