using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai.Input;

namespace Takai.UI
{
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

    public partial class Static
    {
        //some standard/common events
        public const string PressEvent = "Press";
        public const string ClickEvent = "Click";
        public const string DragEvent = "Drag";
        public const string DropEvent = "Drop";
        public const string TextChangedEvent = "TextChanged";
        public const string ValueChangedEvent = "ValueChanged";
        public const string SelectionChangedEvent = "SelectionChanged";

        //some common style states
        public const string HoverState = "Hover";
        public const string ActiveState = "Active";
        public const string FocusState = "Focus";

        public static readonly HashSet<string> InputEvents = new HashSet<string> { PressEvent, ClickEvent, DragEvent, DropEvent };

        /// <summary>
        /// Global commands that are invoked if routed commands arent triggered
        /// </summary>
        public static Dictionary<string, Action<Static, object>> GlobalCommands
        {
            get => (_globalCommands ?? (_globalCommands = new Dictionary<string, Action<Static, object>>
                (StringComparer.OrdinalIgnoreCase)));
        }
        private static Dictionary<string, Action<Static, object>> _globalCommands;

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
    }
}
