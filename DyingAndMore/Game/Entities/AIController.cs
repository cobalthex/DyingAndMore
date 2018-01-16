using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities
{
    //behavior constraints (cant run while dead/etc)

    public enum BehaviorMask
    {
        Unknown,
        Targeting,
        Movement,
        Weapons,

        _Count_
        //Exclusive option?
        //bitmask?
    }

    public enum BehaviorPriority : int
    {
        Never = int.MinValue,
        Low = -100,
        Normal = 0,
        High = 100,
    }

    public abstract class Behavior
    {
        [Takai.Data.Serializer.Ignored]
        public AIController AI { get; set; }

        /// <summary>
        /// Only one of each behavior mask can be executed at a time
        /// </summary>
        [Takai.Data.Serializer.Ignored]
        public abstract BehaviorMask Mask { get; }

        /// <summary>
        /// Calculate the priority of this action. Return <see cref="BehaviorPriority.Never"/> to disregard
        /// The behavior with the highest priority will be chosen
        /// </summary>
        /// <returns>The priority factor of this action</returns>
        public abstract BehaviorPriority CalculatePriority();
        public abstract void Think(TimeSpan deltaTime);
    }

    public class AIController : Controller
    {
        public List<Behavior> Behaviors { get; set; }

        List<(BehaviorPriority priority, Behavior behavior)>[] behaviorCosts
            = new List<(BehaviorPriority priority, Behavior behavior)>[(int)BehaviorMask._Count_];

        Random random = new Random();

        public ActorInstance Target
        {
            get => _target;
            set
            {
                _target = value;
                TargetTime = _target?.Map?.ElapsedTime ?? TimeSpan.Zero;
            }
        }
        private ActorInstance _target;
        public TimeSpan TargetTime { get; set; }

        public AIController()
        {
            for (int i = 0; i < behaviorCosts.Length; ++i)
                behaviorCosts[i] = new List<(BehaviorPriority priority, Behavior behavior)>();
        }

        public override void Think(TimeSpan deltaTime)
        {
            foreach (var behavior in Behaviors)
            {
                var mask = (int)behavior.Mask;

                behavior.AI = this;

                var priority = behavior.CalculatePriority();
                if (priority == BehaviorPriority.Never)
                    continue;

                if (behaviorCosts[mask].Count > 0)
                {
                    if (priority < behaviorCosts[mask][0].priority)
                        continue;

                    if (priority > behaviorCosts[mask][0].priority)
                        behaviorCosts[mask].Clear();
                }
                behaviorCosts[mask].Add((priority, behavior));
            }

            foreach (var list in behaviorCosts)
            {
                if (list.Count > 0)
                {
                    var choice = random.Next(0, list.Count);
                    list[choice].behavior.Think(deltaTime);
                    list.Clear();
                }
            }
        }
    }

    class TargetEnemyBehavior : Behavior
    {
        public override BehaviorMask Mask => BehaviorMask.Targeting;

        public float SightDistance { get; set; } = 300;

        public override BehaviorPriority CalculatePriority()
        {
            return BehaviorPriority.Normal;
        }

        public override void Think(TimeSpan deltaTime)
        {
            if (AI.Target == null)
            {
                var ents = AI.Actor.Map.FindEntities(AI.Actor.Position, SightDistance);
                var possibles = new List<ActorInstance>();
                foreach (var ent in ents)
                {
                    if (ent != AI.Actor &&
                        ent is ActorInstance actor &&
                        !actor.IsAlliedWith(AI.Actor.Faction))
                        possibles.Add(actor);
                }
                possibles.Sort(delegate (ActorInstance a, ActorInstance b)
                {
                    var afw = Vector2.Dot(a.Forward, AI.Actor.Forward);
                    var bfw = Vector2.Dot(b.Forward, AI.Actor.Forward);
                    return (afw == bfw ? 0 : (int)Math.Ceiling(bfw - afw));
                });

                if (possibles.Count > 0)
                {
                    AI.Target = possibles[0];
                    AI.Target.OutlineColor = Color.Red;
                }
            }

            //match targets by threat

            //track shots for 5 seconds, most damage done new target

            //enemies close by, if have melee weapon, higher threat

            else if (Vector2.DistanceSquared(AI.Target.Position, AI.Actor.Position) >= SightDistance * SightDistance ||
                AI.Actor.IsFacing(AI.Actor.Position))
            {
                AI.Target.OutlineColor = Color.Transparent;
                AI.Target = null;
            }
        }
    }

    //panic behavior (shiver momentarily)
    //flee behavior

    /// <summary>
    /// Commit suicide when close to an enemy (actor of a different faction)
    /// </summary>
    class KamikazeBehavior : Behavior
    {
        public override BehaviorMask Mask => BehaviorMask.Unknown;

        public float Radius { get; set; } = 0;

        /// <summary>
        /// An optional effect to play when committing seppeku
        /// </summary>
        public Takai.Game.EffectsClass Effect { get; set; }

        public override BehaviorPriority CalculatePriority()
        {
            var proximity = AI.Actor.Map.FindEntities(AI.Actor.Position, Radius);
            foreach (var proxy in proximity)
            {
                if (proxy is ActorInstance proxactor
                    && !proxactor.IsAlliedWith(AI.Actor.Faction)
                    && Vector2.DistanceSquared(proxy.Position, AI.Actor.Position) <= Radius * Radius)
                    return BehaviorPriority.Normal;
            }
            return BehaviorPriority.Never;
        }

        public override void Think(TimeSpan deltaTime)
        {
            AI.Actor.IsAlive = false; //should this be handled by the effect?
            if (Effect != null)
                AI.Actor.Map.Spawn(Effect.Create(AI.Actor));
        }
    }

    class ChargePlayerBehavior : Behavior
    {
        public override BehaviorMask Mask => BehaviorMask.Movement;

        public override BehaviorPriority CalculatePriority()
        {
            return BehaviorPriority.Normal;
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
            var pos = (AI.Actor.Position / AI.Actor.Map.Class.TileSize).ToPoint();
            foreach (var dir in NavigationDirections)
            {
                var target = pos + dir;
                if (!AI.Actor.Map.Class.TileBounds.Contains(target))
                    continue;
                var h = AI.Actor.Map.Class.PathInfo[target.Y, target.X].heuristic;
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
            AI.Actor.Forward = next.ToVector2();
            AI.Actor.Velocity = AI.Actor.Forward * 300; //todo: move force
        }
    }

    class ShootBehavior : Behavior
    {
        public override BehaviorMask Mask => BehaviorMask.Weapons;

        public override BehaviorPriority CalculatePriority()
        {
            if (AI.Actor.Weapon == null)
                return BehaviorPriority.Never;

            return BehaviorPriority.Normal;
        }

        public override void Think(TimeSpan deltaTime)
        {
            AI.Actor.Weapon.TryFire();
            //AI.Actor.Weapon.Reset(); //todo: better place
        }
    }
}
