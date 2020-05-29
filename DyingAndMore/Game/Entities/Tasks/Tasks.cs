using System;
using System.Collections.Generic;
using System.Security.Cryptography;
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

    public struct FindClosestActor : ITask
    {
        public bool isAlly;
        public bool isSameClass;

        public float sightDistance;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target != null) //specify as option to change target?
                return TaskResult.Success;

            var ents = ai.Actor.Map.FindEntitiesInRegion(ai.Actor.WorldPosition, sightDistance);

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

    public struct ForgetTarget : ITask
    {
        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            //conditions?
            ai.Target = null;
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

    /// <summary>
    /// Attach to the target.
    /// Fails if not next to target
    /// </summary>
    public struct AttachToTarget : ITask
    {
        //attach angle (may require this task moving into position)
        // e.g. form geometric patterns

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null || ai.Target.WorldParent == ai.Actor)
                return TaskResult.Failure;

            if (ai.Actor.WorldParent == ai.Target)
                return TaskResult.Success;

            var distSq = Vector2.DistanceSquared(ai.Actor.WorldPosition, ai.Target.WorldPosition);
            var desired = ai.Actor.Radius + ai.Target.Radius + 10;
            if (distSq <= desired * desired)
            {
                //move actor to touch?
                ai.Actor.Map.Attach(ai.Target, ai.Actor);
                return TaskResult.Success;
            }
            return TaskResult.Failure;
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
