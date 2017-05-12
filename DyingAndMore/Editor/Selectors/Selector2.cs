using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor.Selectors
{
    class Selector2 : Static
    {
        //todo: internal canvas

        protected ScrollBar scrollBar;

        public Point ItemSize
        {
            get => itemSize;
            set
            {
                itemSize = value;
                OnResize(System.EventArgs.Empty);
            }
        }
        private Point itemSize;

        public int ItemCount
        {
            get => itemCount;
            set
            {
                itemCount = value;
                OnResize(System.EventArgs.Empty);
            }
        }
        private int itemCount;

        public int Padding
        {
            get => padding;
            set
            {
                padding = value;
                OnResize(System.EventArgs.Empty);
            }
        }
        private int padding = 5;

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

        protected override void OnResize(System.EventArgs e)
        {
            scrollBar.ContentSize = (ItemCount / ItemsPerRow) * (ItemSize.Y + Padding);
        }

        protected override bool UpdateSelf(GameTime time)
        {
            if (!base.UpdateSelf(time))
                return false;

            if (AbsoluteBounds.Contains(InputState.MousePoint) && InputState.HasScrolled())
            {
                scrollBar.ContentPosition -= InputState.ScrollDelta();
                return false;
            }

            return true;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Takai.Graphics.Primitives2D.DrawFill(spriteBatch, Color.LightGray, AbsoluteBounds);

            int start = scrollBar.ContentPosition / (ItemSize.Y + Padding) * ItemsPerRow;
            for (int i = start; i < MathHelper.Min(start + (int)(Size.Y / (ItemSize.Y + Padding) + 2) * ItemsPerRow, ItemCount); ++i)
            {
                var rect = new Rectangle(
                    Padding + (i % ItemsPerRow) * (ItemSize.X + Padding),
                    Padding + (i / ItemsPerRow) * (ItemSize.Y + Padding) - scrollBar.ContentPosition,
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
