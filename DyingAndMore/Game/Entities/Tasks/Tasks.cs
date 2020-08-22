using System;
using DyingAndMore.Game.Weapons;
using Microsoft.Xna.Framework;
using Takai.UI;

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

    public class MiscellaneousTaskAttribute : Attribute { }

    [MiscellaneousTask]
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

    [MiscellaneousTask]
    public struct HealSelf : ITask
    {
        public float healthPerSecond;
        public TimeSpan duration;
        public bool canRevive;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Actor.Map.ElapsedTime > ai.CurrentTaskStartTime + duration)
                return TaskResult.Success;

            if (!ai.Actor.IsAlive)
            {
                if (canRevive)
                    ai.Actor.Resurrect();
                else
                    return TaskResult.Failure;
            }

            ai.Actor.CurrentHealth += (float)deltaTime.TotalSeconds * healthPerSecond;
            return TaskResult.Continue;
        }
    }

    [MiscellaneousTask]
    public struct HealTarget : ITask
    {
        public float healthPerSecond;
        public TimeSpan duration;
        public bool canRevive;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return TaskResult.Failure; //must be within certain distance?

            if (ai.Actor.Map.ElapsedTime > ai.CurrentTaskStartTime + duration)
                return TaskResult.Success;

            if (!ai.Target.IsAlive)
            {
                if (canRevive)
                    ai.Actor.Resurrect();
                else
                    return TaskResult.Failure;
            }

            ai.Target.CurrentHealth += (float)deltaTime.TotalSeconds * healthPerSecond;
            return TaskResult.Continue;
        }
    }

    public enum SetOperation
    {
        Replace,
        Union,
        Intersection,
        Outersection, //symmetric difference/xor
        Difference,
    }
    public class SetOperationsSelect : EnumSelect<SetOperation> { }

    [MiscellaneousTask]
    public struct SetTargetFactions : ITask
    {
        public Factions factions;
        public SetOperation method;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return TaskResult.Failure;

            switch (method)
            {
                case SetOperation.Replace:
                    ai.Target.Factions = factions;
                    break;
                case SetOperation.Union:
                    ai.Target.Factions |= factions;
                    break;
                case SetOperation.Intersection:
                    ai.Target.Factions &= factions;
                    break;
                case SetOperation.Outersection:
                    ai.Target.Factions ^= factions;
                    break;
                case SetOperation.Difference:
                    ai.Target.Factions &= ~factions;
                    break;
            }
            return TaskResult.Success;
        }
    }

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

    [MiscellaneousTask]
    public struct SetOwnClass : ITask
    {
        public ActorClass @class;
        public bool inheritController;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            var actor = ai.Actor;
            actor.Class = @class;
            if (inheritController)
                actor.Controller = ai;
            return TaskResult.Success;
        }
    }

    [MiscellaneousTask]
    public struct SetTargetClass : ITask
    {
        public ActorClass @class;
        public bool inheritController;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return TaskResult.Failure;

            var actor = ai.Target;
            actor.Class = @class;
            if (inheritController)
                actor.Controller = ai;
            return TaskResult.Success;
        }
    }

    [MiscellaneousTask]
    public struct SetOwnWeapon : ITask
    {
        public WeaponClass weapon;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            ai.Actor.Weapon = weapon.Instantiate();
            return TaskResult.Success;
        }
    }

    [MiscellaneousTask]
    public struct SetTargetWeapon : ITask
    {
        public WeaponClass weapon;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return TaskResult.Failure;

            ai.Target.Weapon = weapon.Instantiate();
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