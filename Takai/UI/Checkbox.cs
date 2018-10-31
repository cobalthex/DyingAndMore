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
        static int Margin { get; set; } = 10;

        /// <summary>
        /// Is this checkbox currently checked?
        /// </summary>
        public bool IsChecked { get; set; } = false;

        public override bool CanFocus => true;

        public CheckBox() : this(false) { }

        public CheckBox(bool isChecked)
        {
            IsChecked = isChecked;

            Click += delegate (Static sender, ClickEventArgs e)
            {
                IsChecked ^= true;
                return UIEventResult.Handled;
            };
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var size = base.MeasureOverride(availableSize);
            return size + new Vector2(size.Y + Margin, 0);
        }
        
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            //todo: modernize

            var checkboxSize = (int)System.Math.Min(ContentArea.Width, ContentArea.Height);
            var checkBounds = new Rectangle(OffsetContentArea.X, OffsetContentArea.Y, checkboxSize, checkboxSize);
            var checkboxBounds = Rectangle.Intersect(checkBounds, VisibleContentArea);
            Graphics.Primitives2D.DrawRect(spriteBatch, HasFocus ? FocusedBorderColor : CheckColor, checkboxBounds);
            checkboxBounds.Inflate(-1, -1);
            Graphics.Primitives2D.DrawRect(spriteBatch, CheckColor, checkboxBounds);

            if (IsChecked)
            {
                checkBounds.Inflate(-4, -4);
                Graphics.Primitives2D.DrawFill(spriteBatch, CheckColor, Rectangle.Intersect(checkBounds, VisibleContentArea));
            }

            DrawText(spriteBatch, new Point(checkboxSize + Margin, 0));
        }
    }
}
