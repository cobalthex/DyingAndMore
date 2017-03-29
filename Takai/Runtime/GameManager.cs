using System.Collections.Generic;

namespace Takai.Runtime
{
    /// <summary>
    /// A helper/wrapper around the game. Also manages game state
    /// </summary>
    public static class GameManager
    {
        /// <summary>
        /// A reference to the game and its properties
        /// </summary>
        public static Microsoft.Xna.Framework.Game Game { get; internal set; }

        /// <summary>
        /// A pointer to the game's graphics device
        /// </summary>
        public static Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice { get { return Game.GraphicsDevice; } }

        /// <summary>
        /// has the state manager been initialized
        /// </summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// All of the active states
        /// </summary>
        static List<GameState> states;

        /// <summary>
        /// Get the top most state in the state stack
        /// </summary>
        public static GameState TopState { get { return states[states.Count - 1]; } }

        /// <summary>
        /// The number of states in the state manager
        /// </summary>
        public static int Count { get { return states.Count; } }

        /// <summary>
        /// The first state to draw (index in the State stack)
        /// </summary>
        static int firstDraw = 0;

        /// <summary>
        /// Is the game exiting?
        /// </summary>
        public static bool IsExiting { get; private set; }

        /// <summary>
        /// Does the game window have focus?
        /// </summary>
        public static bool HasFocus { get { return Game.IsActive; } }

        /// <summary>
        /// Initialize state manager
        /// </summary>
        /// <param name="game">The game this state manager is part of</param>
        public static void Initialize(Microsoft.Xna.Framework.Game game)
        {
            states = new List<GameState>();
            Game = game;

            //Todo: maybe switch to use game components, and add automatic management

            IsInitialized = true;
        }

        /// <summary>
        /// Update the state manager and all of the states in it
        /// </summary>
        /// <param name="time">Game time</param>
        public static void Update(Microsoft.Xna.Framework.GameTime time)
        {
            if (IsExiting)
                Game.Exit();

            //make sure game has focus
            if (!Game.IsActive)
                return;

            //update the states in reverse order
            for (int i = states.Count - 1; i >= 0; --i)
            {
                GameState s = states[i];
                if (s.IsEnabled)
                {
                    s.Update(time);

                    if (!s.UpdateBelow)
                        break;
                }
            }
        }

        /// <summary>
        /// Draw all of the states in the state manager
        /// </summary>
        /// <param name="time">game time</param>
        public static void Draw(Microsoft.Xna.Framework.GameTime time)
        {
            for (int i = firstDraw; i < states.Count; ++i)
            {
                if (states[i].IsVisible)
                {
                    states[i].Draw(time);
                }
            }
        }

        /// <summary>
        /// Exit the game (handles cleanup)
        /// </summary>
        public static void Exit()
        {
            IsExiting = true;
        }

        /// <summary>
        /// Add a state to the top of the State stack
        /// </summary>
        /// <param name="state">The State to add</param>
        public static void PushState(GameState state)
        {
            if (state == null)
                throw new System.ArgumentNullException("state");

            states.Add(state);
            ActivateState(state);
        }

        /// <summary>
        /// Remove all states until a no below draw state is found and swap it with next
        /// </summary>
        /// <param name="nextState">State to replace the top</param>
        /// <returns>the previous state that was popped off the state stack</returns>
        /// <remarks>The previous state is unloaded</remarks>
        public static void NextState(GameState nextState)
        {
            System.Diagnostics.Contracts.Contract.Assert(nextState != null);

            while (states.Count > 0 && states[states.Count - 1].DrawBelow)
                PopState();
            if (states.Count > 0)
                PopState(); //remove the 'base' non-draw below state

            if (!nextState.DrawBelow)
                firstDraw = states.Count;

            PushState(nextState);
        }

        /// <summary>
        /// Remove the top most State from the state stack
        /// </summary>
        /// <returns>The previous top most State</returns>
        public static GameState PopState()
        {
            GameState s = states[states.Count - 1];
            states.RemoveAt(states.Count - 1);
            s.Unload();
            s.IsLoaded = false;
            return s;
        }

        /// <summary>
        /// Remove a state from the state stack
        /// </summary>
        /// <param name="state">The state to remove</param>
        internal static void RemoveState(GameState state)
        {
            states.Remove(state);
            state.Unload();
            state.IsLoaded = false;
        }

        /// <summary>
        /// Loads the state and sets all properties
        /// </summary>
        /// <param name="state">State to activate</param>
        static void ActivateState(GameState state)
        {
            if (state.GraphicsDevice == null)
                state.GraphicsDevice = Game.GraphicsDevice;

            if (!state.IsLoaded)
            {
                state.Load();
                state.IsLoaded = true;
            }

            state.Activate();
        }

        /// <summary>
        /// Is this state being updated (checks states above it)
        /// </summary>
        /// <param name="state">The state to check</param>
        /// <returns>true if the state is updating, false otherwise</returns>
        public static bool IsUpdating(GameState state)
        {
            for (var i = states.Count - 1; i >= 0; --i)
            {
                if (states[i] != state && !states[i].UpdateBelow)
                    return false;
                if (states[i] == state)
                    return true;
            }

            throw new System.ArgumentOutOfRangeException("The state is not stored in the state manager");
        }

        /// <summary>
        /// Get a rectangle with the TV safe area to draw to (defaults to the inner 85% of the screen)
        /// </summary>
        /// <returns>A rectangle with the safe area</returns>
        public static Microsoft.Xna.Framework.Rectangle GetTitleSafeArea(Microsoft.Xna.Framework.Rectangle viewport, float safeRegion = 0.85f)
        {
            int w = (int)(viewport.Width * safeRegion);
            int h = (int)(viewport.Height * safeRegion);

            return new Microsoft.Xna.Framework.Rectangle((viewport.Width - w) >> 1, (viewport.Height - h) >> 1, w, h);
        }

        /// <summary>
        /// Handler for a state manager event
        /// </summary>
        public delegate void StateEventHandler();
    }
}
