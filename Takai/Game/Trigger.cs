using System.Collections.Generic;

namespace Takai.Game
{
    /// <summary>
    /// A region that can trigger scripts when entered or exited
    /// </summary>
    public class Trigger
    {
        public Microsoft.Xna.Framework.Rectangle Region { get; set; }

        /// <summary>
        /// Entities currently inside this trigger region
        /// </summary>
        internal HashSet<Entity> ContainedEntities { get; set; } = new HashSet<Entity>();

        public Trigger(Microsoft.Xna.Framework.Rectangle region)
        {
            Region = region;
        }

        /// <summary>
        /// Does this trigger currently contain the specified entity
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <returns>True if the entity is colliding with this trigger region</returns>
        public bool Contains(Entity entity)
        {
            return ContainedEntities.Contains(entity);
        }
    }
}
