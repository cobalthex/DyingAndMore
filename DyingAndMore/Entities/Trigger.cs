using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Entities
{
    /// <summary>
    /// The different types of triggers
    /// </summary>
    enum TriggerType
    {
        None,
        /// <summary>
        /// Kills whoever enters this trigger volume
        /// </summary>
        Kill,

    }

    /// <summary>
    /// Triggers an action whenever an entity enters the volume
    /// </summary>
    class TriggerVolume : Takai.Game.Entity
    {
        /// <summary>
        /// This trigger only affects players
        /// </summary>
        public bool PlayerOnly { get; set; }

        public TriggerType Type { get; set; }
    }
}
