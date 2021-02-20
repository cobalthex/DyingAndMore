using System;
namespace DyingAndMore.Game.Entities.Locomotion
{
    public struct NavigateToTarget : ILocomotor
    {
        public bool permanent;
        int lastDirection;

        public LocomotionResult Move(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null) //fail on target death?
                return LocomotionResult.Finished;

            //todo: sight range

            //todo: A*?

            var cur = ai.Actor.Map.PathInfoAt(ai.Actor.WorldPosition).heuristic;
            var target = ai.Actor.Map.PathInfoAt(ai.Target.WorldPosition).heuristic;
            if (Math.Abs(cur - target) <= 1)
                return permanent ? LocomotionResult.Continue : LocomotionResult.Finished;

            NavigateGradient.NavigateToPoint(target, deltaTime, ai.Actor, ref lastDirection);
            return LocomotionResult.Continue;
        }
    }
}
