using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Game
{
    public interface IGameEffect
    {
        void Spawn(EffectsInstance instance);
    }

    /// <summary>
    /// A collection of effects to play in a map
    /// </summary>
    public class EffectsClass : IObjectClass<EffectsInstance>
    {
        public string Name { get; set; }

        [Data.Serializer.Ignored]
        public string File { get; set; }

        public float SkipPercent { get; set; }

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

        /// <summary>
        /// Spawn a set of effects at an entity's position in their map
        /// </summary>
        /// <param name="source">The entity to spawn at</param>
        /// <returns>The effect instance created</returns>
        public EffectsInstance Create(EntityInstance source)
        {
            if (source == null)
                return Create();

            return new EffectsInstance(this)
            {
                Map = source.Map,
                Position = source.Position,
                Direction = source.Forward,
                Velocity = source.Velocity,
                Source = source
            };
        }
    }

    /// <summary>
    /// A collection of effects played at a location in the map
    /// </summary>
    public struct EffectsInstance : IObjectInstance<EffectsClass>
    {
        public EffectsClass Class { get; set; }

        [Data.Serializer.Ignored]
        public MapInstance Map { get; set; }

        public Vector2 Position { get; set; }
        public Vector2 Direction { get; set; }
        public Vector2 Velocity { get; set; }
        public EntityInstance Source { get; set; }

        public EffectsInstance(EffectsClass @class)
        {
            Class = @class;
            Map = null;
            Position = Vector2.Zero;
            Direction = Vector2.UnitX;
            Velocity = Vector2.Zero;
            Source = null;
        }
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

        public void Spawn(EffectsInstance instance)
        {
            if (Permutations == null)
                return;

            instance.Map.Spawn(
                Permutations[instance.Map.Random.Next(Permutations.Count)],
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
        public float Radius { get; set; } = 0; //spawn radius around the spawn point

        public void Spawn(EffectsInstance instance)
        {
            if (Class == null)
                return;

            if (!instance.Map.Particles.ContainsKey(Class))
                instance.Map.Particles.Add(Class, new List<ParticleInstance>());

            var numParticles = RandomRange.Next(Count);
            instance.Map.Particles[Class].Capacity += numParticles;
            for (int i = 0; i < numParticles; ++i)
            {
                var angle = RandomRange.Next(Spread);
                var speed = RandomRange.Next(Class.InitialSpeed);
                var lifetime = RandomRange.Next(Class.Lifetime);

                var dir = Vector2.TransformNormal(instance.Direction, Matrix.CreateRotationZ(angle));
                var initAngle = dir.Angle();

                var position = instance.Position;
                if (Radius != 0)
                {
                    var spawnRadiusAngle = RandomRange.RandomGenerator.NextDouble() * MathHelper.TwoPi;
                    position += new Vector2((float)Math.Cos(spawnRadiusAngle), (float)Math.Sin(spawnRadiusAngle))
                        * (float)RandomRange.RandomGenerator.NextDouble() * Radius;
                }

                var particle = new ParticleInstance()
                {
                    color = Class.ColorOverTime.start,
                    delay = TimeSpan.Zero,
                    position = position,
                    velocity = speed * dir + instance.Velocity,
                    lifetime = lifetime,
                    angle = initAngle,
                    scale = 1,
                    time = instance.Map.ElapsedTime
                };

                instance.Map.Particles[Class].Add(particle);
            }
        }
    }

    public class FluidEffect : IGameEffect
    {
        public FluidClass Class { get; set; }

        public Range<int> Count { get; set; } = 0;
        public Range<float> Spread { get; set; } = new Range<float>(0, MathHelper.TwoPi);
        public Range<float> Speed { get; set; } = 0;

        public void Spawn(EffectsInstance instance)
        {
            var count = RandomRange.Next(Count);
            instance.Map.LiveFluids.Capacity += count;
            for (int i = 0; i < count; ++i)
            {
                var angle = RandomRange.Next(Spread);
                var speed = RandomRange.Next(Speed);
                instance.Map.Spawn(
                    Class,
                    instance.Position,
                    speed * Vector2.TransformNormal(instance.Direction, Matrix.CreateRotationZ(angle)) + instance.Velocity
                );
            }
        }
    }

    /*
    public class ScreenFlashClass : IObjectClass<ScreenFlashInstance>
    {
        public string Name { get; set; }

        [Data.Serializer.Ignored]
        public string File { get; set; }

        public BlendState blendState;
        public ValueCurve<Color> color;

        public ScreenFlashInstance Create()
        {
            return new ScreenFlashInstance(this);
        }

        //todo: convert to class/instance
    }

    public struct ScreenFlashInstance : IObjectInstance<ScreenFlashClass>
    {
        public ScreenFlashClass Class { get; set; }

        public ScreenFlashInstance(ScreenFlashClass @class)
        {
            Class = @class;
        }
    }

    public class ScreenEffect : IGameEffect
    {
        public ScreenFlashClass Flash { get; set; }

        //Camera shake

        //temporary camera rotation
        //permanent camera rotation

        public void Spawn(EffectsInstance instance)
        {

        }
    }*/

    //floating text?
}
