using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Game
{
    /// <summary>
    /// A single type of particle
    /// </summary>
    public class ParticleClass : INamedObject
    {
        public string File { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// The sprite used for each particle of this type
        /// </summary>
        public Graphics.Sprite Sprite
        {
            get => _sprite;
            set
            {
                _sprite = value;
                Radius = Math.Max(_sprite.Width, _sprite.Height) / 2;
            }
        }
        private Graphics.Sprite _sprite;

        /// <summary>
        /// The render blend state this particle
        /// </summary>
        public BlendState Blend { get; set; }

        //follow path? (angle oriented, faces direction, etc)?

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

        /// <summary>
        /// An optional collision effect for this particle. If null, no physics/collision is run
        /// Can get expensive with lots of particles
        /// </summary>
        public EffectsClass CollisionEffect { get; set; }

        [Data.Serializer.Ignored]
        public float Radius { get; private set; }
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
