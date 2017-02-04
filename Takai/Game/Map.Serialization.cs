using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace Takai.Game
{
    [Data.CustomSerialize(typeof(Map), "Serialize"),
     Data.CustomDeserialize(typeof(Map), "Deserialize")]
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

        public void Save()
        {

        }

        //todo: re-add save as tradition. integrate with new save functionality

        static object Serialize(Object Source)
        {
            var map = (Map)Source;
            return new Dictionary<string, object>
            {
                ["Name"] = map.Name,
                ["width"] = map.Width,
                ["height"] = map.Height,
                ["tilesImage"] = map.TilesImage,
                ["tileSize"] = map.TileSize,
                ["Tiles"] = map.Tiles,
                ["State"] = new MapState(map)
            };
        }
        static object Deserialize(Object des) { return des; }

        public void Load(Stream Stream)
        {
            Dictionary<string, object> load;
            using (var reader = new StreamReader(Stream))
                load = (Dictionary<string, object>)Data.Serializer.TextDeserialize(reader);

            T TryGet<T>(string Entry)
            {
                if (load.TryGetValue(Entry, out var value))
                    return Data.Serializer.CastType<T>(value);
                return default(T);
            }

            Name = TryGet<string>("Name");
            Width = TryGet<int>("Width");
            Height = TryGet<int>("Height");
            TilesImage = TryGet<Texture2D>("TilesImage");
            TileSize = TryGet<int>("TileSize");
            Tiles = new short[Height, Width];
            var tiles = TryGet<short[]>("Tiles");
            Buffer.BlockCopy(tiles, 0, Tiles, 0, Width * Height * sizeof(short));

            BuildSectors();
            BuildTileMask(TilesImage, true);

            LoadState(TryGet<MapState>("State"));
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

            public MapState(Map Map)
            {
                entities = new List<Entity>(Map.ActiveEnts);
                blobTypes = new List<BlobType>();
                blobs = new List<BlobSave>();
                decals = new List<Decal>();

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
        }
    }
}
