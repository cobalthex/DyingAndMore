using System;
using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// Allows for modifying the direction of a traveling object
    /// </summary>
    /// <param name="distance">How far the projectile has traveled since it spawned</param>
    /// <param name="direction">The current direction of the projectile</param>
    /// <returns>The new direction of the object</returns>
    public delegate Vector2 DirectionModifier(float distance, Vector2 direction);

    //move projectiles to actors or even generic entities (behaviors for projectile specifics) ?

    public class ProjectileClass : EntityClass
    {
        public static DirectionModifier WaveDirectionMod => delegate (float distance, Vector2 direction)
        {
            var tangent = (float)Math.Sin(distance / 100000) * MathHelper.PiOver2;
            return Vector2.TransformNormal(direction, Matrix.CreateRotationZ(tangent));
        };

        /// <summary>
        /// Initial speed of the projectile
        /// </summary>
        public Takai.Range<float> MuzzleVelocity { get; set; } = 100;

        /// <summary>
        /// How much damage this projectile will inflict upon an emeny
        /// </summary>
        public float Damage { get; set; } = 100; //todo: scale by speed?

        /// <summary>
        /// How far this shot will go before killing itself
        /// </summary>
        /// <remarks>Use zero for infinity</remarks>
        public float Range { get; set; } = 0;

        /// <summary>
        /// How slow this entity can go before killing itself. Can go negative
        /// </summary>
        public float MinimumSpeed { get; set; } = 1;

        /// <summary>
        /// How long this projectile will last before killing itself
        /// </summary>
        /// <remarks>Use zero for infinity</remarks>
        public TimeSpan LifeSpan { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Allow this projectile to damage the creator of this projectile
        /// </summary>
        public bool CanDamageSource { get; set; } = false;

        /// <summary>
        /// When spawning this projectile, use the creator of this projectile's physics info (velocity/etc)
        /// </summary>
        public bool InheritSourcePhysics { get; set; } = false;

        /// <summary>
        /// An effect spawned when the projectile goes out of <see cref="Range"/>, lives longer than <see cref="LifeSpan"/>, or below the <see cref="MinimumSpeed"/>
        /// </summary>
        public EffectsClass FadeEffect { get; set; }

        /// <summary>
        /// Magnetism towards actors
        /// </summary>
        public float MagnetismAnglePerSecond { get; set; }

        /// <summary>
        /// A modifier to modify the direction of the projectile as it travels
        /// </summary>
        public DirectionModifier DirectionMod { get; set; } = null;

        public ProjectileClass()
        {
            DestroyIfOffscreen = true;
        }

        public override EntityInstance Instantiate()
        {
            return new ProjectileInstance(this);
        }
    }

    public class ProjectileInstance : EntityInstance
    {
        [Takai.Data.Serializer.ReadOnly]
        public new ProjectileClass Class
        {
            get => (ProjectileClass)base.Class;
            set => base.Class = value;
        }

        /// <summary>
        /// Who created this projectile
        /// </summary>
        public EntityInstance Source { get; set; }

        /// <summary>
        /// The current actor for magnetism. Reset if the actor is destroyed or the projectile has passed the actor
        /// </summary>
        public ActorInstance CurrentMagnet { get; set; }

        /// <summary>
        /// Where the projectile was spawned
        /// </summary>
        protected Vector2 origin; //origin angle?

        protected TimeSpan nextTargetSearchTime;

        public ProjectileInstance() { }
        public ProjectileInstance(ProjectileClass @class)
            : base(@class) { }

        public override void Think(TimeSpan deltaTime)
        {
            var cdist = Vector2.Distance(origin, Position);

            if (IsAlive &&
                (ForwardSpeed() < Class.MinimumSpeed ||
                (Class.LifeSpan > TimeSpan.Zero && Map.ElapsedTime > SpawnTime + Class.LifeSpan) ||
                (Class.Range != 0 && cdist > Class.Range)))
            {
                DisableNextDestructionEffect = true;
                if (Class.FadeEffect != null)
                {
                    var fx = Class.FadeEffect.Instantiate(this);
                    Map.Spawn(fx);
                }
                Kill();

                //todo: move to collision fx and destruction fx?
            }
            else if (IsAlive)
            {
                if (CurrentMagnet != null)
                {
                    var diff = CurrentMagnet.Position - Position;

                    if (CurrentMagnet.IsAliveIn(Map) && Vector2.Dot(diff, Forward) > 0)
                    {
                        //todo: pid controller

                        var sign = Takai.Util.Determinant(Forward, Vector2.Normalize(diff));

                        Forward = Vector2.TransformNormal(Forward, Matrix.CreateRotationZ(sign * Class.MagnetismAnglePerSecond * (float)deltaTime.TotalSeconds));
                        Velocity = Forward * Velocity.Length();
                    }
                    else
                        CurrentMagnet = null;
                }
                else if (Class.MagnetismAnglePerSecond != 0 && Map.RealTime.TotalGameTime >= nextTargetSearchTime)
                {
                    CurrentMagnet = FindTarget();
                    nextTargetSearchTime = Map.RealTime.TotalGameTime + TimeSpan.FromMilliseconds(50); //store delay in editor config?
                }

                if (Class.DirectionMod != null)
                {
                    var newDirection = Class.DirectionMod(cdist, Forward);
                    Forward = newDirection;
                    Velocity = Forward * Velocity.Length();
                }
            }

            base.Think(deltaTime);
        }

        public ActorInstance FindTarget()
        {
            //search in line
            var minDot = 1f;
            var minDist = float.PositiveInfinity;
            ActorInstance best = null;

            var sourceFaction = Source is ActorInstance sourceActor ? sourceActor.Factions : Factions.None;

            int n = 0;
            //search out in triangle?
            foreach (var sector in Map.TraceSectors(Position, Forward, Class.Range))
            {
                ++n;
                foreach (var ent in sector.entities)
                {
                    if (ent != this && ent is ActorInstance actor && !actor.IsAlliedWith(sourceFaction))
                    {
                        var diff = ent.Position - Position;
                        var length = diff.Length();
                        var norm = diff / length;
                        var dot = Vector2.Dot(norm, Forward);

                        if (dot <= 0)
                            continue;

                        if (length < minDist || (length == minDist && dot < minDot))
                        {
                            best = actor;
                            minDot = dot;
                            minDist = length;
                        }
                    }
                }

                if (best != null)
                    break;
            }

            return best;
        }

        public override void OnSpawn(MapBaseInstance map)
        {
            origin = Position;
            if (Class.InheritSourcePhysics && Source != null) //todo: this should be part of entity spawning?
            {
                Velocity += Source.Velocity;
            }
            base.OnSpawn(map);
        }

        public override void OnMapCollision(Point tile, Vector2 point, TimeSpan deltaTime)
        {
            Kill();
        }

        public override void OnEntityCollision(EntityInstance collider, CollisionManifold collision, TimeSpan deltaTime)
        {
            //if (collider.Material != null &&
            //    Material != null &&
            //    Class.MaterialResponses != null && Class.MaterialResponses.TryGetValue(collider.Material, out var mtl))
            //{
            //    //collision angle, collision depth, etc
            //}
            //else
            {
                Velocity = Vector2.Zero; //todo: physics should handle this
                //Kill();
            }

            if (collider is ActorInstance actor &&
                (collider != Source || Class.CanDamageSource))
                actor.ReceiveDamage(Class.Damage, Source);
        }
    }
}
