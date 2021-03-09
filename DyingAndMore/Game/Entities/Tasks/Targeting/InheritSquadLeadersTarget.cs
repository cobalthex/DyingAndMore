using System;

namespace DyingAndMore.Game.Entities.Tasks.Targeting
{
    [TargetingTask]
    struct InheritSquadLeadersTarget : ITask
    {
        public bool inheritEvenIfNone;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            var squad = ai.Actor.Squad;
            if (squad == null)
                return TaskResult.Failure;

            if (squad.Leader == null ||
                !(squad.Leader.Controller is AIController leaderAi))
                return TaskResult.Success;

            if (inheritEvenIfNone || leaderAi.Target != null)
                ai.Target = leaderAi.Target;

            return TaskResult.Success;
        }
    }
}
