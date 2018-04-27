using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai
{
    public struct CurveValue<TValue> : IComparable
    {
        public float position;
        public TValue value;

        public CurveValue(float position, TValue value)
        {
            this.position = position;
            this.value = value;
        }

        public int CompareTo(object obj)
        {
            return position.CompareTo(((CurveValue<TValue>)obj).position);
        }
    }

    /// <summary>
    /// The base for Catmull-Rom based curves/splines
    /// </summary>
    /// <typeparam name="TValue">The curvable values</typeparam>
    public abstract class ValueCurve<TValue> : Data.IDerivedDeserialize
    {
        public List<CurveValue<TValue>> Values { get; set; } = new List<CurveValue<TValue>>();

        [Data.Serializer.Ignored]
        public int Count => Values.Count;

        public ValueCurve() { }

        public virtual void AddValue(float t, TValue value)
        {
            Values.Add(new CurveValue<TValue>(t, value));
        }

        int GetClosestIndex(float t)
        {
            //binary search?
            for (int i = 1; i < Values.Count; ++i)
            {
                if (t < Values[i].position)
                    return i - 1;
            }
            return 0;
        }

        protected abstract TValue Function(TValue a, TValue b, TValue c, TValue d, float t);

        public TValue Evaluate(float t)
        {
            if (Values.Count == 0)
                return default(TValue);
            if (Values.Count == 1)
                return Values[0].value;

            var i1 = GetClosestIndex(t);
            var i0 = MathHelper.Clamp(i1 - 1, 0, Values.Count - 1);
            var i2 = MathHelper.Clamp(i1 + 1, 0, Values.Count - 1);
            var i3 = MathHelper.Clamp(i1 + 2, 0, Values.Count - 1);

            t = (t - Values[i1].position) / (Values[i2].position - Values[i1].position);
            return Function(Values[i0].value, Values[i1].value, Values[i2].value, Values[i3].value, t);
        }

        public void DerivedDeserialize(Dictionary<string, object> props)
        {
            Values.Sort();
        }
    }

    public class ScalarCurve : ValueCurve<float>
    {
        public bool IsLinear { get; set; }

        public ScalarCurve() { }

        protected override float Function(float a, float b, float c, float d, float t)
        {
            if (IsLinear)
                return MathHelper.Lerp(b, c, t);
            return MathHelper.CatmullRom(a, b, c, d, t);
        }

        public static implicit operator ScalarCurve(float value)
        {
            var curve = new ScalarCurve();
            curve.Values.Add(new CurveValue<float>(0, value));
            return curve;
        }
    }

    public class HSLCurve : ValueCurve<Vector4>
    {
        /// <summary>
        /// Lerp the gradient counter-clockwise around the HSL hue circle as opposed to clockwise
        /// (Blue to red would go blue->purple->red instead of blue->green->yellow->orange->red)
        /// </summary>
        public bool Reverse { get; set; }

        public HSLCurve() { }

        protected override Vector4 Function(Vector4 a, Vector4 b, Vector4 c, Vector4 d, float t)
        {
            if (Reverse)
                return Util.HSLReverseLerp(b, c, t);
            return Vector4.Lerp(b, c, t);
        }

        public static implicit operator HSLCurve(Vector4 hsla)
        {
            var curve = new HSLCurve();
            curve.Values.Add(new CurveValue<Vector4>(0, hsla));
            return curve;
        }
    }

    public class ColorCurve : Data.IDerivedDeserialize
    {
        protected HSLCurve curve = new HSLCurve();

        public bool Reverse
        {
            get => curve.Reverse;
            set => curve.Reverse = value;
        }

        public IEnumerable<CurveValue<Color>> Values
        {
            get
            {
                foreach (var value in curve.Values)
                    yield return new CurveValue<Color>(value.position, Util.ColorFromHSL(value.value));
            }
        }

        [Data.Serializer.Ignored]
        public int Count => curve.Values.Count;

        public ColorCurve() { }

        public void AddValue(float t, Color c)
        {
            curve.AddValue(t, Util.ColorToHSL(c));
        }

        public Color Evaluate(float t)
        {
            return Util.ColorFromHSL(curve.Evaluate(t));
        }

        public static implicit operator ColorCurve(Color color)
        {
            var curve = new ColorCurve();
            curve.AddValue(0, color);
            return curve;
        }

        public void DerivedDeserialize(Dictionary<string, object> props)
        {
            if (props.TryGetValue("Values", out var srcValues))
            {
                var dstValues = Data.Serializer.Cast<List<CurveValue<Color>>>(srcValues);
                foreach (var v in dstValues)
                    curve.Values.Add(new CurveValue<Vector4>(v.position, Util.ColorToHSL(v.value)));
            }
        }
    }
}
