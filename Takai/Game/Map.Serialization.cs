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
        /// Build the tiles mask
        /// </summary>
        /// <param name="texture">The source texture to use</param>
        /// <param name="UseAlpha">Use alpha instead of color value</param>
        /// <remarks>Values with luma/alpha < 0.5 are off and all others are on</remarks>
        public void BuildTileMask(Texture2D texture, bool UseAlpha = true)
        {
            Color[] pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);

            CollisionMask = new System.Collections.BitArray(texture.Width * texture.Height);
            for (var y = 0; y < texture.Height; ++y)
            {
                for (var x = 0; x < texture.Width; ++x)
                {
                    var i = x + (y) * texture.Width; //flip Y
                    if (UseAlpha)
                        CollisionMask[x + y * texture.Width] = pixels[i].A > 127;
                    else
                        CollisionMask[x + y * texture.Width] = (pixels[i].R + pixels[i].G + pixels[i].B) / 3 > 127;
                }
            }

            /*
            //save as tga
            using (var fs = new FileStream("collision.tga", FileMode.Create))
            {
                fs.WriteByte(0);
                fs.WriteByte(0);
                fs.WriteByte(3);
                var bytes = new byte[5 + 4];
                fs.Write(bytes, 0, bytes.Length);

                bytes = BitConverter.GetBytes((short)texture.Width);
                fs.Write(bytes, 0, bytes.Length);
                bytes = BitConverter.GetBytes((short)texture.Height);
                fs.Write(bytes, 0, bytes.Length);

                fs.WriteByte(8); //bpp
                fs.WriteByte(0);

                //bytes = new byte[CollisionMask.Length / 8 + (CollisionMask.Length % 8 == 0 ? 0 : 1)];
                //CollisionMask.CopyTo(bytes, 0);
                //fs.Write(bytes, 0, bytes.Length);

                for (var y = 0; y < texture.Height; ++y)
                {
                    for (var x = 0; x < texture.Width; ++x)
                    {
                        fs.WriteByte(CollisionMask[x + y * texture.Width] ? (byte)255 : (byte)0);
                    }
                }
            }
            */
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

            BuildTileMask(TilesImage, true);
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
        
        public virtual Dictionary<string, object> DerivedSerialize()
        {
            var triggers = new HashSet<Trigger>();
            var fluids = new List<FluidInstance>(LiveFluids); //todo: optimize

            foreach (var sector in Sectors)
            {
                triggers.UnionWith(sector.triggers);
                fluids.AddRange(sector.fluids);
            }

            return new Dictionary<string, object>
            {
                ["Entities"] = AllEntities,
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
