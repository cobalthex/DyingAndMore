using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities
{
    //todo: magnetism (how fast will track)

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
            if (actor.State.State != Takai.Game.EntStateId.Idle)
                return; //todo: shoot with moving barrel?

            if (actor.State.State == Takai.Game.EntStateId.Idle && actor.Weapon.IsDepleted())
            {
                actor.State.TransitionTo(Takai.Game.EntStateId.Idle, Takai.Game.EntStateId.Inactive, "Inactive");
                return;
            }

            if (trackedActor != null)
            {
                //todo: find more efficient test if possible
                var filter = new System.Collections.Generic.SortedDictionary<float, Takai.Game.EntityInstance>
                {
                    { Vector2.DistanceSquared(actor.Position, trackedActor.Position), trackedActor }
                };
                //if (actor.Map.TraceLine(actor.Position, actor.Direction, out var hit, MaxRange, filter))
                //{
                //    //todo: maybe shoot w/ lead (shoot out in front of target's velocity)
                //    if (CanRotate)
                //        actor.Direction = Vector2.Normalize(trackedActor.Position - actor.Position); //todo: slerp
                //    actor.FireWeapon();
                //    actor.Map.DrawLine(actor.Position, actor.Position + actor.Direction * MaxRange, Color.Orange);
                //}
                //else
                //    trackedActor = null;
            }
            else
            {
                var ents = actor.Map.FindEntities(actor.Position, MaxRange);
                foreach (var ent in ents)
                {
                    if (ent != actor && ent is ActorInstance nearbyActor && (nearbyActor.Faction & actor.Faction) == 0 &&
                        actor.IsFacing(nearbyActor.Position))
                    {
                        trackedActor = nearbyActor;
                        break;
                    }
                }
            }
        }
    }
}
