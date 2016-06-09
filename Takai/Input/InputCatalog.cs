﻿//Input.cs

#define USE_ACCELEROMETER

using Microsoft.Xna.Framework.Input;

#if WINDOWS_PHONE
#if USE_ACCELEROMETER
using Microsoft.Devices.Sensors;
#endif
using Microsoft.Xna.Framework.Input.Touch;
#endif

namespace Takai.Input
{
    /// <summary>
    /// Contains input states for all systems and updates them
    /// Must call update regularly
    /// All built in Takai systems that use input use this
    /// </summary>
    public static class InputCatalog
    {
        #region Data

        /// <summary>
        /// Have the systems been initialized?
        /// </summary>
        public static bool isCreated { get; private set; }

#if WINDOWS_PHONE || ZUNE_HD

#if USE_ACCELEROMETER
        private static Accelerometer accel;
        public static AccelerometerReading AccelValue { get; private set; }
        public static AccelerometerReading LastAccelValue { get; private set; }
#endif

        public static TouchCollection Touches { get; private set; }
        public static TouchCollection LastTouches { get; private set; }
#else

#if WINDOWS

        public static MouseState MouseState { get; private set; }
        public static MouseState lastMouseState { get; private set; }
#endif
        public static KeyboardState KBState { get; private set; }
        public static KeyboardState lastKBState { get; private set; }

        public static GamePadState[] GPadState { get; private set; }
        public static GamePadState[] lastGPadState { get; private set; }

        public static Microsoft.Xna.Framework.PlayerIndex ActivePlayer = Microsoft.Xna.Framework.PlayerIndex.One;
#endif

        #endregion

        public static void Update()
        {
            #region Not Created
            if (!isCreated)
            {
#if WINDOWS_PHONE || ZUNE_HD
#if USE_ACCELEROMETER
                accel = new Accelerometer();
                accel.Start();
                LastAccelValue = accel.CurrentValue;
#endif
                LastTouches = TouchPanel.GetState();
#else
#if WINDOWS
                lastMouseState = Mouse.GetState();
#endif
                lastKBState = Keyboard.GetState();

                lastGPadState = new GamePadState[4];
                GPadState = new GamePadState[4];

                lastGPadState[0] = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
                lastGPadState[1] = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.Two);
                lastGPadState[2] = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.Three);
                lastGPadState[3] = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.Four);
#endif

                isCreated = true;
            }
            #endregion

            #region Created, update last
            else
            {
#if WINDOWS_PHONE || ZUNE_HD
#if USE_ACCELEROMETER
                LastAccelValue = AccelValue;
#endif
                LastTouches = Touches;
#else
#if WINDOWS
                lastMouseState = MouseState;
#endif
                lastKBState = KBState;

                for (int i = 0; i < 4; i++)
                    lastGPadState[i] = GPadState[i];
#endif
            }
            #endregion

            #region Update Curerent
#if WINDOWS_PHONE || ZUNE_HD
#if USE_ACCELEROMETER
            AccelValue = accel.CurrentValue;
#endif
            Touches = TouchPanel.GetState();
#else
#if WINDOWS
            MouseState = Mouse.GetState();
#endif
            KBState = Keyboard.GetState();

            for (int i = 0; i < 4; i++)
                GPadState[i] = GamePad.GetState((Microsoft.Xna.Framework.PlayerIndex)i);
#endif
            #endregion
        }

        #region Helpers

#if WINDOWS
        /// <summary>
        /// Has a key been pressed (just pressed)
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if pressed</returns>
        public static bool IsKeyPress(Keys Key)
        {
            return KBState.IsKeyDown(Key) && lastKBState.IsKeyUp(Key);
        }

        /// <summary>
        /// Has a key been clicked (just released)
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if clicked</returns>
        public static bool IsKeyClick(Keys Key)
        {
            return KBState.IsKeyUp(Key) && lastKBState.IsKeyDown(Key);
        }

        /// <summary>
        /// A specific mouse button
        /// </summary>
        public enum MouseButton
        {
            Left,
            Middle,
            Right,
            Button4,
            Button5
        }

        /// <summary>
        /// Has a mouse button been clicked (just pressed)
        /// </summary>
        /// <param name="Button">The mouse button</param>
        /// <returns>True if pressed</returns>
        public static bool IsMouseClick(MouseButton Button)
        {
            switch (Button)
            {
                case MouseButton.Left:
                    return MouseState.LeftButton == ButtonState.Pressed && lastMouseState.LeftButton == ButtonState.Released;
                case MouseButton.Middle:
                    return MouseState.MiddleButton == ButtonState.Pressed && lastMouseState.MiddleButton == ButtonState.Released;
                case MouseButton.Right:
                    return MouseState.RightButton == ButtonState.Pressed && lastMouseState.RightButton == ButtonState.Released;
                case MouseButton.Button4:
                    return MouseState.XButton1 == ButtonState.Pressed && lastMouseState.XButton1 == ButtonState.Released;
                case MouseButton.Button5:
                    return MouseState.XButton2 == ButtonState.Pressed && lastMouseState.XButton2 == ButtonState.Released;
            }
            return false;
        }
        /// <summary>
        /// Check if a mouse button is clicked and in a region
        /// </summary>
        /// <param name="Button">The mouse button</param>
        /// <param name="Region">The window region to check</param>
        /// <returns>True if clicked and in region, false if not</returns>
        public static bool IsMouseClickInRegion(MouseButton Button, Microsoft.Xna.Framework.Rectangle Region)
        {
            return IsMouseClick(Button) && Region.Contains(MouseState.X, MouseState.Y);
        }
        /// <summary>
        /// Has the mouse wheel been scrolled
        /// </summary>
        /// <returns>True if scrolled, false if not</returns>
        public static bool HasMouseScrolled()
        {
            return MouseState.ScrollWheelValue != lastMouseState.ScrollWheelValue;
        }
#endif

#if WINDOWS || XBOX

        /// <summary>
        /// Is a button just pressed
        /// </summary>
        /// <param name="Button">The button(s) to check</param>
        /// <param name="Player">The player to check</param>
        /// <returns>True if pressed</returns>
        public static bool IsButtonPress(Buttons Button, Microsoft.Xna.Framework.PlayerIndex Player)
        {
            return GPadState[(int)Player].IsButtonDown(Button) && lastGPadState[(int)Player].IsButtonUp(Button);
        }

        /// <summary>
        /// Is a button clicked (just released)
        /// </summary>
        /// <param name="Button">The button(s) to check</param>
        /// <param name="Player">The player to check</param>
        /// <returns>True if just released</returns>
        public static bool IsButtonClick(Buttons Button, Microsoft.Xna.Framework.PlayerIndex Player)
        {
            return GPadState[(int)Player].IsButtonUp(Button) && lastGPadState[(int)Player].IsButtonDown(Button);
        }

#endif

        //public static bool IsMouseClick();
        //public static bool isGPadClick();

        #endregion
    }
}
