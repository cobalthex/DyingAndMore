using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

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

        public EffectsInstance Instantiate()
        {
            return new EffectsInstance(this);
        }

        /// <summary>
        /// Spawn a set of effects at an entity's position in their map
        /// </summary>
        /// <param name="source">The entity to spawn at</param>
        /// <returns>The effect instance created</returns>
        public EffectsInstance Instantiate(EntityInstance source)
        {
            if (source == null)
                return Instantiate();

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

    /// <summary>
    /// Spawn <see cref="Count"/> random effects
    /// </summary>
    public class RandomEffect : IGameEffect
    {
        public List<IGameEffect> Effects { get; set; }
        public Range<int> Count { get; set; } = 1;

        public void Spawn(EffectsInstance instance)
        {
            if (Effects == null || Effects.Count < 1)
                return;

            for (int i = 0; i < Count.Random(); ++i)
            {
                var which = RangeUtil.RandomGenerator.Next(0, Effects.Count);
                Effects[which].Spawn(instance);
            }
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
        SoundClass Class { get; set; }

        //pitch bend (amount, time)
        //strength (distance this sound can be heard) (+ minimum?)
        //sound cone (inner/outer cone affect how loud the sound is + attenuation)

        public void Spawn(EffectsInstance instance)
        {
            if (Class == null)
                return;

            instance.Map.Spawn(
                Class,
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

            var numParticles = Math.Max(0, Count.Random());
            instance.Map.Particles[Class].Capacity += numParticles;
            for (int i = 0; i < numParticles; ++i)
            {
                var angle = Spread.Random();
                var dir = Vector2.TransformNormal(instance.Direction, Matrix.CreateRotationZ(-angle));
                var initAngle = dir.Angle();

                var position = instance.Position;
                if (Radius != 0)
                {
                    var spawnRadiusAngle = RangeUtil.RandomGenerator.NextDouble() * MathHelper.TwoPi;
                    position += new Vector2((float)Math.Cos(spawnRadiusAngle), (float)Math.Sin(spawnRadiusAngle))
                        * (float)RangeUtil.RandomGenerator.NextDouble() * Radius;
                }

                var particle = new ParticleInstance()
                {
                    color = Class.ColorOverTime.Count > 0 ? Class.ColorOverTime.Evaluate(0) : Color.White,
                    delay = TimeSpan.Zero,
                    position = position,
                    velocity = Class.InitialSpeed.Random() * dir + instance.Velocity,
                    angularVelocity = Class.InitialAngularSpeed.Random(),
                    lifetime = Class.Lifetime.Random(),
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
            var count = Count.Random();
            instance.Map.LiveFluids.Capacity += count;
            for (int i = 0; i < count; ++i)
            {
                var angle = Spread.Random();
                var speed = Speed.Random();
                instance.Map.Spawn(
                    Class,
                    instance.Position,
                    speed * Vector2.TransformNormal(instance.Direction, Matrix.CreateRotationZ(angle)) + instance.Velocity
                );
            }
        }
    }

    public class EntityEffect : IGameEffect
    {
        public EntityClass Class { get; set; }

        //some sort of generation
        //separate destruction and burnout effect in entities

        //todo: transform spread to instance direction

        public Range<int> Count { get; set; } = 0;
        public Range<float> Spread { get; set; } = new Range<float>(0, MathHelper.TwoPi);
        public Range<float> Speed { get; set; } = 0;
        public float Radius { get; set; } //radius to spawn entities in around the origin

        public void Spawn(EffectsInstance instance)
        {
            var count = Count.Random();
            for (int i = 0; i < count; ++i)
            {
                //CanSpawn? (test objects around spawn point)

                var angle = Spread.Random();
                var direction = Vector2.TransformNormal(instance.Direction, Matrix.CreateRotationZ(angle));
                var entity = instance.Map.Spawn(
                    Class,
                    instance.Position,
                    direction,
                    direction * Speed.Random()
                );
                //entity.Source = instance.Source;
            }
        }
    }

    //screen effects/flashes

    //floating text?/objects
}
