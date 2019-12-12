using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Behaviors
{
    public class OrbitBehavior : Behavior
    {
        public float DesiredRadius { get; set; } = 100; // include parent and self radius in this distance? (dist between edges or dist between centers)

        public override BehaviorMask Mask => BehaviorMask.Movement;

        //public override BehaviorFilters Filter => BehaviorFilters.RequiresParent;

        public override BehaviorPriority CalculatePriority()
        {
            return BehaviorPriority.Normal;
        }

        public override void Think(TimeSpan deltaTime)
        {
            Vector2 origin = new Vector2(50);

            var diff = AI.Actor.Position - origin;
            var diffN = Vector2.Normalize(diff);

            var r = diffN * DesiredRadius;

            float orientation = Takai.Util.Determinant(r, (AI.Actor.Position - r));
            Takai.DebugPropertyDisplay.AddRow("Orientation", orientation.ToString("N4"));
            
            float theta = MathHelper.PiOver4 * Math.Sign(orientation); //todo

            //AI.Actor.Map.DrawCircle(AI.Actor.WorldParent.RealPosition, DesiredRadius, Color.Gray);
            AI.Actor.Map.DrawArrow(AI.Actor.RealPosition, diffN, DesiredRadius, Color.Red);

            AI.Actor.Forward = Vector2.TransformNormal(AI.Actor.Forward, Matrix.CreateRotationZ(theta * (float)deltaTime.TotalSeconds));
            AI.Actor.Accelerate(AI.Actor.Forward);
        }
    }
}

//r x (o - r)