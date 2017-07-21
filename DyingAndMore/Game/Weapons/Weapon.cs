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
    [Takai.Data.DesignerModdable]
    abstract class WeaponClass : Takai.IObjectClass<WeaponInstance>
    {
        public string Name { get; set; }

        [Takai.Data.Serializer.Ignored]
        public string File { get; set; } = null;

        /// <summary>
        /// How long to delay between each shot (random) -- todo: calculate from animation
        /// </summary>
        public Takai.Game.Range<TimeSpan> Delay { get; set; } = TimeSpan.FromMilliseconds(100);

        public UnderchargeAction UnderchargeAction { get; set; } = UnderchargeAction.Dissipate; //todo: cooldown?

        public OverchargeAction OverchargeAction { get; set; } = OverchargeAction.Discharge;

        public abstract WeaponInstance Create();

        /*animation:
            charge, (fire) discharge, weapon firing plays between two animations
            rechamber
            reload
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
            //todo: maybe move to weapon base

            if (Actor.State.TryGet(Takai.Game.EntStateId.ChargeWeapon, out var state) && state.HasFinished())
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

        /// <summary>
        /// Begin charging the weapon
        /// </summary>
        public virtual void Charge()
        {
            if (Actor?.Map == null)
                return;

            if (CanUse(Actor.Map.ElapsedTime))
                Actor.State.TransitionTo(Takai.Game.EntStateId.ChargeWeapon);
        }

        /// <summary>
        /// Discharge the active charge. Behavior is implementation defined
        /// </summary>
        public virtual void Discharge()
        {
            NextShot = Actor.Map.ElapsedTime + Takai.Game.RandomRange.Next(Class.Delay);
            Actor.State.TransitionTo(Takai.Game.EntStateId.ChargeWeapon, Takai.Game.EntStateId.DischargeWeapon);
            Actor.State.TransitionTo(Takai.Game.EntStateId.DischargeWeapon, Takai.Game.EntStateId.Idle);
        }

        public virtual bool CanUse(TimeSpan totalTime)
        {
            return !IsDepleted() && totalTime > NextShot;
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
