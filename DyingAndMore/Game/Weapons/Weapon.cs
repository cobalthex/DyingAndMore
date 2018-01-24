using System;
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
        /// how long charging takes
        /// </summary>
        public TimeSpan ChargeTime { get; set; }

        /// <summary>
        /// how long to wait after discharging
        /// </summary>
        public TimeSpan DischargeTime { get; set; }

        public TimeSpan WarmupTime { get; set; }
        public TimeSpan CooldownTime { get; set; }

        public abstract WeaponInstance Instantiate();
    }

    public abstract class WeaponInstance : Takai.IObjectInstance<WeaponClass>
    {
        public enum WeaponState
        {
            Idle,
            Warming,
            Cooling,
            Charging,
            Discharging,
            //relaading, rechambering
        }

        //rate of fire curve
        //maybe warmup requires certain rate of fire
        //speedup/down times (separate)
        //tracer effect (every n shots)

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
            }
        }
        WeaponState _state;

        /// <summary>
        /// How warm the weapon is (warmup/cooldown)
        /// </summary>
        public float Warmth { get; set; } = 0;

        public WeaponInstance() { }
        public WeaponInstance(WeaponClass @class)
        {
            Class = @class;
        }

        public virtual void Think(TimeSpan deltaTime)
        {
            switch (State)
            {
                case WeaponState.Warming:
                    Warmth += (float)(deltaTime.TotalSeconds * Class.WarmupTime.TotalSeconds);
                    break;

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
                    {
                        Actor.StopAnimation($"{Class.AnimationClass}DischargeWeapon");
                        State = WeaponState.Cooling;
                        Actor.PlayAnimation($"{Class.AnimationClass}CoolWeapon");
                    }
                    break;

                case WeaponState.Cooling:
                    Warmth -= (float)(deltaTime.TotalSeconds * Class.CooldownTime.TotalSeconds);
                    if (Warmth <= 0)
                    {
                        Warmth = 0;
                        State = WeaponState.Idle;
                        Actor.StopAnimation($"{Class.AnimationClass}CoolWeapon");
                    }
                    break;
            }
        }

        /// <summary>
        /// Begin charging the weapon
        /// </summary>
        public virtual void TryUse()
        {
            if (CanUse(Actor.Map.ElapsedTime))
            {
                if (Warmth < 1 && Class.WarmupTime > TimeSpan.Zero)
                {
                    State = WeaponState.Warming;
                    Actor.PlayAnimation($"{Class.AnimationClass}WarmWeapon");
                }
                else
                {
                    Warmth = 1;
                    Actor.StopAnimation($"{Class.AnimationClass}WarmWeapon");
                    //todo: if charge time is zero, skip to discharge

                    State = WeaponState.Charging;
                    Actor.PlayAnimation($"{Class.AnimationClass}ChargeWeapon");
                }
            }
        }
        /// <summary>
        /// Reset the firing state. For example, should reset burst counter
        /// Called whenever player depresses fire button
        /// Should not reset the gun entirely (create a new instance for that)
        /// </summary>
        public virtual void Reset()
        {
            if (State == WeaponState.Charging)
            {
                switch (Class.UnderchargeAction)
                {
                    case UnderchargeAction.Dissipate:
                        State = WeaponState.Cooling;
                        Actor.StopAnimation($"{Class.AnimationClass}ChargeWeapon");
                        Actor.StopAnimation($"{Class.AnimationClass}DischargeWeapon");
                        Actor.StopAnimation($"{Class.AnimationClass}WarmWeapon");
                        Actor.PlayAnimation($"{Class.AnimationClass}CoolWeapon");
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
