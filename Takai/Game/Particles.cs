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
        /// The grahpic used for each particle of this type
        /// </summary>
        public Graphics.Sprite Graphic { get; set; }
        /// <summary>
        /// The render blend state this particle
        /// </summary>
        public BlendState Blend { get; set; }

        public ValueCurve<Color> ColorOverTime { get; set; } = Color.White;
        public ValueCurve<float> ScaleOverTime { get; set; } = 1;
        public ValueCurve<float> AngleOverTime { get; set; } = 0;

        /// <summary>
        /// Spawn a fluid on the death of a particle
        /// </summary>
        public FluidClass DestructionFluid { get; set; } = null;
        //death fluid cutoff?

        /// <summary>
        /// How long each particle should last
        /// </summary>
        public Range<TimeSpan> Lifetime { get; set; } = TimeSpan.FromSeconds(1);

        //start delay

        public Range<float> InitialSpeed { get; set; } = 1;
        public float Drag { get; set; } = 0.05f;

        //angular velocity/drag?
    }

    /// <summary>
    /// An individual particle
    /// </summary>
    public struct ParticleInstance
    {
        public TimeSpan time; //spawn time
        public TimeSpan lifetime;
        public TimeSpan delay;

        public Vector2 position;
        public Vector2 velocity;
        //angular velocity

        //cached properties
        public Color color;
        public float scale;
        public float angle;
    }
}
