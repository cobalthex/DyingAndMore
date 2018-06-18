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

    public static class RangeHelpers
    {
        public static int Random(this Range<int> range)
        {
            return Util.RandomGenerator.Next(range.min, range.max + 1);
        }
        public static float Random(this Range<float> range)
        {
            return (float)Util.RandomGenerator.NextDouble() * (range.max - range.min) + range.min;
        }
        public static TimeSpan Random(this Range<TimeSpan> range)
        {
            if (range.min == range.max)
                return range.min;

            byte[] buf = new byte[8];
            Util.RandomGenerator.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return TimeSpan.FromTicks(Math.Abs(longRand % (range.max.Ticks - range.min.Ticks)) + range.min.Ticks);
        }

        public static bool Contains(this Range<float> range, float value)
        {
            return (value >= range.min && value <= range.max);
        }
        public static bool Contains(this Range<int> range, int value)
        {
            return (value >= range.min && value <= range.max);
        }
        public static bool Contains(this Range<TimeSpan> range, TimeSpan value)
        {
            return (value >= range.min && value <= range.max);
        }

        public static int TotalRange(this Range<int> range)
        {
            return range.max - range.min;
        }
        public static float TotalRange(this Range<float> range)
        {
            return range.max - range.min;
        }
        public static TimeSpan TotalRange(this Range<TimeSpan> range)
        {
            return range.max - range.min;
        }
    }

    public class Decal
    {
        public Texture2D texture; //todo: random section of texture
        public Vector2 position;
        public float angle;
        public float scale;
    }

    public class Material : INamedObject
    {
        public string Name { get; set; }
        public string File { get; set; }

        public Dictionary<Material, EffectsClass> Responses { get; set; }
    }
}
