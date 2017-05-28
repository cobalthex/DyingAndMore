
using Microsoft.Xna.Framework;
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
            get => itemSize;
            set
            {
                itemSize = value;
                OnResize(System.EventArgs.Empty);
            }
        }
        private Point itemSize = new Point(1);

        public int ItemCount
        {
            get => itemCount;
            set
            {
                itemCount = value;
                OnResize(System.EventArgs.Empty);
            }
        }
        private int itemCount = 0;

        public int Padding
        {
            get => padding;
            set
            {
                padding = value;
                OnResize(System.EventArgs.Empty);
            }
        }
        private int padding = 2;

        protected int ItemsPerRow { get; private set; } = 1;

        public int SelectedItem
        {
            get => selectedItem;
            set
            {
                selectedItem = value;
                SelectionChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }
        private int selectedItem = 0;

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
            var col = (int)((e.position.X - (padding / 2)) / (itemSize.X + padding));

            SelectedItem = MathHelper.Clamp(row + col, 0, ItemCount - 1);
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsClick(Microsoft.Xna.Framework.Input.Keys.Tab))
                RemoveFromParent();

            else if (AbsoluteBounds.Contains(InputState.MousePoint))
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
            Takai.Graphics.Primitives2D.DrawFill(spriteBatch, new Color(1, 1, 1, 0.75f), AbsoluteBounds);

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
