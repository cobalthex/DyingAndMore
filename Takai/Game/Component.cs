using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Takai.Game
{
    /// <summary>
    /// A basic component for entities
    /// Components offer individual properties and functions for entities, such as the ability to fire a weapon or possess an inventory
    /// </summary>
    public abstract class Component
    {
        /// <summary>
        /// The parent entity that owns this component
        /// </summary>
        public Entity Entity { get; internal set; }

        public bool IsEnabled { get; set; } = true;
        
        public virtual void Load() { }
        public virtual void Unload() { }

        public virtual void Think(GameTime Time) { }
    }
}
