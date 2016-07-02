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
    /// A basic trigger, performs an action when an entity enters the collidable region
    /// </summary>
    class Trigger : Takai.Game.Entity
    {
        /// <summary>
        /// This trigger only affects players
        /// </summary>
        public bool PlayerOnly { get; set; }

        public TriggerType Type { get; set; }
    }
}
