using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    public class ProjectileResponse
    {
        /// <summary>
        /// If the angle of collision is within this range, the projectile will reflect
        /// </summary>
        /// <remarks>0 to 2pi radians, NaN not to reflect</remarks>
        public Range<float> ReflectAngle { get; set; } = float.NaN;
        public Range<float> ReflectSpeed { get; set; } = 0;

        //Overpenetrate (glass/breakable materials?) -- enemies
        //attach

        //friction, dampening

        //refraction (reflection offset jitter?)
    }

    public class ProjectileClass : EntityClass
    {
        /// <summary>
        /// Initial speed of the projectile
        /// </summary>
        public Range<float> MuzzleVelocity { get; set; } = 100;

        /// <summary>
        /// The amount of power this projectile has. Determines speed and therefore damage
        /// </summary>
        public float Power { get; set; } = 100;

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
        public bool UseSourcePhysics { get; set; } = false;

        /// <summary>
        /// An effect spawned when the projectile goes out of <see cref="Range"/>, lives longer than <see cref="LifeSpan"/>, or below the <see cref="MinimumSpeed"/>
        /// </summary>
        public EffectsClass FadeEffect { get; set; }

        public Dictionary<Material, ProjectileResponse> MaterialResponses { get; set; } = new Dictionary<Material, ProjectileResponse>();

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
        public override EntityClass Class
        {
            get => base.Class;
            set
            {
                System.Diagnostics.Contracts.Contract.Assert(value == null || value is ProjectileClass);
                base.Class = value;
                _class = value as ProjectileClass;
            }
        }
        private ProjectileClass _class;

        /// <summary>
        /// Who created this projectile
        /// </summary>
        public EntityInstance Source { get; set; }

        /// <summary>
        /// Where the projectile was spawned
        /// </summary>
        protected Vector2 origin; //origin angle?

        public ProjectileInstance() { }
        public ProjectileInstance(ProjectileClass @class)
            : base(@class)
        {

        }

        public override void Think(TimeSpan DeltaTime)
        {
            if (IsAlive &&
                (ForwardSpeed() < _class.MinimumSpeed ||
                (_class.LifeSpan > TimeSpan.Zero && Map.ElapsedTime > SpawnTime + _class.LifeSpan) ||
                (_class.Range != 0 && Vector2.DistanceSquared(origin, Position) > _class.Range * _class.Range)))
            {
                DisableNextDestructionEffect = true;
                if (_class.FadeEffect != null)
                {
                    var fx = _class.FadeEffect.Create(this);
                    Map.Spawn(fx);
                }
                IsAlive = false;

                //todo: move to collision fx and destruction fx?
            }

            base.Think(DeltaTime);
        }

        public override void OnSpawn()
        {
            origin = Position;
        }

        public override void OnMapCollision(Point tile, Vector2 point, TimeSpan deltaTime)
        {
            IsAlive = false;
        }

        public override void OnEntityCollision(EntityInstance collider, CollisionManifold collision, TimeSpan deltaTime)
        {
            if (collider.Material != null && _class.MaterialResponses.TryGetValue(collider.Material, out var mtl))
            {
                //collision angle, collision depth, etc
            }
            else
                IsAlive = false;

            if (collider is ActorInstance actor &&
                (collider != Source || _class.CanDamageSource))
                actor.ReceiveDamage((int)_class.Power, Source);
        }
    }
}
