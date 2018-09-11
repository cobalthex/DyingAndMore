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
            FitToContainer(container);

            float t = 0;

            //pre-position items into a list
            for (int i = 0; i < Children.Count; ++i)
            {
                var child = Children[i];
                if (!child.IsEnabled)
                    continue;

                //todo: account for auto size padding (first item offset)

                if (i > 0)
                    t += Margin;

                if (Direction == Direction.Horizontal)
                {
                    Children[i].Reflow(new Rectangle(
                        (int)t + AbsoluteDimensions.X,
                        AbsoluteDimensions.Y,
                        (int)Children[i].AbsoluteBounds.Width,
                        (int)Size.Y
                    ));
                    t += Children[i].AbsoluteBounds.Width;
                }
                else
                {
                    Children[i].Reflow(new Rectangle(
                        AbsoluteDimensions.X,
                        (int)t + AbsoluteDimensions.Y,
                        (int)Size.X,
                        (int)Children[i].AbsoluteBounds.Height
                    ));
                    t += Children[i].AbsoluteBounds.Height;
                }
            }
        }
    }
}
