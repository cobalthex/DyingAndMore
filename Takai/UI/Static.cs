using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;

//todo: on font/text changed, autosize

namespace Takai.UI
{
    /// <summary>
    /// How an element aligns itself inside its parent
    /// </summary>
    public enum Alignment
    {
        Start,
        Middle,
        End,
        Stretch, //Special case, overrides position and size

        Left = Start,
        Top = Start,

        Right = End,
        Bottom = End,
    }

    public class ClickEventArgs : System.EventArgs
    {
        /// <summary>
        /// The relative position of the click inside the element
        /// If activated via keyboard, this is Zero
        /// </summary>
        public Vector2 position;

        public int inputIndex;
        //input device
    }

    public class ParentChangedEventArgs : System.EventArgs
    {
        public Static Previous { get; set; }

        public ParentChangedEventArgs(Static previousParent)
        {
            Previous = previousParent;
        }
    }

    /// <summary>
    /// Command handler for commands issued from UI
    /// These are called after event handles
    /// </summary>
    /// <param name="source">The source UI object issuing the command</param>
    public delegate void Command(Static source);

    //todo: invalidation/dirty states, instead of reflow each time property is updated, mark dirty. On next update, reflow if dirty

    /// <summary>
    /// The basic UI element
    /// </summary>
    public class Static : Data.IDerivedDeserialize
    {
        #region Properties

        //private static int idCounter = 0;
        //private readonly int id = (++idCounter);

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

        [Data.Serializer.Ignored]
        public object UserData { get; set; } = null;

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
        private string _text;
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
                    textSize = _font?.MeasureString(Text) ?? Vector2.One;
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

        public virtual Graphics.Sprite BackgroundSprite { get; set; }

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

        public Vector2 Padding
        {
            get => _padding;
            set
            {
                if (_padding != value)
                {
                    _padding = value;
                    ResizeAndReflow();
                }
            }
        }
        private Vector2 _padding = Vector2.Zero;

        /// <summary>
        /// The dimensions of this element, calculated from <see cref="Position"/> and <see cref="Size"/>
        /// Does not account for padding or alignment
        /// </summary>
        /// <seealso cref="AbsoluteDimensions"/>
        /// <seealso cref="VisibleBounds"/>
        [Data.Serializer.Ignored]
        public Rectangle LocalDimensions
        {
            get => new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
            set
            {
                if (LocalDimensions != value)
                {
                    _position = new Vector2(value.X, value.Y);
                    _size = new Vector2(value.Width, value.Height);
                    ResizeAndReflow();
                }
            }
        }

        /// <summary>
        /// dimensions relative to the outermost container (can be outside the parent)
        /// This is adjusted by (but does not include) <see cref="Padding"/>
        /// This also includes alignment
        /// This should be used when drawing inside the element
        /// </summary>
        /// <seealso cref="VisibleBounds"/>
        [Data.Serializer.Ignored]
        public Rectangle AbsoluteDimensions
        {
            get;
            private set;
        }

        /// <summary>
        /// <see cref="AbsoluteDimensions"/> including padding
        /// This should be used when determining the inclusive size of an element
        /// </summary>
        [Data.Serializer.Ignored]
        public Rectangle AbsoluteBounds
        {
            get;
            private set;
        }

        /// <summary>
        /// <see cref="AbsoluteBounds"/> clipped to the parent container
        /// </summary>
        [Data.Serializer.Ignored]
        protected Rectangle VisibleBounds
        {
            get;
            private set;
        }

        /// <summary>
        /// Automatically size this element whenever its text changes or a child element changes
        /// Does not resize if bounds change but will overwrite any changes on next size
        /// </summary>
        public bool AutoSize { get; set; } = false;

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
                    while (next.Parent != null)
                        next = next.Parent;

                    defocusing.Push(next);
                    while (defocusing.Count > 0)
                    {
                        next = defocusing.Pop();
                        next._hasFocus = false;

                        foreach (var child in next.Children)
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
        /// Is this element visible and updating. if false, does not take part in reflow/updating/darwing/etc
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        #region Events

        /// <summary>
        /// Can this element be focused
        /// </summary>
        [Data.Serializer.Ignored]
        public virtual bool CanFocus { get => Click != null || clickCommandFn != null; }

        /// <summary>
        /// Called whenever the element has its parent changed
        /// </summary>
        public event System.EventHandler<ParentChangedEventArgs> ParentChanged = null;
        protected virtual void OnParentChanged(ParentChangedEventArgs e) { }

        /// <summary>
        /// Called whenever the element is pressed.
        /// </summary>
        public event System.EventHandler<ClickEventArgs> Press = null;
        protected virtual void OnPress(ClickEventArgs e) { }

        /// <summary>
        /// Called whenever the element is clicked (mouse just released).
        /// By default, whether or not there is a click handler determines if this is focusable
        /// </summary>
        public event System.EventHandler<ClickEventArgs> Click = null;
        protected virtual void OnClick(ClickEventArgs e) { }

        public event System.EventHandler Resize = null;
        protected virtual void OnResize(System.EventArgs e) { }

        public string OnClickCommand { get; set; }
        protected Command clickCommandFn;

        #endregion

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
        protected bool DidPressInside(Input.MouseButtons button) =>
            didPress && Input.InputState.IsButtonDown(button);

        /// <summary>
        /// Who owns/contains this element
        /// </summary>
        [Data.Serializer.Ignored]
        public Static Parent
        {
            get => _parent;
            protected set
            {
                if (_parent == value)
                    return;

                SetParentNoReflow(value);
                Reflow();
            }
        }
        private Static _parent = null;

        /// <summary>
        /// A readonly collection of all of the children in this element (including disabled children)
        /// </summary>
        [Data.CustomDeserialize(typeof(Static), "DeserializeChildren")]
        public ReadOnlyCollection<Static> Children { get; private set; } //todo: maybe observable
        private List<Static> children = new List<Static>();

        /// <summary>
        /// Bind properties of <see cref="BindingSource"/> to properties of this UI element
        /// Any modifications to this list will require rebinding
        /// </summary>
        public List<Data.Binding> Bindings { get; set; }

        #endregion

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
        /// Create a simple static label. Calls <see cref="AutoSize(float)"/>
        /// </summary>
        /// <param name="text">The text to set</param>
        public Static(string text)
            : this()
        {
            Text = text;
            SizeToContain();
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
        /// Bind this UI element to an object
        /// </summary>
        /// <param name="source">The source object for the bindings</param>
        /// <param name="recursive">Recurse through all children and set their source aswell</param>
        public void BindTo(object source, bool recursive = true)
        {
            if (recursive)
            {
                foreach (var elem in EnumerateRecursive())
                    elem.BindToThis(source);
            }
            else
                BindToThis(source);
        }

        protected virtual void BindToThis(object source)
        {
            if (Bindings == null)
                return;

            foreach (var binding in Bindings)
                binding.BindTo(source, this);
        }

        /// <summary>
        /// Bind this (and/or children) to a command on click/submit/etc
        /// </summary>
        /// <param name="command">The command to bind to</param>
        /// <param name="commandFn">The function to call when the command is triggered</param>
        /// <param name="recursive">Bind this command to any children?</param>
        public void BindCommand(string command, Command commandFn, bool recursive = true)
        {
            if (command == null || commandFn == null)
                return;

            if (recursive)
            {
                foreach (var elem in EnumerateRecursive())
                    elem.BindCommandToThis(command, commandFn);
            }
            else
                BindCommandToThis(command, commandFn);
        }

        protected virtual void BindCommandToThis(string command, Command commandFn)
        {
            if (OnClickCommand == command)
                clickCommandFn = commandFn;
        }

        #region Hierarchy/cloning

        private void SetParentNoReflow(Static newParent)
        {
            var changed = new ParentChangedEventArgs(_parent);
            _parent = newParent;

            OnParentChanged(changed);
            ParentChanged?.Invoke(this, changed);
        }

        /// <summary>
        /// Create a clone of this static and all of its children
        /// Does not add to parent
        /// </summary>
        /// <returns>The cloned static</returns>
        public Static Clone()
        {
            var clone = CloneSelf();
            clone.SetParentNoReflow(null);
            Stack<Static> clones = new Stack<Static>(new[] { clone });
            while (clones.Count > 0)
            {
                var top = clones.Pop();
                for (int i = 0; i < top.Children.Count; ++i)
                {
                    var child = top.Children[i].CloneSelf();
                    child.SetParentNoReflow(top);
                    top.children[i] = child;
                    if (child.Children.Count > 0)
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

        public virtual Static AddChild(Static child)
        {
            if (child.Parent == this)
                return child;

            if (child.Parent != null)
                child.RemoveFromParent();

            //todo: come up with a common interface for modifying children
            child.SetParentNoReflow(this);
            children.Add(child);
            if (child.HasFocus)
                child.HasFocus = true;
            Reflow();
            return child;
        }

        /// <summary>
        /// Replace the child at a specific index
        /// </summary>
        /// <param name="child">the child to replace with</param>
        /// <param name="index">the index of the child to replace. Throws if out of range</param>
        /// <returns>The staticadded</returns>
        public virtual Static ReplaceChild(Static child, int index)
        {
            if (children[index] != null)
                children[index].Parent = null;

            child.SetParentNoReflow(this);
            children[index] = child;
            if (child.HasFocus)
                child.HasFocus = true;
            Reflow();

            return child;
        }

        public virtual Static InsertChild(Static child, int index = 0)
        {
            if (child.Parent == this)
                return child;

            child.SetParentNoReflow(this);
            children.Insert(index, child);
            if (child.HasFocus) //re-apply throughout tree
                child.HasFocus = true;
            Reflow();

            return child;
        }

        public void AddChildren(params Static[] children)
        {
            //todo: see below
            Static lastFocus = null;
            foreach (var child in children)
            {
                if (child.Parent == this)
                    continue;

                child.SetParentNoReflow(this);
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

                child.SetParentNoReflow(this);
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
        public Static RemoveChild(Static child)
        {
            return RemoveChildAt(children.IndexOf(child));
        }

        public virtual Static RemoveChildAt(int index)
        {
            if (index < 0 || index >= children.Count)
                return null;

            var child = children[index];
            children.RemoveAt(index);
            if (child.Parent == this)
                child.SetParentNoReflow(null);
            return child;
        }

        public void RemoveAllChildren()
        {
            foreach (var child in children)
            {
                if (child.Parent == this)
                    child.SetParentNoReflow(null);
            }
            children.Clear();
        }

        /// <summary>
        /// Move all children from this element to another
        /// </summary>
        /// <param name="target">The target element to move them to</param>
        public void MoveAllChildrenTo(Static target)
        {
            target.AddChildren(children);
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
        /// Enumerate through all children and their descendents recursively (including this)
        /// </summary>
        /// <param name="includeDisabled">Include elements that havee <see cref="IsEnabled"/> set to false</param>
        /// <returns>An enumerator to all elements</returns>
        public IEnumerable<Static> EnumerateRecursive(bool includeDisabled = false)
        {
            Stack<Static> enumeration = new Stack<Static>();
            enumeration.Push(this);
            while (enumeration.Count > 0)
            {
                var top = enumeration.Pop();
                yield return top;

                foreach (var child in top.Children)
                {
                    if (child.IsEnabled)
                        enumeration.Push(child);
                }
            }
        }

        #endregion

        #region Navigation

        public Static GetRoot()
        {
            var current = this;
            while (current.Parent != null)
                current = current.Parent;
            return current;
        }

        private Static FindNextFocus()
        {
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
                foreach (var child in next.Children)
                {
                    if (child.CanFocus && child.IsEnabled)
                        return child;

                    if (child.Children.Count > 0)
                    {
                        next = child;
                        break;
                    }
                }

                if (current != next)
                    continue;

                while (next.Parent != null)
                {
                    var index = next.Parent.Children.IndexOf(next) + 1;
                    if (index < next.Parent.Children.Count)
                    {
                        next = next.Parent.Children[index];
                        break;
                    }
                    else
                        next = next.Parent;
                }

                if (next.CanFocus && next.IsEnabled)
                    return next;
            }

            return null;
        }

        /// <summary>
        /// Focus the next element in the tree
        /// </summary>
        /// <remarks>If this is not the focused element, finds the focused element and calls this function</remarks>
        protected Static FocusNext()
        {
            if (!HasFocus)
                return FindFocused()?.FocusNext();

            var next = FindNextFocus();
            if (next != null)
                next.HasFocus = true;
            return next;
        }

        /// <summary>
        /// Focus the previous element, using the reverse order of FocusNext()
        /// </summary>
        protected Static FocusPrevious()
        {
            if (!HasFocus)
                return FindFocused()?.FocusPrevious();

            var prev = this;
            while (true)
            {
                if (prev.Parent == null)
                {
                    if (prev.Children.Count == 0)
                        return null;

                    while (prev.Children.Count > 0)
                        prev = prev.Children[prev.Children.Count - 1];
                }
                else
                {
                    var index = prev.Parent.Children.IndexOf(prev) - 1;
                    if (index >= 0)
                    {
                        prev = prev.Parent.Children[index];

                        while (prev.Children.Count > 0)
                            prev = prev.Children[prev.Children.Count - 1];
                    }
                    else
                        prev = prev.Parent;
                }


                if (prev.CanFocus && prev.IsEnabled)
                {
                    prev.HasFocus = true;
                    return prev;
                }
            }
        }

        /// <summary>
        /// Focus the closest element in a specific direction
        /// If there are no elements in that direction, doesn't focus
        /// </summary>
        /// <param name="direction">The direction to search</param>
        /// <param name="bias">How wide to make the search</param>
        /// <returns>The focused element, or null if none</returns>
        public Static FocusGeographically(Vector2 direction, float bias = 0.25f)
        {
            direction *= new Vector2(1, -1);
            direction.Normalize();

            //todo: search parents incrementally outward

            var prox = new SortedList<float, Static>();

            var stack = new Stack<Static>();
            stack.Push(GetRoot());
            while (stack.Count > 0)
            {
                //todo: search up and down tree

                //sort by combined score of dot and dist (length?)

                var top = stack.Pop();
                if (top != this && top.CanFocus && top.IsEnabled)
                {
                    var diff = (top.AbsoluteDimensions.Location - AbsoluteDimensions.Location).ToVector2();

                    var dot = Vector2.Dot(direction, Vector2.Normalize(diff));
                    if (dot >= bias)
                    {
                        //todo: should be combination of dot and length
                        var mag = diff.LengthSquared();
                        prox[mag] = top;
                    }
                }
                foreach (var child in top.Children)
                    stack.Push(child);
            }

            if (prox.Count > 0)
            {
                prox.Values[0].HasFocus = true;
                return prox.Values[0];
            }
            return null;
        }

        /// <summary>
        /// Find the first element that can focus and focus it, starting at this element
        /// Only traverses down
        /// </summary>
        /// <returns>The focused element, null if none</returns>
        public Static FocusFirstAvailable()
        {

            //bfs
            //while (next.Count > 0)
            //{
            //    var elem = next.Dequeue();
            //    if (elem.CanFocus && elem.IsEnabled)
            //    {
            //        elem.HasFocus = true;
            //        return elem;
            //    }

            //    foreach (var child in elem.Children)
            //        next.Enqueue(child);
            //}

            var next = FindNextFocus();
            if (next != null)
                next.HasFocus = true;
            return next;
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
        public Static FindChildByName(string name, bool caseSensitive = false, System.Type elementType = null)
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
                    (elementType == null || elementType.IsInstanceOfType(elem)) &&
                    elem.Name.Equals(name, caseSensitive ? System.StringComparison.Ordinal : System.StringComparison.OrdinalIgnoreCase))
                    return elem;

                foreach (var child in elem.Children)
                    next.Push(child);
            }

            return null;
        }

        public T FindChildByName<T>(string name, bool caseSensitive = false) where T : Static
        {
            return (T)FindChildByName(name, caseSensitive, typeof(T));
        }

        #endregion

        #region Sizing

        //todo: separate Measure/Arrange steps

        /// <summary>
        /// Is this element in a reflow currently?
        /// Used to prevent <see cref="OnChildReflow"/> from getting stuck in a loop
        /// </summary>
        bool isReflowing = false;

        public void Reflow()
        {
            if (isReflowing)
                return;
            isReflowing = true;

            Reflow(Parent == null ? Runtime.GraphicsDevice.Viewport.Bounds : Parent.VisibleBounds);

            Parent?.OnChildReflow(this);
            isReflowing = false;
        }

        /// <summary>
        /// Reflow child elements relative to this element
        /// Called whenever this element's position or size is adjusted
        ///
        /// Any overrides of this should call FitToContainer() first
        /// </summary>
        public virtual void Reflow(Rectangle container)
        {
            AdjustToContainer(container);

            foreach (var child in Children)
            {
                if (child.IsEnabled)
                    child.Reflow(AbsoluteDimensions);
            }
        }

        protected void ResizeAndReflow()
        {
            Reflow();

            OnResize(System.EventArgs.Empty);
            Resize?.Invoke(this, System.EventArgs.Empty); //todo: re-evaluate necessity
        }

        /// <summary>
        /// Calculate all of the bounds to this element in relation to a container (absolutely positioned)
        /// </summary>
        /// <param name="container">The container to fit this to. Should be in absolute coordinates</param>
        protected void AdjustToContainer(Rectangle container)
        {
            //todo: ideally this should take local dimensions for container

            if (HorizontalAlignment == Alignment.Stretch)
            {
                _position.X = 0;
                _size.X = container.Width - Padding.X * 2;
            }
            if (VerticalAlignment == Alignment.Stretch)
            {
                _position.Y = 0;
                _size.Y = container.Height - Padding.Y * 2;
            }
            //todo: resize events

            if (Parent == null)
            {
                //todo: this can probably be removed and just use container
                AbsoluteDimensions = LocalDimensions;
                AbsoluteDimensions.Offset(Padding);
            }
            else
            {
                AbsoluteDimensions = new Rectangle(
                    (int)(container.X + GetLocalOffset(HorizontalAlignment, Position.X, Size.X, Padding.X, container.Width)),
                    (int)(container.Y + GetLocalOffset(VerticalAlignment, Position.Y, Size.Y, Padding.Y, container.Height)),
                    (int)Size.X,
                    (int)Size.Y
                );
            }

            AbsoluteBounds = new Rectangle(
                (int)(AbsoluteDimensions.X - Padding.X),
                (int)(AbsoluteDimensions.Y - Padding.Y),
                (int)(AbsoluteDimensions.Width + Padding.X * 2),
                (int)(AbsoluteDimensions.Height + Padding.Y * 2)
            );

            VisibleBounds = Rectangle.Intersect(container, AbsoluteBounds);
            if (Parent != null)
                VisibleBounds = Rectangle.Intersect(Parent.VisibleBounds, VisibleBounds);
        }

        public float GetLocalOffset(Alignment alignment, float position, float size, float padding, float containerSize)
        {
            switch (alignment)
            {
                case Alignment.Middle:
                    return (containerSize - size) / 2 + position;
                case Alignment.End:
                    return containerSize - size - position - padding;
                default:
                    return position + padding;
            }
        }

        /// <summary>
        /// Called by a child when it reflows, this element can reflow/resize in relation
        /// </summary>
        /// <param name="child">The child element that reflowed</param>
        protected virtual void OnChildReflow(Static child)
        {
            if (AutoSize) //todo: autosize mode (grow only, minimum size, single dimension)
            {
                //grow only
                //var bounds = Rectangle.Union(AbsoluteDimensions, child.AbsoluteBounds);
                //Size = new Vector2(bounds.Width, bounds.Height);

                SizeToContain();
            }
        }

        /// <summary>
        /// Automatically size this element. By default, will size based on text and children
        /// This does not affect the position of the element
        /// Padding affects child positioning
        /// </summary>
        public virtual void SizeToContain()
        {
            var bounds = new Rectangle(
                AbsoluteDimensions.X,
                AbsoluteDimensions.Y,
                (int)textSize.X,
                (int)textSize.Y
            );
            foreach (var child in Children)
            {
                if (!child.IsEnabled)
                    continue;

                bounds = Rectangle.Union(bounds, child.AbsoluteBounds);
            }
            Size = new Vector2(bounds.Width, bounds.Height);
        }

        #endregion

        #region Updating/Drawing

        /// <summary>
        /// Update this element and all of its children
        /// </summary>
        /// <param name="time">Game time</param>
        public virtual void Update(GameTime time)
        {
            if (!IsEnabled)
                return;

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

            //find deepest darkest child
            var toUpdate = this;
            for (int i = toUpdate.Children.Count - 1; i >= 0; --i)
            {
                if (!toUpdate.Children[i].IsEnabled)
                    continue;

                toUpdate = toUpdate.Children[i];
                i = toUpdate.Children.Count;
            }

            bool handleInput = Runtime.HasFocus;
            while (true)
            {
                if (handleInput)
                    handleInput = toUpdate.HandleInput(time) && !toUpdate.IsModal;

                toUpdate.UpdateSelf(time);

                //stop at this element
                if (toUpdate.Parent == null || toUpdate == this)
                    break;

                //iterate through previous children of current level
                var index = toUpdate.Parent.Children.IndexOf(toUpdate) - 1;
                if (index >= 0)
                {
                    toUpdate = toUpdate.Parent.Children[index];

                    //find deepest child
                    for (int i = toUpdate.Children.Count - 1; i >= 0; --i)
                    {
                        if (!toUpdate.Children[i].IsEnabled)
                            continue;

                        toUpdate = toUpdate.Children[i];
                        i = toUpdate.Children.Count;
                    }
                }
                else
                    toUpdate = toUpdate.Parent;
            }
        }

        /// <summary>
        /// Update this UI's state here. Input should be handled in <see cref="HandleInput"/>
        /// Bindings should be applied here
        /// </summary>
        /// <param name="time">game time</param>
        protected virtual void UpdateSelf(GameTime time)
        {
            if (Bindings != null)
            {
                bool didUpdateBinding = false;
                foreach (var binding in Bindings)
                    didUpdateBinding |= binding.Update();
                if (didUpdateBinding && AutoSize)
                    SizeToContain();
            }
        }

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
                if ((!ignoreTabKey && Input.InputState.IsPress(Keys.Tab)) ||
                    Input.InputState.IsAnyPress(Buttons.RightShoulder))
                {
                    if (Input.InputState.IsMod(Input.KeyMod.Shift))
                        FocusPrevious();
                    else
                        FocusNext();
                    return false;
                }

                if (Input.InputState.IsAnyPress(Buttons.LeftShoulder))
                {
                    FocusPrevious();
                    return false;
                }

                var thumb = Input.InputState.Thumbsticks().Left;
                var lastThumb = Input.InputState.LastThumbsticks().Left;
                if (thumb != Vector2.Zero && lastThumb == Vector2.Zero)
                {
                    if (FocusGeographically(thumb) != null)
                        return false;
                }

                if ((!ignoreEnterKey && Input.InputState.IsPress(Keys.Enter)) ||
                    (!ignoreSpaceKey && Input.InputState.IsPress(Keys.Space)) ||
                    Input.InputState.IsAnyPress(Buttons.A)) //optionally restrict input to player
                {
                    TriggerClick(Vector2.Zero);
                    return false;
                }
            }

            //todo: improve
            if (Input.InputState.IsPress(0) && VisibleBounds.Contains(Input.InputState.touches[0].Position))
            {
                var e = new ClickEventArgs { position = Vector2.Zero };
                OnClick(e);
                Click?.Invoke(this, e);

                if (CanFocus)
                {
                    HasFocus = true;
                    return false;
                }

                return false;
            }

            var mouse = Input.InputState.MousePoint;
            return HandleMouseInput(mouse, Input.MouseButtons.Left);
        }

        bool HandleMouseInput(Point mousePosition, Input.MouseButtons button)
        {
            if (Input.InputState.IsPress(Input.MouseButtons.Left) && VisibleBounds.Contains(mousePosition))
            {
                var e = new ClickEventArgs { position = (mousePosition - AbsoluteBounds.Location).ToVector2() };
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
            else if (DidPressInside(Input.MouseButtons.Left))
                return false;

            else if (Input.InputState.IsButtonUp(Input.MouseButtons.Left))
            //else if (Input.InputState.Gestures.TryGetValue(GestureType.Tap, out var gesture))
            {
                if (didPress && VisibleBounds.Contains(mousePosition)) //gesture pos
                {
                    TriggerClick((mousePosition - AbsoluteBounds.Location).ToVector2());
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
            if (!IsEnabled)
                return;

            var draws = new Queue<Static>(Children.Count + 1);
            draws.Enqueue(this);

            Static lastDraw = null;
            while (draws.Count > 0)
            {
                var toDraw = draws.Dequeue();

                Graphics.Primitives2D.DrawFill(spriteBatch, toDraw.BackgroundColor, toDraw.VisibleBounds);
                if (BackgroundSprite != null)
                    BackgroundSprite.Draw(spriteBatch, toDraw.VisibleBounds, 0);

                toDraw.DrawSelf(spriteBatch);

                var borderColor = (toDraw.HasFocus && toDraw.CanFocus) ? FocusedBorderColor : toDraw.BorderColor;
                if (DebugFont != null && borderColor == Color.Transparent)
                    borderColor = Color.SteelBlue;

                if (DebugFont != null && toDraw.VisibleBounds.Contains(Input.InputState.MousePoint))
                    lastDraw = toDraw;

                Graphics.Primitives2D.DrawRect(spriteBatch, borderColor, toDraw.VisibleBounds);

                foreach (var child in toDraw.Children)
                {
                    if (child.IsEnabled)
                        draws.Enqueue(child);
                }
            }

            if (lastDraw != null)
            {
                var rect = lastDraw.AbsoluteBounds;
                rect.Inflate(1, 1);
                Graphics.Primitives2D.DrawRect(spriteBatch, Color.Red, rect);
                Graphics.Primitives2D.DrawRect(spriteBatch, new Color(Color.Red, 0.5f), lastDraw.AbsoluteDimensions);

                string info = $"Name: {(Name ?? "(No name)")}\nBounds: {lastDraw.AbsoluteDimensions}\nDimensions: {lastDraw.LocalDimensions} Padding: {lastDraw.Padding}";
                DebugFont.Draw(spriteBatch, info, (rect.Location + new Point(rect.Width, rect.Height)).ToVector2(), Color.Gold);
            }
        }

        /// <summary>
        /// Draw only this item (no children)
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to use</param>
        protected virtual void DrawSelf(SpriteBatch spriteBatch)
        {
            var textPos = ((Size - textSize) / 2).ToPoint();
            DrawText(spriteBatch, textPos);
        }

        /// <summary>
        /// Draw this element's text. Position specifies the base location
        /// </summary>
        /// <param name="position">The position of the text relative to this element</param>
        protected void DrawText(SpriteBatch spriteBatch, Point position)
        {
            if (Font == null || Text == null)
                return;

            position += AbsoluteDimensions.Location - VisibleBounds.Location;
            Font.Draw(spriteBatch, Text, 0, Text.Length, VisibleBounds, position, Color);
        }

        #endregion

        //todo: better name
        public void TriggerClick(Vector2 relativePosition)
        {
            var ce = new ClickEventArgs { position = relativePosition, inputIndex = 0 };
            OnClick(ce);
            Click?.Invoke(this, ce);

            clickCommandFn?.Invoke(this);
        }

        public override string ToString()
        {
            return $"{base.ToString()} \"{Name ?? "(No name)"}\"{(HasFocus ? " *" : "")}";
        }

        public virtual void DerivedDeserialize(Dictionary<string, object> props)
        {

            if (!(props.ContainsKey("Bounds") ||
                  props.ContainsKey("Size")) &&
                 (!props.TryGetValue("AutoSize", out var doAutosize) || doAutosize as bool? != false))
                SizeToContain();

            //allow autosizing only one dimension
            if (props.TryGetValue("Width", out var width))
                Size = new Vector2(Data.Serializer.Cast<float>(width), Size.Y);

            if (props.TryGetValue("Height", out var height))
                Size = new Vector2(Size.X, Data.Serializer.Cast<float>(height));
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
            var typeInfo = type.GetTypeInfo();

            if (typeInfo.IsEnum)
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
                        check.SizeToContain();
                        root.AddChild(check);
                        maxWidth = System.Math.Max(maxWidth, check.Size.X);
                    }
                }
                root.Size = new Vector2(maxWidth, 1);
                root.SizeToContain();
                return root;
            }

            //todo: item spacing (can't use list margin without nesting)

            var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public);

            //todo: move these into type handlers
            foreach (var member in members)
            {
                System.Type memberType;
                object curValue;
                System.Action<object, object> setValue;

                if (member is FieldInfo fInfo)
                {
                    memberType = fInfo.FieldType;
                    curValue = fInfo.GetValue(obj);
                    setValue = fInfo.SetValue;
                }
                else if (member is PropertyInfo pInfo)
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
                    label.SizeToContain();
                    root.AddChild(label);

                    maxWidth = System.Math.Max(maxWidth, label.Size.X);
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
                    check.SizeToContain();
                    root.AddChild(check);
                    maxWidth = System.Math.Max(maxWidth, check.Size.X);
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
                    input.SizeToContain();
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
                    input.SizeToContain();
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
                    input.SizeToContain();
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
                    input.SizeToContain();
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
                    input.SizeToContain();
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
                        setValue(obj, Data.Cache.Load<Texture2D>(input.Text));
                    };
                    input.SizeToContain();
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
                    input.SizeToContain();

                    var mSecLabel = new Static()
                    {
                        Text = "msec",
                        Font = font,
                        Color = color
                    };
                    mSecLabel.SizeToContain();

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
                    container.SizeToContain();
                    root.AddChild(container);
                }
                else
                {
                    label.Text += $"\n`aab--- ({memberType.Name}) ---\nthird line`x";
                    label.SizeToContain();
                }
            }

            root._size = new Vector2(maxWidth, 1);
            root.SizeToContain();
            return root;
        }
    }
}
