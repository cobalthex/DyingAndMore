using System;

namespace DyingAndMore.Game.Entities.Tasks.Movement
{
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
}
