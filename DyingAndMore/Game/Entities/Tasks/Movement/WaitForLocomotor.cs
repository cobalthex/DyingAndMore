using System;

namespace DyingAndMore.Game.Entities.Tasks.Movement
{
    [MovementTask]
    public struct WaitForLocomotor : ITask
    {
        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            // wait for specific? original when first activated?
            return ai.CurrentLocomotor == null ? TaskResult.Success : TaskResult.Continue;
        }
    }
}
