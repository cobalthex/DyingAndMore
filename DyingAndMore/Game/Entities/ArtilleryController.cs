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
            //todo: one-time animation play, then die

            if (Actor.Weapon.IsDepleted())
            {
                Actor.PlayAnimation("Inactive"); //todo: call once?
                return;
            }

            if (trackedActor != null)
            {
                Takai.Game.TraceHit hit;

                if (Actor.IsFacing(trackedActor.Position))
                    hit = Actor.Map.Trace(Actor.Position, Vector2.Normalize(trackedActor.Position - Actor.Position), MaxRange, Actor);
                else
                    hit = new Takai.Game.TraceHit();

                if (hit.entity != null)
                {
                    //todo: maybe shoot w/ lead (shoot towards target's next position)
                    if (CanRotate)
                        Actor.Forward = Vector2.Normalize(trackedActor.Position - Actor.Position); //todo: slerp
                    Actor.Weapon?.TryUse();
                    Actor.Map.DrawLine(Actor.Position, hit.entity.Position, Color.Orange);
                }
                else
                    trackedActor = null;
            }
            else
            {
                var ents = Actor.Map.FindEntitiesInRegion(Actor.Position, MaxRange);
                foreach (var ent in ents)
                {
                    if (ent != Actor && ent is ActorInstance nearbyActor && (nearbyActor.Factions & Actor.Factions) == 0 &&
                        Actor.IsFacing(nearbyActor.Position))
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
                        var hit = Actor.Map.Trace(Actor.Position, direction, MaxRange, Actor);
                        Actor.Map.DrawLine(Actor.Position, Actor.Position + direction * hit.distance, color);
                    }
                    Vector2 TransformDirection(float radians)
                    {
                        return Vector2.TransformNormal(
                            Actor.Forward,
                            Matrix.CreateRotationZ(radians)
                        );
                    }

                    var fov = ((ActorClass)Actor.Class).FieldOfView / 2;

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
                            (Actor.Map.ElapsedTime.TotalSeconds * 2)
                            - (phase * i)
                        ) * fov;

                        ScanRay(TransformDirection(step), Color.MediumAquamarine);
                    }
                }
            }
        }
    }
}
