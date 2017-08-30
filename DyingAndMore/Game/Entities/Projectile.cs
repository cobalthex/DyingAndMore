using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    class ProjectileClass : EntityClass
    {
        /// <summary>
        /// The amount of power this projectile has. Determines speed and therefore damage
        /// </summary>
        public float Power { get; set; } = 100;

        /// <summary>
        /// How far this shot will go before destroying itself
        /// </summary>
        /// <remarks>Use zero for infinite</remarks>
        public float Range { get; set; } = 0;

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
                _Class = value as ProjectileClass;
            }
        }
        private ProjectileClass _Class;

        public ProjectileInstance() { }
        public ProjectileInstance(ProjectileClass @class)
            : base(@class)
        {
            
        }

        public override void Think(TimeSpan DeltaTime)
        {
            //todo: destroy if 0 velocity

            base.Think(DeltaTime);
        }

        public override void OnSpawn()
        {
        }

        public override void OnMapCollision(Point tile, Vector2 point, System.TimeSpan deltaTime)
        {
            Map.Destroy(this);
        }

        public override void OnEntityCollision(EntityInstance collider, Vector2 point, System.TimeSpan deltaTime)
        {
            State.TransitionTo(EntStateId.Dead, "Dead"); //todo

            if (collider is ActorInstance actor)
            {
                actor.CurrentHealth -= (int)_Class.Power;//todo
            }
        }
    }
}
