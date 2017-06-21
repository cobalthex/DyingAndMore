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
        public int Margin { get; set; } = 0;

        /// <summary>
        /// Which direction should list items flow
        /// </summary>
        public Direction Direction { get; set; } = Direction.Vertical;

        public List(params Static[] children)
            : base(children) { }

        public override void Reflow()
        {
            float t = 0;

            int stretched = 0;
            float size = Direction == Direction.Horizontal ? Size.X : Size.Y;
            foreach (var child in children)
            {
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
                var child = children[i];
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

            //todo: decide how to handle alignment on main axis, vertical list with child:vertical-middle will put child in middle of list container

            base.Reflow();
        }

        /// <summary>
        /// Resize this list to fit to the children
        /// </summary>
        /// <param name="padding">Optional padding around the element. Will adjust childrens' positions</param>
        public override void AutoSize(float padding = 0)
        {
            Vector2 size = new Vector2(padding);

            if (Direction == Direction.Horizontal)
            {
                for (int i = 0; i < Children.Count; ++i)
                {
                    if (i > 0)
                        size.X += Margin;

                    size.X += Children[i].Size.X;
                    size.Y = MathHelper.Max(size.Y, Children[i].Size.Y);

                    Children[i].Position += new Vector2(padding);
                }
            }
            else if (Direction == Direction.Vertical)
            {
                for (int i = 0; i < Children.Count; ++i)
                {
                    if (i > 0)
                        size.Y += Margin;

                    size.X = MathHelper.Max(size.X, Children[i].Size.X);
                    size.Y += Children[i].Size.Y;

                    Children[i].Position += new Vector2(padding);
                }
            }

            Size = size;
        }
    }
}
