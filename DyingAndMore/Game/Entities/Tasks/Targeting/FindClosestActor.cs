﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Tasks.Targeting
{
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

    //target actor who shot at 'me' most recently

    //find downed/dead/low heatlh ally
}
