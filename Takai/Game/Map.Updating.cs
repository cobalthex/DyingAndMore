using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;

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

            var startX = (int)half.X / SectorPixelSize;
            var startY = (int)half.Y / SectorPixelSize;

            var width = 1 + ((Viewport.Width - 1) / SectorPixelSize);
            var height = 1 + ((Viewport.Height - 1) / SectorPixelSize);

            var activeRect = new Rectangle(startX - 1, startY - 1, width + 2, height + 2);
            var mapRect = new Rectangle(0, 0, Width * tileSize, Height * tileSize);
            var tileSq = new Vector2(tileSize).LengthSquared();

            var deltaT = (float)Time.ElapsedGameTime.TotalSeconds;

            #region active blobs

            for (int i = 0; i < ActiveBlobs.Count; i++)
            {
                var blob = ActiveBlobs[i];
                var deltaV = blob.velocity * deltaT;
                blob.position += deltaV;
                blob.velocity -= deltaV * blob.type.Drag;

                //todo: maybe add collision detection for better fluid simulation (combine drag when colliding)

                if (System.Math.Abs(blob.velocity.X) < 1 && System.Math.Abs(blob.velocity.Y) < 1)
                {
                    Spawn(blob.type, blob.position, Vector2.Zero); //this will move the blob to the static area of the map
                    ActiveBlobs[i] = ActiveBlobs[ActiveBlobs.Count - 1];
                    ActiveBlobs.RemoveAt(ActiveBlobs.Count - 1);
                    i--;
                }
                else
                    ActiveBlobs[i] = blob;
            }

            #endregion

            #region active entities

            for (int i = 0; i < ActiveEnts.Count; i++)
            {
                var ent = ActiveEnts[i];
                if (!ent.AlwaysActive && !activeRect.Contains(ent.Position / SectorPixelSize))
                {
                    //ents outside the map are deleted
                    if (mapRect.Contains((ent.Position / tileSize).ToPoint()))
                        Sectors[(int)ent.Position.Y / SectorPixelSize, (int)ent.Position.X / SectorPixelSize].entities.Add(ent);
                    else
                    {
                        Destroy(ent);
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
                        ent.OnMapCollision(targetCell, targetPos);

                        if (ent.IsPhysical)
                            ent.Velocity = Vector2.Zero;
                    }

                    else if (ent.Velocity != Vector2.Zero)
                    {
                        float t;
                        var nv = ent.Velocity;
                        nv.Normalize();
                        var target = TraceLine(ent.Position, nv, out t);
                        if (target != null && t * t < ent.RadiusSq + target.RadiusSq)
                        {
                            ent.OnEntityCollision(target, ent.Position + (nv * t));

                            if (ent.IsPhysical)
                                ent.Velocity = Vector2.Zero;
                        }
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
                }
            }

            #endregion

            //add new entities to active set (will be updated next frame)
            for (var y = System.Math.Max(activeRect.Top, 0); y < System.Math.Min(Height / SectorSize, activeRect.Bottom); y++)
            {
                for (var x = System.Math.Max(activeRect.Left, 0); x < System.Math.Min(Width / SectorSize, activeRect.Right); x++)
                {
                    ActiveEnts.AddRange(Sectors[y, x].entities);
                    Sectors[y, x].entities.Clear();
                }
            }

            #region particles

            var ts = (float)Time.ElapsedGameTime.TotalSeconds;
            foreach (var p in Particles)
            {
                for (var i = 0; i < p.Value.Count; i++)
                {
                    var x = p.Value[i];

                    if (x.time == TimeSpan.Zero)
                        x.time = Time.TotalGameTime;
                    else if (Time.TotalGameTime > x.time + x.lifetime + x.delay)
                    {
                        p.Value[i] = p.Value[p.Value.Count - 1];
                        p.Value.RemoveAt(p.Value.Count - 1);
                        i--;
                        continue;
                    }

                    var life = (float)((Time.TotalGameTime - (x.time + x.delay)).TotalSeconds / x.lifetime.TotalSeconds);
                    
                    x.speed = MathHelper.Lerp(p.Key.Speed.start, p.Key.Speed.end, p.Key.Speed.curve.Evaluate(life));
                    x.scale = MathHelper.Lerp(p.Key.Scale.start, p.Key.Scale.end, p.Key.Scale.curve.Evaluate(life));
                    x.color = Color.Lerp(p.Key.Color.start, p.Key.Color.end, p.Key.Color.curve.Evaluate(life));

                    x.position += (x.direction * x.speed) * ts;

                    p.Value[i] = x;
                }
            }

            #endregion

            //update triggers
            var tempTriggers = triggers;
            triggers = nextTriggers;
            nextTriggers = tempTriggers;
            nextTriggers.Clear();
        }
    }
}
