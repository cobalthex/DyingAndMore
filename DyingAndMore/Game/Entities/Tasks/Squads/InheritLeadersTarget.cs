using System;

namespace DyingAndMore.Game.Entities.Tasks.Squads
{
    [SquadTask]
    [Takai.UI.DisplayName("Inherit leader's target")]
    public struct InheritLeadersTarget : ITask
    {
        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            var squad = ai.Actor.Squad;
            if (squad == null || squad.Leader == null || !squad.Leader.IsAlive)
                return TaskResult.Failure;

            if (!(squad.Leader.Controller is AIController leaderAI))
                return TaskResult.Failure;

            ai.Target = leaderAI.Target;
            return TaskResult.Success;
        }
    }
}
