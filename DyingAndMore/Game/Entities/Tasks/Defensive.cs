using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Tasks
{
    public class DefensiveTaskAttribute : Attribute { }

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

            throw new NotImplementedException();
            return TaskResult.Continue;
        }
    }

    //move in specific/random direction (less than 90deg off forward?)
}
