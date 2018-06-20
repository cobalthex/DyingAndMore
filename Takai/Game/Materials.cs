using System.Collections.Generic;

namespace Takai.Game
{
    public class Material : INamedObject
    {
        public string Name { get; set; }
        public string File { get; set; }
    }

    public struct CollisionResponse
    {
        public EffectsClass Effect { get; set; }

        /// <summary>
        /// If the angle of collision is within this range, the projectile will reflect
        /// </summary>
        /// <remarks>0 to 2pi radians, NaN not to reflect</remarks>
        public Range<float> ReflectAngle { get; set; }
        public Range<float> ReflectSpeed { get; set; }

        //Overpenetrate (glass/breakable materials?) -- enemies
        //attach

        //friction, dampening

        //refraction (reflection offset jitter?)
    }

    public struct MaterialInteraction : System.IComparable<MaterialInteraction>
    {
        public Material A { get; set; }
        public Material B { get; set; }

        public int CompareTo(MaterialInteraction other)
        {
            return ((A == other.A && B == other.B) ||
                    (A == other.B && B == other.A)) ?
                   0 : (A == other.A ? -1 : 1); //does this work?
        }
    }
}
