using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Tasks.Miscellaneous
{
    [MiscellaneousTask]
    public struct FaceTarget : ITask
    {
        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return TaskResult.Failure;

            var dir = Vector2.Normalize(ai.Target.WorldPosition - ai.Actor.WorldPosition);
            ai.Actor.TurnTowards(dir, deltaTime);

            return (Vector2.Dot(ai.Actor.WorldForward, dir) < 0.99f) ? TaskResult.Continue : TaskResult.Success;
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