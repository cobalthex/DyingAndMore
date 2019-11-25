using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace Takai.Game
{
    public struct CollisionManifold
    {
        public Vector2 point;
        public Vector2 direction;
        public float depth;

        public CollisionManifold Reciprocal()
        {
            return new CollisionManifold
            {
                point = point,
                direction = -direction,
                depth = depth
            };
        }
    }

    public partial class MapBaseInstance
    {
        public struct MapUpdateStats
        {
            public int updatedEntities;
            public TimeSpan lastFrameDuration;
        }

        [Data.Serializer.Ignored]
        public MapUpdateStats UpdateStats => _updateStats;
        protected MapUpdateStats _updateStats;
        internal System.Diagnostics.Stopwatch updateClock = new System.Diagnostics.Stopwatch();

        public class UpdateSettings
        {
            public bool isEntityLogicEnabled;
            public bool isPhysicsEnabled;
            public bool isMapCollisionEnabled;
            public bool isEntityCollisionEnabled;
            public bool isSoundEnabled;
            public bool allowEntityDeletion; //only applies for deletions in Update() (Destroy() can still be called)

            public void SetGame()
            {
                isEntityLogicEnabled = true;
                isPhysicsEnabled = true;
                isMapCollisionEnabled = true;
                isEntityCollisionEnabled = true;
                isSoundEnabled = true;
                allowEntityDeletion = true;
            }

            public void SetEditor()
            {
                isEntityLogicEnabled = false;
                isPhysicsEnabled = false;
                isMapCollisionEnabled = true;
                isEntityCollisionEnabled = true;
                isSoundEnabled = false;
                allowEntityDeletion = false;
            }

            public UpdateSettings Clone()
            {
                return (UpdateSettings)MemberwiseClone();
            }
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
        /// Real time passed in to update. Primarily used for slowing down update cycles
        /// </summary>
        [Data.Serializer.Ignored]
        public GameTime RealTime { get; protected set; }

        /// <summary>
        /// Entities to destroy during the next Update()
        /// </summary>
        protected List<EntityInstance> entsToDestroy = new List<EntityInstance>(8);

        /// <summary>
        /// Ents to update during the next Update()
        /// </summary>
        protected HashSet<EntityInstance> activeEntities = new HashSet<EntityInstance>();
        protected HashSet<EntityInstance> possibleOffscreenEntities = new HashSet<EntityInstance>();

        HashSet<FluidInstance> collidingFluids = new HashSet<FluidInstance>();

        protected List<TrailInstance> trailsToDestroy = new List<TrailInstance>();

        /// <summary>
        /// Entities were/are active (visible and updated)
        /// </summary>
        public IEnumerable<EntityInstance> ActiveEntities => activeEntities;

        //public event EventHandler<List<EntityInstance>> EntitiesRemoved;
        //public event EventHandler<List<EntityInstance>> EntitiesAdded;
        /// <summary>
        /// Entities added within the last update cycle 
        /// </summary>
        //protected List<EntityInstance> entsAdded = new List<EntityInstance>(8);

        public void BeginUpdate()
        {
            activeEntities.Clear();
        }

        public void MarkRegionActive(Camera camera)
        {
            var visibleRegion = camera.VisibleRegion;
            var _activeRegion = new Rectangle(
                visibleRegion.X - Class.SectorPixelSize,
                visibleRegion.Y - Class.SectorPixelSize,
                visibleRegion.Width + Class.SectorPixelSize * 2,
                visibleRegion.Height + Class.SectorPixelSize * 2
            );
            var activeSectors = GetOverlappingSectors(_activeRegion);

            for (int y = activeSectors.Top; y < activeSectors.Bottom; ++y)
                for (int x = activeSectors.Left; x < activeSectors.Right; ++x)
                    activeEntities.UnionWith(Sectors[y, x].entities);
        }

        protected virtual void UpdateEntities(TimeSpan deltaTime)
        {
            var deltaSeconds = (float)deltaTime.TotalSeconds;

            //at end of function?
            //if (entsAdded.Count > 0)
            //{
            //    EntitiesAdded?.Invoke(this, entsAdded);
            //    entsAdded.Clear();
            //}

            if (updateSettings.allowEntityDeletion)
            {
                foreach (var entity in possibleOffscreenEntities)
                {
                    if (!activeEntities.Contains(entity) &&
                        (!entity.Class.DestroyIfDeadAndOffscreen || !entity.IsAlive))
                        Destroy(entity);
                }
            }
            possibleOffscreenEntities.Clear();

            if (entsToDestroy.Count > 0)
            {
                //remove entities that have been destroyed
                foreach (var entity in entsToDestroy)
                {
                    if (entity.Map != this || entity.Map == null)
                        continue; //delete may be being called twice?

                    FinalDestroy(entity);
                }
                //EntitiesRemoved?.Invoke(this, entsToDestroy);
                entsToDestroy.Clear();
            }

            #region entity physics

            bool isPhysicsEnabled = updateSettings.isPhysicsEnabled;
            bool isMapCollisionEnabled = updateSettings.isMapCollisionEnabled;
            bool isEntityCollisionEnabled = updateSettings.isEntityCollisionEnabled;

            foreach (var entity in activeEntities)
            {
                var deltaV = entity.Velocity * deltaSeconds;
                var lastBounds = entity.AxisAlignedBounds;

                if (isPhysicsEnabled && deltaV != Vector2.Zero)
                {
                    var deltaVLen = deltaV.Length();
                    var normV = deltaV / deltaVLen;

                    var offset = entity.Radius + 1;
                    var start = entity.RealPosition + (offset * normV);
                    var hit = Trace(start, normV, deltaVLen, entity);
                    var target = start + (normV * (hit.distance - offset));

                    //todo: ignore collisions if child?

                    if (hit.didHit)
                    {
                        if (entity.Trail != null)
                            entity.Trail.AddPoint(target, normV);

                        if (hit.entity == null) //map collision
                        {
                            if (isMapCollisionEnabled) //cleanup
                            {
                                entity.SetPositionTransformed(target);

                                var interaction = Class.MaterialInteractions.Find(entity.Material, Class.TilesMaterial);

                                var tangent = GetTilesCollisionTangent(target, normV);
                                //tangent = Vector2.UnitX;
                                var colNorm = -tangent.Ortho();
                                if (Math.Acos(Math.Abs(Vector2.Dot(tangent, normV))) <= interaction.MaxBounceAngle)
                                {
                                    //add remaining distance to relfection? (trace that)
                                    entity.Velocity = Vector2.Reflect(entity.Velocity, colNorm) * (1 - interaction.Friction.Random());
                                    entity.Forward = Vector2.Reflect(entity.Forward, colNorm);
                                }
                                else
                                    //todo: improve
                                    entity.Velocity = Vector2.Zero;// (hit.distance / deltaVLen) * entity.Velocity;

                                if (interaction.Effect != null)
                                {
                                    var fx = interaction.Effect.Instantiate();
                                    fx.Source = entity;
                                    fx.Position = target;
                                    fx.Direction = colNorm; // Vector2.Reflect(entity.Forward, colNorm);
                                    Spawn(fx);
                                }

                                //entity.OnMapCollision((target / Class.TileSize).ToPoint(), target, deltaTime);
                            }
                        }
                        else if (isEntityCollisionEnabled)
                        {
                            entity.SetPositionTransformed(target);

                            var cm = new CollisionManifold
                            {
                                point = target,
                                direction = entity.Forward,
                                depth = deltaVLen - hit.distance //todo: distance between origins minus radii
                            };

                            //interaction should happen at reflect angle

                            var interaction = Class.MaterialInteractions.Find(entity.Material, hit.entity.Material);

                            if (entity.Class.IsPhysical)
                            {
                                var diff = Vector2.Normalize(hit.entity.Position - entity.Position);
                                entity.Velocity -= diff * Vector2.Dot(entity.Velocity, diff);
                            }

                            if (interaction.Effect != null)
                            {
                                var fx = interaction.Effect.Instantiate();
                                fx.Source = entity;
                                fx.Target = hit.entity;
                                fx.Position = target;
                                fx.Direction = -Vector2.Reflect(entity.Forward, Vector2.Normalize(target - hit.entity.Position));
                                Spawn(fx);
                            }

                            entity.OnEntityCollision(hit.entity, cm, deltaTime);
                            hit.entity.OnEntityCollision(entity, cm.Reciprocal(), deltaTime);
                        }
                    }

                    //Fluid collision
                    if (!entity.Class.IgnoreTrace)
                    {
                        var drag = 0f;
                        var dc = 0u;
                        var targetSector = GetOverlappingSector(target);
                        var sector = Sectors[targetSector.Y, targetSector.X];
                        foreach (var fluid in sector.fluids)
                        {
                            if (Vector2.DistanceSquared(fluid.position, target) <= (fluid.Class.Radius * fluid.Class.Radius) + entity.RadiusSq)
                            {
                                drag += fluid.Class.Drag;
                                ++dc;

                                if (fluid.Class.EntityCollisionEffect != null)
                                    collidingFluids.Add(fluid);
                            }
                        }
                        if (dc > 0)
                        {
                            var fd = (drag / dc / 7.5f) * entity.Velocity.LengthSquared();
                            entity.Velocity += ((-fd * normV) * (deltaSeconds / TimeScale)); //todo: radius affects
                        }

                        foreach (var fluid in collidingFluids)
                        {
                            var fx = fluid.Class.EntityCollisionEffect.Instantiate();
                            fx.Position = entity.Position;
                            fx.Direction = Vector2.Normalize(entity.Position - fluid.position);
                            Spawn(fx);
                        }
                        collidingFluids.Clear();
                    }

                    entity.Position += entity.Velocity * deltaSeconds;
                    entity.Velocity -= entity.Velocity * entity.Class.Drag * deltaSeconds;
                    entity.UpdateAxisAlignedBounds();
                }

                //place this entity into the pathing grid as an obstacle
                if (entity.Class.IsPhysical && !entity.Class.IgnoreTrace)
                {
                    var entBounds = entity.AxisAlignedBounds;
                    entBounds = Rectangle.Union(entBounds, lastBounds);
                    AddObstacle(entBounds, 4, entity.Radius);
                }
            }

            #endregion

            //todo: should movement be in physics phase?

            foreach (var entity in activeEntities)
            {
                UpdateEntitySectors(entity);

                if (entity.Class.DestroyIfDeadAndOffscreen || entity.Class.DestroyIfOffscreen)
                    possibleOffscreenEntities.Add(entity);

                if (entity.Trail != null)
                    Trails.Add(entity.Trail);

                entity.UpdateAnimations(deltaTime);

                if (updateSettings.isEntityLogicEnabled)
                    entity.Think(deltaTime);
            }
        }

        public void MoveEnt(EntityInstance entity, Vector2 position, Vector2 forward)
        {
            entity.Position = position;
            entity.Forward = Vector2.Normalize(forward);

            UpdateEntitySectors(entity);

        }

        void UpdateEntitySectors(EntityInstance entity)
        {
            var lastSectors = GetOverlappingSectors(entity.lastAABB);
            var nextSectors = GetOverlappingSectors(entity.AxisAlignedBounds);

            var eaabb = entity.AxisAlignedBounds;

            //if (lastSectors != nextSectors)
            {
                var diffSectors = Rectangle.Union(lastSectors, nextSectors);
                for (var y = diffSectors.Top; y < diffSectors.Bottom; ++y)
                {
                    for (var x = diffSectors.Left; x < diffSectors.Right; ++x)
                    {
                        if (new Rectangle(
                            x * Class.SectorPixelSize,
                            y * Class.SectorPixelSize,
                            Class.SectorPixelSize,
                            Class.SectorPixelSize
                        ).Intersects(eaabb))
                        {
                            Sectors[y, x].entities.Add(entity);
                        }
                        else
                        {
                            Sectors[y, x].entities.Remove(entity);
                        }

                        foreach (var trigger in Sectors[y, x].triggers)
                        {
                            if (trigger.Region.Intersects(eaabb))
                                trigger.TryEnter(entity);
                            else
                                trigger.TryExit(entity);
                        }
                    }
                }
            }
            entity.lastAABB = eaabb;

            if (entity.WorldChildren != null)
            {
                foreach (var child in entity.WorldChildren)
                    UpdateEntitySectors(child);
            }
        }

        /// <summary>
        /// Update the map state
        /// Updates the active set and then the contents of the active set
        /// Does not update ElapsedTime. Call <see cref="Tick"/>
        /// </summary>
        /// <param name="realTime">(Real) game time</param>
        /// <param name="camera">Where on the map to view</param>
        /// <param name="Viewport">Where on screen to draw the map. The viewport is centered around the camera</param>
        public virtual void Update(GameTime realTime)
        {
            updateClock.Restart();
            RealTime = realTime;
            _updateStats = new MapUpdateStats();

            var deltaTicks = (long)(realTime.ElapsedGameTime.Ticks * (double)TimeScale);
            var deltaTime = TimeSpan.FromTicks(deltaTicks);
            var deltaSeconds = (float)deltaTime.TotalSeconds;
            ElapsedTime += deltaTime;

            //if (deltaTicks == 0)
            //    return;

            #region moving/live fluids

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

            UpdateEntities(deltaTime);
            _updateStats.updatedEntities = activeEntities.Count;

            #endregion

            #region particles

            foreach (var p in Particles)
            {
                for (var i = 0; i < p.Value.Count; ++i)
                {
                    var x = p.Value[i];

                    bool isDead = ElapsedTime >= x.spawnTime + x.lifeTime;

                    if (!Class.IsInsideMap(x.position))
                        isDead = true;

                    else if (p.Key.CollisionEffect != null)
                    {
                        var ent = FindClosestEntity(x.position, p.Key.Radius * x.scale);
                        if (ent != null)
                        {
                            isDead = true;
                            var fx = p.Key.CollisionEffect.Instantiate();
                            fx.Position = x.position;
                            fx.Direction = Util.Direction(x.angle);
                            fx.Target = ent;
                            Spawn(fx);
                        }
                    }

                    if (isDead)
                    {
                        p.Value[i] = p.Value[p.Value.Count - 1];
                        p.Value.RemoveAt(p.Value.Count - 1);
                        --i;

                        if (p.Key.DestructionFluid != null)
                            Spawn(p.Key.DestructionFluid, x.position, x.velocity / 10);

                        continue;
                    }

                    var life = (float)((ElapsedTime - (x.spawnTime)).TotalSeconds / x.lifeTime.TotalSeconds);

                    x.color = p.Key.ColorOverTime.Evaluate(life);
                    x.scale = p.Key.ScaleOverTime.Evaluate(life);
                    x.spin = p.Key.SpinOverTime.Evaluate(life);
                    var angle = x.angle + p.Key.AngleOverTime.Evaluate(life);

                    var xvl = x.velocity.Length();
                    x.velocity = xvl * Vector2.TransformNormal(x.velocity / xvl, Matrix.CreateRotationZ(angle - x.angle));
                    x.angle = angle;

                    var deltaV = x.velocity * deltaSeconds;
                    x.velocity -= deltaV * p.Key.Drag;
                    x.position += deltaV;

                    p.Value[i] = x;
                }
            }

            #endregion

            #region trails

            Trails.ExceptWith(trailsToDestroy);
            trailsToDestroy.Clear();

            renderedTrailPointCount = 0;
            foreach (var trail in Trails)
            {
                trail.Update(deltaTime);
                if (trail.Count < 1)
                    trailsToDestroy.Add(trail);
                else
                    renderedTrailPointCount += trail.Count;
            }

            #endregion

            #region sounds

            if (updateSettings.isSoundEnabled)
            {
                for (int i = 0; i < Sounds.Count; ++i)
                {
                    var s = Sounds[i];
                    s.Instance.Resume();
                    //handle sound positioning here (relative to camera)

                    if (s.Instance.State == Microsoft.Xna.Framework.Audio.SoundState.Stopped)
                    {
                        s.Instance.Dispose();
                        s = Sounds[Sounds.Count - 1];
                        Sounds.RemoveAt(Sounds.Count - 1);
                        --i;
                        continue;
                    }

                    if (s.Owner != null)
                    {
                        s.Position = s.Owner.Position;
                        s.Forward = s.Owner.Forward;
                        s.Velocity = s.Owner.Velocity;

                        if (!s.Owner.IsAliveIn(this) && s.Class.DestroyIfOwnerDies)
                        {
                            s.Instance.Stop();
                            continue;
                        }
                    }

                    //var cameraPos = camera.ActualPosition;

                    //s.Instance.Volume = MathHelper.Clamp(1 / (Vector2.DistanceSquared(s.Position, cameraPos) / 5000), 0, 1);

                    //var pan = Vector2.Dot(Vector2.Normalize(s.Position - cameraPos), Util.Direction(camera.Rotation)) * s.Instance.Volume;
                    //s.Instance.Pan = (float)Math.Pow(pan, 2); //add some bias towards 1

                    //var sos = 200;
                    //var pitch = 1 - ((sos + camera.Follow.Velocity.LengthSquared()) / (sos + s.Velocity.LengthSquared()));
                    ////s.Instance.Pitch = MathHelper.Clamp(pitch, -1, 1);

                    Sounds[i] = s;
                }
            }
            else
            {
                for (int i = 0; i < Sounds.Count; ++i)
                    Sounds[i].Instance.Pause();
            }

            #endregion

            currentScreenFadeElapsedTime += deltaTime;
            _updateStats.lastFrameDuration = updateClock.Elapsed;
        }

        //todo: move ↓ to map.cs

        public void Attach(EntityInstance parent, EntityInstance child, Vector2 relativePosition)
        {
            //limit depth?

            if (parent == null || child == null)
                throw new ArgumentNullException("Parent nor child can be null");
            if (child == parent)
                throw new ArgumentException("Parent cannot be child");

            if (child.WorldParent != null)
                Detach(child);

            child.WorldParent = parent;
            if (parent._worldChildren == null)
                parent._worldChildren = new List<EntityInstance>(1);
            parent._worldChildren.Add(child);

            child.Velocity = Vector2.Zero;
            child.Position = relativePosition;

            child.UpdateAxisAlignedBounds();
            UpdateEntitySectors(child);
            parent.UpdateAxisAlignedBounds();
            UpdateEntitySectors(parent);
        }

        public void Detach(EntityInstance child)
        {
            if (child == null)
                throw new ArgumentNullException("child cannot be null");

            if (child.WorldParent == null)
                return;

            var parent = child.WorldParent;
            parent._worldChildren?.Remove(child);
            child.WorldParent = null;
            child.Position = Vector2.Transform(child.Position, child.Transform); //todo: verify

            UpdateEntitySectors(child);
            UpdateEntitySectors(parent);
        }
    }
}
