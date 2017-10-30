using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Weapons
{
    enum UnderchargeAction
    {
        Dissipate,
        Discharge
    }

    enum OverchargeAction
    {
        None,
        Discharge,
        Explode,
    }

    /// <summary>
    /// The base for all weapons
    /// </summary>
    abstract class WeaponClass : Takai.IObjectClass<WeaponInstance>
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

        /// <summary>
        /// how long charging takes
        /// </summary>
        public TimeSpan ChargeTime { get; set; }

        /// <summary>
        /// how long to wait after discharging
        /// </summary>
        public TimeSpan DischargeTime { get; set; }

        //todo: shot delay, burst delay in gun

        public abstract WeaponInstance Create();
    }

    abstract class WeaponInstance : Takai.IObjectInstance<WeaponClass>
    {
        public enum WeaponState
        {
            Idle,
            Charging,
            Discharging,
            //relaading, rechambering
        }

        [Takai.Data.Serializer.Ignored]
        public Entities.ActorInstance Actor { get; set; }

        public virtual WeaponClass Class { get; set; }

        /// <summary>
        /// When the current state was entered
        /// </summary>
        protected TimeSpan StateTime { get; set; } = TimeSpan.Zero;
        public WeaponState State
        {
            get => _state;
            protected set
            {
                _state = value;
                StateTime = Actor.Map?.ElapsedTime ?? TimeSpan.Zero;
                Takai.LogBuffer.Append(value.ToString());
            }
        }
        WeaponState _state;

        /// <summary>
        /// The number of consecutive shots taken
        /// </summary>
        //public int burstCount = 0;

        public WeaponInstance() { }
        public WeaponInstance(WeaponClass @class)
        {
            Class = @class;
        }

        public virtual void Think(TimeSpan deltaTime)
        {
            //remove reliance on state machine? (or augment to allow multiple states for this)

            switch (State)
            {
                case WeaponState.Charging:
                    if (Actor.Map.ElapsedTime >= StateTime + Class.ChargeTime)
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
                        State = WeaponState.Idle;
                    break;

            }
        }

        /// <summary>
        /// Begin charging the weapon
        /// </summary>
        public virtual void TryFire()
        {
            if (CanUse(Actor.Map.ElapsedTime))
            {
                //todo: if charge time is zero, skip to discharge

                State = WeaponState.Charging;
                Actor.State.TransitionTo(Takai.Game.EntStateId.ChargeWeapon, $"{Class.AnimationClass} ChargeWeapon"); //todo; animation classes in state machine?
            }
        }
        /// <summary>
        /// Reset the firing state. For example, should reset burst counter
        /// Called whenever player depresses fire button
        /// Should not reset the gun entirely (create a new instance for that)
        /// </summary>
        public virtual void Reset()
        {
            switch (Class.UnderchargeAction)
            {
                case UnderchargeAction.Discharge:
                    State = WeaponState.Discharging; //?
                    OnDischarge();
                    break;
                case UnderchargeAction.Dissipate:
                    State = WeaponState.Idle;
                    break;
            }
        }

        /// <summary>
        /// Discharge the active charge. Behavior is implementation defined
        /// Called automatically by Think()
        /// </summary>
        protected virtual void OnDischarge()
        {
            Actor.State.TransitionTo(Takai.Game.EntStateId.DischargeWeapon, $"{Class.AnimationClass} DischargeWeapon");

            if (Class.DischargeEffect != null)
            {
                var fx = Class.DischargeEffect.Create(Actor);
                fx.Position += (Actor.Forward * (Actor.Radius));
                Actor.Map.Spawn(fx);
            }
        }


        public virtual bool CanUse(TimeSpan totalTime)
        {
            return !IsDepleted() && State == WeaponState.Idle;
        }

        /// <summary>
        /// Is the weapon completely depleted? (should be permanent)
        /// </summary>
        /// <returns>True if the weapon is depleted, false otherwise</returns>
        public abstract bool IsDepleted();

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}

//todo: return to default state?