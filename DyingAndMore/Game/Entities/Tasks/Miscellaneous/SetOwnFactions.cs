using System;

namespace DyingAndMore.Game.Entities.Tasks.Miscellaneous
{
    [MiscellaneousTask]
    public struct SetOwnFactions : ITask
    {
        public Factions factions;
        public SetOperation method;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            switch (method)
            {
                case SetOperation.Replace:
                    ai.Actor.Factions = factions;
                    break;
                case SetOperation.Union:
                    ai.Actor.Factions |= factions;
                    break;
                case SetOperation.Intersection:
                    ai.Actor.Factions &= factions;
                    break;
                case SetOperation.Outersection:
                    ai.Actor.Factions ^= factions;
                    break;
                case SetOperation.Difference:  
                    ai.Actor.Factions &= ~factions;
                    break;
            }
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