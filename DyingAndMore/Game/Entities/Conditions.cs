using System;
using Takai.Game;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// A condition that affects actors, such as poison
    /// </summary>
    public class ConditionClass : Takai.INamedClass<ConditionInstance>
    {
        public string Name { get; set; }
        public string File { get; set; }

        /// <summary>
        /// An effect to play while this condition is active
        /// </summary>
        public EffectsClass ActiveEffect { get; set; }

        /// <summary>
        /// How much health to add or remove per second (positive for boon, negative for poison)
        /// </summary>
        public float HealthPerSecond { get; set; } = 0; //todo: effect?

        /// <summary>
        /// Affect actor speed
        /// </summary>
        public float SpeedScale { get; set; } = 1;

        /// <summary>
        /// How likely is this condition to pass between actors on collision (as percent)
        /// </summary>
        public float ContagiousChance { get; set; } = 0; //todo

        //taper?

        //todo: move duration into here?

        public ConditionInstance Instantiate()
        {
            return new ConditionInstance
            {
                Class = this
            };
        }
    }

    public class ConditionInstance : Takai.IInstance<ConditionClass>
    {
        public ConditionClass Class { get; set; }

        public TimeSpan TimeRemaining { get; set; }

        public void Update(ActorInstance actor, TimeSpan deltaTime)
        {
            if (TimeRemaining <= TimeSpan.Zero)
                return;

            TimeRemaining -= deltaTime;

            actor.CurrentHealth += (float)(Class.HealthPerSecond * deltaTime.TotalSeconds);
            if (Class.ActiveEffect != null && actor.Map != null)
            {
                var fx = Class.ActiveEffect.Instantiate(actor, actor);
                actor.Map.Spawn(fx);

                //effect radius scaled by entity size?
            }
        }
    }

    //todo: condition protection (gas mask would protect against poison) / resistance

    /// <summary>
    /// Apply a condition (poison, etc) to any actor within the effect radius
    /// </summary>
    public class ConditionEffect : IGameEffect
    {
        [Takai.Data.Serializer.Required]
        public ConditionClass Condition { get; set; }
        public float Radius { get; set; }
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// How likely actors in range acquire this condition
        /// </summary>
        public float AcquireChance { get; set; } = 1;

        void ApplyCondition(EntityInstance entity)
        {
            if (!(entity is ActorInstance actor) || !Takai.Util.PassChance(AcquireChance))
                return;

            if (!actor.Conditions.TryGetValue(Condition, out var cond))
                actor.Conditions[Condition] = cond = Condition.Instantiate();

            cond.TimeRemaining = Takai.Util.Max(cond.TimeRemaining, Duration);
        }

        public void Spawn(EffectsInstance instance)
        {
            if (instance.Target != null)
                ApplyCondition(instance.Target);
            else
            {
                var ents = instance.Map.FindEntitiesInRegion(instance.Position, Radius);
                var rSq = Radius * Radius;
                foreach (var ent in ents)
                    ApplyCondition(ent);
            }
        }
    }
}
