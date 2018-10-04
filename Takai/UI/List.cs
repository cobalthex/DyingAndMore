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

        public override void Reflow(Rectangle container)
        {
            AdjustToContainer(container);

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
                        usedSize += Children[i].LocalBounds.Right;
                }
                else
                {
                    if (Children[i].VerticalAlignment == Alignment.Stretch)
                        ++stretches;
                    else
                        usedSize += Children[i].LocalBounds.Bottom;
                }
            }
            usedSize += Margin * (Children.Count - 1);

            float stretchSize;
            if (Direction == Direction.Horizontal)
                stretchSize = System.Math.Max(0, (LocalDimensions.Width - usedSize) / stretches);
            else
                stretchSize = System.Math.Max(0, (LocalDimensions.Height - usedSize) / stretches);

            float t = 0;
            for (int i = 0; i < Children.Count; ++i)
            {
                var child = Children[i];
                if (!child.IsEnabled)
                    continue;

                if (i > 0)
                    t += Margin;

                float size;
                if (Direction == Direction.Horizontal)
                {
                    if (child.HorizontalAlignment == Alignment.Stretch)
                        size = stretchSize;
                    else
                        size = child.LocalBounds.Right;

                    child.Reflow(new Rectangle(
                        (int)(t + child.Position.X) + AbsoluteDimensions.X,
                        (int)child.Position.Y + AbsoluteDimensions.Y,
                        (int)size,
                        AbsoluteDimensions.Height
                    ));
                    t += child.LocalBounds.Right;
                }
                else
                {
                    if (child.VerticalAlignment == Alignment.Stretch)
                        size = stretchSize;
                    else
                        size = child.LocalBounds.Bottom;

                    child.Reflow(new Rectangle(
                        (int)child.Position.X + AbsoluteDimensions.X,
                        (int)t + AbsoluteDimensions.Y,
                        AbsoluteDimensions.Width,
                        (int)size
                    ));
                    t += child.LocalBounds.Bottom;
                }
            }

            NotifyChildReflow();
        }
    }
}
