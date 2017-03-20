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
        public List<Vector2> ControlPoints { get; set; }

        /// <summary>
        /// Calculate the position on the curve given a specific T value
        /// </summary>
        /// <param name="percent">The T value to test. Clamped between 0 and 1</param>
        /// <returns>The calculated point on the curve</returns>
        /// <remarks>Requires at least one point</remarks>
        public Vector2 Evaluate(float percent)
        {
            if (percent < 0)
                return ControlPoints[0];

            if (percent >= 1)
                return ControlPoints[ControlPoints.Count - 1];

            var startSegment = (int)(percent * ControlPoints.Count);

            var weight = (percent - ((float)startSegment / ControlPoints.Count)) * ControlPoints.Count;

            return Vector2.CatmullRom(
                ControlPoints[MathHelper.Max(0, startSegment - 1)],
                ControlPoints[startSegment],
                ControlPoints[MathHelper.Min(ControlPoints.Count - 1, startSegment + 1)],
                ControlPoints[MathHelper.Min(ControlPoints.Count - 1, startSegment + 2)],
                weight
            );
        }
    }
}
