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

        protected int ItemsPerRow { get; private set; } = 8;

        public Vector2 ItemMargin { get; set; } = new Vector2(2);

        public int SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = Takai.Util.Clamp(value, 0, ItemCount - 1);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private int _selectedItem = 0;

        public event EventHandler SelectionChanged;

        public override bool CanFocus => true;

        public Selector()
        {
            HorizontalAlignment = Alignment.Stretch;
            VerticalAlignment = Alignment.Stretch;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return new Vector2(ItemsPerRow * ItemSize.X, ItemCount / ItemsPerRow * ItemSize.Y);
        }

        protected override void ReflowOverride(Vector2 availableSize)
        {
            ItemsPerRow = Math.Max(1, (int)((availableSize.X - ItemMargin.X) / (ItemSize.X + ItemMargin.X)));
            base.ReflowOverride(availableSize);
        }

        protected override void OnPress(ClickEventArgs e)
        {
            var row = (int)((e.position.Y - (ItemMargin.Y / 2)) / (ItemSize.Y + ItemMargin.Y)) * ItemsPerRow;
            var col = (int)((e.position.X - (ItemMargin.X / 2)) / (ItemSize.X + ItemMargin.X));

            SelectedItem = row + col;
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(Keys.Left))
                --SelectedItem;
            else if (InputState.IsPress(Keys.Right))
                ++SelectedItem;
            else if (InputState.IsPress(Keys.Up))
                SelectedItem -= ItemsPerRow;
            else if (InputState.IsPress(Keys.Down))
                SelectedItem += ItemsPerRow;
            else
                return base.HandleInput(time);
            return false;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var visPos = VisibleContentArea.Y - OffsetContentArea.Y;
            int start = (int)(visPos / (ItemSize.Y + ItemMargin.X) * ItemsPerRow);
            start = Math.Max(start, 0);
            for (int i = start; i < Math.Min(start + (int)(ContentArea.Height / (ItemSize.Y + ItemMargin.Y) + 2) * ItemsPerRow, ItemCount); ++i)
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
