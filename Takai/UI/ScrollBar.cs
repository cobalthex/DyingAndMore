using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace Takai.UI
{
    public class ScrollEventArgs : UIEventArgs
    {
        public float Delta { get; set; }

        public ScrollEventArgs(Static source, float delta)
            : base(source)
        {
            Delta = delta;
        }
    }

    public class ScrollBar : Static
    {
        public const string HScrollEvent = "HScroll";
        public const string VScrollEvent = "VScroll";

        /// <summary>
        /// The size of the content. This determines how big the scroll thumb is
        /// </summary>
        public float ContentSize
        {
            get => contentSize;
            set
            {
                contentSize = Math.Max(value, 0);
                ContentPosition = _contentPosition;
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
            get => _contentPosition;
            set
            {
                var size = Direction == Direction.Horizontal ? ContentArea.Width : ContentArea.Height;

                float newPosition;
                if (size > contentSize)
                    newPosition = 0;
                else
                    newPosition = Util.Clamp(value, 0, ContentSize - size);

                if (newPosition != _contentPosition)
                {
                    var e = new ScrollEventArgs(this, newPosition - _contentPosition);
                    _contentPosition = newPosition;
                    BubbleEvent(Direction == Direction.Horizontal ? HScrollEvent : VScrollEvent, e);
                }
            }
        }
        private float _contentPosition = 0;

        /// <summary>
        /// Which direction the scrollbar moves
        /// </summary>
        public Direction Direction { get; set; } = Direction.Vertical;

        public Color ThumbColor { get; set; } = Color.White;

        protected bool didPressThumb = false;

        public override bool CanFocus => IsThumbVisible;

        public ScrollBar()
        {
            BorderColor = ThumbColor;
            On(PressEvent, OnPress);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            if (Direction == Direction.Horizontal)
                return new Vector2(float.IsInfinity(availableSize.X) ? 20 : availableSize.X, 20);
            else
                return new Vector2(20, float.IsInfinity(availableSize.Y) ? 20 : availableSize.Y);
        }

        protected override bool HandleInput(GameTime time)
        {
            //todo: should bubble focus

            if (IsThumbVisible && HasFocus)
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
                    Scroll(InputState.ScrollDelta());
                    return false;
                }

                if (IsThumbVisible)
                {
                    if (Direction == Direction.Vertical)
                    {
                        if (InputState.IsButtonDown(Microsoft.Xna.Framework.Input.Keys.Up))
                        {
                            _contentPosition -= (float)(400 * time.ElapsedGameTime.TotalSeconds);
                            return false;
                        }

                        if (InputState.IsButtonDown(Microsoft.Xna.Framework.Input.Keys.Down))
                        {
                            _contentPosition += (float)(400 * time.ElapsedGameTime.TotalSeconds);
                            return false;
                        }
                    }

                    else if (Direction == Direction.Horizontal)
                    {
                        if (InputState.IsButtonDown(Microsoft.Xna.Framework.Input.Keys.Left))
                        {
                            _contentPosition -= (float)(400 * time.ElapsedGameTime.TotalSeconds);
                            return false;
                        }

                        if (InputState.IsButtonDown(Microsoft.Xna.Framework.Input.Keys.Right))
                        {
                            _contentPosition += (float)(400 * time.ElapsedGameTime.TotalSeconds);
                            return false;
                        }
                    }
                    else if (InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.PageUp))
                    {
                        ContentPosition -= GetContainerSize() / 2;
                        return false;
                    }
                    else if (InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.PageDown))
                    {
                        ContentPosition += GetContainerSize() / 2;
                        return false;
                    }
                }
            }
            return base.HandleInput(time);
        }

        public void Scroll(int direction)
        {
            ContentPosition -= Math.Sign(direction) * (Font != null ? Font.MaxCharHeight : 20);
        }

        static UIEventResult OnPress(Static sender, UIEventArgs e)
        {
            //todo: compare against absolute bounds not dimensions

            var pe = (PointerEventArgs)e;
            var sbar = (ScrollBar)sender;

            var thumb = sbar.GetThumbBounds();
            if (!thumb.Contains(pe.position))
            {
                //todo: maybe scroll to mouse over time

                //center thumb around mouse
                if (sbar.Direction == Direction.Vertical)
                    sbar.ContentPosition = (int)((pe.position.Y - sbar.GetThumbSize() / 2) * (sbar.ContentSize / sbar.GetContainerSize()));
                else if (sbar.Direction == Direction.Horizontal)
                    sbar.ContentPosition = (int)((pe.position.X - sbar.GetThumbSize() / 2) * (sbar.ContentSize / sbar.GetContainerSize()));
            }
            sbar.didPressThumb = true;
            return UIEventResult.Handled;
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
    public class ScrollBox : Static
    {
        /// <summary>
        /// An optional style to apply to the scrollbars (write-only)
        /// </summary>
        public ScrollBar ScrollBarTemplate
        {
            set
            {
                if (value == null)
                    return;

                var hsp = horizontalScrollbar?.ChildIndex ?? -1;
                horizontalScrollbar = (ScrollBar)value.CloneHierarchy();
                horizontalScrollbar.HorizontalAlignment = Alignment.Stretch;
                horizontalScrollbar.Direction = Direction.Horizontal;

                var vsp = verticalScrollbar?.ChildIndex ?? -1;
                verticalScrollbar = (ScrollBar)value.CloneHierarchy();
                verticalScrollbar.VerticalAlignment = Alignment.Stretch;
                verticalScrollbar.Direction = Direction.Vertical;

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
                    InvalidateMeasure();
            }
        }

        public bool EnableHorizontalScrolling { get; set; } = true;
        public bool EnableVerticalScrolling { get; set; } = true;

        protected ScrollBar verticalScrollbar;
        protected ScrollBar horizontalScrollbar;

        public IEnumerable<Static> EnumerableChildren
        {
            get
            {
                for (int i = 2; i < Children.Count; ++i)
                    yield return Children[i];
            }
        }

        /// <summary>
        /// The curently scrolled position of this scrollbox.
        /// X = horizontal, Y = vertical
        /// </summary>
        [Data.Serializer.Ignored]
        public Vector2 ScrollPosition
        {
            get => new Vector2(horizontalScrollbar.ContentPosition, verticalScrollbar.ContentPosition);
            set
            {
                //InternalSetScrollPosition and reflow after?
                verticalScrollbar.ContentPosition = value.Y;
                horizontalScrollbar.ContentPosition = value.X;
            }
        }

        public override bool CanFocus => true;

        public ScrollBox()
        {
            ScrollBarTemplate = new ScrollBar();

            base.InternalInsertChild(verticalScrollbar, 0, false);
            base.InternalInsertChild(horizontalScrollbar, 1, true);

            On(ScrollBar.HScrollEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (ScrollBox)sender;
                var se = (ScrollEventArgs)e;
                var lastPos = ScrollPosition;
                var newPos = lastPos - new Vector2(se.Delta, 0);
                if (lastPos != newPos)
                {
                    ScrollPosition = newPos;
                    return UIEventResult.Handled;
                }
                return UIEventResult.Continue;
            });

            On(ScrollBar.VScrollEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (ScrollBox)sender;
                var se = (ScrollEventArgs)e;
                var lastPos = ScrollPosition;
                var newPos = lastPos - new Vector2(se.Delta, 0);
                if (lastPos != newPos)
                {
                    ScrollPosition = newPos;
                    return UIEventResult.Handled;
                }
                return UIEventResult.Continue;
            });
        }

        public ScrollBox(params Static[] children)
            : this()
        {
            AddChildren(children);

            //todo: correctly serialize
        }

        protected override void FinalizeClone()
        {
            verticalScrollbar = (ScrollBar)Children[0];
            horizontalScrollbar = (ScrollBar)Children[1];
        }

        public override bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
        {
            return base.InternalInsertChild(child, index >= 0 ? index + 2 : -1, reflow, ignoreFocus);
        }

        public override bool InternalRemoveChildIndex(int index, bool reflow = true)
        {
            return base.InternalRemoveChildIndex(index + 2, reflow);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            verticalScrollbar.Measure(new Vector2(InfiniteSize));
            horizontalScrollbar.Measure(new Vector2(InfiniteSize));

            var bounds = Rectangle.Union(
                new Rectangle(0, 0, (int)textSize.X, (int)textSize.Y),
                DefaultMeasureSomeChildren(availableSize, 2, Children.Count)
            );
            return new Vector2(bounds.Width, bounds.Height);
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            verticalScrollbar.ContentSize = MeasuredSize.Y;
            horizontalScrollbar.ContentSize = MeasuredSize.X;

            verticalScrollbar.IsEnabled = EnableVerticalScrolling && MeasuredSize.Y > availableSize.Y - horizontalScrollbar.MeasuredSize.Y;
            horizontalScrollbar.IsEnabled = EnableHorizontalScrolling && MeasuredSize.X > availableSize.X - verticalScrollbar.MeasuredSize.X;

            var hs = horizontalScrollbar.IsEnabled ? horizontalScrollbar.MeasuredSize : Vector2.Zero;
            var vs = verticalScrollbar.IsEnabled ? verticalScrollbar.MeasuredSize : Vector2.Zero;

            verticalScrollbar.Arrange(new Rectangle((int)(availableSize.X - vs.X), 0, (int)vs.X, (int)(availableSize.Y - hs.Y)));
            horizontalScrollbar.Arrange(new Rectangle(0, (int)(availableSize.Y - hs.Y), (int)(availableSize.X - vs.X), (int)hs.Y));

            var container = new Rectangle(
                (int)horizontalScrollbar.ContentPosition,
                (int)verticalScrollbar.ContentPosition,
                (int)(availableSize.X - vs.X),
                (int)(availableSize.Y - hs.Y)
            );
            for (int i = 2; i < Children.Count; ++i)
            {
                if (Children[i].IsEnabled)
                    Children[i].Arrange(container);
            }
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.HasScrolled() && VisibleContentArea.Contains(InputState.MousePoint))
            {
                if (InputState.IsMod(KeyMod.Shift))
                {
                    if (horizontalScrollbar.IsEnabled)
                    {
                        horizontalScrollbar.Scroll(InputState.ScrollDelta());
                        return false;
                    }
                }
                else if (verticalScrollbar.IsEnabled)
                {
                    verticalScrollbar.Scroll(InputState.ScrollDelta());
                    return false;
                }
            }

            if (HasFocus)
            {
                //todo: refer scroll input to horizontal/vertical
                return false;
            }

            return base.HandleInput(time);
        }
    }
}
