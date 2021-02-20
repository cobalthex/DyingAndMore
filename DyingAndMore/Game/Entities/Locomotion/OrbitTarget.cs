using Microsoft.Xna.Framework;
using System;
using Takai;

namespace DyingAndMore.Game.Entities.Locomotion
{
    public struct OrbitTarget : ILocomotor
    {
        public float radius;
        public bool faceTarget; //as opposed to facing in the direction of travel

        //success condition? (time?)

        public LocomotionResult Move(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return LocomotionResult.Finished; //no ghosts allowed

            var origin = ai.Target.WorldPosition;

            var diff = origin - ai.Actor.WorldPosition;
            var diffLen = diff.Length();

            Vector2 accelAngle;

            //ai.Actor.Map.DrawCircle(ai.Target.WorldPosition, radius, Color.CornflowerBlue, 2, 4);

            //var diffSpacing = difflen - ai.actor.radius - ai.target.radius :: use below and in a
            if (diffLen > radius)
            {
                var angleToTangent = (float)Math.Asin(radius / diffLen); //angle between position relative to circle and intersecting tangent
                // θ = asin (opp / hyp)
                //if at edge of circle, opp (radius) = hyp (diffLen) so actor should travel along tangent, (triangle has zero area)
                //if farther away, hyp > opp so angle grows meaning the actor must turn farther in to approach the tangent
                //if closer than radius, asin returns imaginary result (NaN in C#)
                //at opp = hyp, will be fwd - 90deg

                var relAngle = Util.Angle(diff); //angle between actor and target (to orient angleToTangent)

                //if fwd is pointing left of diff, move leftwards (clockwise), otherwise ccw
                //det > 0 == cw
                var det = Util.Determinant(ai.Actor.WorldForward, diff);

                var desiredAngle = relAngle - (angleToTangent * Math.Sign(det));
                var desiredDir = Util.Direction(desiredAngle);

                if (faceTarget)
                {
                    ai.Actor.TurnTowards(diff, deltaTime);
                    accelAngle = Vector2.Lerp(ai.Actor.Forward.Ortho(), desiredDir, 0.2f);
                }
                else
                {
                    //todo: might be able to exploit (radius / diffLen) to skip angle calculations

                    var sign = Util.Determinant(ai.Actor.WorldForward, desiredDir);

                    //crude
                    float angle = ActorInstance.TurnSpeed * sign * (float)deltaTime.TotalSeconds;
                    accelAngle = ai.Actor.Forward = Vector2.TransformNormal(ai.Actor.Forward, Matrix.CreateRotationZ(angle));
                }
            }
            else
            {
                if (faceTarget && ai.Actor.Velocity != Vector2.Zero)
                    accelAngle = Vector2.Normalize(ai.Actor.Velocity);
                else
                    accelAngle = ai.Actor.Forward;
            }

            ai.Actor.Accelerate(accelAngle);

            return LocomotionResult.Continue;
        }
    }
}
