﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    /// <summary>
    /// Specifies a simple min/max range
    /// </summary>
    /// <typeparam name="T">The type of range</typeparam>
    public struct Range<T>
    {
        public T min;
        public T max;

        public Range(T Value)
        {
            min = max = Value;
        }
        public Range(T Min, T Max)
        {
            min = Min;
            max = Max;
        }

        public static implicit operator Range<T>(T value)
        {
            return new Range<T>
                (value);
        }
    }

    public static class RandomRange
    {
        private static System.Random randomGen = new System.Random();

        public static int Next(Range<int> range)
        {
            return randomGen.Next(range.min, range.max);
        }

        public static float Next(Range<float> range)
        {
            return (float)randomGen.NextDouble() * (range.max - range.min) + range.min;
        }
    }

    public struct ValueCurve<T>
    {
        public Curve curve;
        public T start;
        public T end;

        public ValueCurve(T Value)
        {
            curve = new Curve();
            start = Value;
            end = Value;
        }
        public ValueCurve(Curve Curve, T Start, T End)
        {
            curve = Curve;
            start = Start;
            end = End;
        }
    }

    /// <summary>
    /// A single type of Fluid
    /// This struct defines the graphics for the Fluid and physical properties that can affect the game
    /// </summary>
    [Data.DesignerCreatable]
    public class FluidType
    {
        /// <summary>
        /// The texture to render the Fluid with
        /// </summary>
        public Texture2D Texture { get; set; }
        /// <summary>
        /// A reflection map, controlling reflection of entities, similar to a normal map
        /// Set to null or alpha = 0 for no reflection
        /// </summary>
        public Texture2D Reflection { get; set; }

        /// <summary>
        /// The alpha value to draw the texture with
        /// </summary>
        public float Alpha { get; set; } = 1;

        /// <summary>
        /// The radius of an individual Fluid
        /// </summary>
        public float Radius { get; set; }
        /// <summary>
        /// Drag affects both how quickly the Fluid stops moving and how much resistance there is to entities moving through it
        /// </summary>
        public float Drag { get; set; }
    }

    /// <summary>
    /// A single fluid, rendered as a meta-blob
    /// Fluids can have physics per their fluid type
    /// Fluids can be spawned with a velocity which is decreased by their drag over time. Once the velocity reaches zero, the fluid is considered inactive (permanently)
    /// </summary>
    [Data.DesignerCreatable]
    public struct Fluid
    {
        public FluidType type;
        public Vector2 position;
        public Vector2 velocity;
    }

    [Data.DesignerCreatable]
    public class Decal
    {
        public Texture2D texture;
        public Vector2 position;
        public float angle;
        public float scale;
    }

    /// <summary>
    /// A single type of particle
    /// </summary>
    [Data.DesignerCreatable]
    public class ParticleType
    {
        /// <summary>
        /// The grahpic used for each particle of this type
        /// </summary>
        public Graphics.Sprite Graphic { get; set; }
        /// <summary>
        /// How to blend this particle
        /// </summary>
        public BlendState BlendMode { get; set; }

        /// <summary>
        /// Color over time
        /// </summary>
        public ValueCurve<Color> Color { get; set; }
        /// <summary>
        /// Speed over time
        /// </summary>
        public ValueCurve<float> Speed { get; set; }
        /// <summary>
        /// Scale over time
        /// </summary>
        public ValueCurve<float> Scale { get; set; }
        /// <summary>
        /// Angle over time (not angular velocity)
        /// </summary>
        public ValueCurve<float> Angle { get; set; }

        //todo: multiple values
    }

    /// <summary>
    /// An individual particle
    /// </summary>
    public struct Particle
    {
        public System.TimeSpan time; //spawn time
        public System.TimeSpan lifetime;
        public System.TimeSpan delay;

        public Vector2 position;
        public Vector2 direction;
        //angular velocity

        //cached properties
        public float scale;
        public float speed;
        public float rotation; //not angular velocity
        public Color color;
    }

    /// <summary>
    /// Description for spawning particles
    /// </summary>
    public struct ParticleSpawn
    {
        /// <summary>
        /// The type of particles to spawn
        /// </summary>
        public ParticleType type;

        /// <summary>
        /// The number of particles to spawn
        /// </summary>
        public Range<int> count;
        /// <summary>
        /// How long each particle should live for
        /// </summary>
        public Range<System.TimeSpan> lifetime;
        /// <summary>
        /// How long to wait before drawing the particle
        /// </summary>
        public Range<System.TimeSpan> delay;
        /// <summary>
        /// Where to spawn the particles from
        /// </summary>
        public Range<Vector2> position;
        /// <summary>
        /// the directional cone to spawn particles from
        /// </summary>
        public Range<float> angle;
    }
}
