using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    class Squad { }
    class Spawner : Actor
    {
        protected static System.Random randGen = new System.Random();

        /// <summary>
        /// The entities to spawn from this spawner and a range of each to spawn
        /// </summary>
        public List<Tuple<Entity, Range<int>>> Template { get; set; }

        /// <summary>
        /// The actual list of entities to spawn
        /// </summary>
        public Queue<Entity> SpawnQueue { get; protected set; }

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
        public List<Entity> GenerateSpawnList()
        {
            var spawns = new List<Entity>();

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
            SpawnQueue = new Queue<Entity>(GenerateSpawnList());
        }

        public override void Think(TimeSpan DeltaTime)
        {
            if (SpawnQueue.Count < 1)
                State.Transition(EntStateKey.Inactive);
            else if (State.Is(EntStateKey.Idle) && Map.ElapsedTime - lastSpawn > SpawnDelay.min) //todo
            {
                var radii = Map.FindEntities(Position, SearchRadius);
                foreach (var ent in radii)
                {
                    if (ent is Actor actor && (actor.Faction & Faction) == Factions.None)
                    {
                        State.Transition(EntStateKey.Idle, EntStateKey.Active);
                        lastSpawn = Map.ElapsedTime;
                        break;
                    }
                }
            }

            if (State.Is(EntStateKey.Active) && State.States[EntStateKey.Active].HasFinished())
            {
                SpawnNext();
                lastSpawn = Map.ElapsedTime;
            }

            base.Think(DeltaTime);
        }

        /// <summary>
        /// Spawn a clone of the next entity in the list. Does nothing if there are none remaining or if this spawner is dead
        /// </summary>
        /// <returns>The entity spawned</returns>
        public Entity SpawnNext()
        {
            if (Map == null || SpawnQueue.Count < 1)
                return null;

            var next = SpawnQueue.Dequeue();
            return Map.Spawn(next, Position + Direction * (Radius + next.Radius + 5), Direction, Direction * 3);
        }
    }
}
