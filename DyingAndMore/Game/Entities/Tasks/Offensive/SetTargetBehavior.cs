using System;

namespace DyingAndMore.Game.Entities.Tasks.Offensive
{
    //create condition to light up and then blow up

    [OffensiveTask]
    public struct SetTargetBehavior : ITask
    {
        public Behavior applyBehavior; //setting to null will render target 'dead'

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null || !(ai.Target.Controller is AIController tai))
                return TaskResult.Failure;

            //store old behavior?
            tai.CurrentBehavior = applyBehavior;

            return TaskResult.Success;
        }
    }

    //Unpossess task(auto queued)?

}