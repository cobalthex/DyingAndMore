namespace Takai.States
{
    /// <summary>
    /// A single state for a state manager
    /// </summary>
    public class State
    {
        #region Data

        /// <summary>
        /// The type of this state
        /// </summary>
        public StateType type;

        /// <summary>
        /// When this state started
        /// (Reset on reactivation)
        /// </summary>
        public System.TimeSpan startTime { get; internal set; }

        /// <summary>
        /// Is the state visible?
        /// </summary>
        public bool isVisible;
        /// <summary>
        /// Is the state updating?
        /// </summary>
        public bool isEnabled;

        /// <summary>
        /// Has the state been loaded?
        /// </summary>
        public bool isLoaded = false;
        
        /// <summary>
        /// Force this state to draw to its render target (Disabled becauase breaks rendering)
        /// </summary>
        //public bool forceRenderTarget = false;

        /// <summary>
        /// The graphics device for this game (only set if state is active)
        /// </summary>
        public Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice { get; internal set; }

        #endregion

        #region Functions

        /// <summary>
        /// Create a new state
        /// (defaults to invisible and inactive)
        /// </summary>
        /// <param name="screenType">The type of state this is</param>
        public State(StateType screenType)
        {
            type = screenType;
        }

        /// <summary>
        /// Load data for the state (The screen can either create its own content manager or use the state manager's content manager)
        /// </summary>
        public virtual void Load() { isLoaded = true; }
        /// <summary>
        /// Unload data from the state
        /// </summary>
        public virtual void Unload() { isLoaded = false; }

        /// <summary>
        /// Update the state
        /// </summary>
        /// <param name="time">game time</param>
        public virtual void Update(Microsoft.Xna.Framework.GameTime Time) { }
        /// <summary>
        /// Draw the state
        /// </summary>
        /// <param name="time">game time</param>
        public virtual void Draw(Microsoft.Xna.Framework.GameTime Time) { }

        #region Helper functions

        /// <summary>
        /// Enable and show the state
        /// </summary>
        public void Activate() { isEnabled = true; isVisible = true; }
        /// <summary>
        /// Disable and hide the state
        /// </summary>
        public void Deactivate() { isEnabled = isVisible = false; }

        #endregion

        #region Events

        /// <summary>
        /// Called when the state is activated
        /// </summary>
        public StateManager.StateEventHandler OnEnter;
        /// <summary>
        /// Called when the game window is resized
        /// </summary>
        public StateManager.StateEventHandler OnResize;

        #endregion

        #endregion
    }

    #region Enums

    /// <summary>
    /// The type of state
    /// </summary>
    public enum StateType
    {
        /// <summary>
        /// A full state (disables drawing and updating for any state under)
        /// </summary>
        Full,
        /// <summary>
        /// A modal popup (allows drawing but disables updating under)
        /// </summary>
        Popup,
        /// <summary>
        /// A non blocking popup (allows drawing and updating under)
        /// </summary>
        Overlay
    }

    #endregion
}
