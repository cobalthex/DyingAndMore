using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai;

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

            //todo: A*?

            var cur = ai.Actor.Map.PathInfoAt(ai.Actor.WorldPosition).heuristic;
            var target = ai.Actor.Map.PathInfoAt(ai.Target.WorldPosition).heuristic;
            if (Math.Abs(cur - target) <= 1)
                return permanent ? TaskResult.Continue : TaskResult.Success;

            NavigateGradient.NavigateToPoint(target, deltaTime, ai.Actor, ref lastDirection);
            return TaskResult.Continue;
        }
    }

    /// <summary>
    /// Orbit around the target at a specified radius (from the center point).
    /// Automatically moves to face the target 
    /// Optionally face the target as orbiting
    /// </summary>
    public struct OrbitTarget : ITask
    {
        public float radius;
        public bool faceTarget; //as opposed to facing in the direction of travel

        //success condition? (time?)

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return TaskResult.Failure; //no ghosts allowed

            var origin = ai.Target.WorldPosition;

            var diff = origin - ai.Actor.WorldPosition;
            var diffLen = diff.Length();

            Vector2 accelAngle;

            //ai.Actor.Map.DrawCircle(ai.Target.WorldPosition, radius, Color.CornflowerBlue, 2, 4);

            //var diffSpacing = difflen - ai.actor.radius - ai.target.radius :: use below and in a
            if (diffLen > radius)
            {
                var angleToTangent = (float)Math.Asin(radius / diffLen); //angle between position relative to circle and intersecting tangent
                // θ = asin (opp / hyp)
                //if at edge of circle, opp (radius) = hyp (diffLen) so actor should travel along tangent, (triangle has zero area)
                //if farther away, hyp > opp so angle grows meaning the actor must turn farther in to approach the tangent
                //if closer than radius, asin returns imaginary result (NaN in C#)
                //at opp = hyp, will be fwd - 90deg

                var relAngle = Util.Angle(diff); //angle between actor and target (to orient angleToTangent)

                //if fwd is pointing left of diff, move leftwards (clockwise), otherwise ccw
                //det > 0 == cw
                var det = Util.Determinant(ai.Actor.WorldForward, diff);

                var desiredAngle = relAngle - (angleToTangent * Math.Sign(det));
                var desiredDir = Util.Direction(desiredAngle);

                if (faceTarget)
                {
                    ai.Actor.TurnTowards(diff, deltaTime);
                    accelAngle = Vector2.Lerp(ai.Actor.Forward.Ortho(), desiredDir, 0.2f);
                }
                else
                {
                    //todo: might be able to exploit (radius / diffLen) to skip angle calculations

                    var sign = Util.Determinant(ai.Actor.WorldForward, desiredDir);

                    //crude
                    float angle = ActorInstance.TurnSpeed * sign * (float)deltaTime.TotalSeconds;
                    accelAngle = ai.Actor.Forward = Vector2.TransformNormal(ai.Actor.Forward, Matrix.CreateRotationZ(angle));
                }
            }
            else
            {
                if (faceTarget && ai.Actor.Velocity != Vector2.Zero)
                    accelAngle = Vector2.Normalize(ai.Actor.Velocity);
                else
                    accelAngle = ai.Actor.Forward;
            }

            ai.Actor.Accelerate(accelAngle);

            return TaskResult.Continue;
        }
    }

    public struct FollowPath : ITask
    {
        public Takai.Game.VectorCurve path;

        int currentPathIndex;
        List<Point> aStarPath;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (path == null || path.Values.Count < 1)
                return TaskResult.Failure;

            switch (ai.CurrentTaskState)
            {
                case 0:
                    {
                        aStarPath = ai.Actor.Map.AStarBuildPath(ai.Actor.WorldPosition, path.Evaluate(0));
                        ++ai.CurrentTaskState;
                        currentPathIndex = 0;
                        goto case 1;
                    }
                case 1:
                    {
                        if (currentPathIndex >= aStarPath.Count)
                        {
                            ++ai.CurrentTaskState;
                            currentPathIndex = 0;
                            goto case 2;
                        }

                        var ap = (ai.Actor.WorldPosition / ai.Actor.Map.Class.TileSize).ToPoint();
                        if (ap == aStarPath[currentPathIndex])
                            ++currentPathIndex;
                        else
                        {
                            var ts = ai.Actor.Map.Class.TileSize;
                            var target = aStarPath[currentPathIndex].ToVector2() * ts + new Vector2(ts / 2);
                            var diff = Vector2.Normalize(target - ai.Actor.WorldPosition);

                            ai.Actor.Map.DrawX(target, 10, Color.Gold);

                            ai.Actor.TurnTowards(diff, deltaTime);
                            ai.Actor.Accelerate(ai.Actor.Forward * Math.Max(0, Vector2.Dot(ai.Actor.Forward, diff)));
                        }
                    }
                    break;
                case 2:
                    {
                        if (currentPathIndex >= path.Count)
                            return TaskResult.Success;

                        var target = path.Values[currentPathIndex].value; //todo: actually follow path
                        var diff = target - ai.Actor.WorldPosition;
                        if (diff.LengthSquared() <= ai.Actor.RadiusSq * 2)
                            ++currentPathIndex;

                        //check direction and approximate location

                        ai.Actor.Map.DrawX(target, 10, Color.Gold);

                        diff.Normalize();
                        ai.Actor.TurnTowards(diff, deltaTime);
                        ai.Actor.Accelerate(ai.Actor.Forward * Math.Max(0, Vector2.Dot(ai.Actor.WorldForward, diff)));
                    }
                    break;
            }

            return TaskResult.Continue;
        }
    }


    //navigate behind entity (for assassination)
    //navigate to entity (powerup/weapon/etc)
    //teleport
    //wander
    //follow path
}
