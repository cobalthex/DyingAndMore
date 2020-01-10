using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Takai.Input;

using Stylesheet = System.Collections.Generic.Dictionary<string, object>;

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

    public class UIEventArgs : System.EventArgs
    {
        public Static Source { get; set; }

        public UIEventArgs(Static source)
        {
            Source = source;
        }
    }

    public class PointerEventArgs : UIEventArgs
    {
        /// <summary>
        /// The relative position of the click inside the element
        /// If activated via keyboard, this is Zero
        /// </summary>
        public Vector2 position;

        public int button; //maps to relevent enum for device
        public DeviceType device;
        public int deviceIndex; //e.g. player index

        public PointerEventArgs(Static source)
            : base(source) { }
    }

    public class DragEventArgs : PointerEventArgs
    {
        public Vector2 delta;

        public DragEventArgs(Static source)
            : base(source) { }
    }

    public enum UIEventResult
    {
        Continue,
        Handled,
    }

    public delegate UIEventResult UIEventHandler(Static sender, UIEventArgs e);
    public delegate UIEventResult UIEventHandler<TEventArgs>(Static sender, TEventArgs e) where TEventArgs : UIEventArgs;

    [Data.Serializer.Ignored]
    public struct UIEvent//todo: this could probably be a struct
    {
        public enum RouteDirection
        {
            Bubble,
            Tunnel
        }

        public RouteDirection Direction { get; set; }

        private List<UIEventHandler> handlers;

        public int HandlerCount => handlers?.Count ?? 0;

        public UIEvent(bool allocHandlers)
        {
            Direction = RouteDirection.Bubble;

            if (allocHandlers)
                handlers = new List<UIEventHandler>();
            else
                handlers = null;
        }

        public void AddHandler(UIEventHandler handler)
        {
            if (handlers == null)
                handlers = new List<UIEventHandler>(1);
            handlers.Add(handler);
        }

        public bool RemoveHandler(UIEventHandler handler)
        {
            return handlers != null &&
                handlers.Remove(handler);
        }

        public void RemoveAllHandlers()
        {
            handlers.Clear();
        }

        public static UIEvent operator +(UIEvent e, UIEventHandler handler)
        {
            e.AddHandler(handler);
            return e;
        }
        public static UIEvent operator -(UIEvent e, UIEventHandler handler)
        {
            e.RemoveHandler(handler);
            return e;
        }

        public UIEventResult Invoke(Static sender, UIEventArgs e)
        {
            if (handlers == null)
                return UIEventResult.Continue;

            foreach (var handler in handlers)
            {
                if (handler.Invoke(sender, e) == UIEventResult.Handled)
                    return UIEventResult.Handled;
            }

            return UIEventResult.Continue;
        }
    }

    public struct EventCommandBinding
    {
        public string command;
        public object argument;

        public EventCommandBinding(string command, object argument = null)
        {
            this.command = command;
            this.argument = argument;
        }

        public static implicit operator EventCommandBinding(string command)
        {
            return new EventCommandBinding(command);
        }
    }

    /// <summary>
    /// The basic UI element
    /// </summary>
    public class Static : Data.IDerivedDeserialize
    {
        #region Events + Command Definitions

        //some standard/common events
        public const string PressEvent = "Press";
        public const string ClickEvent = "Click";
        public const string DragEvent = "Drag";
        public const string TextChangedEvent = "TextChanged";
        public const string ValueChangedEvent = "ValueChanged";
        public const string SelectionChangedEvent = "SelectionChanged";

        public static readonly HashSet<string> InputEvents = new HashSet<string> { PressEvent, ClickEvent, DragEvent };

        /// <summary>
        /// Global commands that are invoked if routed commands arent triggered
        /// </summary>
        public static Dictionary<string, System.Action<Static, object>> GlobalCommands
        {
            get => (_globalCommands ?? (_globalCommands = new Dictionary<string, System.Action<Static, object>>
                (System.StringComparer.OrdinalIgnoreCase)));
        }
        private static Dictionary<string, System.Action<Static, object>> _globalCommands;

        #endregion

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

        /// <summary>
        /// Marks a size to automatically expand to fit its contents
        /// </summary>
        public const float AutoSize = float.NaN;
        public const float InfiniteSize = float.PositiveInfinity;

        #region Properties

        /// <summary>
        /// All available/known styles (Apply custom styles using <see cref="ApplyStyles(Stylesheet, Static)"/>)
        /// </summary>
        public static Dictionary<string, Stylesheet> Styles { get; set; }

        const string DefaultStyleName = nameof(Static);

        public string Style
        {
            get => _style;
            set
            {
                if (_style == value)
                    return;

                _style = value;
                ApplyStyles(GetStyles(_style));
            }
        }
        private string _style;

        /// <summary>
        /// A font to use for drawing debug info.
        /// If null, debug info is not drawn
        /// </summary>
        public static Graphics.BitmapFont DebugFont = null;

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
        public virtual Graphics.BitmapFont Font
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
        private Graphics.BitmapFont _font;

        /// <summary>
        /// The color of this element. Usage varies between element types
        /// Usually applies to text color
        /// </summary>
        public virtual Color Color { get; set; } = Color.White;

        /// <summary>
        /// The color to draw the outline with, by default, transparent
        /// </summary>
        public virtual Color BorderColor { get; set; } = Color.Transparent;

        /// <summary>
        /// An optional fill color for this element, by default, transparent
        /// </summary>
        public virtual Color BackgroundColor { get; set; } = Color.Transparent;

        /// <summary>
        /// An optional background sprite to draw behind the element. Drawn over BackgroundColor
        /// </summary>
        public Graphics.NinePatch BackgroundSprite { get; set; }

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
                    //  System.Diagnostics.Debug.WriteLine($"{this}: Size=Infinity will always render as collapse");

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
        /// The last size returned by this element's <see cref="Measure"/>
        /// This includes padding and position
        /// </summary>
        [Data.DebugSerialize]
        public Vector2 MeasuredSize { get; private set; }

        /// <summary>
        /// The bounds of the content area, as determined by <see cref="Reflow"/>
        /// </summary>
        [Data.DebugSerialize]
        public Rectangle ContentArea { get; private set; }

        /// <summary>
        /// The <see cref="ContentArea"/> of this element offset relative to the screen
        /// </summary>
        [Data.DebugSerialize]
        public Rectangle OffsetContentArea { get; private set; }

        /// <summary>
        /// The visible region of this element on the screen, excluding padding
        /// </summary>
        [Data.DebugSerialize]
        public Rectangle VisibleContentArea { get; private set; }

        /// <summary>
        /// The visible region of this element, including padding
        /// </summary>
        [Data.DebugSerialize]
        public Rectangle VisibleBounds { get; private set; }

        public Point VisibleOffset => new Point(VisibleContentArea.X - OffsetContentArea.X, VisibleContentArea.Y - OffsetContentArea.Y);

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
        protected bool DidPressInside(MouseButtons button) =>
            didPress && InputState.IsButtonDown(button);

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
                Parent?.OnChildRemeasure(this); //todo: evaluate
            }
        }
        [Data.Serializer.Ignored]
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

        /// <summary>
        /// If not null, child elements will be bound with the specified property of this class
        /// </summary>
        public string ChildBindScope { get; set; }
        private Data.GetSet bindScopeGetset;

        #endregion

        public Static()
        {
            //System.Diagnostics.Debug.WriteLine($"New ID:{DebugId}");
            Children = _children.AsReadOnly();
#if DEBUG
            DebugTreePath = $"/{(GetType().Name)}({DebugId})";
#endif
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
                throw new System.ArgumentException("Children must be a list of UI elements");

            foreach (var element in elements)
            {
                if (element is Static child)
                    AddChild(child);
            }
        }

        #region Commands/Events

        /// <summary>
        /// Commands that can be bound to an action
        /// Commands are routed until an element has a matching action
        /// 
        /// Action&lt;Static, object&gt; Static is the sender (not source), object is an optional argument
        /// </summary>
        [Data.Serializer.Ignored]
        public Dictionary<string, System.Action<Static, object>> CommandActions =>
            (_commandActions ?? (_commandActions = new Dictionary<string, System.Action<Static, object>>
                (System.StringComparer.OrdinalIgnoreCase)));
        private Dictionary<string, System.Action<Static, object>> _commandActions;

        /// <summary>
        /// A map from events to commands
        /// e.g. Click->SpawnEntity
        /// Multiple commands can be called for a single event
        /// Evaluate in order and if any are handled (all run), the event is considered handled
        /// </summary>
        public Dictionary<string, EventCommandBinding> EventCommands
        {
            get => _eventCommands ?? (_eventCommands = new Dictionary<string, EventCommandBinding>(System.StringComparer.OrdinalIgnoreCase));
            set => _eventCommands = new Dictionary<string, EventCommandBinding>(value, System.StringComparer.OrdinalIgnoreCase); //mod passed in value?
        }
        private Dictionary<string, EventCommandBinding> _eventCommands;

        private Dictionary<string, UIEvent> events;

        public void On(string @event, UIEventHandler handler)
        {
            if (events == null)
                events = new Dictionary<string, UIEvent>(System.StringComparer.OrdinalIgnoreCase);

            if (!events.TryGetValue(@event, out var handlers))
                events[@event] = handlers = new UIEvent(true);
            handlers.AddHandler(handler);
        }

        public void On(IEnumerable<string> events, UIEventHandler handler)
        {
            if (this.events == null)
                this.events = new Dictionary<string, UIEvent>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var @event in events)
            {
                if (!this.events.TryGetValue(@event, out var handlers))
                    this.events[@event] = handlers = new UIEvent(true);
                handlers.AddHandler(handler);
            }
        }

        public bool Off(string @event, UIEventHandler handler)
        {
            if (events == null)
                return false;

            if (!events.TryGetValue(@event, out var handlers))
                return false;

            return handlers.RemoveHandler(handler);
        }

        public bool Off(IEnumerable<string> events, UIEventHandler handler)
        {
            if (this.events == null)
                return false;

            bool removed = false;
            foreach (var @event in events)
            {
                if (this.events.TryGetValue(@event, out var handlers))
                {
                    handlers.RemoveHandler(handler);
                    removed = true;
                }
            }

            return removed;
        }

        /// <summary>
        /// Turn all events off for a specific handler
        /// </summary>
        /// <param name="events">The events to remove handlers for</param>
        /// <returns>True if one or more of the handlers was turned off</returns>
        public bool Off(params string[] events)
        {
            bool removed = false;
            foreach (var @event in events)
                removed |= this.events.Remove(@event);
            return removed;
        }

        /// <summary>
        /// Remove all event handlers
        /// </summary>
        public void OffAll()
        {
            this.events.Clear();
        }

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

        protected void BubbleEvent(string @event, UIEventArgs args)
        {
            BubbleEvent(this, @event, args);
        }

        /// <summary>
        /// Bubble an event back towards the root element.
        /// Stops at any modal element if event is part of <see cref="InputEvents"/>
        /// </summary>
        /// <param name="source">The element to start bubbling from</param>
        /// <param name="event">The event name</param>
        /// <param name="eventArgs">Arguments for the event</param>
        protected void BubbleEvent(Static source, string @event, UIEventArgs eventArgs)
        {
            if (source == null || @event == null)
                return;

            //System.Diagnostics.Debug.WriteLine($"Bubbling event {@event} from {GetType().Name}({DebugId})");

            var target = source;
            while (target != null)
            {
                if (target._eventCommands != null && target.EventCommands.TryGetValue(@event, out var command) &&
                    BubbleCommand(command.command, command.argument))
                    return;

                if ((target.events != null && target.events.TryGetValue(@event, out var handlers) &&
                    handlers.Invoke(target, eventArgs) == UIEventResult.Handled) ||
                    (target.IsModal && InputEvents.Contains(@event))) //no events are routed to the parent when modal
                    return;

                target = target.Parent;
            }
        }

        /// <summary>
        /// Bubble a command up to the root element and then global handlers
        /// Stops once an element has the required handler
        /// </summary>
        /// <param name="command">The command to run</param>
        /// <param name="argument">An optional argument to pass thru</param>
        /// <returns>True if the command had a matching action. False otherwise or if command was null</returns>
        public bool BubbleCommand(string command, object argument = null)
        {
            if (command == null)
                return false;

            //System.Diagnostics.Debug.WriteLine($"Bubbling command {command} from {GetType().Name}({DebugId})");

            var target = this;
            while (target != null)
            {
                if (target._commandActions != null && target.CommandActions.TryGetValue(command, out var caction))
                {
                    //check if modal?
                    caction.Invoke(target /*this?*/, argument);
                    return true;
                }

                target = target.Parent;
            }

            if (target == null && GlobalCommands.TryGetValue(command, out var action))
            {
                action.Invoke(this, argument);
                return true;
            }

            return false;
        }


        //protected void TunnelEvent(string @event, UIEventArgs args)
        //{
        //    TunnelEvent(this, @event, args);
        //}

        //protected void TunnelEvent(Static source, string @event, UIEventArgs args)
        //{
        //    //from root to source
        //}

        #endregion

        #region Hierarchy/Cloning

        protected virtual void OnParentChanged(Static oldParent)
        {
        } //this event should not bubble and is internal

        private void SetParentNoReflow(Static newParent) //todo: re-evaluate necessity
        {
            var oldParent = _parent;
            _parent = newParent;
#if DEBUG
            //todo: make this on-demand?
            foreach (var child in EnumerateRecursive(true))
            {
                child.DebugTreePath = $"/{(child.GetType().Name)}({child.DebugId})";
                if (child.Parent != null)
                    child.DebugTreePath = child.Parent.DebugTreePath + child.DebugTreePath;
            }
#endif

            OnParentChanged(oldParent);
        }

        /// <summary>
        /// Create a clone of this static and all of its children
        /// Does not add to parent
        /// </summary>
        /// <returns>The cloned static</returns>
        public virtual Static CloneHierarchy()
        {
            var clone = (Static)MemberwiseClone();
#if DEBUG
            clone.DebugId = GenerateId();
#endif
            clone.SetParentNoReflow(null);
            clone._children = new List<Static>(_children);
            clone.Children = clone._children.AsReadOnly();
            for (int i = 0; i < clone._children.Count; ++i)
            {
                var child = clone._children[i].CloneHierarchy();
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
        /// The index of this child in its parents, -1 if no parent
        /// </summary>
        public int ChildIndex => Parent?.Children.IndexOf(this) ?? -1;

        /// <summary>
        /// Get the index of a child to this element
        /// </summary>
        /// <param name="child">The child to search</param>
        /// <returns>-1 if the element is not a child or null</returns>
        public int IndexOf(Static child)
        {
            return Children.IndexOf(child);
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
            //System.Diagnostics.Debug.WriteLine($"Inserting child ID:{child.DebugId} @ {index} into ID:{DebugId}");
            //todo: maybe have a forward setting (forward all additions to specified child)

            if (child == null || child.Parent == this)
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

            if (reflow) //force rebase now (prevents elements from popping in) ?
                OnChildRemeasure(child);

            return true;
        }

        public virtual bool InternalRemoveChildIndex(int index, bool reflow = true)
        {
            if (index < 0 || index >= Children.Count)
                return false;

            var child = Children[index];
            //System.Diagnostics.Debug.WriteLine($"Removing child ID:{child.DebugId} @ {index} from ID:{DebugId}");
            _children.RemoveAt(index);
            if (child.Parent == this)
                child.SetParentNoReflow(null);

            if (reflow)
                OnChildRemeasure(this);

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

            //todo: set parent normally?
            foreach (var child in children)
                child.SetParentNoReflow(null);

            Static lastFocus = null;
            foreach (var child in children)
            {
                if (InternalInsertChild(child, -1, false, true) && child.HasFocus)
                    lastFocus = child;
            }

            if (lastFocus != null)
                lastFocus.HasFocus = true;

            if (Children.Count != count)
                InvalidateMeasure();
        }

        /// <summary>
        /// Remove an element from this element. Does not search children
        /// </summary>
        /// <param name="child"></param>
        public Static RemoveChild(Static child)
        {
            InternalRemoveChildIndex(IndexOf(child));
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
        /// <param name="includeDisabled">Include elements that havee <see cref="IsEnabled"/> set to false (ignoring this)</param>
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

#if DEBUG
        public Static _FindInTreeByDebugId(uint id, bool breakOnResult = false)
        {
            var root = GetRoot();
            foreach (var elem in root.EnumerateRecursive(true))
            {
                if (elem.DebugId == id)
                {
                    if (breakOnResult)
                        elem.BreakOnThis();
                    return elem;
                }
            }
            return null;
        }
#endif

        /// <summary>
        /// Get the root element in this hierarchy
        /// </summary>
        /// <returns>The root element (or this if no parent)</returns>
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
                    var diff = (top.VisibleContentArea.Location - VisibleContentArea.Location).ToVector2();

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
        /// Find the focused element searching this and its children
        /// </summary>
        /// <returns>The focused element, or null if none</returns>
        public Static FindFocusedNoParent()
        {
            var next = new Stack<Static>();
            next.Push(this);

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
        /// Find the element in this tree that has focus (recursively)
        /// </summary>
        /// <returns>The focused element, or null if there is none</returns>
        public Static FindFocused()
        {
            var parent = this;
            while (parent.Parent != null)
                parent = parent.Parent;

            return parent.FindFocusedNoParent();
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

        /// <summary>
        /// Enumerate children with a matching binding source/target
        /// Searches siblings before nested children
        /// </summary>
        /// <param name="bindingSource">The name of the source binding to use, ignored if null</param>
        /// <param name="bindingTarget">The name of the target binding to use, ignored if null</param>
        /// <returns>A list of children with a matching binding</returns>
        public IEnumerable<Static> FindChildrenWithBinding(string bindingSource, string bindingTarget)
        {
            if (bindingSource == null && bindingTarget == null)
                throw new System.ArgumentException("bindingSource and bindingTarget cannot both be null");

            var stack = new Stack<Static>(Children);
            while (stack.Count > 0)
            {
                var top = stack.Pop();

                if (top.Bindings != null)
                {
                    foreach (var binding in top.Bindings)
                    {
                        if ((bindingSource == null || binding.Source == bindingSource) &&
                            (bindingTarget == null || binding.Target == bindingTarget))
                            yield return top;
                    }
                }

                foreach (var child in top.Children)
                    stack.Push(child);
            }
        }

        #endregion

        #region Layout

        //how many measures/arranges are currently queued
        //if 0, measure/arrange valid
        //if 1, next measure/arrange will act accordingly
        //if >1, skipped
        //this (hopefully) ensures that child measures/invalids happen in correct order
        private bool isMeasureValid = true;
        private bool isArrangeValid = true;

        private Vector2 lastMeasureAvailableSize = new Vector2(InfiniteSize);
        private Rectangle lastMeasureContainerBounds = Rectangle.Empty;

        /// <summary>
        /// Invalidat the size/measurement of this element.
        /// <see cref="Measure(Vector2)"/> will be called on this element at some point in the future.
        /// Typically called when an element is resized.
        /// </summary>
        public void InvalidateMeasure()
        {
            if (isMeasureValid)
            {
                isMeasureValid = false;
                measureQueue.Add(this);
            }
        }

        /// <summary>
        /// Invalidate the arrangement of this element.
        /// <see cref="Arrange(Rectangle)"/> will be called on this element at some point in the future.
        /// Typically called when an element is moved or was resized previously.
        /// </summary>
        public void InvalidateArrange()
        {
            if (isArrangeValid)
            {
                isArrangeValid = false;
                arrangeQueue.Add(this);
            }
        }
        /// <summary>
        /// force the entire tree to be remeasured and relayed-out
        /// </summary>
        public void DebugInvalidateTree()
        {
            foreach (var element in EnumerateRecursive())
                element.InvalidateMeasure();
        }

        /// <summary>
        /// Calculate the desired containing region of this element and its children. Can be customized through <see cref="MeasureOverride"/>.
        /// Sets <see cref="MeasuredSize"/> to value calculated
        /// This returns a size that is large enough to include the offset element with padding
        /// </summary>
        /// <returns>The desired size of this element, including padding</returns>
        public Vector2 Measure(Vector2 availableSize)
        {
            if (isMeasureValid && availableSize == lastMeasureAvailableSize)
                return MeasuredSize;
#if DEBUG
            ++totalMeasureCount;
#endif
            lastMeasureAvailableSize = availableSize;

            //System.Diagnostics.Debug.WriteLine($"Measuring ID:{DebugId} (available size:{availableSize})");

            var size = Size;
            bool isWidthAutoSize = float.IsNaN(size.X);
            bool isHeightAutoSize = float.IsNaN(size.Y);
            bool isHStretch = HorizontalAlignment == Alignment.Stretch;
            bool isVStretch = VerticalAlignment == Alignment.Stretch;

            if (availableSize.X < InfiniteSize)
            {
                availableSize.X -= Padding.X * 2;
                if (isWidthAutoSize && isHStretch)
                    availableSize.X -= (int)Position.X;
            }
            else if (!float.IsNaN(size.X))
                availableSize.X = size.X;

            if (availableSize.Y < InfiniteSize)
            {
                availableSize.Y -= Padding.Y * 2;
                if (isHeightAutoSize && isVStretch)
                    availableSize.Y -= (int)Position.Y;
            }
            else if (!float.IsNaN(size.Y))
                availableSize.Y = size.Y;

            var measuredSize = MeasureOverride(availableSize);
            if (isWidthAutoSize || isHeightAutoSize)
            {
                if (float.IsInfinity(measuredSize.X) || float.IsNaN(measuredSize.X)
                 || float.IsInfinity(measuredSize.Y) || float.IsNaN(measuredSize.Y))
                    throw new System./*NotFiniteNumberException*/InvalidOperationException("Measured size cannot be NaN or infinity");

                if (isWidthAutoSize)
                    size.X = measuredSize.X; //stretched items do have intrinsic size
                                             //size.X = isHStretch ? 0 : measuredSize.X; //stretched items have no intrinsic size

                if (isHeightAutoSize)
                    size.Y = measuredSize.Y; //stretched items do have intrinsic size
                                             //size.Y = isVStretch ? 0 : measuredSize.Y; //stretched items have no intrinsic size
            }

            var lastMeasuredSize = MeasuredSize;
            MeasuredSize = Position + size + Padding * 2;

            isMeasureValid = true;
            if (MeasuredSize != lastMeasuredSize)
            {
                InvalidateArrange();
                NotifyParentMeasuredSizeChanged();
            }

            return MeasuredSize;
        }

        void NotifyParentMeasuredSizeChanged()
        {
            if (Parent == null)
                return;

            Parent.OnChildRemeasure(this);
        }

        /// <summary>
        /// Called whenever a child of this element resizes.
        /// By default, will remeasure if the child is auto-sized
        /// </summary>
        /// <param name="child">The child that was remeasured</param>
        protected virtual void OnChildRemeasure(Static child)
        {
            if (IsAutoSized)
                //&& (HorizontalAlignment != Alignment.Stretch && VerticalAlignment != Alignment.Stretch)) broken; should be able to handle scrollboxes
                InvalidateMeasure();
            else
                InvalidateArrange();
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
            var textSize = new Point();
            if (Font != null && Text != null)
                textSize = Font.MeasureString(Text).ToPoint();

            var bounds = Rectangle.Union(
                new Rectangle(0, 0, textSize.X, textSize.Y),
                DefaultMeasureSomeChildren(availableSize, 0, Children.Count)
            );
            return new Vector2(bounds.Width, bounds.Height);
        }

        /// <summary>
        /// Get the clip bounds for a selection of (enabled) child elements
        /// </summary>
        /// <param name="startIndex">The first child to measure</param>
        /// <param name="count">The number of children to measure (including disabled)</param>
        /// <returns>The measured area</returns>
        protected Rectangle DefaultMeasureSomeChildren(Vector2 availableSize, int startIndex, int count)
        {
            var bounds = new Rectangle();
            for (int i = startIndex; i < System.Math.Min(Children.Count, startIndex + count); ++i)
            {
                if (!Children[i].IsEnabled)
                    continue;

                var mes = Children[i].Measure(availableSize).ToPoint();
                bounds = Rectangle.Union(bounds, new Rectangle(
                    0, //measured size includes offset
                    0, //ditto
                    mes.X,
                    mes.Y
                ));
            }
            return bounds;
        }

#if DEBUG
        private static uint totalMeasureCount = 0; //set breakpoint for speicifc reflow
        private static uint totalArrangeCount = 0; //set breakpoint for speicifc reflow
        private static System.TimeSpan lastUpdateDuration;
        private static System.TimeSpan lastDrawDuration;
        private static System.Diagnostics.Stopwatch boop = new System.Diagnostics.Stopwatch();//todo: should be one for update and one for draw
#endif

        /// <summary>
        /// Reflow this container, relative to its parent
        /// </summary>
        /// <param name="container">Container in relative coordinates</param>
        public void Arrange(Rectangle container)
        {
            isArrangeValid = true;
#if DEBUG
            ++totalArrangeCount;
#endif
            //todo: this needs to be called less

            //System.Diagnostics.Debug.WriteLine($"Arranging ID:{DebugId} ({this}) [container:{container}]");
            AdjustToContainer(container);
            ArrangeOverride(ContentArea.Size.ToVector2()); //todo: this needs to be visibleDimensions (?)
        }

        /// <summary>
        /// Reflow child elements relative to this element
        /// Called whenever this element's position or size is adjusted
        /// </summary>
        protected virtual void ArrangeOverride(Vector2 availableSize)
        {
            foreach (var child in Children)
            {
                if (child.IsEnabled)
                    child.Arrange(new Rectangle(0, 0, (int)availableSize.X, (int)availableSize.Y));
            }
        }

        /// <summary>
        /// Calculate all of the bounds to this element in relation to a container.
        /// </summary>
        /// <param name="container">The container to fit this to, in relative coordinates</param>
        private void AdjustToContainer(Rectangle container)
        {
            Rectangle parentContentArea;
            Rectangle parentBounds;
            var offsetParent = Point.Zero;
            if (Parent == null)
                parentBounds = parentContentArea = Runtime.GraphicsDevice.Viewport.Bounds;
            else
            {
                offsetParent = Parent.OffsetContentArea.Location;
                parentContentArea = Parent.VisibleContentArea;
                parentBounds = Parent.VisibleBounds;
            }

            var finalSize = MeasuredSize;

            //todo: should this go into Measure?
            if (HorizontalAlignment == Alignment.Stretch)
                finalSize.X = float.IsNaN(Size.X) ? container.Width : Size.X;
            if (VerticalAlignment == Alignment.Stretch)
                finalSize.Y = float.IsNaN(Size.Y) ? container.Height : Size.Y;

            var localPos = new Vector2(
                GetAlignedPosition(HorizontalAlignment, Position.X, finalSize.X, Padding.X, container.Width),
                GetAlignedPosition(VerticalAlignment, Position.Y, finalSize.Y, Padding.Y, container.Height)
            );
            var bounds = new Rectangle((int)localPos.X, (int)localPos.Y, (int)(finalSize.X), (int)(finalSize.Y));

            bounds.Width -= (int)(Padding.X * 2 + Position.X);
            bounds.Height -= (int)(Padding.Y * 2 + Position.Y);
            bounds.Offset(container.Location);
            ContentArea = bounds;

            var tmp = bounds;
            tmp.Offset(offsetParent);
            OffsetContentArea = tmp;

            tmp = Rectangle.Intersect(bounds, container);
            tmp.Offset(offsetParent);
            VisibleContentArea = Rectangle.Intersect(tmp, parentContentArea);

            tmp.Inflate(Padding.X, Padding.Y);
            VisibleBounds = Rectangle.Intersect(tmp, parentBounds);

            lastMeasureContainerBounds = container;
        }

        public float GetAlignedPosition(Alignment alignment, float position, float size, float padding, float containerSize)
        {
            switch (alignment)
            {
                case Alignment.Middle:
                case Alignment.Stretch: // stretched items will either fill full area or center in available space
                    return (containerSize - size + padding * 2) / 2 + position;
                case Alignment.End:
                    return containerSize - size; //size includes padding and position
                default:
                    return position + padding;
            }
        }

        static List<Static> measureQueue = new List<Static>();
        static List<Static> arrangeQueue = new List<Static>();

        /// <summary>
        /// Complete any pending reflows/arranges
        /// </summary>
        public static void Reflow()
        {
            for (int i = 0; i < measureQueue.Count; ++i)
            {
                measureQueue[i].Measure(measureQueue[i].lastMeasureAvailableSize);
                if (!measureQueue[i].isMeasureValid) //todo: que?
                    measureQueue[i].Measure(new Vector2(InfiniteSize));
            }
            measureQueue.Clear();
            for (int i = 0; i < arrangeQueue.Count; ++i)
            {
                if (!arrangeQueue[i].isArrangeValid)
                    arrangeQueue[i].Arrange(arrangeQueue[i].lastMeasureContainerBounds);
            }
            arrangeQueue.Clear();
        }

        #endregion

        #region Updating/Drawing

        /// <summary>
        /// Update this element and all of its children
        /// </summary>
        /// <param name="time">Game time</param>
        public virtual void Update(GameTime time)
        {
            boop.Restart();
            Reflow();

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
                {
                    handleInput = toUpdate.HandleInput(time) && !toUpdate.IsModal;
                }

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
            lastUpdateDuration = boop.Elapsed;
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
                foreach (var binding in Bindings)
                    binding.Update();
            }

            //child bind scopes (hacky)
            if (bindScopeGetset.get != null)
            {
                var newVal = bindScopeGetset.get();
                var newHash = (newVal ?? 0).GetHashCode();
                if (newHash != bindScopeGetset.cachedHash)
                {
                    bindScopeGetset = Data.GetSet.GetMemberAccessors(newVal, ChildBindScope);
                    bindScopeGetset.cachedValue = newVal;
                    bindScopeGetset.cachedHash = newHash;

                    foreach (var child in Children)
                        child.BindTo(newVal);
                }
            }
        }

        /// <summary>
        /// React to user input here. Updating should be performed in <see cref="UpdateSelf"/>
        /// </summary>
        /// <param name="time">game time</param>
        /// <returns>False if the input has been handled by this UI</returns>
        protected virtual bool HandleInput(GameTime time)
        {
            if (HasFocus)
            {
                if ((!ignoreTabKey && InputState.IsPress(Keys.Tab)) ||
                    InputState.IsAnyPress(Buttons.RightShoulder))
                {
                    if (InputState.IsMod(KeyMod.Shift))
                        FocusPrevious();
                    else
                        FocusNext();
                    return false;
                }

                if (InputState.IsAnyPress(Buttons.LeftShoulder))
                {
                    FocusPrevious();
                    return false;
                }

                var thumb = InputState.Thumbsticks().Left;
                var lastThumb = InputState.LastThumbsticks().Left;
                if (thumb != Vector2.Zero && lastThumb == Vector2.Zero)
                {
                    if (FocusGeographically(thumb) != null)
                        return false;
                }

                if (!ignoreEnterKey && InputState.IsPress(Keys.Enter))
                {
                    TriggerClick(Vector2.Zero, (int)Keys.Enter, DeviceType.Keyboard);
                    return false;
                }
                if (!ignoreSpaceKey && InputState.IsPress(Keys.Space))
                {
                    TriggerClick(Vector2.Zero, (int)Keys.Space, DeviceType.Keyboard);
                    return false;
                }
                if (InputState.IsAnyPress(Buttons.A, out var player))
                {
                    TriggerClick(Vector2.Zero, (int)Keys.Enter, DeviceType.Gamepad, (int)player);
                    return false;
                }
            }

            if (!HandleTouchInput())
                return false;

            //todo: improve
            if (InputState.IsPress(0) && VisibleBounds.Contains(InputState.touches[0].Position))
            {
                TriggerClick(Vector2.Zero, 0, DeviceType.Touch);

                if (CanFocus)
                {
                    HasFocus = true;
                    return false;
                }

                //return false;
            }

            var mouse = InputState.MousePoint;
            return HandleMouseInput(mouse, MouseButtons.Left) &&
                HandleMouseInput(mouse, MouseButtons.Right) &&
                HandleMouseInput(mouse, MouseButtons.Middle);
        }

        bool HandleTouchInput()
        {
            if (InputState.Gestures.TryGetValue(GestureType.Tap, out var gesture) && VisibleBounds.Contains(gesture.Position))
            {
                var pea = new PointerEventArgs(this)
                {
                    position = (gesture.Position - OffsetContentArea.Location.ToVector2()) + Padding,
                    button = 0,
                    device = DeviceType.Touch
                };
                BubbleEvent(PressEvent, pea);
                BubbleEvent(ClickEvent, pea);

                if (CanFocus)
                {
                    HasFocus = true;
                    return false;
                }
            }

            return true;
        }

        bool HandleMouseInput(Point mousePosition, MouseButtons button)
        {
            //create a button map?

            if (InputState.IsPress(button) && VisibleBounds.Contains(mousePosition))
            {
                var pea = new PointerEventArgs(this)
                {
                    position = (mousePosition - OffsetContentArea.Location).ToVector2() + Padding,
                    button = (int)button,
                    device = DeviceType.Mouse
                };
                BubbleEvent(PressEvent, pea);

                didPress = true; //todo: should be per button
                if (CanFocus)
                {
                    HasFocus = true;
                    return false; //todo: always return false?
                }
            }

            //input capture
            //todo: maybe add capture setting
            else if (DidPressInside(button))
            {
                var lastMousePosition = InputState.LastMouseVector;
                var newMousePosition = InputState.MouseVector;
                if (lastMousePosition != newMousePosition)
                {
                    var pea = new DragEventArgs(this)
                    {
                        delta = newMousePosition - lastMousePosition,
                        position = (mousePosition - OffsetContentArea.Location).ToVector2() + Padding,
                        button = (int)button,
                        device = DeviceType.Mouse
                    };
                    BubbleEvent(DragEvent, pea);
                }
                return false;
            }

            else if (InputState.IsButtonUp(button))
            //else if (InputState.Gestures.TryGetValue(GestureType.Tap, out var gesture))
            {
                if (didPress && VisibleBounds.Contains(mousePosition)) //gesture pos
                {
                    TriggerClick(
                        (mousePosition - OffsetContentArea.Location).ToVector2() + Padding,
                        (int)button,
                        DeviceType.Mouse
                    );
                    if (InputState.IsClick(button))
                        didPress = false;
                    return false;
                }
                if (InputState.IsClick(button))
                    didPress = false;
            }

            return true;
        }

        /// <summary>
        /// Draw this element, its decorators, and any children
        ///
        /// Draws depth-first, parent-most first
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to use</param>
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            boop.Restart();
            if (!IsEnabled)
                return;

            var draws = new Stack<Static>(Children.Count + 1);
            draws.Push(this);

            Static debugDraw = null;
            while (draws.Count > 0)
            {
                var toDraw = draws.Pop();

                if (toDraw.BackgroundColor.A > 0)
                    Graphics.Primitives2D.DrawFill(spriteBatch, toDraw.BackgroundColor, toDraw.VisibleBounds);
                toDraw.BackgroundSprite.Draw(spriteBatch, toDraw.VisibleBounds);

                toDraw.DrawSelf(spriteBatch);

                var borderColor = (toDraw.HasFocus && toDraw.CanFocus) ? FocusedBorderColor : toDraw.BorderColor;
                if (DebugFont != null && borderColor == Color.Transparent)
                    borderColor = isMeasureValid && isArrangeValid ? Color.SteelBlue : Color.Tomato;

                if (DebugFont != null && toDraw.VisibleBounds.Contains(InputState.MousePoint))
                    debugDraw = toDraw;

                if (borderColor.A > 0)
                {
                    var offsetRect = toDraw.OffsetContentArea;
                    offsetRect.Inflate(toDraw.Padding.X, toDraw.Padding.Y);
                    var offset = offsetRect.Location.ToVector2();
                    DrawHLine(spriteBatch, borderColor, 0, 0, offsetRect.Width, offset, toDraw.VisibleBounds);
                    DrawVLine(spriteBatch, borderColor, offsetRect.Width, 0, offsetRect.Height, offset, toDraw.VisibleBounds);
                    DrawHLine(spriteBatch, borderColor, offsetRect.Height, 0, offsetRect.Width, offset, toDraw.VisibleBounds);
                    DrawVLine(spriteBatch, borderColor, 0, 0, offsetRect.Height, offset, toDraw.VisibleBounds);
                }

                for (int i = toDraw.Children.Count - 1; i >= 0; --i)
                {
                    if (toDraw.Children[i].IsEnabled)
                        draws.Push(toDraw.Children[i]);
                }
            }

#if DEBUG //todo: re-evaluate
            if (debugDraw != null)
            {
                DebugFont.Draw(
                    spriteBatch,
                    $"Measure Count: {totalMeasureCount}\n" +
                    $"Arrange Count: {totalArrangeCount}\n" +
                    $"Total Elements Created: {idCounter}\n" +
                    $"Total binding Updates: {Takai.Data.Binding.TotalUpdateCount}",
                    new Vector2(10),
                    Color.CornflowerBlue
                );

                debugDraw.DrawDebugInfo(spriteBatch);
                if (InputState.IsPress(Keys.Pause))
                    debugDraw.BreakOnThis();

                if (InputState.IsPress(Keys.F9))
                {
                    using (var stream = new System.IO.StreamWriter(System.IO.File.OpenWrite("ui.tk")))
                        Data.Serializer.TextSerialize(stream, debugDraw, 0, false, false, true);
                }
            }
#endif

            lastDrawDuration = boop.Elapsed;
        }

        private void BreakOnThis()
        {
            System.Diagnostics.Debugger.Break();
        }

        public void DrawDebugInfo(SpriteBatch spriteBatch)
        {
            Graphics.Primitives2D.DrawRect(spriteBatch, Color.Cyan, VisibleBounds);

            var rect = OffsetContentArea;
            Graphics.Primitives2D.DrawRect(spriteBatch, new Color(Color.Orange, 0.5f), rect);

            rect.Inflate(Padding.X, Padding.Y);
            Graphics.Primitives2D.DrawRect(spriteBatch, Color.OrangeRed, rect);

            string info = $"`_{GetType().Name}`_\n"
#if DEBUG
                         + $"ID: {DebugId}\n"
#endif
                         + $"Name: {(Name ?? "(No name)")}\n"
#if DEBUG
                         + $"Parent ID: {Parent?.DebugId}\n"
#endif
                         + $"Children: {Children?.Count ?? 0}\n"
                         + $"Bounds: {OffsetContentArea}\n" //visible bounds?
                         + $"Position: {Position}, Size: {Size}, Padding: {Padding}\n"
                         + $"HAlign: {HorizontalAlignment}, VAlign: {VerticalAlignment}\n"
                         + $"Bindings: {(Bindings == null ? "(None)" : string.Join(",", Bindings))}";

            var drawPos = rect.Location + new Point(rect.Width + 10, rect.Height + 10);
            var size = DebugFont.MeasureString(info);
            drawPos = Util.Clamp(new Rectangle(drawPos.X, drawPos.Y, (int)size.X, (int)size.Y), Runtime.GraphicsDevice.Viewport.Bounds);
            drawPos -= new Point(10);
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
        /// Draw text clipped to the visible region of this element
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to use</param>
        /// <param name="position">The relative position (to the element) to draw this text</param>
        protected void DrawText(SpriteBatch spriteBatch, Point position)
        {
            if (Font == null || Text == null)
                return;

            position += (OffsetContentArea.Location - VisibleContentArea.Location);
            Font.Draw(spriteBatch, Text, 0, Text.Length, VisibleContentArea, position, Color);
        }

        /// <summary>
        /// Draw a line clipped to the visible region of this element
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to use</param>
        /// <param name="a">The start of the line</param>
        /// <param name="b">The end of the line</param>
        protected void DrawLine(SpriteBatch spriteBatch, Color color, Vector2 a, Vector2 b)
        {
            //todo: Map.DrawLine has color at end of arglist
            throw new System.NotImplementedException();
        }

        protected void DrawVLine(SpriteBatch spriteBatch, Color color, float x, float y1, float y2)
        {
            DrawVLine(spriteBatch, color, x, y1, y2, OffsetContentArea.Location.ToVector2(), VisibleContentArea);
        }
        private void DrawVLine(SpriteBatch spriteBatch, Color color, float x, float y1, float y2, Vector2 offset, Rectangle visibleClip)
        {
            x += offset.X;
            if (x < visibleClip.Left || x > visibleClip.Right)
                return;

            y1 = Util.Clamp(y1 + offset.Y, visibleClip.Top, visibleClip.Bottom);
            y2 = Util.Clamp(y2 + offset.Y, visibleClip.Top, visibleClip.Bottom);

            if (y1 == y2)
                return;

            Graphics.Primitives2D.DrawLine(spriteBatch, color, new Vector2(x, y1), new Vector2(x, y2));
        }

        protected void DrawHLine(SpriteBatch spriteBatch, Color color, float y, float x1, float x2)
        {
            DrawHLine(spriteBatch, color, y, x1, x2, OffsetContentArea.Location.ToVector2(), VisibleContentArea);
        }
        private void DrawHLine(SpriteBatch spriteBatch, Color color, float y, float x1, float x2, Vector2 offset, Rectangle visibleClip)
        {
            y += offset.Y;
            if (y < visibleClip.Top || y > visibleClip.Bottom)
                return;

            x1 = Util.Clamp(x1 + offset.X, visibleClip.Left, visibleClip.Right);
            x2 = Util.Clamp(x2 + offset.X, visibleClip.Left, visibleClip.Right);

            if (x1 == x2)
                return;

            Graphics.Primitives2D.DrawLine(spriteBatch, color, new Vector2(x1, y), new Vector2(x2, y));
        }

        protected void DrawRect(SpriteBatch spriteBatch, Color color, Rectangle rect)
        {
            var offset = OffsetContentArea.Location.ToVector2();
            DrawHLine(spriteBatch, color, rect.Top, rect.Left, rect.Right, offset, VisibleContentArea);
            DrawVLine(spriteBatch, color, rect.Right, rect.Top, rect.Bottom, offset, VisibleContentArea);
            DrawHLine(spriteBatch, color, rect.Bottom, rect.Left, rect.Right, offset, VisibleContentArea);
            DrawVLine(spriteBatch, color, rect.Left, rect.Top, rect.Bottom, offset, VisibleContentArea);
        }

        protected void DrawSprite(SpriteBatch spriteBatch, Graphics.Sprite sprite, Rectangle destRect)
        {
            DrawSpriteCustomRegion(spriteBatch, sprite, destRect, VisibleContentArea);
        }

        void DrawSpriteCustomRegion(SpriteBatch spriteBatch, Graphics.Sprite sprite, Rectangle destRect, Rectangle clipRegion)
        {
            //todo: use this w/ background sprite

            if (sprite?.Texture == null || destRect.Width == 0 || destRect.Height == 0)
                return;

            var sx = sprite.Width / (float)destRect.Width;
            var sy = sprite.Height / (float)destRect.Height;

            var dx = VisibleContentArea.X - OffsetContentArea.X;
            var dy = VisibleContentArea.Y - OffsetContentArea.Y;

            destRect.X += clipRegion.X - dx;
            destRect.Y += clipRegion.Y - dy;
            //destRect.Width = System.Math.Min(destRect.Width, clipRegion.Width);
            //destRect.Height = System.Math.Min(destRect.Height, clipRegion.Height);
            var finalRect = Rectangle.Intersect(destRect, clipRegion);

            var vx = -(int)((finalRect.Width - destRect.Width) * sx);
            var vy = -(int)((finalRect.Height - destRect.Height) * sy);
            var clip = new Rectangle(
                vx,
                vy,
                sprite.Width - vx,
                sprite.Height - vy
            );

            sprite.Draw(spriteBatch, finalRect, clip, 0, Color.White, sprite.ElapsedTime);

            //Graphics.Primitives2D.DrawRect(spriteBatch, Color.Gold, finalRect);
        }

        #endregion

        public override string ToString()
        {
            string extraInfo = "";
#if DEBUG
            extraInfo = $" ID:{DebugId}";
#endif
            return $"{base.ToString()} {{{Name ?? "(No name)"}}}{(HasFocus ? "*" : "")} \"{Text ?? ""}\" {(IsEnabled ? "👁" : "❌")}{extraInfo}";
        }

        public virtual void DerivedDeserialize(Dictionary<string, object> props)
        {
            //allow autosizing only one dimension
            if (props.TryGetValue("Width", out var width))
                Size = new Vector2(Data.Serializer.Cast<float>(width), Size.Y);

            if (props.TryGetValue("Height", out var height))
                Size = new Vector2(Size.Y, Data.Serializer.Cast<float>(height));
        }

        protected T GetStyleRule<T>(Stylesheet styleRules, string propName, T fallback)
        {
            //Cast?
            if (styleRules == null || !styleRules.TryGetValue(propName, out var sProp) || !(sProp is T prop))
                prop = fallback;
            return prop;
        }

        public static Stylesheet GetStyles(string styleName)
        {
            Stylesheet styles = null;
            if (styleName != null)
                Styles.TryGetValue(styleName, out styles);
            return styles;
        }

        public virtual void ApplyStyles(Stylesheet styleRules)
        {
            if (styleRules != null && styleRules.TryGetValue("proto", out var proto))
            {
                if (proto is string str)
                    ApplyStyles(GetStyles(str));
                //support proto = dictionary
            }
            else
            {
                var sr = GetStyles(DefaultStyleName);
                if (sr != styleRules)
                    ApplyStyles(sr);
            }
            //if "proto" is explicitly null, then don't inherit

            Color = GetStyleRule(styleRules, "Color", Color);
            Font = GetStyleRule(styleRules, "Font", Font);

            BorderColor = GetStyleRule(styleRules, "BorderColor", BorderColor);
            BackgroundColor = GetStyleRule(styleRules, "BackgroundColor", BackgroundColor);
            BackgroundSprite = GetStyleRule(styleRules, "BackgroundSprite", BackgroundSprite);

            Padding = GetStyleRule(styleRules, "Padding", Padding);
            HorizontalAlignment = GetStyleRule(styleRules, "HorizontalAlignment", HorizontalAlignment);
            VerticalAlignment = GetStyleRule(styleRules, "VerticalAlignment", VerticalAlignment);
            Position = GetStyleRule(styleRules, "Position", Position);
            Size = GetStyleRule(styleRules, "Size", Size);

            //base on rules
        }

        #region Helpers

        //todo: better name

        public void TriggerClick(Vector2 relativePosition, int button = 0, DeviceType device = DeviceType.Mouse, int deviceIndex = 0)
        {
            var ce = new PointerEventArgs(this)
            {
                position = relativePosition,
                button = 0,
                device = device,
                deviceIndex = deviceIndex
            };
            BubbleEvent(ClickEvent, ce);
        }

        public static Static GeneratePropSheet(object obj)
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
                            IsChecked = @enum.HasFlag(value)
                        };
                        check.On(ClickEvent, delegate (Static sender, UIEventArgs e)
                        {
                            throw new System.NotImplementedException(); //verify that the code below works

                            var chkbx = (CheckBox)sender;
                            var parsed = System.Convert.ToUInt64(System.Enum.Parse(type, chkbx.Name));
                            var n = System.Convert.ToUInt64(@enum);

                            if (chkbx.IsChecked)
                                obj = System.Enum.ToObject(type, n | parsed);
                            else
                                obj = System.Enum.ToObject(type, n & ~parsed);

                            return UIEventResult.Handled;
                        });
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
                            new Data.Binding(member.Name, "IsChecked", Data.BindingDirection.TwoWay)
                        }
                    };
                    checkbox.BindTo(obj);
                    root.AddChild(checkbox);
                    continue;
                }

                root.AddChild(new Static(Util.ToSentenceCase(member.Name))); //label

                if (Data.Serializer.IsInt(member) ||
                    Data.Serializer.IsFloat(member))
                {
                    var numeric = new NumericInput
                    {
                        Bindings = new List<Data.Binding>
                        {
                            new Data.Binding(member.Name, "Value", Data.BindingDirection.TwoWay)
                        }
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
                            new Data.Binding(member.Name, "Text", Data.BindingDirection.TwoWay)
                        }
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
