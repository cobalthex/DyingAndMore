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
                var relPos = (Input.InputState.MousePoint - OffsetBounds.Location).ToVector2();
                Value = (int)((relPos.X / Bounds.Width) * (Maximum - Minimum)) + Minimum;
                return false;
            }

            if (VisibleBounds.Contains(Input.InputState.MousePoint) && Input.InputState.HasScrolled())
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
            //todo: clip to visible bounds

            var pos = OffsetBounds.Location.ToVector2();

            var y = (OffsetBounds.Height - 1) / 2;

            Graphics.Primitives2D.DrawLine(spriteBatch, Color,
                pos + new Vector2(0, y), pos + new Vector2(Bounds.Width, y));
            ++y;
            Graphics.Primitives2D.DrawLine(spriteBatch, Color,
                pos + new Vector2(0, y), pos + new Vector2(Bounds.Width, y));

            var sliderPos = (Value - Minimum) / (float)(Maximum - Minimum);
            var x = sliderPos * Bounds.Width;

            Graphics.Primitives2D.DrawLine(spriteBatch, Color,
                pos + new Vector2(x, 0), pos + new Vector2(x,Bounds.Height));
            ++x;
            Graphics.Primitives2D.DrawLine(spriteBatch, Color,
                pos + new Vector2(x, 0), pos + new Vector2(x,Bounds.Height));
        }
    }
}
