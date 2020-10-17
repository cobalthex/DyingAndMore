using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
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

        public List<ActorClass> LeaderTemplate { get; set; } //a list of possible leaders. Only one is spawned
        public List<ActorClass> UnitsTemplate { get; set; } //a list of all possible members of this squad (not including leaders)

        public Vector2 SpawnPosition { get; set; }
        public float SpawnRadius { get; set; }

        /// <summary>
        /// Direction to have entities facing when spawned
        /// </summary>
        public Vector2 SpawnForward { get; set; } = -Vector2.UnitX;

        public bool DontSpawnAutomatically { get; set; }
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

        //spawn only off screen?

        //live data below

        /// <summary>
        /// The leader of this squad. There is only one and when they die units may flee
        /// </summary>
        [Takai.Data.Serializer.AsReference]
        public ActorInstance Leader { get; set; }

        /// <summary>
        /// All active units in the squad, including the leader
        /// </summary>
        [Takai.Data.Serializer.Ignored]
        public List<ActorInstance> Units { get; set; } = new List<ActorInstance>(); //includes leader

        /// <summary>
        /// The total number of units spawned
        /// </summary>
        public int TotalSpawnCount { get; set; } = 0;

        /// <summary>
        /// When the last unit was spawned
        /// </summary>
        public TimeSpan LastSpawnTime { get; set; } = TimeSpan.Zero;

        public Squad Clone()
        {
            var newSquad = (Squad)MemberwiseClone();
            newSquad.Leader = null;
            newSquad.LeaderTemplate = LeaderTemplate != null ? new List<ActorClass>(LeaderTemplate) : null;
            newSquad.UnitsTemplate = UnitsTemplate != null ? new List<ActorClass>(UnitsTemplate) : null;
            TotalSpawnCount = 0;
            LastSpawnTime = TimeSpan.Zero;

            return newSquad;
        }

        protected internal void AddUnit(ActorInstance actor)
        {
            if (Units == null)
                Units = new List<ActorInstance>();
            Units.Add(actor);
        }

        protected ActorInstance SpawnUnit(MapBaseInstance map, List<ActorClass> template)
        {
            if (template == null || template.Count < 1)
                return null;

            var unit = (ActorInstance)Takai.Util.Random(template).Instantiate();
            unit.Squad = this;
            if (TryPlaceUnit(unit, map))
                ++TotalSpawnCount;
            return unit;
        }

        protected virtual bool TryPlaceUnit(ActorInstance unit, MapBaseInstance map)
        {
            for (int i = 0; i < 10; ++i) //# of retries to find spawn point - todo: don't hard code (maybe based off radius)
            {
                unit.SetPositionTransformed(Takai.Util.RandomCircle(SpawnPosition, SpawnRadius));
                unit.SetForwardTransformed(SpawnForward);

                if (map.TestRegionForEntities(unit.WorldPosition, unit.Radius / 2))
                    continue;

                //todo: set other entity props here
                map.Spawn(unit);
                return true;
            }

            return false;
        }

        public void SpawnUnits(MapBaseInstance map)
        {
            if (Leader == null)
                Leader = SpawnUnit(map, LeaderTemplate);

            int liveUnits = Units.Count;
            if (liveUnits < MinLiveCount)
            {
                for (int i = 0; i < MinLiveCount && TotalSpawnCount < MaxSpawnCount; ++i)
                    SpawnUnit(map, UnitsTemplate);
            }
            else if (map.ElapsedTime >= LastSpawnTime + SpawnDelay)
            {
                //limit this per delay cycle?
                for (int i = 0; liveUnits < MaxLiveCount && (MaxSpawnCount == 0 || TotalSpawnCount < MaxSpawnCount); ++i, ++liveUnits)
                    SpawnUnit(map, UnitsTemplate);
            }

            if (Units.Count > liveUnits)
                LastSpawnTime = map.ElapsedTime;
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
                if (!Units[i].IsAlive || Units[i].Map != map)
                {
                    //if (Units[i] == Leader)
                    //{
                    //    //todo: notify units that leader is dead
                    //}
                    Units[i] = Units[Units.Count - 1];
                    Units.RemoveAt(Units.Count - 1);
                    --i;
                }
            }

            if (UnitsTemplate == null ||
                (DisableSpawningIfLeaderIsDead && Leader != null && !Leader.IsAlive) ||
                DontSpawnAutomatically)
                return;

            SpawnUnits(map);
        }
    }
}

//encounters(?) are groups of squads that allow spawning a bunch of squads at  one time if desired

//leader promotion