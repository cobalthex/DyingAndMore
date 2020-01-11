using System;
using System.Collections.Generic;
using DyingAndMore.Game.Entities;

namespace DyingAndMore.Game.Weapons
{
    class SpawnerClass : WeaponClass
    {   
        public override WeaponInstance Instantiate()
        {
            return new SpawnerInstance(this);
        }
    }

    class SpawnerInstance : WeaponInstance
    {
        /// <summary>
        /// The squad to pick actors from
        /// </summary>
        [Takai.Data.Serializer.AsReference]
        public Squad Squad
        {
            get => _squad;
            set
            {
                if (_squad == value)
                    return;

                _squad = value;
                SpawnQueue?.Clear();
            }
        }
        private Squad _squad;

        public new SpawnerClass Class
        {
            get => (SpawnerClass)base.Class;
            set => base.Class = value;
        }

        /// <summary>
        /// The actual list of entities to spawn
        /// </summary>
        public Queue<ActorClass> SpawnQueue { get; set; }

        public SpawnerInstance() { }
        public SpawnerInstance(SpawnerClass @class)
            : base(@class)
        {
            GenerateSpawnQueue();
        }

        public void GenerateSpawnQueue()
        {
            if (Squad == null)
                return;

            if (SpawnQueue == null)
                SpawnQueue = new Queue<ActorClass>();
            else
                SpawnQueue.Clear();

            //todo: bind to squad?

            //spawn leader last?
            if (Squad.LeaderTemplate.Count > 0)
                SpawnQueue.Enqueue(Takai.Util.Random(Squad.LeaderTemplate));

            if (Squad.UnitsTemplate.Count > 0)
            {
                for (int i = 0; i < (Squad.MaxSpawnCount == 0 ? 50 : Squad.MaxSpawnCount); ++i) //todo: dont limit
                    SpawnQueue.Enqueue(Takai.Util.Random(Squad.UnitsTemplate));
            }
        }

        public override bool IsDepleted()
        {
            //reset delay?
            return Squad == null || (SpawnQueue != null && SpawnQueue.Count <= 0); //todo: handle deserializing spawn queue better
        }

        public override bool CanUse(TimeSpan totalTime)
        {
            //use squad timers here
            return base.CanUse(totalTime);
        }

        protected override void OnDischarge()
        {
            if (SpawnQueue == null)
            {
                GenerateSpawnQueue();
                if (IsDepleted())
                    return;
            }

            var next = SpawnQueue.Dequeue();
            if (next != null)
            {
                Actor.Map.Spawn(next, Actor.WorldPosition + Actor.WorldForward * (Actor.Radius + 10), Actor.WorldForward, Actor.Forward * 100);
                base.OnDischarge();
            }
        }

        public override bool Combine(WeaponInstance other)
        {
            if (Class == null || other.Class == Class)
                return false;

            var spawner = (SpawnerInstance)other;
            foreach (var ent in spawner.SpawnQueue)
                SpawnQueue.Enqueue(ent);
            spawner.SpawnQueue.Clear();

            return true;
        }

        public override string ToString()
        {
            return $"{base.ToString()} ({SpawnQueue?.Count ?? 0})";
        }
    }
}
