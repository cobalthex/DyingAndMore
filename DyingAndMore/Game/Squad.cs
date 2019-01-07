using System;
using System.Collections.Generic;
using DyingAndMore.Game.Entities;

namespace DyingAndMore.Game
{
    public struct ActorSpawn
    {
        public ActorClass actor;
        public Takai.Range<int> count;
    }

    /// <summary>
    /// Represents a group of actors that can coordinate. 
    /// Squads can spawn a variable number of members and are optionally directed by a leader.
    /// Squads can respawn per rules set in this class
    /// </summary>
    public class Squad
    {
        public string Name { get; set; } //todo: this should auto-generate

        public List<ActorClass> LeaderTemplate { get; set; }
        public List<ActorSpawn> UnitsTemplate { get; set; }

        public Microsoft.Xna.Framework.Vector2 SpawnPosition { get; set; }
        public float SpawnRadius { get; set; }

        public bool DontSpawnAutomatically { get; set; } = false;
        public int MinLiveCount { get; set; } = 0;
        public int MaxLiveCount { get; set; } = 1;
        public int MaxSpawnCount { get; set; } = 0; //0 for infinite (spawns forever)
        public TimeSpan RespawnDelay { get; set; }

        //live data below

        public ActorInstance Leader { get; set; }
        public List<ActorInstance> Units { get; set; } //includes leader
        
        public bool HasSpawned { get; set; }
        public int TotalSpawnCount { get; set; } = 0;
        public TimeSpan LastSpawnTime { get; set; } = TimeSpan.MinValue;

        //one leader? (units flee when leader is killed)

        public void Update(TimeSpan deltaTime)
        {
            //udpate live count (remove dead units)
            for (int i = 0; i < Units.Count; ++i)
            {
                if (!Units[i].IsAlive)
                {
                    if (Units[i] == Leader)
                        Leader = null;
                    Units[i] = Units[Units.Count - 1];
                    --i;
                }
            }
        }

        /// <summary>
        /// Get the next spawns for this squad. Updates Leader/Units
        /// </summary>
        /// <param name="elapsedTime">Elapsed game time</param>
        /// <returns>Call repeatedly to get all spawns</returns>
        public IEnumerable<ActorInstance> GetNextSpawns(TimeSpan elapsedTime)
        {
            if (!HasSpawned ||
                //todo: respawn delays
                Units.Count >= MaxLiveCount ||
                TotalSpawnCount >= MaxSpawnCount)
                yield break;

            ActorInstance nextUnit = null;
            if (Leader == null)
            {
                if (LeaderTemplate != null)
                    Leader = nextUnit = (ActorInstance)Takai.Util.Random(LeaderTemplate).Instantiate();
            }
            else if (UnitsTemplate != null)
            {
                if (UnitT)
            }
        }
    }
}

//MinSpawnCount (minimum number of enemies spawned)
//MaxSpawnCount (total number allowed at a time)
//TotalSpawnCount (total number of allowed spawns)
//RespawnDelay (how long to wait after MinSpawnCount is reached)


//track live squad information
//squad class/instance?

//encounters are groups of squads that allow spawning a bunch of squads at  one time if desired