using System;
using System.Collections.Generic;
using Takai.Game;

namespace DyingAndMore.Game.Weapons
{
    class SpawnerClass : WeaponClass
    {
        /// <summary>
        /// Spawns are generated from this template. All classes will be spawned with a random number between its range
        /// Spawn order is randomized
        /// </summary>
        public List<Tuple<EntityClass, Takai.Range<int>>> Spawns { get; set; }

        /// <summary>
        /// Generate a list of entities based on the templates provided in <see cref="Spawns"/>. Does not create clones
        /// </summary>
        /// <returns>The list of (references to original) entities</returns>
        public List<EntityClass> GenerateSpawnList()
        {
            var spawns = new List<EntityClass>();

            if (Spawns == null)
                return spawns;

            foreach (var ent in Spawns)
            {
                int count = Takai.Util.RandomGenerator.Next(ent.Item2.min, ent.Item2.max);
                for (int i = 0; i < count; ++i)
                    spawns.Add(ent.Item1);
            }

            //randomize spawns
            int n = spawns.Count;
            while (n > 1)
            {
                int k = (Takai.Util.RandomGenerator.Next(0, n) % n);
                --n;
                var value = spawns[k];
                spawns[k] = spawns[n];
                spawns[n] = value;
            }

            return spawns;
        }

        public override WeaponInstance Instantiate()
        {
            return new SpawnerInstance(this);
        }
    }

    class SpawnerInstance : WeaponInstance
    {
        public new SpawnerClass Class
        {
            get => (SpawnerClass)base.Class;
            set => base.Class = value;
        }

        /// <summary>
        /// The actual list of entities to spawn
        /// </summary>
        public Queue<EntityClass> SpawnQueue { get; set; }

        public SpawnerInstance() { }
        public SpawnerInstance(SpawnerClass @class)
            : base(@class)
        {
            SpawnQueue = new Queue<EntityClass>(Class.GenerateSpawnList());
        }

        public override bool IsDepleted()
        {
            return SpawnQueue == null || SpawnQueue.Count <= 0; //todo: handle deserializing spawn queue better
        }

        protected override void OnDischarge()
        {
            var next = SpawnQueue.Dequeue();
            Actor.Map.Spawn(next, Actor.Position + Actor.Forward * (Actor.Radius + 10), Actor.Forward, Actor. Forward * 100);
            base.OnDischarge();
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
