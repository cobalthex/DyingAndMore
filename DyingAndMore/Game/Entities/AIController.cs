using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities
{
    //behavior constraints (cant run while dead/etc)

    abstract class Behavior
    {
        [Takai.Data.Serializer.Ignored]
        public ActorInstance Actor { get; set; }

        /// <summary>
        /// Calculate the priority of this action. Return <see cref="int.MinValue"/> to disregard
        /// The behavior with the highest priority will be chosen
        /// </summary>
        /// <returns>The priority factor of this action</returns>
        public abstract int CalculatePriority();
        public abstract void Think(TimeSpan deltaTime);
    }

    class AIController : Controller
    {
        public List<Behavior> Behaviors { get; set; }

        List<(int cost, Behavior behavior)> behaviorCosts
            = new List<(int, Behavior)>();

        Random random = new Random();

        public override void Think(TimeSpan deltaTime)
        {
            behaviorCosts.Clear();
            foreach (var behavior in Behaviors)
            {
                behavior.Actor = Actor;

                var cost = behavior.CalculatePriority();
                if (cost == int.MinValue)
                    continue;

                if (behaviorCosts.Count > 0)
                {
                    if (cost < behaviorCosts[0].cost)
                        continue;

                    if (cost > behaviorCosts[0].cost)
                        behaviorCosts.Clear();
                }
                behaviorCosts.Add((cost, behavior));
            }

            if (behaviorCosts.Count > 0)
            {
                var choice = random.Next(0, behaviorCosts.Count);
                behaviorCosts[choice].behavior.Think(deltaTime);
            }
        }
    }



    /// <summary>
    /// Commit suicide when close to an enemy (actor of a different faction)
    /// </summary>
    class KamikazeBehavior : Behavior
    {
        public float Radius { get; set; } = 0;

        /// <summary>
        /// An optional effect to play when committing seppeku
        /// </summary>
        public Takai.Game.EffectsClass Effect { get; set; }

        public override int CalculatePriority()
        {
            var proximity = Actor.Map.FindEntities(Actor.Position, Radius);
            foreach (var proxy in proximity)
            {
                if (proxy is ActorInstance proxactor
                    && !proxactor.IsAlliedWith(Actor.Faction)
                    && Vector2.DistanceSquared(proxy.Position, Actor.Position) <= Radius * Radius)
                    return 10;
            }
            return int.MinValue;
        }

        public override void Think(TimeSpan deltaTime)
        {
            Actor.IsAlive = false; //should this be handled by the effect?
            if (Effect != null)
                Actor.Map.Spawn(Effect.Create(Actor));
        }
    }

    class ChargeBehavior : Behavior
    {
        public override int CalculatePriority()
        {
            return 1;
        }

        public static readonly Point[] NavigationDirections =
        {
            new Point(-1, -1),
            new Point( 0, -1),
            new Point( 1, -1),
            new Point(-1,  0),
            new Point( 1,  0),
            new Point(-1,  1),
            new Point( 0,  1),
            new Point( 1,  1),
        };

        List<Point> minimums = new List<Point>(8);
        public override void Think(TimeSpan deltaTime)
        {
            var min = uint.MaxValue;
            minimums.Clear();
            var pos = (Actor.Position / Actor.Map.Class.TileSize).ToPoint();
            foreach (var dir in NavigationDirections)
            {
                var target = pos + dir;
                if (!Actor.Map.Class.TileBounds.Contains(target))
                    continue;
                var h = Actor.Map.Class.PathInfo[target.Y, target.X].heuristic;
                if (h < min)
                {
                    minimums.Clear();
                    minimums.Add(dir);
                    min = h;
                }
                else if (h == min)
                    minimums.Add(dir);
            }

            var next = minimums[0];
            Actor.Forward = next.ToVector2();
            Actor.Velocity = Actor.Forward * 300; //todo: move force
        }
    }

}
