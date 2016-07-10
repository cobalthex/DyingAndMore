using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Weapons
{
    /// <summary>
    /// The base for a weapon
    /// </summary>
    abstract class Weapon
    {
        /// <summary>
        /// When the last shot was taken
        /// </summary>
        protected TimeSpan lastShot = TimeSpan.Zero;
        /// <summary>
        /// The number of consecutive shots taken
        /// </summary>
        protected int shotsTaken = 0;

        /// <summary>
        /// How long to delay between each shot
        /// </summary>
        public TimeSpan shotDelay = TimeSpan.FromMilliseconds(100);
        /// <summary>
        /// The maximum allowed consecutive shots
        /// </summary>
        public int maxShots = 0;

        /// <summary>
        /// Attempt to fire the weapon
        /// Fires in the 
        /// </summary>
        /// <param name="Entity">The entity to fire from</param>
        /// <remarks>Fires from the owner entity's position in their forward direction</remarks>
        public virtual void Fire(GameTime Time, Takai.Game.Entity Entity)
        {
            if (CanFire(Time))
            {
                SingleFire(Entity);
                lastShot = Time.TotalGameTime;
                shotsTaken++;
            }
        }

        /// <summary>
        /// Is the weapon currently able to fire?
        /// </summary>
        /// <param name="Time">The current time</param>
        /// <returns>True if able to fire</returns>
        public virtual bool CanFire(GameTime Time)
        {
            return (maxShots == 0 || shotsTaken < maxShots) && Time.TotalGameTime > lastShot + shotDelay;
        }

        /// <summary>
        /// Fire a single shot
        /// </summary>
        /// <param name="Entity">The entity to fire from</param>
        /// <remarks>Unaffected by firing conditions</remarks>
        protected abstract void SingleFire(Takai.Game.Entity Entity);
    }
}
