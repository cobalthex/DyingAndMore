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
    public class EffectsClass : Data.INamedClass<EffectsInstance>
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
        /// with the entity as the source
        /// </summary>
        /// <param name="source">The entity to spawn at</param>
        /// <returns>The effect instance created</returns>
        public EffectsInstance Instantiate(EntityInstance source, EntityInstance target = null)
        {
            var instance = Instantiate();
            if (source != null)
            {
                instance.Map = source.Map;
                instance.Position = source.Position;
                instance.Direction = source.Forward;
                instance.Velocity = source.Velocity;
                instance.Source = source;
            }
            instance.Target = target;

            return instance;
        }
    }

    /// <summary>
    /// A collection of effects played at a location in the map
    /// </summary>
    public struct EffectsInstance : Data.IInstance<EffectsClass>
    {
        public EffectsClass Class { get; set; }

        [Data.Serializer.Ignored]
        public MapInstance Map { get; set; }

        public Vector2 Position { get; set; }
        public Vector2 Direction { get; set; }
        public Vector2 Velocity { get; set; }
        /// <summary>
        /// The entity that spawned this effect. null for none
        /// </summary>
        public EntityInstance Source { get; set; }
        /// <summary>
        /// Contextual target for recieving any affects
        /// </summary>
        public EntityInstance Target { get; set; }

        public EffectsInstance(EffectsClass @class)
        {
            Class = @class;
            Map = null;
            Position = Vector2.Zero;
            Direction = Vector2.UnitX;
            Velocity = Vector2.Zero;
            Source = null;
            Target = null;
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
                var which = Util.RandomGenerator.Next(0, Effects.Count);
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
        public RandomDistribution SpreadDistribution { get; set; }
        public float Radius { get; set; } = 0; //spawn radius around the spawn point

        public bool InheritParentVelocity { get; set; }

        public void Spawn(EffectsInstance instance)
        {
            if (Class == null)
                return;

            if (!instance.Map.Particles.ContainsKey(Class))
                instance.Map.Particles.Add(Class, new List<ParticleInstance>());

            var numParticles = Math.Max(0, Count.Random());
            instance.Map.Particles[Class].Capacity = Math.Max(instance.Map.Particles[Class].Capacity, numParticles);
            for (int i = 0; i < numParticles; ++i)
            {
                var angle = Spread.Random(SpreadDistribution);
                var dir = Vector2.TransformNormal(instance.Direction, Matrix.CreateRotationZ(-angle));
                var initAngle = dir.Angle();

                var position = instance.Position;
                if (Radius != 0)
                {
                    var spawnRadiusAngle = Util.RandomGenerator.NextDouble() * MathHelper.TwoPi;
                    position += new Vector2((float)Math.Cos(spawnRadiusAngle), (float)Math.Sin(spawnRadiusAngle))
                        * (float)Util.RandomGenerator.NextDouble() * Radius;
                }

                var particle = new ParticleInstance
                {
                    color = Class.ColorOverTime.Count > 0 ? Class.ColorOverTime.Evaluate(0) : Color.White,
                    position = position,
                    velocity = Class.InitialSpeed.Random() * dir + (InheritParentVelocity ? instance.Velocity : Vector2.Zero),
                    lifeTime = Class.LifeSpan.Random(),
                    spawnAngle = initAngle,
                    spin = Class.SpinOverTime.Count > 0 ? Class.SpinOverTime.Evaluate(0) : 0,
                    scale = Class.ScaleOverTime.Count > 0 ? Class.ScaleOverTime.Evaluate(0) : 1,
                    angle = Class.AngleOverTime.Count > 0 ? Class.AngleOverTime.Evaluate(0) : 0,
                    spawnTime = instance.Map.ElapsedTime
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
        public EntityClass Entity { get; set; }

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
                    Entity,
                    instance.Position,
                    direction,
                    direction * Speed.Random()
                );
                //entity.Source = instance.Source;
            }
        }
    }

    /// <summary>
    /// Apply an outward push force to nearby physical entities
    /// </summary>
    public class ForceEffect : IGameEffect
    {
        public float Force { get; set; } //range?
        /// <summary>
        /// The cone of effect for the force (-pi to pi for a full circle)
        /// </summary>
        public Range<float> Angle { get; set; } = new Range<float>(-MathHelper.Pi, MathHelper.Pi);

        public void Spawn(EffectsInstance instance)
        {
            if (Force == 0 || Angle.TotalRange() == 0)
                return;

            var nearby = instance.Map.FindEntitiesInRegion(instance.Position, (float)Math.Sqrt(Force));
            foreach (var ent in nearby)
            {
                if (!ent.Class.IsPhysical || ent == instance.Source)
                    continue;

                var d = ent.Position - instance.Position;
                var l = d.Length();
                ent.Velocity += (d / l) * (Force);
            }
        }
    }

    /// <summary>
    /// A timed screen fade/flash effect
    /// </summary>
    public class ScreenFadeEffect : IGameEffect
    {
        public ScreenFade Fade { get; set; }

        //effective radius, scale by radius

        public void Spawn(EffectsInstance instance)
        {
            instance.Map.currentScreenFade = Fade;
            instance.Map.currentScreenFadeElapsedTime = TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Shake the camera. May move the camera
    /// </summary>
    public class CameraJerkEffect : IGameEffect
    {
        /// <summary>
        /// Positional jitter
        /// </summary>
        public Range<float> Jitter { get; set; }
        /// <summary>
        ///
        /// </summary>
        public Range<float> Tilt { get; set; }

        Range<TimeSpan> Duration { get; set; }

        public void Spawn(EffectsInstance instance)
        {
            //todo
        }
    }

    /// <summary>
    /// Tint an entity temporarily
    /// </summary>
    public class EntityTintEffect : IGameEffect
    {
        public Color Tint { get; set; }
        Range<TimeSpan> Duration { get; set; }

        public void Spawn(EffectsInstance instance)
        {
            if (instance.Target == null)
                return;

            instance.Target.TintColor = Tint;
            instance.Target.TintColorDuration = Duration.Random();
        }
    }

    //floating text?/objects
}

//todo: effects with timers