using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using SDF = Takai.Graphics.SignedDistanceField;

namespace Takai.Game
{
    public partial class MapBaseClass : Data.IDerivedSerialize, Data.IDerivedDeserialize
    {
        //test if inside the map
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte TileValueAt(int x, int y, Color[] tilemap) //x and y here scaled down
        {
            var tile = Tiles[y / TileSize, x / TileSize];
            if (tile < 0)
                return 0;

            return tilemap[
                ((tile / Tileset.TilesPerRow) * TileSize + (y % TileSize)) * Tileset.texture.Width +
                ((tile % Tileset.TilesPerRow) * TileSize + (x % TileSize))
            ].A;
        }

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

            CollisionMaskSize = new Point(w, h);
            if (w <= 0 || h <= 0)
            {
                CollisionMask = new byte[0];
                return;
            }
            
            //1px border around map is considered unpathable

            //do each individually for better cache perf

            CollisionMask = new byte[h * w];
            for (int y = 0; y < h - 1; ++y)
                for (int x = 0; x < w - 1; ++x)
                    CollisionMask[y * w + x] = byte.MaxValue;

            var p = new Point[h * w];
            for (int y = 1; y < h - 1; ++y)
                for (int x = 1; x < w - 1; ++x)
                    p[y * w + x] = new Point(-1);

            Color[] tilemap = new Color[Tileset.texture.Width * Tileset.texture.Height];
            Tileset.texture.GetData(tilemap);

            //can skip bounds checking by using padded array, but requires copying array at end

            //find edges
            //can be parallelized
            const byte threshold = 127;
            for (int y = 0; y < h - 1; ++y)
            {
                var ys = y << scale;
                for (int x = 0; x < w - 1; ++x)
                {
                    var xs = x << scale;
                    byte cur = TileValueAt(xs, ys, tilemap);
                    //from 'Fast Edge Detection Algorithm for Embedded Systems'
                    bool isEdge = 
                        (cur - TileValueAt((x + 1) << scale, ys, tilemap) > threshold) || 
                        (cur - TileValueAt(xs, (y + 1) << scale, tilemap) > threshold);
                    if (cur < threshold || isEdge)
                    {
                        CollisionMask[y * w + x] = 0;
                        p[y * w + x] = new Point(x, y);
                    }

                }
            }
            System.Diagnostics.Debug.WriteLine("Edge pass SDF in " + timer.Elapsed);
            
            //first pass
            for (int y = 1; y < h - 1; ++y)
            {
                for (int x = 1; x < w - 1; ++x)
                {
                    var cc = y * w + x;

                    var cp = (y - 1) * w + (x - 1);
                    if (CollisionMask[cp] + SDF.DHyp < CollisionMask[cc])
                    {
                        p[cc] = p[cp];
                        CollisionMask[cc] = SDF.Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                    cp = (y - 1) * w + x;
                    if (CollisionMask[cp] + SDF.DY < CollisionMask[cc])
                    {
                        p[cc] = p[cp];
                        CollisionMask[cc] = SDF.Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                    cp = (y - 1) * w + (x + 1);
                    if (CollisionMask[cp] + SDF.DHyp < CollisionMask[cc])
                    {
                        p[cc] = p[cp];
                        CollisionMask[cc] = SDF.Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                    cp = y * w + (x - 1);
                    if (CollisionMask[cp] + SDF.DX < CollisionMask[cc])
                    {
                        p[cc] = p[cp];
                        CollisionMask[cc] = SDF.Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                }
            }

            //second, and final pass
            for (int y = h - 2; y > 0; --y)
            {
                for (int x = w - 2; x > 0; --x)
                {
                    var cc = y * w + x;

                    var cp = y * w + (x + 1);
                    if (CollisionMask[cp] + SDF.DX < CollisionMask[cc])
                    {
                        p[cc] = p[cp];
                        CollisionMask[cc] = SDF.Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                    cp = (y + 1) * w + (x - 1);
                    if (CollisionMask[cp] + SDF.DHyp < CollisionMask[cc])
                    {
                        p[cc] = p[cp];
                        CollisionMask[cc] = SDF.Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                    cp = (y + 1) * w + x;
                    if (CollisionMask[cp] + SDF.DY < CollisionMask[cc])
                    {
                        p[cc] = p[cp];
                        CollisionMask[cc] = SDF.Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                    cp = (y + 1) * w + (x + 1);
                    if (CollisionMask[cp] + SDF.DX < CollisionMask[cc])
                    {
                        p[cc] = p[cp];
                        CollisionMask[cc] = SDF.Euclidean(x - p[cc].X, y - p[cc].Y);
                    }
                }
            }

            timer.Stop();
            System.Diagnostics.Debug.WriteLine("Generated collision SDF in " + timer.Elapsed);

            if (collisionMaskSDF == null || collisionMaskSDF.Width < w || collisionMaskSDF.Height < h)
            {
                collisionMaskSDF?.Dispose();
                collisionMaskSDF = new Microsoft.Xna.Framework.Graphics.Texture2D(
                    Runtime.GraphicsDevice,
                    Util.NextPowerOf2(w),
                    Util.NextPowerOf2(h),
                    false,
                    Microsoft.Xna.Framework.Graphics.SurfaceFormat.Alpha8
                );
            }
            collisionMaskSDF.SetData(0, new Rectangle(0, 0, w, h), CollisionMask, 0, CollisionMask.Length);

#if DEBUG
            //save as TGA image for testing
            using (var fs = new System.IO.FileStream("collision.tga", System.IO.FileMode.Create))
                SDF.SaveToTGA(CollisionMask, (short)w, (short)h, fs);
#endif
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
            SectorPixelSize = SectorSize * TileSize;
            GenerateCollisionMask(); //todo: necessary here?
            //todo: this is being called several times
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
            Class?.InitializeGraphics();
            Resize(Class.Width, Class.Height, true); //builds spacial info, etc

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
        }
    }
}
