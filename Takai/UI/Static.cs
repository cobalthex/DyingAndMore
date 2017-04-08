using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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
    public delegate void ClickHandler(Static Sender, ClickEventArgs Args);

    /// <summary>
    /// The basic UI element
    /// </summary>
    [Data.DerivedTypeDeserialize(typeof(Static), "DerivedDeserialize")]
    public class Static
    {
        public static Color FocusOutlineColor = Color.RoyalBlue;

        public static bool DrawBoundingRects = false;

        /// <summary>
        /// A unique name for this element. Can be null
        /// </summary>
        public string Name { get; set; } = null;

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

        /// <summary>
        /// The font to draw the text of this element with.
        /// Optional if text is null
        /// </summary>
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

        /// <summary>
        /// The color of this element. Usage varies between element types
        /// Usually applies to text color
        /// </summary>
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
        [Data.Serializer.ReadOnly]
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
        [Data.Serializer.ReadOnly]
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
        [Data.Serializer.Ignored]
        public Rectangle AbsoluteBounds
        {
            get => absoluteBounds;
        }
        private Rectangle absoluteBounds; //cached bounds

        /// <summary>
        /// Does this element currently have focus?
        /// </summary>
        public bool HasFocus
        {
            get => hasFocus;
            set
            {
                if (value == false)
                    hasFocus = false;

                else
                {
                    Stack<Static> defocusing = new Stack<Static>();

                    //defocus all elements in tree
                    Static next = this;
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
        [Data.Serializer.Ignored]
        public virtual bool CanFocus { get => OnClick != null; }

        /// <summary>
        /// The click handler. If null, this item is not clickable/focusable (Does not apply to children)
        /// </summary>
        [Data.Serializer.Ignored]
        public event ClickHandler OnClick = null;

        /// <summary>
        /// Disable the default behavior of the tab key
        /// </summary>
        protected bool ignoreTabKey = false;
        /// <summary>
        /// Disable the default behavior of the space key
        /// </summary>
        protected bool ignoreSpaceKey = false;
        /// <summary>
        /// Disable the default behavior of the enter key
        /// </summary>
        protected bool ignoreEnterKey = false;

        /// <summary>
        /// The input must start inside the element to register a click
        /// </summary>
        protected bool didPress = false;

        /// <summary>
        /// Who owns/contains this element
        /// </summary>
        [Data.Serializer.Ignored]
        public Static Parent
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
        private Static parent = null;

        /// <summary>
        /// A readonly collection of all of the children in this element
        /// </summary>
        [Data.CustomDeserialize(typeof(Static), "DeserializeChildren")]
        public ReadOnlyCollection<Static> Children { get; private set; } //todo: maybe observable
        protected List<Static> children = new List<Static>();

        private void DeserializeChildren(object objects)
        {
            var elements = objects as List<object>;
            foreach (var element in elements)
            {
                if (element is Static child)
                    AddChild(child);
            }
        }

        public Static()
        {
            Children = children.AsReadOnly();
        }

        /// <summary>
        /// Create a new element
        /// </summary>
        /// <param name="children">Optionally add children to this element</param>
        public Static(params Static[] children)
            : this()
        {
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

        public void AddChild(Static child)
        {
            child.Parent = this;
            children.Add(child);
            if (child.CanFocus)
                child.HasFocus = true;
            Reflow();
        }

        public void RemoveChild(Static child)
        {
            children.Remove(child);
        }

        public Static RemoveChildAt(int index)
        {
            var child = children[index];
            children.RemoveAt(index);
            return child;
        }

        public void RemoveAllChildren()
        {
            children.Clear();
        }

        public void ReplaceChildren(params Static[] newChildren)
        {
            RemoveAllChildren();
            children.AddRange(newChildren);
            Reflow();
        }
        public void ReplaceChildren(List<Static> newChildren)
        {
            RemoveAllChildren();
            children.AddRange(newChildren);
            Reflow();
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

            return new Rectangle(pos.ToPoint() + container.Location, Size.ToPoint());
        }

        protected Rectangle CalculateAbsoluteBounds()
        {
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
        /// Focus the next element in the tree
        /// </summary>
        /// <remarks>If this is not the focused element, finds the focused element and calls this function</remarks>
        protected void FocusNext()
        {
            if (!HasFocus)
            {
                FindFocusedElement()?.FocusNext();
                return;
            }

            /* focus in the following order (13 will wrap around back to 1)

            1
                2
                3
                    4
                5
                6
            7
                8
                    9
                        10
                        11
                    12
                13

            */

            var next = this;
            while (next != null)
            {
                var current = next;
                foreach (var child in next.children)
                {
                    if (child.CanFocus)
                    {
                        child.HasFocus = true;
                        return;
                    }

                    if (child.Children.Count > 0)
                    {
                        next = child;
                        break;
                    }
                }

                if (current != next)
                    continue;

                while (next.parent != null)
                {
                    var index = next.Parent.Children.IndexOf(next) + 1;
                    if (index < next.Parent.Children.Count)
                    {
                        next = next.Parent.Children[index];
                        if (next.CanFocus)
                        {
                            next.HasFocus = true;
                            return;
                        }
                    }
                    else
                        next = next.parent;
                }

                if (next.CanFocus)
                {
                    next.HasFocus = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Focus the previous element, using the reverse order of FocusNext()
        /// </summary>
        protected void FocusPrevious()
        {
            if (!HasFocus)
            {
                FindFocusedElement()?.FocusPrevious();
                return;
            }

            var prev = this;

            while (true)
            {
                if (prev.parent == null)
                {
                    while (prev.children.Count > 0)
                        prev = prev.children[prev.children.Count - 1];
                }
                else
                {
                    var index = prev.Parent.Children.IndexOf(prev) - 1;
                    if (index >= 0)
                    {
                        prev = prev.Parent.Children[index];

                        while (prev.children.Count > 0)
                            prev = prev.children[prev.children.Count - 1];
                    }
                    else
                        prev = prev.parent;
                }


                if (prev.CanFocus)
                {
                    prev.HasFocus = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Find the element in this treethat has focus
        /// Searches up and down
        /// </summary>
        /// <returns>The focused element, or null if there is none</returns>
        public Static FindFocusedElement()
        {
            var parent = this;
            while (parent.Parent != null)
                parent = parent.Parent;

            var next = new Stack<Static>();
            next.Push(parent);

            while (next.Count > 0)
            {
                var elem = next.Pop();
                if (elem.HasFocus)
                    return elem;

                foreach (var child in elem.Children)
                    next.Push(child);
            }

            return null;
        }

        /// <summary>
        /// Find an element by its name
        /// </summary>
        /// <param name="name">The name of the element to search for</param>
        /// <returns>The first element found or null if no element found with the specified name</returns>
        public Static FindElementByName(string name, bool caseSensitive = true)
        {
            var parent = this;
            while (parent.Parent != null)
                parent = parent.Parent;

            var next = new Stack<Static>();
            next.Push(parent);

            while (next.Count > 0)
            {
                var elem = next.Pop();
                if (elem.Name != null &&
                    elem.Name.Equals(name, caseSensitive ? System.StringComparison.Ordinal : System.StringComparison.OrdinalIgnoreCase))
                    return elem;

                foreach (var child in elem.Children)
                    next.Push(child);
            }

            return null;
        }

        /// <summary>
        /// Add specific behavior before <see cref="OnClick"/> is called. Called by Update()
        /// </summary>
        /// <param name="args">Click event args passed from Update. Forwarded to <see cref="OnClick"/></param>
        protected virtual void BeforeClick(ClickEventArgs args)
        {
            OnClick?.Invoke(this, args);
            //todo: evaluate removing this and only using event handlers
        }
        protected virtual void BeforePress(ClickEventArgs args) { }

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

            if (HasFocus)
            {
                if (!ignoreTabKey && Input.InputState.IsPress(Keys.Tab))
                {
                    if (Input.InputState.IsMod(Input.KeyMod.Shift))
                        FocusPrevious();
                    else
                        FocusNext();
                    return false;
                }
                if ((!ignoreEnterKey && Input.InputState.IsPress(Keys.Enter)) ||
                    (!ignoreSpaceKey && Input.InputState.IsPress(Keys.Space)))
                {
                    BeforeClick(new ClickEventArgs { position = Vector2.Zero });
                    return false;
                }
            }

            var mouse = Input.InputState.MousePoint;

            if (Input.InputState.IsPress(Input.MouseButtons.Left) && AbsoluteBounds.Contains(mouse))
            {
                BeforePress(new ClickEventArgs { position = (mouse - AbsoluteBounds.Location).ToVector2() });

                didPress = true;
                if (CanFocus)
                {
                    HasFocus = true;
                    return false;
                }
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
            if (DrawBoundingRects)
                Graphics.Primitives2D.DrawRect(spriteBatch, new Color(Color.Crimson, 0.35f), AbsoluteBounds);

            if (HasFocus)
                Graphics.Primitives2D.DrawRect(spriteBatch, FocusOutlineColor, AbsoluteBounds);

            if (Font != null)
            {
                var textBounds = CenterInRect(textSize, AbsoluteBounds);
                Font.Draw(spriteBatch, Text, textBounds, Color);
            }

            foreach (var child in Children)
                child.Draw(spriteBatch);
        }

        /// <summary>
        /// Caluclate a rectangle where the region is centered inside bounds
        /// </summary>
        /// <param name="size">The size of the region</param>
        /// <param name="bounds">The bounds to center inside</param>
        /// <returns>The absolute bounds of the region rect</returns>
        protected static Rectangle CenterInRect(Vector2 size, Rectangle bounds)
        {
            var minSize = new Point(
                MathHelper.Min(bounds.Width, (int)size.X),
                MathHelper.Min(bounds.Height, (int)size.Y)
            );
            return new Rectangle(
                bounds.X + ((bounds.Width - minSize.X) / 2),
                bounds.Y + ((bounds.Height - minSize.Y) / 2),
                minSize.X,
                minSize.Y
            );
        }

        protected void DerivedDeserialize(Dictionary<string, object> props)
        {
            if (props.TryGetValue("AutoSize", out var autoSize))
            {
                if (autoSize is int autoSizeValue)
                    AutoSize(autoSizeValue);
                else if (autoSize is bool doAutoSize)
                    AutoSize();
            }
            else if (!(props.ContainsKey("Bounds") ||
                       props.ContainsKey("Size")))
                AutoSize();

            if (props.TryGetValue("Width", out var width))
                Size = new Vector2(Data.Serializer.CastType<float>(width), Size.Y);

            if (props.TryGetValue("Height", out var height))
                Size = new Vector2(size.X, Data.Serializer.CastType<float>(height));
        }

        public override string ToString()
        {
            return $"{base.ToString()}: \"{Name ?? "(No name)"}\"";
        }
    }
}
