using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Takai.Input;
using Takai.Graphics;

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

    public class UIEventArgs : EventArgs
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
    public struct DrawContext
    {
        public SpriteBatch spriteBatch;
        public TextRenderer textRenderer;
    }

    /// <summary>
    /// The basic UI element
    /// </summary>
    //[Data.Cache.AlwaysReload] //required for multiple imports of same file, otherwise not required
    public partial class Static : Data.IDerivedDeserialize
    {
        #region Events + Command Definitions

        //some standard/common events
        public const string PressEvent = "Press";
        public const string ClickEvent = "Click";
        public const string DragEvent = "Drag";
        public const string TextChangedEvent = "TextChanged";
        public const string ValueChangedEvent = "ValueChanged";
        public const string SelectionChangedEvent = "SelectionChanged";

        //some common style states
        public const string HoverState = "Hover";
        public const string ActiveState = "Active";
        public const string FocusState = "Focus";

        public static readonly HashSet<string> InputEvents = new HashSet<string> { PressEvent, ClickEvent, DragEvent };

        /// <summary>
        /// Global commands that are invoked if routed commands arent triggered
        /// </summary>
        public static Dictionary<string, Action<Static, object>> GlobalCommands
        {
            get => (_globalCommands ?? (_globalCommands = new Dictionary<string, Action<Static, object>>
                (StringComparer.OrdinalIgnoreCase)));
        }
        private static Dictionary<string, Action<Static, object>> _globalCommands;

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
                //todo: this needs to clear styles when switching
            }
        }
        private string _style;

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
        /// The last size returned by this element's <see cref="Measure"/>
        /// This includes padding and position
        /// </summary>
        [Data.DebugSerialize]
        public Vector2 MeasuredSize { get; private set; }

        /// <summary>
        /// The bounds of the content area, as determined by <see cref="PerformReflows"/>
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
                        if (next._hasFocus)
                            ApplyStyles(GetStyles(Style, "Focus")); //needs to not overwrite other states (eg both focus and hover)
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
        /// Was the current mouse press inside this element
        /// </summary>
        private BitVector32 didPress = new BitVector32(0);

        /// <summary>
        /// Was a drag event registered? (prevents click events)
        /// </summary>
        private bool didDrag; //meld with didPress?

        /// <summary>
        /// Was the mouse pressed inside this static (and is the mouse still down)
        /// </summary>
        /// <returns>True if the mouse is currently down and was pressed inside this static</returns>
        protected bool DidPressInside(MouseButtons button) =>
            didPress[1 << (int)button] && InputState.IsButtonDown(button);

        /// <summary>
        /// Was the touch pressed inside this static (and is the mouse still down)
        /// </summary>
        /// <returns>True if the mouse is currently down and was pressed inside this static</returns>
        protected bool DidPressInside(int touchIndex) =>
            didPress[1 << (touchIndex + (int)MouseButtons._TouchIndex)] && InputState.IsButtonDown(touchIndex);

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

        #region Commands/Events

        /// <summary>
        /// Commands that can be bound to an action
        /// Commands are routed until an element has a matching action
        /// 
        /// Action&lt;Static, object&gt; Static is the sender (not source), object is an optional argument
        /// </summary>
        [Data.Serializer.Ignored]
        public Dictionary<string, Action<Static, object>> CommandActions =>
            (_commandActions ?? (_commandActions = new Dictionary<string, Action<Static, object>>
                (StringComparer.OrdinalIgnoreCase)));
        private Dictionary<string, Action<Static, object>> _commandActions;

        /// <summary>
        /// A map from events to commands
        /// e.g. Click->SpawnEntity
        /// Multiple commands can be called for a single event
        /// Evaluate in order and if any are handled (all run), the event is considered handled
        /// </summary>
        public Dictionary<string, EventCommandBinding> EventCommands
        {
            get => _eventCommands ?? (_eventCommands = new Dictionary<string, EventCommandBinding>(StringComparer.OrdinalIgnoreCase));
            set => _eventCommands = new Dictionary<string, EventCommandBinding>(value, StringComparer.OrdinalIgnoreCase); //mod passed in value?
        }
        private Dictionary<string, EventCommandBinding> _eventCommands;

        private Dictionary<string, UIEvent> events;

        public void On(string @event, UIEventHandler handler)
        {
            if (events == null)
                events = new Dictionary<string, UIEvent>(StringComparer.OrdinalIgnoreCase);

            if (!events.TryGetValue(@event, out var handlers))
                events[@event] = handlers = new UIEvent(true);
            handlers.AddHandler(handler);
        }

        public void On(IEnumerable<string> events, UIEventHandler handler)
        {
            if (this.events == null)
                this.events = new Dictionary<string, UIEvent>(StringComparer.OrdinalIgnoreCase);

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

        /// <summary>
        /// Bubble an event up towards the root element
        /// Stops at any modal element if event is part of <see cref="InputEvents"/>
        /// </summary>
        /// <param name="event">The event name</param>
        /// <param name="eventArgs">Arguments for the event</param>
        /// <returns>The UI that handled this event (returned UIEVentResult.Handled), null if none</returns>
        protected Static BubbleEvent(string @event, UIEventArgs args)
        {
            return BubbleEvent(this, @event, args);
        }

        /// <summary>
        /// Bubble an event up towards the root element.
        /// Stops at any modal element if event is part of <see cref="InputEvents"/>
        /// </summary>
        /// <param name="source">The element to start bubbling from</param>
        /// <param name="event">The event name</param>
        /// <param name="eventArgs">Arguments for the event</param>
        /// <returns>The UI that handled this event (returned UIEVentResult.Handled), null if none</returns>
        protected Static BubbleEvent(Static source, string @event, UIEventArgs eventArgs)
        {
            if (source == null || @event == null)
                return null;

            //Diagnostics.Debug.WriteLine($"Bubbling event {@event} from {GetType().Name}({DebugId})");

            var target = source;
            while (target != null)
            {
                if (target._eventCommands != null && target.EventCommands.TryGetValue(@event, out var command))
                {
                    var cmdTarget = BubbleCommand(command.command, command.argument);
                    if (cmdTarget != null)
                        return cmdTarget;
                }

                if ((target.events != null && target.events.TryGetValue(@event, out var handlers) &&
                    handlers.Invoke(target, eventArgs) == UIEventResult.Handled) ||
                    (target.IsModal && InputEvents.Contains(@event))) //no events are routed to the parent when modal
                    return target;

                target = target.Parent;
            }

            return null;
        }

        /// <summary>
        /// Bubble a command up to the root element and then global handlers
        /// Stops once an element has the required handler
        /// </summary>
        /// <param name="command">The command to run</param>
        /// <param name="argument">An optional argument to pass thru</param>
        /// <returns>The element that acted upon the command, null if none (or global)</returns>
        public Static BubbleCommand(string command, object argument = null)
        {
            if (command == null)
                return null;

            //Diagnostics.Debug.WriteLine($"Bubbling command {command} from {GetType().Name}({DebugId})");

            var target = this;
            while (target != null)
            {
                if (target._commandActions != null && target.CommandActions.TryGetValue(command, out var caction))
                {
                    //check if modal?
                    caction.Invoke(target /*this?*/, argument);
                    return target;
                }

                target = target.Parent;
            }

            if (target == null && GlobalCommands.TryGetValue(command, out var action))
            {
                action.Invoke(this, argument);
                return null;
            }

            return null;
        }

        //protected void TunnelEvent(string @event, UIEventArgs args)
        //{
        //    TunnelEvent(this, @event, args);
        //}

        //protected void TunnelEvent(Static source, string @event, UIEventArgs args)
        //{
        //    //from root to source
        //}

        protected virtual void OnParentChanged(Static oldParent)
        {
            //todo: make pubically accessable (on modal closed e.g.)
        } //this event should not bubble and is internal

        #endregion

        #region Hierarchy/Cloning

        private void SetParentNoReflow(Static newParent) //todo: re-evaluate necessity
        {
            var oldParent = _parent;
            _parent = newParent;
#if DEBUG
            //todo: make this on-demand?
            foreach (var child in EnumerateRecursive())
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
        protected virtual bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
        {
            //Diagnostics.Debug.WriteLine($"Inserting child ID:{child.DebugId} @ {index} into ID:{DebugId}");
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

        /// <summary>
        /// Swap a child, can be set to null
        /// </summary>
        /// <param name="child">The new child to replace the old with</param>
        /// <param name="index">The index to swap (must be in range)</param>
        /// <param name="reflow">Reflow after swapping</param>
        /// <returns>The old element that was swapped out, or null if the index is out of bounds</returns>
        protected virtual Static InternalSwapChild(Static child, int index, bool reflow = true, bool ignoreFocus = false)
        {
            if (index < 0 || index >= Children.Count)
                return null;

            var old = Children[index];
            if (old.Parent == this)
                old.SetParentNoReflow(null);

            _children[index] = child;
            if (child != null)
            {
                child.SetParentNoReflow(this);
                if (child.HasFocus && !ignoreFocus)
                    child.HasFocus = true;
            }

            if (reflow)
                OnChildRemeasure(this);

            return old;
        }

        /// <summary>
        /// Remove a child element at the specified index
        /// </summary>
        /// <param name="index">The index to remove</param>
        /// <param name="reflow">Reflow after removing</param>
        /// <returns>The element that was removed, or null if the index is out of bounds</returns>
        protected virtual Static InternalRemoveChild(int index, bool reflow = true)
        {

            if (index < 0 || index >= Children.Count)
                return null;

            var old = Children[index];
            if (old.Parent == this)
                old.SetParentNoReflow(null);

            _children.RemoveAt(index);

            if (reflow)
                OnChildRemeasure(this);

            return old;
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
            InternalSwapChild(child, index);
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
                child?.SetParentNoReflow(null);

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
            var index = IndexOf(child);
            InternalRemoveChild(index);
            return child;
        }

        public Static RemoveChildAt(int index)
        {
            return InternalRemoveChild(index);
        }

        public void RemoveAllChildren()
        {
            //todo: this may break things like Accordians
            var count = _children.Count;
            if (count == 0)
                return;

            for (int i = 0; i < count; ++i)
                InternalSwapChild(null, i, false);
            _children.Clear();
            OnChildRemeasure(this);
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
            InvalidateMeasure();
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
        /// <returns>An enumerator to all elements</returns>
        public IEnumerable<Static> EnumerateRecursive()
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
            foreach (var elem in root.EnumerateRecursive())
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

        private void BreakOnThis()
        {
            System.Diagnostics.Debugger.Break();
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
        public Static FindChildByName(string name, bool caseSensitive = false, Type elementType = null)
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
                    elem.Name.Equals(name, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
                    return elem;

                foreach (var child in elem.Children)
                    next.Push(child);
            }

            return null;
        }

        public Static FindChildAtPoint(Point point)
        {
            var process = new Stack<Static>(Children);
            var result = VisibleBounds.Contains(point) ? this : null; ;
            while (process.Count > 0)
            {
                var top = process.Pop();
                if (top.VisibleBounds.Contains(point))
                {
                    process.Clear();
                    result = top;
                    foreach (var child in top.Children)
                        process.Push(child);
                }
            }
            return result;
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
                throw new ArgumentException("bindingSource and bindingTarget cannot both be null");

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
            {
                element.lastMeasureContainerBounds = Rectangle.Empty;
                element.lastMeasureAvailableSize = new Vector2(InfiniteSize);
                element.InvalidateMeasure();
            }
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

            //Diagnostics.Debug.WriteLine($"Measuring ID:{DebugId} (available size:{availableSize})");

            var size = Size;
            bool isWidthAutoSize = float.IsNaN(size.X);
            bool isHeightAutoSize = float.IsNaN(size.Y);
            bool isHStretch = HorizontalAlignment == Alignment.Stretch;
            bool isVStretch = VerticalAlignment == Alignment.Stretch;

            if (availableSize.X < InfiniteSize)
            {
                availableSize.X -= Padding.X * 2;
                if (isWidthAutoSize && isHStretch)
                    availableSize.X -= Position.X;
            }
            else if (!float.IsNaN(size.X))
                availableSize.X = size.X;

            if (availableSize.Y < InfiniteSize)
            {
                availableSize.Y -= Padding.Y * 2;
                if (isHeightAutoSize && isVStretch)
                    availableSize.Y -= Position.Y;
            }
            else if (!float.IsNaN(size.Y))
                availableSize.Y = size.Y;

            var measuredSize = MeasureOverride(availableSize);
            if (isWidthAutoSize || isHeightAutoSize)
            {
                if (float.IsInfinity(measuredSize.X) || float.IsNaN(measuredSize.X)
                 || float.IsInfinity(measuredSize.Y) || float.IsNaN(measuredSize.Y))
                    throw new /*NotFiniteNumberException*/InvalidOperationException("Measured size cannot be NaN or infinity");

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
                textSize = Vector2.Ceiling(Font.MeasureString(Text, TextStyle)).ToPoint();

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
            for (int i = startIndex; i < Math.Min(Children.Count, startIndex + count); ++i)
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
        private static TimeSpan lastUpdateDuration;
        private static TimeSpan lastDrawDuration;
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

            //Diagnostics.Debug.WriteLine($"Arranging ID:{DebugId} ({this}) [container:{container}]");
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
                    // child.Arrange(new Rectangle(0, 0, (int)availableSize.X, (int)availableSize.Y));
                    child.Arrange(new Rectangle(child.lastMeasureContainerBounds.Location, availableSize.ToPoint())); //?
            }
        }

        /// <summary>
        /// Calculate all of the bounds to this element in relation to a container.
        /// </summary>
        /// <param name="container">The container to fit this to, in relative coordinates</param>
        private void AdjustToContainer(Rectangle container)
        {
            lastMeasureContainerBounds = container;

            Rectangle parentContentArea;
            var offsetParent = Point.Zero;
            if (Parent == null)
            {
                var viewport = Runtime.GraphicsDevice.Viewport.Bounds;
                viewport.X = 0;
                viewport.Y = 0;
                container = parentContentArea = Rectangle.Intersect(container, viewport);
            }
            else
            {
                offsetParent = Parent.OffsetContentArea.Location;
                parentContentArea = Parent.VisibleContentArea;
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
            container.Offset(offsetParent);
            container = Rectangle.Intersect(container, parentContentArea);
            VisibleContentArea = Rectangle.Intersect(tmp, container);

            tmp.Inflate(Padding.X, Padding.Y);
            VisibleBounds = Rectangle.Intersect(tmp, container);
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

        static readonly List<Static> measureQueue = new List<Static>();
        static readonly List<Static> arrangeQueue = new List<Static>();

        /// <summary>
        /// Complete any pending reflows/arranges
        /// </summary>
        public static void PerformReflows()
        {
            for (int i = 0; i < measureQueue.Count; ++i)
            {
                measureQueue[i].Measure(measureQueue[i].lastMeasureAvailableSize);
                if (!measureQueue[i].isMeasureValid)
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
#if DEBUG
            boop.Restart();
#endif
            PerformReflows();

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
#if DEBUG
            lastUpdateDuration = boop.Elapsed;
#endif
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

            var mouse = InputState.MousePoint;
            return HandleMouseInput(mouse, MouseButtons.Left) &&
                HandleMouseInput(mouse, MouseButtons.Right) &&
                HandleMouseInput(mouse, MouseButtons.Middle);
        }

        bool HandleTouchInput()
        {
            /* gestures don't work very conveniently 
            if (InputState.Gestures.TryGetValue(GestureType.Tap, out var gesture) &&
                VisibleBounds.Contains(gesture.Position))
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
            else if (InputState.Gestures.TryGetValue(GestureType.FreeDrag, out gesture) && 
                VisibleBounds.Contains(gesture.Position))
            {
                //todo: this needs to support didPress style of moving finger outside of control

                var pea = new DragEventArgs(this)
                {
                    position = (gesture.Position - OffsetContentArea.Location.ToVector2()) + Padding,
                    button = 0,
                    device = DeviceType.Touch,
                    delta = gesture.Delta
                };
                BubbleEvent(DragEvent, pea);

                if (CanFocus)
                {
                    HasFocus = true;
                    return false;
                }
            }*/

            bool touched = false;
            for (int touchIndex = InputState.touches.Count - 1; touchIndex >= 0; --touchIndex)
            {
                var touch = InputState.touches[touchIndex];

                if (InputState.IsPress(touchIndex) && VisibleBounds.Contains(touch.Position))
                {
                    var pea = new PointerEventArgs(this)
                    {
                        position = touch.Position - OffsetContentArea.Location.ToVector2() + Padding,
                        button = touchIndex,
                        device = DeviceType.Touch
                    };
                    BubbleEvent(PressEvent, pea);

                    didPress[1 << (touchIndex + (int)MouseButtons._TouchIndex)] = true;
                    if (CanFocus)
                    {
                        HasFocus = true;
                        return false;
                    }
                }

                //input capture
                //todo: maybe add capture setting
                else if (InputState.lastTouches.Count > touchIndex && DidPressInside(touchIndex))
                {
                    // first if clause ^ shouldnt be necessary

                    var lastTouch = InputState.lastTouches[touchIndex];
                    if (lastTouch.Position != touch.Position)
                    {
                        var dea = new DragEventArgs(this)
                        {
                            delta = touch.Position - lastTouch.Position,
                            position = touch.Position - OffsetContentArea.Location.ToVector2() + Padding,
                            button = touchIndex,
                            device = DeviceType.Touch
                        };
                        BubbleEvent(DragEvent, dea);
                    }
                    return false;
                }
            }

            for (int touchIndex = 0; touchIndex < InputState.lastTouches.Count; ++touchIndex)
            {
                var touch = InputState.lastTouches[touchIndex];
             
                if (InputState.IsButtonUp(touchIndex)) //may need to be alted for touch
                {
                    if (didPress[1 << (touchIndex + (int)MouseButtons._TouchIndex)])
                    {
                        didPress[1 << (touchIndex + (int)MouseButtons._TouchIndex)] = false;
                        if (VisibleBounds.Contains(touch.Position)) //gesture pos
                        {
                            //todo: only trigger click if did not drag (?) (only if drag event)

                            TriggerClick(
                                touch.Position - OffsetContentArea.Location.ToVector2() + Padding,
                                touchIndex,
                                DeviceType.Touch
                            );
                            return false;
                        }
                    }
                }
            }

            return !touched;
        }

        bool HandleMouseInput(Point mousePosition, MouseButtons button)
        {
            bool isHovering = VisibleBounds.Contains(mousePosition);
            //todo: these should be handled at HandleInput globally
            if (isHovering || didPress[1 << (int)button])
                ApplyStyles(GetStyles(Style, "Hover"));
            else
                ApplyStyles(GetStyles(Style));

            if (InputState.IsPress(button) && isHovering)
            {
                var pea = new PointerEventArgs(this)
                {
                    position = (mousePosition - OffsetContentArea.Location).ToVector2() + Padding,
                    button = (int)button,
                    device = DeviceType.Mouse
                };
                BubbleEvent(PressEvent, pea);

                ApplyStyles(GetStyles(Style, "Press"));
                didPress[1 << (int)button] = true;
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
                if (isHovering)
                    ApplyStyles(GetStyles(Style, "Press"));
                var lastMousePosition = InputState.LastMousePoint;
                if (lastMousePosition != mousePosition)
                {
                    //wrap mouse in window?
                    var dea = new DragEventArgs(this)
                    {
                        delta = (mousePosition - lastMousePosition).ToVector2(),
                        position = (mousePosition - OffsetContentArea.Location).ToVector2() + Padding,
                        button = (int)button,
                        device = DeviceType.Mouse
                    };

                    //not perfect
                    didDrag |= (BubbleEvent(DragEvent, dea) != null);
                }
                return false;
            }

            else if (InputState.IsButtonUp(button))
            {
                if (didPress[1 << (int)button])
                {
                    didPress[1 << (int)button] = false;
                    if (!didDrag && VisibleBounds.Contains(mousePosition)) //gesture pos
                    {
                        TriggerClick(
                            (mousePosition - OffsetContentArea.Location).ToVector2() + Padding,
                            (int)button,
                            DeviceType.Mouse
                        );

                        return false;
                    }

                    didDrag = false;
                }
            }

            return true;
        }

        /// <summary>
        /// Draw this element, its decorators, and any children
        ///
        /// Draws depth-first, parent-most first
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to use</param>
        public virtual void Draw(DrawContext context)
        {
#if DEBUG
            boop.Restart();
#endif
            if (!IsEnabled)
                return;

            var draws = new Stack<Static>(Children.Count + 1);
            draws.Push(this);

            Static debugDraw = null;
            while (draws.Count > 0)
            {
                var toDraw = draws.Pop();

                if (toDraw.BackgroundColor.A > 0)
                    Primitives2D.DrawFill(context.spriteBatch, toDraw.BackgroundColor, toDraw.VisibleBounds);
                toDraw.BackgroundSprite.Draw(context.spriteBatch, toDraw.VisibleBounds);

                toDraw.DrawSelf(context);

                var borderColor = /*(toDraw.HasFocus && toDraw.CanFocus) ? FocusedBorderColor : */toDraw.BorderColor;
                if (DisplayDebugInfo && borderColor == Color.Transparent)
                    borderColor = isMeasureValid && isArrangeValid ? Color.SteelBlue : Color.Tomato;

                if (DisplayDebugInfo && toDraw.VisibleBounds.Contains(InputState.MousePoint))
                    debugDraw = toDraw;

                if (borderColor.A > 0)
                {
                    var offsetRect = toDraw.OffsetContentArea;
                    offsetRect.Inflate(toDraw.Padding.X, toDraw.Padding.Y);
                    var offset = offsetRect.Location.ToVector2();
                    DrawHLine(context.spriteBatch, borderColor, 0, 0, offsetRect.Width, offset, toDraw.VisibleBounds);
                    DrawVLine(context.spriteBatch, borderColor, offsetRect.Width, 0, offsetRect.Height, offset, toDraw.VisibleBounds);
                    DrawHLine(context.spriteBatch, borderColor, offsetRect.Height, 0, offsetRect.Width, offset, toDraw.VisibleBounds);
                    DrawVLine(context.spriteBatch, borderColor, 0, 0, offsetRect.Height, offset, toDraw.VisibleBounds);
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
                DrawTextOptions drawText = new DrawTextOptions(
                    $"Measure Count: {totalMeasureCount}\n" +
                    $"Arrange Count: {totalArrangeCount}\n" +
                    $"Total Elements Created: {idCounter}\n" +
                    $"Total binding Updates: {Data.Binding.TotalUpdateCount}",
                    DebugFont, DebugTextStyle, Color.CornflowerBlue, new Vector2(10)
                );
                context.textRenderer.Draw(drawText);

                debugDraw.DrawDebugInfo(context);
                if (InputState.IsPress(Keys.Pause))
                    debugDraw.BreakOnThis();
            }
            lastDrawDuration = boop.Elapsed;
#endif
        }

        public void DrawDebugInfo(DrawContext context)
        {
            Primitives2D.DrawRect(context.spriteBatch, Color.Cyan, VisibleBounds);

            var rect = OffsetContentArea;
            Primitives2D.DrawRect(context.spriteBatch, new Color(Color.Orange, 0.5f), rect);

            rect.Inflate(Padding.X, Padding.Y);
            Primitives2D.DrawRect(context.spriteBatch, Color.OrangeRed, rect);

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
                        + $"Style: {Style}\n"
                        + $"Bindings: {(Bindings == null ? "(None)" : string.Join(",", Bindings))}\n"
                        + $"Events: {events?.Count}, Commands: {CommandActions?.Count}\n";

            var drawPos = rect.Location + new Point(rect.Width + 10, rect.Height + 10);
            var size = DebugFont.MeasureString(info, DebugTextStyle);
            drawPos = Util.Clamp(new Rectangle(drawPos.X, drawPos.Y, (int)size.X, (int)size.Y), Runtime.GraphicsDevice.Viewport.Bounds);
            drawPos -= new Point(10);

            var drawText = new DrawTextOptions(info, DebugFont, DebugTextStyle, Color.Gold, drawPos.ToVector2());
            context.textRenderer.Draw(drawText);
        }

        /// <summary>
        /// Draw only this item (no children)
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to use</param>
        protected virtual void DrawSelf(DrawContext context)
        {
            DrawText(context.textRenderer, Text, Vector2.Zero);
        }

        //todo: pass in context to methods below

        /// <summary>
        /// Draw text clipped to the visible region of this element
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to use</param>
        /// <param name="position">The relative position (to the element) to draw this text</param>
        protected void DrawText(TextRenderer textRenderer, string text, Vector2 position)
        {
            if (Font == null || text == null)
                return;

            position += (OffsetContentArea.Location - VisibleContentArea.Location).ToVector2();

            var drawText = new DrawTextOptions(
                text,
                Font,
                TextStyle,
                Color,
                VisibleContentArea.Location.ToVector2()
            )
            {
                clipSize = VisibleContentArea.Size.ToVector2(),
                relativeOffset = position
            };
            textRenderer.Draw(drawText);
            //Font.Draw(spriteBatch, Text, 0, Text.Length, VisibleContentArea, position, Color);
        }

        /// <summary>
        /// Draw an arbitrary line, clipped to the visible content area.
        /// Note: more computationally expensive than DrawVLine or DrawHLine
        /// </summary>
        /// <param name="spriteBatch">spriteBatch to use</param>
        /// <param name="color">Color to draw the line</param>
        /// <param name="a">The start of the line</param>
        /// <param name="b">The end of the line</param>
        protected void DrawLine(SpriteBatch spriteBatch, Color color, Vector2 a, Vector2 b)
        {
            var offset = OffsetContentArea.Location.ToVector2();
            a += offset;
            b += offset;

            if (!Util.ClipLine(ref a, ref b, VisibleContentArea))
                return;

            Primitives2D.DrawLine(spriteBatch, color, a, b);
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

            Primitives2D.DrawLine(spriteBatch, color, new Vector2(x, y1), new Vector2(x, y2));
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

            Primitives2D.DrawLine(spriteBatch, color, new Vector2(x1, y), new Vector2(x2, y));
        }

        protected void DrawRect(SpriteBatch spriteBatch, Color color, Rectangle localRect)
        {
            var offset = OffsetContentArea.Location.ToVector2();
            DrawHLine(spriteBatch, color, localRect.Top, localRect.Left, localRect.Right, offset, VisibleContentArea);
            DrawVLine(spriteBatch, color, localRect.Right, localRect.Top, localRect.Bottom, offset, VisibleContentArea);
            DrawHLine(spriteBatch, color, localRect.Bottom, localRect.Left, localRect.Right, offset, VisibleContentArea);
            DrawVLine(spriteBatch, color, localRect.Left, localRect.Top, localRect.Bottom, offset, VisibleContentArea);
        }

        protected void DrawFill(SpriteBatch spriteBatch, Color color, Rectangle localRect)
        {
            localRect.Offset(OffsetContentArea.Location);
            Primitives2D.DrawFill(spriteBatch, color, Rectangle.Intersect(VisibleContentArea, localRect));
        }

        protected void DrawSprite(SpriteBatch spriteBatch, Sprite sprite, Rectangle localRect)
        {

            if (sprite == null)
                return;

            DrawSpriteCustomRegion(spriteBatch, sprite, localRect, VisibleContentArea, sprite.ElapsedTime);
        }

        protected void DrawSprite(SpriteBatch spriteBatch, Sprite sprite, Rectangle localRect, TimeSpan elapsedTime)
        {
            DrawSpriteCustomRegion(spriteBatch, sprite, localRect, VisibleContentArea, elapsedTime);
        }

        void DrawSpriteCustomRegion(SpriteBatch spriteBatch, Sprite sprite, Rectangle localRect, Rectangle clipRegion, TimeSpan elapsedTime)
        {
            if (sprite?.Texture == null || localRect.Width == 0 || localRect.Height == 0)
                return;

            //adjust sprite based on offset of container and clip to clipRegion
            localRect.Offset(clipRegion.Location - VisibleOffset);
            var finalRect = Rectangle.Intersect(localRect, clipRegion);

            //scale the clip region by the size of the destRect
            //to get a relative clip size
            var sx = sprite.Width / (float)localRect.Width;
            var sy = sprite.Height / (float)localRect.Height;

            var vx = (int)((localRect.Width - finalRect.Width) * sx);
            var vy = (int)((localRect.Height - finalRect.Height) * sy);
            var clip = new Rectangle(
                vx,
                vy,
                sprite.Width - vx,
                sprite.Height - vy
            );

            //todo: do this without conditionals
            if (finalRect.Right == clipRegion.Right)
                clip.X = 0;
            if (finalRect.Bottom == clipRegion.Bottom)
                clip.Y = 0;

            sprite.Draw(spriteBatch, finalRect, clip, 0, Color.White, elapsedTime);
            //Primitives2D.DrawRect(spriteBatch, Color.LightSteelBlue, finalRect);
        }

        #endregion

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

        protected T GetStyleRule<T>(Stylesheet styleRules, string propName, T fallback)
        {
            //Cast?
            if (styleRules == null ||
                !styleRules.TryGetValue(propName, out var sProp))
                return fallback;

            if (sProp is T prop)
                return prop;

            if (sProp == null)
                return default;

            if (Data.Serializer.TryNumericCast(sProp, out var destNumber, sProp.GetType().GetTypeInfo(), typeof(T).GetTypeInfo()))
                return (T)destNumber;

            return fallback;
        }

        public static Stylesheet GetStyles(string styleName, string styleState = null)
        {
            Stylesheet styles = null;
            if (styleName != null)
            {
                styleName += (styleState != null ? ("+" + styleState) : null);
                Styles.TryGetValue(styleName, out styles); //todo: revamp
            }
            return styles;
        }

        public virtual void ApplyStyles(Stylesheet styleRules)
        {
            //todo: switch to enumeration based method? (enumerate dictionary vs try and fetch)

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
            TextStyle = GetStyleRule(styleRules, "TextStyle", TextStyle);

            BorderColor = GetStyleRule(styleRules, "BorderColor", BorderColor);
            BackgroundColor = GetStyleRule(styleRules, "BackgroundColor", BackgroundColor);
            BackgroundSprite = GetStyleRule(styleRules, "BackgroundSprite", BackgroundSprite);
            if (BackgroundSprite.Sprite != null)
                BackgroundSprite.Sprite.ElapsedTime = TimeSpan.Zero;

            Padding = GetStyleRule(styleRules, "Padding", Padding);
            HorizontalAlignment = GetStyleRule(styleRules, "HorizontalAlignment", HorizontalAlignment);
            VerticalAlignment = GetStyleRule(styleRules, "VerticalAlignment", VerticalAlignment);
            Position = GetStyleRule(styleRules, "Position", Position);
            Size = GetStyleRule(styleRules, "Size", Size);

            //base on rules
        }

        public static void MergeStyleRules(Dictionary<string, Stylesheet> stylesheets)
        {
            if (Styles == null)
            {
                Styles = stylesheets;
                return;
            }

            foreach (var rules in stylesheets)
            {
                if (Styles.TryGetValue(rules.Key, out var sheet))
                {
                    foreach (var rule in rules.Value)
                        sheet[rule.Key] = rule.Value;
                }
                else
                    Styles.Add(rules.Key, rules.Value);
            }
        }

#region Helpers

        //todo: better name

        public void TriggerClick(Vector2 relativePosition, int button = 0, DeviceType device = DeviceType.Mouse, int deviceIndex = 0)
        {
            var ce = new PointerEventArgs(this)
            {
                position = relativePosition,
                button = button,
                device = device,
                deviceIndex = deviceIndex
            };
            BubbleEvent(ClickEvent, ce);
        }
        
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
