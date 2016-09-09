using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// A device can be marked on or off and responds to events to set power state
    /// </summary>
    class Device : Entity
    {
        /// <summary>
        /// Is the device currently powered?
        /// </summary>
        public bool IsPowered { get; set; } = false;
        /// <summary>
        /// The power group to listen to for events
        /// </summary>
        public string PowerGroup { get; set; } = null;
    }
}
