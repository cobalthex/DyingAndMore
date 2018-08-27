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

        public override void Reflow()
        {
            float t = 0;

            int stretched = 0;
            float size = Direction == Direction.Horizontal ? Size.X : Size.Y;
            foreach (var child in Children)
            {
                if (!child.IsEnabled)
                    continue;

                if ((Direction == Direction.Horizontal
                && child.HorizontalAlignment == Alignment.Stretch)
                || (Direction == Direction.Vertical
                && child.VerticalAlignment == Alignment.Stretch))
                    ++stretched;
                else
                {
                    if (Direction == Direction.Horizontal)
                        size -= child.Size.X;
                    else if (Direction == Direction.Vertical)
                        size -= child.Size.Y;
                }
            }
            size /= stretched;

            //todo: base.Reflow() overrides stretch, need to move elsewhere (into method maybe?)

            //pre-position items into a list
            for (int i = 0; i < Children.Count; ++i)
            {
                var child = Children[i];
                if (!child.IsEnabled)
                    continue;

                //todo: account for auto size padding (first item offset)

                if (i > 0)
                    t += Margin;

                Vector2 position = child.Position;
                if (Direction == Direction.Horizontal)
                {
                    position = new Vector2(t, position.Y);
                    if (child.HorizontalAlignment == Alignment.Stretch)
                        child.Size = new Vector2(size, child.Size.Y);

                    t += child.Size.X;
                }
                else if (Direction == Direction.Vertical)
                {
                    position = new Vector2(position.X, t);
                    if (child.VerticalAlignment == Alignment.Stretch)
                        child.Size = new Vector2(child.Size.X, size);

                    t += child.Size.Y;
                }

                child.Position = position;
            }

            //todo: stretched elements

            //currently broken:
            //todo: decide how to handle alignment on main axis, vertical list with child:vertical-middle will put child in middle of list container

            base.Reflow();
        }
    }
}
