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
            var checkSize = System.Math.Min(ContentArea.Width, ContentArea.Height);

            var boxBounds = new Rectangle(0, 0, checkSize, checkSize);
            DrawRect(spriteBatch, HasFocus ? FocusedBorderColor : CheckColor, boxBounds);
            //todo: DrawNinePatch that is localized
            boxBounds.Offset(OffsetContentArea.Location);
            BoxSprite.Draw(spriteBatch, Rectangle.Intersect(boxBounds, VisibleContentArea));

            if (IsChecked)
                DrawSprite(spriteBatch, CheckSprite, new Rectangle(2, 2, checkSize - 4, checkSize - 4));

            DrawText(spriteBatch, new Point(checkSize + Margin, 0));
        }
    }
}
 