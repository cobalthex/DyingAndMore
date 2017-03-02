﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;

namespace Takai.Game
{
    public struct MapUpdateSettings
    {
        public bool isAiEnabled;
        public bool isInputEnabled;
        public bool isPhysicsEnabled;
        public bool isCollisionEnabled;

        public static readonly MapUpdateSettings Game = new MapUpdateSettings
        {
            isAiEnabled = true,
            isInputEnabled = true,
            isPhysicsEnabled = true,
            isCollisionEnabled = true,
        };

        public static readonly MapUpdateSettings Editor = new MapUpdateSettings
        {
            isAiEnabled = false,
            isInputEnabled = false,
            isPhysicsEnabled = false,
            isCollisionEnabled = true
        };
    }

    public partial class Map
    {
        [Data.NonSerialized]
        public MapUpdateSettings updateSettings = new MapUpdateSettings();

        /// <summary>
        /// How long since this map started (updated every Update()). Affected by <see cref="TimeScale"/>
        /// </summary>
        [Takai.Data.NonSerialized] //serialized in state
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;
        /// <summary>
        /// How fast the game is moving (default = 1)
        /// </summary>
        [Takai.Data.NonSerialized] //serialized in state
        public float TimeScale { get; set; } = 1;

        //todo: switch update method to add tiem to total game time

        /// <summary>
        /// Update the map state
        /// Updates the active set and then the contents of the active set
        /// </summary>
        /// <param name="RealTime">(Real) game time</param>
        /// <param name="Camera">Where on the map to view</param>
        /// <param name="Viewport">Where on screen to draw the map. The viewport is centered around the camera</param>
        public void Update(GameTime RealTime, Camera Camera)
        {
            if (TimeScale == 0)
                return; //may need to revisit

            var deltaTicks = (long)(RealTime.ElapsedGameTime.Ticks * (double)TimeScale);
            var deltaTime = TimeSpan.FromTicks(deltaTicks);
            var deltaSeconds = (float)deltaTime.TotalSeconds;
            ElapsedTime += deltaTime;

            var invTransform = Matrix.Invert(Camera.Transform);

            var visibleRegion = Camera.VisibleRegion;
            var visibleSectors = GetVisibleSectors(visibleRegion);
            visibleSectors.Inflate(1, 1);
            var mapBounds = Bounds;

            var tileSq = new Vector2(tileSize).LengthSquared();

            #region active blobs

            for (int i = 0; i < ActiveBlobs.Count; i++)
            {
                var blob = ActiveBlobs[i];
                var deltaV = blob.velocity * deltaSeconds;
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

                if (!ent.AlwaysActive && !visibleSectors.Contains(ent.Position / SectorPixelSize))
                {
                    //ents outside the map are deleted
                    if (mapBounds.Contains((ent.Position / tileSize).ToPoint()))
                        Sectors[(int)ent.Position.Y / SectorPixelSize, (int)ent.Position.X / SectorPixelSize].entities.Add(ent);
                    else
                    {
                        Destroy(ent);
                        continue;
                    }

                    if (ent.DestroyIfDeadAndInactive && ent.State.Is(EntStateKey.Dead))
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
                    if (!ent.IsAlive)
                        continue;

                    if (ent.State.BaseState == EntStateKey.Invalid)
                        Destroy(ent);

                    if (updateSettings.isAiEnabled)
                        ent.Think(deltaTime);

                    if (updateSettings.isPhysicsEnabled)
                    {
                        if (ent.Velocity != Vector2.Zero)
                        {
                            var deltaV = ent.Velocity * deltaSeconds;
                            var deltaVLen = deltaV.Length();

                            var direction = deltaV / deltaVLen;
                            var startPos = ent.Position + (ent.Radius * direction);
                            var targetPos = startPos + deltaV;
                            var targetCell = (targetPos / tileSize).ToPoint();

                            //entity collision
                            bool didCollide = TraceLine(startPos, direction, out var hit, deltaVLen);

                            if (didCollide)
                            {
                                if (hit.entity != null)
                                {
                                    ent.OnEntityCollision(hit.entity, startPos + (direction * hit.distance), deltaTime);
                                    hit.entity.OnEntityCollision(ent, startPos + (direction * hit.distance), deltaTime);

                                    if (ent.IsPhysical)
                                        ent.Velocity = Vector2.Zero;
                                }
                                else
                                {
                                    ent.OnMapCollision(targetCell, startPos + (direction * hit.distance), deltaTime); //todo: update w/ correct tile

                                    if (ent.IsPhysical)
                                        ent.Velocity = Vector2.Zero;
                                }
                            }

                            //todo: GetOverlappingSectors

                            //blob collision
                            if (!ent.IgnoreTrace)
                            {
                                var drag = 0f;
                                var dc = 0u;
                                var sector = Sectors[targetCell.Y / SectorSize, targetCell.X / SectorSize];
                                foreach (var blob in sector.blobs)
                                {
                                    if (Vector2.DistanceSquared(blob.position, targetPos) <= (blob.type.Radius * blob.type.Radius) + ent.RadiusSq)
                                    {
                                        drag += blob.type.Drag;
                                        dc++;
                                    }
                                }
                                if (dc > 0)
                                {
                                    var fd = (drag / dc / 7.5f) * ent.Velocity.LengthSquared();
                                    ent.Velocity += ((-fd * direction) * (deltaSeconds / TimeScale)); //todo: radius affects
                                }
                            }
                        }

                        ent.Position += ent.Velocity * deltaSeconds;
                    }
                }

                //remove entity from map
                if (ent.Map == null)
                {
                    ent.Map = this;
                    ent.OnDestroy();
                    ent.SpawnTime = TimeSpan.Zero;

                    if (ent.Sector != null)
                    {
                        ent.Sector.entities.Remove(ent);
                        ent.Sector = null;
                    }
                    else
                        ActiveEnts.Remove(ent);
                    ent.Map = null;
                    TotalEntitiesCount--;
                }
            }

            //add new entities to active set (will be updated next frame)
            for (var y = System.Math.Max(visibleSectors.Top, 0); y < System.Math.Min(Height / SectorSize, visibleSectors.Bottom); y++)
            {
                for (var x = System.Math.Max(visibleSectors.Left, 0); x < System.Math.Min(Width / SectorSize, visibleSectors.Right); x++)
                {
                    ActiveEnts.AddRange(Sectors[y, x].entities);
                    Sectors[y, x].entities.Clear();
                }
            }

            #endregion

            #region particles

            foreach (var p in Particles)
            {
                for (var i = 0; i < p.Value.Count; i++)
                {
                    var x = p.Value[i];

                    if (ElapsedTime > x.time + x.lifetime + x.delay)
                    {
                        p.Value[i] = p.Value[p.Value.Count - 1];
                        p.Value.RemoveAt(p.Value.Count - 1);
                        i--;
                        continue;
                    }

                    var life = (float)((ElapsedTime - (x.time + x.delay)).TotalSeconds / x.lifetime.TotalSeconds);

                    x.speed = MathHelper.Lerp(p.Key.Speed.start, p.Key.Speed.end, p.Key.Speed.curve.Evaluate(life));
                    x.scale = MathHelper.Lerp(p.Key.Scale.start, p.Key.Scale.end, p.Key.Scale.curve.Evaluate(life));
                    x.color = Color.Lerp(p.Key.Color.start, p.Key.Color.end, p.Key.Color.curve.Evaluate(life));

                    x.position += (x.direction * x.speed) * deltaSeconds;

                    p.Value[i] = x;
                }
            }

            #endregion

            foreach (var script in scripts)
                script.Value.Step(deltaTime);
        }
    }
}
