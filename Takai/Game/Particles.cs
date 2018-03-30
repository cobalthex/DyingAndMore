using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Game
{
    //todo: use iobjectClass?

    /// <summary>
    /// A single type of particle
    /// </summary>
    public class ParticleClass
    {
        /// <summary>
        /// The sprite used for each particle of this type
        /// </summary>
        public Graphics.Sprite Sprite { get; set; }
        /// <summary>
        /// The render blend state this particle
        /// </summary>
        public BlendState Blend { get; set; }

        public ColorCurve ColorOverTime { get; set; } = Color.White;
        public ScalarCurve ScaleOverTime { get; set; } = 1;
        public ScalarCurve SpinOverTime { get; set; } = 0; //relative to spawn angle
        public ScalarCurve AngleOverTime { get; set; } = 0; //relative to spawn angle

        /// <summary>
        /// Spawn a fluid on the death of a particle
        /// </summary>
        public FluidClass DestructionFluid { get; set; } = null;
        //death fluid cutoff?

        /// <summary>
        /// How long each particle should last
        /// </summary>
        public Range<TimeSpan> Lifetime { get; set; } = TimeSpan.FromSeconds(1);

        public Range<float> InitialSpeed { get; set; } = 1;
        public float Drag { get; set; } = 0.05f;
    }

    /// <summary>
    /// An individual particle
    /// </summary>
    public struct ParticleInstance
    {
        public TimeSpan spawnTime;
        public TimeSpan lifetime;

        public Vector2 position;
        public Vector2 velocity;
        public float spawnAngle;

        //cached properties
        public Color color;
        public float scale;
        public float spin;
        public float angle;
    }
}
