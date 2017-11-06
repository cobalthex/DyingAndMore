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
        public float MuzzleVelocity { get; set; } = 400;

        /// <summary>
        /// The amount of power this projectile has. Determines speed and therefore damage
        /// </summary>
        public float Power { get; set; } = 100;

        /// <summary>
        /// How far this shot will go before destroying itself
        /// </summary>
        /// <remarks>Use zero for infinity</remarks>
        public float Range { get; set; } = 0;

        /// <summary>
        /// Allow this projectile to damage the creator of this projectile
        /// </summary>
        public bool CanDamageSource { get; set; } = false;

        public ProjectileClass()
        {
            DestroyIfInactive = true;
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
            if (Velocity.LengthSquared() < 0.001f ||
                (_class.Range != 0 && Vector2.DistanceSquared(origin, Position) > _class.Range * _class.Range))
                KillSelf();

            base.Think(DeltaTime);
        }

        public override void OnSpawn()
        {
            origin = Position;
        }

        public override void OnMapCollision(Point tile, Vector2 point, TimeSpan deltaTime)
        {
            KillSelf();
        }

        public override void OnEntityCollision(EntityInstance collider, Vector2 point, TimeSpan deltaTime)
        {
            KillSelf();
            Takai.LogBuffer.Append(ToString() + " " + collider.ToString());

            if (collider is ActorInstance actor &&
                (collider != Source || _class.CanDamageSource))
                actor.ReceiveDamage((int)_class.Power, Source);
        }
    }
}
