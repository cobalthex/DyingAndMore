using System;

namespace DyingAndMore.Game.Entities.Tasks.Squads
{
    [SquadTask]
    public struct SpawnSquadUnits : ITask
    {
        public bool mustBeLeader; // todo: make all squad tasks 'squad leader' tasks?

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            var squad = ai.Actor.Squad;
            if (squad == null)
                return TaskResult.Failure;

            if (mustBeLeader && squad.Leader != ai.Actor)
                return TaskResult.Failure;

            squad.SpawnUnits(ai.Actor.Map);
            return TaskResult.Success;
        }
    }
}
