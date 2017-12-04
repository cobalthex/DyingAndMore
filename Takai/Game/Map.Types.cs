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

    /// <summary>
    /// A spline using Catmull-Rom curves (However the curve function is defined by <see cref="CurveFn"/>)
    /// </summary>
    /// <typeparam name="TValue">The curvable values</typeparam>
    public struct Spline<TValue>
    {
        struct KeyCompare : IComparer<KeyValuePair<float, TValue>>
        {
            public int Compare(KeyValuePair<float, TValue> a, KeyValuePair<float, TValue> b)
            {
                return a.Key.CompareTo(b.Key);
            }

            public static KeyCompare Instance = new KeyCompare();
        }

        public List<KeyValuePair<float, TValue>> Values { get; set; }

        [Data.Serializer.Ignored]
        public Func<TValue, TValue, TValue, TValue, float, TValue> CurveFn { get; set; }

        public Spline(TValue value)
        {
            Values = new List<KeyValuePair<float, TValue>>
            {
                new KeyValuePair<float, TValue>(0, value)
            };
            CurveFn = null;
        }

        public void AddValue(float t, TValue value)
        {
            if (Values == null)
                Values = new List<KeyValuePair<float, TValue>>();

            Values.Add(new KeyValuePair<float, TValue>(t, value));
            Values.Sort(KeyCompare.Instance);
        }

        int GetClosestIndex(float t)
        {
            //binary search?
            for (int i = 1; i < Values.Count; ++i)
            {
                if (t < Values[i].Key)
                    return i - 1;
            }
            return 0;
        }

        public TValue Evaluate(float t)
        {
            if (Values == null)
                return default(TValue);

            var i1 = GetClosestIndex(t);
            var i0 = MathHelper.Clamp(i1 - 1, 0, Values.Count - 1);
            var i2 = MathHelper.Clamp(i1 + 1, 0, Values.Count - 1);
            var i3 = MathHelper.Clamp(i1 + 2, 0, Values.Count - 1);

            t = (t - Values[i1].Key) / (Values[i2].Key - Values[i1].Key);
            return CurveFn(Values[i0].Value, Values[i1].Value, Values[i2].Value, Values[i3].Value, t);
        }
    }

    [Data.CustomSerialize("CustomSerialize"),
     Data.CustomDeserialize(typeof(ColorCurve), "CustomDeserialize")]
    public class ColorCurve
    {
        protected Spline<Vector4> spline = new Spline<Vector4>();

        public int Count => spline.Values.Count;

        [Data.CustomDeserialize(typeof(ColorCurve), "DeserializeValues")]
        public IEnumerable<KeyValuePair<float, Color>> Values
        {
            get
            {
                foreach (var value in spline.Values)
                    yield return new KeyValuePair<float, Color>(value.Key, Util.ColorFromHSL(value.Value));
            }
        }

        public ColorCurve()
        {
            spline.Values = new List<KeyValuePair<float, Vector4>>();
            spline.CurveFn = (a, b, c, d, t) => Vector4.Lerp(b, c, t);
        }

        public void AddValue(float t, Color c)
        {
            spline.AddValue(t, Util.ColorToHSL(c));
        }

        public Color Evaluate(float t)
        {
            return Util.ColorFromHSL(spline.Evaluate(t));
        }

        void DeserializeValues(object obj)
        {
            //var values = Data.Serializer.Cast<IEnumerable<>>
        }
    }

    public class Decal //todo: make struct
    {
        public Texture2D texture;
        public Vector2 position;
        public float angle;
        public float scale;
    }
}
