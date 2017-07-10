using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class Graphic : Static
    {
        public Graphics.Sprite Sprite { get; set; }

        /// <summary>
        /// Draw an X in the graphic if the sprite is missing
        /// </summary>
        public bool DrawXIfMissingSprite { get; set; } = false;

        public override void AutoSize(float padding = 0)
        {
            Size = (Sprite == null ? Vector2.Zero : Sprite.Size.ToVector2()) + new Vector2(padding);
        }

        protected override void UpdateSelf(GameTime time)
        {
            if (Sprite != null)
                //Sprite.ElapsedTime += time.ElapsedGameTime;
                Sprite.ElapsedTime = time.TotalGameTime;
            base.UpdateSelf(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            //todo: custom positioning/sizing
            if (Sprite?.Texture != null)
            {
                var rect = VisibleBounds;
                rect.Offset(Sprite.Origin);
                Sprite.Draw(spriteBatch, rect, 0);
            }
            else if (DrawXIfMissingSprite)
            {
                var rect = VisibleBounds;
                rect.Inflate(-4, -4);
                Graphics.Primitives2D.DrawX(spriteBatch, Color.Tomato, rect);
            }

            base.DrawSelf(spriteBatch);
        }
    }
}
