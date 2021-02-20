using System;

namespace DyingAndMore.Game.Entities.Tasks
{
    public class MovementTaskAttribute : Attribute { }

    [MovementTask]
    public struct SetLocomotor : ITask
    {
        public ILocomotor locomotor;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            ai.CurrentLocomotor = locomotor;
            return TaskResult.Success;
        }
    }

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
