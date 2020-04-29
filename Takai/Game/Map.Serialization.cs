using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Game
{
    public partial class MapBaseClass : Data.IDerivedSerialize, Data.IDerivedDeserialize
    {
        /// <summary>
        /// Generate the SDF based collision mask from the tile map/tileset.
        /// Calculates 
        /// </summary>
        /// <param name="scale">Fractional scale of the map to generate (in format of Size >> scale)</param>
        /// <remarks> Based on https://github.com/arkanis/single-header-file-c-libs/blob/master/sdt_dead_reckoning.h </remarks>
        public void GenerateCollisionMask(int scale = CollisionMaskScale)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            var w = (Width * TileSize) >> scale;
            var h = (Height * TileSize) >> scale;

            if (w <= 0 || h <= 0)
            {
                CollisionMask = new byte[0, 0];
                return;
            }
            
            //use floats?

            //1px border around map is considered unpathable

            //do each individually for better cache perf

            CollisionMask = new byte[h, w];
            for (int y = 1; y < h - 1; ++y)
                for (int x = 1; x < w - 1; ++x)
                    CollisionMask[y, x] = 255;

            var p = new Point[h, w];
            for (int y = 1; y < h - 1; ++y)
                for (int x = 1; x < w - 1; ++x)
                    p[y, x] = new Point(-1);

            Color[] tilesImageData = new Color[TilesImage.Width * TilesImage.Height];
            TilesImage.GetData(tilesImageData);

            //test if inside the map
            bool I(int x, int y) //x and y here scaled down
            {
                x <<= scale;
                y <<= scale;

                var tile = Tiles[y / TileSize, x / TileSize];
                if (tile < 0)
                    return false;

                const byte threshold = 127;
                return tilesImageData[
                    ((tile / TilesPerRow) * TileSize + (y % TileSize)) * TilesImage.Width +
                    ((tile % TilesPerRow) * TileSize + (x % TileSize))
                ].A > threshold;
            }

            //can skip bounds checking by using padded array, but requires copying array at end
            
            //find edges
            for (int y = 1; y < h - 1; ++y)
            {
                for (int x = 1; x < w - 1; ++x)
                {
                    bool onEdge = (
                        I(x - 1, y) != I(x, y) || I(x + 1, y) != I(x, y) ||
                        I(x, y - 1) != I(x, y) || I(x, y + 1) != I(x, y)
                    );
                    if (!I(x, y) || onEdge)
                    {
                        CollisionMask[y, x] = 0;
                        p[y, x] = new Point(x, y);
                    }

                }
            }

            //distance functions - these assume X - x, Y - y
            byte euclidean(int x, int y) => (byte)Math.Sqrt(x * x + y * y);
            //byte manhattan(int x, int y) => (byte)(Math.Abs(x) + Math.Abs(y));
            //byte chebyshev(int x, int y) => (byte)Math.Max(Math.Abs(x), Math.Abs(y));

            const float dx = 1f, dy = 1f, dh = 1.4142135623730950488f /* sqrt(2) */;

            //first pass
            for (int y = 1; y < h - 1; ++y)
            {
                for (int x = 1; x < w - 1; ++x)
                {
                    if (CollisionMask[y - 1, x - 1] + dh < CollisionMask[y, x])
                    {
                        p[y, x] = p[y - 1, x - 1];
                        CollisionMask[y, x] = euclidean(x - p[y, x].X, y - p[y, x].Y);
                    }
                    if (CollisionMask[y - 1, x] + dy < CollisionMask[y, x])
                    {
                        p[y, x] = p[y - 1, x];
                        CollisionMask[y, x] = euclidean(x - p[y, x].X, y - p[y, x].Y);
                    }
                    if (CollisionMask[y - 1, x + 1] + dh < CollisionMask[y, x])
                    {
                        p[y, x] = p[y - 1, x + 1];
                        CollisionMask[y, x] = euclidean(x - p[y, x].X, y - p[y, x].Y);
                    }
                    if (CollisionMask[y, x - 1] + dx < CollisionMask[y, x])
                    {
                        p[y, x] = p[y, x - 1];
                        CollisionMask[y, x] = euclidean(x - p[y, x].X, y - p[y, x].Y);
                    }
                }
            }

            //second, and final pass
            for (int y = h - 2; y > 0; --y)
            {
                for (int x = w - 2; x > 0; --x)
                {
                    if (CollisionMask[y, x + 1] + dx < CollisionMask[y, x])
                    {
                        p[y, x] = p[y, x + 1];
                        CollisionMask[y, x] = euclidean(x - p[y, x].X, y - p[y, x].Y);
                    }
                    if (CollisionMask[y + 1, x - 1] + dh < CollisionMask[y, x])
                    {
                        p[y, x] = p[y + 1, x - 1];
                        CollisionMask[y, x] = euclidean(x - p[y, x].X, y - p[y, x].Y);
                    }
                    if (CollisionMask[y + 1, x] + dy < CollisionMask[y, x])
                    {
                        p[y, x] = p[y + 1, x];
                        CollisionMask[y, x] = euclidean(x - p[y, x].X, y - p[y, x].Y);
                    }
                    if (CollisionMask[y + 1, x + 1] + dx < CollisionMask[y, x])
                    {
                        p[y, x] = p[y + 1, x + 1];
                        CollisionMask[y, x] = euclidean(x - p[y, x].X, y - p[y, x].Y);
                    }
                }
            }

            timer.Stop();
            System.Diagnostics.Debug.WriteLine("Generated collision SDF in " + timer.Elapsed);

            //todo: this is being called several times

            
            //save as TGA image for testing
            using (var fs = new System.IO.FileStream("collision.tga", System.IO.FileMode.Create))
            {
                fs.WriteByte(0);
                fs.WriteByte(0);
                fs.WriteByte(3);
                var bytes = new byte[5 + 4];
                fs.Write(bytes, 0, bytes.Length);

                bytes = BitConverter.GetBytes((short)w);
                fs.Write(bytes, 0, bytes.Length);
                bytes = BitConverter.GetBytes((short)h);
                fs.Write(bytes, 0, bytes.Length);

                fs.WriteByte(8); //bpp
                fs.WriteByte(0);

                for (var y = h - 1; y >= 0; --y) //y is flipped
                    for (var x = 0; x < w; ++x)
                        fs.WriteByte(CollisionMask[y, x]); //maybe a hacky way to get a single dimensional array but ¯\_(ツ)_/¯
            }
            
        }

        public virtual Dictionary<string, object> DerivedSerialize()
        {
            return new Dictionary<string, object>
            {
                ["Width"] = Width,
                ["Height"] = Height,
                ["Tiles"] = Tiles,
            };
        }

        public virtual void DerivedDeserialize(Dictionary<string, object> props)
        {
            var tiles = Data.Serializer.Cast<List<short>>(props["Tiles"]);

            int width = 1, height = 1;
            if (props.TryGetValue("Width", out var _width) && _width is long width_)
            {
                width = (int)width_;
                height = tiles.Count / width;
            }
            else if (props.TryGetValue("Height", out var _height) && _height is long height_)
            {
                height = (int)height_;
                width = tiles.Count / height;
            }
            else
                throw new ArgumentException("Maps must have either a Width or Height property (measuring number of tiles)");

            Tiles = new short[height, width];
            Buffer.BlockCopy(tiles.ToArray(), 0, Tiles, 0, width * height * sizeof(short));
            TilesPerRow = (TilesImage != null ? (TilesImage.Width / TileSize) : 0);
            SectorPixelSize = SectorSize * TileSize;
            GenerateCollisionMask(); //todo: necessary here?
        }
    }

    public partial class MapBaseInstance : Data.IDerivedSerialize, Data.IDerivedDeserialize
    {
        /// <summary>
        /// Package the map into an archive
        /// </summary>
        /// <param name="file">The file to package the map into. Typical extension is .d2map</param>
        public void PackageMap(string file)
        {
            System.Diagnostics.Contracts.Contract.Requires(Class != null);

            //write MapClass as map.tk and MapInstance as state.tk to file
            using (var arc = new System.IO.Compression.ZipArchive(System.IO.File.Create(file), System.IO.Compression.ZipArchiveMode.Create, false, System.Text.Encoding.UTF8))
            {
                var entry = arc.CreateEntry("map.tk", System.IO.Compression.CompressionLevel.Optimal);
                using (var stream = new System.IO.StreamWriter(entry.Open(), System.Text.Encoding.UTF8, 4096, false))
                    Data.Serializer.TextSerialize(stream, Class, 0);

                var lastMapFile = Class.File;
                Class.File = "./map.tk";
                entry = arc.CreateEntry("state.tk", System.IO.Compression.CompressionLevel.Optimal);
                using (var stream = new System.IO.StreamWriter(entry.Open(), System.Text.Encoding.UTF8, 4096, false))
                    Data.Serializer.TextSerialize(stream, this, 0);
                Class.File = lastMapFile;
            }
        }

        public static MapBaseInstance FromPackage(string file)
        {
            return Data.Cache.Load<MapBaseInstance>("state.tk", file);
        }

        /// <summary>
        /// Save this map instance as a save state
        /// </summary>
        /// <param name="file">The file to save to</param>
        public void Save(string file, bool setName = true)
        {
            File = null;
            Data.Serializer.TextSerialize(file, this);
            File = file;
        }

        struct EntityAttachment
        {
            [Data.Serializer.AsReference]
            public EntityInstance parent;
            [Data.Serializer.AsReference]
            public EntityInstance child;
        }
        
        public virtual Dictionary<string, object> DerivedSerialize()
        {
            var attachments = new List<EntityAttachment>();
            var triggers = new HashSet<Trigger>();
            var fluids = new List<FluidInstance>(LiveFluids); //todo: optimize

            foreach (var ent in AllEntities)
            {
                //todo: requires non-null name
                //todo: needs to store correct (transformed) position
                if (ent.WorldParent != null)
                    attachments.Add(new EntityAttachment { parent = ent.WorldParent, child = ent });
            }

            foreach (var sector in Sectors)
            {
                triggers.UnionWith(sector.triggers);
                fluids.AddRange(sector.fluids);
            }

            return new Dictionary<string, object>
            {
                ["Entities"] = AllEntities,
                ["EntityAttachments"] = attachments,
                ["Triggers"] = triggers,
                ["Fluids"] = fluids
            };
        }

        public virtual void DerivedDeserialize(Dictionary<string, object> props)
        {
            if (props.TryGetValue("Entities", out var ents))
            {
                foreach (var ent in Data.Serializer.Cast<List<EntityInstance>>(ents))
                    Spawn(ent, false);
            }

            if (props.TryGetValue("EntityAttachments", out var attachments))
            {
                foreach (var attach in Data.Serializer.Cast<List<EntityAttachment>>(attachments))
                {
                    var childPos = attach.child.Position;
                    var childFwd = attach.child.Forward;
                    Attach(attach.parent, attach.child);
                    attach.child.Position = childPos; //ghetto (send RealPosition through ent.DerivedSerialize instead?)
                    attach.child.Forward = childFwd; //ditto
                }
            }

            if (props.TryGetValue("Fluids", out var fluids)) //todo: load from class?
            {
                foreach (var fluid in Data.Serializer.Cast<List<FluidInstance>>(fluids))
                    Spawn(fluid);
            }


            if (props.TryGetValue("Triggers", out var triggers)) //todo: load from class?
            {
                foreach (var trigger in Data.Serializer.Cast<List<Trigger>>(triggers))
                    AddTrigger(trigger);
            }
            
            Class?.InitializeGraphics(); //todo: this is a hack fix
            Resize(Class.Width, Class.Height); //builds spacial info, etc
        }
    }
}
