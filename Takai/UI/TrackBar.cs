using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class TrackBar : NumericBase
    {
        //public Direction Direction { get; set; } = Direction.Horizontal; //todo

        public override bool CanFocus => true;

        protected override bool HandleInput(GameTime time)
        {
            if (!HasFocus)
                return base.HandleInput(time);

            if (DidPressInside(Input.MouseButtons.Left))
            {
                var relPos = (Input.InputState.MousePoint - OffsetContentArea.Location).ToVector2();
                Value = (int)((relPos.X / ContentArea.Width) * (Maximum - Minimum)) + Minimum;
            }
            else if (VisibleContentArea.Contains(Input.InputState.MousePoint) && Input.InputState.HasScrolled())
                IncrementValue(System.Math.Sign(Input.InputState.ScrollDelta()));
            else if (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Left))
                DecrementValue();
            else if (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Right))
                IncrementValue();
            else
                return base.HandleInput(time);

            return false;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return new Vector2(200, 20);
        }

        protected override void DrawSelf(DrawContext context)
        {
            var y = (ContentArea.Height - 1) / 2;
            DrawHLine(context.spriteBatch, Color, y, 0, ContentArea.Width);
            DrawHLine(context.spriteBatch, Color, y + 1, 0, ContentArea.Width);

            var sliderPos = (Value - Minimum) / (float)(Maximum - Minimum);
            var x = sliderPos * ContentArea.Width;
            DrawVLine(context.spriteBatch, Color, x, 0, ContentArea.Height);
            DrawVLine(context.spriteBatch, Color, x + 1, 0, ContentArea.Height);
        }
    }
}
