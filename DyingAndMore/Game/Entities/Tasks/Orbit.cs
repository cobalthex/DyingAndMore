using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Tasks
{
    public struct OrbitTarget : ITask
    {
        public float radius;

        //success condition? (time?)

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return TaskResult.Failure; //no ghosts allowed

            var origin = ai.Target.WorldPosition;

            var diff = origin - ai.Actor.WorldPosition;
            var diffLen = diff.Length();
            //var diffSpacing = difflen - ai.actor.radius - ai.target.radius :: use below and in a
            if (diffLen > radius)
            {
                var a = (float)Math.Asin(radius / diffLen);
                var b = Takai.Util.Angle(diff); //can this be optimized? (w/ above)

                float t;
                Vector2 tr;

                var det = Takai.Util.Determinant(ai.Actor.WorldForward, diff / diffLen); //normalize necessary?
                if (det > 0)
                {
                    t = b - a;
                    tr = new Vector2((float)Math.Sin(t), (float)-Math.Cos(t)) * radius;
                }
                else
                {
                    t = b + a;
                    tr = new Vector2((float)-Math.Sin(t), (float)Math.Cos(t)) * radius;
                }

                var tangentDir = Vector2.Normalize((origin + tr) - ai.Actor.WorldPosition);

                var cross = Takai.Util.Determinant(ai.Actor.WorldForward, tangentDir);

                float angle = (2 * ActorInstance.TurnSpeed) * cross * (float)deltaTime.TotalSeconds;
                ai.Actor.Forward = Vector2.TransformNormal(ai.Actor.Forward, Matrix.CreateRotationZ(angle));
            }
            ai.Actor.Accelerate(ai.Actor.WorldForward);

            return TaskResult.Continue;
        }
    }
}