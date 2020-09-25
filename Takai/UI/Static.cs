using Microsoft.Xna.Framework;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using Takai.Graphics;

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
        Stretch,

        Left = Start,
        Top = Start,

        Right = End,
        Bottom = End,

        Center = Middle,
    }

    /// <summary>
    /// The basic UI element
    /// </summary>
    public partial class Static : Data.IDerivedDeserialize
    {
#if DEBUG
        /// <summary>
        /// A unique ID for this element. Only present in DEBUG
        /// </summary>
        [Data.DebugSerialize]
        public uint DebugId { get; private set; } = GenerateId();

        private static uint idCounter = 0;
        private static uint GenerateId()
        {
            //a method here allows for easier debuggin
            return ++idCounter;
        }

        /// <summary>
        /// the XPath equivelent for this UI
        /// </summary>
        [Data.DebugSerialize]
        public string DebugTreePath { get; private set; }
#endif

        #region Basic Properties

        /// <summary>
        /// A font to use for drawing debug info.
        /// If null, debug info is not drawn
        /// </summary>
        public static Font DebugFont = null;
        public static TextStyle DebugTextStyle = new TextStyle
        {
            size = 15
        };
        public static bool DisplayDebugInfo = false;

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
                if (_text == value)
                    return;

                _text = value;
                InvalidateMeasure();
            }
        }
        private string _text;

        /// <summary>
        /// The font to draw the text of this element with.
        /// Optional if text is null
        /// </summary>
        public virtual Font Font
        {
            get => _font;
            set
            {
                if (_font == value)
                    return;

                _font = value;
                InvalidateMeasure();
            }
        }
        private Font _font;

        public virtual TextStyle TextStyle
        {
            get => _textStyle;
            set
            {
                if (_textStyle == value)
                    return;

                _textStyle = value;
                InvalidateMeasure();
            }
        }
        private TextStyle _textStyle;

        /// <summary>
        /// The color of this element. Usage varies between element types
        /// Usually applies to text color
        /// </summary>
        public virtual Color Color { get; set; } = Color.White;

        /// <summary>
        /// The color to draw the outline with, by default, transparent
        /// </summary>
        public Color BorderColor { get; set; } = Color.Transparent;

        /// <summary>
        /// An optional fill color for this element, by default, transparent
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.Transparent;

        /// <summary>
        /// An optional background sprite to draw behind the element. Drawn over BackgroundColor
        /// </summary>
        public NinePatch BackgroundSprite { get; set; }

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
                    if (_verticalAlignment == Alignment.Stretch)
                        InvalidateMeasure();
                    else
                        InvalidateArrange();
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
                    if (_verticalAlignment == Alignment.Stretch)
                        InvalidateMeasure();
                    else
                        InvalidateArrange();
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
                    InvalidateMeasure(); //measured size includes position
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
                    //if (float.IsInfinity(value.X) || float.IsInfinity(value.Y))
                    //  Diagnostics.Debug.WriteLine($"{this}: Size=Infinity will always render as collapse");

                    _size = value;
                    InvalidateMeasure();
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
                    InvalidateMeasure();
                }
            }
        }
        private Vector2 _padding = Vector2.Zero;

        /// <summary>
        /// Does this element currently have focus?
        /// </summary>
        public bool HasFocus
        {
            get => _hasFocus;// && Runtime.HasFocus;
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
                        if (next._hasFocus)
                        {
                            next._hasFocus = false;
                            next.ApplyStateStyle();
                        }

                        foreach (var child in next.Children)
                            defocusing.Push(child);
                    }

                }

                _hasFocus = value;
                if (_hasFocus == true)
                    ApplyStateStyle();
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
                    InvalidateArrange();
            }
        }
        private bool _isEnabled = true;

        /// <summary>
        /// Can this element be focused
        /// </summary>
        [Data.Serializer.Ignored]
        public virtual bool CanFocus => (_eventCommands != null && EventCommands.ContainsKey(ClickEvent)) ||
                                        (events != null && events.ContainsKey(ClickEvent)); //todo: make user-editable?

        /// <summary>
        /// Bind properties of <see cref="BindingSource"/> to properties of this UI element
        /// Any modifications to this list will require rebinding
        /// </summary>
        public List<Data.Binding> Bindings { get; set; }

        /// <summary>
        /// If not null, child elements will be bound with the specified property of this class
        /// </summary>
        public string ChildBindScope { get; set; }
        private Data.GetSet bindScopeGetset;

        #endregion

        public Static()
        {
            //Diagnostics.Debug.WriteLine($"New ID:{DebugId}");
            Children = _children.AsReadOnly();
#if DEBUG
            DebugTreePath = $"/{(GetType().Name)}({DebugId})";
#endif
            if (Runtime.GraphicsDevice != null)
                lastMeasureContainerBounds = Runtime.GraphicsDevice.Viewport.Bounds;
            Style = GetType().Name;
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

        private void DeserializeChildren(object objects)
        {
            if (!(objects is List<object> elements))
                throw new ArgumentException("Children must be a list of UI elements");

            foreach (var element in elements)
            {
                if (element is Static child)
                    AddChild(child);
            }
        }

        /// <summary>
        /// Create a clone of this static and all of its children
        /// Does not add to parent
        /// </summary>
        /// <returns>The cloned static</returns>
        public Static CloneHierarchy()
        {
            var clone = (Static)MemberwiseClone();
#if DEBUG
            clone.DebugId = GenerateId();
#endif

            //move to copy constructor?
            clone.didPress = new BitVector32(0);
            if (events != null)
                clone.events = new Dictionary<string, UIEvent>(events);
            if (_eventCommands != null)
                clone._eventCommands = new Dictionary<string, EventCommandBinding>(_eventCommands);
            if (_commandActions != null)
                clone._commandActions = new Dictionary<string, Action<Static, object>>(_commandActions);
            if (Bindings != null)
                clone.Bindings = new List<Data.Binding>(Bindings);

            //rebind?
            clone.SetParentNoReflow(null);
            clone._children = new List<Static>(_children);
            clone.Children = clone._children.AsReadOnly();
            for (int i = 0; i < clone._children.Count; ++i)
            {
                var child = clone._children[i].CloneHierarchy(); //make iterative?
                child.SetParentNoReflow(clone);
                clone._children[i] = child;

                //do these immediately (on original)?
                if (!child.isMeasureValid)
                    measureQueue.Add(child);
                if (!child.isArrangeValid)
                    arrangeQueue.Add(child);
            }
            clone.FinalizeClone();
            clone.InvalidateMeasure();
            return clone;
        }

        /// <summary>
        /// Allows this element the opportunity to refresh any references after it and its children have been cloned
        /// </summary>
        protected virtual void FinalizeClone() { }

        /// <summary>
        /// Bind this UI element to an object
        /// Calls <see cref="BindTo"/> on child elements by default
        /// Can be overriden to customize this behavior
        /// </summary>
        /// <param name="source">The source object for the bindings</param>
        public virtual void BindTo(object source, Dictionary<string, object> customBindProps = null)
        {
            BindToThis(source, customBindProps);

            var childScope = source;
            if (ChildBindScope != null)
            {
                bindScopeGetset = Data.GetSet.GetMemberAccessors(source, ChildBindScope);
                childScope = bindScopeGetset.cachedValue = bindScopeGetset.get?.Invoke();
                bindScopeGetset.cachedHash = (bindScopeGetset.cachedValue ?? 0).GetHashCode();
            }

            foreach (var child in Children)
                child.BindTo(childScope, customBindProps);
        }

        /// <summary>
        /// Update bindings only this element and not children
        /// </summary>
        /// <param name="source">The source object for the binding</param>
        protected void BindToThis(object source, Dictionary<string, object> customBindProps = null)
        {
            if (Bindings == null)
                return;

            foreach (var binding in Bindings)
                binding.BindTo(source, this, customBindProps);
        }

        public object GetChildBindScope()
        {
            if (ChildBindScope == null)
                return null;

            return bindScopeGetset.cachedValue;
        }

        public override string ToString()
        {
            string extraInfo = "";
#if DEBUG
            extraInfo = $" ID:{DebugId}";
#endif
            return $"{GetType().Name} {{{Name ?? "(No name)"}}}{(HasFocus ? "*" : "")} \"{Text ?? ""}\" {(IsEnabled ? "👁" : "❌")}{extraInfo}";
        }

        public virtual void DerivedDeserialize(Dictionary<string, object> props)
        {
            //allow autosizing only one dimension
            if (props.TryGetValue("Width", out var width))
                Size = new Vector2(Data.Serializer.Cast<float>(width), Size.Y);

            if (props.TryGetValue("Height", out var height))
                Size = new Vector2(Size.Y, Data.Serializer.Cast<float>(height));
        }

        #region Helpers

        public Vector2 LocalToScreen(Vector2 point)
        {
            return point + new Vector2(OffsetContentArea.X, OffsetContentArea.Y);
        }

        public Point LocalToScreen(Point point)
        {
            return point + OffsetContentArea.Location;
        }

        public Vector2 ScreenToLocal(Vector2 point)
        {
            return point - new Vector2(OffsetContentArea.X, OffsetContentArea.Y);
        }

        public Point ScreenToLocal(Point point)
        {
            return point - OffsetContentArea.Location;
        }

        #endregion
    }
}
