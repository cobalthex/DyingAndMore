using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Tasks.Miscellaneous
{
    [MiscellaneousTask]
    public struct CloneSelf : ITask
    {
        /// <summary>
        /// Where to spawn the clone, relative to this actor's forward direction
        /// Should be normalized
        /// </summary>
        public Vector2 relativeDirection;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            //ensure space
            Vector2 targetPos = ai.Actor.WorldPosition;
            targetPos += (ai.Actor.Radius * 2 + 10) * (relativeDirection * ai.Actor.WorldForward);
            if (!ai.Actor.Map.Class.IsInsideMap(targetPos))
                return TaskResult.Failure;

            var clone = (ActorInstance)ai.Actor.Clone();
            ((AIController)clone.Controller).Reset();
            clone.SetPositionTransformed(targetPos);
            ai.Actor.Map.Spawn(clone);
            return TaskResult.Success;
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