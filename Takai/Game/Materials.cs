using System.Collections.Generic;

namespace Takai.Game
{
    /// <summary>
    /// material interactions that occur when two materials collide
    /// </summary>
    public struct MaterialInteraction
    {
        public EffectsClass Effect { get; set; }

        /// <summary>
        /// If the angle of collision is within this range, the projectile will reflect
        /// </summary>
        /// <remarks>0 to 2pi radians, empty range to not reflect</remarks>
        public Range<float> ReflectAngle { get; set; }
        public Range<float> ReflectSpeed { get; set; }

        //Overpenetrate (glass/breakable materials?) -- enemies
        //attach

        //friction, dampening

        //refraction (reflection offset jitter?)
    }

    public class MaterialInteractions
    {
        public struct MaterialInteractionPair
        {
            public string materialA;
            public string materialB;
            public MaterialInteraction interaction;
        }

        protected Dictionary<string, Dictionary<string, MaterialInteraction>> interactions = new Dictionary<string, Dictionary<string, MaterialInteraction>>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// All of the interactions as pairs.
        /// Warning, calculated on demand, may be costly
        /// </summary>
        public IEnumerable<MaterialInteractionPair> Interactions
        {
            get
            {
                var matched = new Dictionary<string, string>();
                foreach (var outer in interactions)
                {
                    foreach (var inner in outer.Value)
                    {
                        if (matched.ContainsKey(outer.Key) ||
                            matched.ContainsKey(inner.Key))
                            continue;

                        yield return new MaterialInteractionPair
                        {
                            materialA = inner.Key,
                            materialB = outer.Key,
                            interaction = inner.Value
                        };

                        matched[outer.Key] = inner.Key;
                        matched[inner.Key] = outer.Key;
                    }
                }
            }
            set
            {
                foreach (var pair in value)
                    AddInteraction(pair.materialA, pair.materialB, pair.interaction);
            }
        }

        public void AddInteraction(string materialA, string materialB, MaterialInteraction interaction)
        {
            if (materialA == null || materialB == null)
                return;

            //default interactions? (*)

            AddInteractionSingle(materialA, materialB, interaction);
            AddInteractionSingle(materialB, materialA, interaction);
        }

        private void AddInteractionSingle(string outer, string inner, MaterialInteraction interaction)
        {
            if (!interactions.TryGetValue(outer, out var found))
                found = interactions[outer] = new Dictionary<string, MaterialInteraction>(System.StringComparer.OrdinalIgnoreCase);
            found[inner] = interaction;
        }

        public MaterialInteraction Find(string materialA, string materialB)
        {
            MaterialInteraction mat = new MaterialInteraction();
            if (materialA != null && materialB != null &&
                interactions.TryGetValue(materialA, out var inner))
                inner.TryGetValue(materialB, out mat);
            return mat;
        }
    }
}
