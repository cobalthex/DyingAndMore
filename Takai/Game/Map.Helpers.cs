using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public partial class MapBaseClass
    {
        internal void Resize(int newWidth, int newHeight)
        {
            Tiles = Tiles.Resize(newHeight, newWidth);

            if (tilesEffect != null)
            {
                tilesEffect.Parameters["TilesPerRow"].SetValue(TilesPerRow);
                tilesEffect.Parameters["TileSize"].SetValue(new Vector2(TileSize));
                tilesEffect.Parameters["MapSize"].SetValue(new Vector2(Width, Height));
                tilesEffect.Parameters["TilesImage"]?.SetValue(TilesImage);
                tilesEffect.Parameters["TilesLayout"]?.SetValue(tilesLayoutTexture);
            }

            GenerateCollisionMask();
        }

        /// <summary>
        /// Create a map that can be used as a canvas for showing things like particle systems
        /// </summary>
        /// <param name="size">The width and height of the map (in pixels)</param>
        /// <param name="initializeGraphics">Call <see cref="InitializeGraphics"/></param>
        /// <returns>the created map</returns>
        public static MapBaseInstance CreateCanvasMap(int size = 10000, bool initializeGraphics = true)
        {
            var map = new MapBaseClass
            {
                TileSize = size,
            };
            if (initializeGraphics)
                map.InitializeGraphics();
            var inst = map.Instantiate();
            return inst;
        }

        /// <summary>
        /// Get the collision value at a point on the map
        /// </summary>
        /// <param name="point">The point to check</param>
        /// <returns>True if in the map, false otherwise</returns>
        public bool IsInsideMap(Vector2 point)
        {
            if (!Bounds.Contains(point))
                return false;

            return CollisionMask[(int)point.Y >> CollisionMaskScale, (int)point.X >> CollisionMaskScale] > 0;
        }
        public byte DistanceToEdge(Vector2 point)
        {
            if (!Bounds.Contains(point))
                return 0;

            return CollisionMask[(int)point.Y >> CollisionMaskScale, (int)point.X >> CollisionMaskScale];
        }
    }

    public partial class MapBaseInstance
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

            var newSectorsX = (newWidth - 1) / MapBaseClass.SectorSize + 1;
            var newSectorsY = (newHeight - 1) / MapBaseClass.SectorSize + 1;

            if (Sectors == null)
                Sectors = new MapSector[newSectorsY, newSectorsX];
            else
                Sectors = Sectors.Resize(newSectorsY, newSectorsX);

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

        public IEnumerable<MapSector> EnumeratateSectorsInRegion(Rectangle region)
        {
            var sectors = GetOverlappingSectors(region);
            for (int y = sectors.Top; y < sectors.Bottom; ++y)
            {
                for (int x = sectors.Left; x < sectors.Right; ++x)
                    yield return Sectors[y, x];
            }
        }

        /// <summary>
        /// Find all entities within a certain radius
        /// </summary>
        /// <param name="position">The origin search point</param>
        /// <param name="searchRadius">The maximum search radius</param>
        /// <param name="searchInSectors">Also search entities in sectors</param>
        public List<EntityInstance> FindEntitiesInRegion(Vector2 position, float searchRadius)
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
                if (Vector2.DistanceSquared(ent.WorldPosition, position) <= ent.RadiusSq + radiusSq) //scale ent radius?
                    ents.Add(ent);
            }

            return new List<EntityInstance>(ents);
        }

        /// <summary>
        /// Find entities inside a rectangle
        /// </summary>
        /// <param name="region">The region of the map to search</param>
        /// <returns></returns>
        public List<EntityInstance> FindEntitiesInRegion(Rectangle region)
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
        public EntityInstance FindClosestEntity(Vector2 position, float searchRadius = 0)
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
                var dist = Vector2.DistanceSquared(ent.WorldPosition, position);
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
        
        /// <summary>
        /// Test a region to see if it contains any entities
        /// </summary>
        /// <param name="position">the point to search from</param>
        /// <param name="searchRadius">how far out to search before stopping. 0 for infinite</param>
        /// <returns>True if any entities are in the region</returns>
        public bool TestRegionForEntities(Vector2 position, float searchRadius = 0)
        {
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
                var dist = Vector2.DistanceSquared(ent.WorldPosition, position);
                if ((searchRadius == 0 || dist <= radiusSq) && dist <= ent.RadiusSq + radiusSq)
                    return true;
            }

            return false;
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
        }

        public void CleanupOffscreen(CleanupOptions options)
        {
            throw new NotImplementedException();
        }

        public void RemoveAllEntities()
        {
            possibleOffscreenEntities.Clear();
            activeEntities.Clear();
            _allEntities.Clear();

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
        /// <param name="direction">In which direction to search. Should be normalized</param>
        /// <returns>The distance from start before colliding with a map edge"/></returns>
        public int TraceTiles(Vector2 start, Vector2 direction, int maxDistance = 1000)
        {
            //max distance?

            var pos = start;
            int total = 0;
            byte dist;
            do
            {
                total += (dist = Class.DistanceToEdge(pos));
                pos += direction * dist;
            } while (dist != 0 && total < maxDistance);

            return Math.Min(total, maxDistance);
        }

        /// <summary>
        /// Approximate the tangent of the wall based on the collision at a point in the map
        /// </summary>
        /// <param name="collision"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public Vector2 GetTilesCollisionTangent(Vector2 collision, Vector2 direction)
        {
            var tangent = Util.Ortho(direction);
            var a = Class.IsInsideMap(Util.Round(collision - tangent));
            var c = Class.IsInsideMap(Util.Round(collision + tangent));

            if (!a && c)
                return Vector2.Normalize(direction + tangent);
            if (a && !c)
                return Vector2.Normalize(direction - tangent);

            //todo: if -1 align to tile edge

            //todo: needs work

            return tangent;
        }

        public IEnumerable<MapSector> TraceSectors(Vector2 start, Vector2 direction, float maxDistance = 0)
        {
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
                    yield break;

                var sector = Sectors[(int)sectorPos.Y, (int)sectorPos.X];
                sectorPos += sectorDelta;
                yield return sector;
            }
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
        public TraceHit Trace(Vector2 start, Vector2 direction, float maxDistance = 0, EntityInstance ignored = null)
        {
            //todo: switch to using TraceSectors

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
                        Intersects(ent.WorldPosition, ent.RadiusSq, start, direction, out var t0, out var t1) && //todo: maybe add source ent radius to search
                        t0 < shortestDist)
                    {
                        shortest = ent;
                        shortestDist = (Math.Abs(t0) < Math.Abs(t1) ? t0 : t1); //todo: better way?
                    }
                }

                if (shortest != null && shortestDist <= maxDistance)
                {
                    var trace = TraceTiles(start, direction, (int)shortestDist);
                    if (trace >= (int)shortestDist)
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
                        distance = trace,
                        entity = null,
                        didHit = true
                    };
                }

                sectorPos += sectorDelta;
            }
            {
                var trace = TraceTiles(start, direction, (int)maxDistance);
                return new TraceHit()
                {
                    distance = trace,
                    entity = null,
                    didHit = trace < (int)maxDistance
                };
            }
        }

        public Trigger FindTriggerByName(string name)
        {
            foreach (var sector in Sectors)
            {
                var found = sector.triggers.Find((t) => t.Name == name);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// Check if the region is currently in its top left sector
        /// </summary>
        /// <param name="sectorX">the sector X index</param>
        /// <param name="sectorY">the sector Y index</param>
        /// <param name="region">The region to check (in pixels)</param>
        /// <returns>True if the sector contains the top left of the region</returns>
        public bool IsInFirstSector(int sectorX, int sectorY, Rectangle region)
        {
            return new Rectangle(
                sectorX * Class.SectorPixelSize,
                sectorY * Class.SectorPixelSize,
                Class.SectorPixelSize,
                Class.SectorPixelSize
            ).Contains(region.X, region.Y);
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