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

        public UnderchargeAction UnderchargeAction { get; set; } = UnderchargeAction.Dissipate; //todo: cooldown?

        public OverchargeAction OverchargeAction { get; set; } = OverchargeAction.Discharge;

        public Takai.Game.EffectsClass DischargeEffect { get; set; }

        public TimeSpan ChargeTime { get; set; }

        public TimeSpan DischargeTime { get; set; }

        //todo: shot delay, burst delay in gun

        public abstract WeaponInstance Create();

        /*possible animations:
            charge, [weapon fires], discharge
            rechamber
            reload

            ** firing speeds tied to animation length
        */
    }

    abstract class WeaponInstance : Takai.IObjectInstance<WeaponClass>
    {
        [Takai.Data.Serializer.Ignored]
        public Entities.ActorInstance Actor { get; set; }

        public virtual WeaponClass Class { get; set; }

        /// <summary>
        /// When the next shot can be taken (calculated as shot time + random shot delay)
        /// </summary>
        public TimeSpan NextShot { get; set; } = TimeSpan.Zero;
        protected TimeSpan chargeTime;

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

            if (Actor.State.State == Takai.Game.EntStateId.ChargeWeapon &&
                Actor.Map.ElapsedTime > chargeTime + Class.ChargeTime)
            {
                switch (Class.OverchargeAction)
                {
                    case OverchargeAction.Discharge:
                        Discharge();
                        return;
                    case OverchargeAction.Explode:
                        return; //todo
                }
            }
        }

        protected void SetNextShotTime(TimeSpan delay)
        {
            NextShot = Actor.Map.ElapsedTime + delay;
        }

        /// <summary>
        /// Begin charging the weapon
        /// </summary>
        public virtual void Charge()
        {
            if (Actor?.Map == null)
                return;

            if (CanUse(Actor.Map.ElapsedTime))
            {
                Actor.State.TransitionTo(Takai.Game.EntStateId.ChargeWeapon, "ChargeWeapon");
                chargeTime = Actor.Map.ElapsedTime;
            }
        }

        /// <summary>
        /// Discharge the active charge. Behavior is implementation defined
        /// Called automatically by Think()
        /// </summary>
        public virtual void Discharge()
        {
            SetNextShotTime(Class.DischargeTime);

            Actor.State.TransitionTo(Takai.Game.EntStateId.ChargeWeapon, Takai.Game.EntStateId.DischargeWeapon, "DischargeWeapon");
            Actor.State.TransitionTo(Takai.Game.EntStateId.DischargeWeapon, Takai.Game.EntStateId.Idle, "Idle");

            if (Class.DischargeEffect != null)
            {
                var fx = Class.DischargeEffect.Create(Actor);
                fx.Position += (Actor.Forward * (Actor.Radius));
                Actor.Map.Spawn(fx);
            }
        }

        /// <summary>
        /// Reset the firing state. For example, should reset burst counter
        /// Called whenever player depresses fire button
        /// Should not reset the gun entirely (create a new instance for that)
        /// </summary>
        public virtual void Reset() { }

        public virtual bool CanUse(TimeSpan totalTime)
        {
            return !IsDepleted() &&
                Actor.State.State != Takai.Game.EntStateId.ChargeWeapon &&
                Actor.State .State != Takai.Game.EntStateId.DischargeWeapon;
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
