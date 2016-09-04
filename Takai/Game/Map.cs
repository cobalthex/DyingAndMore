using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Takai.Game
{
    /// <summary>
    /// Specifies a simple min/max range
    /// </summary>
    /// <typeparam name="T">The type of range</typeparam>
    public struct Range<T>
    {
        public T min;
        public T max;

        public Range(T Value)
        {
            min = max = Value;
        }
        public Range(T Min, T Max)
        {
            min = Min;
            max = Max;
        }
    }

    /// <summary>
    /// A single type of blob
    /// This struct defines the graphics for the blob and physical properties that can affect the game
    /// </summary>
    [Data.DesignerCreatable]
    public class BlobType
    {
        /// <summary>
        /// The texture to render the blob with
        /// </summary>
        public Texture2D Texture { get; set; }
        /// <summary>
        /// A reflection map, controlling reflection of entities, similar to a normal map
        /// Set to null or alpha = 0 for no reflection
        /// </summary>
        public Texture2D Reflection { get; set; }

        /// <summary>
        /// The radius of an individual blob
        /// </summary>
        public float Radius { get; set; }
        /// <summary>
        /// Drag affects both how quickly the blob stops moving and how much resistance there is to entities moving through it
        /// </summary>
        public float Drag { get; set; }
    }

    /// <summary>
    /// A single blob, rendered as a metablob
    /// Blobs can have physics per their blob type
    /// Blobs can be spawned with a velocity which is decreased by their drag over time. Once the velocity reaches zero, the blob is considered inactive (permanently)
    /// </summary>
    [Data.DesignerCreatable]
    public struct Blob
    {
        public BlobType type;
        public Vector2 position;
        public Vector2 velocity;
    }

    [Data.DesignerCreatable]
    public struct Decal
    {
        public Texture2D texture;
        public Vector2 position;
        public float angle;
        public float scale;
    }

    /// <summary>
    /// A single type of particle
    /// </summary>
    [Data.DesignerCreatable]
    public class ParticleType
    {
        /// <summary>
        /// The grahpic used for each particle of this type
        /// </summary>
        public Graphics.Graphic Graphic { get; set; }
        /// <summary>
        /// How to blend this particle
        /// </summary>
        public BlendState BlendMode { get; set; }
        /// <summary>
        /// How quickly this icon should slow down, 0 for none, -1 for speed up
        /// </summary>
        public float Drag { get; set; }

        //todo: maybe switch values to 'over time' values w/ curves
    }

    /// <summary>
    /// An individual particle
    /// </summary>
    public struct Particle
    {
        public Vector2 position;
        public Vector2 velocity;
        public System.TimeSpan lifetime;
        public System.TimeSpan delay;
        public System.TimeSpan time; //spawn time
        //angular velocity
    }

    /// <summary>
    /// Description for spawning particles
    /// </summary>
    public struct ParticleSpawn
    {
        public ParticleType type;

        public Range<int> count;
        public Range<Vector2> position;
        public Range<float> angle;
        public Range<float> speed;
        public Range<System.TimeSpan> lifetime;
        public Range<System.TimeSpan> delay;
    }

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
    /// A 2D, tile based map system that handles logic and rendering of the map
    /// </summary>
    public partial class Map
    {
        private System.Random random = new System.Random();
        private byte[] _r64b = new byte[8];

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
        /// The set of live entities that are being updated.
        /// Entities not in this list are not updated
        /// Automatically updated as the view changes
        /// </summary>
        public List<Entity> ActiveEnts { get; protected set; } = new List<Entity>(128);
        /// <summary>
        /// The list of live blobs. Once the blobs' velocity is zero, they are removed from this and not re-added
        /// </summary>
        public List<Blob> ActiveBlobs { get; protected set; } = new List<Blob>(32);

        /// <summary>
        /// The list of active particles. Not serialized
        /// </summary>
        [Data.NonSerialized]
        public Dictionary<ParticleType, List<Particle>> Particles { get; protected set; } = new Dictionary<ParticleType, List<Particle>>();
        
        /// <summary>
        /// Build the tiles mask
        /// </summary>
        /// <param name="Texture">The source texture to use</param>
        /// <param name="UseAlpha">Use alpha instead of color value</param>
        /// <remarks>Values with luma/alpha < 0.5 are off and all others are on</remarks>
        public void BuildMask(Texture2D Texture, bool UseAlpha = false)
        {
            Color[] pixels = new Color[Texture.Width * Texture.Height];
            Texture.GetData(pixels);

            TilesMask = new bool[Texture.Height * Texture.Width];
            for (var i = 0; i < pixels.Length; i++)
            {
                if (UseAlpha)
                    TilesMask[i] = pixels[i].A > 127;
                else
                    TilesMask[i] = (pixels[i].R + pixels[i].G + pixels[i].B) / 3 > 127;
            }
        }

        /// <summary>
        /// Create the spacial subdivisions for the map
        /// </summary>
        public void BuildSectors()
        {
            if (Tiles != null)
            {
                //round up
                var width = 1 + ((Tiles.GetLength(1) - 1) / SectorSize);
                var height = 1 + ((Tiles.GetLength(0) - 1) / SectorSize);

                Sectors = new MapSector[height, width];

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                        Sectors[y, x] = new MapSector();
                }
            }
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
        public TEntity Spawn<TEntity>(Vector2 Position, Vector2 Direction, Vector2 Velocity, bool LoadEntity = true) where TEntity : Entity, new()
        {
            var ent = new TEntity();
            ent.Map = this;

            ent.Position = Position;
            ent.Direction = Direction;
            ent.Velocity = Velocity;

            //will be removed in next update if out of visible range
            //only inactive entities are placed in sectors
            ActiveEnts.Add(ent);

            if (LoadEntity)
                ent.Load();
            
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
        public TEntity Spawn<TEntity>(TEntity Template, Vector2 Position, Vector2 Direction, Vector2 Velocity, bool LoadEntity = true) where TEntity : Entity, new()
        {
            var ent = (TEntity)Template.Clone();
            ent.Map = this;
            
            ent.Position = Position;
            ent.Direction = Direction;
            ent.Velocity = Velocity;

            //will be removed in next update if out of visible range
            //only inactive entities are placed in sectors
            ActiveEnts.Add(ent);

            if (LoadEntity)
                ent.Load();

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
                random.NextBytes(_r64b);
                var lifetime = Spawn.lifetime.max.Ticks - Spawn.lifetime.min.Ticks;
                if (lifetime < 0)
                    continue;
                lifetime = (lifetime > 0 ? (System.BitConverter.ToInt64(_r64b, 0) % lifetime) : 0) + Spawn.lifetime.min.Ticks;

                random.NextBytes(_r64b);
                var delay = Spawn.delay.max.Ticks - Spawn.delay.min.Ticks;
                     delay = delay > 0 ? (System.BitConverter.ToInt64(_r64b, 0) % delay) + Spawn.delay.min.Ticks : 0;

                var speed = (float)((random.NextDouble() * (Spawn.speed.max - Spawn.speed.min)) + Spawn.speed.min);

                var theta = (random.NextDouble() * (Spawn.angle.max - Spawn.angle.min)) + Spawn.angle.min;

                Particles[Spawn.type].Add(new Particle
                {
                    time = System.TimeSpan.Zero,
                    lifetime = System.TimeSpan.FromTicks(lifetime),
                    delay = System.TimeSpan.FromTicks(delay),
                    position = new Vector2
                    (
                        (float)((random.NextDouble() * (Spawn.position.max.X - Spawn.position.min.X)) + Spawn.position.min.X),
                        (float)((random.NextDouble() * (Spawn.position.max.Y - Spawn.position.min.Y)) + Spawn.position.min.Y)
                    ),
                    velocity = new Vector2
                    (
                        (float)System.Math.Cos(theta),
                        (float)System.Math.Sin(theta)
                    ) * (float)((random.NextDouble() * (Spawn.speed.max - Spawn.speed.min)) + Spawn.speed.min),
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
        /// Remove an entity from the map
        /// </summary>
        /// <param name="Ent">The entity to remove</param>
        /// <remarks>Will be marked for removal to be removed during the next Update cycle</remarks>
        public void Destroy(Entity Ent)
        {
            Ent.Map = null;
        }
    }
}
