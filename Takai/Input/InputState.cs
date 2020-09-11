using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Takai.Input
{
    public enum MouseButtons : int
    {
        Left,
        Right,
        Middle,
        X4,
        X5,
        
        _TouchIndex = 8, //for indexing in bitset
    }

    public enum KeyMod
    {
        None,
        Control,
        Shift,
        Alt,
        Windows,
        Super
    }

    //todo: touchdownPoint (where first touched?) and press/release time (maybe)

    public static class InputState
    {
        private static KeyboardState keyState, lastKeyState;
        private static GamePadState[] gamePadState, lastGamePadState;
        private static MouseState mouseState, lastMouseState;
        private static Vector2[] mouseDownPositions;
        public static TouchCollection touches, lastTouches; //todo: make better

        //accelerometer

        static InputState()
        {
            mouseDownPositions = new Vector2[System.Enum.GetNames(typeof(MouseButtons)).Length];
            gamePadState = new GamePadState[4];
            lastGamePadState = new GamePadState[4];
        }

        /// <summary>
        /// Get a vector2 of the current mouse position
        /// </summary>
        public static Vector2 MouseVector { get { return new Vector2(mouseState.X, mouseState.Y); } }
        /// <summary>
        /// Get a point of the current mouse position
        /// </summary>
        public static Point MousePoint { get { return new Point(mouseState.X, mouseState.Y); } }

        /// <summary>
        /// Get a vector2 of the last mouse position
        /// </summary>
        public static Vector2 LastMouseVector { get { return new Vector2(lastMouseState.X, lastMouseState.Y); } }
        /// <summary>
        /// Get a point of the last mouse position
        /// </summary>
        public static Point LastMousePoint { get { return new Point(lastMouseState.X, lastMouseState.Y); } }

        /// <summary>
        /// Mouse position based on center screen
        /// </summary>
        public static Vector2 PolarMouseVector { get; private set; }
        /// <summary>
        /// The previous mouse position based on center screen
        /// </summary>
        public static Vector2 LastPolarMouseVector { get; private set; }

        public static GestureType EnabledGestures
        {
            get => TouchPanel.EnabledGestures;
            set
            {
                TouchPanel.EnabledGestures = value;
                //TouchPanel.EnableMouseGestures = TouchPanel.EnabledGestures > 0;
            }
        }

        public static System.Collections.Generic.Dictionary<GestureType, GestureSample> Gestures { get; set; }
            = new System.Collections.Generic.Dictionary<GestureType, GestureSample>();

        //todo: unify mouse and touch using gestures

        public static void Update(Rectangle Viewport)
        {
            lastKeyState = keyState;
            keyState = Keyboard.GetState();

            Gestures.Clear();
            while (TouchPanel.IsGestureAvailable)
            {
                var tg = TouchPanel.ReadGesture();
                Gestures[tg.GestureType] = tg;
            }

            for (int i = 0; i < gamePadState.Length; ++i)
            {
                lastGamePadState[i] = gamePadState[i];
                gamePadState[i] = GamePad.GetState((PlayerIndex)i);
            }

            lastMouseState = mouseState;
            mouseState = Mouse.GetState();

            LastPolarMouseVector = PolarMouseVector;
            PolarMouseVector = MouseVector - new Vector2(Viewport.Width / 2, Viewport.Height / 2);

            for (int i = 0; i < mouseDownPositions.Length; ++i)
            {
                if (IsPress((MouseButtons)i))
                    mouseDownPositions[i] = MouseVector;
            }

            lastTouches = touches;
            touches = TouchPanel.GetState();
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
        /// Get the change in position of the mouse's position
        /// </summary>
        /// <returns>The change in position</returns>
        public static Vector2 MouseDelta()
        {
            return MouseVector - LastMouseVector;
        }

        /// <summary>
        /// Calculate the amount pinch-zoomed between touch 0 and touch 1
        /// </summary>
        /// <returns>The delta, or zero if no touch</returns>
        public static float TouchPinchDelta()
        {
            if (touches.Count < 2 || lastTouches.Count < 2)
                return 0;

            var lNow = Vector2.DistanceSquared(touches[0].Position, touches[1].Position);
            var lOld = Vector2.DistanceSquared(lastTouches[0].Position, lastTouches[1].Position);
            var diff = lNow - lOld;
            return System.Math.Sign(diff) * (float)System.Math.Sqrt(System.Math.Abs(diff));
        }

        /// <summary>
        /// Has the mouse been dragged since the mouse was pressed
        /// </summary>
        /// <param name="Epsilon">The minimum length required to be considered dragging</param>
        /// <returns>True if the mouse is held down and is being moved</returns>
        public static bool HasMouseDragged(MouseButtons Button, int Epsilon = 10)
        {
            return (IsButtonHeld(Button) &&
                   (MouseVector - mouseDownPositions[(int)Button]).LengthSquared() >= (Epsilon * Epsilon));
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
        /// Was a mouse button just released
        /// </summary>
        /// <param name="Button">The mouse button to check</param>
        /// <returns>True if the mouse button was just clicked</returns>
        public static bool IsClick(MouseButtons Button)
        {
            return (GetButtonState(Button, ref mouseState) == ButtonState.Released &&
                    GetButtonState(Button, ref lastMouseState) == ButtonState.Pressed);
        }

        public static bool IsPress(int touchIndex)
        {
            return (touches.Count > touchIndex && touches[touchIndex].State == TouchLocationState.Pressed);
        }

        public static bool IsClick(int touchIndex)
        {
            return (touches.Count > touchIndex && touches[touchIndex].State == TouchLocationState.Released);
        }

        public static bool IsButtonDown(int touchIndex)
        {
            return (touches.Count > touchIndex);
        }
        public static bool IsButtonUp(int touchIndex)
        {
            return (touches.Count <= touchIndex);
        }

        /// <summary>
        /// Is a key currently pressed?
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key is pressed</returns>
        public static bool IsButtonDown(Keys key)
        {
            return keyState.IsKeyDown(key);
        }

        /// <summary>
        /// Is a key currently released?
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key is released</returns>
        public static bool IsButtonUp(Keys key)
        {
            return keyState.IsKeyUp(key);
        }

        /// <summary>
        /// Is a key currently held down?
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key is held down</returns>
        public static bool IsButtonHeld(Keys key)
        {
            return (keyState.IsKeyDown(key) && lastKeyState.IsKeyDown(key));
        }

        /// <summary>
        /// Was a key was just pressed
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key was just pressed</returns>
        public static bool IsPress(Keys key)
        {
            return (keyState.IsKeyDown(key) && lastKeyState.IsKeyUp(key));
        }

        /// <summary>
        /// Was a key was just released
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key was just clicked</returns>
        public static bool IsClick(Keys key)
        {
            return (keyState.IsKeyUp(key) && lastKeyState.IsKeyDown(key));
        }

        /// <summary>
        /// Is a button currently pressed?
        /// </summary>
        /// <param name="key">The button to check</param>
        /// <returns>True if the button is pressed</returns>
        public static bool IsButtonDown(Buttons button, PlayerIndex player = PlayerIndex.One)
        {
            return gamePadState[(int)player].IsButtonDown(button);
        }

        /// <summary>
        /// Is a button currently released?
        /// </summary>
        /// <param name="key">The button to check</param>
        /// <returns>True if the button is released</returns>
        public static bool IsButtonUp(Buttons button, PlayerIndex player = PlayerIndex.One)
        {
            return gamePadState[(int)player].IsButtonUp(button);
        }

        /// <summary>
        /// Is a button currently held down?
        /// </summary>
        /// <param name="key">The button to check</param>
        /// <returns>True if the button is held down</returns>
        public static bool IsButtonHeld(Buttons button, PlayerIndex player = PlayerIndex.One)
        {
            return (gamePadState[(int)player].IsButtonDown(button) && lastGamePadState[(int)player].IsButtonDown(button));
        }

        /// <summary>
        /// Was a button was just pressed
        /// </summary>
        /// <param name="key">The button to check</param>
        /// <returns>True if the button was just pressed</returns>
        public static bool IsPress(Buttons button, PlayerIndex player = PlayerIndex.One)
        {
            return (gamePadState[(int)player].IsButtonDown(button) && lastGamePadState[(int)player].IsButtonUp(button));
        }

        /// <summary>
        /// Was a button was just released
        /// </summary>
        /// <param name="key">The button to check</param>
        /// <returns>True if the button was just clicked</returns>
        public static bool IsClick(Buttons button, PlayerIndex player = PlayerIndex.One)
        {
            return (gamePadState[(int)player].IsButtonUp(button) && lastGamePadState[(int)player].IsButtonDown(button));
        }

        public static bool IsAnyPress(Buttons button)
        {
            return IsAnyPress(button, out var player);
        }

        public static bool IsAnyPress(Buttons button, out PlayerIndex player)
        {
            for (int i = 0; i < gamePadState.Length; ++i)
            {
                if (IsPress(button, (PlayerIndex)i))
                {
                    player = (PlayerIndex)i;
                    return true;
                }
            }
            player = (PlayerIndex)(-1);
            return false;
        }

        public static bool IsAnyClick(Buttons button)
        {
            for (int i = 0; i < gamePadState.Length; ++i)
            {
                if (IsClick(button, (PlayerIndex)i))
                    return true;
            }
            return false;
        }

        public static GamePadThumbSticks Thumbsticks(PlayerIndex player = PlayerIndex.One)
        {
            return gamePadState[(int)player].ThumbSticks;
        }

        public static GamePadThumbSticks LastThumbsticks(PlayerIndex player = PlayerIndex.One)
        {
            return lastGamePadState[(int)player].ThumbSticks;
        }

        public static GamePadTriggers Triggers(PlayerIndex player = PlayerIndex.One)
        {
            return gamePadState[(int)player].Triggers;
        }

        public static GamePadTriggers LastTriggers(PlayerIndex player = PlayerIndex.One)
        {
            return lastGamePadState[(int)player].Triggers;
        }

        /// <summary>
        /// Checks if a modifier key is currently held down (both left or right)
        /// </summary>
        /// <param name="Mod">The modifier key to check</param>
        /// <returns>True if the modifier key is down</returns>
        public static bool IsMod(KeyMod Mod)
        {
            switch (Mod)
            {
                case KeyMod.Control:
                    return (IsButtonDown(Keys.LeftControl) || IsButtonDown(Keys.RightControl));

                case KeyMod.Shift:
                    return (IsButtonDown(Keys.LeftShift) || IsButtonDown(Keys.RightShift));

                case KeyMod.Alt:
                    return (IsButtonDown(Keys.LeftAlt) || IsButtonDown(Keys.RightAlt));

                case KeyMod.Windows:
                    return (IsButtonDown(Keys.LeftWindows) || IsButtonDown(Keys.RightWindows));

                case KeyMod.None:
                    return !(IsMod(KeyMod.Control) || IsMod(KeyMod.Shift) || IsMod(KeyMod.Alt) || IsMod(KeyMod.Windows));

                default:
                    return false;
            }
        }

        /// <summary>
        /// Get all currently pressed keyboard keys
        /// </summary>
        /// <returns>Currently pressed keys</returns>
        public static Keys[] GetPressedKeys()
        {
            return keyState.GetPressedKeys();
        }
    }
}
