using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public enum Direction
    {
        Horizontal,
        Vertical
    }

    public class List : Static
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
            foreach (var child in Children)
            {
                if (!child.IsEnabled)
                    continue;

                var childSize = child.Measure(InfiniteSize);
                if (Direction == Direction.Horizontal)
                {
                    if (child.HorizontalAlignment != Alignment.Stretch)
                    {
                        usedSize.X += childSize.X; //todo: better format for this
                        usedSize.Y = System.Math.Max(usedSize.Y, childSize.Y);
                    }
                }
                else
                {
                    if (child.VerticalAlignment != Alignment.Stretch)
                    {
                        usedSize.X = System.Math.Max(usedSize.X, childSize.X);
                        usedSize.Y += childSize.Y;
                    }
                }
            }
            if (Direction == Direction.Horizontal)
                usedSize.X += Margin * (Children.Count - 1);
            else
                usedSize.Y += Margin * (Children.Count - 1);

            return usedSize;

            //todo: bounds may be affected by Stretch which would prove wrong here
        }

        protected override void ReflowOverride(Vector2 availableSize)
        {
            float usedSize = 0;
            int stretches = 0;
            for (int i = 0; i < Children.Count; ++i)
            {
                if (!Children[i].IsEnabled)
                    continue;

                if (Direction == Direction.Horizontal)
                {
                    if (Children[i].HorizontalAlignment == Alignment.Stretch)
                        ++stretches;
                    else
                        usedSize += Children[i].Position.X + Children[i].ContentArea.Width;
                }
                else
                {
                    if (Children[i].VerticalAlignment == Alignment.Stretch)
                        ++stretches;
                    else
                        usedSize += Children[i].Position.Y + Children[i].ContentArea.Height;
                }
            }
            usedSize += Margin * (Children.Count - 1);

            float stretchSize;
            if (Direction == Direction.Horizontal)
                stretchSize = System.Math.Max(0, (availableSize.X - usedSize) / stretches); //todo: availableSize?
            else
                stretchSize = System.Math.Max(0, (availableSize.Y - usedSize) / stretches);

            float t = 0;
            for (int i = 0; i < Children.Count; ++i)
            {
                if (!Children[i].IsEnabled)
                    continue;

                if (i > 0)
                    t += Margin;

                float itemSize;
                if (Direction == Direction.Horizontal)
                {
                    if (Children[i].HorizontalAlignment == Alignment.Stretch)
                        itemSize = stretchSize;
                    else
                        itemSize = Children[i].Position.X + Children[i].ContentArea.Width;

                    Children[i].Reflow(new Rectangle(
                        (int)(t + Children[i].Position.X),
                        (int)Children[i].Position.Y,
                        (int)itemSize,
                        (int)availableSize.Y
                    ));
                    t += Children[i].Position.X + Children[i].ContentArea.Width;
                }
                else
                {
                    if (Children[i].VerticalAlignment == Alignment.Stretch)
                        itemSize = stretchSize;
                    else
                        itemSize = Children[i].Position.Y + Children[i].ContentArea.Height;

                    Children[i].Reflow(new Rectangle(
                        (int)Children[i].Position.X,
                        (int)(t + Children[i].Position.Y),
                        (int)availableSize.X,
                        (int)itemSize
                    ));
                    t += Children[i].Position.Y + Children[i].ContentArea.Height;
                }
            }

            NotifyChildReflow();
        }
    }
}
