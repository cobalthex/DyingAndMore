using System;
using System.CodeDom;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Tasks
{


    /// <summary>
    /// Move in a straight line (or there abouts) towards the target
    /// </summary>
    public struct MoveToTarget : ITask
    {
        /// <summary>
        /// Maximum distance required for this task to complete.
        /// Offset from distance between two actors combined radii
        /// </summary>
        public float distance;

        /// <summary>
        /// Continue to follow target
        /// </summary>
        public bool permanent;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return TaskResult.Failure;

            var interDist = ai.Target.RadiusSq + ai.Actor.RadiusSq;
            if (Vector2.DistanceSquared(ai.Target.Position, ai.Actor.Position)
                <= (distance * distance) + interDist)
                return permanent ? TaskResult.Continue : TaskResult.Success;
            //must be able to see target?

            var dir = Vector2.Normalize(ai.Target.Position - ai.Actor.Position);
            ai.Actor.TurnTowards(dir, deltaTime);
            ai.Actor.Accelerate(ai.Actor.Forward);

            return TaskResult.Continue;
        }
    }

    /// <summary>
    /// Determine when a gradient navigation task is successful
    /// Compares current nav value to target value
    /// </summary>
    public enum NavGradientSuccessCondition
    {
        LessOrEqual,
        Equal,
        GreaterOreEqual
    }

    /// <summary>
    /// Navigate towards a particular value in the flow field gradient
    /// </summary>
    public struct NavigateGradient : ITask
    {
        public uint targetValue;
        public NavGradientSuccessCondition successCondition;

        int lastDirection;

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

        //todo: move
        static internal bool NavigateToPoint(uint target, TimeSpan deltaTime, ActorInstance actor, ref int lastDirection)
        {
            var testPos = actor.WorldPosition + (actor.Radius + 1) * actor.Forward;

            var cur = actor.Map.PathInfoAt(testPos).heuristic;
            var best = target < cur ? uint.MaxValue : 0;

            var possible = Point.Zero;

            var pos = (testPos / actor.Map.Class.TileSize).ToPoint();
            //calculate best direction to move
            //start at last direction moved towards
            for (int i = 0, n = lastDirection; 
                i < NavigationDirections.Length; 
                ++i, n = (n + 1) % NavigationDirections.Length)
            {
                var dir = NavigationDirections[n];
                var next = pos + dir;
                if (!actor.Map.Class.TileBounds.Contains(next))
                    continue;

                //note: doesn't really work with cur < target due to how heuristic is generated
                //maybe use sdf edge detection to work around this?
                var h = actor.Map.PathInfo[next.Y, next.X].heuristic;
                if ((cur > target && h <= best) ||
                    (cur < target && h >= best))
                {
                    possible = dir;
                    best = h;
                    lastDirection = n;
                }
                //else if (h == best)
                //    possible = dir; //can store this in a list and pick randomly among possible directions
            }

            if (possible != Point.Zero)
            {
                actor.TurnTowards(Vector2.Normalize(possible.ToVector2()), deltaTime);
                actor.Accelerate(actor.Forward);
                return true;
            }
            return false;
        }

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            var cur = ai.Actor.Map.PathInfoAt(ai.Actor.WorldPosition).heuristic;
            switch (successCondition)
            {
                case NavGradientSuccessCondition.Equal:
                    if (Math.Abs(cur - targetValue) <= 1)
                        return TaskResult.Success;
                    break;

                case NavGradientSuccessCondition.LessOrEqual:
                    if (cur <= targetValue + 1)
                        return TaskResult.Success;
                    break;

                case NavGradientSuccessCondition.GreaterOreEqual:
                    if (cur >= targetValue - 1)
                        return TaskResult.Success;
                    break;
            }

            if (!NavigateToPoint(targetValue, deltaTime, ai.Actor, ref lastDirection))
                return TaskResult.Success; //hack
            return TaskResult.Continue;
        }
    }

    /// <summary>
    /// Navigate to a target using the map's heuristic
    /// </summary>
    public struct NavigateToTarget : ITask
    {
        public bool permanent;
        int lastDirection;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null) //fail on target death?
                return TaskResult.Failure;

            //todo: sight range

            var cur = ai.Actor.Map.PathInfoAt(ai.Actor.WorldPosition).heuristic;
            var target = ai.Actor.Map.PathInfoAt(ai.Target.WorldPosition).heuristic;
            if (Math.Abs(cur - target) <= 1)
                return permanent ? TaskResult.Continue : TaskResult.Success;

            NavigateGradient.NavigateToPoint(target, deltaTime, ai.Actor, ref lastDirection);
            return TaskResult.Continue;
        }
    }

    //navigate behind entity (for assassination)
    //navigate to entity (powerup/weapon/etc)
    //teleport
    //wander
    //follow path
}
