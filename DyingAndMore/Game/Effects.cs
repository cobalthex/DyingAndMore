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
        public bool CanDamageCreator { get; set; }

        //falloff curve?

        //material dampeners
        //breaking effect

        public void Spawn(Map map, EffectsInstance instance)
        {
            //todo: consolidate arguments (position+velocity can be inherited from entity maybe)
            var ents = map.FindEntities(instance.Position, Radius);
            foreach (var ent in ents)
            {
                if ((ent == instance.Source && !CanDamageCreator) ||
                    !(ent is Entities.ActorInstance actor))
                    continue;

                var rSq = Vector2.DistanceSquared(instance.Position, ent.Position);
                actor.CurrentHealth -= (int)(DamageScale * (1 / rSq));
            }
        }
    }
}
