using System;

namespace Takai
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

    /// <summary>
    /// Preferred distribution of values when returning a random value
    /// </summary>
    public enum RandomDistribution
    {
        Default, //otherwise unspecified
        Gaussian, //normal distribution
        Polar,
        //front weighted
        //back weighted
    }

    public static class RangeHelpers
    {
        public static int Random(this Range<int> range, RandomDistribution distribution = RandomDistribution.Default)
        {
            double random;
            switch (distribution)
            {
                case RandomDistribution.Gaussian:
                    return (int)Util.RandomGenerator.NextGaussian((range.max + range.min) / 2f, (range.max - range.min) / 6f);
                case RandomDistribution.Polar:
                    random = Math.Sin(Util.RandomGenerator.NextDouble());
                    break;
                default:
                    return Util.RandomGenerator.Next(range.min, range.max + 1);
            }
            return (int)(random * (range.max - range.min) + range.min);
        }
        public static float Random(this Range<float> range, RandomDistribution distribution = RandomDistribution.Default)
        {
            double random;
            switch (distribution)
            {
                case RandomDistribution.Gaussian:
                    return (float)Util.RandomGenerator.NextGaussian((range.max + range.min) / 2, (range.max - range.min) / 6);
                case RandomDistribution.Polar:
                    random = Math.Sin(Util.RandomGenerator.NextDouble());
                    break;
                default:
                    random = Util.RandomGenerator.NextDouble();
                    break;
            }
            return (float)(random * (range.max - range.min) + range.min);
        }
        public static TimeSpan Random(this Range<TimeSpan> range, RandomDistribution distribution = RandomDistribution.Default)
        {
            switch (distribution)
            {
                //todo: gaussian distribution
                default:
                    {
                        if (range.min == range.max)
                            return range.min;

                        byte[] buf = new byte[8];
                        Util.RandomGenerator.NextBytes(buf);
                        long longRand = BitConverter.ToInt64(buf, 0);

                        return TimeSpan.FromTicks(Math.Abs(longRand % (range.max.Ticks - range.min.Ticks)) + range.min.Ticks);
                    }
            }
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
}
