using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public partial class MapBaseClass
    {
        public bool CanPath(Point tile)
        {
            if (!TileBounds.Contains(tile))
                return false;
            return Tiles[tile.Y, tile.X] >= 0;
        }
    }

    public partial class MapBaseInstance
    {
        public struct AStarPathNode
        {
            public Point tile;
            public int cost;
            public int score;

            public AStarPathNode(Point tile, int cost, int score)
            {
                this.tile = tile;
                this.cost = cost;
                this.score = score;
            }

            public override int GetHashCode()
            {
                return (int)((tile.X << 4) & 0xffff0000) + (tile.Y & 0x0000ffff);
            }
        }

        public const int StraightCost = 10;
        public const int DiagonalCost = 14;

        readonly static AStarPathNode[] SearchDirections =
        {
            new AStarPathNode(new Point(-1, -1), DiagonalCost, 0),
            new AStarPathNode(new Point( 0, -1), StraightCost, 0),
            new AStarPathNode(new Point( 1, -1), DiagonalCost, 0),
            new AStarPathNode(new Point(-1,  0), StraightCost, 0),
            new AStarPathNode(new Point( 1,  0), StraightCost, 0),
            new AStarPathNode(new Point(-1,  1), DiagonalCost, 0),
            new AStarPathNode(new Point( 0,  1), StraightCost, 0),
            new AStarPathNode(new Point( 1,  1), DiagonalCost, 0)
        };

        public static int ManhattanDistance(Point start, Point end)
        {
            const int StraightCost = 10;
            const int DiagonalCost = 14;

            var dx = Math.Abs(start.X - end.X);
            var dy = Math.Abs(start.Y - end.Y);
            return StraightCost * (dx + dy) + (DiagonalCost - 2 * StraightCost) * Math.Min(dx, dy);
        }

        //fluids should affect cost of moving through tile

        public List<Point> AStarBuildPath(Vector2 start, Vector2 goal)
        {
            var pstart = (start / Class.TileSize).ToPoint();
            var pgoal = (goal / Class.TileSize).ToPoint();

            if (!Class.CanPath(pstart) || !Class.CanPath(pgoal))
                return new List<Point> { pstart };

            AStarPathNode goalNode = new AStarPathNode(pgoal, 0, 0)
            {
                score = ManhattanDistance(pstart, pgoal)
            };

            var open = new SortedList<int, HashSet<AStarPathNode>>(); //score: nodes -- //todo: use heap?
            var closed = new HashSet<Point>();
            var traveled = new List<Tuple<AStarPathNode, AStarPathNode>>(); //current and link

            int openCount = 0;
            void addOpen(AStarPathNode node)
            {
                if (!open.TryGetValue(node.score, out var list) || list == null)
                    open[node.score] = new HashSet<AStarPathNode> { node };
                else
                    list.Add(node);
                ++openCount;
            }
            void removeOpen(AStarPathNode node)
            {
                if (open.TryGetValue(node.score, out var set))
                {
                    set.Remove(node);
                    --openCount;
                }
            }

            addOpen(new AStarPathNode(pstart, 0, 0));
            //pathFindScores[(int)start.X, (int)start.Y] = GetManhattanDist(start, goal);

            while (openCount > 0)
            {
                AStarPathNode current = new AStarPathNode(Point.Zero, 0, int.MaxValue);
                foreach (var i in open)
                {
                    if (i.Value.Count > 0)
                    {
                        current = System.Linq.Enumerable.FirstOrDefault(i.Value);
                        break;
                    }
                }

                if (current.tile == pgoal) //path navigated
                    return ReconstructPath(traveled, goalNode);

                removeOpen(current);
                closed.Add(current.tile);
                //search in 8 directions around point
                foreach (var dir in SearchDirections)
                {
                    //jump to end
                    var neighbor = new AStarPathNode(current.tile + dir.tile, 0, 0);

                    if (closed.Contains(neighbor.tile) || !Class.CanPath(neighbor.tile)) //neighbor part of closed set or cannot be pathed on
                        continue;

                    //if diagonal check if cardinals are blocked to disable corner cutting
                    if ((Math.Abs(dir.tile.X) == Math.Abs(dir.tile.Y)) &&
                        (!Class.CanPath(new Point(current.tile.X, neighbor.tile.Y)) || !Class.CanPath(new Point(neighbor.tile.X, current.tile.Y))))
                        continue;

                    int cost = current.cost + dir.cost;

                    //the open list does not contain the neighbor or the neighbor has a better score
                    bool containsNeighbor = open.TryGetValue(neighbor.score, out var neighborsList) && neighborsList.Contains(neighbor);
                    if (!containsNeighbor || cost <= neighbor.cost)
                    {
                        if (containsNeighbor)
                            neighborsList.Remove(neighbor);

                        traveled.Add(new Tuple<AStarPathNode, AStarPathNode>(neighbor, current));
                        neighbor.cost = cost;
                        neighbor.score = cost + ManhattanDistance(neighbor.tile, pgoal);
                        addOpen(neighbor);
                    }
                }

            }

            return new List<Point> { pstart }; //No path between start and goal
        }

        List<Point> ReconstructPath(List<Tuple<AStarPathNode, AStarPathNode>> traveled, AStarPathNode current)
        {
            foreach (var t in traveled)
            {
                if (t.Item1.tile == current.tile)
                {
                    var p = ReconstructPath(traveled, t.Item2);
                    p.Add(current.tile);
                    return p;
                }
            }
            return new List<Point> { current.tile };
        }

        //namespace path info?

        public struct TileNav
        {
            public uint heuristic;
            internal uint generation;
            internal uint total;
            internal uint count;
        }
        internal uint navGeneration = 0;

        [Data.Serializer.Ignored]
        public TileNav[,] NavInfo { get; set; } = new TileNav[0, 0];

        internal uint MaxHeuristic = 0;

        public TileNav NavInfoAt(Vector2 position)
        {
            if (!Class.Bounds.Contains(position))
                return new TileNav { heuristic = uint.MaxValue };

            var tile = (position / Class.TileSize).ToPoint();
            return NavInfo[tile.Y, tile.X];
        }

        public struct HeuristicScore
        {
            public Point tile;
            public uint value;
        }

        internal static readonly Point[] HeuristicDirections =
        {
            new Point( 0, -1),
            new Point(-1,  0),
            new Point( 0,  1),
            new Point( 1,  0),
        };

        public static readonly Point[] NavigationDirections =
        {
            //new Point(-1, -1),
            new Point( 0, -1),
            //new Point( 1, -1),
            new Point(-1,  0),
            new Point( 1,  0),
            //new Point(-1,  1),
            new Point( 0,  1),
            //new Point( 1,  1),
        };

        public const uint WallPathingBias = 5;

        //dynamic heuristic that only builds to visible region/entities

        /// <summary>
        /// Build the hueristic from a speicified start point.
        /// Only overwrites areas that can be reached from start
        /// </summary>
        /// <param name="start">Where to start the fill</param>
        /// <param name="region">The area to update (in tiles)</param>
        public void BuildHeuristic(Point start, Rectangle region, bool blend = false)
        {
            if (!Class.CanPath(start))
                return;

            var visibleTiles = Rectangle.Intersect(Class.Bounds, new Rectangle(
                region.X / Class.TileSize,
                region.Y / Class.TileSize,
                Util.CeilDiv(region.Width, Class.TileSize),
                Util.CeilDiv(region.Height, Class.TileSize)
            ));

            ++navGeneration;
            NavInfo[start.Y, start.X] = new TileNav
            {
                heuristic = 0,
                generation = navGeneration
            };

            var queue = new Queue<HeuristicScore>(); //static?
            queue.Enqueue(new HeuristicScore { tile = start, value = 1 });
            while (queue.Count > 0)
            {
                var first = queue.Dequeue();
                var heuristic = first.value;

                uint edge = 0;
                foreach (var i in HeuristicDirections)
                {
                    var pos = first.tile + i;
                    if (Class.CanPath(pos) &&
                        visibleTiles.Contains(pos))
                    {
                        if (NavInfo[pos.Y, pos.X].generation != navGeneration)
                        {
                            if (blend)
                            {
                                var pi = NavInfo[pos.Y, pos.X];
                                pi.generation = navGeneration;
                                pi.total += heuristic;
                                ++pi.count;
                                pi.heuristic = Math.Min(heuristic, pi.heuristic + 1);
                                heuristic = pi.heuristic;
                                NavInfo[pos.Y, pos.X] = pi;
                            }
                            else
                            {
                                NavInfo[pos.Y, pos.X] = new TileNav
                                {
                                    heuristic = heuristic,
                                    generation = navGeneration,
                                    count = 1,
                                    total = first.value
                                };
                            }
                            queue.Enqueue(new HeuristicScore
                            {
                                tile = pos,
                                value = NavInfo[pos.Y, pos.X].heuristic + 1
                            });
                        }
                    }
                    else
                        ++edge;
                }

                // todo: need to bias against convex corners
                // pre-calc&cache where convex corners are and store them in a table?

                //bias against corners/walls
                if (edge > 1)
                {
                    var pi = NavInfo[first.tile.Y, first.tile.X];
                    pi.heuristic += edge + WallPathingBias;
                    NavInfo[first.tile.Y, first.tile.X] = pi;
                }
                MaxHeuristic = Math.Max(MaxHeuristic, heuristic);
            }
        }

        //https://stackoverflow.com/questions/15114950/how-to-improve-the-perfomance-of-my-a-path-finder/15120213#15120213

        /// <summary>
        /// Add an obstacle to the pathing grid
        /// </summary>
        /// <param name="region">The region to block out (in pixels)</param>
        /// <param name="weight">How strong the block</param>
        /// <param name="weightRadius">A falloff curve for the weight. 0 for constant</param>
        public void AddPathObstacle(Rectangle region, float weight = 4, float weightRadius = 0)
        {
            region.X /= Class.TileSize;
            region.Y /= Class.TileSize;
            region.Width = (region.Width - 1) / Class.TileSize + 1;
            region.Height = (region.Height - 1) / Class.TileSize + 1;

            //falloff based on radius
            var tilePos = region.Center.ToVector2();
            for (int tbY = Math.Max(0, region.Top); tbY < Math.Min(Class.Height, region.Bottom); ++tbY)
            {
                for (int tbX = Math.Max(0, region.Left); tbX < Math.Min(Class.Width, region.Right); ++tbX)
                {
                    if (NavInfo[tbY, tbX].heuristic == uint.MaxValue)
                        continue;

                    var dist = Vector2.DistanceSquared(tilePos, new Vector2(tbX, tbY));
                    var pi = NavInfo[tbY, tbX];
                    var rad = 1 - Math.Min(1, dist / weightRadius);
                    pi.heuristic = Math.Min(uint.MaxValue, pi.heuristic + (uint)(4 * rad));
                    NavInfo[tbY, tbX] = pi;
                }
            }
        }
    }
}
