using System;

namespace DyingAndMore.Game.Entities.Tasks.Squads
{
    [SquadTask]
    class SetUnitBehaviorIfLeader : ITask
    {
        // % or N affected?

        public Behavior applyBehavior;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (!ai.Actor.IsSquadLeader)
            {
                System.Diagnostics.Debug.WriteLine("AI actor must be leader");
                return TaskResult.Failure;
            }

            foreach (var unit in ai.Actor.Squad.Units)
            {
                if (!(unit.Controller is AIController unitAi))
                    continue;

                unitAi.CurrentBehavior = applyBehavior;
            }

            return TaskResult.Success;
        }
    }
}
