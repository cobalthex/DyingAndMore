using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.UI;
using System;

namespace DyingAndMore.Editor.Selectors
{
    public abstract class Selector : Static
    {
        public Vector2 ItemSize { get; set; } = new Vector2(1);

        public int ItemCount
        {
            get => _ItemCount;
            set
            {
                if (_ItemCount == value)
                    return;

                _ItemCount = value;
                InvalidateMeasure();
            }
        }
        private int _ItemCount = 0;

        public int ItemsPerRow { get; private set; }

        public Vector2 ItemMargin { get; set; } = new Vector2(2);

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = Takai.Util.Clamp(value, 0, ItemCount - 1);
                BubbleEvent(SelectionChangedEvent, new UIEventArgs(this));
            }
        }
        private int _selectedIndex = 0;

        public override bool CanFocus => true;


        public Selector()
        {
            On(PressEvent, OnPress);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var iz = ItemSize + ItemMargin;

            int cols;
            if (float.IsPositiveInfinity(availableSize.X))
                cols = (int)Math.Sqrt(ItemCount);
            else
                cols = Math.Max(1, (int)(availableSize.X / iz.X));

            return new Vector2(cols, (float)Math.Ceiling((float)ItemCount / cols)) * iz + ItemMargin;
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            ItemsPerRow = (int)(availableSize.X / (ItemSize.X + ItemMargin.X));
            base.ArrangeOverride(availableSize);
        }

        protected virtual UIEventResult OnPress(Static sender, UIEventArgs e)
        {
            var ce = (PointerEventArgs)e;

            var row = (int)((ce.position.Y - (ItemMargin.Y / 2)) / (ItemSize.Y + ItemMargin.Y)) * ItemsPerRow;
            var col = (int)((ce.position.X - (ItemMargin.X / 2)) / (ItemSize.X + ItemMargin.X));

            SelectedIndex = row + col;
            return UIEventResult.Handled;
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(Keys.Left))
                --SelectedIndex;
            else if (InputState.IsPress(Keys.Right))
                ++SelectedIndex;
            else if (InputState.IsPress(Keys.Up))
                SelectedIndex -= ItemsPerRow;
            else if (InputState.IsPress(Keys.Down))
                SelectedIndex += ItemsPerRow;
            else
                return base.HandleInput(time);
            return false;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var iz = ItemSize + ItemMargin;
            int start = (int)(ContentArea.Y / iz.Y) * ItemsPerRow;
            start = Math.Max(start, 0);
            for (int i = start; i < Math.Min(start + (int)Math.Ceiling(ContentArea.Height / iz.Y + ItemMargin.Y) * ItemsPerRow, ItemCount); ++i)
            {
                var rect = new Rectangle(
                    (int)(OffsetContentArea.X + ItemMargin.X + (i % ItemsPerRow) * iz.X),
                    (int)(OffsetContentArea.Y + ItemMargin.Y + (i / ItemsPerRow) * iz.Y),
                    (int)ItemSize.X,
                    (int)ItemSize.Y
                );

                rect = Rectangle.Intersect(rect, VisibleContentArea); //todo: DrawItem needs to account for stretching (e.g. tilesets)

                //Takai.Graphics.Primitives2D.DrawFill(
                //    spriteBatch,
                //    new Color((i * 24) % 255, (i * 32) % 255, (i * 16) % 255),
                //    rect
                //);
                DrawItem(spriteBatch, i, rect);
                if (i == SelectedIndex)
                {
                    rect.Inflate(1, 1);
                    Takai.Graphics.Primitives2D.DrawRect(spriteBatch, Color.White, rect);
                    rect.Inflate(1, 1);
                    Takai.Graphics.Primitives2D.DrawRect(spriteBatch, Color.Black, rect);
                }
            }
        }

        public abstract void DrawItem(SpriteBatch spriteBatch, int itemIndex, Rectangle offsetBounds);
    }
}
