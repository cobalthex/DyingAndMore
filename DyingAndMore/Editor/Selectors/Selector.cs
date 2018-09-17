using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor.Selectors
{
    //todo: update to modern UI practices

    abstract class Selector : Static
    {
        protected Editor editor;

        protected ScrollBar scrollBar;

        public Point ItemSize { get; set; } = new Point(1);

        public int ItemCount { get; set; } = 0;

        protected int ItemsPerRow { get; private set; } = 1;

        public Vector2 ItemMargin { get; set; } = new Vector2(2);

        public int SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = Takai.Util.Clamp(value, 0, ItemCount - 1);
                SelectionChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }
        private int _selectedItem = 0;

        public event System.EventHandler SelectionChanged;

        public override bool CanFocus => true;

        public Selector(Editor editor)
        {
            this.editor = editor;

            AddChild(scrollBar = new ScrollBar()
            {
                Size = new Vector2(20, 1),
                VerticalAlignment = Alignment.Stretch,
                HorizontalAlignment = Alignment.End,
                Direction = Direction.Vertical
            });
        }

        public override void Reflow(Rectangle container) //todo
        {
            ItemsPerRow = Size.X > scrollBar.Size.X
                        ? (int)((ItemMargin.X + Size.X - scrollBar.Size.X) / (ItemSize.X + ItemMargin.X))
                        : 1;
            scrollBar.ContentSize = (int)((ItemCount / ItemsPerRow) * (ItemSize.Y + ItemMargin.Y) + ItemMargin.Y);

            base.Reflow(container);
        }

        protected override void OnPress(ClickEventArgs e)
        {
            //todo: handle scroll
            var row = (int)((e.position.Y + scrollBar.ContentPosition - (ItemMargin.Y / 2)) / (ItemSize.Y + ItemMargin.Y)) * ItemsPerRow;
            var col = (int)((e.position.X - (ItemMargin.X / 2)) / (ItemSize.X + ItemMargin.X));

            SelectedItem = row + col;
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsClick(Keys.Tab))
                RemoveFromParent();

            else if (InputState.IsPress(Keys.Left))
                --SelectedItem;
            else if (InputState.IsPress(Keys.Right))
                ++SelectedItem;
            else if (InputState.IsPress(Keys.Up))
                SelectedItem -= ItemsPerRow;
            else if (InputState.IsPress(Keys.Down))
                SelectedItem += ItemsPerRow;

            else if (VisibleBounds.Contains(InputState.MousePoint))
            {
                if (InputState.HasScrolled())
                    scrollBar.ContentPosition -= InputState.ScrollDelta();
            }

            else if (InputState.IsPress(MouseButtons.Left))
                RemoveFromParent();

            base.HandleInput(time);
            return false;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            int start = (int)(scrollBar.ContentPosition / (ItemSize.Y + ItemMargin.X) * ItemsPerRow);
            start = System.Math.Max(start, 0);
            for (int i = start; i < System.Math.Min(start + (int)(Size.Y / (ItemSize.Y + ItemMargin.Y) + 2) * ItemsPerRow, ItemCount); ++i)
            {
                var rect = new Rectangle(
                    (int)(VisibleBounds.X + ItemMargin.X + (i % ItemsPerRow) * (ItemSize.X + ItemMargin.X)),
                    (int)(VisibleBounds.Y + ItemMargin.Y + (i / ItemsPerRow) * (ItemSize.Y + ItemMargin.Y) - scrollBar.ContentPosition),
                    ItemSize.X,
                    ItemSize.Y
                );
                rect = Rectangle.Intersect(rect, VisibleBounds);

                //Takai.Graphics.Primitives2D.DrawFill(
                //    spriteBatch,
                //    new Color((i * 24) % 255, (i * 32) % 255, (i * 16) % 255),
                //    rect
                //);
                DrawItem(spriteBatch, i, rect);
                if (i == SelectedItem)
                {
                    rect.Inflate(1, 1);
                    Takai.Graphics.Primitives2D.DrawRect(spriteBatch, Color.White, rect);
                    rect.Inflate(1, 1);
                    Takai.Graphics.Primitives2D.DrawRect(spriteBatch, Color.Black, rect);
                }
            }
        }

        public abstract void DrawItem(SpriteBatch spriteBatch, int itemIndex, Rectangle bounds);
    }
}
