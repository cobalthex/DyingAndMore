using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;

namespace Takai.Game
{
    public partial class Map
    {
        /// <summary>
        /// Load tile data from a CSV
        /// </summary>
        /// <remarks>Assumes csv is well formed (all rows are the same length)</remarks>
        public void ReadTiles(Stream Stream)
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
        /// write the map tiles to a CSV
        /// </summary>
        /// <param name="Stream">The stream to write to</param>
        public void WriteTiles(Stream Stream)
        {
            var writer = new StreamWriter(Stream);
            
            bool first = true;
            for (int y = 0; y < Tiles.GetLength(0); y++)
            {
                for (int x = 0; x < Tiles.GetLength(1); x++)
                {
                    if (!first)
                        writer.Write(',');
                    first = false;
                    writer.Write(Tiles[y, x]);
                }
                writer.Write('\n');
            }
        }



        /// <summary>
        /// Write this map to a file
        /// </summary>
        /// <param name="Filename">The file to write to</param>
        public void Write(string Filename)
        {

        }

        /// <summary>
        /// Write the state of the map to a file (stores only states for things that change (ents, blobs, etc))
        /// </summary>
        /// <param name="Filename">The file to write to</param>
        /// <remarks>This relies on the map named <see cref="File"/> existing at load time</remarks>
        public void WriteState(string Filename)
        {

        }
    }
}
