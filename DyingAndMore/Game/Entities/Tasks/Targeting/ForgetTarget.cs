using System;

namespace DyingAndMore.Game.Entities.Tasks.Targeting
{
    [TargetingTask]
    public struct ForgetTarget : ITask
    {
        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            ai.Target = null;
            return TaskResult.Success;
        }
    }

    //target actor who shot at 'me' most recently

    //find downed/dead/low heatlh ally
}
