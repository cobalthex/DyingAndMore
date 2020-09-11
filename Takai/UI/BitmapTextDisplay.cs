using Microsoft.Xna.Framework;

namespace Takai.UI
{
    /// <summary>
    /// Simple element to display bitmapped text (Useful for logos/etc)
    /// </summary>
    public class BitmapTextDisplay : Static
    {
        public Graphics.BitmapFont BitmapFont { get; set; }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return BitmapFont?.MeasureString(Text) ?? Vector2.Zero;
        }

        protected override void DrawSelf(DrawContext context)
        {
            BitmapFont?.Draw(
                context.spriteBatch, 
                Text, 0, -1,
                VisibleContentArea, 
                VisibleContentArea.Location - OffsetContentArea.Location, 
                Color
            );
        }
    }
}
