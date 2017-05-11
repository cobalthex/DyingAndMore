using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor.Selectors
{
    class Selector2 : Static
    {
        protected ScrollBar scrollBar;

        public Point ItemSize { get; set; }
        public int ItemCount { get; set; }
        public int Padding { get; set; } = 5;

        protected int ItemsPerRow
        {
            get => (int)((Padding + Size.X - scrollBar.Size.X) / (ItemSize.X + Padding));
        }

        public Selector2()
        {
            AddChild(scrollBar = new ScrollBar()
            {
                Size = new Vector2(20, 1),
                VerticalAlignment = Alignment.Stretch,
                HorizontalAlignment = Alignment.End,
                Direction = Direction.Vertical
            });
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Takai.Graphics.Primitives2D.DrawFill(spriteBatch, Color.LightGray , AbsoluteBounds);

            for (int i = 0; i < ItemCount; ++i)
            {
                var rect = new Rectangle(
                    Padding + (i % ItemsPerRow) * (ItemSize.X + Padding),
                    Padding + (i / ItemsPerRow) * (ItemSize.Y + Padding),
                    ItemSize.X,
                    ItemSize.Y
                );
                rect.Offset(AbsoluteBounds.Location);

                Takai.Graphics.Primitives2D.DrawFill(
                    spriteBatch,
                    new Color((i * 24) % 255, (i * 32) % 255, (i * 16) % 255),
                    rect
                );
            }
        }
    }
}
