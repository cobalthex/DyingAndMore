using Microsoft.Xna.Framework;
using Takai.Game;

//todo: area effect container?

namespace DyingAndMore.Game
{
    /// <summary>
    /// A focused or area health (damage or boon) effect (for example, grenade) on actors
    /// </summary>
    public class HealthEffect : IGameEffect
    { //todo: split into HealthEffect and AreaHealthEffect
        public float MaxDamage { get; set; }
        /// <summary>
        /// If radius is zero, has zero area of affect and only hits target or actor directly on point
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// Can this effect affect the creator of this effect?
        /// </summary>
        public bool CanAffectSource { get; set; } //todo: should this be a setting in game settings? (and controlled via actor.ReceiveDamage())?

        //falloff curve?

        //material dampeners
        //breaking effect

        public void Spawn(EffectsInstance instance)
        {
            var rSq = Radius * Radius;

            if (instance.Target != null &&
                (CanAffectSource || instance.Source != instance.Target) &&
                instance.Target is Entities.ActorInstance targetActor)
            {
                if (rSq == 0)
                    targetActor.ReceiveDamage(MaxDamage, instance.Source);
                else
                {
                    var distSq = Vector2.DistanceSquared(instance.Position, instance.Target.WorldPosition);
                    if (distSq <= rSq)
                        targetActor.ReceiveDamage(MaxDamage * (rSq - distSq) / rSq, instance.Source); //falloff curve?
                }
            }
            else
            {
                var ents = instance.Map.FindEntitiesInRegion(instance.Position, Radius);
                foreach (var ent in ents)
                {
                    if ((ent == instance.Source && !CanAffectSource) ||
                        !(ent is Entities.ActorInstance actor))
                        continue;

                    var distSq = Vector2.DistanceSquared(instance.Position, ent.WorldPosition);

                    if (rSq == 0)
                        actor.ReceiveDamage(MaxDamage, instance.Source); //source?
                    else if (distSq <= rSq)
                        actor.ReceiveDamage(MaxDamage * (rSq - distSq) / rSq, instance.Source); //falloff curve?

                    //instance.Map.DrawCircle(instance.Position, Radius, Color.Gold);
                }
            }
        }
    }
}
