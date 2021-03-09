using System;

namespace DyingAndMore.Game.Entities.Tasks.Offensive
{
    //must be 'touching'
    [OffensiveTask]
    public struct SetConditionOnTarget : ITask
    {
        public ConditionClass condition;
        public TimeSpan duration;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null || !ai.Actor.InRange(ai.Target, 20) || condition == null)
                return TaskResult.Failure; //facing target?

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