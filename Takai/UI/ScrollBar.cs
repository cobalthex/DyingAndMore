using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace Takai.UI
{
    public class ScrollEventArgs : EventArgs
    {
        public float Delta { get; set; }

        public ScrollEventArgs(float delta)
        {
            Delta = delta;
        }
    }

    public class ScrollBar : Static
    {
        /// <summary>
        /// The size of the content. This determines how big the scroll thumb is
        /// </summary>
        public float ContentSize
        {
            get => contentSize;
            set
            {
                contentSize = Math.Max(value, 0);
                ContentPosition = contentPosition;
            }
        }
        private float contentSize = 1;

        /// <summary>
        /// Is the scrollbar thumb visible
        /// </summary>
        [Data.Serializer.Ignored]
        public bool IsThumbVisible =>
            (Direction == Direction.Horizontal ? ContentArea.Width : ContentArea.Height) < ContentSize;

        /// <summary>
        /// Where the content is scrolled to
        /// </summary>
        public float ContentPosition
        {
            get => contentPosition;
            set
            {
                var newPosition = value;
                var size = Direction == Direction.Horizontal ? ContentArea.Width : ContentArea.Height;

                if (size > contentSize)
                    newPosition = 0;
                else
                    newPosition = Util.Clamp(value, 0, ContentSize - size);

                if (newPosition != contentPosition)
                {
                    var e = new ScrollEventArgs(newPosition - contentPosition);
                    contentPosition = newPosition;
                    OnScroll(e);
                    Scroll?.Invoke(this, e);
                }
            }
        }
        private float contentPosition = 0;

        /// <summary>
        /// Which direction the scrollbar moves
        /// </summary>
        public Direction Direction { get; set; } = Direction.Vertical;

        public Color ThumbColor { get; set; } = Color.White;

        protected bool didPressThumb = false;

        public override bool CanFocus => IsThumbVisible;

        public event EventHandler<ScrollEventArgs> Scroll;
        protected virtual void OnScroll(ScrollEventArgs e) { }

        public ScrollBar() { }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            if (Direction == Direction.Horizontal)
                return new Vector2(float.IsInfinity(availableSize.X) ? 20 : availableSize.X, 20);
            else
                return new Vector2(20, float.IsInfinity(availableSize.Y) ? 20 : availableSize.Y);
        }

        protected override bool HandleInput(GameTime time)
        {
            if (IsThumbVisible)
            {
                if (DidPressInside(MouseButtons.Left))
                {
                    var mouse = InputState.MousePoint - OffsetContentArea.Location;
                    var deltaMouse = InputState.MouseDelta();

                    if (didPressThumb)
                    {
                        if (Direction == Direction.Vertical)
                            ContentPosition += (int)(deltaMouse.Y * ((float)ContentSize / GetContainerSize()));
                        else if (Direction == Direction.Horizontal)
                            ContentPosition += (int)(deltaMouse.X * ((float)ContentSize / GetContainerSize()));
                        return false;
                    }
                }
                else
                    didPressThumb = false;

                if (VisibleContentArea.Contains(InputState.MousePoint) && InputState.HasScrolled())
                {
                    ScrollTowards(InputState.ScrollDelta());
                    return false;
                }

                //todo up/down + pgup/pgdn
            }
            return base.HandleInput(time);
        }

        public void ScrollTowards(int direction)
        {
            ContentPosition -= Math.Sign(direction) * (Font != null ? Font.MaxCharHeight : 20);
        }

        protected override void OnPress(ClickEventArgs args)
        {
            //todo: compare against absolute bounds not dimensions

            var thumb = GetThumbBounds();
            if (!thumb.Contains(args.position))
            {
                //todo: maybe scroll to mouse over time

                //center thumb around mouse
                if (Direction == Direction.Vertical)
                    ContentPosition = (int)((args.position.Y - GetThumbSize() / 2) * ((float)ContentSize / GetContainerSize()));
                else if (Direction == Direction.Horizontal)
                    ContentPosition = (int)((args.position.X - GetThumbSize() / 2) * ((float)ContentSize / GetContainerSize()));
            }
            didPressThumb = true;
        }

        protected float GetContainerSize()
        {
            //todo: precalculate
            switch (Direction)
            {
                case Direction.Vertical:
                    return ContentArea.Height;
                case Direction.Horizontal:
                    return ContentArea.Width;
                default:
                    return 1;
            }
        }

        protected float GetThumbSize()
        {
            //todo: cache in Measure

            switch (Direction)
            {
                case Direction.Vertical:
                    return (ContentArea.Height / ContentSize) * GetContainerSize();
                case Direction.Horizontal:
                    return (ContentArea.Width / ContentSize) * GetContainerSize(); //todo: this should be inclusive size
                default:
                    return 1;
            }
        }

        protected float GetThumbOffset()
        {
            var containerSize = GetContainerSize();
            var size = GetThumbSize();

            return Util.Clamp((ContentPosition / ContentSize) * containerSize, 0, containerSize - size);
        }

        protected Rectangle GetThumbBounds()
        {
            switch (Direction)
            {
                case Direction.Vertical:
                    return new Rectangle(0, (int)GetThumbOffset(), (int)ContentArea.Width, (int)GetThumbSize());
                case Direction.Horizontal:
                    return new Rectangle((int)GetThumbOffset(), 0, (int)GetThumbSize(), (int)ContentArea.Height);
                default:
                    return Rectangle.Empty;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (!IsThumbVisible)
                return;

            var thumb = GetThumbBounds();
            thumb.Offset(OffsetContentArea.Location);
            Graphics.Primitives2D.DrawFill(spriteBatch, ThumbColor, Rectangle.Intersect(VisibleContentArea, thumb));
        }
    }

    //todo: convert scroll bars to use enabled/disabled
    public class ScrollBox : Table
    {
        //todo: simplify to custom layout?

        /// <summary>
        /// An optional style to apply to the scrollbars (write-only)
        /// </summary>
        public ScrollBar ScrollBarTemplate
        {
            set
            {
                if (value == null)
                    return;

                var vsp = verticalScrollbar?.ChildIndex ?? -1;
                verticalScrollbar = (ScrollBar)value.Clone();
                verticalScrollbar.VerticalAlignment = Alignment.Stretch;
                verticalScrollbar.Direction = Direction.Vertical;

                verticalScrollbar.Scroll += delegate (object sender, ScrollEventArgs e)
                {
                    contentArea.Position -= new Vector2(0, e.Delta);
                };

                var hsp = horizontalScrollbar?.ChildIndex ?? -1;
                horizontalScrollbar = (ScrollBar)value.Clone();
                horizontalScrollbar.HorizontalAlignment = Alignment.Stretch;
                horizontalScrollbar.Direction = Direction.Horizontal;

                horizontalScrollbar.Scroll += delegate (object sender, ScrollEventArgs e)
                {
                    contentArea.Position -= new Vector2(e.Delta, 0);
                };

                if (vsp >= 0)
                {
                    base.InternalRemoveChildIndex(vsp);
                    base.InternalInsertChild(verticalScrollbar, vsp, false);
                }
                if (hsp >= 0)
                {
                    base.InternalRemoveChildIndex(hsp);
                    base.InternalInsertChild(horizontalScrollbar, hsp, false);
                }

                if (vsp >= 0 || hsp >= 0)
                    Reflow();
            }
        }

        protected ScrollBar verticalScrollbar;
        protected ScrollBar horizontalScrollbar;

        protected Static contentArea = new Static
        {
            HorizontalAlignment = Alignment.Stretch,
            VerticalAlignment = Alignment.Stretch
        };

        public ScrollBox()
        {
            ScrollBarTemplate = new ScrollBar();

            ColumnCount = 2;
            base.InternalInsertChild(contentArea);
            base.InternalInsertChild(verticalScrollbar);
            base.InternalInsertChild(horizontalScrollbar);

            //contentArea.ChildReflow += delegate {
            //    ResizeContentArea();
            //};
        }

        public ScrollBox(params Static[] children)
            : this()
        {
            AddChildren(children);

            //todo: correctly serialize
        }

        public override bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
        {
            return contentArea.InternalInsertChild(child, index, reflow, ignoreFocus);
        }

        public override bool InternalRemoveChildIndex(int index)
        {
            return contentArea.InternalRemoveChildIndex(index);
        }

        protected override void ReflowOverride(Vector2 availableSize)
        {
            horizontalScrollbar.ContentSize = contentArea.MeasuredSize.X;
            verticalScrollbar.ContentSize = contentArea.MeasuredSize.Y;
            base.ReflowOverride(availableSize);
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.HasScrolled() && VisibleContentArea.Contains(InputState.MousePoint))
            {
                if (InputState.IsMod(KeyMod.Shift))
                    horizontalScrollbar.ScrollTowards(InputState.ScrollDelta());
                else
                    verticalScrollbar.ScrollTowards(InputState.ScrollDelta());
                return false;
            }

            return base.HandleInput(time);
        }
    }
}
