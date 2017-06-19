﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace Takai.UI
{
    public class ScrollEventArgs : System.EventArgs
    {
        public int Delta { get; set; }

        public ScrollEventArgs(int delta)
        {
            Delta = delta;
        }
    }

    public class ScrollBar : Static
    {
        static int ThumbMargin = 3; //todo: apply correctly

        /// <summary>
        /// The size of the content. This determines how big the scroll thumb is
        /// </summary>
        public int ContentSize
        {
            get => contentSize;
            set
            {
                contentSize = MathHelper.Max(value, 0);
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
                if (Direction == Direction.Vertical)
                    newPosition = MathHelper.Clamp(value, 0, ContentSize - (int)Size.Y);
                else if (Direction == Direction.Horizontal)
                    newPosition = MathHelper.Clamp(value, 0, ContentSize - (int)Size.X);

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

        public override bool CanFocus => true;

        public event System.EventHandler<ScrollEventArgs> Scroll;
        protected virtual void OnScroll(ScrollEventArgs e) { }

        public ScrollBar()
        {
            BorderColor = Color;
        }

        protected override bool HandleInput(GameTime time)
        {
            if (IsThumbVisible)
            {
                if (DidPressInside())
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
                    return (int)Size.Y - (ThumbMargin * 2);
                case Direction.Horizontal:
                    return (int)Size.X - (ThumbMargin * 2);
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

            return ThumbMargin + MathHelper.Clamp((int)((ContentPosition / (float)ContentSize) * containerSize), 0, containerSize - size);
        }

        protected Rectangle GetThumbBounds()
        {
            switch (Direction)
            {
                case Direction.Vertical:
                    return new Rectangle(ThumbMargin, GetThumbOffset(), (int)Size.X - (ThumbMargin * 2), GetThumbSize());
                case Direction.Horizontal:
                    return new Rectangle(GetThumbOffset(), ThumbMargin, GetThumbSize(), (int)Size.Y - (ThumbMargin * 2));
                default:
                    return Rectangle.Empty;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var bounds = VisibleBounds;
            Takai.Graphics.Primitives2D.DrawRect(spriteBatch, BorderColor, bounds);

            if (IsThumbVisible)
            {
                var thumb = GetThumbBounds();
                thumb.Offset(VisibleBounds.Location);
                Takai.Graphics.Primitives2D.DrawFill(spriteBatch, BorderColor, thumb);
            }
        }
    }

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
                    if (child != horizontalScrollbar &&
                        child != verticalScrollbar)
                        child.Position -= new Vector2(e.Delta, 0);
                }
                Reflow();
            };
            verticalScrollbar.Scroll += delegate (object sender, ScrollEventArgs e)
            {
                foreach (var child in contentArea.Children)
                {
                    if (child != horizontalScrollbar &&
                        child != verticalScrollbar)
                        child.Position -= new Vector2(0, e.Delta);
                }
                Reflow();
            };
        }

        //todo: virtualize
        public new void AddChild(Static child)
        {
            contentArea.AddChild(child);
        }

        public override void Reflow()
        {
            //todo:
            // maybe use contentContainer and contentArea
            // autosize contentArea and move inside contentContainer
            // does not disrupt positions of elements inside area

            var bounds = Rectangle.Empty;
            foreach (var child in contentArea.Children)
                bounds = Rectangle.Union(bounds, child.Bounds);

            horizontalScrollbar.ContentSize = bounds.Width;
            verticalScrollbar.ContentSize = bounds.Height;
            base.Reflow();
        }

        protected override void OnResize(EventArgs e)
        {
            horizontalScrollbar.Size = new Vector2(
                Size.X - verticalScrollbar.Size.X,
                horizontalScrollbar.Size.Y
            );

            verticalScrollbar.Size = new Vector2(
                verticalScrollbar.Size.X,
                Size.Y - horizontalScrollbar.Size.Y
            );

            contentArea.Size = Size - new Vector2(verticalScrollbar.Size.X, horizontalScrollbar.Size.Y);
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
