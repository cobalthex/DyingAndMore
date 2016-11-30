using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Weapons
{
    class Gun : Weapon
    {
        /// <summary>
        /// The projectile to fire
        /// </summary>
        /// <remarks>The projectile should already be loaded</remarks>
        public Entities.Projectile Projectile { get; set; } = null;

        /// <summary>
        /// The initial speed of the projectile
        /// </summary>
        /// <remarks>If the speed is zero, the weapon is fired as a trace (AddVelocity is ignored)</remarks>
        public float Speed { get; set; } = 1;

        /// <summary>
        /// Add the entity's velocity to the velocity of this projectile
        /// </summary>
        public bool AddVelocity { get; set; } = false;

        protected override void SingleFire(Takai.Game.Entity Entity)
        {
            var origin = Entity.Position + (Entity.Direction * (Entity.Radius + Projectile.Radius + 1));
            
            //trace fire
            if (Speed == 0)
            {
                bool didHit = Entity.Map.TraceLine(origin, Entity.Direction, out var hit, Projectile.Range);
                if (didHit)
                {
                    var hitPos = origin + (Entity.Direction * hit.distance);

                    var particles = new Takai.Game.ParticleSpawn()
                    {
                        type = Projectile.explosion,
                        count = 20,
                        lifetime = new Takai.Game.Range<System.TimeSpan>(System.TimeSpan.FromSeconds(1), System.TimeSpan.FromSeconds(2)),
                        position = hitPos
                    };
                    var normal = hit.entity != null ? Vector2.Normalize(hitPos - hit.entity.Position) : (Vector2.Zero - Entity.Direction);
                    //normal = Vector2.Reflect(Direction, normal);
                    var angle = (float)System.Math.Atan2(normal.Y, normal.X);
                    particles.angle = new Takai.Game.Range<float>(angle - 0.75f, angle + 0.75f);

                    Entity.Map.Spawn(particles);
                    Entity.Map.DrawLine(Entity.Position, hitPos, Color.NavajoWhite);
                }
                else
                {
                    //explosion at the end
                }
            }
            else
                Entity.Map.Spawn(Projectile, origin, Entity.Direction, (AddVelocity ? Entity.Velocity : Vector2.Zero) + (Entity.Direction * Speed));
        }
    }
}
