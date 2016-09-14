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
    /// <param name="TriggerType"></param>
    /// <param name="TriggerValue"></param>
    public delegate void TriggeredEvent(string TriggerType, int TriggerValue);

    /// <summary>
    /// A 2D, tile based map system that handles logic and rendering of the map
    /// </summary>
    public partial class Map
    {
        /// <summary>
        /// The file that this map was loaded for
        /// </summary>
        /// <remarks>This must be set in order to create a save-state</remarks>
        public string File { get; set; }

        /// <summary>
        /// The human-friendly map name
        /// </summary>
        public string Name { get; set; }
        
        public bool[] TilesMask { get; set; }

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
        public int SectorPixelSize { get; private set; } = 0;

        /// <summary>
        /// All of the tiles in the map
        /// </summary>
        /// <remarks>Size is Height,Width</remarks>
        public short[,] Tiles { get; set; }
        /// <summary>
        /// All of the sectors in the map
        /// Typically inactive entities are stored here
        /// </summary>
        /// <remarks>Size is Height,Width</remarks>
        public MapSector[,] Sectors { get; protected set; }
        protected Point NumSectors { get; private set; }

        /// <summary>
        /// The list of active particles. Not serialized
        /// </summary>
        public Dictionary<ParticleType, List<Particle>> Particles { get; protected set; } = new Dictionary<ParticleType, List<Particle>>();

        /// <summary>
        /// The set of live entities that are being updated.
        /// Entities not in this list are not updated
        /// Automatically updated as the view changes
        /// </summary>
        public List<Entity> ActiveEnts { get; protected set; } = new List<Entity>(128);
        /// <summary>
        /// The list of live blobs. Once the blobs' velocity is zero, they are removed from this and not re-added
        /// </summary>
        public List<Blob> ActiveBlobs { get; protected set; } = new List<Blob>(128);
        
        /// <summary>
        /// Event handlers for specific triggered events
        /// Name -> Handlers
        /// </summary>
        public Dictionary<string, TriggeredEvent> EventHandlers = new Dictionary<string, TriggeredEvent>();

        /// <summary>
        /// Trigger an event
        /// </summary>
        /// <param name="Name">The name of the trigger</param>
        /// <param name="Value">The value of the trigger. Value interpreted by the handler</param>
        public void TriggerEvent(string Name, int Value)
        {
            TriggeredEvent handler;
            if (EventHandlers.TryGetValue(Name, out handler))
                handler(Name, Value);
        }   

        /// <summary>
        /// Adds an existing entity to the map
        /// </summary>
        /// <param name="Entity">The entity to add</param>
        /// <param name="AddToActive">Add this entity to the active set (defaults to true)</param>
        public void Spawn(Entity Entity, bool AddToActive = true)
        {
            if (Entity.Map != null)
                Destroy(Entity);

            Entity.Map = this;
            Entity.OnSpawn();

            if (AddToActive)
                ActiveEnts.Add(Entity);
            else
            {
                var sector = GetSector(Entity.Position);
                Sectors[sector.Y, sector.X].entities.Add(Entity);
            }
        }

        /// <summary>
        /// Spawn an entity in the map
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to spawn</typeparam>
        /// <param name="Position">Where to spawn the entity</param>
        /// <param name="Direction">The direction the entity should face</param>
        /// <param name="Velocity">The entity's initial velocity</param>
        /// <param name="LoadEntity">Should the entity be loaded, defaults to true</param>
        /// <returns>The entity spawned</returns>
        public TEntity Spawn<TEntity>(Vector2 Position, Vector2 Direction, Vector2 Velocity) where TEntity : Entity, new()
        {
            var ent = new TEntity();
            ent.Map = this;
            ent.OnSpawn();

            ent.Position = Position;
            ent.Direction = Direction;
            ent.Velocity = Velocity;

            //will be removed in next update if out of visible range
            //only inactive entities are placed in sectors
            ActiveEnts.Add(ent);
            
            return ent;
        }

        /// <summary>
        /// Spawn an entity, cloning another
        /// </summary>
        /// <param name="Template">The entity to clone</param>
        /// <param name="Position">Where to spawn the entity</param>
        /// <param name="Direction">The direction the entity should face</param>
        /// <param name="Velocity">The entity's initial velocity</param>
        /// <param name="LoadEntity">Should the entity be loaded, defaults to true</param>
        /// <returns>The new entity spawned</returns>
        public TEntity Spawn<TEntity>(TEntity Template, Vector2 Position, Vector2 Direction, Vector2 Velocity) where TEntity : Entity, new()
        {
            var ent = (TEntity)Template.Clone();
            ent.Map = this;
            ent.OnSpawn();
            
            ent.Position = Position;
            ent.Direction = Direction;
            ent.Velocity = Velocity;

            //will be removed in next update if out of visible range
            //only inactive entities are placed in sectors
            ActiveEnts.Add(ent);

            return ent;
        }

        /// <summary>
        /// Spawn a single blob onto the map
        /// </summary>
        /// <param name="Position">The position of the blob</param>
        /// <param name="Velocity">The blob's initial velocity</param>
        /// <param name="Type">The blob's type</param>
        public void Spawn(BlobType Type, Vector2 Position, Vector2 Velocity)
        {
            //todo: don't spawn blobs outside the map (position + radius)

            if (Velocity == Vector2.Zero)
            {
                var sector = GetSector(Position);
                Sectors[sector.Y, sector.X].blobs.Add(new Blob { position = Position, velocity = Velocity, type = Type });
            }
            else
                ActiveBlobs.Add(new Blob { position = Position, velocity = Velocity, type = Type });
        }

        /// <summary>
        /// Spawn particles
        /// </summary>
        /// <param name="Spawn">The rules for spawning</param>
        public void Spawn(ParticleSpawn Spawn)
        {
            int count = random.Next(Spawn.count.min, Spawn.count.max);

            if (!Particles.ContainsKey(Spawn.type))
                Particles.Add(Spawn.type, new List<Particle>());

            for (int i = 0; i < count; i++)
            {
                var lifetime = RandTime(Spawn.lifetime.min, Spawn.lifetime.max);
                if (lifetime <= System.TimeSpan.Zero)
                    continue;

                var delay = RandTime(Spawn.delay.min, Spawn.delay.max);

                var theta = RandFloat(Spawn.angle.min, Spawn.angle.max);
                var dir = new Vector2
                (
                    (float)System.Math.Cos(theta),
                    (float)System.Math.Sin(theta)
                );

                Particles[Spawn.type].Add(new Particle
                {
                    time     = System.TimeSpan.Zero,
                    lifetime = lifetime,
                    delay    = delay,

                    position = RandVector2(Spawn.position.min, Spawn.position.max),
                    direction = dir,

                    //cached
                    speed = Spawn.type.Speed.start,
                    scale = Spawn.type.Scale.start,
                    color = Spawn.type.Color.start
                });
            }
        }

        /// <summary>
        /// Add a decal to the map
        /// </summary>
        /// <param name="Texture"></param>
        /// <param name="Position">The position of the decal on the map</param>
        /// <param name="Angle">The angle the decal faces</param>
        /// <param name="Scale">How much to scale the decal</param>
        public void AddDecal(Texture2D Texture, Vector2 Position, float Angle = 0, float Scale = 1)
        {
            if (Texture == null)
                return;

            var sector = GetSector(Position);
            Sectors[sector.Y, sector.X].decals.Add(new Decal { texture = Texture, position = Position, angle = Angle, scale = Scale });
        }

        public void AddDecal(Decal Decal)
        {
            if (Decal.texture == null)
                return;

            var sector = GetSector(Decal.position);
            Sectors[sector.Y, sector.X].decals.Add(Decal);
        }

        /// <summary>
        /// Remove an entity from the map.
        /// Entity.OnDestroy() is called immediately
        /// </summary>
        /// <param name="Ent">The entity to remove</param>
        /// <remarks>Will be marked for removal to be removed during the next Update cycle</remarks>
        public void Destroy(Entity Ent)
        {
            Ent.OnDestroy();
            Ent.Map = null;
        }
    }
}
