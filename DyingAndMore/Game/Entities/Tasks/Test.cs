﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Behaviors
{
    /*
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
                var ents = AI.Actor.Map.FindEntitiesInRegion(AI.Actor.WorldPosition, SightDistance);
                var possibles = new List<ActorInstance>();
                foreach (var ent in ents)
                {
                    if (ent != AI.Actor &&
                        ent is ActorInstance actor &&
                        !actor.IsAlliedWith(AI.Actor.Factions) &&
                        AI.Actor.IsFacing(actor.WorldPosition))
                        possibles.Add(actor);
                }
                possibles.Sort(delegate (ActorInstance a, ActorInstance b)
                {
                    var afw = Vector2.Dot(a.WorldForward, AI.Actor.WorldForward);
                    var bfw = Vector2.Dot(b.WorldForward, AI.Actor.WorldForward);
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

            else if (Vector2.DistanceSquared(AI.Target.WorldPosition, AI.Actor.WorldPosition) >= SightDistance * SightDistance ||
                AI.Actor.IsFacing(AI.Actor.WorldPosition))
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

        public float Radius { get; set; } = 0;

        /// <summary>
        /// An optional effect to play when committing seppeku
        /// </summary>
        public Takai.Game.EffectsClass Effect { get; set; }

        public override BehaviorPriority CalculatePriority()
        {
            var distance = Vector2.DistanceSquared(AI.Actor.WorldPosition, AI.Target.WorldPosition);
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
            var flow = AI.Actor.Map.PathInfoAt(AI.Actor.WorldPosition);
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
    /// Move along the flow map towards one of the <see cref="Takai.Game.MapBaseInstance.PathOrigins"/>
    /// </summary>
    class FlowSeekBehavior : Behavior
    {
        //stopping distance

        public override BehaviorMask Mask => BehaviorMask.Movement;

        public override BehaviorPriority CalculatePriority()
        {
            var curPI = AI.Actor.Map.PathInfoAt(AI.Actor.WorldPosition);
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

        List<Point> possibles = new List<Point>(8);
        public override void Think(TimeSpan deltaTime)
        {
            var min = uint.MaxValue;
            possibles.Clear();
            var pos = (AI.Actor.WorldPosition / AI.Actor.Map.Class.TileSize).ToPoint();
            foreach (var dir in NavigationDirections)
            {
                var target = pos + dir;
                if (!AI.Actor.Map.Class.TileBounds.Contains(target))
                    continue;
                var h = AI.Actor.Map.PathInfo[target.Y, target.X].heuristic;
                if (h < min)
                {
                    possibles.Clear();
                    possibles.Add(dir);
                    min = h;
                }
                else if (h == min)
                    possibles.Add(dir);
            }

            //todo: support moving to higher ground

            var next = Vector2.Normalize(possibles[0].ToVector2());
            AI.Actor.TurnTowards(next, deltaTime);
            AI.Actor.Accelerate(AI.Actor.Forward);
        }
    }

    class ShootBehavior : Behavior
    {
        public override BehaviorMask Mask => BehaviorMask.Weapons;

        public override BehaviorPriority CalculatePriority()
        {
            if (AI.Actor.Weapon == null || !AI.Actor.IsFacing(AI.Target.WorldPosition))
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
            var diff = Vector2.Normalize(AI.Target.WorldPosition - AI.Actor.WorldPosition);
            var det = Takai.Util.Determinant(AI.Actor.WorldForward, diff);
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

        public override BehaviorPriority CalculatePriority()
        {
            return BehaviorPriority.Normal;
        }

        public override void Think(TimeSpan deltaTime)
        {
            AI.Actor.Weapon.TryUse();
        }
    }
    */
}

// A*/similar navigation