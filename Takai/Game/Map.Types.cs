using System;
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
            return RandomGenerator.Next(range.min, range.max);
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


    public class Decal //todo: make struct
    {
        public Texture2D texture;
        public Vector2 position;
        public float angle;
        public float scale;
    }

    public class BobClass : IObjectClass<BobInstance>
    {
        [Data.Serializer.Ignored]
        public string File { get; set; }

        public string Name { get; set; }

        public Graphics.Sprite Sprite { get; set; }

        /// <summary>
        /// A continuous effect played on the bob
        /// </summary>
        public EffectsClass Effect { get; set; }

        public BobInstance Create()
        {
            return new BobInstance(this);
        }
    }

    /// <summary>
    /// Simple physical object for visual fx (gibs, etc)
    /// </summary>
    public struct BobInstance : IObjectInstance<BobClass>
    {
        public BobClass Class { get; set; }

        public Vector2 position;
        public float angle;
        public Vector2 velocity; //vector3, z = angular velocity?

        public BobInstance(BobClass @class)
        {
            Class = @class;
            position = Vector2.Zero;
            angle = 0;
            velocity = Vector2.Zero;
        }
    }
}
