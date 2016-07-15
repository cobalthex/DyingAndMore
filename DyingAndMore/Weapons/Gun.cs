using Microsoft.Xna.Framework;

namespace DyingAndMore.Weapons
{
    class Gun : Weapon
    {
        /// <summary>
        /// The projectile to fire
        /// </summary>
        /// <remarks>The projectile should already be loaded</remarks>
        public Entities.Projectile projectile = null;
        /// <summary>
        /// The initial speed of the projectile
        /// </summary>
        public float speed = 1;

        protected override void SingleFire(Takai.Game.Entity Entity)
        {
            var pos = Entity.Position + (Entity.Direction * (Entity.Radius + projectile.Radius + 1));
            Entity.Map.SpawnEntity(projectile, pos, Entity.Direction, Entity.Velocity + (Entity.Direction * speed), false);
        }
    }
}
