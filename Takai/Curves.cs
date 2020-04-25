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
            Values.Sort();
        }
        public virtual TValue RemoveValue(float t)
        {
            var find = Values.BinarySearch(new CurveValue<TValue>(t, default));
            if (find < 0)
                return default;
            var outv = Values[find];
            Values.RemoveAt(find);
            return outv.value;
        }

        int GetClosestIndex(float t)
        {
            //binary search?
            for (int i = 1; i < Values.Count; ++i)
            {
                if (t < Values[i].position)
                    return i - 1;
            }
            return Values.Count - 1;
        }

        protected abstract TValue Function(TValue a, TValue b, TValue c, TValue d, float t);

        public TValue Evaluate(float t)
        {
            if (Values.Count == 0)
                return default;
            if (Values.Count == 1)
                return Values[0].value;

            var i1 = GetClosestIndex(t);
            var i0 = Util.Clamp(i1 - 1, 0, Values.Count - 1);
            var i2 = Util.Clamp(i1 + 1, 0, Values.Count - 1);
            var i3 = Util.Clamp(i1 + 2, 0, Values.Count - 1);

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

        //from deserializer
        public static implicit operator ScalarCurve(double value)
        {
            var curve = new ScalarCurve();
            curve.AddValue(0, (float)value);
            return curve;
        }
        public static implicit operator ScalarCurve(Int64 value)
        {
            var curve = new ScalarCurve();
            curve.AddValue(0, value);
            return curve;
        }
    }

    public class ChromaCurve : ValueCurve<Vector4>
    {
        /// <summary>
        /// Lerp the gradient counter-clockwise around the HSL hue circle as opposed to clockwise
        /// (Blue to red would go blue->purple->red instead of blue->green->yellow->orange->red)
        /// </summary>
        public bool Reverse { get; set; }

        public ChromaCurve() { }

        protected override Vector4 Function(Vector4 a, Vector4 b, Vector4 c, Vector4 d, float t)
        {
            if (Reverse)
                return Graphics.ColorUtil.HSLReverseLerp(b, c, t);
            return Vector4.Lerp(b, c, t);
        }

        public static implicit operator ChromaCurve(Vector4 color)
        {
            var curve = new ChromaCurve();
            curve.Values.Add(new CurveValue<Vector4>(0, color));
            return curve;
        }
    }

    public enum ChromaMode
    {
        HSL,
        HSV,
        RGB
    }

    public class ColorCurve : Data.IDerivedDeserialize
    {
        protected ChromaCurve curve = new ChromaCurve();
        /// <summary>
        /// How to interpret colors stored in this curve. Changing this does not change the stored values
        /// </summary>
        public ChromaMode Mode { get; set; }

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
                {
                    switch (Mode)
                    {
                        case ChromaMode.HSL:
                            yield return new CurveValue<Color>(value.position, Graphics.ColorUtil.ColorFromHSL(value.value));
                            break;
                        case ChromaMode.HSV:
                            yield return new CurveValue<Color>(value.position, Graphics.ColorUtil.ColorFromHSV(value.value));
                            break;
                        default:
                            yield return new CurveValue<Color>(value.position, Color.FromNonPremultiplied(value.value));
                            break;
                    }
                }
            }
        }

        public int Count => curve.Values.Count;

        public ColorCurve() { }

        public void AddValue(float t, Color c)
        {
            switch (Mode)
            {
                case ChromaMode.HSL:
                   curve.AddValue(t, Graphics.ColorUtil.ColorToHSL(c));
                    break;
                case ChromaMode.HSV:
                   curve.AddValue(t, Graphics.ColorUtil.ColorToHSV(c));
                    break;
                default:
                    curve.AddValue(t, c.ToVector4());
                    break;
            }
        }

        public Color RemoveValue(float t)
        {
            var v = curve.RemoveValue(t);
            switch (Mode)
            {
                case ChromaMode.HSL:
                    return Graphics.ColorUtil.ColorFromHSL(v);
                case ChromaMode.HSV:
                    return Graphics.ColorUtil.ColorFromHSV(v);
                default:
                    return Color.FromNonPremultiplied(v);
            }
        }

        public Color Evaluate(float t)
        {
            switch (Mode)
            {
                case ChromaMode.HSL:
                    return Graphics.ColorUtil.ColorFromHSL(curve.Evaluate(t));
                case ChromaMode.HSV:
                    return Graphics.ColorUtil.ColorFromHSV(curve.Evaluate(t));
                default:
                    return Color.FromNonPremultiplied(curve.Evaluate(t));
            }
        }

        public Vector4 EvaluateChroma(float t)
        {
            return curve.Evaluate(t);
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
                {

                    switch (Mode)
                    {
                        case ChromaMode.HSL:
                            curve.Values.Add(new CurveValue<Vector4>(v.position, Graphics.ColorUtil.ColorToHSL(v.value)));
                            break;
                        case ChromaMode.HSV:
                            curve.Values.Add(new CurveValue<Vector4>(v.position, Graphics.ColorUtil.ColorToHSV(v.value)));
                            break;
                        default:
                            curve.Values.Add(new CurveValue<Vector4>(v.position, v.value.ToVector4()));
                            break;
                    }
                }
            }
        }
    }
}
