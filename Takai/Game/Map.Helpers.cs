﻿using Microsoft.Xna.Framework;
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
            public EntityInstance entity;
        }

        //bool Intersects(Rectangle rect, Vector2 rayOrigin, Vector2 rayDirection)
        //{
        //    var tx1 = (rect.Left - rayOrigin.X) * r.n_inv.x;
        //    var tx2 = (rect.Right - rayOrigin.X) * r.n_inv.x;

        //    var tmin = System.Math.Min(tx1, tx2);
        //    var tmax = System.Math.Max(tx1, tx2);

        //    var ty1 = (rect.Top - rayOrigin.Y) * r.n_inv.y;
        //    var ty2 = (rect.Bottom - rayOrigin.Y) * r.n_inv.y;

        //    tmin = System.Math.Max(tmin, System.Math.Min(ty1, ty2));
        //    tmax = System.Math.Min(tmax, System.Math.Max(ty1, ty2));

        //    return tmax >= tmin;
        //}

        /// <summary>
        /// Traca line and check for collisions with entities and the map. Uses PotentialVisibleSet (same rules apply)
        /// </summary>
        /// <param name="start">The starting position to search</param>
        /// <param name="direction">The direction to search from</param>
        /// <param name="hit">Collision info, if collision exists</param>
        /// <param name="maxDistance">The maximum search distance</param>
        /// <param name="entsFilter">
        /// Provide an explicit list of entities to search through
        /// Key should be the squared distance between start and the entity
        /// If null, uses PotentialVisibleSet()
        /// </param>
        /// <returns>True if there was a collision</returns>
        public bool TraceLine(Vector2 start, Vector2 direction, out TraceHit hit, float maxDistance = 0)
        {
            if (maxDistance == 0)
                maxDistance = 10000;

            var target = start + direction * maxDistance;
            //todo: clip target at map bounds

            var stepSize = 5;

            var pos = start;
            var diff = (target - start);
            var n = MathHelper.Max(System.Math.Abs(diff.X), System.Math.Abs(diff.Y)) / stepSize;
            var delta = Vector2.Normalize(diff) * stepSize;

            for (int i = 0; i < n; ++i)
            {
                //todo: move to pre-calc
                if (!IsInside(pos))
                {
                    hit = new TraceHit()
                    {
                        distance = (pos - start).Length(),
                        entity = null
                    };
                    return true;
                }

                var tile = Tiles[(int)pos.Y / TileSize, (int)pos.X / TileSize];
                if (tile == -1)
                {
                    hit = new TraceHit()
                    {
                        distance = (pos - start).Length(),
                        entity = null
                    };
                    return true;
                }

                var relPos = new Point((int)pos.X % TileSize, (int)pos.Y % TileSize);
                var mask = relPos.X + (relPos.Y * TilesImage.Width);
                mask += (tile % TilesPerRow) * TileSize;
                mask += (tile / TilesPerRow) * TileSize * TilesImage.Width;

                var collis = CollisionMask[mask];
                if (!collis)
                {
                    hit = new TraceHit()
                    {
                        distance = (pos - start).Length(),
                        entity = null
                    };
                    return true;
                }

                //DrawRect(new Rectangle((int)pos.X, (int)pos.Y, 1, 1), Color.MintCream);
                pos += delta;
            }

            hit = new TraceHit();
            return false;
        }
    }
}