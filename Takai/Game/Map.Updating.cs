﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;

namespace Takai.Game
{
    public class MapUpdateSettings
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
        [Data.Serializer.Ignored]
        public MapUpdateSettings updateSettings = new MapUpdateSettings();

        /// <summary>
        /// How long since this map started (updated every Update()). Affected by <see cref="TimeScale"/>
        /// </summary>
        [Takai.Data.Serializer.Ignored] //serialized in state
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;
        /// <summary>
        /// How fast the game is moving (default = 1)
        /// </summary>
        [Takai.Data.Serializer.Ignored] //serialized in state
        public float TimeScale { get; set; } = 1;

        /// <summary>
        /// Ents that are to be destroyed during the next Update()
        /// </summary>
        protected List<Entity> entsToDestroy = new List<Entity>(8);
        protected List<Entity> entsToRemoveFromActive = new List<Entity>(16);

        /// <summary>
        /// Update the map state
        /// Updates the active set and then the contents of the active set
        /// </summary>
        /// <param name="RealTime">(Real) game time</param>
        /// <param name="Camera">Where on the map to view</param>
        /// <param name="Viewport">Where on screen to draw the map. The viewport is centered around the camera</param>
        public void Update(GameTime RealTime, Camera Camera = null)
        {
            if (Camera == null)
                Camera = ActiveCamera;

            Camera.Update(RealTime);

            var deltaTicks = (long)(RealTime.ElapsedGameTime.Ticks * (double)TimeScale);
            var deltaTime = TimeSpan.FromTicks(deltaTicks);
            var deltaSeconds = (float)deltaTime.TotalSeconds;
            ElapsedTime += deltaTime;

            var invTransform = Matrix.Invert(Camera.Transform);

            var visibleRegion = Camera.VisibleRegion;
            var visibleSectors = GetOverlappingSectors(visibleRegion);
            var mapBounds = Bounds;

            #region active Fluids

            for (int i = 0; i < ActiveFluids.Count; ++i)
            {
                var Fluid = ActiveFluids[i];
                var deltaV = Fluid.velocity * deltaSeconds;
                Fluid.position += deltaV;
                Fluid.velocity -= deltaV * Fluid.type.Drag;

                //todo: maybe add collision detection for better fluid simulation (combine drag when colliding)

                if (System.Math.Abs(Fluid.velocity.X) < 1 && System.Math.Abs(Fluid.velocity.Y) < 1)
                {
                    Spawn(Fluid.type, Fluid.position, Vector2.Zero); //this will move the Fluid to the static area of the map
                    ActiveFluids[i] = ActiveFluids[ActiveFluids.Count - 1];
                    ActiveFluids.RemoveAt(ActiveFluids.Count - 1);
                    --i;
                }
                else
                    ActiveFluids[i] = Fluid;
            }

            #endregion

            #region entities

            //remove entities that have been destroyed
            foreach (var ent in entsToDestroy)
            {
                if (ent.Map != this)
                    continue;

                ent.OnDestroy();
                ent.SpawnTime = TimeSpan.Zero;

                ActiveEnts.Remove(ent);
                var sectors = GetOverlappingSectors(ent.AxisAlignedBounds);
                for (int y = sectors.Top; y < sectors.Bottom; ++y)
                {
                    for (int x = sectors.Left; x < sectors.Right; ++x)
                        Sectors[y, x].entities.Remove(ent);
                }

                ent.Map = null;
                --TotalEntitiesCount;
            }
            entsToDestroy.Clear();

            ActiveEnts.ExceptWith(entsToRemoveFromActive);
            entsToRemoveFromActive.Clear();

            foreach (var ent in ActiveEnts)
            {
                var entBounds = ent.AxisAlignedBounds;

                //ents outside the map are deleted
                if (!Bounds.Contains(entBounds))
                    Destroy(ent);

                else if (!ent.AlwaysActive && !visibleRegion.Contains(entBounds))
                {
                    if (ent.DestroyIfDeadAndInactive && ent.State.Is(EntStateKey.Dead))
                        Destroy(ent);
                    else
                        entsToRemoveFromActive.Add(ent);
                }
                else
                {
                    if (!ent.IsAlive)
                        continue;

                    if (ent.State.BaseState == EntStateKey.Invalid)
                    {
                        Destroy(ent);
                        continue;
                    }

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

                            //Fluid collision
                            if (!ent.IgnoreTrace)
                            {
                                var drag = 0f;
                                var dc = 0u;
                                var sector = Sectors[targetCell.Y / SectorSize, targetCell.X / SectorSize];
                                foreach (var Fluid in sector.Fluids)
                                {
                                    if (Vector2.DistanceSquared(Fluid.position, targetPos) <= (Fluid.type.Radius * Fluid.type.Radius) + ent.RadiusSq)
                                    {
                                        drag += Fluid.type.Drag;
                                        ++dc;
                                    }
                                }
                                if (dc > 0)
                                {
                                    var fd = (drag / dc / 7.5f) * ent.Velocity.LengthSquared();
                                    ent.Velocity += ((-fd * direction) * (deltaSeconds / TimeScale)); //todo: radius affects
                                }
                            }
                        }

                        if (ent.Velocity != Vector2.Zero)
                        {
                            ent.Position += ent.Velocity * deltaSeconds;
                            //update entity sector data, for collision
                            var sectors = GetOverlappingSectors(ent.AxisAlignedBounds);
                            for (int y = sectors.Top; y < sectors.Bottom; ++y)
                            {
                                for (int x = sectors.Left; x < sectors.Right; ++x)
                                    Sectors[y, x].entities.Add(ent);
                            }
                        }
                    }
                }
            }

            //add new entities to active set (will be updated next frame)
            for (int y = visibleSectors.Top; y < visibleSectors.Bottom; ++y)
            {
                for (int x = visibleSectors.Left; x < visibleSectors.Right; ++x)
                    ActiveEnts.UnionWith(Sectors[y, x].entities);
            }

            #endregion

            #region particles

            foreach (var p in Particles)
            {
                for (var i = 0; i < p.Value.Count; ++i)
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
