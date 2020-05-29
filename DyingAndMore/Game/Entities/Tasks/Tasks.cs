using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game.Entities.Tasks
{
    public enum TaskResult
    {
        Continue, //rename?
        Failure,
        Success
    }

    public interface ITask
    {
        TaskResult Think(TimeSpan deltaTime, AIController ai);
    }

    //pre/suffix tasks with 'Task' ?

    public struct Wait : ITask
    {
        public TimeSpan duration;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Actor.Map.ElapsedTime < ai.CurrentTaskStartTime + duration)
                return TaskResult.Continue;
            return TaskResult.Success;
        }
    }

    public struct FindClosestEnemy : ITask
    {
        public float sightDistance;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            //auto-success if already have target?
            if (ai.Target != null)
                return TaskResult.Success;

            var ents = ai.Actor.Map.FindEntitiesInRegion(ai.Actor.WorldPosition, sightDistance);

            var possibles = new List<ActorInstance>();
            foreach (var ent in ents)
            {
                if (ent != ai.Actor &&
                    ent is ActorInstance actor &&
                    !actor.IsAlliedWith(ai.Actor.Factions) &&
                    ai.Actor.IsFacing(actor.WorldPosition))
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

    //provide cover
    //clone
    //spawn entities
    //group with allies
    //attach
    //shield/provide cover
    //get in cover
    //follow path
    //assasinate (attack from behind)

    //tasks are individual actions
    //run, face direction, pick target, etc

    //behaviors are task state machines
    //tasks run until completion or interrupted
}
