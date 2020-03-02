using System;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    public class WeaponPickupClass : EntityClass
    {
        public Weapons.WeaponClass Weapon { get; set; }

        /// <summary>
        /// Respawn time. Cooresponds with <see cref="EntityClass.DestroyOnDeath"/>
        /// </summary>
        public TimeSpan RespawnTime { get; set; } = TimeSpan.Zero;

        public bool OnlyDestroyWhenDepleted { get; set; }

        public override EntityInstance Instantiate()
        {
            return new WeaponPickupInstance(this);
        }
    }

    public class WeaponPickupInstance : EntityInstance
    {
        /// <summary>
        /// The weapon to pick up.
        /// Will instantiate a new one from the class if null
        /// </summary>
        public Weapons.WeaponInstance Weapon
        {
            get => _weapon ?? (_weapon = _Class?.Weapon?.Instantiate()) ?? null;
            set => _weapon = value;
        }
        private Weapons.WeaponInstance _weapon;

        [Takai.Data.Serializer.Ignored]
        public WeaponPickupClass _Class
        {
            get => (WeaponPickupClass)base.Class;
            set => base.Class = value;
        }

        public TimeSpan NextRespawnTime { get; set; }

        public WeaponPickupInstance() : this(null) { }
        public WeaponPickupInstance(WeaponPickupClass @class)
            : base(@class)
        {
        }

        public override void Think(TimeSpan deltaTime)
        {
            if (!IsAlive && Map.ElapsedTime > NextRespawnTime)
                Resurrect();

            base.Think(deltaTime);
        }

        /// <summary>
        /// Apply to the existing weapon or clone and return a new weapon
        /// </summary>
        /// <param name="weapon">The original weapon to compare against</param>
        /// <returns>THe original weapon or a cloned weapon</returns>
        public Weapons.WeaponInstance ApplyTo(Weapons.WeaponInstance weapon)
        {
            if (!IsAlive)
                return weapon;

            var thisWeapon = Weapon;
            if (thisWeapon == null)
                return weapon;

            if (weapon != null && weapon.Class == thisWeapon.Class)
            {
                weapon.Combine(thisWeapon);
                if (!_Class.OnlyDestroyWhenDepleted || thisWeapon.IsDepleted())
                {
                    Weapon = null;
                    NextRespawnTime = Map.ElapsedTime + _Class.RespawnTime;
                    Kill();
                }
            }
            else
            {
                weapon = thisWeapon.Clone();
                Weapon = null;
                NextRespawnTime = Map.ElapsedTime.Add(_Class.RespawnTime);
                Kill();
            }

            return weapon;
        }
    }
}
