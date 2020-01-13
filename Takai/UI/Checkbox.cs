using System.Collections.Generic;
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
        /// 9patch sprite to draw for the checkbox
        /// </summary>
        public Graphics.NinePatch BoxSprite { get; set; }

        /// <summary>
        /// The color of the checkbox check
        /// </summary>
        public Color CheckColor { get; set; } = Color.White;
        /// <summary>
        /// A sprite to draw as the check, stretched to the box
        /// </summary>
        public Graphics.Sprite CheckSprite { get; set; }

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

            On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                ((CheckBox)sender).IsChecked ^= true;
                BubbleEvent(sender, ValueChangedEvent, new UIEventArgs(sender));
                return UIEventResult.Handled;
            });
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var size = base.MeasureOverride(availableSize);
            return size + new Vector2(size.Y + Margin, 0);
        }

        public override void ApplyStyles(Dictionary<string, object> styleRules)
        {
            base.ApplyStyles(styleRules);
            Margin = GetStyleRule(styleRules, "Margin", Margin);
            CheckColor = GetStyleRule(styleRules, "CheckColor", CheckColor);
            BoxSprite = GetStyleRule(styleRules, "BoxSprite", BoxSprite);
            CheckSprite = GetStyleRule(styleRules, "CheckSprite", CheckSprite);
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
            BoxSprite.Draw(spriteBatch, checkboxBounds);

            if (IsChecked)
            {
                checkBounds.Inflate(checkboxSize * -0.2f, checkboxSize * -0.2f);
                Graphics.Primitives2D.DrawFill(spriteBatch, CheckColor, Rectangle.Intersect(checkBounds, VisibleContentArea));
                var relClip = new Rectangle(VisibleOffset, VisibleContentArea.Size - VisibleOffset);
                CheckSprite?.Draw(spriteBatch, new Rectangle(checkBounds.Location, relClip.Size), relClip, 0, Color.White, CheckSprite.ElapsedTime);
            }

            DrawText(spriteBatch, new Point(checkboxSize + Margin, 0));
        }
    }
}
