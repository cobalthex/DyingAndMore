using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    //scroll base?
    public class TrackBar : NumericBase
    {
        public override bool CanFocus => true;

        protected override bool HandleInput(GameTime time)
        {
            if (DidPressInside(Input.MouseButtons.Left))
            {
                var relPos = (Input.InputState.MousePoint - OffsetContentArea.Location).ToVector2();
                Value = (int)((relPos.X / ContentArea.Width) * (Maximum - Minimum)) + Minimum;
                return false;
            }

            if (VisibleContentArea.Contains(Input.InputState.MousePoint) && Input.InputState.HasScrolled())
            {
                Value += Increment * System.Math.Sign(Input.InputState.ScrollDelta());
                return false;
            }

            return base.HandleInput(time);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return new Vector2(100, 20);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var y = (OffsetContentArea.Height - 1) / 2;
            DrawHLine(spriteBatch, Color, y, 0, ContentArea.Width);
            DrawHLine(spriteBatch, Color, y + 1, 0, ContentArea.Width);


            var sliderPos = (Value - Minimum) / (float)(Maximum - Minimum);
            var x = sliderPos * ContentArea.Width;
            DrawVLine(spriteBatch, Color, x, 0, ContentArea.Height);
            DrawVLine(spriteBatch, Color, x + 1, 0, ContentArea.Height);
        }
    }
}
