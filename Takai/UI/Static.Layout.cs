using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.UI
{
    public partial class Static
    {
        /// <summary>
        /// Marks a size to automatically expand to fit its contents
        /// </summary>
        public static readonly Vector2 AutoSize = new Vector2(float.NaN);
        public static readonly Vector2 InfiniteSize = new Vector2(float.PositiveInfinity);

        /// <summary>
        /// The last size returned by this element's <see cref="Measure"/>
        /// This includes padding and position
        /// </summary>
        [Data.DebugSerialize]
        public Vector2 MeasuredSize { get; private set; }

        /// <summary>
        /// The bounds of the content area, as determined by <see cref="PerformReflows"/>
        /// </summary>
        [Data.DebugSerialize]
        public Rectangle ContentArea { get; private set; }

        /// <summary>
        /// The <see cref="ContentArea"/> of this element offset relative to the screen
        /// </summary>
        [Data.DebugSerialize]
        public Rectangle OffsetContentArea { get; private set; }

        /// <summary>
        /// The visible region of this element on the screen, excluding padding
        /// </summary>
        [Data.DebugSerialize]
        public Rectangle VisibleContentArea { get; private set; }

        /// <summary>
        /// The visible region of this element, including padding
        /// </summary>
        [Data.DebugSerialize]
        public Rectangle VisibleBounds { get; private set; }

        public Point VisibleOffset => new Point(VisibleContentArea.X - OffsetContentArea.X, VisibleContentArea.Y - OffsetContentArea.Y);

        //how many measures/arranges are currently queued
        //if 0, measure/arrange valid
        //if 1, next measure/arrange will act accordingly
        //if >1, skipped
        //this (hopefully) ensures that child measures/invalids happen in correct order
        private bool isMeasureValid = true;
        private bool isArrangeValid = true;

        private Vector2 lastMeasureAvailableSize = InfiniteSize;
        private Rectangle lastMeasureContainerBounds = Rectangle.Empty;

        /// <summary>
        /// Invalidat the size/measurement of this element.
        /// <see cref="Measure(Vector2)"/> will be called on this element at some point in the future.
        /// Typically called when an element is resized.
        /// </summary>
        public void InvalidateMeasure()
        {
            if (isMeasureValid)
            {
                isMeasureValid = false;
                measureQueue.Add(this);
            }
        }

        /// <summary>
        /// Invalidate the arrangement of this element.
        /// <see cref="Arrange(Rectangle)"/> will be called on this element at some point in the future.
        /// Typically called when an element is moved or was resized previously.
        /// </summary>
        public void InvalidateArrange()
        {
            if (isArrangeValid)
            {
                isArrangeValid = false;
                arrangeQueue.Add(this);
            }
        }
        /// <summary>
        /// force the entire tree to be remeasured and relayed-out
        /// </summary>
        public void DebugInvalidateTree()
        {
            foreach (var element in EnumerateRecursive())
            {
                element.lastMeasureContainerBounds = Rectangle.Empty;
                element.lastMeasureAvailableSize = InfiniteSize;
                element.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Calculate the desired containing region of this element and its children. Can be customized through <see cref="MeasureOverride"/>.
        /// Sets <see cref="MeasuredSize"/> to value calculated
        /// This returns a size that is large enough to include the offset element with padding
        /// </summary>
        /// <returns>The desired size of this element, including padding</returns>
        public Vector2 Measure(Vector2 availableSize)
        {
            if (isMeasureValid && availableSize == lastMeasureAvailableSize)
                return MeasuredSize;
#if DEBUG
            ++totalMeasureCount;
#endif
            lastMeasureAvailableSize = availableSize;

            //Diagnostics.Debug.WriteLine($"Measuring ID:{DebugId} (available size:{availableSize})");

            var size = Size;
            bool isWidthAutoSize = float.IsNaN(size.X);
            bool isHeightAutoSize = float.IsNaN(size.Y);
            bool isHStretch = HorizontalAlignment == Alignment.Stretch;
            bool isVStretch = VerticalAlignment == Alignment.Stretch;

            if (availableSize.X < InfiniteSize.X)
            {
                availableSize.X -= Padding.X * 2;
                if (isWidthAutoSize && isHStretch)
                    availableSize.X -= Position.X;
            }
            else if (!float.IsNaN(size.X))
                availableSize.X = size.X;

            if (availableSize.Y < InfiniteSize.Y)
            {
                availableSize.Y -= Padding.Y * 2;
                if (isHeightAutoSize && isVStretch)
                    availableSize.Y -= Position.Y;
            }
            else if (!float.IsNaN(size.Y))
                availableSize.Y = size.Y;

            var measuredSize = MeasureOverride(availableSize);
            if (isWidthAutoSize || isHeightAutoSize)
            {
                if (float.IsInfinity(measuredSize.X) || float.IsNaN(measuredSize.X)
                 || float.IsInfinity(measuredSize.Y) || float.IsNaN(measuredSize.Y))
                    throw new /*NotFiniteNumberException*/InvalidOperationException("Measured size cannot be NaN or infinity");

                if (isWidthAutoSize)
                    size.X = measuredSize.X; //stretched items do have intrinsic size
                                             //size.X = isHStretch ? 0 : measuredSize.X; //stretched items have no intrinsic size

                if (isHeightAutoSize)
                    size.Y = measuredSize.Y; //stretched items do have intrinsic size
                                             //size.Y = isVStretch ? 0 : measuredSize.Y; //stretched items have no intrinsic size
            }

            var lastMeasuredSize = MeasuredSize;
            MeasuredSize = Position + size + Padding * 2;

            isMeasureValid = true;
            if (MeasuredSize != lastMeasuredSize)
            {
                InvalidateArrange();
                NotifyParentMeasuredSizeChanged();
            }

            return MeasuredSize;
        }

        void NotifyParentMeasuredSizeChanged()
        {
            if (Parent == null)
                return;

            Parent.OnChildRemeasure(this);
        }

        /// <summary>
        /// Called whenever a child of this element resizes.
        /// By default, will remeasure if the child is auto-sized
        /// </summary>
        /// <param name="child">The child that was remeasured</param>
        protected virtual void OnChildRemeasure(Static child)
        {
            if (IsAutoSized)
                //&& (HorizontalAlignment != Alignment.Stretch && VerticalAlignment != Alignment.Stretch)) broken; should be able to handle scrollboxes
                InvalidateMeasure();
            else
                InvalidateArrange();
        }

        /// <summary>
        /// Measure the preferred size of this object.
        /// May be overriden to provide custom sizing (this should not include <see cref="Size"/> or <see cref="Padding"/>)
        /// By default calculates the shrink-wrapped size
        /// Measurements to children should call <see cref="Measure"/>
        /// </summary>
        /// <param name="availableSize">This is the available size of the container,. The returned size can be larger or smaller</param>
        /// <returns>The preferred size of this element</returns>
        protected virtual Vector2 MeasureOverride(Vector2 availableSize)
        {
            var textSize = new Point();
            if (Font != null && Text != null)
                textSize = Vector2.Ceiling(Font.MeasureString(Text, TextStyle)).ToPoint();

            var bounds = Rectangle.Union(
                new Rectangle(0, 0, textSize.X, textSize.Y),
                DefaultMeasureSomeChildren(availableSize, 0, Children.Count)
            );
            return new Vector2(bounds.Width, bounds.Height);
        }

        /// <summary>
        /// Get the clip bounds for a selection of (enabled) child elements
        /// </summary>
        /// <param name="startIndex">The first child to measure</param>
        /// <param name="count">The number of children to measure (including disabled)</param>
        /// <returns>The measured area</returns>
        protected Rectangle DefaultMeasureSomeChildren(Vector2 availableSize, int startIndex, int count)
        {
            var bounds = new Rectangle();
            for (int i = startIndex; i < Math.Min(Children.Count, startIndex + count); ++i)
            {
                if (!Children[i].IsEnabled)
                    continue;

                var mes = Children[i].Measure(availableSize).ToPoint();
                bounds = Rectangle.Union(bounds, new Rectangle(
                    0, //measured size includes offset
                    0, //ditto
                    mes.X,
                    mes.Y
                ));
            }
            return bounds;
        }

#if DEBUG
        private static uint totalMeasureCount = 0; //set breakpoint for speicifc reflow
        private static uint totalArrangeCount = 0; //set breakpoint for speicifc reflow
        private static TimeSpan lastUpdateDuration;
        private static TimeSpan lastDrawDuration;
        private static System.Diagnostics.Stopwatch boop = new System.Diagnostics.Stopwatch();//todo: should be one for update and one for draw
#endif

        /// <summary>
        /// Reflow this container, relative to its parent
        /// </summary>
        /// <param name="container">Container in relative coordinates</param>
        public void Arrange(Rectangle container)
        {
            isArrangeValid = true;
#if DEBUG
            ++totalArrangeCount;
#endif
            //todo: this needs to be called less

            //Diagnostics.Debug.WriteLine($"Arranging ID:{DebugId} ({this}) [container:{container}]");
            AdjustToContainer(container);
            ArrangeOverride(ContentArea.Size.ToVector2()); //todo: this needs to be visibleDimensions (?)
        }

        /// <summary>
        /// Reflow child elements relative to this element
        /// Called whenever this element's position or size is adjusted
        /// </summary>
        protected virtual void ArrangeOverride(Vector2 availableSize)
        {
            foreach (var child in Children)
            {
                if (child.IsEnabled)
                    // child.Arrange(new Rectangle(0, 0, (int)availableSize.X, (int)availableSize.Y));
                    child.Arrange(new Rectangle(child.lastMeasureContainerBounds.Location, availableSize.ToPoint())); //?
            }
        }

        /// <summary>
        /// Calculate all of the bounds to this element in relation to a container.
        /// </summary>
        /// <param name="container">The container to fit this to, in relative coordinates</param>
        private void AdjustToContainer(Rectangle container)
        {
            lastMeasureContainerBounds = container;

            Rectangle parentContentArea;
            var offsetParent = Point.Zero;
            if (Parent == null)
            {
                var viewport = Runtime.GraphicsDevice.Viewport.Bounds;
                viewport.X = 0;
                viewport.Y = 0;
                container = parentContentArea = Rectangle.Intersect(container, viewport);
            }
            else
            {
                offsetParent = Parent.OffsetContentArea.Location;
                parentContentArea = Parent.VisibleContentArea;
            }

            var finalSize = MeasuredSize;

            //todo: should this go into Measure?
            if (HorizontalAlignment == Alignment.Stretch)
                finalSize.X = float.IsNaN(Size.X) ? container.Width : Size.X;
            if (VerticalAlignment == Alignment.Stretch)
                finalSize.Y = float.IsNaN(Size.Y) ? container.Height : Size.Y;

            var localPos = new Vector2(
                GetAlignedPosition(HorizontalAlignment, Position.X, finalSize.X, Padding.X, container.Width),
                GetAlignedPosition(VerticalAlignment, Position.Y, finalSize.Y, Padding.Y, container.Height)
            );
            var bounds = new Rectangle((int)localPos.X, (int)localPos.Y, (int)(finalSize.X), (int)(finalSize.Y));

            bounds.Width -= (int)(Padding.X * 2 + Position.X);
            bounds.Height -= (int)(Padding.Y * 2 + Position.Y);
            bounds.Offset(container.Location);
            ContentArea = bounds;

            var tmp = bounds;
            tmp.Offset(offsetParent);
            OffsetContentArea = tmp;

            tmp = Rectangle.Intersect(bounds, container);
            tmp.Offset(offsetParent);
            container.Offset(offsetParent);
            container = Rectangle.Intersect(container, parentContentArea);
            VisibleContentArea = Rectangle.Intersect(tmp, container);

            tmp.Inflate(Padding.X, Padding.Y);
            VisibleBounds = Rectangle.Intersect(tmp, container);
        }

        public float GetAlignedPosition(Alignment alignment, float position, float size, float padding, float containerSize)
        {
            switch (alignment)
            {
                case Alignment.Middle:
                case Alignment.Stretch: // stretched items will either fill full area or center in available space
                    return (containerSize - size + padding * 2) / 2 + position;
                case Alignment.End:
                    return containerSize - size; //size includes padding and position
                default:
                    return position + padding;
            }
        }

        static readonly List<Static> measureQueue = new List<Static>();
        static readonly List<Static> arrangeQueue = new List<Static>();

        /// <summary>
        /// Complete any pending reflows/arranges
        /// </summary>
        public static void PerformReflows()
        {
            for (int i = 0; i < measureQueue.Count; ++i)
            {
                measureQueue[i].Measure(measureQueue[i].lastMeasureAvailableSize);
                if (!measureQueue[i].isMeasureValid)
                    measureQueue[i].Measure(InfiniteSize);
            }
            measureQueue.Clear();
            for (int i = 0; i < arrangeQueue.Count; ++i)
            {
                if (!arrangeQueue[i].isArrangeValid)
                    arrangeQueue[i].Arrange(arrangeQueue[i].lastMeasureContainerBounds);
            }
            arrangeQueue.Clear();
        }
    }
}
