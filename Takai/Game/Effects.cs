using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public interface IGameEffect
    {
        void Spawn(Map map, EffectsInstance instance);
    }

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
        
        public void Spawn(Map map, EffectsInstance instance)
        {
            if (Permutations == null)
                return;

            map.Spawn(
                Permutations[map.Random.Next(Permutations.Count)],
                instance.Position,
                instance.Direction,
                instance.Velocity
            );
        }
    }

    public class ParticleEffect : IGameEffect
    {
        public ParticleClass Class { get; set; }

        public Range<int> Count { get; set; } = 0;
        public Range<float> Spread { get; set; } = new Range<float>(0, MathHelper.TwoPi);

        public void Spawn(Map map, EffectsInstance instance)
        {
            if (Class == null)
                return;

            if (!map.Particles.ContainsKey(Class))
                map.Particles.Add(Class, new List<ParticleInstance>());

            var numParticles = RandomRange.Next(Count);
            map.Particles[Class].Capacity += numParticles;
            for (int i = 0; i < numParticles; ++i)
            {
                var angle = RandomRange.Next(Spread);
                var speed = RandomRange.Next(Class.InitialSpeed);
                var lifetime = RandomRange.Next(Class.Lifetime);

                var dir = Vector2.TransformNormal(instance.Direction, Matrix.CreateRotationZ(angle));
                var initAngle = (float)Math.Atan2(dir.Y, dir.X);

                var particle = new ParticleInstance()
                {
                    color = Class.ColorOverTime.start,
                    delay = TimeSpan.Zero,
                    position = instance.Position,
                    velocity = speed * dir + instance.Velocity,
                    lifetime = lifetime,
                    angle = initAngle,
                    scale = 1,
                    time = map.ElapsedTime
                };

                map.Particles[Class].Add(particle);
            }
        }
    }

    public class FluidEffect : IGameEffect
    {
        public FluidClass Class { get; set; }

        public Range<int> Count { get; set; } = 0;
        public Range<float> Spread { get; set; } = new Range<float>(0, MathHelper.TwoPi);
        public Range<float> Speed { get; set; } = 0;

        public void Spawn(Map map, EffectsInstance instance)
        {
            var count = RandomRange.Next(Count);
            map.ActiveFluids.Capacity += count;
            for (int i = 0; i < count; ++i)
            {
                var angle = RandomRange.Next(Spread);
                var speed = RandomRange.Next(Speed);
                map.Spawn(
                    Class,
                    instance.Position,
                    speed * Vector2.TransformNormal(instance.Direction, Matrix.CreateRotationZ(angle)) + instance.Velocity
                );
            }
        }
    }

    public class ScreenEffect : IGameEffect
    {
        //todo
        public void Spawn(Map map, EffectsInstance instance) { }
    }

    public class EffectsClass : IObjectClass<EffectsInstance> //todo: better name?
    {
        public string Name { get; set; }

        [Data.Serializer.Ignored]
        public string File { get; set; }

        public float SkipChance { get; set; }

        public Range<TimeSpan> Delay { get; set; } //todo: queuedEvents in map

        /// <summary>
        /// Relative position to the spawn point
        /// </summary>
        public Vector2 Offset { get; set; } = Vector2.Zero;

        public List<IGameEffect> Effects { get; set; }

        public EffectsInstance Create()
        {
            return new EffectsInstance(this);
        }
        
        public EffectsInstance Create(EntityInstance source)
        {
            return new EffectsInstance(this)
            {
                Position = source.Position,
                Direction = source.Forward,
                Velocity = source.Velocity,
                Source = source
            };
        }
    }

    public struct EffectsInstance : IObjectInstance<EffectsClass>
    {
        public EffectsClass Class { get; set; }

        public Vector2 Position { get; set; }
        public Vector2 Direction { get; set; }
        public Vector2 Velocity { get; set; }
        public EntityInstance Source { get; set; }

        public EffectsInstance(EffectsClass @class)
        {
            Class = @class;
            Position = Vector2.Zero;
            Direction = Vector2.UnitX;
            Velocity = Vector2.Zero;
            Source = null;
        }
    }
}
