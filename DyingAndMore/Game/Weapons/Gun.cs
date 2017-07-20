using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Weapons
{
    enum OverchargeAction
    {
        None,
        Discharge,
        Explode,
    }

    class GunClass : WeaponClass
    {
        public Entities.ProjectileClass Projectile { get; set; }

        public int MaxAmmo { get; set; } = 100;

        //clip size, shots reloaded per load

        public int BurstCont { get; set; } = 0;
        public int ShotsPerBurst { get; set; } = 1;

        public Takai.Game.Range<float> ErrorAngle { get; set; }

        public OverchargeAction OverchargeAction { get; set; } = OverchargeAction.Discharge;

        //spew (how long continuous fire after overcharge)

        //bloom (error angle increases over time)

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

        protected override void Discharge(Takai.Game.EntityInstance source)
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


//weak events for states
//weapons weak subscribe to charge/discharge events
//one time events?