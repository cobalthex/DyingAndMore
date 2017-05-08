using System.Collections.Generic;

namespace Takai.Game
{
    /// <summary>
    /// A region that can trigger scripts when entered or exited
    /// </summary>
    public class Trigger
    {
        /// <summary>
        /// A name to identify this trigger
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// The rectangular area of this trigger
        /// </summary>
        public Microsoft.Xna.Framework.Rectangle Region { get; set; }

        /// <summary>
        /// Entities currently inside this trigger region
        /// </summary>
        internal HashSet<Entity> ContainedEntities { get; set; } = new HashSet<Entity>();

        public Trigger()
        {
            Region = new Microsoft.Xna.Framework.Rectangle(0, 0, 1, 1);
        }

        public Trigger(Microsoft.Xna.Framework.Rectangle region, string name = null)
        {
            Region = region;
            Name = name;
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
