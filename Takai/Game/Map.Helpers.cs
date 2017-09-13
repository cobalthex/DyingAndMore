using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public partial class Map
    {
        protected Random random = new Random();
        private byte[] _r64b = new byte[8];

        protected float RandFloat(float Min, float Max)
        {
            return (float)(random.NextDouble() * (Max - Min)) + Min;
        }
        protected TimeSpan RandTime(TimeSpan Min, TimeSpan Max)
        {
            var diff = Max.Ticks - Min.Ticks;
            return TimeSpan.FromTicks(diff != 0 ? (BitConverter.ToInt64(_r64b, 0) % diff) : 0) + Min;
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
                if (ent.Class != null && ent.Class.Name.Equals(className, StringComparison.OrdinalIgnoreCase))
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

        public enum CleanupOptions
        {
            None            = 0,
            All             = ~0,
            Fluids          = 0b0001,
            Decals          = 0b0010,
            Particles       = 0b0100,
            DeadEntities    = 0b1000,
            //non players
        }

        public void CleanupAll(CleanupOptions options)
        {
            if (options.HasFlag(CleanupOptions.Fluids))
                ActiveFluids.Clear();

            if (options.HasFlag(CleanupOptions.DeadEntities))
                ActiveEnts.RemoveWhere((ent) => ent.State.State == EntStateId.Dead); //todo: use destroy?

            if (options.HasFlag(CleanupOptions.Particles))
                Particles.Clear();

            foreach (var sector in Sectors)
            {
                if (options.HasFlag(CleanupOptions.Fluids))
                    sector.fluids.Clear();

                if (options.HasFlag(CleanupOptions.Decals))
                    sector.decals.Clear();

                if (options.HasFlag(CleanupOptions.DeadEntities))
                    sector.entities.RemoveWhere((ent) => ent.State.State == EntStateId.Dead);
            }

            TotalEntitiesCount = System.Linq.Enumerable.Count(AllEntities);
        }

        public void CleanupOffscreen(CleanupOptions options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The outcome of a trace
        /// </summary>
        public struct TraceHit
        {
            public float distance;
            public EntityInstance entity; //null if collided with map
        }

        /// <summary>
        /// Test if a ray collides with a circle.
        /// If ray is inside of the circle, t0 and t1 = 0
        /// </summary>
        /// <param name="circle">the circle's origin to check</param>
        /// <param name="radiusSq">the squared radius of the cirlce</param>
        /// <param name="rayOrigin">the origin of the ray</param>
        /// <param name="rayDirection">the (normalized) direction of the ray</param>
        /// <param name="t0">the length along the ray where the ray collided with the circle, infinity if none</param>
        /// <param name="t1">the length along the ray where the ray collided with the back of the circle, infinity if none</param>
        /// <returns>true if the ray collided with the circle</returns>
        public bool Intersects(Vector2 circle, float radiusSq, Vector2 rayOrigin, Vector2 rayDirection, out float t0, out float t1)
        {
            //fast check (won't set t)
            //var rejection = Util.Reject(circleOrigin - lineStart, lineEnd - lineStart);
            //return rejection.LengthSquared() < radiusSq;

            //ray starts inside circle
            if (Vector2.DistanceSquared(circle, rayOrigin) < radiusSq)
            {
                t0 = t1 = 0;
                return true;
            }

            //does ray direction have to be normalized?

            var diff = circle - rayOrigin;
            var lf = Vector2.Dot(diff, rayDirection); //scalar projection
            var s = radiusSq - Vector2.Dot(diff, diff) + (lf * lf);

            //no collision
            if (s < 0)
            {
                t0 = t1 = float.PositiveInfinity;
                return false;
            }

            s = (float)Math.Sqrt(s);
            t0 = lf - s;
            t1 = lf + s;
            return t0 > 0; //make sure the object is in front of the ray
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
            var n = MathHelper.Max(Math.Abs(diff.X), Math.Abs(diff.Y)) / stepSize;
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
        /// <param name="maxDistance">The total distance to search (in the tilemap)</param>
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
            if (Math.Abs(sectorDiff.X) > Math.Abs(sectorDiff.Y))
                n = (int)Math.Ceiling(Math.Abs(sectorDiff.X));
            else
                n = (int)Math.Ceiling(Math.Abs(sectorDiff.Y));

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
                        shortestDist = (Math.Abs(t0) < Math.Abs(t1) ? t0 : t1); //todo: better way?
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