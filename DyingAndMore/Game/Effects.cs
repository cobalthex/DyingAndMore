using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game
{
    /// <summary>
    /// An area of effect damge (for example, grenade)
    /// </summary>
    public class DamageEffect : IGameEffect
    {
        public float DamageScale { get; set; }
        public float Radius { get; set; }

        /// <summary>
        /// Can this effect damage the creator of this effect?
        /// </summary>
        public bool CanDamageSource { get; set; }

        //falloff curve?

        //material dampeners
        //breaking effect

        public void Spawn(EffectsInstance instance)
        {
            var ents = instance.Map.FindEntities(instance.Position, Radius);
            foreach (var ent in ents)
            {
                if ((ent == instance.Source && !CanDamageSource) ||
                    !(ent is Entities.ActorInstance actor))
                    continue;

                var rSq = Vector2.DistanceSquared(instance.Position, ent.Position);
                int damageAmount = (int)(DamageScale * (rSq / (Radius * Radius)));
                actor.ReceiveDamage(damageAmount, instance.Source);

                Takai.LogBuffer.Append(damageAmount.ToString());
                instance.Map.DrawCircle(instance.Position, Radius, Color.Gold);
            }
        }
    }
}
