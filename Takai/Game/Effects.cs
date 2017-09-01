using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public interface IGameEffect { }

    //sound environments
    //(underwater, inside, outside, etc)

    /*looping sound:
        intro/mid/outro variants
        delay between repeat
    */

    public class SoundImpulse : IGameEffect
    {
        /// <summary>
        /// A list of possible sounds to play. One will be chosen at random
        /// </summary>
        public List<SoundClass> Permutations { get; set; }

        //pitch bend (amount, time)
        //strength (distance this sound can be heard) (+ minimum?)
        //sound cone (inner/outer cone affect how loud the sound is + attenuation)
    }

    public class ParticleEffect : IGameEffect
    {
        public ParticleClass Class { get; set; }

        public Range<int> Count { get; set; } = 0;
        public Range<float> Spread { get; set; } = new Range<float>(0, MathHelper.TwoPi);
    }

    public class FluidEffect : IGameEffect
    {
        public FluidClass Class { get; set; }

        public Range<int> Count { get; set; } = 0;
        public Range<float> Spread { get; set; } = new Range<float>(0, MathHelper.TwoPi);
        public Range<float> Speed { get; set; } = 0;
    }

    public struct ScreenEffect : IGameEffect
    {

    }
}
