using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public List<Entity> Entities { get; set; }
    }
}
