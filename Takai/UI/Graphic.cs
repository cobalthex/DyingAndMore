using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class Graphic : Static
    {
        public Graphics.Sprite Sprite { get; set; }

        public override void AutoSize(float padding = 0)
        {
            Size = Sprite.Size.ToVector2() + new Vector2(padding);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            //todo: custom positioning/sizing
            if (Sprite?.Texture != null)
                Sprite.Draw(spriteBatch, AbsoluteBounds, 0);

            base.DrawSelf(spriteBatch);
        }
    }
}
