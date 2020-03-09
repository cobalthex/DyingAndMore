using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Behaviors
{
    /// <summary>
    /// Orbit around a point
    /// (Does not perform astrodynamics)
    /// Attempts to maintain orbit at standard acceleration speed and turn speed
    /// Turns towards tangent
    /// </summary>
    public class OrbitBehavior : Behavior
    {
        public float DesiredDistance { get; set; } = 150; // include parent and self radius in this distance? (dist between edges or dist between centers)

        /// <summary>
        /// How fast to turn towards the desired orbit radius
        /// Defaults to π
        /// </summary>
        public float TurnSpeed = MathHelper.Pi;

        public override BehaviorMask Mask => BehaviorMask.Movement;
        public override BehaviorFilters Filter => BehaviorFilters.RequiresTarget;

        public override BehaviorPriority CalculatePriority()
        {
            return BehaviorPriority.Normal;
        }

        public override void Think(TimeSpan deltaTime)
        {
            var origin = AI.Target.WorldPosition;
                //new Vector2(0); //orbit around parent

            var diff = origin - AI.Actor.Position;
            var diffLen = diff.Length();
            //var diffSpacing = difflen - ai.actor.radius - ai.target.radius :: use below and in a
            if (diffLen > DesiredDistance)
            {
                var a = (float)Math.Asin(DesiredDistance / diffLen);
                var b = Takai.Util.Angle(diff); //can this be optimized? (w/ above)

                float t;
                Vector2 tr;

                var det = Takai.Util.Determinant(AI.Actor.Forward, diff / diffLen); //normalize necessary?
                if (det > 0)
                {
                    t = b - a;
                    tr = new Vector2((float)Math.Sin(t), (float)-Math.Cos(t)) * DesiredDistance;
                }
                else
                {
                    t = b + a;
                    tr = new Vector2((float)-Math.Sin(t), (float)Math.Cos(t)) * DesiredDistance;
                }

                var tangentDir = Vector2.Normalize((origin + tr) - AI.Actor.Position);

                var cross = Takai.Util.Determinant(AI.Actor.Forward, tangentDir);
                float angle = TurnSpeed * cross * (float)deltaTime.TotalSeconds;

                //turn towards?
                AI.Actor.Forward = Vector2.TransformNormal(AI.Actor.Forward, Matrix.CreateRotationZ(angle));
            }
            AI.Actor.Accelerate(AI.Actor.Forward);
        }
    }
}