using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    public class InitialMapState
    {
        public struct EntitySpawn
        {
            public EntityClass Class { get; set; }
            public Vector2 Position { get; set; }
            public Vector2 Forward { get; set; }
            public string Name { get; set; }

            //Initial state
        }

        public List<EntitySpawn> Entities { get; set; }
        public List<FluidInstance> Fluids { get; set; }
        public List<Decal> Decals { get; set; }
    }

    /// <summary>
    /// An intermediate struct encapsulating tileset properties
    /// </summary>
    public struct Tileset
    {
        public Texture2D texture; //sprite?
        public int size;
    }

    /// <summary>
    /// The (mostly) static properties of a single map
    /// </summary>
    public partial class MapClass : IObjectClass<MapInstance>
    {
        [Data.Serializer.Ignored]
        public string File { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// The collision mask for the tilemap
        /// </summary>
        [Data.Serializer.Ignored]
        public System.Collections.BitArray CollisionMask { get; set; }

        /// <summary>
        /// All of the tiles in the map (y,x)
        /// </summary>
        [Data.Serializer.Ignored]
        public short[,] Tiles { get; set; } = new short[0, 0];

        /// <summary>
        /// The Width and height of each tile in the map
        /// </summary>
        public int TileSize
        {
            get { return _tileSize; }
            set
            {
                _tileSize = value;
                TilesPerRow = (TilesImage != null ? (TilesImage.Width / value) : 0);
                SectorPixelSize = SectorSize * value;
            }
        }
        private int _tileSize = 1;
        [Data.Serializer.Ignored]
        public int TilesPerRow { get; private set; } = 1;

        /// <summary>
        /// The horizontal size of the map in tiles
        /// </summary>
        public int Width => Tiles.GetLength(1);
        /// <summary>
        /// The vertical size of the map in tiles
        /// </summary>
        public int Height => Tiles.GetLength(0);

        /// <summary>
        /// The bounds of the map in tiles
        /// </summary>
        public Rectangle TileBounds => new Rectangle(0, 0, Width, Height);

        public const int SectorSize = 4; //The number of tiles that a map sector spans
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
        /// The initial state of this map. This is what is first created when this map is played
        /// </summary>
        public InitialMapState InitialState { get; set; } //todo: lazy

        public MapInstance Create()
        {
            var instance = new MapInstance(this);

            if (InitialState != null)
            {
                if (InitialState.Entities != null)
                    foreach (var ent in InitialState.Entities)
                        instance.Spawn(ent.Class, ent.Position, ent.Forward, Vector2.Zero, ent.Name);

                if (InitialState.Fluids != null)
                    foreach (var fluid in InitialState.Fluids)
                        instance.Spawn(fluid);

                if (InitialState.Decals != null)
                    foreach (var decal in InitialState.Decals)
                        instance.AddDecal(decal);
            }

            return instance;
        }

    }

    /// <summary>
    /// A single sector of the map, used for spacial calculations
    /// Sectors typically contain inactive entities that are not visible as well as dummy objects (inactive Fluids, decals).
    /// </summary>
    public class MapSector
    {
        //dynamic
        public HashSet<EntityInstance> entities = new HashSet<EntityInstance>(); //todo: profile against list

        //static
        public List<FluidInstance> fluids = new List<FluidInstance>();
        public List<Decal> decals = new List<Decal>();
        public List<Trigger> triggers = new List<Trigger>(); //triggers may be in one or more sectors
    }

    public partial class MapInstance : IObjectInstance<MapClass>
    {
        /// <summary>
        /// The map backing this instance. Cannot be null
        /// Changing the class will reset the map
        /// </summary>
        public MapClass Class
        {
            get => _class;
            set
            {
                if (value != null && value != _class)
                {
                    _class = value;

                    Sectors = new MapSector[
                        Util.CeilDiv(_class.Height, MapClass.SectorSize),
                        Util.CeilDiv(_class.Width, MapClass.SectorSize)
                    ];

                    for (int y = 0; y < Sectors.GetLength(0); ++y)
                        for (int x = 0; x < Sectors.GetLength(1); ++x)
                            Sectors[y, x] = new MapSector();

                    _allEntities.Clear();
                }
            }
        }
        private MapClass _class;

        /// <summary>
        /// Grid based spacial storage for objects in the map
        /// </summary>
        /// <remarks>Size is Height,Width</remarks>
        [Data.Serializer.Ignored]
        public MapSector[,] Sectors { get; protected set; } = new MapSector[0, 0];

        /// <summary>
        /// An enumerable list of all entities, entities must be added through Spawn()
        /// </summary>
        [Data.Serializer.Ignored]
        public IEnumerable<EntityInstance> AllEntities //todo: move to serialization
        {
            get => _allEntities;
        }
        private HashSet<EntityInstance> _allEntities = new HashSet<EntityInstance>();

        /// <summary>
        /// The list of live Fluids. Once the Fluids' velocity is zero, they are removed from this and not re-added
        /// </summary>
        [Data.Serializer.Ignored]
        public List<FluidInstance> LiveFluids { get; protected set; } = new List<FluidInstance>(128);

        /// <summary>
        /// Currently playing sounds
        /// </summary>
        public List<SoundInstance> Sounds { get; protected set; } = new List<SoundInstance>(16);

        [Data.Serializer.Ignored] //particles are not serialized (maybe?)
        public Dictionary<ParticleClass, List<ParticleInstance>> Particles { get; protected set; } = new Dictionary<ParticleClass, List<ParticleInstance>>();

        public Dictionary<string, Script> Scripts { get; set; } = new Dictionary<string, Script>();

        /// <summary>
        /// The currently active camera. Determines what part of the map is active
        /// </summary>
        public Camera ActiveCamera { get; set; } //todo: necessary?

        public MapInstance() { }

        public MapInstance(MapClass @class)
        {
            System.Diagnostics.Contracts.Contract.Assert(@class != null);
            Class = @class;
        }

        #region Spawning/Destroying

        /// <summary>
        /// Spawn a new entity
        /// </summary>
        /// <param name="entity">The entity class to create from</param>
        /// <param name="position">Where to spawn the entity</param>
        /// <param name="forward">Where the entity should face</param>
        /// <param name="velocity">How fast the entity is moving</param>
        /// <param name="name">The name of the entity to spawn</param>
        /// <returns>The spawned entity</returns>
        public EntityInstance Spawn(EntityClass entity, Vector2 position, Vector2 forward, Vector2 velocity, string name = null)
        {
            var instance = entity.Create();

            instance.Position = position;
            instance.Forward = forward;
            instance.Velocity = velocity;
            instance.Name = name;

            Spawn(instance);
            return instance;
        }

        /// <summary>
        /// Adds an existing instance to the map
        /// </summary>
        /// <param name="instance">The instance to add</param>
        public void Spawn(EntityInstance instance)
        {
            if (instance.Map != this && instance.Map != null)
                instance.Map.Destroy(instance);

            instance.Map = this;
            instance.SpawnTime = ElapsedTime;
            instance.RefreshBounds();
            instance.OnSpawn();

            _allEntities.Add(instance);

            var sectors = GetOverlappingSectors(instance.AxisAlignedBounds);
            for (int y = sectors.Top; y < sectors.Bottom; ++y)
            {
                for (int x = sectors.Left; x < sectors.Right; ++x)
                    Sectors[y, x].entities.Add(instance);
            }

            var fx = instance.Class?.SpawnEffect?.Create(instance);
            if (fx.HasValue)
                Spawn(fx.Value);
        }

        public void Destroy(EntityInstance ent)
        {
            entsToDestroy.Add(ent);
        }

        /// <summary>
        /// Destroy an entity without removing it from the map
        /// </summary>
        /// <param name="instance">the ent to destroy</param>
        protected void FinalDestroy(EntityInstance instance)
        {
            var fx = instance.Class?.DestructionEffect?.Create(instance);
            if (fx.HasValue)
                Spawn(fx.Value);

            instance.OnDestroy();
            instance.SpawnTime = TimeSpan.Zero;
            instance.Map = null;
            activeEntities.Remove(instance);
            _allEntities.Remove(instance);
        }
        /// <summary>
        /// Remove an entity from the map sectors
        /// </summary>
        /// <param name="instance">The entity to remove</param>
        protected void RemoveFromSectors(EntityInstance instance)
        {
            var sectors = GetOverlappingSectors(instance.AxisAlignedBounds);
            for (int y = sectors.Top; y < sectors.Bottom; ++y)
            {
                for (int x = sectors.Left; x < sectors.Right; ++x)
                    Sectors[y, x].entities.Remove(instance);
            }
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
        public void Destroy(Trigger trigger)
        {
            var sectors = GetOverlappingSectors(trigger.Region);
            for (var y = sectors.Top; y < sectors.Bottom; ++y)
            {
                for (var x = sectors.Left; x < sectors.Right; ++x)
                    Sectors[y, x].triggers.Remove(trigger);
            }
        }

        /// <summary>
        /// Spawn a single fluid onto the map
        /// </summary>
        /// <param name="fluid">The type of fluid to spawn</param>
        /// <param name="position">Where to spawn the fluid (center)</param>
        /// <param name="velocity">How fast the fluid should be moving</param>
        public void Spawn(FluidClass fluid, Vector2 position, Vector2 velocity)
        {
            var instance = fluid.Create();
            instance.position = position;
            instance.velocity = velocity;
            Spawn(instance);
        }

        public void Spawn(FluidInstance instance)
        {
            var sz = new Point((int)instance.Class.Radius);
            if (!Class.Bounds.Intersects(new Rectangle(instance.position.ToPoint() - sz, sz)))
                return;

            if (instance.velocity == Vector2.Zero)
            {
                var sector = GetOverlappingSector(instance.position);
                Sectors[sector.Y, sector.X].fluids.Add(instance);
            }
            else
                LiveFluids.Add(instance);
        }

        public void Spawn(SoundClass sound, Vector2 position, Vector2 forward, Vector2 velocity)
        {
            if (sound.Sound == null)
                return;

            var instance = sound.Create();
            instance.Position = position;
            instance.Forward = forward;
            instance.Velocity = velocity;
            instance.Instance.Play();
            Sounds.Add(instance);
        }

        public void Spawn(EffectsInstance effects)
        {
            if (effects.Class == null)
                return;

            effects.Map = this;
            foreach (var effect in effects.Class.Effects)
                effect.Spawn(effects);
        }

        public void Spawn(Script script)
        {
            script.Map = this;
            script.StartTime = ElapsedTime;
            script.OnSpawn();
            Scripts[script.Name] = script;
        }

        public void Destroy(Script script)
        {
            Scripts.Remove(script.Name); //todo: use hashset?
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

        #endregion
    }
}