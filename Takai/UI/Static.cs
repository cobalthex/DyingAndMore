﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

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
        //for testing
        //private static uint idCounter = 0;
        //public readonly uint Id = ++idCounter;

        /// <summary>
        /// Marks a size to automatically expand to fit its contents
        /// </summary>
        public const float AutoSize = float.NaN;

        public readonly Vector2 InfiniteSize = new Vector2(float.PositiveInfinity);

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
                    if (IsAutoSized)
                        Reflow();
                }
            }
        }
        private string _text;

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
                    if (IsAutoSized)
                        Reflow();
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
        /// The size of the element. Use <see cref="float.NaN"/> to auto-size
        /// </summary>
        [Data.Serializer.ReadOnly]
        public Vector2 Size //todo: NaN for autosize
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
        private Vector2 _size = new Vector2(float.NaN);

        /// <summary>
        /// Is one or both of the dimensions autosized?
        /// </summary>
        public bool IsAutoSized => float.IsNaN(Size.X) || float.IsNaN(Size.Y);

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
        /// The desired bounds of this object relative to its parent.
        /// This includes padding and stretching. Calculated by <see cref="Measure"/>
        /// </summary>
        [Data.Serializer.Ignored]
        public Rectangle Bounds { get; private set; }

        /// <summary>
        /// The <see cref="Bounds"/> of this element offset relative to the screen
        /// </summary>
        [Data.Serializer.Ignored]
        public Rectangle OffsetBounds { get; private set; }

        /// <summary>
        /// The visible region of this element on the screen
        /// </summary>
        [Data.Serializer.Ignored]
        protected Rectangle VisibleBounds { get; private set; }

        /// <summary>
        /// The container that this element fits into
        /// </summary>
        private Rectangle containerBounds;

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
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;

                _isEnabled = value;
                if (_isEnabled)
                    Reflow();
            }
        }
        private bool _isEnabled = true;

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
        private List<Static> _children = new List<Static>();

        /// <summary>
        /// Bind properties of <see cref="BindingSource"/> to properties of this UI element
        /// Any modifications to this list will require rebinding
        /// </summary>
        public List<Data.Binding> Bindings { get; set; }

        #endregion

        private void DeserializeChildren(object objects)
        {
            if (!(objects is List<object> elements))
                throw new System.ArgumentException("Children must be a list of UI elements");

            foreach (var element in elements)
            {
                if (element is Static child)
                    AddChild(child);
            }
        }

        public Static()
        {
            Children = _children.AsReadOnly();
        }

        /// <summary>
        /// Create a simple static label. Calls <see cref="AutoSize(float)"/>
        /// </summary>
        /// <param name="text">The text to set</param>
        public Static(string text)
            : this()
        {
            Text = text;
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
                    top._children[i] = child;
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

        /// <summary>
        /// Insert the child into the children without reflowing
        /// </summary>
        /// <param name="child">The child element to add</param>
        /// <param name="index">The insert to add at. Out of bounds are added to the end</param>
        /// <param name="ignoreFocus">ignore <see cref="HasFocus"/></param>
        /// <returns>True if the child as added, false otherwise</returns>
        public virtual bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
        {
            //todo: maybe have a forward setting (forward all additions to specified child)

            if (child.Parent == this)
                return false;

            if (child.Parent != null)
                child.RemoveFromParent();

            child.SetParentNoReflow(this);

            if (index < 0 || index >= _children.Count)
                _children.Add(child);
            else
                _children.Insert(index, child);

            if (child.HasFocus && !ignoreFocus)
                child.HasFocus = true;

            if (reflow)
                Reflow();

            return true;
        }

        public virtual bool InternalRemoveChildIndex(int index)
        {
            if (index < 0 || index >= Children.Count)
                return false;

            var child = Children[index];
            _children.RemoveAt(index);
            if (child.Parent == this)
                child.SetParentNoReflow(null);
            return true;
        }

        public Static AddChild(Static child)
        {
            InternalInsertChild(child);
            return child;
        }

        /// <summary>
        /// Replace the child at a specific index
        /// </summary>
        /// <param name="child">the child to replace with</param>
        /// <param name="index">the index of the child to replace. Throws if out of range</param>
        /// <returns>The staticadded</returns>
        public Static ReplaceChild(Static child, int index)
        {
            InternalRemoveChildIndex(index);
            InternalInsertChild(child, index);
            return child;
        }

        public Static InsertChild(Static child, int index = 0)
        {
            InternalInsertChild(child, index);
            return child;
        }

        public void AddChildren(params Static[] children)
        {
            AddChildren((IEnumerable<Static>)children);
        }

        public void AddChildren(IEnumerable<Static> children)
        {
            //todo: add disable reflow and then switch this to use InsertChild
            int count = Children.Count;

            Static lastFocus = null;
            foreach (var child in children)
            {
                if (InternalInsertChild(child, -1, false, true) && child.HasFocus)
                    lastFocus = child;
            }

            if (lastFocus != null)
                lastFocus.HasFocus = true;

            if (Children.Count != count)
                Reflow();
        }

        /// <summary>
        /// Remove an element from this element. Does not search children
        /// </summary>
        /// <param name="child"></param>
        public Static RemoveChild(Static child)
        {
            InternalRemoveChildIndex(_children.IndexOf(child));
            return child;
        }

        public Static RemoveChildAt(int index)
        {
            var child = Children[index];
            InternalRemoveChildIndex(index);
            return child;
        }

        public void RemoveAllChildren()
        {
            for (int i = _children.Count - 1; i >= 0; --i)
                InternalRemoveChildIndex(i);
        }

        /// <summary>
        /// Move all children from this element to another
        /// </summary>
        /// <param name="target">The target element to move them to</param>
        public void MoveAllChildrenTo(Static target)
        {
            //todo: use internal?
            target.AddChildren(_children);
            _children.Clear();
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
        /// This can be overriden by
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
                    var diff = (top.VisibleBounds.Location - VisibleBounds.Location).ToVector2();

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

        /// <summary>
        /// Is this element in a reflow currently?
        /// Used to prevent <see cref="OnChildReflow"/> from getting stuck in a loop
        /// </summary>
        bool isReflowing = false;

        public void Reflow()
        {
            Reflow(containerBounds);
        }

        /// <summary>
        /// Reflow this container, relative to its parent
        /// </summary>
        /// <param name="container">Container in relative coordinates</param>
        public void Reflow(Rectangle container)
        {
            if (!IsEnabled || isReflowing)
                return;

            isReflowing = true;
            AdjustToContainer(container);
            ReflowOverride(VisibleBounds.Size.ToVector2()); //todo: this needs to be visibleDimensions
            NotifyChildReflow();
            isReflowing = false;
        }

        /// <summary>
        /// Reflow child elements relative to this element
        /// Called whenever this element's position or size is adjusted
        /// </summary>
        protected virtual void ReflowOverride(Vector2 availableSize)
        {
            //todo: this availableSize needs to be Size or stretch

            foreach (var child in Children)
                child.Reflow(new Rectangle(0, 0, (int)availableSize.X, (int)availableSize.Y));
        }

        public event System.EventHandler ChildReflow = null;

        /// <summary>
        /// Called by a child when it reflows, this element can reflow/resize in relation
        /// </summary>
        /// <param name="child">The child element that reflowed</param>
        protected virtual void OnChildReflow(Static child)
        {
            if (IsAutoSized)
                Reflow();
        }

        //todo: this shouldnt need to exist
        public void NotifyChildReflow()
        {
            if (Parent == null)
                return;

            Parent.OnChildReflow(this);
            Parent.ChildReflow?.Invoke(this, System.EventArgs.Empty);
        }


        protected void ResizeAndReflow()
        {
            Reflow();

            OnResize(System.EventArgs.Empty);
            Resize?.Invoke(this, System.EventArgs.Empty);
        }

        /// <summary>
        /// Calculate all of the bounds to this element in relation to a container.
        /// </summary>
        /// <param name="container">The container to fit this to, in relative coordinates</param>
        private void AdjustToContainer(Rectangle container)
        {
            //todo: ideally this should take local dimensions for container
            Rectangle parentContainer;
            var offsetParent = Point.Zero;
            if (Parent == null)
                parentContainer = Runtime.GraphicsDevice.Viewport.Bounds;
            else
            {
                offsetParent = Parent.OffsetBounds.Location;
                parentContainer = Parent.VisibleBounds;
            }

            var measuredSize = Measure(container.Size.ToVector2()); //todo: this should go elsewhere
            var localPos = new Vector2(
                GetLocalOffset(HorizontalAlignment, Position.X, measuredSize.X, Padding.X, container.Width),
                GetLocalOffset(VerticalAlignment, Position.Y, measuredSize.Y, Padding.Y, container.Height)
            );
            var bnd = new Rectangle((int)localPos.X, (int)localPos.Y, (int)(measuredSize.X - Padding.X * 2), (int)(measuredSize.Y - Padding.Y * 2));
            bnd.Offset(container.Location);
            Bounds = bnd;

            bnd.Offset(offsetParent);
            OffsetBounds = bnd;

            bnd = Rectangle.Intersect(Bounds, container);
            bnd.Offset(offsetParent);
            bnd.Inflate(Padding.X, Padding.Y); //testing
            VisibleBounds = Rectangle.Intersect(bnd, parentContainer);
            containerBounds = container;
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
        /// Calculate the desired size of this element and its children. Can be customized through <see cref="MeasureOverride"/>.
        /// Sets <see cref="DesiredSize"/> to value calculated
        /// </summary>
        /// <returns>The desired size of this element, including padding</returns>
        public Vector2 Measure(Vector2 availableSize)
        {
            var size = Size;
            bool isWidthAutoSize = float.IsNaN(size.X);
            bool isHeightAutoSize = float.IsNaN(size.Y);
            bool isHStretch = HorizontalAlignment == Alignment.Stretch;
            bool isVStretch = VerticalAlignment == Alignment.Stretch;

            if (availableSize.X < InfiniteSize.X)
            {
                availableSize.X -= Padding.X * 2;
                if (isWidthAutoSize && isHStretch)
                    availableSize.X -= (int)Position.X;
            }

            if (availableSize.Y < InfiniteSize.Y)
            {
                availableSize.Y -= Padding.Y * 2;
                if (isHeightAutoSize && isVStretch)
                    availableSize.Y -= (int)Position.Y;
            }

            if (isWidthAutoSize || isHeightAutoSize)
            {
                var measuredSize = MeasureOverride(availableSize);
                if (float.IsInfinity(measuredSize.X) || float.IsNaN(measuredSize.X)
                 || float.IsInfinity(measuredSize.Y) || float.IsNaN(measuredSize.Y))
                    throw new System.ArgumentOutOfRangeException("Measured size cannot be NaN or infinity");

                if (HorizontalAlignment == Alignment.Stretch)
                    size.X = availableSize.X;
                else if (isWidthAutoSize)
                    size.X = measuredSize.X;

                if (VerticalAlignment == Alignment.Stretch)
                    size.Y = availableSize.Y;
                else if (isHeightAutoSize)
                    size.Y = measuredSize.Y;
            }

            var desired = Position + size + Padding * 2;
            return desired;
        }

        /// <summary>
        /// Measure the preferred size of this object.
        /// May be overriden to provide custom sizing (this should not include <see cref="Size"/> or <see cref="Padding"/>)
        /// By default calculates the shrink-wrapped size
        /// Measurements to children should call <see cref="Measure"/>
        /// </summary>
        /// <param name="availableSize">This is the available size of the container,. The returned size can be larger or smaller</param>
        /// <returns>The preferred size of this element</returns>
        protected virtual Vector2 MeasureOverride(Vector2 availableSize)
        {
            var textSize = Point.Zero;
            if (Text != null && Font != null)
                textSize = Font.MeasureString(Text).ToPoint();
            var bounds = new Rectangle(0, 0, textSize.X, textSize.Y);

            foreach (var child in Children)
            {
                if (!child.IsEnabled)
                    continue;

                var mes = child.Measure(availableSize).ToPoint();
                bounds = Rectangle.Union(bounds, new Rectangle((int)child.Position.X, (int)child.Position.Y, mes.X, mes.Y));
            }

            return new Vector2(bounds.Width, bounds.Height);
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

            bool handleInput = true;// Runtime.HasFocus;
            while (true)
            {
                if (handleInput)
                    handleInput = toUpdate.HandleInput(time) && !toUpdate.IsModal;

                toUpdate.UpdateSelf(time);

                //stop at this element
                if (toUpdate.Parent == null || toUpdate == this)
                    break;

                //iterate through previous children of current level
                var i = toUpdate.Parent._children.IndexOf(toUpdate) - 1;
                for (; i >= 0; --i)
                {
                    toUpdate = toUpdate.Parent.Children[i];
                    if (!toUpdate.IsEnabled)
                        continue;

                    //find deepest child
                    for (int j = toUpdate.Children.Count - 1; j >= 0; --j)
                    {
                        if (!toUpdate.Children[j].IsEnabled)
                            continue;

                        toUpdate = toUpdate.Children[j];
                        j = toUpdate.Children.Count;
                    }
                    break;
                }
                if (i < 0) //todo: does this skip the first child?
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
                //if (didUpdateBinding && AutoSize)
                //    SizeToContain();
                //should be handled by binding setters
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
                var e = new ClickEventArgs { position = (mousePosition - OffsetBounds.Location).ToVector2() };
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
                    TriggerClick((mousePosition - OffsetBounds.Location).ToVector2());
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

            Static debugDraw = null;
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
                    debugDraw = toDraw;

                Graphics.Primitives2D.DrawRect(spriteBatch, borderColor, toDraw.VisibleBounds);

                foreach (var child in toDraw.Children)
                {
                    if (child.IsEnabled)
                        draws.Enqueue(child);
                }
            }

            debugDraw?.DrawDebugInfo(spriteBatch);
        }

        public void DrawDebugInfo(SpriteBatch spriteBatch)
        {
            var rect = OffsetBounds;
            Graphics.Primitives2D.DrawRect(spriteBatch, new Color(Color.OrangeRed, 0.5f), rect);

            rect.Inflate(Padding.X, Padding.Y);
            Graphics.Primitives2D.DrawRect(spriteBatch, Color.Red, rect);

            string info = $"{GetType().Name}\n"
                        + $"Name: {(Name ?? "(No name)")}\n"
                        + $"Bounds: {OffsetBounds}\n"
                        + $"Position: {Position}: Size {Size}, Padding: {Padding}\n"
                        + $"HAlign: {HorizontalAlignment}, VAlign: {VerticalAlignment}";

            var drawPos = rect.Location + new Point(rect.Width + 10, rect.Height + 10);
            var size = DebugFont.MeasureString(info);
            drawPos = Util.Clamp(new Rectangle(drawPos.X, drawPos.Y, (int)size.X, (int)size.Y), Runtime.GraphicsDevice.Viewport.Bounds);
            DebugFont.Draw(spriteBatch, info, drawPos.ToVector2(), Color.Gold);
        }

        /// <summary>
        /// Draw only this item (no children)
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to use</param>
        protected virtual void DrawSelf(SpriteBatch spriteBatch)
        {
            DrawText(spriteBatch, Point.Zero);
        }

        /// <summary>
        /// Draw this element's text. Position specifies the base location
        /// </summary>
        /// <param name="position">The position of the text relative to this element</param>
        protected void DrawText(SpriteBatch spriteBatch, Point position)
        {
            if (Font == null || Text == null)
                return;

            position += Padding.ToPoint() - (VisibleBounds.Location - OffsetBounds.Location);
            Font.Draw(spriteBatch, Text, 0, Text.Length, VisibleBounds, position, Color);
        }

        #endregion

        public override string ToString()
        {
            return $"{base.ToString()} {{{Name ?? "(No name)"}}}{(HasFocus ? "*" : "")} \"{Text ?? ""}\"";
        }

        public virtual void DerivedDeserialize(Dictionary<string, object> props)
        {
            //allow autosizing only one dimension
            if (props.TryGetValue("Width", out var width))
                Size = new Vector2(Data.Serializer.Cast<float>(width), Size.Y);

            if (props.TryGetValue("Height", out var height))
                Size = new Vector2(Size.X, Data.Serializer.Cast<float>(height));
        }

        #region Helpers

        //todo: better name
        public void TriggerClick(Vector2 relativePosition)
        {
            var ce = new ClickEventArgs { position = relativePosition, inputIndex = 0 };
            OnClick(ce);
            Click?.Invoke(this, ce);

            clickCommandFn?.Invoke(this);
        }

        public static Static GeneratePropSheet(object obj, Graphics.BitmapFont font, Color color)
        {
            var root = new List() { Margin = 2, Direction = Direction.Vertical };

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
                            Text = Util.ToSentenceCase(flag),
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
                        root.AddChild(check);
                    }
                }
                return root;
            }

            //todo: item spacing (can't use list margin without nesting)

            var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public);

            //todo: move these into type handlers
            foreach (var member in members)
            {
                System.Type mt;
                if (member is FieldInfo fi)
                    mt = fi.FieldType;
                else if (member is PropertyInfo pi)
                    mt = pi.PropertyType;
                else
                    continue;

                if (mt == typeof(bool))
                {
                    var checkbox = new CheckBox
                    {
                        Text = Util.ToSentenceCase(member.Name),
                        Bindings = new List<Data.Binding>
                        {
                            new Data.Binding(member.Name, "IsChecked", Data.BindingMode.TwoWay)
                        },
                        Font = font,
                        Color = color
                    };
                    checkbox.BindTo(obj);
                    root.AddChild(checkbox);
                    continue;
                }

                root.AddChild(new Static(Util.ToSentenceCase(member.Name))
                {
                    Font = font,
                    Color = color
                }); //label

                if (Data.Serializer.IsInt(member) ||
                    Data.Serializer.IsFloat(member))
                {
                    var numeric = new NumericInput
                    {
                        Bindings = new List<Data.Binding>
                        {
                            new Data.Binding(member.Name, "Value", Data.BindingMode.TwoWay)
                        },
                        Font = font,
                        Color = color
                    };
                    numeric.BindTo(obj);
                    root.AddChild(numeric);
                }
                else if (mt == typeof(string))
                {
                    var text = new TextInput
                    {
                        Bindings = new List<Data.Binding>
                        {
                            new Data.Binding(member.Name, "Text", Data.BindingMode.TwoWay)
                        },
                        Font = font,
                        Color = color
                    };
                    text.BindTo(obj);
                    root.AddChild(text);
                }
                else if (mt == typeof(Dictionary<,>))
                {
                    //todo
                }
                else if (mt == typeof(IEnumerable<>))
                {
                    //todo: must generate for list not object
                }
            }

            return root;
        }

        #endregion
    }
}
