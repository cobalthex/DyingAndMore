using Microsoft.Xna.Framework;
using System;

namespace DyingAndMore.Game.Entities.Locomotion
{
    /// <summary>
    /// Determine when a gradient navigation task is successful
    /// Compares current nav value to target value
    /// </summary>
    public enum ComparisonMethod
    {
        LessOrEqual,
        Equal,
        GreaterOreEqual
    }

    public struct NavigateGradient : ILocomotor
    {
        public uint targetValue;
        public ComparisonMethod successCondition;

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

        public LocomotionResult Move(TimeSpan deltaTime, AIController ai)
        {
            var cur = ai.Actor.Map.PathInfoAt(ai.Actor.WorldPosition).heuristic;
            switch (successCondition)
            {
                case ComparisonMethod.Equal:
                    if (Math.Abs(cur - targetValue) <= 1)
                        return LocomotionResult.Finished;
                    break;

                case ComparisonMethod.LessOrEqual:
                    if (cur <= targetValue + 1)
                        return LocomotionResult.Finished;
                    break;

                case ComparisonMethod.GreaterOreEqual:
                    if (cur >= targetValue - 1)
                        return LocomotionResult.Finished;
                    break;
            }

            if (!NavigateToPoint(targetValue, deltaTime, ai.Actor, ref lastDirection))
                return LocomotionResult.Finished; //hack

            return LocomotionResult.Continue;
        }
    }
}
