using Microsoft.Xna.Framework;
using System;

namespace DyingAndMore.Game.Entities.Locomotion
{
    class MoveToTarget : ILocomotor
    {
        /// <summary>
        /// minimum distance required for this task to complete.
        /// Offset from distance between two actors combined radii
        /// </summary>
        public float distance;

        /// <summary>
        /// Continue to follow target
        /// </summary>
        public bool permanent;

        public LocomotionResult Move(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return LocomotionResult.Finished;

            var interDist = ai.Target.RadiusSq + ai.Actor.RadiusSq;
            if (Vector2.DistanceSquared(ai.Target.Position, ai.Actor.Position)
                <= (distance * distance) + interDist)
                return permanent ? LocomotionResult.Continue : LocomotionResult.Finished;
            //must be able to see target?

            var dir = Vector2.Normalize(ai.Target.Position - ai.Actor.Position);
            ai.Actor.TurnTowards(dir, deltaTime);
            ai.Actor.Accelerate(ai.Actor.Forward);

            return LocomotionResult.Continue;
        }
    }
}
