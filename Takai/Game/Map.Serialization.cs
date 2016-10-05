using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.Serialization;
using System.Linq;

namespace Takai.Game
{
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
        /// Load tile data from a CSV
        /// </summary>
        /// <remarks>Assumes csv is well formed (all rows are the same length)</remarks>
        public void LoadCsv(Stream Stream)
        {
            var reader = new StreamReader(Stream);
            
            var rows = new List<short[]>();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var split = line.Split(',');
                var values = new short[split.Length];
                for (var i = 0; i < split.Length; i++)
                    values[i] = short.Parse(split[i]);

                if (rows.Count > 0 && Width != values.Length)
                    throw new InvalidDataException("All rows must be the same length");

                rows.Add(values);
                Width = values.Length;
            }
            
            Height = rows.Count;

            Tiles = new short[Height, Width];
            for (var i = 0; i < Height; i++)
                for (var j = 0; j < Width; j++)
                    Tiles[i, j] = rows[i][j];
        }

        /// <summary>
        /// A temporary struct for organizing data to be serialized
        /// </summary>
        struct MapSave
        {
            public string name;

            public int width, height;
            public int tileSize;
            public Texture2D tilesImage;
            public short[] tiles;

            public MapState state;
            
            public MapSave(Map Map)
            {
                name = Map.Name;
                width = Map.Width;
                height = Map.Height;
                tilesImage = Map.TilesImage;
                tileSize = Map.TileSize;
                tiles = new short[width * height];
                Buffer.BlockCopy(Map.Tiles, 0, tiles, 0, width * height * sizeof(short));

                state = new MapState(Map);
            }
        }

        public void Save(Stream Stream)
        {
            var save = new MapSave(this);
            using (var writer = new StreamWriter(Stream))
                Data.Serializer.TextSerialize(writer, save);
        }

        public void Load(Stream Stream, bool LoadState = true)
        {
            MapSave load;
            using (var reader = new StreamReader(Stream))
            {
                var temp = Data.Serializer.TextDeserialize(reader);
                if (!(temp is MapSave))
                    return;
                load = (MapSave)temp;
            }
            
            Name = load.name;
            Width = load.width;
            Height = load.height;
            TilesImage = load.tilesImage;
            TileSize = load.tileSize;
            Tiles = new short[Height, Width];
            Buffer.BlockCopy(load.tiles, 0, Tiles, 0, Width * Height * sizeof(short));

            BuildSectors();
            BuildTileMask(TilesImage);

            if (LoadState)
                this.LoadState(load.state);
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
                    int index;
                    if (!typeIndices.TryGetValue(type, out index))
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
                        int index;
                        if (!typeIndices.TryGetValue(type, out index))
                        {
                            index = blobTypes.Count;
                            blobTypes.Add(type);
                            typeIndices.Add(type, index);
                        }
                        blobs.Add(new BlobSave { type = index, position = blob.position, velocity = blob.velocity });
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
            for (var i = 0; i < State.blobTypes.Count; i++)
                blobTypes[i] = State.blobTypes[i];

            foreach (var ent in State.entities)
            {
                ent.Map = this;
                ent.OnSpawn();
            }

            ActiveEnts = State.entities;

            ActiveBlobs = State.blobs.Select((BlobSave Save) =>
            {
                return new Blob
                {
                    type = blobTypes[Save.type],
                    position = Save.position,
                    velocity = Save.velocity
                };
            }).ToList();

            BuildSectors();

            foreach (var decal in State.decals)
                AddDecal(decal);
        }
    }
}
