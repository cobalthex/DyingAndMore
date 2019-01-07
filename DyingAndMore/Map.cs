using Takai.Game;
using System.Collections.Generic;

namespace DyingAndMore
{
    public class MapClass : MapBaseClass 
    {
        public override MapBaseInstance Instantiate()
        {
            return new MapInstance();
        }
    }

    public class MapInstance : MapBaseInstance
    {
        public Dictionary<string, Game.Squad> Squads { get; set; }

        public MapInstance() : this(null) { }
        public MapInstance(MapClass @class)
            : base(@class)
        {
            if (@class == null)
                return;

            if (Squads != null)
            {
                foreach (var squad in Squads)
                {
                    if (squad.Value.DontSpawnAutomatically)
                        continue;

                    Spawn(squad.Value);
                }
            }
        }
        
        /// <summary>
        /// Reset squad spawn timer/coounters and spawn (up to limits, including previously spawned members)
        /// </summary>
        /// <param name="squad">The squad to spawn</param>
        public void Spawn(Game.Squad squad)
        {
            squad.HasSpawned = true;
            
        }
    }
}
