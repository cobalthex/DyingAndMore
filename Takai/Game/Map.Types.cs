using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    /// <summary>
    /// Specifies a simple min/max range
    /// </summary>
    /// <typeparam name="T">The type of range</typeparam>
    [Data.CustomSerialize("CustomSerialize")]
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
            return new Range<T>(value);
        }

        private object CustomSerialize()
        {
            return Data.Serializer.LinearStruct;
        }

        public override string ToString()
        {
            return $"{min.ToString()} - {max.ToString()}";
        }
    }

    public static class RandomRange
    {
        private static Random randomGen = new Random();

        public static int Next(Range<int> range)
        {
            return randomGen.Next(range.min, range.max);
        }

        public static float Next(Range<float> range)
        {
            return (float)randomGen.NextDouble() * (range.max - range.min) + range.min;
        }

        public static TimeSpan Next(Range<TimeSpan> range)
        {
            if (range.min == range.max)
                return range.min;

            byte[] buf = new byte[8];
            randomGen.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return TimeSpan.FromTicks(Math.Abs(longRand % (range.max.Ticks - range.min.Ticks)) + range.min.Ticks);
        }
    }

    [Data.CustomSerialize("CustomSerialize")]
    public struct ValueCurve<T>
    {
        public Curve curve;
        public T start;
        public T end;

        //todo: multiple values along curve
        //todo: custom serialize curve

        public static readonly Curve Linear = new Curve();
        static ValueCurve()
        {
            Linear.Keys.Add(new CurveKey(0, 0));
            Linear.Keys.Add(new CurveKey(1, 1));
        }

        public ValueCurve(T value)
        {
            curve = Linear;
            start = value;
            end = value;
        }
        public ValueCurve(Curve curve, T start, T end)
        {
            this.curve = curve;
            this.start = start;
            this.end = end;
        }

        public static implicit operator ValueCurve<T>(T value)
        {
            return new ValueCurve<T>(value);
        }

        private object CustomSerialize()
        {
            return Data.Serializer.LinearStruct;
        }
    }

    /// <summary>
    /// A single type of Fluid
    /// This struct defines the graphics for the Fluid and physical properties that can affect the game
    /// </summary>
    [Data.DesignerModdable]
    public class FluidClass : IObjectClass<FluidInstance>
    {
        public string Name { get; set; }

        [Data.Serializer.Ignored]
        public string File { get; set; } = null;

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
        /// The scale of the textures. Does not affect radius
        /// </summary>
        public float Scale { get; set; } = 1;

        /// <summary>
        /// The radius of an individual Fluid
        /// </summary>
        public float Radius { get; set; }
        /// <summary>
        /// Drag affects both how quickly the Fluid stops moving and how much resistance there is to entities moving through it
        /// </summary>
        public float Drag { get; set; }


        public FluidInstance Create()
        {
            return new FluidInstance()
            {
                Class = this
            };
        }
    }

    /// <summary>
    /// A single fluid, rendered as a meta-blob
    /// Fluids can have physics per their fluid type
    /// Fluids can be spawned with a velocity which is decreased by their drag over time. Once the velocity reaches zero, the fluid is considered inactive (permanently)
    /// </summary>
    [Data.DesignerModdable]
    public struct FluidInstance : IObjectInstance<FluidClass>
    {
        public FluidClass Class { get; set; }
        public Vector2 position;
        public Vector2 velocity;
    }

    [Data.DesignerModdable]
    public class Decal //todo: make struct
    {
        public Texture2D texture;
        public Vector2 position;
        public float angle;
        public float scale;
    }

    //todo: switch to class/instance system

    /// <summary>
    /// A single type of particle
    /// </summary>
    [Data.DesignerModdable]
    public class ParticleClass
    {
        /// <summary>
        /// The grahpic used for each particle of this type
        /// </summary>
        public Graphics.Sprite Graphic { get; set; }
        /// <summary>
        /// How to blend this particle
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
