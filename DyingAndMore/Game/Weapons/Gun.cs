using System;
using Microsoft.Xna.Framework;
using Takai;

namespace DyingAndMore.Game.Weapons
{
    public class GunClass : WeaponClass
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

        public override WeaponInstance Instantiate()
        {
            return new GunInstance(this);
        }
    }

    public class GunInstance : WeaponInstance
    {
        [Takai.Data.Serializer.ReadOnly]
        public new GunClass Class
        {
            get => (GunClass)base.Class;
            set => base.Class = value;
        }

        public int AmmoCount { get; set; }

        protected int currentBurstShotCount = 0;
        protected int burstCount = 0;

        public GunInstance() : this(null) { }
        public GunInstance(GunClass @class)
            : base(@class)
        {
            if (@class != null)
            {
                AmmoCount = @class.MaxAmmo;
            }
        }

        protected override void OnEndUse()
        {
            burstCount = 0;
            base.OnEndUse();
        }

        public override bool IsDepleted()
        {
            return Class.MaxAmmo > 0 && AmmoCount <= 0;
        }

        public override bool CanUse(TimeSpan elapsedTime)
        {
            return (Class.MaxBursts == 0 || burstCount < Class.MaxBursts)
                && (burstCount == 0 || currentBurstShotCount > 0 || elapsedTime > StateTime + Class.BurstCooldownTime)
                && base.CanUse(elapsedTime);
        }

        public override void Think(TimeSpan deltaTime)
        {
            if (currentBurstShotCount > 0)
            {
                if (currentBurstShotCount < Class.RoundsPerBurst && !IsDepleted())
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
            //todo: at high rates of fire, occasionally discharge effect doesnt play (or at least doesnt play correctly)

            if (Class.Projectile != null)
            {
                for (int i = 0; i < Class.ProjectilesPerRound; ++i)
                {
                    var projectile = (Entities.ProjectileInstance)Class.Projectile.Instantiate();
                    projectile.Position = Actor.Position + (Actor.Forward * (Actor.Radius + projectile.Radius + 2));

                    var error = Class.ErrorAngle.Random();
                    projectile.Forward = Vector2.TransformNormal(Actor.Forward, Matrix.CreateRotationZ(error));
                    projectile.Velocity = projectile.Forward * Class.Projectile.MuzzleVelocity.Random();
                    if (Class.Projectile.InheritSourcePhysics)
                        projectile.Velocity += Actor.Velocity;
                    projectile.Source = Actor;
                    Actor.Map.Spawn(projectile);
                }
            }

            --AmmoCount;
            ++currentBurstShotCount;

            base.OnDischarge();
        }

        public override bool Combine(WeaponInstance other)
        {
            if (Class == null || other.Class != Class)
                return false;

            //todo: option to combine if weapon shares same projectile type

            var gun = (GunInstance)other;
            var lastAmmoCount = AmmoCount;
            AmmoCount = Math.Min(Class.MaxAmmo, AmmoCount + (gun.AmmoCount));
            gun.AmmoCount -= AmmoCount - lastAmmoCount;
            return true;
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