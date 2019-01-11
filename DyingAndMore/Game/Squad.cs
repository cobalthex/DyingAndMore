﻿using System;
using System.Collections.Generic;
using DyingAndMore.Game.Entities;
using Takai.Game;

namespace DyingAndMore.Game
{
    /// <summary>
    /// Represents a group of actors that can coordinate. 
    /// Squads can spawn a variable number of members and are optionally directed by a leader.
    /// Squads can respawn per rules set in this class
    /// </summary>
    public class Squad : ISpawnable
    {
        public string Name { get; set; } //todo: this should auto-generate

        public List<ActorClass> LeaderTemplate { get; set; } //a list of possible leaders. Only one is spawned
        public List<ActorClass> UnitsTemplate { get; set; } //a list of all possible members of this squad (not including leaders)

        public Microsoft.Xna.Framework.Vector2 SpawnPosition { get; set; }
        public float SpawnRadius { get; set; }

        public Takai.Range<int> InitialLiveCount { get; set; } = 0; //0 for don't spawn automatically
        public int MinLiveCount { get; set; } = 0;
        public int MaxLiveCount { get; set; } = 1;
        public int MaxSpawnCount { get; set; } = 0; //0 for infinite (spawns forever)
        public TimeSpan SpawnDelay { get; set; }

        /// <summary>
        /// How long to wait before resetting the spawn count. 
        /// All units must be dead and at MaxSpawnCount
        /// </summary>
        public TimeSpan ResetDelay { get; set; }

        public bool DisableSpawningIfLeaderIsDead { get; set; }

        //live data below

        /// <summary>
        /// The leader of this squad. There is only one and when they die units may flee
        /// </summary>
        public ActorInstance Leader { get; set; }
        public List<ActorInstance> Units { get; set; } = new List<ActorInstance>(); //includes leader
        
        public int TotalSpawnCount { get; set; } = 0;
        public TimeSpan LastSpawnTime { get; set; } = TimeSpan.Zero;
        
        protected ActorInstance SpawnUnit(MapBaseInstance map, List<ActorClass> template)
        {
            var unit = (ActorInstance)Takai.Util.Random(template).Instantiate();
            Units.Add(unit);
            if (TryPlaceUnit(unit, map))
                ++TotalSpawnCount;
            return unit;
        }

        public virtual void OnSpawn(MapBaseInstance map)
        {
            if (LeaderTemplate != null)
                Leader = SpawnUnit(map, LeaderTemplate);
            
            if (UnitsTemplate != null)
            {
                for (int i = 0; i < Takai.RangeHelpers.Random(InitialLiveCount) - 1; ++i) //- 1 for leader
                    SpawnUnit(map, UnitsTemplate);
            }

            LastSpawnTime = map.ElapsedTime;
        }

        protected virtual bool TryPlaceUnit(ActorInstance unit, MapBaseInstance map)
        {
            for (int i = 0; i < 10; ++i) //# of retries to find spawn point - todo: don't hard code (maybe based off radius)
            {
                unit.Position = Takai.Util.RandomCircle(SpawnPosition, SpawnRadius);

                if (map.TestRegionForEntities(unit.Position, unit.Radius / 2))
                    continue;

                //todo: set other entity props here
                map.Spawn(unit);
                return true;
            }

            return false;
        }

        public virtual void OnDestroy(MapBaseInstance map)
        {
            foreach (var unit in Units)
                unit.Kill();

            Units.Clear();
            Leader = null;
        }

        public void Update(MapBaseInstance map, TimeSpan deltaTime)
        {
            //todo: reset delay
            if (TotalSpawnCount == 0 && InitialLiveCount.max > 0)
                OnSpawn(map);

            else if (ResetDelay > TimeSpan.Zero &&
                     TotalSpawnCount >= MaxSpawnCount &&
                     Units.Count == 0 &&
                     map.ElapsedTime >= LastSpawnTime + ResetDelay)
            {
                Leader = null;
                TotalSpawnCount = 0;
                OnSpawn(map);

            }

            //udpate live count (remove dead units)
            for (int i = 0; i < Units.Count; ++i)
            {
                if (!Units[i].IsAlive)
                {
                    if (Units[i] == Leader)
                    {
                        //todo: notify units that leader is dead
                    }
                    Units[i] = Units[Units.Count - 1];
                    Units.RemoveAt(Units.Count - 1);
                    --i;
                }
            }

            if (UnitsTemplate == null || (DisableSpawningIfLeaderIsDead && Leader != null && !Leader.IsAlive))
                return;

            int liveUnits = Units.Count;
            if (liveUnits < MinLiveCount)
            {
                for (int i = 0; i < MinLiveCount && TotalSpawnCount < MaxSpawnCount; ++i)
                    SpawnUnit(map, UnitsTemplate);
            }
            else if (map.ElapsedTime >= LastSpawnTime + SpawnDelay)
            {
                //limit this per delay cycle?
                for (int i = 0; i < MaxLiveCount && TotalSpawnCount < MaxSpawnCount; ++i)
                    SpawnUnit(map, UnitsTemplate);
            }

            if (Units.Count > liveUnits)
                LastSpawnTime = map.ElapsedTime;
        }
    }
}

//encounters(?) are groups of squads that allow spawning a bunch of squads at  one time if desired