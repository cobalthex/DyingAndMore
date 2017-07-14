using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Weapons
{
    class GunClass : WeaponClass
    {
        public Entities.ProjectileClass Projectile { get; set; }

        public int MaxAmmo { get; set; } = 100;

        public override WeaponInstance Create()
        {
            return new GunInstance(this);
        }
    }

    class GunInstance : WeaponInstance
    {
        public override WeaponClass Class
        {
            get => base.Class;
            set
            {
                System.Diagnostics.Contracts.Contract.Assert(value is GunClass);
                base.Class = value;
                _Class = value as GunClass;
            }
        }
        private GunClass _Class;

        public int CurrentAmmo { get; set; }

        public GunInstance() { }
        public GunInstance(GunClass @class)
            : base(@class)
        {
            CurrentAmmo = @class.MaxAmmo;
        }

        public override bool IsDepleted()
        {
            return CurrentAmmo <= 0;
        }

        protected override void Use(Takai.Game.EntityInstance source)
        {
            if (source.Map == null)
                return;

            var speed = _Class.Projectile.Power;
            source.Map.Spawn(_Class.Projectile, source.Position + source.Direction * (source.Radius + 10), source.Direction, source.Direction * speed);
        }
    }
}

/*todo:
    Muzzle blast
    Smoke trail (for trace fire)

*/