using System;

namespace DyingAndMore.Game.Entities.Tasks.Targeting
{
    [TargetingTask]
    public struct TargetAggressor : ITask
    {
        public bool includeAllies;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            //retry if null?

            if (ai.Actor.LastAggressor != null && ai.Actor.LastAggressor is ActorInstance actor &&
                (includeAllies || !actor.IsAlliedWith(ai.Actor.Factions)) &&
                actor.IsAlive)
            {
                ai.Target = actor;
                return TaskResult.Success;
            }
            return TaskResult.Failure;
        }
    }

    //target actor who shot at 'me' most recently

    //find downed/dead/low heatlh ally
}
