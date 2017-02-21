using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class TextBox : Element
    {

        public override void Draw(SpriteBatch SpriteBatch)
        {
            var size = new Point(
                MathHelper.Min(AbsoluteBounds.Width, (int)textSize.X),
                MathHelper.Min(AbsoluteBounds.Height, (int)textSize.Y)
            );

            Font?.Draw(
                SpriteBatch,
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
                child.Draw(SpriteBatch);
        }
    }
}
