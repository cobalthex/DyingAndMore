using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Tasks
{
    public class TargetingTaskAttribute : Attribute { }

    [TargetingTask]
    public struct ForgetTarget : ITask
    {
        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            ai.Target = null;
            return TaskResult.Success;
        }
    }

    [TargetingTask]
    public struct FindClosestActor : ITask
    {
        public bool isAlly;
        public bool isSameClass;

        //class filter?

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            var ents = ai.Actor.Map.FindEntitiesInRegion(ai.Actor.WorldPosition, ai.SightRange);

            var possibles = new List<ActorInstance>();
            foreach (var ent in ents)
            {
                if (ent == ai.Actor || !(ent is ActorInstance actor))
                    continue;

                if (isAlly ^ actor.IsAlliedWith(ai.Actor.Factions))
                    continue;

                if (isSameClass ^ (actor.Class == ai.Actor.Class)) //compare class names?
                    continue;

                //    ai.Actor.IsFacing(actor.WorldPosition)) ?

                possibles.Add(actor);
            }

            possibles.Sort(delegate (ActorInstance a, ActorInstance b)
            {
                var afw = Vector2.Dot(a.WorldForward, ai.Actor.WorldForward);
                var bfw = Vector2.Dot(b.WorldForward, ai.Actor.WorldForward);
                return (afw == bfw ? 0 : (int)Math.Ceiling(bfw - afw));
            });

            if (possibles.Count > 0)
            {
                ai.Target = possibles[0];
                return TaskResult.Success;
            }

            ai.Target = null;
            return TaskResult.Continue;
        }
    }

    [TargetingTask]
    public struct TargetAggressor : ITask
    {
        public bool includeAllies;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            //retry if null?

            if (ai.Actor.LastAgressor != null && ai.Actor.LastAgressor is ActorInstance actor &&
                (includeAllies || !actor.IsAlliedWith(ai.Actor.Factions)))
                ai.Target = actor;

            return TaskResult.Success;
        }
    }

    //target actor who shot at 'me' most recently

    //find downed/dead/low heatlh ally
}
