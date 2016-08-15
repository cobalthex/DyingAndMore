namespace Takai.Input
{
    using Microsoft.Xna.Framework.Input;

    /// <summary>
    /// A system to handle and update sequence based input for the xbox controller(s) and PC keyboard (mapped to xbox buttons)
    /// </summary>
    public class InputSequencer
    {
        #region Data

        #region Public
        /// <summary>
        /// The current stream of input
        /// </summary>
        public System.Collections.Generic.List<Buttons> stream { get; private set; }

        /// <summary>
        /// how long the stream of events can be alive before being cleared out
        /// </summary>
        public System.TimeSpan streamTimeOut;

        /// <summary>
        /// how long the player has to make multiple button presses
        /// </summary>
        public System.TimeSpan streamMergeTime;

        /// <summary>
        /// allow opposite input directions to be recorded at once (useful for games like DDR)
        /// </summary>
        public bool allowOppositeDirections = true;

        /// <summary>
        /// allow non-directional (ABXY) buttons to be grouped together as one button
        /// </summary>
        public bool allowButtonGrouping = true;

        /// <summary>
        /// maximum number of inputs in a stream at one point
        /// </summary>
        public int maxHits = 12;

        /// <summary>
        /// The default player index
        /// </summary>
        public Microsoft.Xna.Framework.PlayerIndex player;

        /// <summary>
        /// Use touch (mouse + touch) input (on applicable platforms)
        /// </summary>
        public bool useTouch = true;
        /// <summary>
        /// Use keyboard input (on applicable platforms)
        /// </summary>
        public bool useKeyboard = true;
        /// <summary>
        /// Use gamepad input (on applicable platforms)
        /// </summary>
        public bool useGamepad = true;

        #endregion

        /// <summary>
        /// current button presses (for merging)
        /// </summary>
        Buttons currentHits = 0;

        /// <summary>
        /// Hits from the previous input query
        /// </summary>
        Buttons oldHits = 0;

        /// <summary>
        /// Last time an input event was taken (in ticks)
        /// </summary>
        System.TimeSpan LastInput = System.TimeSpan.Zero;

#if WINDOWS
        /// <summary>
        /// minimum required delta position to recognize gesture
        /// </summary>
        int minReqVelocity = 10;

        bool hasPressed = false;
        float ldx, ldy; //Last delta mouse position
#endif

        #endregion

        #region Setup

        public InputSequencer(Microsoft.Xna.Framework.PlayerIndex Player)
        {
            streamTimeOut = System.TimeSpan.FromMilliseconds(750);
            streamMergeTime = System.TimeSpan.FromMilliseconds(50);

            stream = new System.Collections.Generic.List<Buttons>();

            player = Player;
        }

        #endregion

        #region Updating

        public void Update(Microsoft.Xna.Framework.GameTime time)
        {
            if (!InputCatalog.IsInitialized)
                return;

            int eventCount = stream.Count; //used to track changes to see if need to clear stream

            Buttons LastHits = currentHits; //used to see if new button was pressed

            bool merge = true;

#if !(WINDOWS_PHONE || ZUNE)
            //directions
            if (useGamepad)
            {
                if (isButton(Buttons.A))
                    currentHits |= Buttons.A;
                if (isButton(Buttons.B))
                    currentHits |= Buttons.B;
                if (isButton(Buttons.X))
                    currentHits |= Buttons.X;
                if (isButton(Buttons.Y))
                    currentHits |= Buttons.Y;

                Buttons b = getDirection();
                if (b != oldHits)
                {
                    currentHits |= b;
                    merge = false; //force no merge so that direction can't be changed
                }
                oldHits = b;
            }
#endif

#if WINDOWS
            if (useKeyboard)
            {
                //buttons (a, b, x, y)
                if (isKey(Keys.A))
                    currentHits |= Buttons.A;
                if (isKey(Keys.B))
                    currentHits |= Buttons.B;
                if (isKey(Keys.X))
                    currentHits |= Buttons.X;
                if (isKey(Keys.Y))
                    currentHits |= Buttons.Y;
            }

            if (useTouch)
            {

                if (hasPressed && InputCatalog.MouseState.LeftButton == ButtonState.Released)
                    hasPressed = false;

                int difX = InputCatalog.MouseState.X - InputCatalog.LastMouseState.X;
                int difY = InputCatalog.MouseState.Y - InputCatalog.LastMouseState.Y;

                if (!hasPressed && InputCatalog.MouseState.LeftButton == ButtonState.Pressed)
                {
                    Buttons pDpad = currentHits;
                    if (difX > minReqVelocity)
                        currentHits |= Buttons.DPadRight;
                    if (difX < -minReqVelocity && (allowButtonGrouping || (currentHits & Buttons.DPadRight) == 0))
                        currentHits |= Buttons.DPadLeft;
                    if (difY < -minReqVelocity)
                        currentHits |= Buttons.DPadUp;
                    if (difY > minReqVelocity && (allowButtonGrouping || (currentHits & Buttons.DPadUp) == 0))
                        currentHits |= Buttons.DPadDown;

                    if (pDpad != currentHits)
                        hasPressed = true;
                }

                ldx = difX;
                ldy = difY;
            }
#elif WINDOWS_PHONE || ZUNE_HD

#endif

            //log the first hit so multiple presses can be merged
            if (LastHits == 0 && currentHits > 0)
            {
                stream.Add(currentHits);
                LastInput = time.TotalGameTime;
            }

            //should the buttons merge?
            if (merge)
                merge = currentHits > 0 && time.TotalGameTime - LastInput < streamMergeTime;

            //add current button sequence to the stream if it has passed the merge time
            if (!merge)
            {
                if (stream.Count > maxHits)
                {
                    stream.RemoveAt(0);
                    eventCount++;
                }
                currentHits = 0;
            }
            else
                stream[stream.Count - 1] = currentHits; //merge new input with Last 

            if (stream.Count != eventCount) //update the Last input time on new button
                LastInput = time.TotalGameTime;
            else if (time.TotalGameTime - LastInput > streamTimeOut)
                stream.Clear();
        }

        #endregion

        #region Helper functions

#if WINDOWS
        bool isKey(Keys k)
        {
            if (InputCatalog.KBState.IsKeyDown(k) && InputCatalog.lastKBState.IsKeyUp(k) && (allowButtonGrouping || currentHits == 0))
                return true;
            return false;
        }
#endif

#if !(WINDOWS_PHONE || ZUNE_HD)
        bool isButton(Buttons b)
        {
            if (InputCatalog.GPadState[(int)player].IsButtonDown(b) && InputCatalog.GPadState[(int)player].IsButtonUp(b) && (allowButtonGrouping || currentHits == 0))
                return true;
            return false;
        }

        /// <summary>
        /// A special function for direction so smooth direction changes are possible (change directions without releasing button)
        /// </summary>
        /// <returns>A combined buttons enum (can compare current to previous to see changes)</returns>
        Buttons getDirection()
        {
            Buttons b = 0;

            //directions (up, down, left, right)
            //a bit of multiplatform code here (quite ugly) because keyboard state is only defined on windows (no use for it on the xbox)
            //inline if statement to only call once (probably not the nicest way of doing it, but...)
            if (InputCatalog.GPadState[(int)player].IsButtonDown(Buttons.DPadUp) || InputCatalog.GPadState[(int)player].IsButtonDown(Buttons.LeftThumbstickUp)
#if WINDOWS
 || InputCatalog.KBState.IsKeyDown(Keys.Up)
#endif
)
            {
                if (allowOppositeDirections || (b & Buttons.DPadDown) != Buttons.DPadDown)
                    b |= Buttons.DPadUp;
            }
            if (InputCatalog.GPadState[(int)player].IsButtonDown(Buttons.DPadDown) || InputCatalog.GPadState[(int)player].IsButtonDown(Buttons.LeftThumbstickDown)
#if WINDOWS
 || InputCatalog.KBState.IsKeyDown(Keys.Down)
#endif
)
                if (allowOppositeDirections || (b & Buttons.DPadUp) != Buttons.DPadUp)
                    b |= Buttons.DPadDown;

            if (InputCatalog.GPadState[(int)player].IsButtonDown(Buttons.DPadLeft) || InputCatalog.GPadState[(int)player].IsButtonDown(Buttons.LeftThumbstickLeft)
#if WINDOWS
 || InputCatalog.KBState.IsKeyDown(Keys.Left)
#endif
)
            {
                if (allowOppositeDirections || (b & Buttons.DPadRight) != Buttons.DPadRight)
                    b |= Buttons.DPadLeft;
            }
            if (InputCatalog.GPadState[(int)player].IsButtonDown(Buttons.DPadRight) || InputCatalog.GPadState[(int)player].IsButtonDown(Buttons.LeftThumbstickRight)
#if WINDOWS
 || InputCatalog.KBState.IsKeyDown(Keys.Right)
#endif
)
                if (allowOppositeDirections || (b & Buttons.DPadLeft) != Buttons.DPadLeft)
                    b |= Buttons.DPadRight;

            return b;
        }
#endif

        public void Clear()
        {
            stream.Clear();
            currentHits = 0;
            LastInput = System.TimeSpan.Zero;
        }

        #endregion
    }
}
