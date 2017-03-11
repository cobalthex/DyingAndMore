using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    /// <summary>
    /// A single sector of the map, used for spacial calculations
    /// Sectors typically contain inactive entities that are not visible as well as dummy objects (inactive blobs, decals).
    /// </summary>
    public class MapSector
    {
        public List<Entity> entities = new List<Entity>();
        public List<Blob> blobs = new List<Blob>();
        public List<Decal> decals = new List<Decal>();
    }

    /// <summary>
    /// An event handler for triggered events
    /// </summary>
    /// <param name="triggerType"></param>
    /// <param name="triggerValue"></param>
    public delegate void TriggeredEvent(string triggerType, int triggerValue);

    /// <summary>
    /// A 2D, tile based map system that handles logic and rendering of the map
    /// </summary>
    public partial class Map
    {
        /// <summary>
        /// The file that this map was loaded for
        /// </summary>
        /// <remarks>This must be set in order to create a save-state</remarks>
        [Data.NonSerialized]
        public string File { get; set; }

        /// <summary>
        /// The human-friendly map name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The collision mask for the tilemap
        /// </summary>
        [Data.NonSerialized]
        public System.Collections.BitArray TilesMask { get; set; }

        public int TileSize
        {
            get { return tileSize; }
            set
            {
                tileSize = value;
                TilesPerRow = (TilesImage != null ? (TilesImage.Width / value) : 0);
                SectorPixelSize = SectorSize * value;
            }
        }
        private int tileSize;
        [Data.NonSerialized]
        public int TilesPerRow { get; private set; }

        /// <summary>
        /// The horizontal size of the map in tiles
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// The vertical size of the map in tiles
        /// </summary>
        public int Height { get; set; }

        public const int SectorSize = 4; //The number of tiles in a map sector
        [Data.NonSerialized]
        public int SectorPixelSize { get; private set; } = 0;

        /// <summary>
        /// The bounds of the map in pixels
        /// </summary>
        [Data.NonSerialized]
        public Rectangle Bounds
        {
            get { return new Rectangle(0, 0, Width * TileSize, Height * TileSize); }
        }

        /// <summary>
        /// All of the tiles in the map
        /// </summary>
        /// <remarks>Size is Height,Width</remarks>
        [Data.NonSerialized]
        public short[,] Tiles { get; set; }
        /// <summary>
        /// All of the sectors in the map
        /// Typically inactive entities are stored here
        /// </summary>
        /// <remarks>Size is Height,Width</remarks>
        [Data.NonSerialized]
        public MapSector[,] Sectors { get; protected set; }
        protected Point NumSectors { get; private set; }

        /// <summary>
        /// The list of active particles. Not serialized
        /// </summary>
        [Data.NonSerialized]
        public Dictionary<ParticleType, List<Particle>> Particles { get; protected set; } = new Dictionary<ParticleType, List<Particle>>();

        /// <summary>
        /// The set of live entities that are being updated.
        /// Entities not in this list are not updated
        /// Automatically updated as the view changes
        /// </summary>
        [Data.NonSerialized]
        public List<Entity> ActiveEnts { get; protected set; } = new List<Entity>(128);
        /// <summary>
        /// The list of live blobs. Once the blobs' velocity is zero, they are removed from this and not re-added
        /// </summary>
        [Data.NonSerialized]
        public List<Blob> ActiveBlobs { get; protected set; } = new List<Blob>(128);

        [Data.NonSerialized]
        public int TotalEntitiesCount { get; private set; } = 0;

        /// <summary>
        /// Event handlers for specific triggered events
        /// Name -> Handlers
        /// </summary>
        protected Dictionary<string, TriggeredEvent> eventHandlers = new Dictionary<string, TriggeredEvent>();

        /// <summary>
        /// Active scripts running on this map
        /// </summary>
        protected Dictionary<string, Script> scripts = new Dictionary<string, Script>();

        /// <summary>
        /// An enumerable list of all entities, entities must be added through Spawn()
        /// </summary>
        [Data.NonSerialized]
        public IEnumerable<Entity> AllEntities
        {
            get
            {
                foreach (var ent in ActiveEnts)
                    yield return ent;

                foreach (var sector in Sectors)
                {
                    foreach (var ent in sector.entities)
                        yield return ent;
                }
            }
        }

        public Camera ActiveCamera { get; set; }

        public Map() { }

        /// <summary>
        /// Add an event handler
        /// </summary>
        /// <param name="name">The name of the event to add a handler to. Null or empty names are ignored</param>
        /// <param name="eventHandler">The event handler to add</param>
        /// <returns>True if the handler was added, false otherwise</returns>
        public bool AddEventHandler(string name, TriggeredEvent eventHandler)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (eventHandlers.ContainsKey(name))
                eventHandlers[name] += eventHandler;
            else
                eventHandlers[name] = eventHandler;

            return true;
        }

        /// <summary>
        /// Remove an event handler
        /// </summary>
        /// <param name="name">The name of the event to remove the handler from</param>
        /// <param name="eventHandler">The event handler to remove</param>
        /// <returns>True if the handler was removed, false otherwise</returns>
        public bool RemoveEventHandler(string name, TriggeredEvent eventHandler)
        {
            if (eventHandlers.ContainsKey(name))
            {
                eventHandlers[name] -= eventHandler;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Trigger an event
        /// </summary>
        /// <param name="name">The name of the trigger</param>
        /// <param name="value">The value of the trigger. Value interpreted by the handler</param>
        public void TriggerEvent(string name, int value)
        {
            if (eventHandlers.TryGetValue(name, out var handler))
                handler(name, value);
        }


        /// <summary>
        /// Add a script to the map, overwrites any existing scripts with the sane name
        /// </summary>
        /// <param name="script">The script to add</param>
        /// <returns>False if the script is null</returns>
        public bool AddScript(Script script)
        {
            if (script == null)
                return false;

            if (scripts.TryGetValue(script.Name, out var old))
                old.Map = null;

            script.Map = this;
            scripts[script.Name] = script;
            return true;
        }

        public bool RemoveScript(string Name)
        {
            return scripts.Remove(Name);
        }
        public bool RemoveScript(Script script)
        {
            return scripts.Remove(script?.Name);
        }

        /// <summary>
        /// Adds an existing entity to the map
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <param name="addToActive">Add this entity to the active set (defaults to true)</param>
        public void Spawn(Entity entity, bool addToActive = true)
        {
            if (entity.Map != null)
                Destroy(entity);

            entity.Map = this;
            entity.SpawnTime = ElapsedTime;

            if (addToActive)
                ActiveEnts.Add(entity);
            else
            {
                var sector = GetSector(entity.Position);
                Sectors[sector.Y, sector.X].entities.Add(entity);
            }
            ++TotalEntitiesCount;
        }

        /// <summary>
        /// Spawn an entity in the map
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to spawn</typeparam>
        /// <param name="position">Where to spawn the entity</param>
        /// <param name="direction">The direction the entity should face</param>
        /// <param name="velocity">The entity's initial velocity</param>
        /// <param name="LoadEntity">Should the entity be loaded, defaults to true</param>
        /// <returns>The entity spawned</returns>
        public TEntity Spawn<TEntity>(Vector2 position, Vector2 direction, Vector2 velocity) where TEntity : Entity, new()
        {
            var ent = new TEntity()
            {
                Map = this,
                SpawnTime = ElapsedTime,

                Position = position,
                Direction = direction,
                Velocity = velocity
            };

            //will be removed in next update if out of visible range
            //only inactive entities are placed in sectors
            ActiveEnts.Add(ent);
            ++TotalEntitiesCount;

            return ent;
        }

        /// <summary>
        /// Spawn an entity, cloning another
        /// </summary>
        /// <param name="template">The entity to clone</param>
        /// <param name="position">Where to spawn the entity</param>
        /// <param name="direction">The direction the entity should face</param>
        /// <param name="velocity">The entity's initial velocity</param>
        /// <param name="LoadEntity">Should the entity be loaded, defaults to true</param>
        /// <returns>The new entity spawned</returns>
        public TEntity Spawn<TEntity>(TEntity template, Vector2 position, Vector2 direction, Vector2 velocity) where TEntity : Entity, new()
        {
            var ent = (TEntity)template.Clone();

            ent.Map = this;
            ent.SpawnTime = ElapsedTime;
            ent.OnSpawn();

            ent.Position = position;
            ent.Direction = direction;
            ent.Velocity = velocity;

            //will be removed in next update if out of visible range
            //only inactive entities are placed in sectors
            ActiveEnts.Add(ent);
            TotalEntitiesCount++;

            return ent;
        }

        /// <summary>
        /// Spawn a single blob onto the map
        /// </summary>
        /// <param name="position">The position of the blob</param>
        /// <param name="velocity">The blob's initial velocity</param>
        /// <param name="type">The blob's type</param>
        public void Spawn(BlobType type, Vector2 position, Vector2 velocity)
        {
            //todo: don't spawn blobs outside the map (position + radius)

            if (velocity == Vector2.Zero)
            {
                var sector = GetSector(position);
                Sectors[sector.Y, sector.X].blobs.Add(new Blob { position = position, velocity = velocity, type = type });
            }
            else
                ActiveBlobs.Add(new Blob { position = position, velocity = velocity, type = type });
        }

        /// <summary>
        /// Spawn particles
        /// </summary>
        /// <param name="spawn">The rules for spawning</param>
        public void Spawn(ParticleSpawn spawn)
        {
            int count = random.Next(spawn.count.min, spawn.count.max);

            if (!Particles.ContainsKey(spawn.type))
                Particles.Add(spawn.type, new List<Particle>());

            for (int i = 0; i < count; i++)
            {
                var lifetime = RandTime(spawn.lifetime.min, spawn.lifetime.max);
                if (lifetime <= System.TimeSpan.Zero)
                    continue;

                var delay = RandTime(spawn.delay.min, spawn.delay.max);

                var theta = RandFloat(spawn.angle.min, spawn.angle.max);
                var dir = new Vector2
                (
                    (float)System.Math.Cos(theta),
                    (float)System.Math.Sin(theta)
                );

                Particles[spawn.type].Add(new Particle
                {
                    time     = ElapsedTime,
                    lifetime = lifetime,
                    delay    = delay,

                    position = RandVector2(spawn.position.min, spawn.position.max),
                    direction = dir,

                    //cached
                    speed = spawn.type.Speed.start,
                    scale = spawn.type.Scale.start,
                    color = spawn.type.Color.start
                });
            }
        }

        /// <summary>
        /// Add a decal to the map
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="position">The position of the decal on the map</param>
        /// <param name="angle">The angle the decal faces</param>
        /// <param name="scale">How much to scale the decal</param>
        public Decal AddDecal(Texture2D texture, Vector2 position, float angle = 0, float scale = 1)
        {
            if (texture == null)
                return null;

            var sector = GetSector(position);
            var decal = new Decal { texture = texture, position = position, angle = angle, scale = scale };
            Sectors[sector.Y, sector.X].decals.Add(decal);
            return decal;
        }

        public void AddDecal(Decal decal)
        {
            if (decal.texture == null)
                return;

            var sector = GetSector(decal.position);
            Sectors[sector.Y, sector.X].decals.Add(decal);
        }

        /// <summary>
        /// Remove an entity from the map.
        /// Entity.OnDestroy() is called immediately
        /// </summary>
        /// <param name="ent">The entity to remove</param>
        /// <remarks>Will be marked for removal to be removed during the next Update cycle</remarks>
        public void Destroy(Entity ent)
        {
            ent.Map = null;
        }
    }
}
