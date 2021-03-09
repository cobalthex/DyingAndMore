using System;

namespace DyingAndMore.Game.Entities.Tasks.Miscellaneous
{
    /// <summary>
    /// Attach to the target.
    /// Fails if not next to target
    /// </summary>
    [MiscellaneousTask]
    public struct AttachToTarget : ITask
    {
        //attach angle (may require this task moving into position)
        // e.g. form geometric patterns

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            ai.MaybeInterruptLocomotion();

            if (ai.Target == null || ai.Target.WorldParent == ai.Actor)
                return TaskResult.Failure;

            if (ai.Actor.WorldParent == ai.Target)
                return TaskResult.Success;

            if (ai.Actor.InRange(ai.Target, 20))
            {
                //move actor to touch?
                ai.Actor.Map.Attach(ai.Target, ai.Actor);
                return TaskResult.Success;
            }
            return TaskResult.Failure;
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