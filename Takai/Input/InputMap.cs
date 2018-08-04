using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Takai.Input
{
    public enum InputDeviceType
    {
        Unknown,
        Key,
        MouseButton,
        MousePosition,
        GamepadButton,
        GamepadThumbstick,
        GamepadTrigger,
        TouchPosition,
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

    /// <summary>
    /// An input map that maps device inputs to user specified actions
    /// </summary>
    /// <typeparam name="TAction">The action type to bind to</typeparam>
    public class InputMap<TAction>
    {
        public Dictionary<Keys, InputBinding<TAction>> Keys { get; set; }
        public Dictionary<Buttons, InputBinding<TAction>> GamepadButtons { get; set; }
        public Dictionary<MouseButtons, InputBinding<TAction>> MouseButtons { get; set; }

        //public InputBinding2D MousePosition { get; set; }
        public PolarInputBinding<TAction> Mouse { get; set; }
        public InputBinding<TAction> MouseWheel { get; set; }

        public InputBinding2D<TAction> GamepadLeftThumbstick { get; set; }
        public InputBinding2D<TAction> GamepadRightThumbstick { get; set; }

        public InputBinding<TAction> GamepadLeftTrigger { get; set; }
        public InputBinding<TAction> GamepadRightTrigger { get; set; }

        public PolarInputBinding<TAction>[] Touches { get; set; }

        //todo: touch

        /// <summary>
        /// The current inputs supplied. They are cleared after being read
        /// value is magnitude scaled by analog input
        /// </summary>
        [Data.Serializer.Ignored]
        public Dictionary<TAction, float> CurrentInputs { get; set; } = new Dictionary<TAction, float>();

        //todo: serialization

        protected void SetInput(InputBinding<TAction> binding, float magnitude = 1)
        {
            magnitude *= binding.magnitude;
            if (CurrentInputs.TryGetValue(binding.action, out var input) && input != 0)
                magnitude = (input + magnitude) / 2;
            CurrentInputs[binding.action] = magnitude;
        }

        public void Update(PlayerIndex player, Rectangle viewport)
        {
            if (Keys != null)
            {
                foreach (var key in InputState.GetPressedKeys())
                {
                    if (Keys.TryGetValue(key, out var binding))
                        SetInput(binding);
                }
            }
            if (GamepadButtons != null)
            {
                foreach (var binding in GamepadButtons)
                {
                    if (InputState.IsButtonDown(binding.Key, player))
                        SetInput(binding.Value);
                }
            }
            if (MouseButtons != null)
            {
                foreach (var binding in MouseButtons)
                {
                    if (InputState.IsButtonDown(binding.Key))
                        SetInput(binding.Value);
                }
            }

            var mouse = InputState.MouseVector;
            if (InputState.MouseDelta() != Vector2.Zero && viewport.Contains(mouse))
            {
                var relative = viewport.Relative(mouse) / new Vector2(viewport.Width, viewport.Height);
                relative -= (Mouse.boundsPercent.min + Mouse.boundsPercent.max) / 2;
                SetInput(Mouse.horizontal, relative.X * 2);
                SetInput(Mouse.vertical, relative.Y * 2);
            }
            if (InputState.HasScrolled())
                SetInput(MouseWheel, System.Math.Sign(InputState.ScrollDelta()));

            var thumbsticks = InputState.Thumbsticks(player);
            SetInput(GamepadLeftThumbstick.horizontal, thumbsticks.Left.X);
            SetInput(GamepadLeftThumbstick.vertical, thumbsticks.Left.Y);
            SetInput(GamepadRightThumbstick.horizontal, thumbsticks.Right.X);
            SetInput(GamepadRightThumbstick.vertical, thumbsticks.Right.Y);

            var triggers = InputState.Triggers(player);
            SetInput(GamepadLeftTrigger, triggers.Left);
            SetInput(GamepadRightTrigger, triggers.Right);

            if (Touches != null)
            {
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
}
