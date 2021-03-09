using System;
using Takai.Game;

namespace DyingAndMore.Game.Entities.Tasks.Offensive
{
    [OffensiveTask]
    public struct Suicide : ITask
    {
        public EffectsClass effect;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            ai.Actor.Kill();
            if (effect != null)
                ai.Actor.Map.Spawn(effect.Instantiate(ai.Actor));

            return TaskResult.Success;
        }
    }

    //set all behaviors

    //possess/takeover?

    //Unpossess task (auto queued)?

    //possess target
    //alt weapons/grenades
    //berserk

    //issue commands
}