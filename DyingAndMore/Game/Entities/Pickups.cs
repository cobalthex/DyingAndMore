using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    public class WeaponPickupClass : EntityClass
    {
        public Weapons.WeaponClass Weapon { get; set; }

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
            get => _weapon ?? (_weapon = Class?.Weapon?.Instantiate()) ?? null;
            set => _weapon = value;
        }
        private Weapons.WeaponInstance _weapon;

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
