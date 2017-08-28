using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    /// <summary>
    /// A single sector of the map, used for spacial calculations
    /// Sectors typically contain inactive entities that are not visible as well as dummy objects (inactive Fluids, decals).
    /// </summary>
    public class MapSector
    {
        //dynamic
        public HashSet<EntityInstance> entities = new HashSet<EntityInstance>();

        //static
        public List<FluidInstance> fluids = new List<FluidInstance>();
        public List<Decal> decals = new List<Decal>();
        public List<Trigger> triggers = new List<Trigger>(); //triggers may be in one or more sectors
    }

    /// <summary>
    /// An event handler for game events
    /// </summary>
    /// <param name="eventName">The name of the event that triggered this handler</param>
    /// <param name="eventValue">And optional event value</param>
    public delegate void GameEvent(string eventName, int eventValue = 0);

    /// <summary>
    /// A 2D, tile based map system that handles logic and rendering of the map
    /// </summary>
    public partial class Map
    {
        /// <summary>
        /// The file that this map was loaded for
        /// </summary>
        /// <remarks>This must be set in order to create a save-state</remarks>
        [Data.Serializer.Ignored]
        public string File { get; set; }

        /// <summary>
        /// The human-friendly map name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The collision mask for the tilemap
        /// </summary>
        [Data.Serializer.Ignored]
        public System.Collections.BitArray CollisionMask { get; set; }

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
        [Data.Serializer.Ignored]
        public int TilesPerRow { get; private set; }

        /// <summary>
        /// The horizontal size of the map in tiles
        /// </summary>
        [Data.Serializer.Ignored]
        public int Width => Tiles.GetLength(1);
        /// <summary>
        /// The vertical size of the map in tiles
        /// </summary>
        [Data.Serializer.Ignored]
        public int Height => Tiles.GetLength(0);

        public const int SectorSize = 4; //The number of tiles in a map sector
        [Data.Serializer.Ignored]
        public int SectorPixelSize { get; private set; } = 0;

        /// <summary>
        /// The bounds of the map in pixels
        /// </summary>
        [Data.Serializer.Ignored]
        public Rectangle Bounds
        {
            get { return new Rectangle(0, 0, Width * TileSize, Height * TileSize); }
        }

        /// <summary>
        /// All of the tiles in the map
        /// </summary>
        /// <remarks>Size is Height,Width</remarks>
        [Data.Serializer.Ignored]
        public short[,] Tiles { get; set; }
        /// <summary>
        /// All of the sectors in the map
        /// </summary>
        /// <remarks>Size is Height,Width</remarks>
        [Data.Serializer.Ignored]
        public MapSector[,] Sectors { get; protected set; }

        /// <summary>
        /// The list of active particles. Not serialized
        /// </summary>
        [Data.Serializer.Ignored]
        public Dictionary<ParticleType, List<Particle>> Particles { get; protected set; } = new Dictionary<ParticleType, List<Particle>>();

        /// <summary>
        /// The set of live entities that are being updated.
        /// Entities not in this list are not updated
        /// Automatically updated as the view changes
        /// </summary>
        [Data.Serializer.Ignored]
        public HashSet<EntityInstance> ActiveEnts { get; protected set; } = new HashSet<EntityInstance>();
        /// <summary>
        /// The list of live Fluids. Once the Fluids' velocity is zero, they are removed from this and not re-added
        /// </summary>
        [Data.Serializer.Ignored]
        public List<FluidInstance> ActiveFluids { get; protected set; } = new List<FluidInstance>(128);

        [Data.Serializer.Ignored]
        public int TotalEntitiesCount { get; private set; } = 0;

        /// <summary>
        /// Event handlers for specific game events
        /// Event name -> Handlers
        /// </summary>
        protected Dictionary<string, GameEvent> eventHandlers = new Dictionary<string, GameEvent>();

        /// <summary>
        /// Active scripts running on this map
        /// </summary>
        protected Dictionary<string, Script> scripts = new Dictionary<string, Script>();

        /// <summary>
        /// An enumerable list of all entities, entities must be added through Spawn()
        /// </summary>
        [Data.Serializer.Ignored]
        public IEnumerable<EntityInstance> AllEntities
        {
            get
            {
                for (int y = 0; y < Sectors.GetLength(0); ++y)
                {
                    for (int x = 0; x < Sectors.GetLength(1); ++x)
                    {
                        var sx = x * SectorPixelSize;
                        var sy = y * SectorPixelSize;

                        foreach (var ent in Sectors[y, x].entities)
                        {
                            if (ent.AxisAlignedBounds.X >= sx &&
                                ent.AxisAlignedBounds.Y >= sy)
                                yield return ent;
                        }
                    }
                }
            }
        }

        public Camera ActiveCamera { get; set; }

        public Map() { }

        /// <summary>
        /// Resize the map
        /// Any entities/blobs/decals/etc that are in a area of the map that is removed may be deleted
        /// </summary>
        /// <param name="newWidth">the new width of the map</param>
        /// <param name="newHeight">the new height of the map</param>
        public void Resize(int newWidth, int newHeight)
        {
            System.Diagnostics.Contracts.Contract.Requires(newWidth > 0);
            System.Diagnostics.Contracts.Contract.Requires(newHeight > 0);

            //todo: make width/height/tiles private

            Tiles = Tiles.Resize(newHeight, newWidth);
            Sectors = Sectors.Resize((newHeight - 1) / SectorSize + 1, (newWidth - 1) / SectorSize + 1);

            for (int y = 0; y < Sectors.GetLength(0); ++y)
            {
                for (int x = 0; x < Sectors.GetLength(1); ++x)
                {
                    if (Sectors[y, x] == null)
                        Sectors[y, x] = new MapSector();
                }
            }
        }

        /// <summary>
        /// Add an event handler
        /// </summary>
        /// <param name="name">The name of the event to add a handler to. Null or empty names are ignored</param>
        /// <param name="eventHandler">The event handler to add</param>
        /// <returns>True if the handler was added, false otherwise</returns>
        public bool AddEventHandler(string name, GameEvent eventHandler)
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
        public bool RemoveEventHandler(string name, GameEvent eventHandler)
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
        /// Adds an existing instance to the map
        /// </summary>
        /// <param name="instance">The instance to add</param>
        public void Spawn(EntityInstance instance)
        {
            if (instance.Map != this && instance.Map != null)
                instance.Map.Destroy(instance);

            instance.RefreshBounds();
            instance.Map = this;
            instance.SpawnTime = ElapsedTime;

            var sectors = GetOverlappingSectors(instance.AxisAlignedBounds);
            for (int y = sectors.Top; y < sectors.Bottom; ++y)
            {
                for (int x = sectors.Left; x < sectors.Right; ++x)
                    Sectors[y, x].entities.Add(instance);
            }

            ++TotalEntitiesCount;
        }

        public EntityInstance Spawn(EntityClass @class, Vector2 position, Vector2 direction, Vector2 velocity)
        {
            var instance = @class.Create();

            instance.Position = position;
            instance.Direction = direction;
            instance.Velocity = velocity;

            Spawn(instance);
            return instance;
        }

        /// <summary>
        /// Spawn a single fluid onto the map
        /// </summary>
        /// <param name="position">The position of the fluid</param>
        /// <param name="velocity">The fluid's initial velocity</param>
        /// <param name="class">The fluid's type</param>
        public void Spawn(FluidClass @class, Vector2 position, Vector2 velocity)
        {
            //todo: don't spawn fluids outside the map (position + radius)

            if (velocity == Vector2.Zero)
            {
                var sector = GetOverlappingSector(position);
                Sectors[sector.Y, sector.X].fluids.Add(new FluidInstance { position = position, velocity = velocity, Class = @class });
            }
            else
                ActiveFluids.Add(new FluidInstance { position = position, velocity = velocity, Class = @class });
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

            for (int i = 0; i < count; ++i)
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

            var decal = new Decal { texture = texture, position = position, angle = angle, scale = scale };
            AddDecal(decal);
            return decal;
        }

        public void AddDecal(Decal decal)
        {
            if (decal.texture == null)
                return;

            var sector = GetOverlappingSector(decal.position);
            Sectors[sector.Y, sector.X].decals.Add(decal);
        }

        /// <summary>
        /// Create a new trigger with the given region and add it to the map
        /// </summary>
        /// <param name="region">The region of the trigger to create</param>
        /// <param name="name">An optional name for the trigger</param>
        /// <returns>The trigger created</returns>
        public Trigger AddTrigger(Rectangle region, string name = null)
        {
            var trigger = new Trigger(region, name);
            AddTrigger(trigger);
            return trigger;
        }
        /// <summary>
        /// Add an existing trigger to the map
        /// </summary>
        /// <param name="trigger">The trigger to add</param>
        public void AddTrigger(Trigger trigger)
        {
            var sectors = GetOverlappingSectors(trigger.Region);
            for (var y = sectors.Top; y < sectors.Bottom; ++y)
            {
                for (var x = sectors.Left; x < sectors.Right; ++x)
                    Sectors[y, x].triggers.Add(trigger);
            }
        }

        /// <summary>
        /// Remove an entity from the map.
        /// ent.OnDestroy() is called when the ent is actually removed from the map
        /// </summary>
        /// <param name="ent">The instance to remove</param>
        /// <remarks>Will be marked for removal to be removed during the next Update cycle</remarks>
        public void Destroy(EntityInstance ent)
        {
            entsToDestroy.Add(ent);
        }

        public void Destroy(Trigger trigger)
        {
            var sectors = GetOverlappingSectors(trigger.Region);
            for (var y = sectors.Top; y < sectors.Bottom; ++y)
            {
                for (var x = sectors.Left; x < sectors.Right; ++x)
                    Sectors[y, x].triggers.Remove(trigger);
            }
        }
    }
}
