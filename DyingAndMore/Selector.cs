using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore
{
    abstract class Selector : Takai.States.State
    {
        public Editor editor;

        protected int width = 320;
        protected const int splitterWidth = 8;
        protected const int scrollbarWidth = 17; //1px for divider, 2px on all sides between thumb
        protected Vector2 Start { get { return new Vector2(GraphicsDevice.Viewport.Width - width - GetScrollbarWidth(), 0); } }

        protected SpriteBatch sbatch;

        protected bool isResizing = false;
        private int resizeXOffset = 0;
        private bool isHoveringSplitter = false;
        private bool isHoveringScroll = false;

        public int Padding = 2;

        /// <summary>
        /// The position of the scrollbar (in pixels, clamped to TotalHeight - ViewHeight)
        /// </summary>
        public int ScrollPosition { get; set; } = 0;

        /// <summary>
        /// The size of each item
        /// </summary>
        public Point ItemSize { get; set; } = new Point(1);

        /// <summary>
        /// The number of items
        /// </summary>
        public int ItemCount { get; set; } = 0;

        public int SelectedItem { get; set; } = 0;

        public Selector(Editor Editor)
            : base(Takai.States.StateType.Popup)
        {
            editor = Editor;
        }

        public override void Load()
        {
            sbatch = new SpriteBatch(GraphicsDevice);
        }

        protected int GetTotalHeight()
        {
            var cols = (width - scrollbarWidth - Padding) / (ItemSize.X + Padding);
            if (cols > 0)
            {
                var rows = (ItemCount - 1) / cols + 1;
                return Padding + (rows * (ItemSize.Y + Padding));
            }
            return 0;
        }

        /// <summary>
        /// Get the scrollbar width (handles visibility)
        /// </summary>
        /// <returns>scrollbarWidth if visible, 0 if hidden</returns>
        protected int GetScrollbarWidth()
        {
            if (width < 1)
                return 0;

            if (GetTotalHeight() > GraphicsDevice.Viewport.Height)
                return scrollbarWidth;

            return 0;
        }

        public override void Update(GameTime time)
        {
            if (InputCatalog.IsKeyClick(Keys.Tab))
                Deactivate();

            var mouse = InputCatalog.MouseState.Position;

            var scrollWidth = GetScrollbarWidth();
            var itemsPerRow = (width - Padding) / (ItemSize.X + Padding);
            var splitterX = GraphicsDevice.Viewport.Width - width - scrollWidth - splitterWidth;
            isHoveringSplitter = (mouse.X >= splitterX && mouse.X <= splitterX + splitterWidth);

            if (InputCatalog.IsMousePress(InputCatalog.MouseButton.Left))
            {
                if (isHoveringSplitter)
                {
                    isResizing = true;
                    resizeXOffset = mouse.X - splitterX;
                }
                else
                {
                    //todo: test if scrollbar

                    var mx = mouse.X - splitterX - splitterWidth - Padding;
                    SelectedItem = (((mouse.Y + ScrollPosition - Padding) / (ItemSize.Y + Padding)) * itemsPerRow) + (mx / (ItemSize.X + Padding));
                }
            }
            else if (InputCatalog.IsMouseClick(InputCatalog.MouseButton.Left))
                isResizing = false;

            else if (isResizing)
                width = GraphicsDevice.Viewport.Width - scrollWidth - splitterWidth - mouse.X + resizeXOffset;

            if (InputCatalog.HasMouseScrolled())
                ScrollPosition -= (InputCatalog.ScrollDelta() / 2);

            width = MathHelper.Clamp(width, 0, GraphicsDevice.Viewport.Width - scrollWidth - splitterWidth);
            ScrollPosition = MathHelper.Clamp(ScrollPosition, 0, GetTotalHeight() - GraphicsDevice.Viewport.Height + 4);
        }
        
        public override void Draw(GameTime Time)
        {
            var viewHeight = GraphicsDevice.Viewport.Height;

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            var scrollWidth = GetScrollbarWidth();

            Takai.Graphics.Primitives2D.DrawFill(sbatch, new Color(1, 1, 1, 0.85f), new Rectangle((int)Start.X, (int)Start.Y, width + scrollWidth, viewHeight));

            //splitter
            Takai.Graphics.Primitives2D.DrawFill
            (
                sbatch, 
                isHoveringSplitter ? (InputCatalog.MouseState.LeftButton == ButtonState.Pressed ? Color.DarkGray : Color.LightGray) : Color.Gray, 
                new Rectangle((int)Start.X - splitterWidth, (int)Start.Y, splitterWidth, viewHeight)
            );

            var relHeight = (viewHeight / (float)GetTotalHeight());
            var relScrollPos = (int)(relHeight * ScrollPosition);

            //scrollbar
            if (scrollWidth > 0)
            {
                var x = (int)Start.X + width;
                Takai.Graphics.Primitives2D.DrawLine(sbatch, Color.DarkGray, new Vector2(x, 0), new Vector2(x, viewHeight));

                var thumbHeight = (viewHeight - 4) * relHeight;
                Takai.Graphics.Primitives2D.DrawFill
                (
                    sbatch,
                    isHoveringScroll ? (InputCatalog.MouseState.LeftButton == ButtonState.Pressed ? Color.DarkGray : Color.LightGray) : Color.Gray,
                    new Rectangle(x + 2, relScrollPos + 2, scrollWidth - 4, (int)thumbHeight)
                );
            }

            //items
            var iHeight = ItemSize.Y + Padding;
            var itemsPerRow = (width - Padding) / (ItemSize.X + Padding);
            if (itemsPerRow > 0)
            {
                var first = ((ScrollPosition / iHeight) - 1) * itemsPerRow;
                for (var i = MathHelper.Max(0, first); i < MathHelper.Min(ItemCount, first + ((viewHeight - 1) / itemsPerRow) + 1); i++)
                {
                    var bounds = new Rectangle
                    (
                        Padding + (int)Start.X + (i % itemsPerRow) * (ItemSize.X + Padding),
                        Padding + (int)Start.Y + (i / itemsPerRow) * (ItemSize.Y + Padding) - relScrollPos,
                        ItemSize.X,
                        ItemSize.Y
                    );
                    DrawItem(Time, i, bounds);

                    if (i == SelectedItem)
                    {
                        Takai.Graphics.Primitives2D.DrawRect(sbatch, new Color(1, 1, 1, 0.5f), bounds);
                        bounds.Inflate(1, 1);
                        Takai.Graphics.Primitives2D.DrawRect(sbatch, Color.Black, bounds);
                    }
                }
            }

            sbatch.End();
        }

        public abstract void DrawItem(GameTime Time, int ItemIndex, Rectangle Bounds);
    }
}
