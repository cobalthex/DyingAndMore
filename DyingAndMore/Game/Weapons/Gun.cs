using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Weapons
{
    class GunClass : WeaponClass
    {
        public Entities.ProjectileClass Projectile { get; set; }

        /// <summary>
        /// Maximum ammo count, use 0 for infinite
        /// </summary>
        public int MaxAmmo { get; set; } = 100;

        //clip size, shots reloaded per load

        public int BurstCount { get; set; } = 0;
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
                _class = value as GunClass;
            }
        }
        private GunClass _class;

        public int CurrentAmmo { get; set; }

        protected int burst = 0;
        protected bool canShoot = true;

        public GunInstance() { }
        public GunInstance(GunClass @class)
            : base(@class)
        {
            CurrentAmmo = @class.MaxAmmo;
        }

        public override bool IsDepleted()
        {
            return _class.MaxAmmo > 0 && CurrentAmmo <= 0;
        }

        public override void Reset()
        {
            canShoot = true;
            base.Reset();
        }

        protected virtual void DoCharge()
        {
            base.Charge();
        }

        public override void Charge()
        {
            if (canShoot)
            {
                DoCharge();
                canShoot = false;
            }
        }

        public override void Think(TimeSpan deltaTime)
        {
            if (burst > 0)
            {
                if (burst < _class.BurstCount)
                    DoCharge();
                else
                    burst = 0;
            }

            base.Think(deltaTime);
        }

        public override void Discharge()
        {
            //todo: optimize for cases with no charging (no need to use state machine)

            if (Actor.Map == null || Actor.State.State != Takai.Game.EntStateId.ChargeWeapon)
                return;

            //undercharged
            if (!Actor.State.Instance.HasFinished() &&
                _class.UnderchargeAction == UnderchargeAction.Dissipate)
                return;

            if (_class.Projectile != null)
            {
                var projectile = (Entities.ProjectileInstance)_class.Projectile.Create();
                projectile.Position = Actor.Position + (Actor.Forward * (Actor.Radius + projectile.Radius + 2));
                projectile.Forward = Actor.Forward;
                projectile.Velocity = Actor.Forward * _class.Projectile.Power;
                projectile.Source = Actor;
                Actor.Map.Spawn(projectile);
            }

            --CurrentAmmo;
            ++burst;

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