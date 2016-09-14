using System.Collections.Generic;

namespace Takai.States
{
    /// <summary>
    /// Manages multiple program/game states
    /// </summary>
    public static class StateManager
    {
        #region Data

        /// <summary>
        /// has the state manager been initialized
        /// </summary>
        public static bool isInitialized { get; private set; }

        /// <summary>
        /// All of the active states
        /// </summary>
        static StateStack states;

        /// <summary>
        /// Get the top most state in the state stack
        /// </summary>
        public static State topState { get { return states.Peek(); } }

        /// <summary>
        /// The first state to draw (index in the State stack)
        /// </summary>
        static int firstDraw = 0;

        /// <summary>
        /// Is the game exiting?
        /// </summary>
        public static bool isExiting { get; private set; }

        /// <summary>
        /// A single reference cache usable by anything to store references
        /// </summary>
        public static object cache;

        /// <summary>
        /// the size of the render area (user modifyable)
        /// </summary>
        public static Microsoft.Xna.Framework.Rectangle viewport;
        /// <summary>
        /// A reference to the game and its properties
        /// </summary>
        public static Microsoft.Xna.Framework.Game game { get; internal set; }

        /// <summary>
        /// Reference time
        /// </summary>
        public static Microsoft.Xna.Framework.GameTime time { get; private set; }

        #endregion

        #region Setup

        /// <summary>
        /// Initialize state manager
        /// </summary>
        /// <param name="Game">The game this state manager is part of</param>
        public static void Initialize(Microsoft.Xna.Framework.Game Game)
        {
            states = new StateStack();
            game = Game;
            time = new Microsoft.Xna.Framework.GameTime();

            viewport = new Microsoft.Xna.Framework.Rectangle(Game.GraphicsDevice.Viewport.X, Game.GraphicsDevice.Viewport.Y, Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Height);
            isInitialized = true;
        }

        #endregion

        #region Updating

        /// <summary>
        /// Update the state manager and all of the states in it
        /// </summary>
        /// <param name="time">Game time</param>
        public static void Update(Microsoft.Xna.Framework.GameTime Time)
        {
            time = Time;

            Microsoft.Xna.Framework.Rectangle nView = game.GraphicsDevice.Viewport.Bounds;
            bool resized = viewport != nView;

            if (isExiting)
                game.Exit();

            //make sure game has focus
            if (!game.IsActive)
                return;

            //update the states in reverse order
            for (int i = states.Count - 1; i >= 0; i--)
            {
                State s = states[i];
                if (s.isEnabled)
                {
                    s.Update(time);

                    if (resized)
                        s.OnResize?.Invoke();

                    //only overlays allow updating under
                    if (s.type != StateType.Overlay)
                        break;
                }
            }
        }

        /// <summary>
        /// Draw all of the states in the state manager
        /// </summary>
        /// <param name="time">game time</param>
        public static void Draw(Microsoft.Xna.Framework.GameTime Time)
        {
            for (int i = firstDraw; i < states.Count; i++)
            {
                if (states[i].isVisible)
                {
                    /*if (states[i].forceRenderTarget)
                        RenderStateToRT(states[i], time);
                    else*/
                    states[i].Draw(time);
                }
            }
        }

        /*
        /// <summary>
        /// Render a state to its render target
        /// </summary>
        /// <param name="s">state to render</param>
        /// <param name="time">timing</param>
        static void RenderStateToRT(State s, Microsoft.Xna.Framework.GameTime time)
        {
            game.GraphicsDevice.SetRenderTarget(s);
            game.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
            s.Draw(time);
            game.GraphicsDevice.SetRenderTarget(null);
        }
        */

        #endregion

        #region Public

        /// <summary>
        /// Exit the game (handles cleanup)
        /// </summary>
        public static void Exit()
        {
            isExiting = true;
        }

        /// <summary>
        /// Does the game window have focus?
        /// </summary>
        /// <returns></returns>
        public static bool HasFocus()
        {
            return game.IsActive;
        }

        /// <summary>
        /// Add a state to the top of the State stack
        /// </summary>
        /// <param name="add">The State to add</param>
        public static void PushState(State add)
        {
            if (add == null)
                throw new System.ArgumentNullException("add");

            states.Push(add);
            ActivateState(add);
        }

        /// <summary>
        /// Swap the top State with a new state
        /// </summary>
        /// <param name="next">State to replace the top</param>
        /// <returns>the previous state that was popped off the state stack</returns>
        /// <remarks>The previous state is unloaded</remarks>
        public static void NextState(State next)
        {
            State last = states.Pop();
            last.Unload();
            last.isLoaded = false;
            if (next == null)
                throw new System.ArgumentNullException("next");
            
            if (next.type == StateType.Full)
                firstDraw = states.Count;

            PushState(next);
        }

        /// <summary>
        /// Remove the top most State from the state stack
        /// </summary>
        /// <returns>The previous top most State</returns>
        /// <remarks>The state is not unloaded</remarks>
        public static State PopState()
        {
            State s = states.Pop();
            s.Deactivate();
            return s;
        }

        /// <summary>
        /// Loads the state and sets all properties
        /// </summary>
        /// <param name="s">State to activate</param>
        static void ActivateState(State s)
        {
            s.GraphicsDevice = game.GraphicsDevice;
            s.startTime = time.TotalGameTime;

            s.Activate();
            if (!s.isLoaded)
            {
                s.Load();
                s.isLoaded = true;
            }
        }

        /// <summary>
        /// Get a rectangle with the TV safe area to draw to (defaults to the inner 85% of the screen)
        /// </summary>
        /// <returns>A rectangle with the safe area</returns>
        public static Microsoft.Xna.Framework.Rectangle GetTitleSafeArea(float SafeRegion = 0.85f)
        {
            int w = (int)(viewport.Width * SafeRegion);
            int h = (int)(viewport.Height * SafeRegion);

            return new Microsoft.Xna.Framework.Rectangle((viewport.Width - w) >> 1, (viewport.Height - h) >> 1, w, h);
        }

        #endregion

        #region Events

        #region Delegates

        /// <summary>
        /// Handler for a state manager event
        /// </summary>
        public delegate void StateEventHandler();

        #endregion

        #endregion
    }
}
