using System;

namespace DyingAndMore.Game.Entities.Tasks.Miscellaneous
{
    [MiscellaneousTask]
    public struct HealSelf : ITask
    {
        public float healthPerSecond;
        public TimeSpan duration;
        public bool canRevive;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Actor.Map.ElapsedTime > ai.CurrentTaskStartTime + duration)
                return TaskResult.Success;

            if (!ai.Actor.IsAlive)
            {
                if (canRevive)
                    ai.Actor.Resurrect();
                else
                    return TaskResult.Failure;
            }

            ai.Actor.CurrentHealth += (float)deltaTime.TotalSeconds * healthPerSecond;
            return TaskResult.Continue;
        }
    }

    //todo: play animations
    //todo: recategorize some of these

    //teleport (possibly with delay, e.g. burrowing)
    //  still follows path/trajectory

    //tasks are individual actions
    //run, face direction, pick target, etc

    //behaviors are task state machines
    //tasks run until completion or interrupted
}


//vent/tunnels?
//can enter one, choose one near player to exit out of after some delay (based on distance/move speed?)



//actor shoots part of self, losing health, must recollect to regain health (non-recoverable energy used to fire)