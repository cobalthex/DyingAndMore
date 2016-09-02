using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Takai.Input
{
    public enum MouseButtons
    {
        Left,
        Middle,
        Right,
        X4,
        X5
    }

    //todo: convert mouse points to use touch

    //todo: touchdownPoint and press/release time (maybe)

    public static class InputState
    {
        private static MouseState mouseState, lastMouseState;
        private static KeyboardState keyState, lastKeyState;

        /// <summary>
        /// Get a vector2 of the current mouse position
        /// </summary>
        public static Vector2 MouseVector { get { return mouseState.Position.ToVector2(); } }
        /// <summary>
        /// Get a point of the current mouse position
        /// </summary>
        public static Point MousePoint { get { return mouseState.Position; } }

        /// <summary>
        /// Get a vector2 of the last mouse position
        /// </summary>
        public static Vector2 LastMouseVector { get { return lastMouseState.Position.ToVector2(); } }
        /// <summary>
        /// Get a point of the last mouse position
        /// </summary>
        public static Point LastMousePoint { get { return lastMouseState.Position; } }

        public static void Update()
        {
            lastMouseState = mouseState;
            mouseState = Mouse.GetState();

            lastKeyState = keyState;
            keyState = Keyboard.GetState();
        }

        /// <summary>
        /// Get the amount the mouse has scrolled since the last update
        /// </summary>
        /// <returns></returns>
        public static int ScrollDelta()
        {
            return mouseState.ScrollWheelValue - lastMouseState.ScrollWheelValue;
        }

        /// <summary>
        /// Has the mouse wheel been scrolled
        /// </summary>
        /// <returns>True if the mouse wheel has been scrolled in either direction</returns>
        public static bool HasScrolled()
        {
            return mouseState.ScrollWheelValue != lastMouseState.ScrollWheelValue;
        }

        /// <summary>
        /// Get the change in position of the cursor's (mouse or touch) position
        /// </summary>
        /// <returns>The change in position</returns>
        public static Vector2 MouseDelta()
        {
            return (lastMouseState.Position - mouseState.Position).ToVector2();
        }
        
        /// <summary>
        /// Has the mouse been dragged since last frame
        /// </summary>
        /// <param name="Epsilon">The minimum length required to be considered dragging</param>
        /// <returns>True if the mouse is held down and is being moved</returns>
        public static bool HasMouseDragged(MouseButtons Button, int Epsilon = 10)
        {
            return (IsButtonHeld(Button) && MouseDelta().LengthSquared() > (Epsilon * Epsilon));
        }

        static ButtonState GetButtonState(MouseButtons Button, ref MouseState State)
        {
            switch (Button)
            {
                case MouseButtons.Left:
                    return State.LeftButton;

                case MouseButtons.Middle:
                    return State.MiddleButton;

                case MouseButtons.Right:
                    return State.RightButton;

                case MouseButtons.X4:
                    return State.XButton1;

                case MouseButtons.X5:
                    return State.XButton2;

                default:
                    return ButtonState.Released;
            }
        }

        /// <summary>
        /// Is a mouse button currently pressed?
        /// </summary>
        /// <param name="Button">The button to check</param>
        /// <returns>True if the button is pressed</returns>
        public static bool IsButtonDown(MouseButtons Button)
        {
            switch (Button)
            {
                case MouseButtons.Left:
                    return (mouseState.LeftButton == ButtonState.Pressed);

                case MouseButtons.Middle:
                    return (mouseState.MiddleButton == ButtonState.Pressed);

                case MouseButtons.Right:
                    return (mouseState.RightButton == ButtonState.Pressed);

                case MouseButtons.X4:
                    return (mouseState.XButton1 == ButtonState.Pressed);

                case MouseButtons.X5:
                    return (mouseState.XButton2 == ButtonState.Pressed);

                default:
                    return false;
            }
        }
        /// <summary>
        /// Is a mouse button currently released?
        /// </summary>
        /// <param name="Button">The button to check</param>
        /// <returns>True if the button is released</returns>
        public static bool IsButtonUp(MouseButtons Button)
        {
            return !IsButtonDown(Button);
        }
        /// <summary>
        /// Is the mouse button currently held down (current and last state)?
        /// </summary>
        /// <param name="Button">The mouse button to check</param>
        /// <returns>True if the mouse button is held down</returns>
        public static bool IsButtonHeld(MouseButtons Button)
        {
            return (GetButtonState(Button, ref mouseState) == ButtonState.Pressed &&
                    GetButtonState(Button, ref lastMouseState) == ButtonState.Pressed);
        }

        /// <summary>
        /// Is a key currently pressed?
        /// </summary>
        /// <param name="Key">The key to check</param>
        /// <returns>True if the key is pressed</returns>
        public static bool IsButtonDown(Keys Key)
        {
            return keyState.IsKeyDown(Key);
        }
        /// <summary>
        /// Is a key currently released?
        /// </summary>
        /// <param name="Key">The key to check</param>
        /// <returns>True if the key is released</returns>
        public static bool IsButtonUp(Keys Key)
        {
            return keyState.IsKeyUp(Key);
        }
        /// <summary>
        /// Is a key currently held down?
        /// </summary>
        /// <param name="Key">The key to check</param>
        /// <returns>True if the key is held down</returns>
        public static bool IsButtonHeld(Keys Key)
        {
            return (keyState.IsKeyDown(Key) && lastKeyState.IsKeyDown(Key));
        }

        /// <summary>
        /// Was a mouse button just pressed?
        /// </summary>
        /// <param name="Button">The mouse button to check</param>
        /// <returns>True if the mouse button was just pressed</returns>
        public static bool IsPress(MouseButtons Button)
        {
            return (GetButtonState(Button, ref mouseState) == ButtonState.Pressed &&
                    GetButtonState(Button, ref lastMouseState) == ButtonState.Released);
        }
        /// <summary>
        /// Was a key was just pressed
        /// </summary>
        /// <param name="Key">The key to check</param>
        /// <returns>True if the key was just pressed</returns>
        public static bool IsPress(Keys Key)
        {
            return (keyState.IsKeyDown(Key) && lastKeyState.IsKeyUp(Key));
        }

        /// <summary>
        /// Was a mouse button just released
        /// </summary>
        /// <param name="Button">The mouse button to check</param>
        /// <returns>True if the mouse button was just clicked</returns>
        public static bool IsClick(MouseButtons Button)
        {
            return (GetButtonState(Button, ref mouseState) == ButtonState.Released &&
                    GetButtonState(Button, ref lastMouseState) == ButtonState.Pressed);
        }
        /// <summary>
        /// Was a key was just released
        /// </summary>
        /// <param name="Key">The key to check</param>
        /// <returns>True if the key was just clicked</returns>
        public static bool IsClick(Keys Key)
        {
            return (keyState.IsKeyUp(Key) && lastKeyState.IsKeyDown(Key));
        }
    }
}
