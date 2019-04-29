using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Takai.Input
{
    public enum DeviceType
    {
        Uknown,
        Mouse,
        Touch,
        Keyboard,
        Gamepad,
        Accelerometer
    }

    public struct InputBinding<TAction>
    {
        /// <summary>
        /// The action to take when this input is triggered
        /// </summary>
        public TAction action;
        /// <summary>
        /// The direction and scale of the input
        /// </summary>
        public float magnitude;

        public InputBinding(TAction action, float magnitude = 1)
        {
            this.action = action;
            this.magnitude = magnitude;
        }
    }

    /// <summary>
    /// A two dimensional input binding (
    /// </summary>
    /// <typeparam name="TAction"></typeparam>
    public struct InputBinding2D<TAction>
    {
        public InputBinding<TAction> horizontal;
        public InputBinding<TAction> vertical;

        public InputBinding2D(InputBinding<TAction> horizontal, InputBinding<TAction> vertical)
        {
            this.horizontal = horizontal;
            this.vertical = vertical;
        }
    }

    public struct PolarInputBinding<TAction>
    {
        public InputBinding<TAction> horizontal;
        public InputBinding<TAction> vertical;
        public Extent boundsPercent;

        //required touch?

        public PolarInputBinding(InputBinding<TAction> horizontal, InputBinding<TAction> vertical, Extent boundsPercent)
        {
            this.horizontal = horizontal;
            this.vertical = vertical;
            this.boundsPercent = boundsPercent;
        }

        public Extent GetBounds(Rectangle viewport)
        {
            var sz = new Vector2(viewport.Width, viewport.Height);
            return new Extent(
                sz * boundsPercent.min,
                sz * boundsPercent.max
            );
        }
    }

    public struct InputButtonPair<TAction>
    {
        public object input; //key, mousebuttons, buttons
        public InputBinding<TAction> action;

        public InputButtonPair(object input, InputBinding<TAction> action)
        {
            this.input = input;
            this.action = action;
        }
    }

    /// <summary>
    /// An input map that maps device inputs to user specified actions
    /// </summary>
    /// <typeparam name="TAction">The action type to bind to</typeparam>
    //[Data.CustomSerialize("CustomSerialize"),
    // Data.CustomDeserialize(typeof(InputMap<>), "CustomDeserialize")]
    public class InputMap<TAction>
    {
        [Data.Serializer.Ignored]
        public Dictionary<Keys, InputBinding<TAction>> Keys { get; set; } = new Dictionary<Keys, InputBinding<TAction>>();
        [Data.Serializer.Ignored]
        public Dictionary<Buttons, InputBinding<TAction>> GamepadButtons { get; set; } = new Dictionary<Buttons, InputBinding<TAction>>();
        [Data.Serializer.Ignored]
        public Dictionary<MouseButtons, InputBinding<TAction>> MouseButtons { get; set; } = new Dictionary<MouseButtons, InputBinding<TAction>>();

        //public InputBinding2D MousePosition { get; set; }
        public PolarInputBinding<TAction> Mouse { get; set; }
        public InputBinding<TAction> MouseWheel { get; set; }

        public InputBinding2D<TAction> GamepadLeftThumbstick { get; set; }
        public InputBinding2D<TAction> GamepadRightThumbstick { get; set; }

        public InputBinding<TAction> GamepadLeftTrigger { get; set; }
        public InputBinding<TAction> GamepadRightTrigger { get; set; }

        public PolarInputBinding<TAction>[] Touches { get; set; } = new PolarInputBinding<TAction>[0];

        /// <summary>
        /// All of the button inputs (for serialization)
        /// </summary>
        public IEnumerable<InputButtonPair<TAction>> Buttons
        {
            get
            {
                foreach (var key in Keys)
                    yield return new InputButtonPair<TAction>(key.Key, key.Value);
                foreach (var gamepads in GamepadButtons)
                    yield return new InputButtonPair<TAction>(gamepads.Key, gamepads.Value);
                foreach (var mice in MouseButtons)
                    yield return new InputButtonPair<TAction>(mice.Key, mice.Value);
            }
            set
            {
                foreach (var input in value)
                {
                    switch (input.input)
                    {
                        case Keys key:
                            Keys.Add(key, input.action);
                            break;
                        case Buttons gamepad:
                            GamepadButtons.Add(gamepad, input.action);
                            break;
                        case MouseButtons mouse:
                            MouseButtons.Add(mouse, input.action);
                            break;
                    }
                }
            }
        }

        //todo: touch presses, mouse presses?

        /// <summary>
        /// The current inputs supplied. They are cleared after being read
        /// value is magnitude scaled by analog input
        /// </summary>
        [Data.Serializer.Ignored]
        public Dictionary<TAction, float> CurrentInputs { get; set; } = new Dictionary<TAction, float>();

        /// <summary>
        /// Set the state of one input
        /// </summary>
        /// <param name="binding">The input binding</param>
        /// <param name="magnitude">the magnitude multiplier (e.g. analog inputs)</param>
        protected void SetInput(InputBinding<TAction> binding, float magnitude = 1)
        {
            magnitude *= binding.magnitude;
            if (CurrentInputs.TryGetValue(binding.action, out var input) && input != 0)
                magnitude = (input + magnitude) / 2;
            CurrentInputs[binding.action] = magnitude;
        }

        public void Update(PlayerIndex player, Rectangle viewport)
        {
            foreach (var key in InputState.GetPressedKeys())
            {
                if (Keys.TryGetValue(key, out var binding))
                    SetInput(binding);
            }
            foreach (var binding in GamepadButtons)
            {
                if (InputState.IsButtonDown(binding.Key, player))
                    SetInput(binding.Value);
            }

            if (viewport.Contains(InputState.MousePoint))
            {
                foreach (var binding in MouseButtons)
                {
                    if (InputState.IsButtonDown(binding.Key))
                        SetInput(binding.Value);
                }

                var mouse = InputState.MouseVector;
                if (InputState.MouseDelta() != Vector2.Zero)
                {
                    var v = new Vector2(viewport.Width, viewport.Height);
                    var absMin = (Mouse.boundsPercent.min * v).ToPoint();
                    var absMax = (Mouse.boundsPercent.max * v).ToPoint();
                    var rv = absMax - absMin;
                    if (new Rectangle(absMin.X, absMin.Y, rv.X, rv.Y).Contains(mouse))
                    {
                        var polar = Vector2.Normalize(mouse - (absMin + absMax).ToVector2() / 2);
                        SetInput(Mouse.horizontal, polar.X);
                        SetInput(Mouse.vertical, polar.Y);
                    }
                }
                if (InputState.HasScrolled())
                    SetInput(MouseWheel, System.Math.Sign(InputState.ScrollDelta()));
            }

            var thumbsticks = InputState.Thumbsticks(player);
            SetInput(GamepadLeftThumbstick.horizontal, thumbsticks.Left.X);
            SetInput(GamepadLeftThumbstick.vertical, thumbsticks.Left.Y);
            SetInput(GamepadRightThumbstick.horizontal, thumbsticks.Right.X);
            SetInput(GamepadRightThumbstick.vertical, thumbsticks.Right.Y);

            var triggers = InputState.Triggers(player);
            SetInput(GamepadLeftTrigger, triggers.Left);
            SetInput(GamepadRightTrigger, triggers.Right);

            //todo: convert to circle (allow for magnitude)
            foreach (var touch in InputState.touches)
            {
                foreach (var touchAction in Touches)
                {
                    var v = new Vector2(viewport.Width, viewport.Height);
                    var absMin = (touchAction.boundsPercent.min * v).ToPoint();
                    var absMax = (touchAction.boundsPercent.max * v).ToPoint();
                    var rv = absMax - absMin;
                    if (!new Rectangle(absMin.X, absMin.Y, rv.X, rv.Y).Contains(touch.Position))
                        continue;

                    var polar = (touch.Position - ((absMin + absMax).ToVector2() / 2)) / rv.ToVector2();
                    polar.Normalize();
                    SetInput(touchAction.horizontal, polar.X);
                    SetInput(touchAction.vertical, polar.Y);
                }
            }
        }
    }
}
