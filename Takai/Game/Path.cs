using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public class VectorCurve : CatmullCurve<Vector2>
    {
        //todo: possibly have a custom curve that uses incremental T

        private List<float> sectionLengths = new List<float>();

        /// <summary>
        /// Approximate sector lengths
        /// </summary>
        public IReadOnlyList<float> SectionLengths => sectionLengths;

        public float ApproximateTotalLength { get; private set; } = 0;

        protected override Vector2 Function(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            return Vector2.CatmullRom(a, b, c, d, t);
        }

        public void AddPoint(Vector2 point)
        {
            AddValue(Values.Count, point);
        }

        public override void AddValue(float t, Vector2 value)
        {
            base.AddValue(t, value);
            if (Values.Count > 1)
            {
                var last = Values[Values.Count - 2];
                var coarseLength = Vector2.Distance(last.value, value);
                coarseLength = MathHelper.Min(5, coarseLength / 10);

                var tinc = (t - last.position) / coarseLength;

                var length = 0f;
                Vector2 lastP = last.value;
                for (int i = 0; i < coarseLength; ++i)
                {
                    var p = Evaluate(last.position + (tinc * i));
                    length += Vector2.DistanceSquared(lastP, p);
                    lastP = p;
                }

                length = (float)System.Math.Sqrt(length);
                sectionLengths.Add(length);
                ApproximateTotalLength += length;
            }
        }
    }

    /*
    /// <summary>
    /// Follow along a path
    /// </summary>
    /// <remarks>Caches positions along the path for better performance</remarks>
    public class PathRider
    {
        /// <summary>
        /// The path this rider is following
        /// Setting this value will reset the rider
        /// </summary>
        public Path Path
        {
            get => path;
            set
            {
                path = value;
                offset = 0;
                Segment = 0;
                SegmentRelative = 0;
            }
        }
        private Path path;

        /// <summary>
        /// Offset on the curve from the start of the curve (0 to Path.TotalLength)
        /// </summary>
        public float Offset
        {
            get => offset;
            set
            {
                if (offset != value)
                {
                    offset = value;
                    throw new System.NotImplementedException("Use Path.Evaluate");
                }
            }
        }
        private float offset = 0;

        protected int Segment { get; set; } = 0;
        protected float SegmentRelative { get; set; } = 0;

        /// <summary>
        /// The calculated position on the curve
        /// </summary>
        public Vector2 Position
        {
            get
            {
                return Vector2.CatmullRom(
                    path.ControlPoints[MathHelper.Max(0, Segment - 1)],
                    path.ControlPoints[Segment],
                    path.ControlPoints[MathHelper.Min(Path.ControlPoints.Count - 1, Segment + 1)],
                    path.ControlPoints[MathHelper.Min(Path.ControlPoints.Count - 1, Segment + 2)],
                    SegmentRelative / path.SegmentLengths[Segment]
                );
            }
        }

        /// <summary>
        /// Move along the path
        /// </summary>
        /// <param name="relative">The relative position to move (> 0 for forward, < 0 for backwards)</param>
        public void Move(float relative)
        {
            offset = MathHelper.Clamp(offset + relative, 0, path.TotalLength);
            SegmentRelative += relative;
            var segLength = path.SegmentLengths[Segment];

            if (SegmentRelative < 0)
            {
                --Segment;
                if (Segment < 0)
                {
                    Segment = 0;
                    SegmentRelative = 0;
                }
                else
                    SegmentRelative += path.SegmentLengths[Segment];
            }
            else if (SegmentRelative > segLength)
            {
                SegmentRelative -= segLength;
                ++Segment;

                if (Segment >= path.SegmentLengths.Count)
                {
                    Segment = path.SegmentLengths.Count - 1;
                    SegmentRelative = path.SegmentLengths[Segment];
                }
            }
        }
    }
    */
}
