﻿using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Weapons
{
    /// <summary>
    /// The base for all weapons
    /// </summary>
    [Takai.Data.DesignerModdable]
    abstract class WeaponClass : Takai.IObjectClass<WeaponInstance>
    {
        public string Name { get; set; }

        [Takai.Data.Serializer.Ignored]
        public string File { get; set; } = null;

        /// <summary>
        /// How long to delay between each shot (random)
        /// </summary>
        public Takai.Game.Range<TimeSpan> Delay { get; set; } = TimeSpan.FromMilliseconds(100);

        /*
        /// <summary>
        /// The maximum allowed consecutive shots
        /// 0 for unlimited
        /// </summary>
        public int MaxBurst { get; set; } = 0;
        */

        public abstract WeaponInstance Create();
    }
    abstract class WeaponInstance : Takai.IObjectInstance<WeaponClass>
    {
        public virtual WeaponClass Class { get; set; }

        /// <summary>
        /// When the next shot can be taken (calculated as shot time + random shot delay)
        /// </summary>
        public TimeSpan nextShot = TimeSpan.Zero;

        /// <summary>
        /// The number of consecutive shots taken
        /// </summary>
        //public int burstCount = 0;

        public WeaponInstance() { }
        public WeaponInstance(WeaponClass @class)
        {
            Class = @class;
        }

        /// <summary>
        /// Attempt to fire the weapon
        /// </summary>
        /// <param name="source">The entity to fire from</param>
        /// <remarks>Fires from the owner entity's position in their forward direction</remarks>
        public virtual void TryUse(Takai.Game.EntityInstance source)
        {
            if (CanUse(source.Map.ElapsedTime))
            {
                Use(source);
                nextShot = source.Map.ElapsedTime + Takai.Game.RandomRange.Next(Class.Delay);
            }
        }

        public virtual bool CanUse(TimeSpan totalTime)
        {
            return !IsDepleted() && totalTime > nextShot;
        }

        /// <summary>
        /// Is the weapon completely depleted? (should be permanent)
        /// </summary>
        /// <returns>True if the weapon is depleted, false otherwise</returns>
        public abstract bool IsDepleted();

        /// <summary>
        /// Fire a single shot, even if CanFire returns false
        /// </summary>
        /// <param name="Entity">The entity to fire from</param>
        /// <remarks>Unaffected by firing conditions</remarks>
        protected abstract void Use(Takai.Game.EntityInstance entity);

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
