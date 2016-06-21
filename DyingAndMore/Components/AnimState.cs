using System.Collections.Generic;

namespace DyingAndMore.Components
{
    /// <summary>
    /// A simple component that manages the state for an entity. Does not control when states are activated
    /// </summary>
    class AnimState : Takai.Game.Component
    {
        /// <summary>
        /// All available states
        /// </summary>
        public Dictionary<string, Takai.Graphics.Graphic> States { get; set; } = new Dictionary<string, Takai.Graphics.Graphic>();

        /// <summary>
        /// Get or set the current state.
        /// Automatically updates entity sprite on set
        /// </summary>
        /// <remarks>Does nothing if the state does not exist</remarks>
        public string Current
        {
            get
            {
                return currentState;
            }
            set
            {
                if (States.ContainsKey(value))
                {
                    currentState = value;
                    Entity.Sprite = States[currentState];
                    Entity.Sprite.Restart();
                }
            }
        }
        private string currentState = null;
    }
}
