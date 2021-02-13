using System;

namespace DyingAndMore.Game.Entities.Tasks
{
    public class SquadTaskAttribute : Attribute { }

    [SquadTask]
    public struct SpawnSquadUnits : ITask
    {
        public bool mustBeLeader;

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
