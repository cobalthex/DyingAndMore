using System;

namespace DyingAndMore.Game.Entities.Tasks.Offensive
{
    [OffensiveTask]
    public struct SetConditionOnSelf : ITask
    {
        public ConditionClass condition;
        public TimeSpan duration;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (condition == null)
                return TaskResult.Failure;

            ai.Actor.Conditions[condition] = condition.Instantiate(duration);
            return TaskResult.Success;
        }
    }

    //set all behaviors

    //possess/takeover?

    //Unpossess task (auto queued)?

    //possess target
    //alt weapons/grenades
    //berserk

    //issue commands
}