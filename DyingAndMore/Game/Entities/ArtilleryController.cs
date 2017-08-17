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

        public bool ShowSweepLines { get; set; } = true;

        protected int sweepLines = 6;

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
                var hit = actor.Map.Trace(actor.Position, actor.Direction, MaxRange);

                if (hit.entity != null)
                {
                    //todo: maybe shoot w/ lead (shoot out in front of target's velocity)
                    if (CanRotate)
                        actor.Direction = Vector2.Normalize(trackedActor.Position - actor.Position); //todo: slerp
                    actor.FireWeapon();
                    actor.Map.DrawLine(actor.Position, actor.Position + actor.Direction * MaxRange, Color.Orange);
                }
                else
                    trackedActor = null;
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

                //scanning rays
                if (ShowSweepLines)
                {
                    void ScanRay(Vector2 direction, Color color)
                    {
                        var hit = actor.Map.Trace(actor.Position, direction, MaxRange);
                        actor.Map.DrawLine(actor.Position, actor.Position + direction * hit.distance, color);
                    }
                    Vector2 TransformDirection(float radians)
                    {
                        return Vector2.TransformNormal(
                            actor.Direction,
                            Matrix.CreateRotationZ(radians)
                        );
                    }

                    var fov = ((ActorClass)actor.Class).FieldOfView / 2;

                    //edges
                    ScanRay(TransformDirection(-fov), Color.Aquamarine);
                    ScanRay(TransformDirection( fov), Color.Aquamarine);

                    //how far apart the sweep lines are
                    //maintain a consistent angle regardless of fov
                    var phase = (MathHelper.Pi / 24) / fov;

                    //todo: consistent speed regardless of fov

                    for (int i = 0; i < sweepLines; ++i)
                    {
                        var step = (float)Math.Sin(
                            (actor.Map.ElapsedTime.TotalSeconds * 2)
                            - (phase * i)
                        ) * fov;

                        ScanRay(TransformDirection(step), Color.MediumAquamarine);
                    }
                }
            }
        }
    }
}
