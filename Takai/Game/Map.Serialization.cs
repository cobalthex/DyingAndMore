using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace Takai.Game
{
    [Data.DerivedTypeSerialize(typeof(Map), "Serialize"),
     Data.DerivedTypeDeserialize(typeof(Map), "Deserialize")]
    public partial class Map
    {
        /// <summary>
        /// Build the tiles mask
        /// </summary>
        /// <param name="Texture">The source texture to use</param>
        /// <param name="UseAlpha">Use alpha instead of color value</param>
        /// <remarks>Values with luma/alpha < 0.5 are off and all others are on</remarks>
        public void BuildTileMask(Texture2D Texture, bool UseAlpha = false)
        {
            Color[] pixels = new Color[Texture.Width * Texture.Height];
            Texture.GetData(pixels);

            TilesMask = new System.Collections.BitArray(Texture.Height * Texture.Width);
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
            //round up
            var width = 1 + ((Width - 1) / SectorSize);
            var height = 1 + ((Height - 1) / SectorSize);

            Sectors = new MapSector[height, width];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                    Sectors[y, x] = new MapSector();
            }
        }

        public void Save(Stream Stream)
        {
            using (var writer = new StreamWriter(Stream))
                Data.Serializer.TextSerialize(writer, this);
        }

        public static Map Load(Stream Stream)
        {
            Map map;
            using (var reader = new StreamReader(Stream))
                map = (Map)Data.Serializer.TextDeserialize(reader);

            map.InitializeGraphics(Runtime.GameManager.Game.GraphicsDevice);
            return map;
        }

        Dictionary<string, object> Serialize()
        {
            return new Dictionary<string, object>
            {
                ["Tiles"] = Tiles,
                ["State"] = new MapState(this)
            };
        }
        void Deserialize(Dictionary<string, object> Props)
        {
            if (Props.TryGetValue("State", out var state))
                LoadState((MapState)state);
            
            Tiles = new short[Height, Width];
            var tiles = Data.Serializer.CastType<List<short>>(Props["Tiles"]);
            Buffer.BlockCopy(tiles.ToArray(), 0, Tiles, 0, Width * Height * sizeof(short));

            TilesPerRow = (TilesImage != null ? (TilesImage.Width / TileSize) : 0);
            SectorPixelSize = SectorSize * TileSize;

            BuildSectors();
            BuildTileMask(TilesImage, true);
        }

        struct FluidSave
        {
            public int type;
            public Vector2 position;
            public Vector2 velocity;
        }

        /// <summary>
        /// A temporary struct for organizing state data to be serialized
        /// </summary>
        struct MapState
        {
            public List<Entity> entities;
            public List<FluidType> FluidTypes;
            public List<FluidSave> Fluids;
            public List<Decal> decals;

            public TimeSpan elapsedTime;
            public float timeScale;

            public MapState(Map Map)
            {
                entities = new List<Entity>(Map.ActiveEnts);
                FluidTypes = new List<FluidType>();
                Fluids = new List<FluidSave>();
                decals = new List<Decal>();
                elapsedTime = Map.ElapsedTime;
                timeScale = Map.TimeScale;

                //todo: particles?

                var typeIndices = new Dictionary<FluidType, int>();

                foreach (var Fluid in Map.ActiveFluids)
                {
                    var type = Fluid.type;
                    if (!typeIndices.TryGetValue(type, out var index))
                    {
                        index = FluidTypes.Count;
                        FluidTypes.Add(type);
                        typeIndices.Add(type, index);
                    }
                    Fluids.Add(new FluidSave { type = index, position = Fluid.position, velocity = Fluid.velocity });
                }

                foreach (var sector in (System.Collections.IEnumerable)Map.Sectors)
                {
                    var s = (MapSector)sector;

                    entities.AddRange(s.entities);
                    decals.AddRange(s.decals);

                    foreach (var Fluid in s.Fluids)
                    {
                        var type = Fluid.type;
                        if (!typeIndices.TryGetValue(type, out var index))
                        {
                            index = FluidTypes.Count;
                            FluidTypes.Add(type);
                            typeIndices.Add(type, index);
                        }
                        Fluids.Add(new FluidSave
                        {
                            type = index,
                            position = Fluid.position,
                            velocity = Fluid.velocity
                        });
                    }
                }
            }
        }

        public void SaveState(Stream Stream)
        {
            var save = new MapState(this);
            using (var writer = new StreamWriter(Stream))
                Data.Serializer.TextSerialize(writer, save);
        }

        public void LoadState(Stream Stream)
        {
            MapState load;
            using (var reader = new StreamReader(Stream))
            {
                var temp = Data.Serializer.TextDeserialize(reader);
                if (!(temp is MapState))
                    return;
                load = (MapState)temp;
            }

            LoadState(load);
        }

        private void LoadState(MapState State)
        {
            //todo: map time (cache delta time)

            eventHandlers.Clear();

            var FluidTypes = new Dictionary<int, FluidType>();
            for (var i = 0; i < State.FluidTypes?.Count; i++)
                FluidTypes[i] = State.FluidTypes[i];

            ActiveEnts = State.entities ?? new List<Entity>();
            foreach (var ent in ActiveEnts)
            {
                ent.Map = this;
                ent.OnSpawn();
            }

            ActiveFluids = State.Fluids?.Select((FluidSave Save) =>
            {
                return new Fluid
                {
                    type = FluidTypes[Save.type],
                    position = Save.position,
                    velocity = Save.velocity
                };
            }).ToList() ?? new List<Fluid>();

            BuildSectors();

            if (State.decals != null)
            {
                foreach (var decal in State.decals)
                    AddDecal(decal);
            }

            TimeScale = State.timeScale == 0 ? 1 : State.timeScale;
            ElapsedTime = State.elapsedTime;
        }
    }
}
