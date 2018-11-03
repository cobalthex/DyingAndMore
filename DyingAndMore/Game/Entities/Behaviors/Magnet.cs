using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities.Behaviors
{
    public class MagnetBehavior : Behavior
    {
        public bool IsEnabled = true;

        public override BehaviorMask Mask => BehaviorMask.Movement;

        public override BehaviorFilters Filter => BehaviorFilters.RequiresTarget;

        public override BehaviorPriority CalculatePriority()
        {
            if (IsEnabled)
                return BehaviorPriority.Normal;

            return BehaviorPriority.Never;
        }

        public override void Think(TimeSpan deltaTime)
        {
            var ents = AI.Actor.Map.FindEntitiesInRegion(AI.Actor.Position, 100);
            foreach (var ent in ents)
            {
                if (ent == AI.Actor || !(ent is ActorInstance actor))
                    continue;

                var rad2 = Vector2.DistanceSquared(AI.Actor.Position, actor.Position);
                actor.Accelerate(Vector2.Normalize(AI.Actor.Position - actor.Position) * (1 / rad2));
            }
        }
    }
}
