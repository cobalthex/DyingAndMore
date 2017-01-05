﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    /// <summary>
    /// The outcome of a trace
    /// </summary>
    public struct TraceHit
    {
        public float distance;
        public Entity entity;
    }

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
        /// <param name="Position">The point in the map</param>
        /// <returns>The sector coordinates. Clamped to the map bounds</returns>
        public Point GetSector(Vector2 Position)
        {
            return Vector2.Clamp(Position / SectorPixelSize, Vector2.Zero, new Vector2(Sectors.GetLength(1) - 1, Sectors.GetLength(0) - 1)).ToPoint();
        }

        /// <summary>
        /// Check if a point is 'inside' the map
        /// </summary>
        /// <param name="Point">The point to test</param>
        /// <returns>True if the point is in a navicable area</returns>
        public bool IsInside(Vector2 Point)
        {
            return (Point.X >= 0 && Point.X < (Width * TileSize) && Point.Y >= 0 && Point.Y < (Height * TileSize));
        }

        /// <summary>
        /// Find all entities within a certain radius
        /// </summary>
        /// <param name="Position">The origin search point</param>
        /// <param name="Radius">The maximum search radius</param>
        public List<Entity> FindNearbyEntities(Vector2 Position, float Radius, bool SearchInSectors = false)
        {
            var radiusSq = Radius * Radius;
            var vr = new Vector2(Radius);

            List<Entity> ents = new List<Entity>();

            foreach (var ent in ActiveEnts)
            {
                if (Vector2.DistanceSquared(ent.Position, Position) < radiusSq + ent.RadiusSq)
                    ents.Add(ent);
            }

            if (SearchInSectors)
            {
                var mapSz = new Vector2(Width, Height);
                var start = Vector2.Clamp((Position - vr) / SectorPixelSize, Vector2.Zero, mapSz).ToPoint();
                var end = (Vector2.Clamp((Position + vr) / SectorPixelSize, Vector2.Zero, mapSz) + Vector2.One).ToPoint();

                for (int y = start.Y; y < end.Y; y++)
                {
                    for (int x = start.X; x < end.X; x++)
                    {
                        foreach (var ent in Sectors[y, x].entities)
                        {
                            if (Vector2.DistanceSquared(ent.Position, Position) < radiusSq + ent.RadiusSq)
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
        /// <typeparam name="TEntity">The type of entity to find</typeparam>
        /// <returns>A list of entities found</returns>
        public List<Entity> FindEntitiesByType<TEntity>(bool SearchInactive = false) where TEntity : Entity
        {
            List<Entity> ents = new List<Entity>();

            foreach (var ent in ActiveEnts)
            {
                if (ent is TEntity)
                    ents.Add(ent);
            }

            if (SearchInactive)
            {
                for (var y = 0; y < Sectors.GetLength(0); y++)
                {
                    for (var x = 0; x < Sectors.GetLength(1); x++)
                    {
                        foreach (var ent in Sectors[y, x].entities)
                        {
                            if (ent is TEntity)
                                ents.Add(ent);
                        }
                    }
                }
            }

            return ents;
        }

        /// <summary>
        /// Find an  entity by its name
        /// </summary>
        /// <param name="Name"></param>
        /// <returns>The first entity found or null if none</returns>
        /// <remarks>Searches active ents and then ents in sectors from 0 to end</remarks>
        public Entity FindEntityByName(string Name)
        {
            foreach (var ent in ActiveEnts)
            {
                if (ent.Name == Name)
                    return ent;
            }

            foreach (var s in (System.Collections.IEnumerable)Sectors)
            {
                foreach (var ent in ((MapSector)s).entities)
                    if (ent.Name == Name)
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
        /// Calculate a list of entities that are potentially visible (in front of the viewer)
        /// Only searches through active entities.
        /// </summary>
        /// <param name="Start">Where to look from</param>
        /// <param name="Direction">What direction to look in</param>
        /// <param name="FieldOfView">The field of view, in radians (0 to Pi)</param>
        /// <param name="MaxDistance">The maximum distance to search</param>
        /// <returns>The set of potential visible ents, sorted by distance (Dictionary of &lt;distance sq, entity&gt;)</returns>
        /// <remarks>Entities at Start or ignore traces are not added</remarks>
        public SortedDictionary<float, Entity> PotentialVisibleSet(Vector2 Start, Vector2 Direction, float FieldOfView = MathHelper.Pi, float MaxDistance = 0)
        {
            var ents = new SortedDictionary<float, Entity>();

            MaxDistance *= MaxDistance;
            FieldOfView = 1 - MathHelper.Clamp(FieldOfView / MathHelper.Pi, 0, 1);

            foreach (var ent in ActiveEnts)
            {
                if (ent.IgnoreTrace || ent.Position == Start)
                    continue;

                var diff = ent.Position - Start;

                //must be in front of viewer
                if (Vector2.Dot(diff, Direction) < FieldOfView) //todo: fix to handle non-normalized directions
                    continue;

                var lsq = diff.LengthSquared();
                if (MaxDistance == 0 || lsq <= MaxDistance)
                    ents[lsq] = ent;
            }

            return ents;
        }
        public string debugOut;

        /// <summary>
        /// Trace a line and check for collisions with entities and the map. Uses PotentialVisibleSet (same rules apply)
        /// </summary>
        /// <param name="Start">The starting position to search</param>
        /// <param name="Direction">The direction to search from</param>
        /// <param name="Hit">Collision info, if collided</param>
        /// <param name="MaxDistance">The maximum search distance (not used if EntsToSearch is provided)</param>
        /// <param name="EntsToSearch">
        /// Provide an explicit list of entities to search through
        /// Key should be the squared distance between start and the entity
        /// If null, uses PotentialVisibleSet()
        /// </param>
        /// <returns>True if there was a collision</returns>
        /// <remarks>Does not perform any map testing (use CanSee)</remarks>
        public bool TraceLine(Vector2 Start, Vector2 Direction, out TraceHit Hit, float MaxDistance = 0, SortedDictionary<float, Entity> EntsToSearch = null)
        {
            var lastT = 0f;
            var mapRect = new Rectangle(0, 0, Width, Height);

            const float JumpSize = 10;

            //check for intersections
            foreach (var ent in EntsToSearch ?? PotentialVisibleSet(Start, Direction, MathHelper.Pi, MaxDistance))
            {
                var diff = ent.Value.Position - Start;
                var lf = Vector2.Dot(Direction, diff);
                var s = ent.Value.RadiusSq - ent.Key + (lf * lf);

                var nextT = diff.Length(); //todo: find a better way
                //trace line along map to see if there are any collisions
                for (var t = lastT; t < nextT; t += JumpSize) //test out granularities (or may assumptions about map corners)
                {
                    var pos = (Start + (t * Direction)).ToPoint();
                    var tilePos = new Point(pos.X / TileSize, pos.Y / TileSize);
                    var tileRelPos = new Point(pos.X % TileSize, pos.Y % TileSize);

                    //may not be necessary
                    if (!mapRect.Contains(tilePos))
                    {
                        Hit = new TraceHit()
                        {
                            entity = null,
                            distance = t
                        };
                        return true;
                    }

                    var tile = Tiles[tilePos.Y, tilePos.X];
                    var mask = (tile * TileSize) + ((tile / TilesPerRow) * TileSize);
                    mask += (tileRelPos.Y * TilesImage.Width);
                    mask += tileRelPos.X;
                    if (tile < 0 || (mask < 0 || TilesMask[mask] == false))
                    {
                        Hit = new TraceHit()
                        {
                            entity = null,
                            distance = t
                        };
                        return true;
                    }

                    //todo: if tile == 0 (full tile), skip to next tile
                }
                lastT = nextT;

                if (s < 0)
                    continue; //no intersection

                s = (float)System.Math.Sqrt(s);
                if (lf < s)
                {
                    if (lf + s >= 0)
                    {
                        Hit = new TraceHit()
                        {
                            entity = ent.Value,
                            distance = lf + s
                        };
                        return true;
                    }
                    else
                        throw new System.Exception("When does this happen?");
                }
                else
                {
                    Hit = new TraceHit()
                    {
                        entity = ent.Value,
                        distance = lf - s
                    };
                    return true;
                }
            }

            //todo+ factor out map check code
            //todo: check forward pos
            for (var t2 = lastT; t2 < MathHelper.Min(MaxDistance, 100000); t2 += JumpSize)
            {
                var pos = (Start + (t2 * Direction)).ToPoint();
                var tilePos = new Point(pos.X / TileSize, pos.Y / TileSize);
                var tileRelPos = new Point(pos.X % TileSize, pos.Y % TileSize);

                //may not be necessary
                if (!mapRect.Contains(tilePos))
                {
                    Hit = new TraceHit()
                    {
                        entity = null,
                        distance = t2
                    };
                    return true;
                }

                var tile = Tiles[tilePos.Y, tilePos.X];
                var mask = (tile * TileSize) + ((tile / TilesPerRow) * TileSize);
                mask += (tileRelPos.Y * TilesImage.Width);
                mask += tileRelPos.X;
                if (tile < 0 || (mask < 0 || TilesMask[mask] == false))
                {
                    Hit = new TraceHit()
                    {
                        entity = null,
                        distance = t2 - JumpSize
                    };
                    return true;
                }
            }

            Hit = new TraceHit();
            return false;
        }
    }
}