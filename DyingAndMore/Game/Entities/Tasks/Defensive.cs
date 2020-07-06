using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Tasks
{
    public struct ProvideCover : ITask
    {

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            return TaskResult.Success;
        }
    }

    public struct FleeFromTarget : ITask
    {
        int lastDirection;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return TaskResult.Success; //no ghosts allowed

            //determine if cornered

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

    //Flee

    //Move to cover
}
