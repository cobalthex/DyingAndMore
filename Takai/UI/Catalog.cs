using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class Catalog : Static
    {
        /// <summary>
        /// Spacing between items
        /// </summary>
        public float Margin { get; set; } = 0;

        /// <summary>
        /// Which direction should items flow
        /// (horizontal = rows vs vertical = columns)
        /// </summary>
        public Direction Direction { get; set; } = Direction.Horizontal;

        //todo: shared size? (only measure using MeasuredSize)
        //must manually calculate size?

        //justify option?

        public Catalog() { }
        public Catalog(params Static[] children)
            : base(children) { }

        public override void ApplyStyles(Dictionary<string, object> styleRules)
        {
            base.ApplyStyles(styleRules);
            Margin = GetStyleRule(styleRules, "Margin", Margin);
            Direction = GetStyleRule(styleRules, "Direction", Direction);
        }

        int stretches = 0;
        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            //todo

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

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            float stretchSize;
            if (Direction == Direction.Horizontal)
                stretchSize = System.Math.Max(0, (availableSize.X - MeasuredSize.X) / stretches); //todo: availableSize?
            else
                stretchSize = System.Math.Max(0, (availableSize.Y - MeasuredSize.Y) / stretches);

            float front = 0;
            float back = 0;
            for (int i = 0, n = 0; i < Children.Count; ++i)
            {
                if (!Children[i].IsEnabled)
                    continue;

                if (n++ > 0)
                    front += Margin; //todo: this doesnt apply on right aligned correctly

                float itemSize;
                if (Direction == Direction.Horizontal)
                {
                    if (Children[i].HorizontalAlignment == Alignment.Stretch)
                        itemSize = stretchSize;
                    else
                        itemSize = Children[i].MeasuredSize.X;

                    if (Children[i].HorizontalAlignment == Alignment.End)
                    {
                        back += itemSize;
                        Children[i].Arrange(new Rectangle(
                            (int)(availableSize.X - back),
                            (int)0,
                            (int)itemSize,
                            (int)availableSize.Y
                        ));
                    }
                    else
                    {
                        Children[i].Arrange(new Rectangle(
                            (int)front,
                            (int)0,
                            (int)itemSize,
                            (int)availableSize.Y
                        ));
                        front += itemSize;
                    }
                    //center?
                }
                else
                {
                    if (Children[i].VerticalAlignment == Alignment.Stretch)
                        itemSize = stretchSize;
                    else
                        itemSize = Children[i].MeasuredSize.Y;

                    if (Children[i].VerticalAlignment == Alignment.End)
                    {
                        back += itemSize;
                        Children[i].Arrange(new Rectangle(
                            (int)0,
                            (int)(availableSize.Y - back),
                            (int)availableSize.X,
                            (int)itemSize
                        ));
                    }
                    else
                    {
                        Children[i].Arrange(new Rectangle(
                            (int)0,
                            (int)front,
                            (int)availableSize.X,
                            (int)itemSize
                        ));
                        front += itemSize;
                    }
                    //center?
                }
            }
        }
    }
}
