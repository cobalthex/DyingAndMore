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
        End,
        Stretch //Special case, overrides position and size
    }

    public class ClickEventArgs : System.EventArgs
    {
        /// <summary>
        /// The relative position of the click inside the element
        /// If activated via keyboard, this is Zero
        /// </summary>
        public Vector2 position;
    }

    //todo: invalidation/dirty states, instead of reflow each time property is updated, mark dirty. On next update, reflow if dirty

    /// <summary>
    /// The basic UI element
    /// </summary>
    public class Static : Data.IDerivedDeserialize
    {
        /// <summary>
        /// A font to use for drawing debug info.
        /// If null, debug info is not drawn
        /// </summary>
        public static Graphics.BitmapFont DebugFont = null;

        /// <summary>
        /// The default font used for elements
        /// </summary>
        public static Graphics.BitmapFont DefaultFont = null;
        /// <summary>
        /// The default color used for element text
        /// </summary>
        public static Color DefaultColor = Color.White;

        /// <summary>
        /// The color to use when drawing the focus rectangle around the focused element
        /// </summary>
        public static Color FocusedBorderColor = Color.RoyalBlue;

        /// <summary>
        /// A unique name for this element. Can be null
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// The text of the element. Can be null or empty
        /// </summary>
        public virtual string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    textSize = Font?.MeasureString(_text) ?? Vector2.One;
                }
            }
        }
        private string _text = "";
        protected Vector2 textSize;

        /// <summary>
        /// The font to draw the text of this element with.
        /// Optional if text is null
        /// </summary>
        public virtual Graphics.BitmapFont Font
        {
            get => _font;
            set
            {
                if (_font != value)
                {
                    _font = value;
                    textSize = _font?.MeasureString(_text) ?? Vector2.One;
                }
            }
        }
        private Graphics.BitmapFont _font = DefaultFont;

        /// <summary>
        /// The color of this element. Usage varies between element types
        /// Usually applies to text color
        /// </summary>
        public virtual Color Color { get; set; } = DefaultColor;

        /// <summary>
        /// The color to draw the outline with, by default, transparent
        /// </summary>
        public virtual Color BorderColor { get; set; } = Color.Transparent;

        /// <summary>
        /// An optional fill color for this element, by default, transparent
        /// </summary>
        public virtual Color BackgroundColor { get; set; } = Color.Transparent;

        /// <summary>
        /// How this element is positioned in its container horizontally
        /// </summary>
        public Alignment HorizontalAlignment
        {
            get => _horizontalAlignment;
            set
            {
                if (_horizontalAlignment != value)
                {
                    _horizontalAlignment = value;
                    Reflow();
                }
            }
        }
        private Alignment _horizontalAlignment;

        /// <summary>
        /// How this element is positioned in its container vertically
        /// </summary>
        public Alignment VerticalAlignment
        {
            get => _verticalAlignment;
            set
            {
                if (_verticalAlignment != value)
                {
                    _verticalAlignment = value;
                    Reflow();
                }
            }
        }
        private Alignment _verticalAlignment;

        /// <summary>
        /// The position relative to the orientation.
        /// Start moves down and to the right
        /// Center moves down and to the right from the center
        /// End moves in the opposite direction
        /// </summary>
        /// <remarks>Overriden if Alignment.Stretch is used</remarks>
        [Data.Serializer.ReadOnly]
        public Vector2 Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    Reflow();
                }
            }
        }
        private Vector2 _position = Vector2.Zero;

        /// <summary>
        /// The size of the element
        /// </summary>
        /// <remarks>Overriden if Alignment.Stretch is used</remarks>
        [Data.Serializer.ReadOnly]
        public Vector2 Size
        {
            get => _size;
            set
            {
                if (_size != value)
                {
                    _size = value;
                    ResizeAndReflow();
                }
            }
        }
        private Vector2 _size = Vector2.One;

        /// <summary>
        /// The bounds of this static, calculated from <see cref="Position"/> and <see cref="Size"/>
        /// </summary>
        /// <seealso cref="VirtualBounds"/>
        /// <seealso cref="VisibleBounds"/>
        public Rectangle Bounds
        {
            get => new Rectangle(Position.ToPoint(), Size.ToPoint());
            set
            {
                if (Bounds != value)
                {
                    _position = value.Location.ToVector2();
                    _size = value.Size.ToVector2();
                    ResizeAndReflow();
                }
            }
        }

        /// <summary>
        /// Bounds relative to the outermost container (can be outside the parent)
        /// </summary>
        /// <seealso cref="VisibleBounds"/>
        [Data.Serializer.Ignored]
        public Rectangle VirtualBounds
        {
            get;
            private set;
        }
        /// <summary>
        /// <see cref="VirtualBounds"/> clipped to the parent
        /// </summary>
        [Data.Serializer.Ignored]
        protected Rectangle VisibleBounds
        {
            get;
            private set;
        }

        /// <summary>
        /// Does this element currently have focus?
        /// </summary>
        public bool HasFocus
        {
            get => _hasFocus && Runtime.HasFocus;
            set
            {
                if (value == true)
                {
                    Stack<Static> defocusing = new Stack<Static>();

                    //defocus all elements in tree
                    Static next = this;
                    while (next._parent != null)
                        next = next._parent;

                    defocusing.Push(next);
                    while (defocusing.Count > 0)
                    {
                        next = defocusing.Pop();
                        next._hasFocus = false;

                        foreach (var child in next.children)
                            defocusing.Push(child);
                    }
                }

                _hasFocus = value;
            }
        }
        private bool _hasFocus = false;

        /// <summary>
        /// Disallows input to elements below this one in the tree
        /// </summary>
        public bool IsModal { get; set; } = false;

        /// <summary>
        /// Can this element be focused
        /// </summary>
        [Data.Serializer.Ignored]
        public virtual bool CanFocus { get => Click != null; }

        /// <summary>
        /// Called whenever the element is pressed.
        /// </summary>
        [Data.Serializer.Ignored]
        public event System.EventHandler<ClickEventArgs> Press = null;
        protected virtual void OnPress(ClickEventArgs e) { }

        /// <summary>
        /// Called whenever the element is clicked (mouse just released).
        /// By default, whether or not there is a click handler determines if this is focusable
        /// </summary>
        [Data.Serializer.Ignored]
        public event System.EventHandler<ClickEventArgs> Click = null;
        protected virtual void OnClick(ClickEventArgs e) { }

        /// <summary>
        /// Called whenever the size of this element is updated
        /// </summary>
        [Data.Serializer.Ignored]
        public event System.EventHandler Resize = null;
        protected virtual void OnResize(System.EventArgs e) { }

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
        /// Was the current (left) mouse press inside this element
        /// </summary>
        private bool didPress = false;

        /// <summary>
        /// Was the mouse pressed inside this static (and is the mouse still down)
        /// </summary>
        /// <returns>True if the mouse is currently down and was pressed inside this static</returns>
        protected bool DidPressInside() =>
            didPress && Input.InputState.IsButtonDown(Input.MouseButtons.Left);

        /// <summary>
        /// Who owns/contains this element
        /// </summary>
        [Data.Serializer.Ignored]
        public Static Parent
        {
            get => _parent;
            protected set
            {
                if (_parent != value)
                {
                    _parent = value;
                    Reflow();
                }
            }
        }
        private Static _parent = null;

        /// <summary>
        /// A readonly collection of all of the children in this element
        /// </summary>
        [Data.CustomDeserialize(typeof(Static), "DeserializeChildren")]
        public ReadOnlyCollection<Static> Children { get; private set; } //todo: maybe observable
        private List<Static> children = new List<Static>();

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
        /// Create a clone of this static and all of its children
        /// Does not add to parent
        /// </summary>
        /// <returns>The cloned static</returns>
        public Static Clone()
        {
            var clone = CloneSelf();
            clone._parent = null;
            Stack<Static> clones = new Stack<Static>(new []{ clone });
            while (clones.Count > 0)
            {
                var top = clones.Pop();
                for (int i = 0; i < top.children.Count; ++i)
                {
                    var child = top.children[i].CloneSelf();
                    child._parent = top;
                    top.children[i] = child;
                    if (child.children.Count > 0)
                        clones.Push(child);
                }
            }
            return clone;
        }

        /// <summary>
        /// Clone this item, should not modify parent or children
        /// </summary>
        /// <returns>The cloned item</returns>
        protected virtual Static CloneSelf()
        {
            return (Static)MemberwiseClone();
        }

        /// <summary>
        /// Remove this element from its parent. If parent is null, does nothing
        /// </summary>
        /// <returns>True if the element was removed from its parent or false if parent was null</returns>
        public bool RemoveFromParent()
        {
            if (Parent != null)
            {
                Parent.RemoveChild(this);
                return true;
            }
            return false;
        }

        public virtual void AddChild(Static child)
        {
            if (child.Parent == this)
                return;

            if (child.Parent != null)
                child.RemoveFromParent();

            //todo: come up with a common interface for modifying children
            child._parent = this;
            children.Add(child);
            if (child.HasFocus)
                child.HasFocus = true;
            Reflow();
        }

        /// <summary>
        /// Replace the child at a specific index
        /// </summary>
        /// <param name="child">the child to replace with</param>
        /// <param name="index">the index of the child to replace. Throws if out of range</param>
        public virtual void ReplaceChild(Static child, int index)
        {
            if (children[index] != null)
                children[index].Parent = null;

            child._parent = this;
            children[index] = child;
            if (child.HasFocus)
                child.HasFocus = true;
            Reflow();
        }

        public virtual void InsertChild(Static child, int index = 0)
        {
            if (child.Parent == this)
                return;

            child._parent = this;
            children.Insert(index, child);
            if (child.HasFocus) //re-apply throughout tree
                child.HasFocus = true;
            Reflow();
        }

        public void AddChildren(params Static[] children)
        {
            //todo: see below
            Static lastFocus = null;
            foreach (var child in children)
            {
                if (child.Parent == this)
                    continue;

                child._parent = this;
                this.children.Add(child);
                if (child.HasFocus)
                    lastFocus = child;
            }

            if (lastFocus != null)
                lastFocus.HasFocus = true;

            Reflow();
        }

        public virtual void AddChildren(IEnumerable<Static> children)
        {
            //todo: add disable reflow and then switch this to use InsertChild

            Static lastFocus = null;
            foreach (var child in children)
            {
                if (child.Parent == this)
                    continue;

                child._parent = this;
                this.children.Add(child);
                if (child.HasFocus)
                    lastFocus = child;
            }

            if (lastFocus != null)
                lastFocus.HasFocus = true;

            Reflow();
        }

        /// <summary>
        /// Remove an element from this element. Does not search children
        /// </summary>
        /// <param name="child"></param>
        public void RemoveChild(Static child)
        {
            RemoveChildAt(children.IndexOf(child));
        }

        public virtual Static RemoveChildAt(int index)
        {
            if (index < 0 || index >= children.Count)
                return null;

            var child = children[index];
            children.RemoveAt(index);
            child._parent = null;
            return child;
        }

        public void RemoveAllChildren()
        {
            foreach (var child in children)
                child._parent = null;
            children.Clear();
        }

        public void ReplaceAllChildren(params Static[] newChildren)
        {
            RemoveAllChildren();
            AddChildren(newChildren);
        }
        public void ReplaceAllChildren(IEnumerable<Static> newChildren)
        {
            RemoveAllChildren();
            AddChildren(newChildren);
        }

        /// <summary>
        /// Focus the next element in the tree
        /// </summary>
        /// <remarks>If this is not the focused element, finds the focused element and calls this function</remarks>
        protected void FocusNext()
        {
            if (!HasFocus)
            {
                FindFocused()?.FocusNext();
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

                while (next._parent != null)
                {
                    var index = next.Parent.Children.IndexOf(next) + 1;
                    if (index < next.Parent.Children.Count)
                    {
                        next = next.Parent.Children[index];
                        break;
                    }
                    else
                        next = next._parent;
                }

                if (next.CanFocus)
                {
                    next.HasFocus = true;
                    return;
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
                FindFocused()?.FocusPrevious();
                return;
            }

            var prev = this;
            while (true)
            {
                if (prev._parent == null)
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
                        prev = prev._parent;
                }


                if (prev.CanFocus)
                {
                    prev.HasFocus = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Find the first element that can focus and focus it, starting at this element
        /// Only traverses down
        /// </summary>
        /// <returns>The focused element, null if none</returns>
        public Static FocusFirstAvailable()
        {
            var next = new Queue<Static>();
            next.Enqueue(this);

            while (next.Count > 0)
            {
                var elem = next.Dequeue();
                if (elem.CanFocus)
                {
                    elem.HasFocus = true;
                    return elem;
                }

                foreach (var child in elem.Children)
                    next.Enqueue(child);
            }

            return null;
        }

        /// <summary>
        /// Find the element in this tree that has focus (recursively)
        /// </summary>
        /// <returns>The focused element, or null if there is none</returns>
        public Static FindFocused()
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
        /// Find a child element by its name (recursively)
        /// </summary>
        /// <param name="name">The name of the UI to search for</param>
        /// <returns>The first child found or null if none found with the specified name</returns>
        public Static FindChildByName(string name, bool caseSensitive = false)
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
        /// Reflow child elements relative to this element
        /// Called whenever this element's position or size is adjusted
        /// </summary>
        public virtual void Reflow()
        {
            if (_parent != null &&
                (HorizontalAlignment == Alignment.Stretch ||
                VerticalAlignment == Alignment.Stretch))
            {
                if (HorizontalAlignment == Alignment.Stretch)
                {
                    _position.X = 0;
                    _size.X = _parent._size.X;
                }
                if (VerticalAlignment == Alignment.Stretch)
                {
                    _position.Y = 0;
                    _size.Y = _parent._size.Y;
                }
                CalculateBounds();

                OnResize(System.EventArgs.Empty);
                Resize?.Invoke(this, System.EventArgs.Empty);
            }
            else
                CalculateBounds();

            foreach (var child in Children)
                child.Reflow();
        }

        protected void ResizeAndReflow()
        {
            Reflow();

            OnResize(System.EventArgs.Empty);
            Resize?.Invoke(this, System.EventArgs.Empty);
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

        protected void CalculateBounds()
        {
            VirtualBounds = Parent != null
                ? CalculateBounds(Parent.VirtualBounds)
                : new Rectangle(Position.ToPoint(), Size.ToPoint());
            VisibleBounds = Parent != null
                ? Rectangle.Intersect(Parent.VisibleBounds, VirtualBounds)
                : VirtualBounds;
        }

        /// <summary>
        /// Automatically size this element. By default, will size based on text
        /// This does not affect the position of the element
        /// Padding affects child positioning
        /// </summary>
        public virtual void AutoSize(float padding = 0)
        {
            //todo: maybe make padding an actual property
            var bounds = new Rectangle(Position.ToPoint(), textSize.ToPoint());
            foreach (var child in Children)
            {
                if (child.HorizontalAlignment != Alignment.Middle)
                    child.Position += new Vector2(padding, 0);
                if (child.VerticalAlignment != Alignment.Middle)
                    child.Position += new Vector2(0, padding);
                bounds = Rectangle.Union(bounds, child.Bounds);
            }
            Size = bounds.Size.ToVector2() + new Vector2(padding);
        }

        /// <summary>
        /// Update this element and all of its children
        /// </summary>
        /// <param name="time">Game time</param>
        public virtual void Update(GameTime time)
        {
            /* update in the following order: H G F E D C B A
            A
                B
                    C
                    D
                E
                    F
                        G
                    H
            */

            var toUpdate = this;
            while (toUpdate.children.Count > 0)
                toUpdate = toUpdate.children[toUpdate.children.Count - 1];

            bool handleInput = true;
            while (true)
            {
                if (handleInput)
                    handleInput = toUpdate.HandleInput(time) && !toUpdate.IsModal;

                toUpdate.UpdateSelf(time);

                //stop at this element
                if (toUpdate._parent == null || toUpdate == this)
                    break;

                var index = toUpdate.Parent.Children.IndexOf(toUpdate) - 1;
                if (index >= 0)
                {
                    toUpdate = toUpdate.Parent.Children[index];

                    while (toUpdate.children.Count > 0)
                        toUpdate = toUpdate.children[toUpdate.children.Count - 1];
                }
                else
                    toUpdate = toUpdate._parent;
            }
        }

        /// <summary>
        /// Update this UI's state here. Input should be handled in <see cref="HandleInput"/>
        /// </summary>
        /// <param name="time">game time</param>
        protected virtual void UpdateSelf(GameTime time) { }

        /// <summary>
        /// React to user input here. Updating should be performed in <see cref="UpdateSelf"/>
        /// </summary>
        /// <param name="time">game time</param>
        /// <returns>False if the input has been handled by this UI</returns>
        protected virtual bool HandleInput(GameTime time)
        {
            //todo: maybe move to pre-update (and have pre-update override updateSelf)
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
                    var e = new ClickEventArgs { position = Vector2.Zero };
                    OnClick(e);
                    Click?.Invoke(this, e);
                    return false;
                }
            }

            var mouse = Input.InputState.MousePoint;

            if (Input.InputState.IsPress(Input.MouseButtons.Left) && VisibleBounds.Contains(mouse))
            {
                var e = new ClickEventArgs { position = (mouse - VisibleBounds.Location).ToVector2() };
                didPress = true;
                OnPress(e);
                Press?.Invoke(this, e);

                if (CanFocus)
                {
                    HasFocus = true;
                    return false;
                }
            }

            //input capture
            //todo: maybe add setting
            else if (DidPressInside())
                return false;

            else if (Input.InputState.IsButtonUp(Input.MouseButtons.Left))
            {
                if (didPress && VisibleBounds.Contains(mouse))
                {
                    var e = new ClickEventArgs { position = (mouse - VisibleBounds.Location).ToVector2() };
                    OnClick(e);
                    Click?.Invoke(this, e);
                    didPress = false;
                    return false;
                }
                didPress = false;
            }

            return true;
        }

        /// <summary>
        /// Draw this element, its decorators, and any children
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to use</param>
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            var draws = new Queue<Static>(Children.Count + 1);
            draws.Enqueue(this);

            while (draws.Count > 0)
            {
                var toDraw = draws.Dequeue();

                Graphics.Primitives2D.DrawFill(spriteBatch, toDraw.BackgroundColor, toDraw.VisibleBounds);
                toDraw.DrawSelf(spriteBatch);
                //Graphics.Primitives2D.DrawRect(spriteBatch, Color.Tomato, toDraw.VirtualBounds);
                Graphics.Primitives2D.DrawRect(spriteBatch, toDraw.HasFocus ? FocusedBorderColor : toDraw.BorderColor, toDraw.VisibleBounds);

                if (DebugFont != null && toDraw.VisibleBounds.Contains(Input.InputState.MousePoint))
                {
                    var rect = toDraw.VisibleBounds;
                    rect.Inflate(1, 1);
                    Graphics.Primitives2D.DrawRect(spriteBatch, Color.Gold, rect);

                    string info = $"Name: {(Name ?? "(No name)")}\nBounds: {rect}";
                    DebugFont.Draw(spriteBatch, info, (rect.Location + rect.Size).ToVector2(), Color.Gold);
                }

                foreach (var child in toDraw.Children)
                    draws.Enqueue(child);
            }
        }

        /// <summary>
        /// Draw only this item (no children)
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to use</param>
        protected virtual void DrawSelf(SpriteBatch spriteBatch)
        {
            if (Font != null)
            {
                var textPos = ((_size - textSize) / 2).ToPoint();
                DrawText(spriteBatch, textPos);
            }
        }

        /// <summary>
        /// Draw this element's text. Position specifies the base location
        /// </summary>
        /// <param name="position">The position of the text relative to this element</param>
        protected void DrawText(SpriteBatch spriteBatch, Point position)
        {
            position += VirtualBounds.Location - VisibleBounds.Location;
            Font.Draw(spriteBatch, Text, 0, Text.Length, VisibleBounds, position, Color);
        }

        public void DerivedDeserialize(Dictionary<string, object> props)
        {
            if (props.TryGetValue("AutoSize", out var autoSize))
            {
                if (autoSize is System.Int64 autoSizeInt)
                    AutoSize(autoSizeInt);
                else if (autoSize is double autoSizeFloat)
                    AutoSize((float)autoSizeFloat);
                else if (autoSize is bool doAutoSize)
                    AutoSize();
            }
            else if (!(props.ContainsKey("Bounds") ||
                       props.ContainsKey("Size")))
                AutoSize();

            if (props.TryGetValue("Width", out var width))
                Size = new Vector2(Data.Serializer.Cast<float>(width), Size.Y);

            if (props.TryGetValue("Height", out var height))
                Size = new Vector2(_size.X, Data.Serializer.Cast<float>(height));
        }

        public override string ToString()
        {
            return $"{base.ToString()} \"{Name ?? "(No name)"}\"{(HasFocus ? " *" : "")}";
        }

        /// <summary>
        /// Convert a member name to a more english-friendly name
        /// This includes adding spaces and correct capitalization
        /// </summary>
        /// <param name="name">the member name to convert</param>
        /// <returns>The beautified name</returns>
        public static string BeautifyMemberName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            var builder = new System.Text.StringBuilder(name.Length + 4);
            builder.Append(char.ToUpper(name[0]));
            for (int i = 1; i < name.Length; ++i)
            {
                if (char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                {
                    builder.Append(' ');
                    builder.Append(char.ToLower(name[i]));
                }
                else
                    builder.Append(name[i]);
            }

            return builder.ToString();
        }

        public static Static GeneratePropSheet(object obj, Graphics.BitmapFont font, Color color)
        {
            var root = new List() { Margin = 2 };
            var maxWidth = 0f;

            var type = obj.GetType();

            if (type.IsEnum)
            {
                var @enum = obj as System.Enum;
                var enumValues = System.Enum.GetNames(type);
                foreach (var flag in enumValues)
                {
                    var value = (System.Enum)System.Enum.Parse(type, flag);
                    if (System.Convert.ToUInt64(value) != 0)
                    {
                        var check = new CheckBox()
                        {
                            Name = flag,
                            Text = BeautifyMemberName(flag),
                            Font = font,
                            Color = color,
                            IsChecked = @enum.HasFlag(value)
                        };
                        check.Click += delegate (object sender, ClickEventArgs e)
                        {
                            var chkbx = (CheckBox)sender;
                            var parsed = System.Convert.ToUInt64(System.Enum.Parse(type, chkbx.Name));
                            var n = System.Convert.ToUInt64(@enum);

                            if (chkbx.IsChecked)
                                obj = System.Enum.ToObject(type, n | parsed);
                            else
                                obj = System.Enum.ToObject(type, n & ~parsed);

                            //todo: doesn't work
                        };
                        check.AutoSize();
                        root.AddChild(check);
                        maxWidth = MathHelper.Max(maxWidth, check._size.X);
                    }
                }
                root._size = new Vector2(maxWidth, 1);
                root.AutoSize();
                return root;
            }

            //todo: item spacing (can't use list margin without nesting)

            var members = type.GetMembers(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            //todo: move these into type handlers
            foreach (var member in members)
            {
                System.Type memberType;
                object curValue;
                System.Action<object, object> setValue;

                if (member is System.Reflection.FieldInfo fInfo)
                {
                    memberType = fInfo.FieldType;
                    curValue = fInfo.GetValue(obj);
                    setValue = fInfo.SetValue;
                }
                else if (member is System.Reflection.PropertyInfo pInfo)
                {
                    if (!pInfo.CanWrite)
                        continue;

                    memberType = pInfo.PropertyType;
                    curValue = pInfo.GetValue(obj);
                    setValue = pInfo.SetValue;
                }
                else
                    continue;

                Static label = null;

                if (memberType != typeof(bool))
                {
                    label = new Static()
                    {
                        Text = BeautifyMemberName(member.Name),
                        Font = font,
                        Color = color
                    };
                    label.AutoSize();
                    root.AddChild(label);

                    maxWidth = MathHelper.Max(maxWidth, label._size.X);
                }

                if (memberType == typeof(bool))
                {
                    var check = new CheckBox()
                    {
                        Name = member.Name,
                        Text = BeautifyMemberName(member.Name),
                        Font = font,
                        Color = color
                    };
                    check.Click += delegate
                    {
                        setValue(obj, check.IsChecked);
                    };
                    check.AutoSize();
                    root.AddChild(check);
                    maxWidth = MathHelper.Max(maxWidth, check._size.X);
                }
                else if (memberType == typeof(int))
                {
                    var input = new NumericInput()
                    {
                        Minimum = int.MinValue,
                        Maximum = int.MaxValue,
                        Value = (int)curValue,

                        Name = member.Name,
                        HorizontalAlignment = Alignment.Stretch,
                        Font = font,
                        Color = color
                    };
                    input.ValueChanged += delegate
                    {
                        setValue(obj, (int)input.Value);
                    };
                    input.AutoSize();
                    root.AddChild(input);
                }
                else if (memberType == typeof(uint))
                {
                    var input = new NumericInput()
                    {
                        Minimum = uint.MinValue,
                        Maximum = uint.MaxValue,
                        Value = (long)curValue,

                        Name = member.Name,
                        HorizontalAlignment = Alignment.Stretch,
                        Font = font,
                        Color = color
                    };
                    input.ValueChanged += delegate
                    {
                        setValue(obj, (uint)input.Value);
                    };
                    input.AutoSize();
                    root.AddChild(input);
                }
                else if (memberType == typeof(long))
                {
                    var input = new NumericInput()
                    {
                        Minimum = long.MinValue,
                        Maximum = long.MaxValue,
                        Value = (long)curValue,

                        Name = member.Name,
                        HorizontalAlignment = Alignment.Stretch,
                        Font = font,
                        Color = color
                    };
                    input.ValueChanged += delegate
                    {
                        setValue(obj, input.Value);
                    };
                    input.AutoSize();
                    root.AddChild(input);
                }
                else if (memberType == typeof(string))
                {
                    //todo: file input where applicable (maybe switch vars to use FileInfo class)
                    var input = new TextInput()
                    {
                        Name = member.Name,
                        Text = (string)curValue,
                        HorizontalAlignment = Alignment.Stretch,
                        Font = font,
                        Color = color
                    };
                    input.TextChanged += delegate
                    {
                        setValue(obj, input.Text);
                    };
                    input.AutoSize();
                    root.AddChild(input);
                }
                else if (memberType == typeof(System.IO.FileInfo))
                {
                    var input = new FileInput()
                    {
                        Name = member.Name,
                        Text = ((System.IO.FileInfo)curValue)?.Name,
                        HorizontalAlignment = Alignment.Stretch,
                        Font = font,
                        Color = color
                    };
                    input.FileSelected += delegate
                    {
                        setValue(obj, new System.IO.FileInfo(input.Text));
                    };
                    input.AutoSize();
                    root.AddChild(input);
                }
                else if (memberType == typeof(Texture2D))
                {
                    var input = new FileInput()
                    {
                        Name = member.Name,
                        Text = ((Texture2D)curValue)?.Name,
                        HorizontalAlignment = Alignment.Stretch,
                        Font = font,
                        Color = color
                    };
                    input.FileSelected += delegate
                    {
                        setValue(obj, Takai.Data.Cache.Load<Texture2D>(input.Text));
                    };
                    input.AutoSize();
                    root.AddChild(input);
                }
                else if (memberType == typeof(System.TimeSpan))
                {
                    var input = new NumericInput()
                    {
                        Minimum = long.MinValue,
                        Maximum = long.MaxValue,
                        Value = (long)((System.TimeSpan)curValue).TotalMilliseconds,

                        Name = member.Name,
                        HorizontalAlignment = Alignment.Stretch,
                        Font = font,
                        Color = color,
                    };
                    input.ValueChanged += delegate
                    {
                        setValue(obj, System.TimeSpan.FromMilliseconds(input.Value));
                    };
                    input.AutoSize();

                    var mSecLabel = new Static()
                    {
                        Text = "msec",
                        Font = font,
                        Color = color
                    };
                    mSecLabel.AutoSize();

                    var container = new List()
                    {
                        Direction = Direction.Horizontal,
                        HorizontalAlignment = Alignment.Stretch,
                        Margin = 10
                    };
                    container.AddChildren(
                        input,
                        mSecLabel
                    );
                    container.AutoSize();
                    root.AddChild(container);
                }
                else
                {
                    label.Text += $"\n`aab--- ({memberType.Name}) ---\nthird line`x";
                    label.AutoSize();
                }
            }

            root._size = new Vector2(maxWidth, 1);
            root.AutoSize();
            return root;
        }
    }
}
