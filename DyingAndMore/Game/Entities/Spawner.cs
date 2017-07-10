using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    class Squad { }

    class Spawner : EntityClass
    {
        protected static System.Random randGen = new System.Random();

        /// <summary>
        /// The entities to spawn from this spawner and a range of each to spawn
        /// </summary>
        public List<Tuple<EntityClass, Range<int>>> Template { get; set; }

        /// <summary>
        /// The actual list of entities to spawn
        /// </summary>
        public Queue<EntityClass> SpawnQueue { get; protected set; }

        /// <summary>
        /// how close an enemy (different faction) has to be before spawning
        /// </summary>
        public float SearchRadius { get; set; } = 200;

        /// <summary>
        /// Spawn delay between entities. Weighted based on how quickly entities are killed
        /// </summary>
        public Range<TimeSpan> SpawnDelay { get; set; } =
            new Range<TimeSpan>(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(2));

        protected TimeSpan lastSpawn = TimeSpan.Zero;

        public Spawner()
        {
        }

        /// <summary>
        /// Generate a list of entities based on the templates provided in <see cref="Entities"/>. Does not create clones
        /// </summary>
        /// <returns>The list of (references to original) entities</returns>
        public List<EntityClass> GenerateSpawnList()
        {
            var spawns = new List<EntityClass>();

            foreach (var ent in Template)
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

        public override void OnSpawn()
        {
            SpawnQueue = new Queue<EntityClass>(GenerateSpawnList());
        }

        public override void Think(TimeSpan DeltaTime)
        {
            if (State.Is(EntStateId.Idle) && Map.ElapsedTime > lastSpawn + SpawnDelay.max) //todo
            {
                var radii = Map.FindEntities(Position, SearchRadius);
                foreach (var ent in radii)
                {
                    if (ent is Actor actor && (actor.Faction & Faction) == Factions.None)
                    {
                        State.Transition(EntStateId.Idle, EntStateId.Active);
                        break;
                    }
                }
            }

            if (State.Is(EntStateId.Active) && State.States[EntStateId.Active].HasFinished())
            {
                SpawnNext();
                State.Transition(EntStateId.Active, EntStateId.Idle);

                if (SpawnQueue.Count < 1)
                    State.Transition(EntStateId.Idle, EntStateId.Inactive);
            }

            base.Think(DeltaTime);
        }

        /// <summary>
        /// Spawn a clone of the next entity in the list. Does nothing if there are none remaining or if this spawner is dead
        /// </summary>
        /// <returns>The entity spawned</returns>
        public EntityClass SpawnNext()
        {
            if (Map == null || SpawnQueue.Count < 1)
                return null;

            lastSpawn = Map.ElapsedTime;
            var next = SpawnQueue.Dequeue();
            return Map.Spawn(next, Position + Direction * (Radius + next.Radius + 5), Direction, Direction * 100);
        }
    }
}
