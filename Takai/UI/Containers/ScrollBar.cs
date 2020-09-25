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
        public static float DefaultSize = 20;

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
        /// Sprite to draw over the drag thumb for the scrollbar
        /// </summary>
        public Graphics.NinePatch ThumbSprite { get; set; }

        /// <summary>
        /// Where the content is scrolled to
        /// </summary>
        public float ContentPosition
        {
            get => _contentPosition;
            set
            {
                //use helper methods
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

        protected bool didPressThumb = false;

        public override bool CanFocus => IsThumbVisible;

        public float ThumbSize { get; private set; }

        public float ThumbPosition { get; private set; }

        public bool AtBeginning()
        {
            return ContentPosition == 0;
        }

        public bool AtEnd()
        {
            //todo: use helper methods
            var size = Direction == Direction.Horizontal ? ContentArea.Width : ContentArea.Height;
            return ContentPosition >= ContentSize - size;
        }

        public ScrollBar()
        {
            BorderColor = Color;
            On(PressEvent, OnPress);
            On(DragEvent, OnDrag);
            On(ClickEvent, (Static Sender, UIEventArgs e) => UIEventResult.Handled);
        }

        public override void ApplyStyles(Dictionary<string, object> styleRules)
        {
            base.ApplyStyles(styleRules);
            ThumbSprite = GetStyleRule(styleRules, "ThumbSprite", ThumbSprite);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            if (Direction == Direction.Horizontal)
                return new Vector2(float.IsInfinity(availableSize.X) ? DefaultSize : availableSize.X, DefaultSize);
            else
                return new Vector2(DefaultSize, float.IsInfinity(availableSize.Y) ? DefaultSize : availableSize.Y);
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            var containerSize = GetContainerSize();
            ThumbSize = (containerSize / ContentSize) * containerSize;
            ThumbPosition = Util.Clamp((ContentPosition / ContentSize) * containerSize, 0, containerSize - ThumbSize);
            base.ArrangeOverride(availableSize);
        }

        public bool Scroll(int direction)
        {
            var cpos = ContentPosition;
            ContentPosition -= Math.Sign(direction) * (Font?.GetLineHeight(TextStyle) ?? 30);
            return (cpos != ContentPosition);
        }

        static UIEventResult OnPress(Static sender, UIEventArgs e)
        {
            //todo: compare against absolute bounds not dimensions

            var pea = (PointerEventArgs)e;
            var self = (ScrollBar)sender;

            if (pea.device == DeviceType.Keyboard) //todo
            {
                if (pea.button == (int)Microsoft.Xna.Framework.Input.Keys.Up)
                {
                    self.Scroll(-1);
                    return UIEventResult.Handled;
                }
                if (pea.button == (int)Microsoft.Xna.Framework.Input.Keys.Down)
                {
                    self.Scroll(1);
                    return UIEventResult.Handled;
                }
            }

            var thumb = self.GetThumbBounds();
            if (!thumb.Contains(pea.position))
            {
                //todo: maybe scroll to mouse over time

                //center thumb around mouse
                if (self.Direction == Direction.Vertical)
                    self.ContentPosition = (int)((pea.position.Y - self.ThumbSize / 2) * (self.ContentSize / self.GetContainerSize()));
                else if (self.Direction == Direction.Horizontal)
                    self.ContentPosition = (int)((pea.position.X - self.ThumbSize / 2) * (self.ContentSize / self.GetContainerSize()));
            }
            self.didPressThumb = true;
            return UIEventResult.Handled;
        }

        static UIEventResult OnDrag(Static sender, UIEventArgs e)
        {
            var dea = (DragEventArgs)e;
            var self = (ScrollBar)sender;

            if (self.Direction == Direction.Horizontal)
                self.ContentPosition += dea.delta.X;
            else
                self.ContentPosition += dea.delta.Y;

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

        protected Rectangle GetThumbBounds()
        {
            //cache?
            switch (Direction)
            {
                case Direction.Vertical:
                    return new Rectangle(0, (int)ThumbPosition, (int)ContentArea.Width, (int)ThumbSize);
                case Direction.Horizontal:
                    return new Rectangle((int)ThumbPosition, 0, (int)ThumbSize, (int)ContentArea.Height);
                default:
                    return Rectangle.Empty;
            }
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
                            _contentPosition -= 1;// (float)(400 * time.ElapsedGameTime.TotalSeconds);
                            return false;
                        }

                        if (InputState.IsButtonDown(Microsoft.Xna.Framework.Input.Keys.Down))
                        {
                            _contentPosition += 1;//(float)(400 * time.ElapsedGameTime.TotalSeconds);
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

        protected override void DrawSelf(DrawContext context)
        {
            if (!IsThumbVisible)
                return;

            var thumb = GetThumbBounds();
            thumb.Offset(OffsetContentArea.Location);
            thumb = Rectangle.Intersect(VisibleContentArea, thumb);
            Graphics.Primitives2D.DrawFill(context.spriteBatch, Color, thumb);
            ThumbSprite.Draw(context.spriteBatch, thumb);
        }
    }

    //todo: convert scroll bars to use enabled/disabled
    public class ScrollBox : Static
    {
        /// <summary>
        /// An optional style to apply to the scrollbars (write-only)
        /// </summary>
        public ScrollBar ScrollBarUI
        {
            set
            {
                if (value == null)
                    return;

                var hsp = horizontalScrollbar?.ChildIndex ?? -1;
                horizontalScrollbar = (ScrollBar)value.CloneHierarchy();
                horizontalScrollbar.HorizontalAlignment = Alignment.Stretch;
                horizontalScrollbar.Direction = Direction.Horizontal;
                horizontalScrollbar.VerticalAlignment = Alignment.Bottom;

                var vsp = verticalScrollbar?.ChildIndex ?? -1;
                verticalScrollbar = (ScrollBar)value.CloneHierarchy();
                verticalScrollbar.VerticalAlignment = Alignment.Stretch;
                verticalScrollbar.Direction = Direction.Vertical;
                verticalScrollbar.HorizontalAlignment = Alignment.Right;

                if (vsp >= 0)
                    base.InternalSwapChild(verticalScrollbar, vsp, false);
                if (hsp >= 0)
                    base.InternalSwapChild(horizontalScrollbar, hsp, false);

                if (vsp >= 0 || hsp >= 0)
                    InvalidateMeasure();
            }
        }

        public bool EnableHorizontalScrolling { get; set; } = true;
        public bool EnableVerticalScrolling { get; set; } = true;

        protected ScrollBar verticalScrollbar;
        protected ScrollBar horizontalScrollbar;

        public IEnumerable<Static> EnumerableChildren => System.Linq.Enumerable.Skip(Children, 2); //todo: ghetto

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
                horizontalScrollbar.ContentPosition = value.X;
                verticalScrollbar.ContentPosition = value.Y;
            }
        }

        /// <summary>
        /// Padding between the content and scrollbars (X between content and vertical scrollbar, Y between content and horizontal scrollbar)
        /// </summary>
        public Vector2 InnerPadding { get; set; }

        public Vector2 ContentSize { get; private set; }

        public override bool CanFocus => true; //dont draw focus rect

        /// <summary>
        /// when resizing, if the previous scroll position was at the end, stay at the end
        /// </summary>
        public bool StayAtEnd { get; set; } = false;

        /// <summary>
        /// Show the scrollbars (does not affect whether or not can scroll)
        /// </summary>
        public bool ShowScrollbars { get; set; }
#if ANDROID
            = false;
#else
            = true;
#endif

        //velocity?

        public ScrollBox()
        {
            ScrollBarUI = new ScrollBar();

            base.InternalInsertChild(verticalScrollbar, 0, false);
            base.InternalInsertChild(horizontalScrollbar, 1, true);

            On(ScrollBar.HScrollEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (ScrollBox)sender;
                self.InvalidateArrange();
                return UIEventResult.Handled;
            });

            On(ScrollBar.VScrollEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (ScrollBox)sender;
                self.InvalidateArrange();
                return UIEventResult.Handled;
            });

            On(DragEvent, delegate (Static sender, UIEventArgs e)
            {
                var dea = (DragEventArgs)e;
                if ((dea.device == DeviceType.Mouse && dea.button == (int)MouseButtons.Middle) ||
                    dea.device == DeviceType.Touch)
                {
                    //todo: not working in child
                    var hpos = horizontalScrollbar.ContentPosition;
                    var vpos = verticalScrollbar.ContentPosition;
                    horizontalScrollbar.ContentPosition -= dea.delta.X;
                    verticalScrollbar.ContentPosition -= dea.delta.Y;
                    if (hpos != horizontalScrollbar.ContentPosition ||
                        vpos != verticalScrollbar.ContentPosition)
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

        public override void ApplyStyles(Dictionary<string, object> styleRules)
        {
            base.ApplyStyles(styleRules);
            StayAtEnd = GetStyleRule(styleRules, "StayAtEnd", StayAtEnd);
            InnerPadding = GetStyleRule(styleRules, "InnerPadding", InnerPadding);
            ShowScrollbars = GetStyleRule(styleRules, "ShowScrollbars", ShowScrollbars);
        }

        protected override bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
        {
            return base.InternalInsertChild(child, index < 0 ? -1 : index + 2, reflow, ignoreFocus);
        }

        protected override Static InternalSwapChild(Static child, int index, bool reflow = true, bool ignoreFocus = false)
        {
            return base.InternalSwapChild(child, index + 2, reflow, ignoreFocus);
        }

        protected override Static InternalRemoveChild(int index, bool reflow = true)
        {
            return base.InternalRemoveChild(index + 2, reflow);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var hs = horizontalScrollbar.Measure(InfiniteSize);
            var vs = verticalScrollbar.Measure(InfiniteSize);

            var bounds = new Rectangle();
            for (int i = 2; i < Children.Count; ++i)
            {
                //todo: may need to double measure with scroll bars once, then again without if within container

                var cm = Children[i].Measure(availableSize - new Vector2(vs.X, hs.Y));
                bounds = Rectangle.Union(bounds, new Rectangle(0, 0, (int)cm.X, (int)cm.Y));
            }
            ContentSize = new Vector2(bounds.Width + 1, bounds.Height + 1);
            return ContentSize;
        }

        protected override void OnChildRemeasure(Static child)
        {
            InvalidateMeasure();
            InvalidateArrange();
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            var newPosition = ScrollPosition;
            if (StayAtEnd && newPosition != Vector2.Zero)
            {
                if (horizontalScrollbar.AtEnd())
                    newPosition.X = horizontalScrollbar.ContentSize;
                if (verticalScrollbar.AtEnd())
                    newPosition.Y = verticalScrollbar.ContentSize;
            }

            horizontalScrollbar.ContentSize = ContentSize.X + InnerPadding.X * 2;
            verticalScrollbar.ContentSize = ContentSize.Y + InnerPadding.Y * 2;

            ScrollPosition = newPosition;

            bool canHScroll = EnableHorizontalScrolling && ContentSize.X > availableSize.X - InnerPadding.X;
            bool canVScroll = EnableVerticalScrolling && ContentSize.Y > availableSize.Y - InnerPadding.Y;
            horizontalScrollbar.IsEnabled = ShowScrollbars && canHScroll;
            verticalScrollbar.IsEnabled = ShowScrollbars && canVScroll;

            var hs = horizontalScrollbar.IsEnabled ? new Vector2(0, horizontalScrollbar.MeasuredSize.Y) : Vector2.Zero;
            var vs = verticalScrollbar.IsEnabled ? new Vector2(verticalScrollbar.MeasuredSize.X, 0) : Vector2.Zero;

            horizontalScrollbar.Arrange(new Rectangle(0, (int)(availableSize.Y - hs.Y), (int)(availableSize.X - vs.X), (int)hs.Y));
            verticalScrollbar.Arrange(new Rectangle((int)(availableSize.X - vs.X), 0, (int)vs.X, (int)(availableSize.Y - hs.Y)));

            var scrollX = (int)(-ScrollPosition.X + InnerPadding.X);
            var scrollY = (int)(-ScrollPosition.Y + InnerPadding.Y);
            var arrangeRect = new Rectangle(
                scrollX,
                scrollY,
                (int)(availableSize.X - vs.X - InnerPadding.X - 1) - scrollX,
                (int)(availableSize.Y - hs.Y - InnerPadding.Y - 1) - scrollY
            );
            for (int i = 2; i < Children.Count; ++i)
                Children[i].Arrange(arrangeRect);
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.HasScrolled() && VisibleContentArea.Contains(InputState.MousePoint))
            {
                if (InputState.IsMod(KeyMod.Shift))
                {
                    if (horizontalScrollbar.Scroll(InputState.ScrollDelta()))
                        return false;
                }
                else
                {
                    if (verticalScrollbar.Scroll(InputState.ScrollDelta()))
                        return false;
                }
            }

            return base.HandleInput(time);
        }
    }
}
