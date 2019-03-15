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
        public Point ItemSize { get; set; } = new Point(1);

        public int ItemCount { get; set; } = 0;

        protected int ItemsPerRow
        {
            get => _ItemsPerRow;
            set
            {
                if (_ItemsPerRow == value)
                    return;

                _ItemsPerRow = value;
                InvalidateMeasure();
            }
        }
        private int _ItemsPerRow = 8;

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
            return new Vector2(ItemCount % ItemsPerRow, Takai.Util.CeilDiv(ItemCount, ItemsPerRow)) * (ItemSize.ToVector2() + ItemMargin);
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            ItemsPerRow = Math.Max(1, (int)((availableSize.X - ItemMargin.X) / (ItemSize.X + ItemMargin.X)));
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
            var visPos = VisibleContentArea.Y - OffsetContentArea.Y;
            int start = (int)(visPos / (ItemSize.Y + ItemMargin.X) * ItemsPerRow);
            start = Math.Max(start, 0);
            //todo: fix
            for (int i = start; i < Math.Min(start + (int)(ContentArea.Height / (ItemSize.Y + ItemMargin.Y) + ItemMargin.Y) * ItemsPerRow, ItemCount); ++i)
            {
                var rect = new Rectangle(
                    (int)(OffsetContentArea.X + ItemMargin.X + (i % ItemsPerRow) * (ItemSize.X + ItemMargin.X)),
                    (int)(OffsetContentArea.Y + ItemMargin.Y + (i / ItemsPerRow) * (ItemSize.Y + ItemMargin.Y) - visPos),
                    ItemSize.X,
                    ItemSize.Y
                );
                rect = Rectangle.Intersect(rect, VisibleContentArea);

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

        public abstract void DrawItem(SpriteBatch spriteBatch, int itemIndex, Rectangle bounds);
    }
}
