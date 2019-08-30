using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class ProgressBar : NumericBase
    {
        public Direction Direction { get; set; } = Direction.Horizontal;

        public Color ForegroundColor { get; set; }
        public Graphics.NinePatch ForegroundSprite { get; set; }

        //indeterminate state (nax==min)?

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var rect = OffsetContentArea;
            if (Direction == Direction.Vertical)
                rect.Height = (int)(rect.Height * NormalizedValue);
            else
                rect.Width = (int)(rect.Width * NormalizedValue);

            var vrect = Rectangle.Intersect(rect, VisibleContentArea);
            Graphics.Primitives2D.DrawFill(spriteBatch, ForegroundColor, vrect);
            ForegroundSprite.Draw(spriteBatch, vrect); //todo: clip
            base.DrawSelf(spriteBatch);
        }
    }
}
