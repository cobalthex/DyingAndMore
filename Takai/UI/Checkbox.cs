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
        public static int CheckboxSize = 30;

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
        static float Margin { get; set; } = 10;

        /// <summary>
        /// Is this checkbox currently checked?
        /// </summary>
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value)
                    return;

                _isChecked = value;

                SetStyle("Checked", _isChecked);
                BubbleEvent(this, ValueChangedEvent, new UIEventArgs(this));
            }
        }
        private bool _isChecked = false;

        public override bool CanFocus => true;

        public CheckBox() : this(false) { }

        public CheckBox(bool isChecked)
        {
            IsChecked = isChecked;

            On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                ((CheckBox)sender).IsChecked ^= true;
                return UIEventResult.Handled;
            });
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var size = Font.MeasureString(Text, TextStyle);
            if (size.X > 0)
                size.X += Margin;
            return new Vector2(CheckboxSize + size.X, System.Math.Max(size.Y, CheckboxSize));
        }
        protected override void DrawSelf(DrawContext context)
        {
            var checkY = (ContentArea.Height - CheckboxSize) / 2;
            var boxBounds = new Rectangle(0, checkY, CheckboxSize, CheckboxSize);
            DrawRect(context.spriteBatch, HasFocus ? FocusedBorderColor : CheckColor, boxBounds);
            //todo: DrawNinePatch that is localized
            boxBounds.Offset(OffsetContentArea.Location);
            BoxSprite.Draw(context.spriteBatch, Rectangle.Intersect(boxBounds, VisibleContentArea));

            if (IsChecked)
            {
                if (CheckSprite != null)
                    DrawSprite(context.spriteBatch, CheckSprite, new Rectangle(2, checkY + 2, CheckboxSize - 4, CheckboxSize - 4));
                else
                {
                    var y = checkY + CheckboxSize - 4;
                    DrawLine(context.spriteBatch, CheckColor, new Vector2(4), new Vector2(CheckboxSize - 4, y));
                    DrawLine(context.spriteBatch, CheckColor, new Vector2(4, y), new Vector2(CheckboxSize - 4, checkY + 4));
                }
            }

            DrawText(context.textRenderer, Text, new Vector2(CheckboxSize + Margin, 0));
        }

        protected override void ApplyStyleOverride()
        {
            base.ApplyStyleOverride();

            var style = GenerateStyleSheet<CheckBoxStyleSheet>();

            if (style.Margin.HasValue) Margin = style.Margin.Value;
            if (style.CheckColor.HasValue) CheckColor = style.CheckColor.Value;
            if (style.BoxSprite.HasValue) BoxSprite = style.BoxSprite.Value;
            if (style.CheckSprite != null) CheckSprite = style.CheckSprite;
        }

        public struct CheckBoxStyleSheet : IStyleSheet<CheckBoxStyleSheet>
        {
            public string Name { get; set; }

            public float? Margin;
            public Color? CheckColor;
            public Graphics.NinePatch? BoxSprite;
            public Graphics.Sprite CheckSprite;

            public void LerpWith(CheckBoxStyleSheet other, float t)
            {
                throw new System.NotImplementedException();
            }

            public void MergeWith(CheckBoxStyleSheet other)
            {
                if (other.Margin.HasValue) Margin = other.Margin;
                if (other.CheckColor.HasValue) CheckColor = other.CheckColor;
                if (other.BoxSprite.HasValue) BoxSprite = other.BoxSprite;
                if (other.CheckSprite != null) CheckSprite = other.CheckSprite;
            }
        }

    }
}
 