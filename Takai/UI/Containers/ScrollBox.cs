using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai.Input;

namespace Takai.UI
{
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

        protected override void ApplyStyleOverride()
        {
            base.ApplyStyleOverride();

            var style = GenerateStyleSheet<ScrollBoxStyleSheet>();

            if (style.StayAtEnd.HasValue) StayAtEnd = style.StayAtEnd.Value;
            if (style.InnerPadding.HasValue) InnerPadding = style.InnerPadding.Value;
            if (style.ShowScrollbars.HasValue) ShowScrollbars = style.ShowScrollbars.Value;
        }

        public struct ScrollBoxStyleSheet : IStyleSheet<ScrollBoxStyleSheet>
        {
            public string Name { get; set; }

            public bool? StayAtEnd;
            public Vector2? InnerPadding;
            public bool? ShowScrollbars;

            public ScrollBoxStyleSheet LerpWith(ScrollBoxStyleSheet other, float t)
            {
                throw new NotImplementedException();
            }

            public void MergeWith(ScrollBoxStyleSheet other)
            {
                if (other.StayAtEnd.HasValue) StayAtEnd = other.StayAtEnd;
                if (other.InnerPadding.HasValue) InnerPadding = other.InnerPadding;
                if (other.ShowScrollbars.HasValue) ShowScrollbars = other.ShowScrollbars;
            }

            void IStyleSheet<ScrollBoxStyleSheet>.LerpWith(ScrollBoxStyleSheet other, float t)
            {
                throw new NotImplementedException();
            }
        }
    }
}
