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

        protected override void SingleFire(Takai.Game.EntityInstance entity)
        {
            var origin = entity.Position + (entity.Direction * (entity.Radius + Projectile.Radius + 1));

            //todo: merge projectiles with this

            //trace fire
            if (Speed == 0)
            {
                bool didHit = entity.Map.TraceLine(origin, entity.Direction, out var hit, Projectile.Range);
                if (didHit)
                {
                    var hitPos = origin + (entity.Direction * hit.distance);

                    var particles = new Takai.Game.ParticleSpawn()
                    {
                        type = Projectile.explosion,
                        count = 20,
                        lifetime = new Takai.Game.Range<System.TimeSpan>(System.TimeSpan.FromSeconds(1), System.TimeSpan.FromSeconds(2)),
                        position = hitPos
                    };
                    var normal = hit.entity != null ? Vector2.Normalize(hitPos - hit.entity.Position) : (Vector2.Zero - entity.Direction);
                    //normal = Vector2.Reflect(Direction, normal);
                    var angle = (float)System.Math.Atan2(normal.Y, normal.X);
                    particles.angle = new Takai.Game.Range<float>(angle - 0.75f, angle + 0.75f);

                    entity.Map.Spawn(particles);
                    entity.Map.DrawLine(entity.Position, hitPos, Color.NavajoWhite);

                    var actor = hit.entity as Entities.Actor;
                    if (actor != null)
                    {
                        actor.CurrentHealth -= Projectile.damage;
                    }
                }
                else
                {
                    entity.Map.DrawLine(entity.Position, entity.Position + entity.Direction * (Projectile.Range == 0 ? 100000 : Projectile.Range), Color.NavajoWhite);
                    //explosion at the end
                }
            }
            else
                entity.Map.Spawn(Projectile, origin, entity.Direction, (AddVelocity ? entity.Velocity : Vector2.Zero) + (entity.Direction * Speed));
        }
    }
}

/*todo:
    Muzzle blast
    Smoke trail (for trace fire)

*/