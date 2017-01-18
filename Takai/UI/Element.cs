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

    public class ClickEventArgs : System.EventArgs
    {
        /// <summary>
        /// The relative position of the click inside the element
        /// </summary>
        public Vector2 position;
    }
    public delegate void ClickHandler(Element Sender, ClickEventArgs Args);

    /// <summary>
    /// A single UI Element
    /// </summary>
    public class Element
    {
        public static bool DrawBoundingRects = false;

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
        /// The input must start inside the element to register a click
        /// </summary>
        bool didPress = false;

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
        /// <remarks>Optional extra padding to inflate the element. This will adjust childrens' positioning</remarks>
        public void AutoSize(float Padding = 0)
        {
            var bounds = new Rectangle(Position.ToPoint(), textSize.ToPoint());
            foreach (var child in Children)
            {
                child.Position += new Vector2(Padding);
                bounds = Rectangle.Union(bounds, child.Bounds);
            }
            Size = bounds.Size.ToVector2() + new Vector2(Padding);
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
                    pos.X = (Container.Width - Size.X) / 2 + Position.X;
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

            return new Rectangle(pos.ToPoint() + Container.Location, Size.ToPoint());
        }

        public Rectangle CalculateAbsoluteBounds()
        {
            return Parent != null ? CalculateBounds(Parent.CalculateAbsoluteBounds()) :
                new Rectangle(Position.ToPoint(), Size.ToPoint());
        }

        /// <summary>
        /// Update this element
        /// </summary>
        /// <param name="DeltaTime">Elapsed time since the last update</param>
        /// <returns>True if the element should update (based on children), false otherwise (should cascade)</returns>
        public virtual bool Update(System.TimeSpan DeltaTime)
        {
            for (var i = Children.Count - 1; i >= 0 ; --i)
            {
                if (!Children[i].Update(DeltaTime))
                    return false;
            }

            var bounds = CalculateAbsoluteBounds();
            var mouse = Input.InputState.MousePoint;

            if (Input.InputState.IsPress(Input.MouseButtons.Left) && bounds.Contains(mouse))
                didPress = true;

            if (Input.InputState.IsClick(Input.MouseButtons.Left))
            {
                if (didPress && bounds.Contains(mouse) && OnClick != null)
                    OnClick(this, new ClickEventArgs { position = (mouse - bounds.Location).ToVector2() });

                didPress = false;
            }

            return true;
        }

        public virtual void Draw(SpriteBatch SpriteBatch)
        {
            var bounds = CalculateAbsoluteBounds();

            var size = new Point(
                MathHelper.Min(bounds.Width, (int)textSize.X),
                MathHelper.Min(bounds.Height, (int)textSize.Y)
            );

            if (DrawBoundingRects)
                Takai.Graphics.Primitives2D.DrawRect(SpriteBatch, new Color(0, 0.75f, 1, 0.35f), bounds);

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
