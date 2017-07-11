using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    class ProjectileClass : EntityClass
    {
        public ParticleType explosion;
        public ParticleType trail, trailGlow;

        public int damage = 20;

        /// <summary>
        /// How far this shot will go before destroying itself
        /// </summary>
        /// <remarks>Use zero for infinite</remarks>
        public float Range { get; set; } = 0;

        public override EntityInstance Create()
        {
            return new ProjectileInstance();
        }
    }

    class ProjectileInstance : EntityInstance
    {
        public override EntityClass Class
        {
            get => base.Class;
            set
            {
                System.Diagnostics.Contracts.Contract.Assert(value is ProjectileClass);
                base.Class = value;
                _Class = value as ProjectileClass;

                //apply defaults
            }
        }
        private ProjectileClass _Class;

        System.TimeSpan flipTime;
        public override void Think(System.TimeSpan DeltaTime)
        {
            ParticleSpawn spawn = new ParticleSpawn()
            {
                type = _Class.trail,
                position = new Range<Vector2>(Position - (Direction * Radius), Position - (Direction * Radius)),
                lifetime = System.TimeSpan.FromSeconds(0.25)
            };
            var angle = (float)System.Math.Atan2(Direction.Y, Direction.X);
            spawn.angle = new Range<float>(angle - MathHelper.PiOver2, angle + MathHelper.PiOver2);
            spawn.count = 1;
            Map.Spawn(spawn);

            spawn.type = _Class.trailGlow;
            spawn.position = new Range<Vector2>(Position - (Direction * Radius) - new Vector2(5), Position - (Direction * Radius) + new Vector2(5));
            Map.Spawn(spawn);

            //todo: destroy if 0 velocity

            base.Think(DeltaTime);
        }

        public override void OnSpawn()
        {
            flipTime = Map.ElapsedTime;
        }

        public override void OnMapCollision(Point tile, Vector2 point, System.TimeSpan deltaTime)
        {
            Map.Destroy(this);
        }

        public override void OnEntityCollision(EntityInstance collider, Vector2 point, System.TimeSpan deltaTime)
        {
            ParticleSpawn spawn = new ParticleSpawn()
            {
                type = _Class.explosion,
                count = 20,
                lifetime = new Range<System.TimeSpan>(System.TimeSpan.FromSeconds(1), System.TimeSpan.FromSeconds(2)),
                position = point
            };
            var normal = Vector2.Normalize(point - collider.Position);
            //normal = Vector2.Reflect(Direction, normal);
            var angle = (float)System.Math.Atan2(normal.Y, normal.X);
            spawn.angle = new Range<float>(angle - 0.75f, angle + 0.75f);

            Map.Spawn(spawn);
            Map.Destroy(this);

            if (collider is ActorInstance actor)
            {
                actor.CurrentHealth -= _Class.damage;
            }
        }
    }
}
