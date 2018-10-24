using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public enum Direction
    {
        Horizontal,
        Vertical
    }

    public class List : Container
    {
        /// <summary>
        /// Spacing between items
        /// </summary>
        public float Margin { get; set; } = 0;

        /// <summary>
        /// Which direction should list items flow
        /// </summary>
        public Direction Direction { get; set; } = Direction.Vertical;

        public List() { }
        public List(params Static[] children)
            : base(children) { }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var usedSize = new Vector2();
            foreach (var child in EnumerateChildren())
            {
                if (!child.IsEnabled)
                    continue;

                var childSize = child.Measure(new Vector2(InfiniteSize));
                if (Direction == Direction.Horizontal)
                {
                    if (child.HorizontalAlignment != Alignment.Stretch)
                        usedSize.X += childSize.X; //todo: better format for this
                    usedSize.Y = System.Math.Max(usedSize.Y, childSize.Y);
                }
                else
                {
                    if (child.VerticalAlignment != Alignment.Stretch)
                        usedSize.Y += childSize.Y;
                    usedSize.X = System.Math.Max(usedSize.X, childSize.X);
                }
            }
            if (Direction == Direction.Horizontal)
                usedSize.X += Margin * (InternalChildren.Count - 1);
            else
                usedSize.Y += Margin * (InternalChildren.Count - 1);

            return usedSize;

            //todo: bounds may be affected by Stretch which would prove wrong here
        }

        protected override void OnChildReflow(Static child)
        {
            if (IsAutoSized)
                Reflow();
        }

        protected override void ReflowOverride(Vector2 availableSize)
        {
            float usedSize = 0;
            int stretches = 0;
            foreach (var child in EnumerateChildren())
            {
                if (!child.IsEnabled)
                    continue;

                if (Direction == Direction.Horizontal)
                {
                    if (child.HorizontalAlignment == Alignment.Stretch)
                        ++stretches;
                    else
                        usedSize += child.MeasuredSize.X;
                }
                else
                {
                    if (child.VerticalAlignment == Alignment.Stretch)
                        ++stretches;
                    else
                        usedSize += child.MeasuredSize.Y;
                }
            }
            usedSize += Margin * (TotalChildCount - 1);

            float stretchSize;
            if (Direction == Direction.Horizontal)
                stretchSize = System.Math.Max(0, (availableSize.X - usedSize) / stretches); //todo: availableSize?
            else
                stretchSize = System.Math.Max(0, (availableSize.Y - usedSize) / stretches);

            float t = 0;
            for (int i = 0; i < TotalChildCount; ++i)
            {
                var child = GetChildAt(i);
                if (!child.IsEnabled)
                    continue;

                if (i > 0)
                    t += Margin;

                float itemSize;
                if (Direction == Direction.Horizontal)
                {
                    if (child.HorizontalAlignment == Alignment.Stretch)
                        itemSize = stretchSize;
                    else
                        itemSize = child.Position.X + child.MeasuredSize.X;

                    child.Reflow(new Rectangle(
                        (int)(t + child.Position.X),
                        (int)child.Position.Y,
                        (int)itemSize,
                        (int)availableSize.Y
                    ));
                    t += itemSize;
                }
                else
                {
                    if (child.VerticalAlignment == Alignment.Stretch)
                        itemSize = stretchSize;
                    else
                        itemSize = child.Position.Y + child.MeasuredSize.Y;

                    child.Reflow(new Rectangle(
                        (int)child.Position.X,
                        (int)(t + child.Position.Y),
                        (int)availableSize.X,
                        (int)itemSize
                    ));
                    t += itemSize;
                }
            }

            NotifyChildReflow();
        }
    }
}
