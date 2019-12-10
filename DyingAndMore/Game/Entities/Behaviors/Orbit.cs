using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Behaviors
{
    public class OrbitBehavior : Behavior
    {
        public float DesiredRadius { get; set; } = 100; // include parent and self radius in this distance? (dist between edges or dist between centers)

        public override BehaviorMask Mask => BehaviorMask.Movement;

        public override BehaviorFilters Filter => BehaviorFilters.RequiresParent;

        public override BehaviorPriority CalculatePriority()
        {
            return BehaviorPriority.Normal;
        }

        public override void Think(TimeSpan deltaTime)
        {
            Vector2 origin = Vector2.Zero;

            var diff = AI.Actor.Position - origin;
            var diffN = Vector2.Normalize(diff);

            var r = diffN * DesiredRadius;

            float orientation = Takai.Util.Determinant(r, AI.Actor.RealForward); //  check if 0
            if (orientation == 0)
                orientation = -1;
            
            float theta = MathHelper.PiOver4 * orientation;

            AI.Actor.Map.DrawCircle(AI.Actor.WorldParent.RealPosition, DesiredRadius, Color.Gray);
            AI.Actor.Map.DrawArrow(AI.Actor.RealPosition, diffN, DesiredRadius, Color.Red);

            AI.Actor.Forward = Vector2.TransformNormal(AI.Actor.Forward, Matrix.CreateRotationZ(theta * (float)deltaTime.TotalSeconds));

            //AI.Actor.Accelerate(AI.Actor.Forward);
        }
    }
}

//a = original, b = desired direction, proj = a on b
//proj = a * unit(b) = (a.unit(b))*unit(b)