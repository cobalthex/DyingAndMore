using System;
using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    class ProjectileClass : EntityClass
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
        /// An effect spawned when the projectile goes out of <see cref="Range"/>, lives longer than <see cref="LifeSpan"/>, or below the <see cref="MinimumSpeed"/>
        /// </summary>
        public EffectsClass FadeEffect { get; set; }

        public ProjectileClass()
        {
            DestroyIfOffscreen = true;
        }

        public override EntityInstance Create()
        {
            return new ProjectileInstance(this);
        }
    }

    class ProjectileInstance : EntityInstance
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

        public override void OnEntityCollision(EntityInstance collider, Vector2 point, TimeSpan deltaTime)
        {
            IsAlive = false;

            if (collider is ActorInstance actor &&
                (collider != Source || _class.CanDamageSource))
                actor.ReceiveDamage((int)_class.Power, Source);
        }
    }
}
