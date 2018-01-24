using System;
using Microsoft.Xna.Framework;
using Takai.Game;

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

        /// <summary>
        /// The maximum number of bursts per trigger pull
        /// Use 0 for infinite
        /// </summary>
        public int MaxBursts { get; set; } = 0;

        /// <summary>
        /// The number of rounds fired in a single burst
        /// </summary>
        public int RoundsPerBurst { get; set; } = 1;

        /// <summary>
        /// How many projectiles are fired per a single shot
        /// Independent of ammo count, useful for weapons like shotguns
        /// </summary>
        public int ProjectilesPerRound { get; set; } = 1;

        public TimeSpan BurstCooldownTime { get; set; } = TimeSpan.Zero;

        //zoom, zoom error?
        public Range<float> ErrorAngle { get; set; }

        //bloom (error angle increases over time)

        //spew (how long continuous fire after overcharge)

        //discharged shells effects

        public override WeaponInstance Instantiate()
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

        public int AmmoCount { get; set; }

        protected int currentBurstShotCount = 0;
        protected int burstCount = 0;

        public GunInstance() { }
        public GunInstance(GunClass @class)
            : base(@class)
        {
            AmmoCount = @class.MaxAmmo;
        }

        protected override void OnEndUse()
        {
            burstCount = 0;
            base.OnEndUse();
        }

        public override bool IsDepleted()
        {
            return _class.MaxAmmo > 0 && AmmoCount <= 0;
        }

        public override bool CanUse(TimeSpan elapsedTime)
        {
            return (_class.MaxBursts == 0 || burstCount < _class.MaxBursts)
                && (burstCount == 0 || currentBurstShotCount > 0 || elapsedTime > StateTime + _class.BurstCooldownTime)
                && base.CanUse(elapsedTime);
        }

        public override void Think(TimeSpan deltaTime)
        {
            if (currentBurstShotCount > 0)
            {
                if (currentBurstShotCount < _class.RoundsPerBurst && !IsDepleted())
                    base.TryUse();
                else
                {
                    currentBurstShotCount = 0;
                    ++burstCount;
                }
            }

            base.Think(deltaTime);
        }

        protected override void OnDischarge()
        {
            if (_class.Projectile != null)
            {
                for (int i = 0; i < _class.ProjectilesPerRound; ++i)
                {
                    var projectile = (Entities.ProjectileInstance)_class.Projectile.Instantiate();
                    projectile.Position = Actor.Position + (Actor.Forward * (Actor.Radius + projectile.Radius + 2));

                    var error = RandomRange.Next(_class.ErrorAngle);
                    projectile.Forward = Vector2.TransformNormal(Actor.Forward, Matrix.CreateRotationZ(error));
                    projectile.Velocity = projectile.Forward * RandomRange.Next(_class.Projectile.MuzzleVelocity);
                    if (_class.Projectile.UseSourcePhysics)
                        projectile.Velocity += Actor.Velocity;
                    projectile.Source = Actor;
                    Actor.Map.Spawn(projectile);
                }
            }

            --AmmoCount;
            ++currentBurstShotCount;

            base.OnDischarge();
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