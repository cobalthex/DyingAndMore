using System;

namespace DyingAndMore.Game.Entities.Tasks.Squads
{
    [SquadTask]
    public struct KillSquadUnits : ITask
    {
        public bool includeSelf;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            var squad = ai.Actor.Squad;
            if (squad == null)
                return TaskResult.Failure;

            if (includeSelf)
                squad.DestroyAllUnits();
            else
            {
                foreach (var unit in squad.Units)
                {
                    if (unit == ai.Actor)
                        continue;

                    unit.Kill();
                }

                if (squad.Leader != ai.Actor)
                    squad.Leader = null;

                squad.Units.Clear();
                squad.Units.Add(ai.Actor);
            }

            return TaskResult.Success;
        }
    }
}
