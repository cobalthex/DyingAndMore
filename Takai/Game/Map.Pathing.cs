using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public partial class Map
    {
        public struct PathPos
        {
            public Point pos;
            public int k0, k1;

            public PathPos(Point pos, int k0, int k1)
            {
                this.pos = pos;
                this.k0 = k0;
                this.k1 = k1;
            }
        }

        public struct PathCell
        {
            public int g, rhs, cost;
            public PathCell(int g, int rhs, int cost)
            {
                this.g = g;
                this.rhs = rhs;
                this.cost = cost;
            }
        }

        public class PathState
        {
            public const int C1 = 1; //? (min cost)?

            public PathPos start;
            public PathPos goal;
            public PathPos last;

            public int km = 0; //?

            public List<PathPos> path = new List<PathPos>();

            public Dictionary<Point, PathCell> cells = new Dictionary<Point, PathCell>();

            public static int Hueristic(PathPos a, PathPos b)
            {
                var diff = a.pos - b.pos;
                if (diff.X > diff.Y)
                {
                    var tmp = diff.X;
                    diff.X = diff.Y;
                    diff.Y = tmp;
                }
                return (int)((Math.Sqrt(2) - 1) * diff.X + diff.Y) * C1;
            }

            public PathPos CalculateKey(PathPos p)
            {
                var min = Math.Min(GetRHS(p), GetG(p));
                p.k0 = min + Hueristic(p, start) + km;
                p.k1 = min;
                return p;
            }

            public int GetRHS(PathPos p)
            {
                if (p.pos == goal.pos)
                    return 0;

                if (cells.TryGetValue(p.pos, out var cell))
                    return cell.rhs;

                return Hueristic(p, goal);
            }

            public int GetG(PathPos p)
            {
                if (cells.TryGetValue(p.pos, out var cell))
                    return cell.g;

                return Hueristic(p, goal);
            }
        }

        /// <summary>
        /// Find a path between two points
        /// </summary>
        /// <param name="start">Where to start the search from</param>
        /// <param name="end">Where to try and navigate to</param>
        /// <returns>The points in the navigation set, or null if not able to path to</returns>
        /// <remarks>Will navigate around entities that dont ignore trace</remarks>
        public List<Vector2> GetPath(Vector2 start, Vector2 goal, PathState state)
        {
            //hpa, mtd*, d* lite, jps?

            if (state == null)
                state = new PathState();

            state.start = new PathPos(start.ToPoint(), 0, 0);
            state.goal = new PathPos(goal.ToPoint(), 0, 0);

            var cell = new PathCell(0, 0, PathState.C1);
            state.cells[state.goal.pos] = cell;

            cell.g = cell.rhs = PathState.Hueristic(state.start, state.goal);
            state.cells[state.start.pos] = cell;

            state.start = state.CalculateKey(state.start);


            return null;
        }
    }
}
