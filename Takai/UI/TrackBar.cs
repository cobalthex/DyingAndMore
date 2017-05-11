﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    //scroll base?
    public class TrackBar : NumericBase
    {
        protected override bool UpdateSelf(GameTime time)
        {
            if (!base.UpdateSelf(time))
                return false;

            if (didPress)
            {
                var relPos = Input.InputState.MousePoint - AbsoluteBounds.Location;
                Value = (int)((relPos.X / Size.X) * (Maximum - Minimum)) + Minimum;
                return false;
            }
            return true;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var pos = AbsoluteBounds.Location.ToVector2();

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
