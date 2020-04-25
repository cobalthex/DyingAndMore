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

    public struct FindClosestTarget : ITask
    {
        public float sightDistance;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
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

    /// <summary>
    /// Move in a straight line (or there abouts) towards the target
    /// </summary>
    public struct MoveToTarget : ITask
    {
        /// <summary>
        /// Maximum distance required for this task to complete.
        /// Offset from distance between two actors combined radii
        /// </summary>
        public float distance;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return TaskResult.Failure;

            var interDist = ai.Target.RadiusSq + ai.Actor.RadiusSq;
            if (Vector2.DistanceSquared(ai.Target.Position, ai.Actor.Position) 
                <= (distance * distance) + interDist)
                return TaskResult.Success;

            var dir = Vector2.Normalize(ai.Target.Position - ai.Actor.Position);
            ai.Actor.TurnTowards(dir, deltaTime);
            ai.Actor.Accelerate(ai.Actor.Forward);

            return TaskResult.Continue;
        }
    }

    /// <summary>
    /// Navigate towards a particular value in the flow field gradient
    /// </summary>
    public struct NavigateGradient : ITask
    {
        public static readonly Point[] NavigationDirections =
        {
            new Point(-1, -1),
            new Point( 0, -1),
            new Point( 1, -1),
            new Point(-1,  0),
            new Point( 1,  0),
            new Point(-1,  1),
            new Point( 0,  1),
            new Point( 1,  1),
        };

        //todo: move
        static internal void NavigateToPoint(uint target, TimeSpan deltaTime, ActorInstance actor)
        {
            var cur = actor.Map.PathInfoAt(actor.WorldPosition).heuristic;
            var best = target < cur ? uint.MaxValue : 0;

            var possible = Point.Zero;

            var pos = (actor.WorldPosition / actor.Map.Class.TileSize).ToPoint();
            //calculate best direction to move
            foreach (var dir in NavigationDirections)
            {
                var next = pos + dir;
                if (!actor.Map.Class.TileBounds.Contains(next))
                    continue;
                
                //note: doesn't really work with cur < target due to how heuristic is generated
                var h = actor.Map.PathInfo[next.Y, next.X].heuristic;
                if ((cur > target && h < best) ||
                    (cur < target && h > best))
                {
                    possible = dir;
                    best = h;
                }
                else if (h == best)
                    possible = dir; //can store this in a list and pick randomly among possible directions
            }

            //todo: random direction from possible?
            if (possible != Point.Zero)
            {
                actor.TurnTowards(Vector2.Normalize(possible.ToVector2()), deltaTime);
                actor.Accelerate(actor.Forward);
            }
        }

        public uint targetValue;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            var cur = ai.Actor.Map.PathInfoAt(ai.Actor.WorldPosition).heuristic;
            if (Math.Abs(cur - targetValue) <= 1)
                return TaskResult.Success;

            NavigateToPoint(targetValue, deltaTime, ai.Actor);
            return TaskResult.Continue;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public struct NavigateToTarget : ITask
    {
        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null) //fail on target death?
                return TaskResult.Failure;

            //todo: sight range

            var cur = ai.Actor.Map.PathInfoAt(ai.Actor.WorldPosition).heuristic;
            var target = ai.Actor.Map.PathInfoAt(ai.Target.WorldPosition).heuristic;
            if (Math.Abs(cur - target) < 1)
                return TaskResult.Success;

            NavigateGradient.NavigateToPoint(target, deltaTime, ai.Actor);
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

    //tasks are individual actions
    //run, face direction, pick target, etc

    //behaviors are task state machines
    //tasks run until interrupted
}
