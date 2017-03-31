using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public enum Alignment
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
            get => parent;
            set
            {
                if (parent != value)
                {
                    parent = value;
                    Reflow();
                }
            }
        }
        private Element parent = null;

        /// <summary>
        /// A readonly collection of all of the children in this element
        /// </summary>
        public ReadOnlyCollection<Element> Children { get; private set; } //todo: maybe observable
        protected List<Element> children = new List<Element>();

        /// <summary>
        /// The text of the element. Can be null or empty
        /// </summary>
        public virtual string Text
        {
            get => text;
            set
            {
                if (text != value)
                {
                    text = value;
                    textSize = Font?.MeasureString(text) ?? Vector2.One;
                }
            }
        }
        private string text = "";
        protected Vector2 textSize;

        public Graphics.BitmapFont Font
        {
            get => font;
            set
            {
                if (font != value)
                {
                    font = value;
                    textSize = font?.MeasureString(text) ?? Vector2.One;
                }
            }
        }
        private Graphics.BitmapFont font;

        public Color Color { get; set; } = Color.White;

        /// <summary>
        /// How this element is positioned in its container horizontally
        /// </summary>
        public Alignment HorizontalAlignment
        {
            get => horizontalAlignment;
            set
            {
                if (horizontalAlignment != value)
                {
                    horizontalAlignment = value;
                    Reflow();
                }
            }
        }
        private Alignment horizontalAlignment;

        /// <summary>
        /// How this element is positioned in its container vertically
        /// </summary>
        public Alignment VerticalAlignment
        {
            get => verticalAlignment;
            set
            {
                if (verticalAlignment != value)
                {
                    verticalAlignment = value;
                    Reflow();
                }
            }
        }
        private Alignment verticalAlignment;

        /// <summary>
        /// The position relative to the orientation.
        /// Start moves down and to the right
        /// Center moves down and to the right from the center
        /// End moves in the opposite direction
        /// </summary>
        public Vector2 Position
        {
            get => position;
            set
            {
                if (position != value)
                {
                    position = value;
                    Reflow();
                }
            }
        }
        private Vector2 position = Vector2.Zero;

        /// <summary>
        /// The size of the element
        /// </summary>
        public Vector2 Size
        {
            get => size;
            set
            {
                if (size != value)
                {
                    size = value;
                    ResizeAndReflow();
                }
            }
        }
        private Vector2 size = Vector2.One;

        public Rectangle Bounds
        {
            get => new Rectangle(Position.ToPoint(), Size.ToPoint());
            set
            {
                if (Bounds != value)
                {
                    position = value.Location.ToVector2();
                    size = value.Size.ToVector2();
                    ResizeAndReflow();
                }
            }
        }

        /// <summary>
        /// Bounds relative to the outermost container
        /// </summary>
        public Rectangle AbsoluteBounds
        {
            get => absoluteBounds;
        }
        private Rectangle absoluteBounds; //cached bounds

        //Does this element currently have focus
        public bool HasFocus
        {
            get => hasFocus;
            set
            {
                if (value == false)
                    hasFocus = false;

                else
                {
                    Stack<Element> defocusing = new Stack<Element>();

                    //defocus all elements in tree
                    Element next = this;
                    while (next.parent != null)
                        next = next.parent;

                    defocusing.Push(next);
                    while (defocusing.Count > 0)
                    {
                        next = defocusing.Pop();
                        next.hasFocus = false;

                        foreach (var child in next.children)
                            defocusing.Push(child);
                    }

                    hasFocus = true;
                }
            }
        }
        private bool hasFocus = false;

        /// <summary>
        /// Can this element be focused
        /// </summary>
        public virtual bool CanFocus { get => OnClick != null; }

        /// <summary>
        /// The click handler. If null, this item is not clickable/focusable (Does not apply to children)
        /// </summary>
        [Data.NonSerialized]
        public event ClickHandler OnClick = null;

        /// <summary>
        /// The input must start inside the element to register a click
        /// </summary>
        [Data.NonSerialized]
        protected bool didPress = false;

        /// <summary>
        /// Create a new element
        /// </summary>
        /// <param name="children">Optionally add children to this element</param>
        public Element(params Element[] children)
        {
            Children = new ReadOnlyCollection<Element>(this.children);

            foreach (var child in children)
                AddChild(child);
        }

        /// <summary>
        /// Remove this element from its parent. If parent is null, does nothing
        /// </summary>
        /// <returns>True if the element was removed from its parent or false if parent was null</returns>
        public bool RemoveFromParent()
        {
            if (Parent != null)
            {
                parent.RemoveChild(this);
                parent = null;
                return true;
            }
            return false;
        }

        public void AddChild(Element child)
        {
            child.Parent = this;
            children.Add(child);
            Reflow();
        }

        public void RemoveChild(Element child)
        {
            children.Remove(child);
        }

        public Element RemoveChildAt(int index)
        {
            var child = children[index];
            children.RemoveAt(index);
            return child;
        }

        public void RemoveAllChildren()
        {
            children.Clear();
        }

        protected void ResizeAndReflow()
        {
            CalculateAbsoluteBounds();

            //OnResized()

            Reflow();
        }

        /// <summary>
        /// Reflow child elements relative to this element
        /// Called whenever this element's position or size is adjusted
        /// </summary>
        public virtual void Reflow()
        {
            foreach (var child in Children)
            {
                child.absoluteBounds = child.CalculateBounds(absoluteBounds);
                child.Reflow();
            }
        }

        /// <summary>
        /// Calculate the bounds of this element based on a parent container
        /// </summary>
        /// <param name="container">The region that this element is relative to</param>
        /// <returns>The calculate rectangle relative to the container</returns>
        public virtual Rectangle CalculateBounds(Rectangle container)
        {
            var pos = new Vector2();

            switch (HorizontalAlignment)
            {
                case Alignment.Start:
                    pos.X = Position.X;
                    break;
                case Alignment.Middle:
                    pos.X = (container.Width - Size.X) / 2 + Position.X;
                    break;
                case Alignment.End:
                    pos.X = (container.Width - Size.X) - Position.X;
                    break;
            }
            switch (VerticalAlignment)
            {
                case Alignment.Start:
                    pos.Y = Position.Y;
                    break;
                case Alignment.Middle:
                    pos.Y = (container.Height - Size.Y) / 2 + Position.Y;
                    break;
                case Alignment.End:
                    pos.Y = (container.Height - Size.Y) - Position.Y;
                    break;
            }

            return new Rectangle(pos.ToPoint() + container.Location, Size.ToPoint()); //if size is changed, changes may need to be made elsewhere
        }

        public static int callCount = 0;
        protected Rectangle CalculateAbsoluteBounds()
        {
            callCount++;
            absoluteBounds = Parent != null
                ? CalculateBounds(Parent.CalculateAbsoluteBounds())
                : new Rectangle(Position.ToPoint(), Size.ToPoint());
            return absoluteBounds;

        }

        /// <summary>
        /// Automatically size this element. By default, will size based on text
        /// This does not affect the position of the element
        /// Padding affects child positioning
        /// </summary>
        public virtual void AutoSize(float padding = 0)
        {
            var bounds = new Rectangle(Position.ToPoint(), textSize.ToPoint());
            foreach (var child in Children)
            {
                child.Position += new Vector2(padding);
                bounds = Rectangle.Union(bounds, child.Bounds);
            }
            Size = bounds.Size.ToVector2() + new Vector2(padding);
        }

        /// <summary>
        /// Add specific behavior before <see cref="OnClick"/> is called. Called by Update()
        /// </summary>
        /// <param name="args">Click event args passed from Update. Forwarded to <see cref="OnClick"/></param>
        protected virtual void BeforeClick(ClickEventArgs args)
        {
            OnClick(this, args);
        }

        /// <summary>
        /// Update this element
        /// </summary>
        /// <param name="time">Game time</param>
        /// <returns>Returns true if this element was clicked/triggered. This will prevent parent items from being triggered as well</returns>
        public virtual bool Update(GameTime time)
        {
            if (!Runtime.GameManager.HasFocus)
                return false;

            for (var i = Children.Count - 1; i >= 0; --i)
            {
                if (!Children[i].Update(time))
                    return false;
            }

            var mouse = Input.InputState.MousePoint;

            if (Input.InputState.IsPress(Input.MouseButtons.Left) && AbsoluteBounds.Contains(mouse))
            {
                didPress = true;
                if (CanFocus)
                    HasFocus = true;
            }

            else if (Input.InputState.IsClick(Input.MouseButtons.Left))
            {
                if (didPress && AbsoluteBounds.Contains(mouse) && OnClick != null)
                {
                    BeforeClick(new ClickEventArgs { position = (mouse - AbsoluteBounds.Location).ToVector2() });
                    didPress = false;
                    return false;
                }

                didPress = false;
            }

            return true;
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (DrawBoundingRects || HasFocus)
                Graphics.Primitives2D.DrawRect(spriteBatch, new Color(0, 0.75f, 1, 0.35f), AbsoluteBounds);

            var textBounds = CalculateTextBounds(textSize, AbsoluteBounds);
            Font?.Draw(spriteBatch, Text, textBounds, Color);

            foreach (var child in Children)
                child.Draw(spriteBatch);
        }

        /// <summary>
        /// Caluclate a rectangle where the text is centered inside bounds
        /// </summary>
        /// <param name="textSize">The size of the text</param>
        /// <param name="bounds">The bounds to center inside</param>
        /// <returns>The absolute bounds of the text rect</returns>
        protected static Rectangle CalculateTextBounds(Vector2 textSize, Rectangle bounds)
        {
            var size = new Point(
                MathHelper.Min(bounds.Width, (int)textSize.X),
                MathHelper.Min(bounds.Height, (int)textSize.Y)
            );
            return new Rectangle(
                bounds.X + ((bounds.Width - size.X) / 2),
                bounds.Y + ((bounds.Height - size.Y) / 2),
                size.X,
                size.Y
            );
        }
    }
}
