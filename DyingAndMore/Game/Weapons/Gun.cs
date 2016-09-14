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
        public float Speed { get; set; } = 1;

        /// <summary>
        /// Add the entity's velocity to the velocity of this projectile
        /// </summary>
        public bool AddVelocity { get; set; } = false;

        protected override void SingleFire(Takai.Game.Entity Entity)
        {
            var pos = Entity.Position + (Entity.Direction * (Entity.Radius + Projectile.Radius + 1));
            Entity.Map.Spawn(Projectile, pos, Entity.Direction, (AddVelocity ? Entity.Velocity : Vector2.Zero) + (Entity.Direction * Speed));
        }
    }
}
