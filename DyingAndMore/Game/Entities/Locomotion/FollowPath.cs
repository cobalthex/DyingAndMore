using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Tasks
{
    public struct FollowPath : ILocomotor
    {
        public Takai.Game.VectorCurve path;

        int currentPathIndex;
        List<Point> aStarPath;

        public LocomotionResult Move(TimeSpan deltaTime, AIController ai)
        {
            if (path == null || path.Values.Count < 1)
                return LocomotionResult.Finished;

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
                            return LocomotionResult.Finished;

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

            return LocomotionResult.Continue;
        }
    }
}
