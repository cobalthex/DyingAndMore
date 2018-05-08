using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor.Selectors
{
    abstract class Selector : Static
    {
        protected Editor editor;

        protected ScrollBar scrollBar;

        public Point ItemSize
        {
            get => _itemSize;
            set
            {
                _itemSize = value;
                OnResize(System.EventArgs.Empty);
            }
        }
        private Point _itemSize = new Point(1);

        public int ItemCount
        {
            get => _itemCount;
            set
            {
                _itemCount = value;
                OnResize(System.EventArgs.Empty);
            }
        }
        private int _itemCount = 0;

        public int Padding
        {
            get => _padding;
            set
            {
                _padding = value;
                OnResize(System.EventArgs.Empty);
            }
        }
        private int _padding = 2;

        protected int ItemsPerRow { get; private set; } = 1;

        public int SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = MathHelper.Clamp(value, 0, ItemCount - 1);
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

        protected override void OnResize(System.EventArgs e)
        {
            ItemsPerRow = Size.X > scrollBar.Size.X
                        ? (int)((Padding + Size.X - scrollBar.Size.X) / (ItemSize.X + Padding))
                        : 1;
            scrollBar.ContentSize = (ItemCount / ItemsPerRow) * (ItemSize.Y + Padding) + Padding;
        }

        protected override void OnPress(ClickEventArgs e)
        {
            //todo: handle scroll
            var row = (int)((e.position.Y + scrollBar.ContentPosition - (Padding / 2)) / (ItemSize.Y + Padding)) * ItemsPerRow;
            var col = (int)((e.position.X - (_padding / 2)) / (_itemSize.X + _padding));

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
            Takai.Graphics.Primitives2D.DrawFill(spriteBatch, new Color(1, 1, 1, 0.75f), VisibleBounds);

            int start = scrollBar.ContentPosition / (ItemSize.Y + Padding) * ItemsPerRow;
            for (int i = start; i < Math.Min(start + (int)(Size.Y / (ItemSize.Y + Padding) + 2) * ItemsPerRow, ItemCount); ++i)
            {
                var rect = new Rectangle(
                    Padding + (i % ItemsPerRow) * (ItemSize.X + Padding),
                    Padding + (i / ItemsPerRow) * (ItemSize.Y + Padding) - scrollBar.ContentPosition,
                    ItemSize.X,
                    ItemSize.Y
                );
                rect.Offset(VisibleBounds.Location);

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
