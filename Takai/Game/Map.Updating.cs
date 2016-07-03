using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;


namespace Takai.Game
{
    public partial class Map
    {
        /// <summary>
        /// Update the map state
        /// Updates the active set and then the contents of the active set
        /// </summary>
        /// <param name="Time">Delta time</param>
        /// <param name="Camera">Where on the map to view</param>
        /// <param name="Viewport">Where on screen to draw the map. The viewport is centered around the camera</param>
        public void Update(GameTime Time, Vector2 Camera, Rectangle Viewport)
        {
            var half = Camera - (new Vector2(Viewport.Width, Viewport.Height) / 2);

            var startX = (int)half.X / sectorPixelSize;
            var startY = (int)half.Y / sectorPixelSize;

            var width = 1 + ((Viewport.Width - 1) / sectorPixelSize);
            var height = 1 + ((Viewport.Height - 1) / sectorPixelSize);

            var activeRect = new Rectangle(startX - 1, startY - 1, width + 2, height + 2);
            var mapRect = new Rectangle(0, 0, Width * tileSize, Height * tileSize);
            var tileSq = new Vector2(tileSize).LengthSquared();

            var deltaT = (float)Time.ElapsedGameTime.TotalSeconds;

            //update active blobs
            for (int i = 0; i < ActiveBlobs.Count; i++)
            {
                var blob = ActiveBlobs[i];
                var deltaV = blob.velocity * deltaT;
                blob.position += deltaV;
                blob.velocity -= deltaV * blob.type.Drag;

                //todo: maybe add collision detection for better fluid simulation (drag increases when colliding)

                if (System.Math.Abs(blob.velocity.X) < 1 && System.Math.Abs(blob.velocity.Y) < 1)
                {
                    SpawnBlob(blob.position, Vector2.Zero, blob.type); //this will move the blob to the static area of the map
                    ActiveBlobs[i] = ActiveBlobs[ActiveBlobs.Count - 1];
                    ActiveBlobs.RemoveAt(ActiveBlobs.Count - 1);
                    i--;
                }
                else
                    ActiveBlobs[i] = blob;
            }

            //update active entities
            for (int i = 0; i < ActiveEnts.Count; i++)
            {
                var ent = ActiveEnts[i];
                if (!ent.AlwaysActive && !activeRect.Contains(ent.Position / sectorPixelSize))
                {
                    //ents outside the map are deleted
                    if (mapRect.Contains((ent.Position / tileSize).ToPoint()))
                        Sectors[(int)ent.Position.Y / sectorPixelSize, (int)ent.Position.X / sectorPixelSize].entities.Add(ent);
                    else
                    {
                        ent.Map = null;
                        ent.Unload();
                        continue;
                    }

                    //remove from active set (swap with last)
                    ActiveEnts[i] = ActiveEnts[ActiveEnts.Count - 1];
                    ActiveEnts.RemoveAt(ActiveEnts.Count - 1);
                    i--;
                }
                else
                {
                    if (!ent.IsEnabled)
                        continue;

                    ent.Think(Time);

                    var deltaV = ent.Velocity * deltaT;
                    var targetPos = ent.Position + deltaV;
                    var targetCell = (targetPos / tileSize).ToPoint();
                    var cellPos = new Point((int)targetPos.X % tileSize, (int)targetPos.Y % tileSize);

                    short tile;
                    if (!mapRect.Contains(ent.Position + deltaV) || (tile = Tiles[targetCell.Y, targetCell.X]) < 0)
                    // || !TilesMask[(tile / tilesPerRow) + cellPos.Y, (tile % tileSize) + cellPos.X])
                    {
                        ent.OnMapCollision(targetCell);

                        if (ent.IsPhysical)
                            ent.Velocity = Vector2.Zero;
                    }

                    else if (ent.Velocity != Vector2.Zero)
                    {
                        float t;
                        var target = TraceLine(ent.Position, ent.Direction, out t);
                        if (target != null && t * t < ent.RadiusSq + target.RadiusSq)
                        {
                            ent.OnEntityCollision(target);

                            if (ent.IsPhysical)
                                ent.Velocity = Vector2.Zero;
                        }

                        //perhaps use this for discrete simulation
                        ////todo: use potential visible set instead
                        //for (int j = 0; j < ActiveEnts.Count; j++)
                        //{
                        //    if (i != j && Vector2.DistanceSquared(ent.Position, ActiveEnts[j].Position) < ent.RadiusSq + ActiveEnts[j].RadiusSq)
                        //        ent.OnEntityCollision(ActiveEnts[j]);
                        //}
                    }

                    ent.Position += ent.Velocity * deltaT;
                }

                if (ent.Map == null)
                {
                    if (ent.Sector != null)
                    {
                        ent.Sector.entities.Remove(ent);
                        ent.Sector = null;
                    }
                    else
                        ActiveEnts.Remove(ent);
                    ent.Unload();
                }
            }

            //add new entities to active set (will be updated next frame)
            for (var y = System.Math.Max(activeRect.Top, 0); y < System.Math.Min(Height / sectorSize, activeRect.Bottom); y++)
            {
                for (var x = System.Math.Max(activeRect.Left, 0); x < System.Math.Min(Width / sectorSize, activeRect.Right); x++)
                {
                    ActiveEnts.AddRange(Sectors[y, x].entities);
                    Sectors[y, x].entities.Clear();
                }
            }
        }
    }
}
