using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public partial class MapClass
    {
        internal void Resize(int newWidth, int newHeight)
        {
            Tiles = Tiles.Resize(newHeight, newWidth);
        }

        /// <summary>
        /// Create a map that can be used as a canvas for showing things like particle systems
        /// </summary>
        /// <param name="size">The width and height of the map (in pixels)</param>
        /// <param name="initializeGraphics">Call <see cref="InitializeGraphics"/></param>
        /// <returns>the created map</returns>
        public static MapInstance CreateCanvasMap(int size = 10000, bool initializeGraphics = true)
        {
            var map = new MapClass
            {
                TileSize = size,
            };
            if (initializeGraphics)
                map.InitializeGraphics();
            var inst = map.Instantiate();
            inst.ActiveCamera = new Camera(new Vector2(size / 2));
            return inst;
        }
    }

    public partial class MapInstance
    {
        [Data.Serializer.Ignored]
        public Random Random { get; private set; } = new Random(); //todo: may not be necessary
        private byte[] _r64b = new byte[8];

        ///<summary>
        ///Resize the map (in tiles)
        ///</summary>
        public void Resize(int newWidth, int newHeight)
        {
            System.Diagnostics.Contracts.Contract.Requires(newWidth > 0);
            System.Diagnostics.Contracts.Contract.Requires(newHeight > 0);

            Class.Resize(newWidth, newHeight);

            Sectors = Sectors.Resize((newHeight - 1) / MapClass.SectorSize + 1, (newWidth - 1) / MapClass.SectorSize + 1);

            for (int y = 0; y < Sectors.GetLength(0); ++y)
            {
                for (int x = 0; x < Sectors.GetLength(1); ++x)
                {
                    if (Sectors[y, x] == null)
                        Sectors[y, x] = new MapSector();
                }
            }

            PathInfo = PathInfo.Resize(newHeight, newWidth);
            for (int y = 0; y < newHeight; ++y)
            {
                for (int x = 0; x < newWidth; ++x)
                {
                    if (PathInfo[y, x].generation == 0)
                        PathInfo[y, x] = new PathTile { heuristic = uint.MaxValue, generation = 0 };
                }
            }
        }

        /// <summary>
        /// Get the sector of a point
        /// </summary>
        /// <param name="position">The point in the map</param>
        /// <returns>The sector coordinates. Clamped to the map bounds</returns>
        public Point GetOverlappingSector(Vector2 position)
        {
            return Vector2.Clamp(position / Class.SectorPixelSize, Vector2.Zero, new Vector2(Sectors.GetLength(1) - 1, Sectors.GetLength(0) - 1)).ToPoint();
        }

        /// <summary>
        /// Get the sectors overlapping a region
        /// </summary>
        /// <param name="region">The region to consider (in pixels)</param>
        /// <returns>The sectors (start/end) contained by this region (Clamped in the map bounds)</returns>
        public Rectangle GetOverlappingSectors(Rectangle region)
        {
            var x = region.X / Class.SectorPixelSize;
            var y = region.Y / Class.SectorPixelSize;
            var rect = new Rectangle(
                x, y,
                Util.CeilDiv(region.Right, Class.SectorPixelSize) - x,
                Util.CeilDiv(region.Bottom, Class.SectorPixelSize) - y
            );
            return Rectangle.Intersect(rect, new Rectangle(0, 0, Sectors.GetLength(1), Sectors.GetLength(0)));
        }

        public IEnumerable<EntityInstance> EnumerateEntitiesInSectors(Rectangle sectors)
        {
            //sectors = Rectangle.Intersect(sectors, new Rectangle(0, 0, Sectors.GetLength(1), Sectors.GetLength(0)));
            for (int y = sectors.Top; y < sectors.Bottom; ++y)
                for (int x = sectors.Left; x < sectors.Right; ++x)
                    foreach (var ent in Sectors[y, x].entities)
                        yield return ent; //todo: needs to test if >= sector bounds (to not draw twice)
        }

        public IEnumerable<EntityInstance> EnumerateVisibleEntities()
        {
            return EnumerateEntitiesInSectors(GetOverlappingSectors(ActiveCamera.VisibleRegion));
        }

        /// <summary>
        /// Find all entities within a certain radius
        /// </summary>
        /// <param name="position">The origin search point</param>
        /// <param name="searchRadius">The maximum search radius</param>
        /// <param name="searchInSectors">Also search entities in sectors</param>
        public List<EntityInstance> FindEntities(Vector2 position, float searchRadius)
        {
            var ents = new HashSet<EntityInstance>();

            var radiusSq = searchRadius * searchRadius;

            var rect = new Rectangle(
                (int)(position.X - searchRadius),
                (int)(position.Y - searchRadius),
                (int)(searchRadius * 2),
                (int)(searchRadius * 2)
            );
            var sectors = GetOverlappingSectors(rect);

            foreach (var ent in EnumerateEntitiesInSectors(sectors))
            {
                if (Vector2.DistanceSquared(ent.Position, position) <= ent.RadiusSq + radiusSq)
                    ents.Add(ent);
            }

            return new List<EntityInstance>(ents);
        }

        /// <summary>
        /// Find entities inside a rectangle
        /// </summary>
        /// <param name="region">The region of the map to search</param>
        /// <returns></returns>
        public List<EntityInstance> FindEntities(Rectangle region)
        {
            var ents = new HashSet<EntityInstance>();

            var sectors = GetOverlappingSectors(region);
            foreach (var ent in EnumerateEntitiesInSectors(sectors))
            {
                if (region.Intersects(ent.AxisAlignedBounds))
                    ents.Add(ent);
            }

            return new List<EntityInstance>(ents);
        }

        /// <summary>
        /// Find the closest entity to the point within the search radius
        /// </summary>
        /// <param name="position">the point to search from</param>
        /// <param name="searchRadius">how far out to search before stopping. 0 for infinite</param>
        /// <returns>The closest entity or null if none are found</returns>
        public EntityInstance FindEntity(Vector2 position, float searchRadius = 0)
        {
            var radiusSq = searchRadius * searchRadius;

            var rect = new Rectangle(
                (int)(position.X - searchRadius),
                (int)(position.Y - searchRadius),
                (int)(searchRadius * 2),
                (int)(searchRadius * 2)
            );
            var sectors = GetOverlappingSectors(rect);

            float minDist = float.MaxValue;
            EntityInstance closest = null;

            foreach (var ent in EnumerateEntitiesInSectors(sectors))
            {
                var dist = Vector2.DistanceSquared(ent.Position, position);
                if ((searchRadius == 0 || dist <= radiusSq) && dist <= ent.RadiusSq + radiusSq)
                {
                    minDist = dist;
                    closest = ent;
                }
            }

            return closest;
        }

        /// <summary>
        /// Find an entity by its name
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

        public EntityInstance FindEntityById(int id)
        {
            foreach (var ent in AllEntities)
            {
                if (ent.Id == id)
                    return ent;
            }
            return null;
        }

        public List<EntityInstance> FindEntitiesByClassName(string name)
        {
            var ents = new List<EntityInstance>();
            foreach (var ent in AllEntities)
            {
                if (ent.Class?.Name == name)
                    ents.Add(ent);
            }
            return ents;
        }

        public enum CleanupOptions
        {
            None = 0,
            All = ~0,
            Fluids = 0b0001,
            Decals = 0b0010,
            Particles = 0b0100,
            DeadEntities = 0b1000,
            //non players
        }

        public void CleanupAll(CleanupOptions options)
        {
            if (options.HasFlag(CleanupOptions.Fluids))
                LiveFluids.Clear();

            if (options.HasFlag(CleanupOptions.Particles))
                Particles.Clear();

            foreach (var sector in Sectors)
            {
                if (options.HasFlag(CleanupOptions.Fluids))
                    sector.fluids.Clear();

                if (options.HasFlag(CleanupOptions.Decals))
                    sector.decals.Clear();

                if (options.HasFlag(CleanupOptions.DeadEntities))
                    sector.entities.RemoveWhere((ent) => !ent.IsAlive);
            }

            if (options.HasFlag(CleanupOptions.DeadEntities))
            {
                //todo: this is crashy
                foreach (var ent in _allEntities)
                {
                    if (!ent.IsAlive)
                        FinalDestroy(ent);
                }
            }
        }

        public void CleanupOffscreen(CleanupOptions options)
        {
            throw new NotImplementedException();
        }

        public void RemoveAllEntities()
        {
            foreach (var sector in Sectors)
                sector.entities.Clear();
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
            var n = Math.Max(Math.Abs(diff.X), Math.Abs(diff.Y)) / stepSize;
            var delta = Vector2.Normalize(diff) * stepSize;

            for (int i = 0; i < n; ++i)
            {
                //todo: move to pre-calc (clip start/end at map bounds)
                if (!Class.Bounds.Contains(pos))
                    return pos;

                var tile = Class.Tiles[(int)pos.Y / Class.TileSize, (int)pos.X / Class.TileSize];
                if (tile == -1)
                    return pos;

                var relPos = new Point((int)pos.X % Class.TileSize, (int)pos.Y % Class.TileSize);
                var mask = relPos.X + (relPos.Y * Class.TilesImage.Width);
                mask += (tile % Class.TilesPerRow) * Class.TileSize;
                mask += (tile / Class.TilesPerRow) * Class.TileSize * Class.TilesImage.Width;

                var collis = Class.CollisionMask[mask];
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

            var sectorPos = start / Class.SectorPixelSize;
            var sectorEnd = end / Class.SectorPixelSize;
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
                            entity = shortest,
                            didHit = true
                        };
                    }

                    return new TraceHit()
                    {
                        distance = Vector2.Distance(start, trace),
                        entity = null,
                        didHit = true
                    };
                }

                sectorPos += sectorDelta;
            }

            var dist = TraceTiles(start, end);
            return new TraceHit()
            {
                distance = Vector2.Distance(start, dist),
                entity = null,
                didHit = dist != end
            };
        }
    }

    /// <summary>
    /// The outcome of a trace
    /// </summary>
    public struct TraceHit
    {
        public bool didHit;
        public float distance;
        public EntityInstance entity; //null if collided with map
    }

}