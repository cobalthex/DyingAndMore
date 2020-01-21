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
            var bounds = VisibleContentArea;
            var checkSize = System.Math.Min(ContentArea.Width, ContentArea.Height);
            bounds.Width = System.Math.Min(bounds.Width, checkSize);
            bounds.Height = System.Math.Min(bounds.Height, checkSize);

            Graphics.Primitives2D.DrawRect(spriteBatch, HasFocus ? FocusedBorderColor : CheckColor, bounds);
            BoxSprite.Draw(spriteBatch, bounds);

            if (IsChecked)
            {
                var checkBounds = bounds;
                checkBounds.Inflate(-2, -2);
                if (checkBounds.Width > 0 && checkBounds.Height > 0)
                {
                    Graphics.Primitives2D.DrawFill(spriteBatch, CheckColor, checkBounds);
                    var rely = (bounds.Height / (float)checkSize);
                    var relClip = new Rectangle(
                        VisibleOffset.X,
                        (int)(VisibleOffset.Y * rely),
                        CheckSprite.Width - VisibleOffset.X,
                        (int)(CheckSprite.Height * rely)
                    );
                    CheckSprite?.Draw(spriteBatch, checkBounds, relClip, 0, Color.White, CheckSprite.ElapsedTime);
                }
            }

            DrawText(spriteBatch, new Point(bounds.Width + Margin, 0));
        }
    }
}
 