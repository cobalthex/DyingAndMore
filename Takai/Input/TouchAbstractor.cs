using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Takai.Input
{
    /// <summary>
    /// A touch abstractor that works on windows and windows phone (compiles in xbox but does nothing)
    /// <remarks>On windows, touch[0] is left mouse, 1 is middle, 2 is right, 3 is x button 1, 4 is x button 2</remarks>
    /// </summary>
    public static class TouchAbstractor
    {
        public static List<Touch> touches;
        public static List<Touch> lastTouches;

        public static void Initialize()
        {
            touches = new List<Touch>(8);
            lastTouches = new List<Touch>(8);
        }

        /// <summary>
        /// Update the touch system
        /// </summary>
        public static void Update()
        {
            var lst = lastTouches;
            lastTouches = touches;
            touches = lst;
            touches.Clear();

#if WINDOWS_PHONE

#elif WINDOWS
            touches.Add(new Touch(0, InputCatalog.MouseState.X, InputCatalog.MouseState.Y, InputCatalog.MouseState.LeftButton == ButtonState.Pressed));
            touches.Add(new Touch(1, InputCatalog.MouseState.X, InputCatalog.MouseState.Y, InputCatalog.MouseState.MiddleButton == ButtonState.Pressed));
            touches.Add(new Touch(2, InputCatalog.MouseState.X, InputCatalog.MouseState.Y, InputCatalog.MouseState.RightButton == ButtonState.Pressed));
            touches.Add(new Touch(3, InputCatalog.MouseState.X, InputCatalog.MouseState.Y, InputCatalog.MouseState.XButton1 == ButtonState.Pressed));
            touches.Add(new Touch(4, InputCatalog.MouseState.X, InputCatalog.MouseState.Y, InputCatalog.MouseState.XButton1 == ButtonState.Pressed));
#endif
        }

        /// <summary>
        /// Check if there is a touch registered (touches.count > 0 and touch[0] is pressed)
        /// </summary>
        /// <returns></returns>
        public static bool IsTouch()
        {
            return touches.Count > 0 && touches[0].isPressed;
        }

        /// <summary>
        /// Is the user touching a specific region?
        /// </summary>
        /// <param name="Bounds">The region to check</param>
        /// <returns>True if touching in the region, false otherwise</returns>
        public static bool IsTouch(Microsoft.Xna.Framework.Rectangle Bounds)
        {
            return IsTouch() && Bounds.Contains(touches[0].x, touches[0].y);
        }

        /// <summary>
        /// Has the user just pressed
        /// </summary>
        /// <returns>True if clicked</returns>
        public static bool IsPress()
        {
            return (touches.Count > 0 && touches[0].isPressed) && (lastTouches.Count < 1 || !lastTouches[0].isPressed);
        }

        /// <summary>
        /// Has the user just pressed
        /// </summary>
        /// <param name="Bounds">The region to check</param>
        /// <returns>True if clicked in region</returns>
        public static bool IsPress(Microsoft.Xna.Framework.Rectangle Bounds)
        {
            return (touches.Count > 0 && touches[0].isPressed) && (lastTouches.Count < 1 || (!lastTouches[0].isPressed && Bounds.Contains(touches[0].x, touches[0].y)));
        }

        /// <summary>
        /// Has the user pressed and released
        /// </summary>
        /// <returns>True if clicked</returns>
        public static bool IsClick()
        {
            return (lastTouches.Count > 0 && lastTouches[0].isPressed) && (touches.Count < 1 || !touches[0].isPressed);
        }

        /// <summary>
        /// Has the user pressed and released
        /// </summary>
        /// <param name="Bounds">The region to check</param>
        /// <returns>True if clicked in region</returns>
        public static bool IsClick(Microsoft.Xna.Framework.Rectangle Bounds)
        {
            return (lastTouches.Count > 0 && lastTouches[0].isPressed) && (touches.Count < 1 || (!touches[0].isPressed && Bounds.Contains(touches[0].x, touches[0].y)));
        }

        /// <summary>
        /// Is the current touch in a region
        /// </summary>
        /// <param name="Bounds">The region to check</param>
        /// <returns>True if in the region</returns>
        public static bool InRegion(Microsoft.Xna.Framework.Rectangle Bounds)
        {
            return touches.Count > 0 && Bounds.Contains(touches[0].x, touches[0].y);
        }
    }

    public struct Touch
    {
        public int id;
        public int x, y;
        public bool isPressed;

        public Touch(int ID, int X, int Y, bool IsPressed)
        {
            id = ID;
            x = X;
            y = Y;
            isPressed = IsPressed;
        }

        /// <summary>
        /// Get the position of this touch as a vector
        /// </summary>
        public Microsoft.Xna.Framework.Vector2 position
        {
            get { return new Microsoft.Xna.Framework.Vector2(x, y); }
            set { x = (int)value.X; y = (int)value.Y; }
        }
    }
}
