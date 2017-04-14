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

        public override void Reflow()
        {
            float t = 0;

            //pre-position items into a list
            for (int i = 0; i < Children.Count; ++i)
            {
                var child = children[i];
                //todo: account for auto size padding (first item offset)

                if (i > 0)
                    t += Margin;

                Vector2 position = new Vector2();

                if (Direction == Direction.Horizontal)
                {
                    position = new Vector2(t, 0);
                    t += Children[i].Size.X;
                }
                else if (Direction == Direction.Vertical)
                {
                    position = new Vector2(0, t);
                    t += Children[i].Size.Y;
                }

                children[i].Position = position;
            }

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
