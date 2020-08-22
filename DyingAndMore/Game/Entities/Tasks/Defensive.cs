using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Tasks
{
    public class DefensiveTaskAttribute : Attribute { }

    [DefensiveTask]
    public struct ProvideCover : ITask
    {

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            return TaskResult.Success;
        }
    }

    /// <summary>
    /// Move in opposite direction
    /// </summary>
    [DefensiveTask]
    public struct FleeFromTarget : ITask
    {
        int lastDirection;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return TaskResult.Success; //no ghosts allowed

            //determine if cornered (check sdf to sides & behind), move into senses?

            var cur = ai.Actor.Map.PathInfoAt(ai.Actor.WorldPosition).heuristic;
            uint target = 50;
            DyingAndMoreGame.DebugDisplay("cur", cur);
            DyingAndMoreGame.DebugDisplay("tgt", target);
            if (cur >= target)
                return TaskResult.Success;

            NavigateGradient.NavigateToPoint(target, deltaTime, ai.Actor, ref lastDirection);
            return TaskResult.Continue;

        }
    }

    /// <summary>
    /// Find a point out of sight of the target
    /// </summary>
    [DefensiveTask]
    public struct HideFromTarget : ITask
    {
        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return TaskResult.Success; //no ghosts allowed

            //find closest point on sdf blocking view from target to self
            //limited range (slightly past self?)


            //run perpendicular to enemy then turn away if obstacle
            // quit if target cannot see

            return TaskResult.Continue;
        }
    }

    //move in specific/random direction (less than 90deg off forward?)
}
