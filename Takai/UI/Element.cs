using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public enum Orientation
    {
        Start,
        Middle,
        End
    }

    public delegate void ClickHandler(Vector2 RelativePosition);

    /// <summary>
    /// A single UI Element
    /// </summary>
    public class Element
    {
        public Element Parent { get; set; } = null;
        public List<Element> Children { get; set; } = new List<Element>();

        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                textSize = Font?.MeasureString(text) ?? Vector2.One;
            }
        }
        private string text = "";
        protected Vector2 textSize;

        public Graphics.BitmapFont Font
        {
            get { return font; }
            set
            {
                font = value;
                textSize = font?.MeasureString(text) ?? Vector2.One;
            }
        }
        private Graphics.BitmapFont font;
        public Color Color { get; set; } = Color.White;

        public Orientation HorizontalOrientation { get; set; } = Orientation.Start;
        public Orientation VerticalOrientation { get; set; } = Orientation.Start;

        /// <summary>
        /// The position relative to the orientation.
        /// Start moves down and to the right
        /// Center moves down and to the right from the center
        /// End moves in the opposite direction
        /// </summary>
        public virtual Vector2 Position { get; set; } = Vector2.Zero;

        /// <summary>
        /// The size of the element
        /// </summary>
        public virtual Vector2 Size { get; set; } = Vector2.One;

        public Rectangle Bounds
        {
            get { return new Rectangle(Position.ToPoint(), Size.ToPoint()); }
            set
            {
                Position = value.Location.ToVector2();
                Size = value.Size.ToVector2();
            }
        }

        /// <summary>
        /// The click handler. If null, this item is not clickable/focusable (Does not apply to children)
        /// </summary>
        public event ClickHandler OnClick = null;

        public void AddChild(Element Child)
        {
            Child.Parent = this;
            Children.Add(Child);
        }

        /// <summary>
        /// Update the size of this element based on its text
        /// </summary>
        /// <remarks>Optional extra padding around the text</remarks>
        public void AutoSize(float Padding = 0)
        {
            Size = textSize + new Vector2(Padding);

            var bounds = Bounds;
            foreach (var child in Children)
            {
                child.AutoSize();
                bounds = Rectangle.Union(bounds, child.Bounds);
            }

            Size = bounds.Size.ToVector2();
        }

        /// <summary>
        /// Calculate the bounds of this element based on a parent container
        /// </summary>
        /// <param name="Container">The region that this element is relative to</param>
        /// <returns>The calculate rectangle relative to the container</returns>
        public Rectangle CalculateBounds(Rectangle Container)
        {
            var pos = new Vector2();

            switch (HorizontalOrientation)
            {
                case Orientation.Start:
                    pos.X = Position.X;
                    break;
                case Orientation.Middle:
                    pos.X = Position.X - (Container.Width - Size.X) / 2;
                    break;
                case Orientation.End:
                    pos.X = (Container.Width - Size.X) - Position.X;
                    break;
            }
            switch (VerticalOrientation)
            {
                case Orientation.Start:
                    pos.Y = Position.Y;
                    break;
                case Orientation.Middle:
                    pos.Y = (Container.Height - Size.Y) / 2 + Position.Y;
                    break;
                case Orientation.End:
                    pos.Y = (Container.Height - Size.Y) - Position.Y;
                    break;
            }

            return new Rectangle(pos.ToPoint(), Size.ToPoint());
        }

        public Rectangle CalculateAbsoluteBounds()
        {
            return Parent != null ? CalculateBounds(Parent.CalculateAbsoluteBounds()) :
                    new Rectangle(Position.ToPoint(), Size.ToPoint());
        }

        public virtual void Update(System.TimeSpan DeltaTime)
        {
            var bounds = CalculateAbsoluteBounds();
        }

        public virtual void Draw(SpriteBatch SpriteBatch)
        {
            var bounds = CalculateAbsoluteBounds();

            var size = new Point(
                MathHelper.Min(bounds.Width, (int)textSize.X),
                MathHelper.Min(bounds.Height, (int)textSize.Y)
            );

            Takai.Graphics.Primitives2D.DrawRect(SpriteBatch, new Color(1, 0.75f, 0, 0.25f), bounds);
            
            font?.Draw(
                SpriteBatch,
                Text,
                new Rectangle(
                    bounds.X + ((bounds.Width - size.X) / 2),
                    bounds.Y + ((bounds.Height - size.Y) / 2),
                    size.X,
                    size.Y
                ),
                Color
            );

            foreach (var child in Children)
                child.Draw(SpriteBatch);
        }
    }
}
