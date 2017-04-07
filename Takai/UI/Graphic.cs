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

        public override void Draw(SpriteBatch spriteBatch)
        {
            //todo: custom positioning/sizing
            Sprite.Draw(spriteBatch, AbsoluteBounds, 0);

            base.Draw(spriteBatch);
        }
    }
}
