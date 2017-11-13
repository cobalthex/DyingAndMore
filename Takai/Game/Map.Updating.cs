using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;

namespace Takai.Game
{
    public partial class MapInstance
    {
        public class UpdateSettings
        {
            public bool isAiEnabled;
            public bool isInputEnabled;
            public bool isPhysicsEnabled;
            public bool isCollisionEnabled;
            public bool isSoundEnabled;

            public static readonly UpdateSettings Game = new UpdateSettings
            {
                isAiEnabled = true,
                isInputEnabled = true,
                isPhysicsEnabled = true,
                isCollisionEnabled = true,
                isSoundEnabled = true,
            };

            public static readonly UpdateSettings Editor = new UpdateSettings
            {
                isAiEnabled = false,
                isInputEnabled = false,
                isPhysicsEnabled = false,
                isCollisionEnabled = true,
                isSoundEnabled = false,
            };
        }

        [Data.Serializer.Ignored]
        public UpdateSettings updateSettings = new UpdateSettings();

        /// <summary>
        /// How long since this map started (updated every Update()). Affected by <see cref="TimeScale"/>
        /// </summary>
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// How fast the game is moving (default = 1)
        /// </summary>
        public float TimeScale { get; set; } = 1;

        /// <summary>
        /// Ents to destroy during the next Update()
        /// </summary>
        protected List<EntityInstance> entsToDestroy = new List<EntityInstance>(8);
        protected List<EntityInstance> entsToRemoveFromActive = new List<EntityInstance>(32);

        /// <summary>
        /// Ents to add to the map during the next Update()
        /// </summary>
        protected HashSet<EntityInstance> activeEntities = new HashSet<EntityInstance>();

        /// <summary>
        /// Update the map state
        /// Updates the active set and then the contents of the active set
        /// </summary>
        /// <param name="realTime">(Real) game time</param>
        /// <param name="camera">Where on the map to view</param>
        /// <param name="Viewport">Where on screen to draw the map. The viewport is centered around the camera</param>
        public void Update(GameTime realTime, Camera camera = null)
        {
            if (camera == null)
                camera = ActiveCamera;

            camera.Update(realTime);

            var deltaTicks = (long)(realTime.ElapsedGameTime.Ticks * (double)TimeScale);
            var deltaTime = TimeSpan.FromTicks(deltaTicks);
            var deltaSeconds = (float)deltaTime.TotalSeconds;
            ElapsedTime += deltaTime;

            //if (deltaTicks == 0)
            //    return;

            var invTransform = Matrix.Invert(camera.Transform);

            var visibleRegion = camera.VisibleRegion;
            var visibleSectors = GetOverlappingSectors(visibleRegion);
            var mapBounds = Class.Bounds;

            #region moving/live Fluids

            for (int i = 0; i < LiveFluids.Count; ++i)
            {
                var fluid = LiveFluids[i];
                var deltaV = fluid.velocity * deltaSeconds;
                fluid.position += deltaV;
                fluid.velocity -= deltaV * fluid.Class.Drag;

                //todo: maybe add collision detection for better fluid simulation (combine drag when colliding)

                if (Math.Abs(fluid.velocity.X) < 1 && Math.Abs(fluid.velocity.Y) < 1)
                {
                    Spawn(fluid.Class, fluid.position, Vector2.Zero); //this will move the Fluid to the static area of the map
                    LiveFluids[i] = LiveFluids[LiveFluids.Count - 1];
                    LiveFluids.RemoveAt(LiveFluids.Count - 1);
                    --i;
                }
                else
                    LiveFluids[i] = fluid;
            }

            #endregion

            #region entities

            //remove entities that have been destroyed
            foreach (var ent in entsToDestroy)
            {
                FinalDestroy(ent);
                RemoveFromSectors(ent);
            }

            entsToDestroy.Clear();

            activeEntities.UnionWith(EnumerateEntitiesInSectors(visibleSectors));

            var exclusionZone = visibleRegion;
            exclusionZone.Inflate(Class.SectorPixelSize, Class.SectorPixelSize);

            foreach (var ent in activeEntities)
            {
                if (!ent.AxisAlignedBounds.Intersects(exclusionZone))
                {
                    entsToRemoveFromActive.Add(ent);
                    continue;
                }

                //todo: no state (move to entity?)
                if (!Class.Bounds.Intersects(ent.AxisAlignedBounds)) //outside of the map
                        Destroy(ent);

                else if (!visibleRegion.Intersects(ent.AxisAlignedBounds))
                {
                    //todo: reorganize

                    if (ent.Class.DestroyIfOffscreen ||
                        (ent.Class.DestroyIfDeadAndOffscreen && !ent.IsAlive))
                        Destroy(ent);
                }
                else
                {
                    if (updateSettings.isAiEnabled)
                        ent.Think(deltaTime);

                    if (updateSettings.isPhysicsEnabled && deltaTicks != 0)
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
                            //DrawLine(start, target, Color.Yellow);

                            if (hit.entity != null)
                            {
                                ent.OnEntityCollision(hit.entity, target, deltaTime);
                                hit.entity.OnEntityCollision(ent, target, deltaTime);

                                if (ent.Class.IsPhysical)
                                    ent.Velocity = Vector2.Zero;
                            }
                            else if (Math.Abs(hit.distance - deltaVLen) > 0.5f)
                            {
                                ent.OnMapCollision((target / Class.TileSize).ToPoint(), target, deltaTime);

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

                            ent.Velocity -= ent.Velocity * (ent.Class.Drag * deltaSeconds);
                        }
                    }
                }
            }

            activeEntities.ExceptWith(entsToRemoveFromActive);
            entsToRemoveFromActive.Clear();

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

                        if (p.Key.DestructionFluid != null)
                            Spawn(p.Key.DestructionFluid, x.position, x.velocity / 10);

                        continue;
                    }

                    var life = (float)((ElapsedTime - (x.time + x.delay)).TotalSeconds / x.lifetime.TotalSeconds);

                    x.color = Color.Lerp(
                        p.Key.ColorOverTime.start,
                        p.Key.ColorOverTime.end,
                        (p.Key.ColorOverTime.curve ?? ValueCurve<Color>.Linear).Evaluate(life)
                    );

                    x.scale = MathHelper.Lerp(
                        p.Key.ScaleOverTime.start,
                        p.Key.ScaleOverTime.end,
                        (p.Key.ScaleOverTime.curve ?? ValueCurve<float>.Linear).Evaluate(life)
                    );

                    //todo: calculating angle may be slow (relative)
                    /*
                    x.angle = x.velocity.Angle() + MathHelper.Lerp(
                        p.Key.AngleOverTime.start,
                        p.Key.AngleOverTime.end,
                        (p.Key.AngleOverTime.curve ?? ValueCurve<float>.Linear).Evaluate(life)
                    );*/

                    var deltaV = x.velocity * deltaSeconds;
                    x.velocity -= deltaV * p.Key.Drag;
                    x.position += deltaV;

                    p.Value[i] = x;
                }
            }

            #endregion

            #region sounds

            if (updateSettings.isSoundEnabled)
            {
                for (int i = 0; i < Sounds.Count; ++i)
                {
                    //handle sound positioning here (relative to camera)

                    if (Sounds[i].Instance.State == Microsoft.Xna.Framework.Audio.SoundState.Stopped)
                    {
                        Sounds[i].Instance.Dispose();
                        Sounds[i] = Sounds[Sounds.Count - 1];
                        Sounds.RemoveAt(Sounds.Count - 1);
                        --i;
                    }
                }
            }

            #endregion

            foreach (var script in Scripts)
                script.Step(deltaTime);
        }
    }
}
