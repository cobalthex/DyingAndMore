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
                    child.Reflow(new Rectangle(
                        (int)(t + child.Position.X) + AbsoluteDimensions.X,
                        (int)child.Position.Y + AbsoluteDimensions.Y,
                        child.AbsoluteBounds.Width + (int)child.Position.X,
                        (int)(Size.Y + child.Position.Y)
                    ));
                    t += child.Position.X + child.AbsoluteBounds.Width;
                }
                else
                {
                    child.Reflow(new Rectangle(
                        AbsoluteDimensions.X,
                        (int)t + AbsoluteDimensions.Y,
                        (int)(Size.X + child.Position.X),
                        child.AbsoluteBounds.Height + (int)child.Position.Y
                    ));
                    t += child.Position.Y + child.AbsoluteBounds.Height;
                }
            }
        }
    }
}
