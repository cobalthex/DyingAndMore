using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game
{
    /// <summary>
    /// An area of effect damge (for example, grenade)
    /// </summary>
    public class DamageEffect : IGameEffect
        //todo: rename to health effect
    {
        public float MaxDamage { get; set; }
        public float Radius { get; set; }

        /// <summary>
        /// Can this effect damage the creator of this effect?
        /// </summary>
        public bool CanDamageSource { get; set; } //todo: should this be a setting in game settings? (and controlled via actor.ReceiveDamage())?

        //falloff curve?

        //material dampeners
        //breaking effect

        public void Spawn(EffectsInstance instance)
        {
            var rSq = Radius * Radius;

            if (instance.Target != null &&
                (CanDamageSource || instance.Source != instance.Target) &&
                instance.Target is Entities.ActorInstance targetActor)
            {
                if (rSq == 0)
                    targetActor.ReceiveDamage(MaxDamage, instance.Source);
                else
                {
                    var distSq = Vector2.DistanceSquared(instance.Position, instance.Target.Position);
                    if (distSq <= rSq)
                        targetActor.ReceiveDamage(MaxDamage * (rSq - distSq) / rSq, instance.Source); //falloff curve?

                }
            }
            else
            {
                var ents = instance.Map.FindEntities(instance.Position, Radius);
                foreach (var ent in ents)
                {
                    if ((ent == instance.Source && !CanDamageSource) ||
                        !(ent is Entities.ActorInstance actor))
                        continue;

                    var distSq = Vector2.DistanceSquared(instance.Position, ent.Position);

                    if (rSq == 0)
                        actor.ReceiveDamage(MaxDamage, instance.Source);
                    else if (distSq <= rSq)
                        actor.ReceiveDamage(MaxDamage * (rSq - distSq) / rSq, instance.Source); //falloff curve?

                    instance.Map.DrawCircle(instance.Position, Radius, Color.Gold);
                }
            }
        }
    }
}
