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

            map.InitializeGraphics(GameState.GameStateManager.Game.GraphicsDevice);
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
            LoadState((MapState)Props["State"]);
            
            Tiles = new short[Height, Width];
            var tiles = Data.Serializer.CastType<List<short>>(Props["Tiles"]);
            Buffer.BlockCopy(tiles.ToArray(), 0, Tiles, 0, Width * Height * sizeof(short));

            TilesPerRow = (TilesImage != null ? (TilesImage.Width / TileSize) : 0);
            SectorPixelSize = SectorSize * TileSize;

            BuildSectors();
            BuildTileMask(TilesImage, true);
        }

        struct BlobSave
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
            public List<BlobType> blobTypes;
            public List<BlobSave> blobs;
            public List<Decal> decals;

            public TimeSpan elapsedTime;
            public float timeScale;

            public MapState(Map Map)
            {
                entities = new List<Entity>(Map.ActiveEnts);
                blobTypes = new List<BlobType>();
                blobs = new List<BlobSave>();
                decals = new List<Decal>();
                elapsedTime = Map.ElapsedTime;
                timeScale = Map.TimeScale;

                //todo: particles?

                var typeIndices = new Dictionary<BlobType, int>();

                foreach (var blob in Map.ActiveBlobs)
                {
                    var type = blob.type;
                    if (!typeIndices.TryGetValue(type, out var index))
                    {
                        index = blobTypes.Count;
                        blobTypes.Add(type);
                        typeIndices.Add(type, index);
                    }
                    blobs.Add(new BlobSave { type = index, position = blob.position, velocity = blob.velocity });
                }

                foreach (var sector in (System.Collections.IEnumerable)Map.Sectors)
                {
                    var s = (MapSector)sector;

                    entities.AddRange(s.entities);
                    decals.AddRange(s.decals);

                    foreach (var blob in s.blobs)
                    {
                        var type = blob.type;
                        if (!typeIndices.TryGetValue(type, out var index))
                        {
                            index = blobTypes.Count;
                            blobTypes.Add(type);
                            typeIndices.Add(type, index);
                        }
                        blobs.Add(new BlobSave
                        {
                            type = index,
                            position = blob.position,
                            velocity = blob.velocity
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

            var blobTypes = new Dictionary<int, BlobType>();
            for (var i = 0; i < State.blobTypes?.Count; i++)
                blobTypes[i] = State.blobTypes[i];

            ActiveEnts = State.entities ?? new List<Entity>();
            foreach (var ent in ActiveEnts)
            {
                ent.Map = this;
                ent.OnSpawn();
            }

            ActiveBlobs = State.blobs?.Select((BlobSave Save) =>
            {
                return new Blob
                {
                    type = blobTypes[Save.type],
                    position = Save.position,
                    velocity = Save.velocity
                };
            }).ToList() ?? new List<Blob>();

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
