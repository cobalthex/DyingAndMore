using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

//todo: Map pack file format:
// zip archive: [map.tk, state.tk] <- map = MapClass, state = MapInstance. all saves refer back to map.zip/map.tk

namespace Takai.Game
{
    /// <summary>
    /// The (mostly) static properties of a single map
    /// </summary>
    public partial class MapBaseClass : Data.INamedClass<MapBaseInstance>
    {
        [Data.Serializer.Ignored]
        public string File { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// The SDF based collision mask for the map.
        /// Values represent how far from point in a circle to an map edge (capped at 255).
        /// Stored row major
        /// </summary>
        [Data.Serializer.Ignored]
        public Byte[] CollisionMask { get; set; }
        /// <summary>
        /// The rshift multiple for the collision mask of the map size
        /// e.g. MapWidth >> <see cref="CollisionMaskScale"/>
        /// </summary>
        public const int CollisionMaskScale = 2;
        /// <summary>
        /// Size of the collision mask (rows, columns)
        /// </summary>
        public Point CollisionMaskSize { get; private set; }

        /// <summary>
        /// All of the tiles in the map (y,x)
        /// </summary>
        [Data.Serializer.Ignored]
        public short[,] Tiles { get; set; } = new short[0, 0];

        /// <summary>
        /// The Width and height of each tile in the map
        /// </summary>
        [Data.Serializer.Ignored]
        public int TileSize => Tileset.size;

        /// <summary>
        /// Encapsulated tileset
        /// </summary>
        public Tileset Tileset
        {
            get => _tileset;
            set
            {
                _tileset = value;
                _tileset.CalculateTilesPerRow();

                SectorPixelSize = Math.Max(1, SectorSize * _tileset.size);
            }
        }
        private Tileset _tileset;

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
        public int SectorPixelSize { get; private set; } = 1;

        /// <summary>
        /// The bounds of the map in pixels
        /// </summary>
        [Data.Serializer.Ignored]
        public Rectangle Bounds
        {
            get { return new Rectangle(0, 0, Width * TileSize, Height * TileSize); }
        }

        public MaterialInteractions MaterialInteractions { get; set; } = new MaterialInteractions();

        public virtual MapBaseInstance Instantiate()
        {
            var instance = new MapBaseInstance(this);
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
        public List<SoundInstance> sounds = new List<SoundInstance>(); //all sounds that can be hard from this sector

        //static
        public List<FluidInstance> fluids = new List<FluidInstance>();
        public List<Decal> decals = new List<Decal>();
        public List<Trigger> triggers = new List<Trigger>(); //triggers may be in one or more sectors, list as it shouldn't be modified during runtime

        public override string ToString()
        {
            return $"ents:{entities.Count} sounds:{sounds.Count} fluids:{fluids.Count} decals:{decals.Count} triggers:{triggers.Count}";
        }
    }

    [Data.Cache.AlwaysReload] //todo: necessary?
    public partial class MapBaseInstance : Data.IInstance<MapBaseClass>, Data.ISerializeExternally
    {
        private int nextEntityID = 0;

        [Data.Serializer.Ignored]
        public string File { get; set; }

        /// <summary>
        /// The map backing this instance. Cannot be null
        /// Changing the class will reset the map
        /// </summary>
        public MapBaseClass Class
        {
            get => _class;
            set
            {
                if (value != null && value != _class)
                {
                    _class = value;
                    Resize(Class.Width, Class.Height, false); //ApplyObject needs this false, Class = needs this true
                                                              //if setting class manuall,y should RemoveAllEntities() first
                }
            }
        }
        private MapBaseClass _class;
        
        /// <summary>
        /// Grid based spacial storage for objects in the map
        /// </summary>
        /// <remarks>Size is Height,Width</remarks>
        [Data.Serializer.Ignored]
        public MapSector[,] Sectors { get; protected set; } = null;

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
        /// All particles on the map, grouped by type
        /// </summary>
        [Data.Serializer.Ignored] //particles are not serialized (maybe?)
        public Dictionary<ParticleClass, List<ParticleInstance>> Particles { get; protected set; } = new Dictionary<ParticleClass, List<ParticleInstance>>(4);

        public HashSet<TrailInstance> Trails { get; protected set; } = new HashSet<TrailInstance>();

        /// <summary>
        /// All of the playing sounds
        /// </summary>
        public List<SoundInstance> Sounds { get; set; } = new List<SoundInstance>();

        /// <summary>
        /// Limit the amount of gore spawning in the game (does not affect previously spawned gore)
        /// </summary>
        [Data.Serializer.Ignored]
        public bool LimitGore { get; set; } = false;

        public MapBaseInstance() { }

        public MapBaseInstance(MapBaseClass @class)
        {
            Class = @class;
        }

        ~MapBaseInstance()
        {
            foreach (var sound in Sounds)
                sound.Instance?.Dispose();
            Sounds.Clear();
        }

        #region Spawning/Destroying

        /// <summary>
        /// Spawn a new entity
        /// </summary>
        /// <param name="entity">The entity class to create from</param>
        /// <param name="worldPosition">Where to spawn the entity (World relative)</param>
        /// <param name="worldForward">Where the entity should face (World relative)</param>
        /// <param name="velocity">How fast the entity is moving</param>
        /// <param name="name">The name of the entity to spawn</param>
        /// <returns>The spawned entity</returns>
        public EntityInstance Spawn(EntityClass entity, Vector2 worldPosition, Vector2 worldForward, Vector2 velocity, string name = null)
        {
            var instance = entity.Instantiate();

            instance.SetPositionTransformed(worldPosition);
            instance.SetForwardTransformed(worldForward);
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
            Spawn(instance, true);
        }

        protected void Spawn(EntityInstance instance, bool callSpawnEvents)
        {
            if (instance.Map != this && instance.Map != null)
                instance.Map.Destroy(instance);

            instance.Map = this;
            if (instance.Id > 0)
            {
                if (instance.Id > nextEntityID)
                    nextEntityID = instance.Id;
            }
            else
                instance.Id = (++nextEntityID);
            instance.SpawnTime = ElapsedTime;

            if (callSpawnEvents)
                instance.OnSpawn(this);

            _allEntities.Add(instance);
            //entsAdded.Add(instance);

            var sectors = GetOverlappingSectors(instance.AxisAlignedBounds);
            for (int y = sectors.Top; y < sectors.Bottom; ++y)
            {
                for (int x = sectors.Left; x < sectors.Right; ++x)
                    Sectors[y, x].entities.Add(instance);
            }
            instance.lastAABB = instance.AxisAlignedBounds;
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
            //optimize this behavior?
            Detach(instance);
            if (instance.WorldChildren != null)
            {
                while (instance.WorldChildren.Count > 0)
                    Detach(instance.WorldChildren[0]);
            }

            foreach (var sector in EnumeratateSectorsInRegion(instance.AxisAlignedBounds))
                foreach (var trigger in sector.triggers)
                    trigger.TryExit(instance);

            instance.OnDestroy(this);
            instance.SpawnTime = TimeSpan.Zero;
            //instance.Id = 0;
            instance.Map = null;

            Detach(instance);

            possibleOffscreenEntities.Remove(instance);
            activeEntities.Remove(instance);
            _allEntities.Remove(instance);
            var sectors = GetOverlappingSectors(instance.AxisAlignedBounds);
            for (int y = sectors.Top; y < sectors.Bottom; ++y)
            {
                for (int x = sectors.Left; x < sectors.Right; ++x)
                    Sectors[y, x].entities.Remove(instance);
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
            if (fluid.ContainsGore && LimitGore)
                return;

            var instance = fluid.Instantiate();
            instance.position = position;
            instance.velocity = velocity;
            Spawn(instance);
        }

        public void Spawn(FluidInstance instance)
        {
            if (instance.Class.ContainsGore && LimitGore)
                return;

            var aabb = new Rectangle(
                (int)(instance.position.X - instance.Class.Radius),
                (int)(instance.position.Y - instance.Class.Radius),
                (int)(instance.Class.Radius * 2),
                (int)(instance.Class.Radius * 2)
            );
            if (!Class.Bounds.Intersects(aabb))
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
            if (sound.Source == null)
                return;

            if (sound.ContainsGore && LimitGore)
                return;

            var instance = sound.Instantiate();
            instance.Position = position;
            instance.Forward = forward;
            instance.Velocity = velocity;
            Spawn(instance);
        }

        public void Spawn(SoundInstance sound)
        {
            if (sound.Instance == null)
                return; //logical sounds?

            if (sound.Class.ContainsGore && LimitGore)
                return;

            sound.Instance.Play();
            if (!updateSettings.isSoundEnabled)
                sound.Instance.Pause();

            var range = (int)(Class.TileSize * 5 * sound.Class.Gain);
            var box = new Rectangle(
                (int)sound.Position.X - range,
                (int)sound.Position.Y - range,
                range * 2,
                range * 2
            );
            var sectors = GetOverlappingSectors(box);

            Sounds.Add(sound);

            for (int y = sectors.Top; y < sectors.Bottom; ++y)
                for (int x = sectors.Left; x < sectors.Right; ++x)
                    Sectors[y, x].sounds.Add(sound);
        }

        public void Spawn(EffectsInstance effects)
        {
            if (effects.Class?.Effects == null)
                return;

            effects.Map = this;
            foreach (var effect in effects.Class.Effects)
                effect.Spawn(effects);
        }

        /// <summary>
        /// Add a trail to the map. Will continue to render until faded out completely
        /// </summary>
        /// <param name="trail">The trail to draw</param>
        public void Spawn(TrailInstance trail)
        {
            Trails.Add(trail);
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
            if (decal.texture == null || (decal.containsGore && LimitGore))
                return;

            var sector = GetOverlappingSector(decal.position);
            Sectors[sector.Y, sector.X].decals.Add(decal);
        }

        /// <summary>
        /// Add a trigger to the map
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
        /// Recreate (and reset to default state) all entities
        /// However, they maintain their same world state
        /// </summary>
        public void ReconstructAllEntities()
        {
            var allEnts = new List<EntityInstance>(AllEntities);
            RemoveAllEntities();
            for (int i = 0; i < allEnts.Count; ++i)
                Spawn(allEnts[i].Class, allEnts[i].WorldPosition, allEnts[i].WorldForward, allEnts[i].Velocity, allEnts[i].Name);
        }

        #endregion
    }
}