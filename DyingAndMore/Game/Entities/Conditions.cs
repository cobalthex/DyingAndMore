using System;
using Takai.Game;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// A condition that affects actors, such as poison
    /// </summary>
    public class ConditionClass : Takai.IObjectClass<ConditionInstance>
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
        public float HealthPerSecond { get; set; } = 0;

        /// <summary>
        /// Affect actor speed
        /// </summary>
        public float SpeedScale { get; set; } = 1;

        /// <summary>
        /// How likely is this condition to pass between actors on collision (as percent)
        /// </summary>
        public float ContagiousChance { get; set; } = 0; //todo

        //taper?

        public ConditionInstance Instantiate()
        {
            return new ConditionInstance
            {
                Class = this
            };
        }
    }

    public class ConditionInstance : Takai.IObjectInstance<ConditionClass>
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

    //todo: condition protection (gas mask would protect against poison)

    /// <summary>
    /// Apply a condition (poison, etc) to any actor within the effect radius
    /// </summary>
    public class ConditionEffect : IGameEffect
    {
        public ConditionClass Condition { get; set; }
        public float Radius { get; set; }
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// How likely actors in range acquire this condition
        /// </summary>
        public float AcquireChance { get; set; } = 1;

        public void Spawn(EffectsInstance instance)
        {
            var ents = instance.Map.FindEntities(instance.Position, Radius);
            var rSq = Radius * Radius;
            foreach (var ent in ents)
            {
                if (ent is ActorInstance actor && Takai.Util.PassChance(AcquireChance))
                {
                    if (!actor.Conditions.TryGetValue(Condition, out var cond))
                        actor.Conditions[Condition] = cond = Condition.Instantiate();

                    cond.TimeRemaining = Takai.Util.Max(cond.TimeRemaining, Duration);
                    //todo: refresh vars?
                }
            }
        }
    }
}
