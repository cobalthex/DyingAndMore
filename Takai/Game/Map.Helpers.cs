using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    public partial class Map
    {
        protected System.Random random = new System.Random();
        private byte[] _r64b = new byte[8];

        protected float RandFloat(float Min, float Max)
        {
            return (float)(random.NextDouble() * (Max - Min)) + Min;
        }
        protected System.TimeSpan RandTime(System.TimeSpan Min, System.TimeSpan Max)
        {
            var diff = Max.Ticks - Min.Ticks;
            return System.TimeSpan.FromTicks(diff != 0 ? (System.BitConverter.ToInt64(_r64b, 0) % diff) : 0) + Min;
        }
        protected Vector2 RandVector2(Vector2 Min, Vector2 Max)
        {
            return new Vector2
            (
                RandFloat(Min.X, Max.X),
                RandFloat(Min.Y, Max.Y)
            );
        }

        /// <summary>
        /// Get the sector of a point
        /// </summary>
        /// <param name="position">The point in the map</param>
        /// <returns>The sector coordinates. Clamped to the map bounds</returns>
        public Point GetOverlappingSector(Vector2 position)
        {
            return Vector2.Clamp(position / SectorPixelSize, Vector2.Zero, new Vector2(Sectors.GetLength(1) - 1, Sectors.GetLength(0) - 1)).ToPoint();
        }

        /// <summary>
        /// Get the region of sectors overlapping this region
        /// </summary>
        /// <param name="region">The region to consider</param>
        /// <returns>The region of sectors contained by this region (Clamped in the map bounds)</returns>
        public Rectangle GetOverlappingSectors(Rectangle region)
        {
            var rect = new Rectangle(
                MathHelper.Max(0, region.X / SectorPixelSize),
                MathHelper.Max(0, region.Y / SectorPixelSize),
                ((region.X % SectorPixelSize) + region.Width - 1) / SectorPixelSize + 1,
                ((region.Y % SectorPixelSize) + region.Height - 1) / SectorPixelSize + 1
            );
            rect.Width = MathHelper.Min((Width - 1) / SectorSize + 1 - rect.X, rect.Width);
            rect.Height = MathHelper.Min((Height - 1) / SectorSize + 1 - rect.Y, rect.Height);
            return rect;
        }

        /// <summary>
        /// Check if a point is 'inside' the map
        /// </summary>
        /// <param name="position">The point to test</param>
        /// <returns>True if the point is in a navicable area</returns>
        public bool IsInside(Vector2 position)
        {
            return IsInside(position.ToPoint());
        }
        /// <summary>
        /// Check if a point is 'inside' the map
        /// </summary>
        /// <param name="position">The point to test</param>
        /// <returns>True if the point is in a navicable area</returns>
        public bool IsInside(Point position)
        {
            return (position.X >= 0 && position.X < (Width * TileSize) && position.Y >= 0 && position.Y < (Height * TileSize));
        }

        /// <summary>
        /// Find all entities within a certain radius
        /// </summary>
        /// <param name="position">The origin search point</param>
        /// <param name="searchRadius">The maximum search radius</param>
        /// <param name="searchInSectors">Also search entities in sectors</param>
        public List<EntityInstance> FindEntities(Vector2 position, float searchRadius, bool searchInSectors = false)
        {
            var radiusSq = searchRadius * searchRadius;
            var vr = new Vector2(searchRadius);

            List<EntityInstance> ents = new List<EntityInstance>();

            foreach (var ent in ActiveEnts)
            {
                if (Vector2.DistanceSquared(ent.Position, position) < radiusSq + ent.RadiusSq)
                    ents.Add(ent);
            }

            if (searchInSectors && Bounds.Contains(position))
            {
                var mapSz = new Vector2(Width, Height);
                var start = Vector2.Clamp((position - vr) / SectorPixelSize, Vector2.Zero, mapSz).ToPoint();
                var end = (Vector2.Clamp((position + vr) / SectorPixelSize, Vector2.Zero, mapSz) + Vector2.One).ToPoint();

                for (int y = start.Y; y < end.Y; ++y)
                {
                    for (int x = start.X; x < end.X; ++x)
                    {
                        foreach (var ent in Sectors[y, x].entities)
                        {
                            if (Vector2.DistanceSquared(ent.Position, position) < radiusSq + ent.RadiusSq)
                                ents.Add(ent);
                        }
                    }
                }
            }

            return ents;
        }

        /// <summary>
        /// Find entities inside a rectangle
        /// </summary>
        /// <param name="Region">The rectangle to search</param>
        /// <param name="SearchInSectors">Also search entities in sectors</param>
        /// <returns></returns>
        public List<EntityInstance> FindEntities(Rectangle Region, bool SearchInSectors = false)
        {
            List<EntityInstance> ents = new List<EntityInstance>();

            foreach (var ent in ActiveEnts)
            {
                if (Rectangle.Union(Region, ent.AxisAlignedBounds).Contains(ent.Position))
                    ents.Add(ent);
            }

            if (SearchInSectors)
            {
                var searchSectors = new Rectangle(
                    Region.X / SectorPixelSize,
                    Region.Y / SectorPixelSize,
                    ((Region.Width - 1) / SectorPixelSize) + 1,
                    ((Region.Height - 1) / SectorPixelSize) + 1
                );

                for (int y = searchSectors.Top; y < searchSectors.Bottom; ++y)
                {
                    for (int x = searchSectors.Left; x < searchSectors.Right; ++x)
                    {
                        foreach (var ent in Sectors[y, x].entities)
                        {
                            if (Rectangle.Union(Region, ent.AxisAlignedBounds).Contains(ent.Position))
                                ents.Add(ent);
                        }
                    }
                }
            }

            return ents;
        }

        /// <summary>
        /// Find all of the entities
        /// </summary>
        /// <param name="class">The class type to search</param>
        /// <param name="SearchInactive">Search in the inactive entities as well</param>
        /// <returns>A list of entities found</returns>
        public List<EntityInstance> FindEntitiesByClass(EntityClass @class, bool SearchInactive = false)
        {
            var ents = new List<EntityInstance>();
            foreach (var ent in ActiveEnts)
            {
                if (ent.Class == @class)
                    ents.Add(ent);
            }

            return ents;
            //todo: find inactive
        }

        public List<EntityInstance> FindEntitiesByClassName(string className, bool searchInactive = false)
        {
            var ents = new List<EntityInstance>();
            foreach (var ent in ActiveEnts)
            {
                if (ent.Class != null && ent.Class.Name.Equals(className, System.StringComparison.OrdinalIgnoreCase))
                    ents.Add(ent);
            }

            return ents;
            //todo: find all inactive by class name
        }

        /// <summary>
        /// Find an  entity by its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The first entity found or null if none</returns>
        /// <remarks>Searches active ents and then ents in sectors from 0 to end</remarks>
        public EntityInstance FindEntityByName(string name)
        {
            foreach (var ent in AllEntities)
            {
                if (ent.Name == name)
                    return ent;
            }

            return null;
        }

        /// <summary>
        /// Find a path between two points
        /// </summary>
        /// <param name="Start">Where to start the search from</param>
        /// <param name="End">Where to try and navigate to</param>
        /// <returns>The points in the navigation set, or null if not able to path to</returns>
        /// <remarks>Will navigate around entities that dont ignore trace</remarks>
        public List<Vector2> GetPath(Vector2 Start, Vector2 End)
        {
            //todo: use mtd* lite

            //jps
            /*var successors:Vector.< Node > = new Vector.< Node > ();
            var neighbours:Vector.< Node > = nodeNeighbours(current);

            for each(var neighbour: Node in neighbours) {
                // Direction from current node to neighbor:
                var dX:int = clamp(neighbour.x - current.x, -1, 1);
                var dY:int = clamp(neighbour.y - current.y, -1, 1);


                // Try to find a node to jump to:
                var jumpPoint:Node = jump(current.x, current.y, dX, dY, start, end);


                // If found add it to the list:
                if (jumpPoint) successors.push(jumpPoint);
            }*/

            var successors = new List<Vector2>();
            var neighbors = new List<Vector2>(new[] { Start });

            for (var i = 0; i < neighbors.Count; ++i)
            {

            }

            //return successors;

            return null;
        }

        /// <summary>
        /// The outcome of a trace
        /// </summary>
        public struct TraceHit
        {
            public float distance;
            public EntityInstance entity; //null if collided with map
        }

        public bool Intersects(Vector2 circle, float radiusSq, Vector2 rayOrigin, Vector2 rayDirection, out float t0, out float t1)
        {
            //fast check (won't set t)
            //var rejection = Util.Reject(circleOrigin - lineStart, lineEnd - lineStart);
            //return rejection.LengthSquared() < radiusSq;

            var diff = circle - rayOrigin;
            var lf = Vector2.Dot(rayDirection, diff);
            var s = radiusSq - Vector2.Dot(diff, diff) + (lf * lf);

            if (s < 0)
            {
                t0 = t1 = 0;
                return false;
            }

            s = (float)System.Math.Sqrt(s);
            t0 = lf - s;
            t1 = lf + s;
            return t0 > 0;

            //alternate, more complex solution (uses quadratic formula)

            //D = ray direction, ∆ = ray origin - circle origin, R = circle radius
            // δ = (D · ∆)² − |D|² * (|∆|² - R²)
            // if δ < 0 then no collision, if δ = 0, then tangent, δ > 0 then secant

            /*
            var d = rayOrigin - circle;
            var c = Vector2.Dot(d, d) - radiusSq;
            var b = Vector2.Dot(rayDirection, d);
            var a = Vector2.Dot(rayDirection, rayDirection); //1 if normalized

            float disc = b * b - a * c;
            if (disc < 0.0f)
            {
                t0 = t1 = 0;
                return false;
            }
            //if disc = 0, tangent
            //if disc > 0, secant

            //t = [(−D · ∆) ± √δ] ÷ |D|²

            float sqrtDisc = (float)System.Math.Sqrt(disc);
            float invA = 1 / a;

            t0 = (-b - sqrtDisc) * invA;
            t1 = (-b + sqrtDisc) * invA;
            //todo: flip

            return (t0 > 0);
            */

            //collisions = rayOrigin + t * rayDirection
            //normals = (collisions - c) * (1 / radius)
        }

        /// <summary>
        /// Trace a line in the tilemap
        /// </summary>
        /// <param name="start">Where to search from</param>
        /// <param name="end">Where to search to</param>
        /// <param name="stepSize">how far to skip between steps (larger numbers = faster/less accurate)</param>
        /// <returns>The location of the collision, or end if none found</returns>
        public Vector2 TraceTiles(Vector2 start, Vector2 end, float stepSize = 5)
        {
            //todo: switch to points?

            var pos = start;
            var diff = (end - start);
            var n = MathHelper.Max(System.Math.Abs(diff.X), System.Math.Abs(diff.Y)) / stepSize;
            var delta = Vector2.Normalize(diff) * stepSize;

            for (int i = 0; i < n; ++i)
            {
                //todo: move to pre-calc (clip start/end at map bounds)
                if (!IsInside(pos))
                    return pos;

                var tile = Tiles[(int)pos.Y / TileSize, (int)pos.X / TileSize];
                if (tile == -1)
                    return pos;

                var relPos = new Point((int)pos.X % TileSize, (int)pos.Y % TileSize);
                var mask = relPos.X + (relPos.Y * TilesImage.Width);
                mask += (tile % TilesPerRow) * TileSize;
                mask += (tile / TilesPerRow) * TileSize * TilesImage.Width;

                var collis = CollisionMask[mask];
                if (!collis)
                    return pos;

                //DrawRect(new Rectangle((int)pos.X, (int)pos.Y, 1, 1), Color.MintCream);
                pos += delta;
            }

            return end;
        }

        /// <summary>
        /// Cast a ray from start in direction (searching at most maxDistance)
        /// and check for entity and tile collisions.
        /// Entities located at start are ignored
        /// </summary>
        /// <param name="start">Where to start the search</param>
        /// <param name="direction">What direction to search</param>
        /// <param name="maxDistance">The total distance to search0</param>
        /// <param name="ignored">Any entities to ignore when searching</param>
        /// <returns>The collision. Entity is null if tilemap collision</returns>
        public TraceHit Trace(Vector2 start, Vector2 direction, float maxDistance = 0, EntityInstance ignored = null)
        {
            if (maxDistance <= 0) //support flipping?
                maxDistance = 10000;

            var end = (start + direction * maxDistance);

            var sectorPos = start / SectorPixelSize;
            var sectorEnd = end / SectorPixelSize;
            var sectorDiff = sectorEnd - sectorPos;

            int n = 0;
            if (System.Math.Abs(sectorDiff.X) > System.Math.Abs(sectorDiff.Y))
                n = (int)System.Math.Ceiling(System.Math.Abs(sectorDiff.X));
            else
                n = (int)System.Math.Ceiling(System.Math.Abs(sectorDiff.Y));

            var sectorDelta = sectorDiff / n;

            //todo: visited ents?
            
            for (int i = 0; i <= n; ++i) //todo: improve on number of sectors searching
            {
                if (!new Rectangle(0, 0, Sectors.GetLength(1), Sectors.GetLength(0)).Contains(sectorPos.ToPoint()))
                    break;

                var sector = Sectors[(int)sectorPos.Y, (int)sectorPos.X];

                EntityInstance shortest = null;
                var shortestDist = float.MaxValue;
                foreach (var ent in sector.entities)
                {
                    if (!ent.Class.IgnoreTrace &&
                        ent != ignored &&
                        Intersects(ent.Position, ent.RadiusSq, start, direction, out var t0, out var t1) && //todo: maybe add source ent radius to search
                        t0 < shortestDist)
                    {
                        shortest = ent;
                        shortestDist = (System.Math.Abs(t0) < System.Math.Abs(t1) ? t0 : t1); //todo: better way?
                    }
                }

                if (shortest != null && shortestDist <= maxDistance)
                {
                    var target = start + direction * shortestDist;
                    var trace = TraceTiles(start, target);
                    if (trace == target)
                    {
                        return new TraceHit()
                        {
                            distance = shortestDist,
                            entity = shortest
                        };
                    }

                    return new TraceHit()
                    {
                        distance = Vector2.Distance(start, trace),
                        entity = null
                    };
                }

                sectorPos += sectorDelta;
            }

            return new TraceHit()
            {
                distance = Vector2.Distance(start, TraceTiles(start, end)),
                entity = null
            };
        }
    }
}