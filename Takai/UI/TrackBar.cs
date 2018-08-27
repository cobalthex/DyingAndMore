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
                var relPos = Input.InputState.MousePoint - VisibleBounds.Location;
                Value = (int)((relPos.X / Size.X) * (Maximum - Minimum)) + Minimum;
                return false;
            }

            if (VisibleBounds.Contains(Input.InputState.MousePoint) && Input.InputState.HasScrolled())
            {
                Value += Increment * System.Math.Sign(Input.InputState.ScrollDelta());
                return false;
            }

            return base.HandleInput(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var pos = VisibleBounds.Location.ToVector2();

            var y = (Size.Y - 1) / 2;

            Graphics.Primitives2D.DrawLine(spriteBatch, Color,
                pos + new Vector2(0, y), pos + new Vector2(Size.X, y));
            ++y;
            Graphics.Primitives2D.DrawLine(spriteBatch, Color,
                pos + new Vector2(0, y), pos + new Vector2(Size.X, y));

            var sliderPos = (Value - Minimum) / (float)(Maximum - Minimum);
            var x = sliderPos * Size.X;

            Graphics.Primitives2D.DrawLine(spriteBatch, Color,
                pos + new Vector2(x, 0), pos + new Vector2(x, Size.Y));
            ++x;
            Graphics.Primitives2D.DrawLine(spriteBatch, Color,
                pos + new Vector2(x, 0), pos + new Vector2(x, Size.Y));
        }
    }
}
