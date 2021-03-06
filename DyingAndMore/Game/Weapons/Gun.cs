﻿using System;
using DyingAndMore.Game.Entities;
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
        public int MaxAmmo { get; set; } = 0;

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

        /// <summary>
        /// Optionally refil ammo at this rate (none if zero)
        /// Refills one per tick
        /// </summary>
        public TimeSpan AmmoRefillSpeed { get; set; }
        public bool OnlyRefillWhileIdle { get; set; }

        public override WeaponInstance Instantiate()
        {
            return new GunInstance(this);
        }
    }

    public class GunInstance : WeaponInstance
    {
        [Takai.Data.Serializer.Ignored]
        public GunClass _Class
        {
            get => (GunClass)base.Class;
            set => base.Class = value;
        }

        public int MaxAmmo => _Class.MaxAmmo;

        public int CurrentAmmo { get; set; }
        public TimeSpan LastAmmoRefillTime { get; set; }
        public bool OnlyRefillIfIdle { get; set; } = true;

        protected int currentBurstShotCount = 0;
        protected int burstCount = 0;

        public GunInstance() : this(null) { }
        public GunInstance(GunClass @class)
            : base(@class)
        {
            if (@class != null)
                CurrentAmmo = @class.MaxAmmo;
        }

        protected override void OnEndUse()
        {
            burstCount = 0;
            base.OnEndUse();
        }

        public override bool IsDepleted()
        {
            return _Class.MaxAmmo > 0 && CurrentAmmo <= 0;
        }

        public override bool CanUse(TimeSpan elapsedTime)
        {
            return (_Class.MaxBursts == 0 || burstCount < _Class.MaxBursts)
                && (burstCount == 0 || currentBurstShotCount > 0 || elapsedTime > StateTime + _Class.BurstCooldownTime)
                && base.CanUse(elapsedTime);
        }

        public override void Think(TimeSpan deltaTime)
        {
            if (Actor.Map == null)
                return; //there is a bug in entity update code - cannot find source (actor.Map is null during Think())

            if (currentBurstShotCount > 0)
            {
                if (currentBurstShotCount < _Class.RoundsPerBurst && !IsDepleted())
                    base.TryUse();
                else
                {
                    currentBurstShotCount = 0;
                    ++burstCount;
                }
            }

            if (LastAmmoRefillTime == TimeSpan.Zero ||
                (State == WeaponState.Idle && _Class.OnlyRefillWhileIdle)) //ugly
                LastAmmoRefillTime = Actor.Map.ElapsedTime;

            var timeSinceLastRefill = Actor.Map.ElapsedTime - LastAmmoRefillTime;
            if (_Class.AmmoRefillSpeed != TimeSpan.Zero &&
                timeSinceLastRefill.Ticks >= Math.Abs(_Class.AmmoRefillSpeed.Ticks))
            {
                CurrentAmmo = MathHelper.Clamp(CurrentAmmo + (int)(timeSinceLastRefill.Ticks / _Class.AmmoRefillSpeed.Ticks), 0, MaxAmmo);
                LastAmmoRefillTime = Actor.Map.ElapsedTime;
            }

            base.Think(deltaTime);
        }

        protected override void OnDischarge()
        {
            //todo: at high rates of fire, occasionally discharge effect doesnt play (or at least doesnt play correctly)

            if (_Class.Projectile != null)
            {
                for (int i = 0; i < _Class.ProjectilesPerRound; ++i)
                {
                    var projectile = (ProjectileInstance)_Class.Projectile.Instantiate();
                    projectile.SetPositionTransformed(Actor.WorldPosition + (Actor.WorldForward * (Actor.Radius + projectile.Radius + _Class.SpawnOffset)));

                    var error = _Class.ErrorAngle.Random();
                    projectile.Forward = Vector2.TransformNormal(Actor.Forward, Matrix.CreateRotationZ(error));
                    projectile.Velocity = projectile.Forward * _Class.Projectile.MuzzleVelocity.Random();
                    if (_Class.Projectile.InheritSourcePhysics)
                        projectile.Velocity += Actor.Velocity;
                    projectile.Source = Actor;
                    Actor.Map.Spawn(projectile);
                }
            }

            --CurrentAmmo;
            ++currentBurstShotCount;

            base.OnDischarge();
        }

        public override bool Combine(WeaponInstance other)
        {
            if (Class == null || other.Class != Class)
                return false;

            //todo: option to combine if weapon shares same projectile type

            var gun = (GunInstance)other;
            var lastAmmoCount = CurrentAmmo;
            CurrentAmmo = Math.Min(_Class.MaxAmmo, CurrentAmmo + (gun.CurrentAmmo));
            gun.CurrentAmmo -= CurrentAmmo - lastAmmoCount;
            return true;
        }

        public override string ToString()
        {
            return $"{base.ToString()} ({CurrentAmmo}/{_Class?.MaxAmmo})";
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