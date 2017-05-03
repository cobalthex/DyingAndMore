using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    /// <summary>
    /// A 2D path calculated using hermite splines
    /// </summary>
    public class Path
    {
        /// <summary>
        /// The control points of this curve
        /// </summary>
        public List<Vector2> ControlPoints { get; private set; } = new List<Vector2>();
        public List<float> SegmentLengths { get; private set; } = new List<float>();
        public float TotalLength { get; private set; } = 0;

        public void AddPoint(Vector2 Point)
        {
            ControlPoints.Add(Point);

            if (ControlPoints.Count > 2)
            {
                var coarseLength = Vector2.Distance(ControlPoints[ControlPoints.Count - 2], Point);
                coarseLength = MathHelper.Min(5, coarseLength / 10);
                var length = 0f;

                Vector2 GetPoint(float val)
                {
                    int c = ControlPoints.Count - 1;
                    return Vector2.CatmullRom(
                        ControlPoints[MathHelper.Clamp(ControlPoints.Count - 4, 0, c)],
                        ControlPoints[MathHelper.Clamp(ControlPoints.Count - 3, 0, c)],
                        ControlPoints[MathHelper.Clamp(ControlPoints.Count - 2, 0, c)],
                        ControlPoints[MathHelper.Clamp(ControlPoints.Count - 1, 0, c)],
                        val
                    );
                }

                for (int i = 1; i < coarseLength; ++i)
                    length += Vector2.Distance(GetPoint((i - 1) / coarseLength), GetPoint(i / coarseLength));

                SegmentLengths.Add(length);
                TotalLength += length;
            }
        }

        /// <summary>
        /// Calculate the position on the curve given a specific T value
        /// </summary>
        /// <param name="percent">The T value to test. Clamped between 0 and 1</param>
        /// <returns>The calculated point on the curve</returns>
        /// <remarks>Requires at least one point</remarks>
        public Vector2 Evaluate(float percent)
        {
            throw new System.NotImplementedException("Todo");
        }
    }

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
}
