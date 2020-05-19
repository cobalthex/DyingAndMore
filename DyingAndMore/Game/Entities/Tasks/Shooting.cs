using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Tasks
{
    public static class TrajectoryUtils
    {
        public static float CalculateMaxSpeedTime(Vector2 velocity, Vector2 accel, float maxSpeed) // rename
        {
            var accelLengthSq = accel.LengthSquared();
            var velocityLengthSq = velocity.LengthSquared();
            var maxSpeedSq = maxSpeed * maxSpeed;

            //quadratic formula
            // x = (-b +- sqrt(b^2 - 4ac)) / 2a

            var b = (accel.X * velocity.X * 2) + (accel.Y * velocity.Y * 2);
            var ac4 = 4 * accelLengthSq * (velocityLengthSq - maxSpeedSq);

            var t = (-b - (float)Math.Sqrt((b * b) - ac4)) / (2 * accelLengthSq);
            return float.IsNaN(t) ? 100000 /* some large number */ : t;
        }

        public static void TestTrajectory(float test /* ? */, float sourceSpeed, Vector2 targetRelativePosition, Vector2 targetRelativeVelocity, Vector2 targetAcceleration, float targetMaxSpeed)
        {
            var maxSpeedTime = CalculateMaxSpeedTime(targetRelativeVelocity, targetAcceleration, targetMaxSpeed);
        }
    }

    public struct FireAtTarget : ITask
    {

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null || ai.Actor.Weapon == null)
                return TaskResult.Failure;

            //shoot N times?

            // turn towards actor then shoot

            ai.Actor.Weapon.TryUse();

            return TaskResult.Success;
        }
    }

    // trajectory calculation styles:
    // lead target
    // shoot at target current position
    // shoot around target
    // 
}


/*
a = (V0.x * V0.x) + (V0.y * V0.y) - (s1 * s1)
b = 2 * ((P0.x * V0.x) + (P0.y * V0.y) - (P1.x * V0.x) - (P1.y * V0.y))
c = (P0.x * P0.x) + (P0.y * P0.y) + (P1.x * P1.x) + (P1.y * P1.y) - (2 * P1.x * P0.x) - (2 * P1.y * P0.y)

t1 = (-b + sqrt((b * b) - (4 * a * c))) / (2 * a)
t2 = (-b - sqrt((b * b) - (4 * a * c))) / (2 * a)

discard if t < 0 or NaN. take smaller of two values
*/
