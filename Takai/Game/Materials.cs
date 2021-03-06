﻿using System.Collections.Generic;

namespace Takai.Game
{
    /// <summary>
    /// material interactions that occur when two materials collide
    /// </summary>
    public struct MaterialInteraction
    {
        //effects inheritance?

        public EffectsClass Effect { get; set; } //different effects for different situations?


        /* order of presedence:
            Attach,
            Overpenetrate
            Reflect, //parallel and perpendicular friction
            shatter/explode/break ?
            Impact,
            Fizzle, //impact without effect
        */

        /// <summary>
        /// How likely the collider is to attach to the colliding object
        /// </summary>
        public float AttachChance { get; set; }

        /// <summary>
        /// Overpenetrate 
        /// </summary>
        public float MinimumOverpenetrateForce { get; set; }

        /// <summary>
        /// If the angle of collision is within this range, the projectile will bounce (reflect)
        /// </summary>
        /// <remarks>The maximum angle difference to bounce from</remarks>
        public float MaxBounceAngle { get; set; }
        /// <summary>
        /// Speeds required to bounce
        /// </summary>
        //public Range<float> BounceSpeedRange { get; set; }
        
        /// <summary>
        /// MInimum force required to impact, if not met collider 'fizzles'
        /// </summary>
        public float MinimumImpactForce { get; set; }

        /// <summary>
        /// Energy lost, as a fraction of the total energy
        /// </summary>
        public float Friction { get; set; } //range? (parallel vs perpendicular)

        //restitution

        //Overpenetrate (glass/breakable materials?) -- enemies

        //refraction (reflection offset jitter?)
    }

    public class MaterialInteractions : Data.ISerializeExternally
    {
        public string File { get; set; }

        public const string AnyMaterial = "*";

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

            AddInteractionOneWay(materialA, materialB, interaction);
            AddInteractionOneWay(materialB, materialA, interaction);
        }

        private void AddInteractionOneWay(string outer, string inner, MaterialInteraction interaction)
        {
            if (!interactions.TryGetValue(outer, out var found))
                found = interactions[outer] = new Dictionary<string, MaterialInteraction>(System.StringComparer.OrdinalIgnoreCase);
            found[inner] = interaction;
        }

        public MaterialInteraction Find(string materialA, string materialB)
        {
            MaterialInteraction mat = new MaterialInteraction();

            if (materialA == null)
                materialA = AnyMaterial;
            if (materialB == null)
                materialB = AnyMaterial;

            if (interactions.TryGetValue(materialA, out var inner))
            {
                if (!inner.TryGetValue(materialB, out mat))
                    inner.TryGetValue(AnyMaterial, out mat);
            }
            else if (interactions.TryGetValue(materialB, out inner))
            {
                if (!inner.TryGetValue(materialA, out mat))
                    inner.TryGetValue(AnyMaterial, out mat);
            }
            else if (interactions.TryGetValue(AnyMaterial, out inner))
                inner.TryGetValue(AnyMaterial, out mat);

            return mat;
        }
    }
}
