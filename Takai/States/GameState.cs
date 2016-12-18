namespace Takai.States
{
    /// <summary>
    /// A single state for a state manager
    /// </summary>
    public class GameState
    {
        /// <summary>
        /// Draw states below this one in the state manager's state stack
        /// </summary>
        public bool DrawBelow { get; set; }

        /// <summary>
        /// Update states below this one in the state manager's state stack
        /// </summary>
        public bool UpdateBelow { get; set; }

        /// <summary>
        /// Is the state visible?
        /// </summary>
        public bool IsVisible { get; set; }
        /// <summary>
        /// Is the state updating?
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Has the state been loaded?
        /// </summary>
        public bool IsLoaded { get; set; }

        /// <summary>
        /// The graphics device for this game (only set if state is active)
        /// </summary>
        public Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice { get; internal set; }

        /// <summary>
        /// Create a new state
        /// (defaults to invisible and inactive)
        /// </summary>
        /// <param name="DrawBelow">Allow states below this one in the state stack to draw</param>
        /// <param name="UpdateBelow">Allow states below this one in the state stack to update</param>
        public GameState(bool DrawBelow, bool UpdateBelow)
        {
            this.DrawBelow = DrawBelow;
            this.UpdateBelow = UpdateBelow;
        }

        /// <summary>
        /// Load data for the state (The screen can either create its own content manager or use the state manager's content manager)
        /// </summary>
        public virtual void Load() { IsLoaded = true; }
        /// <summary>
        /// Unload data from the state
        /// </summary>
        public virtual void Unload() { IsLoaded = false; }

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

        /// <summary>
        /// Enable and show the state
        /// </summary>
        public void Activate() { IsEnabled = true; IsVisible = true; }
        /// <summary>
        /// Disable and hide the state
        /// </summary>
        public void Deactivate() { IsEnabled = IsVisible = false; }

        /// <summary>
        /// Called when the state is activated
        /// </summary>
        public GameStateManager.StateEventHandler OnEnter;
        /// <summary>
        /// Called when the game window is resized
        /// </summary>
        public GameStateManager.StateEventHandler OnResize;
    }
}
