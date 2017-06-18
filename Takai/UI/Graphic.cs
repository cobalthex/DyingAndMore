using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class Graphic : Static
    {
        public Graphics.Sprite Sprite { get; set; }

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

            base.DrawSelf(spriteBatch);
        }
    }
}
