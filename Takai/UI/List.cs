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

        //todo: shared size? (only measure using MeasuredSize)
        //must manually calculate size?

        public List() { }
        public List(params Static[] children)
            : base(children) { }

        int stretches = 0;
        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var usedSize = new Vector2();
            for (int i = 0; i < Children.Count; ++i)
            {
                var child = Children[i];
                if (!child.IsEnabled)
                    continue;

                //stretched items in primary axis are rendered at 

                var childSize = child.Measure(new Vector2(InfiniteSize));
                if (Direction == Direction.Horizontal)
                {
                    if (child.HorizontalAlignment == Alignment.Stretch)
                        ++stretches;
                    else //(only else here if stretch size != 0, see Static::Measure)
                        usedSize.X += childSize.X;
                    usedSize.Y = System.Math.Max(usedSize.Y, childSize.Y);
                }
                else
                {
                    if (child.VerticalAlignment == Alignment.Stretch)
                        ++stretches;
                    else //ditto above
                        usedSize.Y += childSize.Y;
                    usedSize.X = System.Math.Max(usedSize.X, childSize.X);
                }
            }
            if (Direction == Direction.Horizontal)
                usedSize.X += Margin * (Children.Count - 1);
            else
                usedSize.Y += Margin * (Children.Count - 1);

            return usedSize;

            //todo: bounds may be affected by Stretch which would prove wrong here
        }

        //protected override void OnChildRemeasure(Static child)
        //{
        //    InvalidateMeasure();
        //    InvalidateArrange();
        //}

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            float stretchSize;
            if (Direction == Direction.Horizontal)
                stretchSize = System.Math.Max(0, (availableSize.X - MeasuredSize.X) / stretches); //todo: availableSize?
            else
                stretchSize = System.Math.Max(0, (availableSize.Y - MeasuredSize.Y) / stretches);

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
                        itemSize = Children[i].MeasuredSize.X;

                    Children[i].Arrange(new Rectangle(
                        (int)t,
                        (int)0,
                        (int)itemSize,
                        (int)availableSize.Y
                    ));
                    t += itemSize;
                }
                else
                {
                    if (Children[i].VerticalAlignment == Alignment.Stretch)
                        itemSize = stretchSize;
                    else
                        itemSize = Children[i].MeasuredSize.Y;

                    Children[i].Arrange(new Rectangle(
                        (int)0,
                        (int)t,
                        (int)availableSize.X,
                        (int)itemSize
                    ));
                    t += itemSize;
                }
            }
        }
    }
}
