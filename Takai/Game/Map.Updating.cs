using Microsoft.Xna.Framework;
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
        protected List<EntityInstance> entsToDestroy = new List<EntityInstance>(8);
        protected List<EntityInstance> entsToRemoveFromActive = new List<EntityInstance>(16);

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
                var fluid = ActiveFluids[i];
                var deltaV = fluid.velocity * deltaSeconds;
                fluid.position += deltaV;
                fluid.velocity -= deltaV * fluid.Class.Drag;

                //todo: maybe add collision detection for better fluid simulation (combine drag when colliding)

                if (System.Math.Abs(fluid.velocity.X) < 1 && System.Math.Abs(fluid.velocity.Y) < 1)
                {
                    Spawn(fluid.Class, fluid.position, Vector2.Zero); //this will move the Fluid to the static area of the map
                    ActiveFluids[i] = ActiveFluids[ActiveFluids.Count - 1];
                    ActiveFluids.RemoveAt(ActiveFluids.Count - 1);
                    --i;
                }
                else
                    ActiveFluids[i] = fluid;
            }

            #endregion

            #region entities

            //remove entities that have been destroyed
            foreach (var ent in entsToDestroy)
            {
                //if (ent.Map != this)
                //    continue;

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

            foreach (var ent in ActiveEnts)
            {
                var entBounds = ent.AxisAlignedBounds;
                if (!Bounds.Intersects(entBounds) || //outside of the map
                    ent.State.Instance == null) //no state
                        Destroy(ent);

                else if (!ent.Class.AlwaysActive && !visibleRegion.Intersects(entBounds))
                {
                    if (ent.State.Instance.Id == EntStateId.Dead)
                        Destroy(ent);
                    else
                        entsToRemoveFromActive.Add(ent);
                }
                else
                {
                    if (updateSettings.isAiEnabled)
                        ent.Think(deltaTime);

                    if (updateSettings.isPhysicsEnabled)
                    {
                        if (ent.Velocity != Vector2.Zero)
                        {
                            var deltaV = ent.Velocity * deltaSeconds;
                            var deltaVLen = deltaV.Length();

                            var direction = deltaV / deltaVLen;

                            //entity collision
                            var start = ent.Position + ((ent.Radius + 1) * direction);
                            var hit = Trace(start, direction, deltaVLen, ent);
                            var target = start + (direction * hit.distance);
                            DrawLine(start, target, Color.Yellow);
                            
                            if (hit.entity != null)
                            {
                                ent.OnEntityCollision(hit.entity, target, deltaTime);
                                hit.entity.OnEntityCollision(ent, target, deltaTime);

                                if (ent.Class.IsPhysical)
                                    ent.Velocity = Vector2.Zero;
                            }
                            else if (Math.Abs(hit.distance - deltaVLen) > 0.5f)
                            {
                                ent.OnMapCollision((target / TileSize).ToPoint(), target, deltaTime);

                                //improve
                                ent.Velocity = Vector2.Zero;// (hit.distance / deltaVLen) * ent.Velocity;
                            }

                            //Fluid collision
                            if (!ent.Class.IgnoreTrace)
                            {
                                var drag = 0f;
                                var dc = 0u;
                                var targetSector = GetOverlappingSector(target);
                                var sector = Sectors[targetSector.Y, targetSector.X];
                                foreach (var fluid in sector.fluids)
                                {
                                    if (Vector2.DistanceSquared(fluid.position, target) <= (fluid.Class.Radius * fluid.Class.Radius) + ent.RadiusSq)
                                    {
                                        drag += fluid.Class.Drag;
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
                            var sectors = GetOverlappingSectors(ent.AxisAlignedBounds);

                            //todo: optimize removal/additions to only remove if not currently inside
                            for (int y = sectors.Top; y < sectors.Bottom; ++y)
                            {
                                for (int x = sectors.Left; x < sectors.Right; ++x)
                                    Sectors[y, x].entities.Remove(ent);
                            }
                            ent.Position += ent.Velocity * deltaSeconds;

                            sectors = GetOverlappingSectors(ent.AxisAlignedBounds);
                            for (int y = sectors.Top; y < sectors.Bottom; ++y)
                            {
                                for (int x = sectors.Left; x < sectors.Right; ++x)
                                    Sectors[y, x].entities.Add(ent);
                            }
                        }
                    }
                }
            }

            ActiveEnts.ExceptWith(entsToRemoveFromActive);
            entsToRemoveFromActive.Clear();

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
