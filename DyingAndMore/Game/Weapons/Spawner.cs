using System;
using System.Collections.Generic;
using DyingAndMore.Game.Entities;

namespace DyingAndMore.Game.Weapons
{
    class SpawnerClass : WeaponClass
    {
        /// <summary>
        /// An optional squad to assign spawned units to
        /// </summary>
        [Takai.Data.Serializer.AsReference]
        public Squad Squad { get; set; }

        public List<ActorClass> Spawns { get; set; }

        /// <summary>
        /// How many spawns can this spawner produce before it is depleted
        /// 0 for infinite
        /// </summary>
        public int MaxSpawnCount { get; set; } = 0;

        public override WeaponInstance Instantiate()
        {
            return new SpawnerInstance(this);
        }
    }

    class SpawnerInstance : WeaponInstance
    {
        [Takai.Data.Serializer.Ignored]
        public SpawnerClass _Class
        {
            get => (SpawnerClass)base.Class;
            set => base.Class = value;
        }

        protected int spawnCount = 0;

        public SpawnerInstance() { }
        public SpawnerInstance(SpawnerClass @class)
            : base(@class) { }

        public override bool IsDepleted()
        {
            //reset delay?
            return spawnCount >= _Class.MaxSpawnCount; //todo: handle deserializing spawn queue better
        }

        protected override void OnDischarge()
        {
            if (Charge > 0.1f)
                TryUse();

            var next = Takai.Util.Random(_Class.Spawns);
            if (next != null)
            {
                var spawn = (ActorInstance)Actor.Map.Spawn(
                    next, 
                    Actor.WorldPosition + Actor.WorldForward * (Actor.Radius + 10), 
                    Actor.WorldForward, 
                    Actor.Forward * 100
                );

                if (_Class.Squad != null)
                    _Class.Squad.Units.Add(spawn);

                base.OnDischarge();
            }
        }

        public override bool Combine(WeaponInstance other)
        {
            if (Class == null || other.Class != Class)
                return false;

            spawnCount = Math.Min(spawnCount, ((SpawnerInstance)other).spawnCount);
            return true;
        }

        public override string ToString()
        {
            return $"{base.ToString()} ({spawnCount})";
        }
    }
}
