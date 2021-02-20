using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DyingAndMore.Game.Entities.Locomotion
{
    public struct FleeFromTarget : ILocomotor
    {
        private int lastDirection;

        public LocomotionResult Move(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return LocomotionResult.Finished; //no ghosts allowed

            //determine if cornered (check sdf to sides & behind), move into senses?

            var cur = ai.Actor.Map.PathInfoAt(ai.Actor.WorldPosition).heuristic;
            uint target = 50;
            DyingAndMoreGame.DebugDisplay("cur", cur);
            DyingAndMoreGame.DebugDisplay("tgt", target);
            if (cur >= target)
                return LocomotionResult.Finished;

            NavigateGradient.NavigateToPoint(target, deltaTime, ai.Actor, ref lastDirection);
            return LocomotionResult.Continue;

        }
    }
}
