using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;
using System.Linq;

namespace Takai.Game
{
    public partial class Map
    {
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
            public int width, height;
            public short[,] tiles;

            public MapState state;

            public MapSave(Map Map)
            {
                width = Map.Width;
                height = Map.Height;
                tiles = Map.Tiles;
                state = new MapState(Map);
            }
        }

        public void Save(Stream Stream)
        {
            var save = new MapSave(this);
            using (var writer = new StreamWriter(Stream))
                Data.Serializer.TextSerialize(writer, save);
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

            //decals are 

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

            var blobTypes = new Dictionary<int, BlobType>();
            for (var i = 0; i < load.blobTypes.Count; i++)
                blobTypes[i] = load.blobTypes[i];

            foreach (var ent in load.entities)
                ent.Map = this;

            ActiveEnts = load.entities;
            ActiveBlobs = load.blobs.Select((BlobSave Save) =>
            {
                return new Blob
                {
                    type = blobTypes[Save.type],
                    position = Save.position,
                    velocity = Save.velocity
                };
            }).ToList();

            BuildSectors();

            foreach (var decal in load.decals)
                AddDecal(decal);
        }
    }
}
