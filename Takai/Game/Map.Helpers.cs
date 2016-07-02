using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    public partial class Map
    {
        /// <summary>
        /// Find all entities within a certain radius
        /// </summary>
        /// <param name="Position">The origin search point</param>
        /// <param name="Radius">The maximum search radius</param>
        public List<Entity> GetNearbyEntities(Vector2 Position, float Radius)
        {
            var radiusSq = Radius * Radius;
            var vr = new Vector2(Radius);

            var mapSz = new Vector2(Width, Height);
            var start = Vector2.Clamp((Position - vr) / sectorPixelSize, Vector2.Zero, mapSz).ToPoint();
            var end = (Vector2.Clamp((Position + vr) / sectorPixelSize, Vector2.Zero, mapSz) + Vector2.One).ToPoint();

            List<Entity> ents = new List<Entity>();
            for (int y = start.Y; y < end.Y; y++)
            {
                for (int x = start.X; x < end.X; x++)
                {
                    foreach (var ent in Sectors[y, x].entities)
                    {
                        if (Vector2.DistanceSquared(ent.Position, Position) < radiusSq)
                            ents.Add(ent);
                    }
                }
            }

            return ents;
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
        /// Trace a line, searching active entities on the map to see if there is a collision
        /// </summary>
        /// <param name="Start">The starting position to search</param>
        /// <param name="Direction">The direction to search from</param>
        /// <param name="T">How far in <paramref name="Direction"/> from <paramref name="Start"/> the collision occcured</param>
        /// <returns>An entity, if found. Null if none</returns>
        /// <remarks>Does not perform any map testing (use CanSee)</remarks>
        public Entity TraceLine(Vector2 Start, Vector2 Direction, out float T, float MaxDistance = 0)
        {
            SortedDictionary<float, Entity> ents = new SortedDictionary<float, Entity>(); //ents sorted by distance

            MaxDistance *= MaxDistance;

            //find the closest entity
            foreach (var ent in ActiveEnts)
            {
                if (ent.Position == Start)
                    continue;

                var diff = ent.Position - Start;

                //must be in front of vector
                if (Vector2.Dot(diff, Direction) <= 0)
                    continue;

                var lsq = diff.LengthSquared();
                if (lsq <= MaxDistance)
                    ents.Add(lsq, ent);
            }

            foreach (var ent in ents)
            {
                //check intersection
                
            }
            
            T = 0;
            return null;
        }
    }
}
