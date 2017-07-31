using System;
using System.Collections.Generic;
using Takai.Game;

namespace DyingAndMore.Game.Weapons
{
    class SpawnerClass : WeaponClass
    {
        protected static Random randGen = new Random();

        /// <summary>
        /// Spawns are generated from this template. All classes will be spawned with a random number between its range
        /// Spawn order is randomized
        /// </summary>
        public List<Tuple<EntityClass, Range<int>>> Spawns { get; set; }

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
                int count = randGen.Next(ent.Item2.min, ent.Item2.max);
                for (int i = 0; i < count; ++i)
                    spawns.Add(ent.Item1);
            }

            //randomize spawns
            int n = spawns.Count;
            while (n > 1)
            {
                int k = (randGen.Next(0, n) % n);
                --n;
                var value = spawns[k];
                spawns[k] = spawns[n];
                spawns[n] = value;
            }

            return spawns;
        }

        public override WeaponInstance Create()
        {
            return new SpawnerInstance(this);
        }
    }

    class SpawnerInstance : WeaponInstance
    {
        public override WeaponClass Class
        {
            get => base.Class;
            set
            {
                System.Diagnostics.Contracts.Contract.Assert(value == null || value is SpawnerClass);
                base.Class = value;
                _Class = value as SpawnerClass;
            }
        }

        private SpawnerClass _Class;

        /// <summary>
        /// The actual list of entities to spawn
        /// </summary>
        public Queue<EntityClass> SpawnQueue { get; set; }

        public SpawnerInstance() { }
        public SpawnerInstance(SpawnerClass @class)
            : base(@class)
        {
            SpawnQueue = new Queue<EntityClass>(_Class.GenerateSpawnList());
        }

        public override bool IsDepleted()
        {
            return SpawnQueue.Count <= 0;
        }

        public override void Discharge()
        {
            if (IsDepleted())
                return;

            var next = SpawnQueue.Dequeue();
            Actor.Map.Spawn(next, Actor.Position + Actor.Direction * (Actor.Radius + 10), Actor.Direction, Actor. Direction * 100);
            base.Discharge();
        }
    }
}
