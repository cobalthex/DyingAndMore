using System.Collections.Generic;

namespace Takai.States
{
    /// <summary>
    /// Manages multiple program/game states
    /// </summary>
    public static class GameStateManager
    {
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
        /// A reference to the game and its properties
        /// </summary>
        public static Microsoft.Xna.Framework.Game Game { get; internal set; }
        
        /// <summary>
        /// Initialize state manager
        /// </summary>
        /// <param name="Game">The game this state manager is part of</param>
        public static void Initialize(Microsoft.Xna.Framework.Game Game)
        {
            states = new List<GameState>();
            GameStateManager.Game = Game;

            IsInitialized = true;
        }

        /// <summary>
        /// Update the state manager and all of the states in it
        /// </summary>
        /// <param name="time">Game time</param>
        public static void Update(Microsoft.Xna.Framework.GameTime Time)
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
                    s.Update(Time);
                   
                    if (!s.UpdateBelow)
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
                if (states[i].IsVisible)
                {
                    states[i].Draw(Time);
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
        /// <param name="State">The State to add</param>
        public static void PushState(GameState State)
        {
            if (State == null)
                throw new System.ArgumentNullException("add");

            states.Add(State);
            ActivateState(State);
        }

        /// <summary>
        /// Remove all states until a no below draw state is found and swap it with next
        /// </summary>
        /// <param name="NextState">State to replace the top</param>
        /// <returns>the previous state that was popped off the state stack</returns>
        /// <remarks>The previous state is unloaded</remarks>
        public static void NextState(GameState NextState)
        {
            System.Diagnostics.Contracts.Contract.Assert(NextState != null);

            GameState s;
            while (states.Count > 0 && ((s = states[states.Count - 1]).DrawBelow))
            {
                s.Unload();
                s.IsLoaded = false;
                states.RemoveAt(states.Count - 1);
            }

            if (!NextState.DrawBelow)
                firstDraw = states.Count;

            PushState(NextState);
        }

        /// <summary>
        /// Remove the top most State from the state stack
        /// </summary>
        /// <returns>The previous top most State</returns>
        public static GameState PopState()
        {
            GameState s = states[states.Count - 1];
            states.RemoveAt(states.Count - 1);
            s.Deactivate();
            return s;
        }

        /// <summary>
        /// Loads the state and sets all properties
        /// </summary>
        /// <param name="State">State to activate</param>
        static void ActivateState(GameState State)
        {
            State.GraphicsDevice = Game.GraphicsDevice;

            State.Activate();
            if (!State.IsLoaded)
            {
                State.Load();
                State.IsLoaded = true;
            }
        }

        /// <summary>
        /// Is this state being updated (checks states above it)
        /// </summary>
        /// <param name="State">The state to check</param>
        /// <returns>true if the state is updating, false otherwise</returns>
        public static bool IsUpdating(GameState State)
        {
            for (var i = states.Count - 1; i >= 0; --i)
            {
                if (states[i] != State && !states[i].UpdateBelow)
                    return false;
                if (states[i] == State)
                    return true;
            }

            throw new System.ArgumentOutOfRangeException("The state is not stored in the state manager");
        }

        /// <summary>
        /// Get a rectangle with the TV safe area to draw to (defaults to the inner 85% of the screen)
        /// </summary>
        /// <returns>A rectangle with the safe area</returns>
        public static Microsoft.Xna.Framework.Rectangle GetTitleSafeArea(Microsoft.Xna.Framework.Rectangle Viewport, float SafeRegion = 0.85f)
        {
            int w = (int)(Viewport.Width * SafeRegion);
            int h = (int)(Viewport.Height * SafeRegion);

            return new Microsoft.Xna.Framework.Rectangle((Viewport.Width - w) >> 1, (Viewport.Height - h) >> 1, w, h);
        }

        /// <summary>
        /// Handler for a state manager event
        /// </summary>
        public delegate void StateEventHandler();
    }
}
