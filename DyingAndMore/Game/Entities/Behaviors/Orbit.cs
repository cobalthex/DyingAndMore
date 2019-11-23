using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Behaviors
{
    public class OrbitBehavior : Behavior
    {
        public float DesiredRadius { get; set; } = 100; // include parent and self radius in this distance? (dist between edges or dist between centers)

        public override BehaviorMask Mask => BehaviorMask.Movement;

        public override BehaviorFilters Filter => BehaviorFilters.RequiresParent;

        public float Strength { get; set; } = 10;
        public float Radius { get; set; } = 200;

        public override BehaviorPriority CalculatePriority()
        {
            return BehaviorPriority.Normal;
        }

        public override void Think(TimeSpan deltaTime)
        {
            var currentRadius = AI.Actor.Position.Length(); //assumes position relative to parent
            var currentTheta = Takai.Util.Angle(AI.Actor.Position);
            var direction = 1; //positive for counterclockwise, negative for clockwise
            //todo: calculate direction based on forward

            var nextTheta = AI.Actor.Velocity.Length() / currentRadius; //do some arc math ?

            var newForward = Takai.Util.Direction(nextTheta);
            //AI.Actor.Forward = newForward;
        }
    }
}
