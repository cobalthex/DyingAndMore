using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Components
{
    class Weapon : Takai.Game.Component
    {
        /// <summary>
        /// Maximum allowed shots per single trigger (0 for none -- full auto)
        /// </summary>
        public int maxShots;
        public TimeSpan shotDelay;
        
        /// <summary>
        /// Speed of the projectile, 0 for a hitscan weapon
        /// </summary>
        public float speed;
        /// <summary>
        /// The projectile to fire. Also determines damage
        /// </summary>
        public Entities.Projectile template;

        protected TimeSpan lastShot;
        protected int shotsTaken;
        protected bool isFiring = false;

        /// <summary>
        /// Fire the weapon
        /// </summary>
        /// <remarks>Will only fire if firing conditions are met</remarks>
        public void Fire()
        {
            isFiring = true;
        }

        public override void Think(GameTime Time)
        {
            if (!isFiring)
            {
                shotsTaken = 0;
                return;
            }

            if ((maxShots == 0 || shotsTaken < maxShots) && Time.TotalGameTime > lastShot + shotDelay)
            {
                shotsTaken++;
                lastShot = Time.TotalGameTime;

                Entity.Map.SpawnEntity
                (
                    template, Entity.Position + (Entity.Direction * (Entity.Radius + template.Radius + 1)),
                    Entity.Direction,
                    Entity.Direction * speed
                );
            }
            isFiring = false;
        }
    }
}
