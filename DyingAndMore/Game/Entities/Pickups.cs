using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    public class WeaponPickupClass : EntityClass
    {
        /// <summary>
        /// The weapon to pick up. Actual instance as to store ammo, etc
        /// </summary>
        public Weapons.WeaponInstance Weapon { get; set; }

        public override EntityInstance Instantiate()
        {
            return new WeaponPickupInstance(this);
        }
    }

    public class WeaponPickupInstance : EntityInstance
    {
        [Takai.Data.Serializer.ReadOnly]
        public new WeaponPickupClass Class
        {
            get => (WeaponPickupClass)base.Class;
            set => base.Class = value;
        }

        public WeaponPickupInstance() : this(null) { }
        public WeaponPickupInstance(WeaponPickupClass @class)
            : base(@class)
        {
        }

    }
}
