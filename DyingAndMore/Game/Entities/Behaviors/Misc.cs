using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Behaviors
{


    class TargetEnemyBehavior : Behavior
    {
        //break out into individual targets that get added to the behavior list

        public override BehaviorMask Mask => BehaviorMask.Targeting;

        public float SightDistance { get; set; } = 100000;

        public override BehaviorPriority CalculatePriority()
        {
            return BehaviorPriority.Low;
        }

        //sound+hearing

        public override void Think(TimeSpan deltaTime)
        {
            if (AI.Target == null || !AI.Target.IsAlive || AI.Target.Map != AI.Actor.Map)
            {
                var ents = AI.Actor.Map.FindEntitiesInRegion(AI.Actor.Position, SightDistance);
                var possibles = new List<ActorInstance>();
                foreach (var ent in ents)
                {
                    if (ent != AI.Actor &&
                        ent is ActorInstance actor &&
                        !actor.IsAlliedWith(AI.Actor.Factions) &&
                        AI.Actor.IsFacing(actor.Position))
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
                    AI.SetNextTarget(possibles[0]);
                    //possibles[0].OutlineColor = Color.Red;
                }
                else
                    AI.SetNextTarget(null);
            }

            //match targets by threat

            //track shots for 5 seconds, most damage done new target

            //enemies close by, if have melee weapon, higher threat

            else if (Vector2.DistanceSquared(AI.Target.Position, AI.Actor.Position) >= SightDistance * SightDistance ||
                AI.Actor.IsFacing(AI.Actor.Position))
            {
                //AI.Target.OutlineColor = Color.Transparent;
                AI.SetNextTarget(null);
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
        public override BehaviorMask Mask => BehaviorMask.Weapons;
        public override BehaviorFilters Filter => BehaviorFilters.RequiresTarget;

        public float Radius { get; set; } = 0;

        /// <summary>
        /// An optional effect to play when committing seppeku
        /// </summary>
        public Takai.Game.EffectsClass Effect { get; set; }

        public override BehaviorPriority CalculatePriority()
        {
            var distance = Vector2.DistanceSquared(AI.Actor.Position, AI.Target.Position);
            if (distance < Radius * Radius)
                return BehaviorPriority.High;
            return BehaviorPriority.Never;
        }

        public override void Think(TimeSpan deltaTime)
        {
            AI.Actor.Kill(); //todo: should this be handled by the effect?
            if (Effect != null)
                AI.Actor.Map.Spawn(Effect.Instantiate(AI.Actor));
        }
    }

    /// <summary>
    /// Similar behavior to <see cref="KamikazeBehavior"/> except relying on the flow map
    /// </summary>
    class FlowKamikazeBehavior : Behavior
    {
        public override BehaviorMask Mask => BehaviorMask.Weapons;
        public override BehaviorFilters Filter => BehaviorFilters.None;

        /// <summary>
        /// maximum flow map value to find to suicide
        /// </summary>
        public int Bias { get; set; } = 1;

        /// <summary>
        /// An optional effect to play when committing seppeku
        /// </summary>
        public Takai.Game.EffectsClass Effect { get; set; }

        public override BehaviorPriority CalculatePriority()
        {
            var flow = AI.Actor.Map.PathInfoAt(AI.Actor.Position);
            if (flow.heuristic <= Bias)
                return BehaviorPriority.High;
            return BehaviorPriority.Never;
        }

        public override void Think(TimeSpan deltaTime)
        {
            AI.Actor.Kill(); //todo: should this be handled by the effect?
            if (Effect != null)
                AI.Actor.Map.Spawn(Effect.Instantiate(AI.Actor));
        }
    }

    /// <summary>
    /// Move along the flow map towards one of the <see cref="Takai.Game.MapInstance.PathOrigins"/>
    /// </summary>
    class FlowSeekBehavior : Behavior
    {
        //stopping distance

        public override BehaviorMask Mask => BehaviorMask.Movement;

        public override BehaviorPriority CalculatePriority()
        {
            var curPI = AI.Actor.Map.PathInfoAt(AI.Actor.Position);
            if (curPI.heuristic * AI.Actor.Map.Class.TileSize > 10000) //todo: sight range per actor
                return BehaviorPriority.Never;
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
                var h = AI.Actor.Map.PathInfo[target.Y, target.X].heuristic;
                if (h < min)
                {
                    minimums.Clear();
                    minimums.Add(dir);
                    min = h;
                }
                else if (h == min)
                    minimums.Add(dir);
            }

            var next = Vector2.Normalize(minimums[0].ToVector2());

            AI.Actor.Forward = Vector2.Lerp(AI.Actor.Forward, next, MathHelper.PiOver2 * (float)deltaTime.TotalSeconds); //factor in speed (tighter turns over speed)
            AI.Actor.Forward.Normalize();

            //AI.Actor.Forward = next.ToVector2();
            AI.Actor.Accelerate(AI.Actor.Forward);
        }
    }

    class ShootBehavior : Behavior
    {
        public override BehaviorMask Mask => BehaviorMask.Weapons;

        public override BehaviorFilters Filter => BehaviorFilters.RequiresTarget;

        public override BehaviorPriority CalculatePriority()
        {
            if (AI.Actor.Weapon == null || !AI.Actor.IsFacing(AI.Target.Position))
                return BehaviorPriority.Never;

            return BehaviorPriority.Normal;
        }

        public override void Think(TimeSpan deltaTime)
        {
            AI.Actor.Weapon.TryUse();
        }
    }

    /// <summary>
    /// Face a target
    /// </summary>
    class FocusBehavior : Behavior
    {
        public override BehaviorMask Mask => BehaviorMask.Movement;

        public override BehaviorFilters Filter => BehaviorFilters.RequiresTarget;

        /// <summary>
        /// Max turn angle (in radians) per second
        /// </summary>
        public float MaxTurn { get; set; } = MathHelper.ToRadians(800);

        public override BehaviorPriority CalculatePriority()
        {
            if (AI.Actor.Velocity == Vector2.Zero)
                return BehaviorPriority.Normal;
            return BehaviorPriority.Low;
        }

        public override void Think(TimeSpan deltaTime)
        {
            var diff = Vector2.Normalize(AI.Target.Position - AI.Actor.Position);
            var det = Takai.Util.Determinant(AI.Actor.Forward, diff);
            var maxTurn = MathHelper.Clamp(MaxTurn / (AI.Actor.Velocity.Length() / 20), 0, MaxTurn);
            var angle = det * maxTurn;

            //angular velocity and influence (if already turning one direction and some threshhold, continue rotating in the same direction)

            AI.Actor.Forward = Vector2.TransformNormal(AI.Actor.Forward, Matrix.CreateRotationZ(angle * (float)deltaTime.TotalSeconds));
        }
    }

    /// <summary>
    /// Simple test behavior to always shoot at direction facing
    /// </summary>
    class DebugShootBehavior : Behavior
    {
        public override BehaviorMask Mask => BehaviorMask.Weapons;
        public override BehaviorFilters Filter => BehaviorFilters.None;

        public override BehaviorPriority CalculatePriority()
        {
            return BehaviorPriority.Normal;
        }

        public override void Think(TimeSpan deltaTime)
        {
            AI.Actor.Weapon.TryUse();
        }
    }
}
