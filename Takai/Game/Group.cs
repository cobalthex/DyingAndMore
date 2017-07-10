using System.Collections.Generic;

namespace Takai.Game
{
    /// <summary>
    /// A group of entities. Can be used for things like triggers or conditional spawns
    /// </summary>
    public class Group
    {
        /// <summary>
        /// A unique name for this group
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// All of the entities in this group
        /// </summary>
        public HashSet<EntityClass> Entities { get; set; } = new HashSet<EntityClass>();
    }
}
