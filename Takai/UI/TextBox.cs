using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class TextBox : Element
    {

        public override void Draw(SpriteBatch spriteBatch)
        {
            Graphics.Primitives2D.DrawRect(spriteBatch, Color.White, AbsoluteBounds);

            var size = new Point(
                MathHelper.Min(AbsoluteBounds.Width, (int)textSize.X),
                MathHelper.Min(AbsoluteBounds.Height, (int)textSize.Y)
            );

            Font?.Draw(
                spriteBatch,
                Text,
                0, -1,
                new Rectangle(
                    AbsoluteBounds.X + 2,
                    AbsoluteBounds.Y + ((AbsoluteBounds.Height - size.Y) / 2),
                    size.X - 4,
                    size.Y
                ),
                new Point(),
                Color
            );

            foreach (var child in Children)
                child.Draw(spriteBatch);
        }
    }
}
