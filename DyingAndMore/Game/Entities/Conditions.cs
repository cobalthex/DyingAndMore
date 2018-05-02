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
        /// How much health to add or remove per second (positive for boon, negative for poison)
        /// </summary>
        public float HealthPerSecond { get; set; }

        /// <summary>
        /// An effect to play while this condition is active
        /// </summary>
        public EffectsClass ActiveEffect { get; set; }

        //taper?

        public ConditionInstance Instantiate()
        {
            return new ConditionInstance();
        }
    }

    public class ConditionInstance : Takai.IObjectInstance<ConditionClass>
    {
        public ConditionClass Class { get; set; }

        public TimeSpan TimeRemaining { get; set; }

        public void Merge(ConditionInstance other)
        {
            TimeRemaining = Takai.Util.Max(TimeRemaining, other.TimeRemaining);
        }
    }

    /// <summary>
    /// Apply a condition (poison, etc) to any actor within the effect radius
    /// </summary>
    public class ConditionEffect : IGameEffect
    {
        public ConditionClass Condition { get; set; }
        public float Radius { get; set; }
        public TimeSpan Duration { get; set; }

        public void Spawn(EffectsInstance instance)
        {
            var ents = instance.Map.FindEntities(instance.Position, Radius);
            var rSq = Radius * Radius;
            foreach (var ent in ents)
            {
                if (ent is ActorInstance actor)
                {

                }
            }
        }
    }
}
