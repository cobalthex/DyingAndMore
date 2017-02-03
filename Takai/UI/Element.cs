using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        /// <summary>
        /// A unique name for this element. Can be null
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Who owns/contains this element
        /// </summary>
        public Element Parent
        {
            get { return parent; }
            set
            {
                parent = value;
                UpdateBounds();
            }
        }
        private Element parent = null;

        /// <summary>
        /// A readonly collection of all of the children in this element
        /// </summary>
        public ReadOnlyCollection<Element> Children { get; set; } //todo: maybe observable
        protected List<Element> children = new List<Element>();

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

        /// <summary>
        /// How this element is positioned in its container horizontally
        /// </summary>
        public Orientation HorizontalOrientation
        {
            get { return horizontalOrientation; }
            set
            {
                horizontalOrientation = value;
                UpdateBounds();
            }
        }
        private Orientation horizontalOrientation;

        /// <summary>
        /// How this element is positioned in its container vertically
        /// </summary>
        public Orientation VerticalOrientation
        {
            get { return verticalOrientation; }
            set
            {
                verticalOrientation = value;
                UpdateBounds();
            }
        }
        private Orientation verticalOrientation;

        /// <summary>
        /// The position relative to the orientation.
        /// Start moves down and to the right
        /// Center moves down and to the right from the center
        /// End moves in the opposite direction
        /// </summary>
        public Vector2 Position
        {
            get { return position; }
            set
            {
                position = value;
                UpdateBounds();
            }
        }
        private Vector2 position = Vector2.Zero;

        /// <summary>
        /// The size of the element
        /// </summary>
        public Vector2 Size
        {
            get { return size; }
            set
            {
                size = value;
                UpdateBounds();
            }
        }
        private Vector2 size = Vector2.One;

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
        /// Bounds relative to the outermost container
        /// </summary>
        public Rectangle AbsoluteBounds
        {
            get { return absoluteBounds; }
        }
        private Rectangle absoluteBounds; //cached bounds

        //todo: focus

        /// <summary>
        /// The click handler. If null, this item is not clickable/focusable (Does not apply to children)
        /// </summary>
        [Data.NonSerialized]
        public event ClickHandler OnClick = null;

        /// <summary>
        /// Can this element be focused
        /// </summary>
        public virtual bool CanFocus { get { return OnClick != null; } }

        /// <summary>
        /// The input must start inside the element to register a click
        /// </summary>
        [Data.NonSerialized]
        protected bool didPress = false;

        public Element()
        {
            Children = new ReadOnlyCollection<Element>(children);
        }

        public void AddChild(Element Child)
        {
            Child.Parent = this;
            children.Add(Child);
        }

        public void RemoveChild(Element Child)
        {
            children.Remove(Child);
        }

        public Element RemoveChildAt(int Index)
        {
            var child = children[Index];
            children.RemoveAt(Index);
            return child;
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

        /// <summary>
        /// Calculate the absolute bounds of this element relative to its parents
        /// </summary>
        /// <returns></returns>
        Rectangle CalculateAbsoluteBounds()
        {
            absoluteBounds = Parent != null ? CalculateBounds(Parent.CalculateAbsoluteBounds()) :
                new Rectangle(Position.ToPoint(), Size.ToPoint());
            return absoluteBounds;
        }

        /// <summary>
        /// Update the bounds of this element and all its children
        /// </summary>
        void UpdateBounds()
        {
            CalculateAbsoluteBounds();

            var elements = new Queue<Element>();
            foreach (var child in Children)
                elements.Enqueue(child);

            while (elements.Count > 0)
            {
                var element = elements.Dequeue();
                element.absoluteBounds = element.CalculateBounds(element.parent.absoluteBounds);

                foreach (var child in element.Children)
                    elements.Enqueue(child);
            }
        }

        /// <summary>
        /// Update this element
        /// </summary>
        /// <param name="DeltaTime">Elapsed time since the last update</param>
        /// <returns>Returns true if this element was clicked/triggered. This will prevent parent items from being triggered as well</returns>
        public virtual bool Update(System.TimeSpan DeltaTime)
        {
            for (var i = Children.Count - 1; i >= 0 ; --i)
            {
                if (!Children[i].Update(DeltaTime))
                    return false;
            }

            var mouse = Input.InputState.MousePoint;

            //todo: focus

            if (Input.InputState.IsPress(Input.MouseButtons.Left) && AbsoluteBounds.Contains(mouse))
                didPress = true;

            else if (Input.InputState.IsClick(Input.MouseButtons.Left))
            {
                if (didPress && AbsoluteBounds.Contains(mouse) && OnClick != null)
                {
                    OnClick(this, new ClickEventArgs { position = (mouse - AbsoluteBounds.Location).ToVector2() });
                    didPress = false;
                    return false;
                }

                didPress = false;
            }

            return true;
        }

        public virtual void Draw(SpriteBatch SpriteBatch)
        {
            var size = new Point(
                MathHelper.Min(AbsoluteBounds.Width, (int)textSize.X),
                MathHelper.Min(AbsoluteBounds.Height, (int)textSize.Y)
            );

            if (DrawBoundingRects)
                Takai.Graphics.Primitives2D.DrawRect(SpriteBatch, new Color(0, 0.75f, 1, 0.35f), AbsoluteBounds);

            font?.Draw(
                SpriteBatch,
                Text,
                new Rectangle(
                    AbsoluteBounds.X + ((AbsoluteBounds.Width - size.X) / 2),
                    AbsoluteBounds.Y + ((AbsoluteBounds.Height - size.Y) / 2),
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
