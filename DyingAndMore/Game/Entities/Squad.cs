using System.Collections.Generic;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// A squad represents a group of actors that work together and have group based rules.
    /// There are also triggers that act on squads such as squad death (all actors dead)
    /// </summary>
    class Squad
    {
        public List<Entities.Actor> actors;
    }
}
