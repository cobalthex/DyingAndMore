using System;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// A fixed placement that will fire at nearby enemies (actors of different factions)
    /// </summary>
    class ArtilleryController : Controller
    {
        /// <summary>
        /// Maximum search range to find actors
        /// </summary>
        public float MaxRange { get; set; } = 200;
        /// <summary>
        /// Can this artillery rotate to face actors? (Affected by this actor's field of view)
        /// </summary>
        public bool CanRotate { get; set; } = false;

        /// <summary>
        /// the actor that this actor is tracking to shoot at
        /// </summary>
        public ActorInstance trackedActor;

        public override void Think(TimeSpan deltaTime)
        {
            if (trackedActor != null)
            {
                if (Microsoft.Xna.Framework.Vector2.DistanceSquared(actor.Position, trackedActor.Position) <= MaxRange * MaxRange)
                {
                    if (CanRotate)
                        actor.Direction = trackedActor.Position - actor.Position; //todo: slerp
                    actor.FireWeapon();
                    actor.Map.DrawLine(actor.Position, actor.Position + actor.Direction * MaxRange, Microsoft.Xna.Framework.Color.Orange);
                }
                else
                    trackedActor = null;
            }
            else
            {
                var ents = actor.Map.FindEntities(actor.Position, MaxRange);
                foreach (var ent in ents)
                {
                    if (ent is ActorInstance nearbyActor && (nearbyActor.Faction & actor.Faction) == 0 &&
                        actor.CanSee(nearbyActor.Position))
                    {
                        trackedActor = nearbyActor;
                        break;
                    }
                }
            }
        }
    }
}
