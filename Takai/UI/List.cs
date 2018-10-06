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

        protected override void ReflowOverride(Point availableSize)
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
                        usedSize += Children[i].Bounds.Right;
                }
                else
                {
                    if (Children[i].VerticalAlignment == Alignment.Stretch)
                        ++stretches;
                    else
                        usedSize += Children[i].Bounds.Bottom;
                }
            }
            usedSize += Margin * (Children.Count - 1);

            float stretchSize;
            if (Direction == Direction.Horizontal)
                stretchSize = System.Math.Max(0, (Size.X - usedSize) / stretches); //todo: availableSize?
            else
                stretchSize = System.Math.Max(0, (Size.Y - usedSize) / stretches);

            float t = 0;
            for (int i = 0; i < Children.Count; ++i)
            {
                var child = Children[i];
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
                        itemSize = child.Bounds.Right;

                    child.Reflow(new Rectangle(
                        (int)(t + child.Position.X),
                        (int)child.Position.Y,
                        (int)itemSize,
                        (int)Size.Y
                    ));
                    t += child.Bounds.Right;
                }
                else
                {
                    if (child.VerticalAlignment == Alignment.Stretch)
                        itemSize = stretchSize;
                    else
                        itemSize = child.Bounds.Bottom;

                    child.Reflow(new Rectangle(
                        (int)child.Position.X,
                        (int)(t + child.Position.Y),
                        (int)Size.X,
                        (int)itemSize
                    ));
                    t += child.Bounds.Bottom;
                }
            }

            NotifyChildReflow();
        }
    }
}
