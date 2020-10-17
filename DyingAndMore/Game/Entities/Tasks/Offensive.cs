using System;
using Microsoft.Xna.Framework;
using Takai.Game;
using DyingAndMore.Game.Weapons;

namespace DyingAndMore.Game.Entities.Tasks
{
    public class OffensiveTaskAttribute : Attribute { }

    public static class TrajectoryUtils
    {
        /// <summary>
        /// How long it takes for an entity to accelerate to its Max speed from its current speed
        /// </summary>
        /// <param name="velocity">current velocity</param>
        /// <param name="acceleration">current acceleration</param>
        /// <param name="maxSpeed">Maximum speed</param>
        /// <returns>Seconds until max speed at current acceleration</returns>
        public static float CalculateTimeToMaxSpeed(Vector2 velocity, Vector2 acceleration, float maxSpeed)
        {
            var accelLengthSq = acceleration.LengthSquared(); //a
            var velocityLengthSq = velocity.LengthSquared();
            var maxSpeedSq = maxSpeed * maxSpeed;

            //quadratic formula
            // x = (-b +- sqrt(b^2 - 4ac)) / 2a

            var b = (acceleration.X * velocity.X * 2) + (acceleration.Y * velocity.Y * 2);
            var ac4 = 4 * accelLengthSq * (velocityLengthSq - maxSpeedSq);

            var t = (-b - (float)Math.Sqrt((b * b) - ac4)) / (2 * accelLengthSq);
            return float.IsNaN(t) ? 100000 /* some large number */ : t;
        }

        private struct Trajectory //rename
        {
            public float difference;
            public Vector2 positionToTarget;

            public Trajectory(float difference, Vector2 positionToTarget)
            {
                this.difference = difference;
                this.positionToTarget = positionToTarget;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="test"></param>
        /// <param name="projectileSpeed">Source objects speed</param>
        /// <param name="targetRelativePosition">Target object's position relative to the source</param>
        /// <param name="targetRelativeVelocity">Target object's velocity relative to the source</param>
        /// <param name="targetAcceleration">Target object's acceleration</param>
        /// <param name="targetMaxSpeed">Target object's speed cap</param>
        /// <returns></returns>
        private static Trajectory TestTrajectory(
            float test /* ? */,
            float projectileSpeed, 
            Vector2 targetRelativePosition, 
            Vector2 targetRelativeVelocity, 
            Vector2 targetAcceleration, 
            float targetMaxSpeed)
        {
            var timeToMaxSpeed = CalculateTimeToMaxSpeed(targetRelativeVelocity, targetAcceleration, targetMaxSpeed);

            Vector2 positionAtTest;
            if (test > timeToMaxSpeed)
            {
                positionAtTest 
                    = targetRelativePosition 
                    + (targetRelativeVelocity * timeToMaxSpeed)
                    + (targetAcceleration * (timeToMaxSpeed * (timeToMaxSpeed + 1) / 2)) 
                    + (targetRelativeVelocity + (targetAcceleration * timeToMaxSpeed) * (test - timeToMaxSpeed));
            }
            else
            {
                positionAtTest
                    = targetRelativePosition 
                    + (targetRelativeVelocity * test) 
                    + (targetAcceleration * (test * (test + 1) / 2));
            }

            var t = positionAtTest.Length() / projectileSpeed;
            var bulletOffset = (positionAtTest - targetRelativePosition).Length();
            Vector2 positionToTarget;
            if (t > timeToMaxSpeed)
            {
                positionToTarget 
                    = targetRelativePosition
                    + (targetRelativeVelocity * timeToMaxSpeed) 
                    + (targetAcceleration * (timeToMaxSpeed * (timeToMaxSpeed + 1) / 2)) 
                    + (targetRelativeVelocity + (targetAcceleration * timeToMaxSpeed) * (t - timeToMaxSpeed));
            }
            else
            {
                positionToTarget 
                    = targetRelativePosition
                    + (targetRelativeVelocity * t) 
                    + (targetAcceleration * (t * (t + 1) / 2));
            }

            var targetOffset = (positionToTarget - targetRelativePosition).Length();
            return new Trajectory(bulletOffset - targetOffset, positionToTarget);
        }

        /// <summary>
        /// Binary search between current and projected target position
        /// </summary>
        /// <param name="left">left extreme of the search range</param>
        /// <param name="right">right extreme of the search range</param>
        /// <param name="projectileSpeed">Source objects speed</param>
        /// <param name="targetRelativePosition">Target object's position relative to the source</param>
        /// <param name="targetRelativeVelocity">Target object's velocity relative to the source</param>
        /// <param name="targetAcceleration">Target object's acceleration</param>
        /// <param name="targetMaxSpeed">Target object's speed cap</param>
        /// <returns></returns>
        public static Vector2 LeadTarget(
            float left,
            float right,
            float projectileSpeed, 
            Vector2 targetRelativePosition, 
            Vector2 targetRelativeVelocity, 
            Vector2 targetAcceleration, 
            float targetMaxSpeed)
        {
            var test = (left + right) / 2;

            var ret = TestTrajectory(
                test, 
                projectileSpeed, 
                targetRelativePosition, 
                targetRelativeVelocity, 
                targetAcceleration, 
                targetMaxSpeed
            );
            
            // SMax when within 1 unit of error
            if (Math.Abs(ret.difference) < 1)
                return ret.positionToTarget;

            if (ret.difference < 0) //undershot
            {
                return LeadTarget(
                    test,
                    right, 
                    projectileSpeed, 
                    targetRelativePosition, 
                    targetRelativeVelocity, 
                    targetAcceleration, 
                    targetMaxSpeed
                );
            }
            else //overshot
            {
                return LeadTarget(
                    left, 
                    test, 
                    projectileSpeed, 
                    targetRelativePosition, 
                    targetRelativeVelocity,
                    targetAcceleration,
                    targetMaxSpeed
                );
            }
        }

        /// <summary>
        /// Predict the path a projectile should take to intercept a target
        /// </summary>
        /// <param name="sourcePosition">Source object's position</param>
        /// <param name="sourceVelocity">Source objec's velocity</param>
        /// <param name="projectileSpeed">Projectile's speed</param>
        /// <param name="targetPosition">Target object's position</param>
        /// <param name="targetVelocity">Target object's velocity</param>
        /// <param name="targetAcceleration">Target object's acceleration</param>
        /// <param name="targetMaxSpeed">Target object's speed cap</param>
        /// <param name="predictedPosition">The position of the interception, if there is one</param>
        /// <returns>True if the target can be intercepted</returns>
        public static bool PredictTargetPosition(
            Vector2 sourcePosition, 
            Vector2 sourceVelocity,
            float projectileSpeed, //projectile's speed
            Vector2 targetPosition, 
            Vector2 targetVelocity, 
            Vector2 targetAcceleration, 
            float targetMaxSpeed, 
            out Vector2 predictedPosition)
        {
            var maxDistance = 4096; //use dist per sec?
            var ret = TestTrajectory(maxDistance, projectileSpeed, targetPosition - sourcePosition, targetVelocity - sourceVelocity, targetAcceleration, targetMaxSpeed);

            if (ret.difference >= 0)
            {
                var positionToTarget = LeadTarget(0, maxDistance, projectileSpeed, targetPosition - sourcePosition, targetVelocity - sourceVelocity, targetAcceleration, targetMaxSpeed);

                predictedPosition = sourcePosition + positionToTarget;
                return true;
            }

            predictedPosition = Vector2.Zero; //targetPosition?
            return false;
        }

    }

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
            if (Vector2.Dot(ai.Actor.WorldForward, direction) < 0.99f)
                return TaskResult.Continue;

            ai.Actor.Weapon.TryUse();
            return TaskResult.Success;
        }
    }

    [OffensiveTask]
    public struct SprayTarget : ITask
    {
        /// <summary>
        /// The total spray angle range to spray
        /// </summary>
        public float sprayAngle;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            //option to turn to -angle/2 first?
            //option to go ccw vs cw
            //step 1: move to -angle/2
            //step 2: spray to +angle

            return TaskResult.Success;
        }
    }

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

    [OffensiveTask]
    public struct SetConditionOnSelf : ITask
    {
        public ConditionClass condition;
        public TimeSpan duration;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (condition == null)
                return TaskResult.Failure;

            ai.Actor.Conditions[condition] = condition.Instantiate(duration);
            return TaskResult.Success;
        }
    }

    //must be 'touching'
    [OffensiveTask]
    public struct SetConditionOnTarget : ITask
    {
        public ConditionClass condition;
        public TimeSpan duration;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null || !ai.Actor.InRange(ai.Target, 20) || condition == null)
                return TaskResult.Failure; //facing target?

            ai.Actor.Conditions[condition] = condition.Instantiate(duration);
            return TaskResult.Success;
        }
    }
    //create condition to light up and then blow up

    [OffensiveTask]
    public struct SetTargetBehavior : ITask
    {
        public Behavior applyBehavior; //setting to null will render target 'dead'

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null || !(ai.Target.Controller is AIController tai))
                return TaskResult.Failure;

            //store old behavior?
            tai.CurrentBehavior = applyBehavior;

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