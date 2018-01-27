using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
        public static readonly Random RandomGenerator = new Random();

        public static int Next(Range<int> range)
        {
            return RandomGenerator.Next(range.min, range.max + 1);
        }

        public static float Next(Range<float> range)
        {
            return (float)RandomGenerator.NextDouble() * (range.max - range.min) + range.min;
        }

        public static TimeSpan Next(Range<TimeSpan> range)
        {
            if (range.min == range.max)
                return range.min;

            byte[] buf = new byte[8];
            RandomGenerator.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return TimeSpan.FromTicks(Math.Abs(longRand % (range.max.Ticks - range.min.Ticks)) + range.min.Ticks);
        }
    }

    public class Decal //todo: make struct
    {
        public Texture2D texture;
        public Vector2 position;
        public float angle;
        public float scale;
    }

    public class MaterialResponse
    {
        //response (overpenetrate, destroy, reflect, etc)

        //friction, dampening

        //refraction

        public EffectsClass Effect { get; set; }
    }

    public class Material : INamedObject
    {
        public string Name { get; set; }
        public string File { get; set; }

        public Dictionary<Material, MaterialResponse> Responses { get; set; }
    }
}
