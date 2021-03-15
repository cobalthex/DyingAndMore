using System;
using System.Collections.Generic;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// Represents a group of actors that can coordinate.
    /// Squads can spawn a variable number of members and are optionally directed by a leader.
    /// Squads can respawn per rules set in this class
    /// </summary>
    public class Squad : Takai.Data.IReferenceable
    {
        public string Name { get; set; } //todo: this should auto-generate

        /// <summary>
        /// The leader of this squad. There is only one and when they die units may flee
        /// </summary>
        [Takai.Data.Serializer.AsReference]
        public ActorInstance Leader { get; set; }

        /// <summary>
        /// All active units in the squad, including the leader
        /// </summary>
        public List<ActorInstance> Units { get; set; } = new List<ActorInstance>();

        public Squad Clone()
        {
            var newSquad = (Squad)MemberwiseClone();
            newSquad.Leader = null;

            return newSquad;
        }

        public override string ToString()
        {
            return nameof(Squad) + ": " + Name;
        }

        /// <summary>
        /// Destroy all units, including the leader
        /// </summary>
        public void DestroyAllUnits()
        {
            foreach (var unit in Units)
                unit.Kill();

            Units.Clear();
            Leader = null;
        }

        public virtual void OnDestroy(MapBaseInstance map)
        {
            DestroyAllUnits();
        }

        public void Update(MapBaseInstance map, TimeSpan deltaTime)
        {
            //udpate live count (remove dead units)
            for (int i = 0; i < Units.Count; ++i)
            {
                if (Units[i].IsAlive && Units[i].Map == map)
                    continue;

                //if (Units[i] == Leader)
                //{
                //    //todo: notify Units that leader is dead
                //}
                Units[i] = Units[Units.Count - 1];
                Units.RemoveAt(Units.Count - 1);
                --i;
            }
        }
    }
}

/* todo:
 * leader promotion
*/