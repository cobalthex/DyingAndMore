using System;
using Microsoft.Xna.Framework;
using DyingAndMore.Game.Weapons;

namespace DyingAndMore.Game.Entities.Tasks.Offensive
{
    public enum AimingMethod
    {
        Forward, //shoot forward without aiming
        Random, // pick a direction (within fov ? ) and aim there
        FaceTarget, //shoot at the target's current position
        LeadTarget, //shoot where the target is likely to be
    }

    [OffensiveTask]
    public struct ShootAtTarget : ITask
    {
        public AimingMethod aimingMethod;

        Vector2 randomDir;
        //leading accuracy

        //option to continue moving in direction?

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null || ai.Actor.Weapon == null)
                return TaskResult.Failure;

            //shoot N times?
            //reloading

            //should try use fail?

            Vector2 direction = Vector2.Zero;

            switch (aimingMethod)
            {
                case AimingMethod.Forward:
                    ai.Actor.Weapon.TryUse();
                    return TaskResult.Success;

                case AimingMethod.Random:
                    if (randomDir == Vector2.Zero)
                        randomDir = Takai.Util.Direction((float)Takai.Util.RandomGenerator.NextDouble() * MathHelper.TwoPi);
                    
                    direction = randomDir;
                    break;

                case AimingMethod.FaceTarget:
                    direction = Vector2.Normalize(ai.Target.Position - ai.Actor.WorldPosition);
                    break;

                case AimingMethod.LeadTarget:
                    {
                        var projectileSpeed = 10f; //pick another number? (maybe infinite?)
                        if (ai.Actor.Weapon.Class is GunClass gun)
                            projectileSpeed = gun.Projectile.MuzzleVelocity.max; //random?

                        bool intercepted = TrajectoryUtils.PredictTargetPosition(
                            ai.Actor.WorldPosition,
                            ai.Actor.Velocity,
                            projectileSpeed,
                            ai.Target.WorldPosition,
                            ai.Target.Velocity,
                            Vector2.Zero, // current - last velocity ?
                            ai.Target.MaxSpeed,
                            out var positionToTarget
                        );

                        if (!intercepted)
                            return TaskResult.Failure;

                        direction = Vector2.Normalize(positionToTarget - ai.Actor.WorldPosition);
                    }
                    break;
            }


            ai.Actor.TurnTowards(direction, deltaTime); //todo: this is too slow
            if (ai.Actor.Velocity.LengthSquared() > 0.001f)
                ai.Actor.Accelerate(ai.Actor.WorldForward);

            if (Vector2.Dot(ai.Actor.WorldForward, direction) < 0.99f)
                return TaskResult.Continue;

            ai.Actor.Weapon.TryUse();
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