﻿using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Weapons
{
    public enum UnderchargeAction
    {
        Dissipate,
        Discharge, //will continue charging cycle
    }

    public enum OverchargeAction
    {
        None,
        Discharge,
        Explode,
    }

    /// <summary>
    /// The base for all weapons
    /// </summary>
    public abstract class WeaponClass : Takai.IObjectClass<WeaponInstance>
    {
        public string Name { get; set; }

        [Takai.Data.Serializer.Ignored]
        public string File { get; set; } = null;

        /// <summary>
        /// The prefix for selecting animations to play for a specific animation state:
        /// E.g. 'TestClass' would play animation "TestClass ChargeWeapon|DischargeWeapon"
        /// </summary>
        public string AnimationClass { get; set; } = null;

        public UnderchargeAction UnderchargeAction { get; set; } = UnderchargeAction.Dissipate; //todo: cooldown?

        public OverchargeAction OverchargeAction { get; set; } = OverchargeAction.Discharge;

        public Takai.Game.EffectsClass DischargeEffect { get; set; }

        //overcharge effect
        //undercharge effect?
        //misfire effect (empty/failure/etc)

        /// <summary>
        /// how long charging takes (Charging only occurs if the weapon is cold)
        /// </summary>
        public TimeSpan ChargeTime { get; set; }

        /// <summary>
        /// how long to wait after discharging a single round
        /// </summary>
        public TimeSpan DischargeTime { get; set; }

        /// <summary>
        /// How long it takes the weapon to fully cool down
        /// </summary>
        public TimeSpan CooldownTime { get; set; }

        /// <summary>
        /// Can charge, even when <see cref="WeaponInstance.CanUse(TimeSpan)"/> returns false
        /// Useful for things like spinning gatling barrels
        /// </summary>
        public bool CanAlwaysCharge { get; set; } = false;

        //charge percentage (for things like chaingun)

        //overheating

        //rate of fire over time, speed up time

        public abstract WeaponInstance Instantiate();
    }

    public enum WeaponState
    {
        Idle,

        Charging,
        Discharging,

        //Loading,
        //chambering,
    }

    public abstract class WeaponInstance : Takai.IObjectInstance<WeaponClass>
    {
        //rate of fire curve
        //maybe warmup requires certain rate of fire
        //speedup/down times (separate)
        //tracer effect (every n shots)

        [Takai.Data.Serializer.Ignored]
        public Entities.ActorInstance Actor { get; set; }

        public virtual WeaponClass Class { get; set; }

        public TimeSpan StateTime { get; set; } = TimeSpan.Zero;

        public WeaponState State
        {
            get => _state;
            protected set
            {
                _state = value;
                StateTime = Actor?.Map?.ElapsedTime ?? TimeSpan.Zero;
            }
        }
        WeaponState _state;

        public float Charge { get; set; } = 0;

        protected bool isUsing = false;
        protected bool wasUsing = false;

        public WeaponInstance() { }
        public WeaponInstance(WeaponClass @class)
        {
            Class = @class;
        }

        public virtual WeaponInstance Clone()
        {
            var clone = (WeaponInstance)MemberwiseClone();
            clone.Actor = null;
            clone.isUsing = false;
            return clone;
        }

        public virtual void Think(TimeSpan deltaTime)
        {
            if (wasUsing && !isUsing)
                OnEndUse();

            if (isUsing)
            {
                if (Class.ChargeTime <= TimeSpan.Zero)
                    Charge = 1;
                else if (Charge < 1)
                    Charge += (float)(deltaTime.TotalSeconds / Class.ChargeTime.TotalSeconds);
            }
            else
            {
                if (Class.CooldownTime <= TimeSpan.Zero)
                    Charge = 0;
                else if (Charge > 0)
                    Charge -= (float)(deltaTime.TotalSeconds / Class.CooldownTime.TotalSeconds);
            }

            switch (State)
            {
                case WeaponState.Charging:
                    if (Charge >= 1 && CanUse(Actor.Map.ElapsedTime))
                    {
                        switch (Class.OverchargeAction)
                        {
                            case OverchargeAction.Discharge:
                                State = WeaponState.Discharging;
                                OnDischarge();
                                break;
                            case OverchargeAction.Explode:
                                throw new NotImplementedException("todo");
                        }
                    }
                    break;

                case WeaponState.Discharging:
                    if (Actor.Map.ElapsedTime >= StateTime + Class.DischargeTime)
                    {
                        Actor.StopAnimation($"{Class.AnimationClass}DischargeWeapon");
                        State = WeaponState.Idle;
                    }
                    break;
            }

            wasUsing = isUsing;
            isUsing = false;
        }

        /// <summary>
        /// Begin charging the weapon
        /// </summary>
        public virtual void TryUse()
        {
            if (Class.CanAlwaysCharge || CanUse(Actor.Map.ElapsedTime))
            {
                isUsing = true;
                if (State == WeaponState.Idle)
                {
                    State = WeaponState.Charging;
                    Actor.PlayAnimation($"{Class.AnimationClass}ChargeWeapon");
                }
            }
        }

        protected virtual void OnEndUse()
        {
            if (State == WeaponState.Charging)
            {
                switch (Class.UnderchargeAction)
                {
                    case UnderchargeAction.Dissipate:
                        State = WeaponState.Idle;
                        Actor.StopAnimation($"{Class.AnimationClass}ChargeWeapon");
                        Actor.StopAnimation($"{Class.AnimationClass}DischargeWeapon");
                        break;

                    case UnderchargeAction.Discharge:
                        //continue charging
                        //needs to handle isUsing
                        break;
                }
            }

        }

        /// <summary>
        /// Discharge the active charge. Behavior is implementation defined
        /// Called automatically by Think()
        /// </summary>
        protected virtual void OnDischarge()
        {
            Actor.StopAnimation($"{Class.AnimationClass}ChargeWeapon");
            Actor.PlayAnimation($"{Class.AnimationClass}DischargeWeapon");

            if (Class.DischargeEffect != null)
            {
                var fx = Class.DischargeEffect.Instantiate(Actor);
                fx.Position += (Actor.Forward * (Actor.Radius));
                Actor.Map.Spawn(fx);
            }
        }


        public virtual bool CanUse(TimeSpan totalTime)
        {
            return !IsDepleted();
        }

        /// <summary>
        /// Is the weapon completely depleted? (should be permanent)
        /// </summary>
        /// <returns>True if the weapon is depleted, false otherwise</returns>
        public abstract bool IsDepleted();

        /// <summary>
        /// Combine another weapon into this one. Implementation defined
        /// E.g. add ammo from other weapon to this one on pickup
        /// </summary>
        /// <param name="other">the other weapon to compare against</param>
        /// <returns>Whether or not the weapon could be combined</returns>
        public abstract bool Combine(WeaponInstance other);

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}

//todo: improve animation code here