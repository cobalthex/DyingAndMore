using System;
using Takai.Game;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// A condition that affects actors, such as poison
    /// </summary>
    public abstract class Condition : Takai.INamedObject
    {
        public string Name { get; set; }
        public string File { get; set; }

        //effect?

        /// <summary>
        /// Apply another condition to this one
        /// </summary>
        /// <param name="condition">The condition to apply</param>
        public abstract void Apply(ActorInstance actor, TimeSpan deltaTime);
    }

    /// <summary>
    /// Apply a condition (poison, etc) to any actor within the effect radius
    /// </summary>
    public class ConditionEffect : IGameEffect
    {
        public Condition Condition { get; set; }
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
                    var ic = actor.Conditions.FindIndex(c => c.condition == Condition);
                    if (ic < 0)
                        actor.Conditions.Add(new ActorInstance.ActiveCondition { condition = Condition, timeLeft = Duration });
                    else
                    {
                        var cond = actor.Conditions[ic];
                        cond.timeLeft = Takai.Util.Max(actor.Conditions[ic].timeLeft, Duration);
                        actor.Conditions[ic] = cond;
                    }
                }
            }
        }
    }

    public class Poison : Condition
    {
        public float DamagePerSecond { get; set; }

        public override void Apply(ActorInstance actor, TimeSpan deltaTime)
        {
            actor.ReceiveDamage(DamagePerSecond * (float)deltaTime.TotalSeconds, null);
        }
    }
}
