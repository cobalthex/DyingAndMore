using Microsoft.Xna.Framework.Audio;
using System.Collections.Generic;

namespace Takai.Game
{
    public interface IGameEffect { }

    public struct SoundImpulse : IGameEffect
    {
        public SoundEffect sound;
    }

    public struct ParticleEffect : IGameEffect
    {   
        public ParticleClass Class { get; set; }
        public Range<int> Count { get; set; }
        public Range<float> Speed { get; set; }
        public Range<float> Spread { get; set; }

        //color curve
        //size curve
        //lifetime
        //rotation
    }

    public struct FluidEffect : IGameEffect
    {
        public FluidClass Class { get; set; }
        public Range<int> Count { get; set; }
        public Range<float> Speed { get; set; }
        public Range<float> Spread { get; set; }
    }
}
