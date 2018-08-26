using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace Takai.Data
{
    public class ScrollEventArgs : EventArgs
    {
        public int Delta { get; set; }

        public ScrollEventArgs(int delta)
        {
            Delta = delta;
        }
    }

    public class ScrollBar : Static
    {
        static float ThumbMargin = 3; //todo: apply correctly

        /// <summary>
        /// The size of the content. This determines how big the scroll thumb is
        /// </summary>
        public int ContentSize
        {
            get => contentSize;
            set
            {
                contentSize = Math.Max(value, 0);
                ContentPosition = contentPosition;
            }
        }
        private int contentSize = 1;

        /// <summary>
        /// Is the scrollbar thumb visible
        /// </summary>
        [Data.Serializer.Ignored]
        public bool IsThumbVisible =>
            (Direction == Direction.Horizontal ? Size.X : Size.Y) < ContentSize;

        /// <summary>
        /// Where the content is scrolled to
        /// </summary>
        public int ContentPosition
        {
            get => contentPosition;
            set
            {
                var newPosition = value;
                int size = (int)(Direction == Direction.Horizontal ? Size.X : Size.Y);

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
        private int contentPosition = 0;

        /// <summary>
        /// Which direction the scrollbar moves
        /// </summary>
        public Direction Direction { get; set; } = Direction.Vertical;

        protected bool didPressThumb = false;

        public override bool CanFocus => IsThumbVisible;

        public event EventHandler<ScrollEventArgs> Scroll;
        protected virtual void OnScroll(ScrollEventArgs e) { }

        public ScrollBar()
        {
            BorderColor = Color;
        }

        protected override bool HandleInput(GameTime time)
        {
            if (IsThumbVisible)
            {
                if (DidPressInside(MouseButtons.Left))
                {
                    var mouse = InputState.MousePoint - VisibleBounds.Location;
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

                if (VisibleBounds.Contains(InputState.MousePoint) && InputState.HasScrolled())
                {
                    ContentPosition -= InputState.ScrollDelta();
                    return false;
                }

                //todo up/down + pgup/pgdn
            }
            return base.HandleInput(time);
        }

        protected override void OnPress(ClickEventArgs args)
        {
            var thumb = GetThumbBounds();
            if (!thumb.Contains(args.position))
            {
                //todo: maybe scroll to mouse over time

                //center thumb around mouse
                if (Direction == Direction.Vertical)
                    ContentPosition = (int)((args.position.Y - ThumbMargin - GetThumbSize() / 2) * ((float)ContentSize / GetContainerSize()));
                else if (Direction == Direction.Horizontal)
                    ContentPosition = (int)((args.position.X - ThumbMargin - GetThumbSize() / 2) * ((float)ContentSize / GetContainerSize()));
            }
            didPressThumb = true;
        }

        protected int GetContainerSize()
        {
            //todo: precalculate
            switch (Direction)
            {
                case Direction.Vertical:
                    return (int)(Size.Y - ThumbMargin * 2);
                case Direction.Horizontal:
                    return (int)(Size.X - ThumbMargin * 2);
                default:
                    return 1;
            }
        }

        protected int GetThumbSize()
        {
            //todo: precalculate
            switch (Direction)
            {
                case Direction.Vertical:
                    return (int)((Size.Y / ContentSize) * GetContainerSize());
                case Direction.Horizontal:
                    return (int)((Size.X / ContentSize) * GetContainerSize());
                default:
                    return 1;
            }
        }

        protected int GetThumbOffset()
        {
            int containerSize = GetContainerSize();
            int size = GetThumbSize();

            return (int)ThumbMargin + Util.Clamp((int)((ContentPosition / (float)ContentSize) * containerSize), 0, containerSize - size);
        }

        protected Rectangle GetThumbBounds()
        {
            switch (Direction)
            {
                case Direction.Vertical:
                    return new Rectangle((int)ThumbMargin, GetThumbOffset(), (int)(Size.X - ThumbMargin * 2), GetThumbSize());
                case Direction.Horizontal:
                    return new Rectangle(GetThumbOffset(), (int)ThumbMargin, GetThumbSize(), (int)(Size.Y - ThumbMargin * 2));
                default:
                    return Rectangle.Empty;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var bounds = VisibleBounds;
            Graphics.Primitives2D.DrawRect(spriteBatch, BorderColor, bounds);

            if (IsThumbVisible)
            {
                var thumb = GetThumbBounds();
                thumb.Offset(VisibleBounds.Location);
                Graphics.Primitives2D.DrawFill(spriteBatch, BorderColor, thumb);
            }
        }
    }

    //todo: convert scroll bars to use enabled/disabled
    public class ScrollBox : Static
    {
        protected ScrollBar verticalScrollbar = new ScrollBar()
        {
            HorizontalAlignment = Alignment.End,
            Size = new Vector2(20, 1),
        };
        protected ScrollBar horizontalScrollbar = new ScrollBar()
        {
            Direction = Direction.Horizontal,
            VerticalAlignment = Alignment.End,
            Size = new Vector2(1, 20),
        };
        protected Static contentArea = new Static();

        public ScrollBox()
        {
            AddChildren(horizontalScrollbar, verticalScrollbar, contentArea);

            horizontalScrollbar.Scroll += delegate (object sender, ScrollEventArgs e)
            {
                foreach (var child in contentArea.Children)
                {
                    if (child.IsEnabled &&
                        child != horizontalScrollbar &&
                        child != verticalScrollbar)
                        child.Position -= new Vector2(e.Delta, 0);
                }
                Reflow();
            };
            verticalScrollbar.Scroll += delegate (object sender, ScrollEventArgs e)
            {
                foreach (var child in contentArea.Children)
                {
                    if (child.IsEnabled &&
                        child != horizontalScrollbar &&
                        child != verticalScrollbar)
                        child.Position -= new Vector2(0, e.Delta);
                }
                Reflow();
            };
        }

        //todo: unified method between all add modes?
        public override Static AddChild(Static child)
        {
            var added = contentArea.AddChild(child);
            ResizeContentArea();
            return added;
        }

        public override void AddChildren(IEnumerable<Static> children)
        {
            base.AddChildren(children);
            ResizeContentArea();
        }

        protected override void OnResize(EventArgs e)
        {
            ResizeContentArea();
            base.OnResize(e);
        }

        protected void ResizeContentArea()
        {
            //todo:
            // maybe use contentContainer and contentArea
            // autosize contentArea and move inside contentContainer
            // does not disrupt positions of elements inside area

            var bounds = Rectangle.Empty;
            foreach (var child in contentArea.Children)
            {
                if (child.IsEnabled)
                    bounds = Rectangle.Union(bounds, child.Bounds);
            }

            horizontalScrollbar.ContentSize = bounds.Width;
            verticalScrollbar.ContentSize = bounds.Height;

            var hsize = Size.X;
            var vsize = Size.Y;

            if (horizontalScrollbar.ContentSize > hsize)
                vsize -= horizontalScrollbar.Size.Y;
            if (verticalScrollbar.ContentSize > vsize)
                hsize -= verticalScrollbar.Size.X;
            if (horizontalScrollbar.ContentSize > hsize)
                vsize -= horizontalScrollbar.Size.Y;

            horizontalScrollbar.Size = new Vector2(
                hsize,
                horizontalScrollbar.Size.Y
            );

            verticalScrollbar.Size = new Vector2(
                verticalScrollbar.Size.X,
                vsize
            );

            if (horizontalScrollbar.IsThumbVisible)
                base.AddChild(horizontalScrollbar);
            else
                base.RemoveChild(horizontalScrollbar);

            if (verticalScrollbar.IsThumbVisible)
                base.AddChild(verticalScrollbar);
            else
                base.RemoveChild(verticalScrollbar);

            contentArea.Size = new Vector2(hsize, vsize);
        }

        public override void DerivedDeserialize(Dictionary<string, object> props)
        {
            base.DerivedDeserialize(props);
            ResizeContentArea();
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.HasScrolled() && VisibleBounds.Contains(InputState.MousePoint))
            {
                if (InputState.IsMod(KeyMod.Shift))
                    horizontalScrollbar.ContentPosition -= InputState.ScrollDelta();
                else
                    verticalScrollbar.ContentPosition -= InputState.ScrollDelta();
                return false;
            }

            return base.HandleInput(time);
        }
    }
}
