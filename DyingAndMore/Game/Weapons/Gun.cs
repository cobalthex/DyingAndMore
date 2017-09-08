using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Weapons
{
    class GunClass : WeaponClass
    {
        public Entities.ProjectileClass Projectile { get; set; }

        public int MaxAmmo { get; set; } = 100;

        //clip size, shots reloaded per load

        public int BurstCont { get; set; } = 0;
        public int ShotsPerBurst { get; set; } = 1;

        public Takai.Game.Range<float> ErrorAngle { get; set; }

        //spew (how long continuous fire after overcharge)

        //bloom (error angle increases over time)

        //todo: give states names, instances use ids set to a specific state name in class

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
                System.Diagnostics.Contracts.Contract.Assert(value == null || value is GunClass);
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

        public override void Discharge()
        {
            //todo: optimize for cases with no charging (no need to use state machine)

            if (Actor.Map == null || Actor.State.State != Takai.Game.EntStateId.ChargeWeapon)
                return;

            //undercharged
            if (!Actor.State.Instance.HasFinished() &&
                _Class.UnderchargeAction == UnderchargeAction.Dissipate)
                return;

            var projectile = (Entities.ProjectileInstance)_Class.Projectile.Create();
            projectile.Position = Actor.Position + (Actor.Forward * (Actor.Radius + projectile.Radius + 2));
            projectile.Forward = Actor.Forward;
            projectile.Velocity = Actor.Forward * _Class.Projectile.Power;
            projectile.Source = Actor;
            Actor.Map.Spawn(projectile);

            base.Discharge();
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