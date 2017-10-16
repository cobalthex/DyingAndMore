using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Game
{
    public partial class MapClass : Data.IDerivedSerialize, Data.IDerivedDeserialize
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

        public Dictionary<string, object> DerivedSerialize()
        {
            return new Dictionary<string, object>
            {
                ["Tiles"] = Tiles
            };
        }

        public void DerivedDeserialize(Dictionary<string, object> props)
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

            BuildHeuristic(new Point(3)); //todo: proper heuristic
        }
    }

    public partial class MapInstance : Data.IDerivedSerialize, Data.IDerivedDeserialize
    {
        /// <summary>
        /// Save this map instance as a save state
        /// </summary>
        /// <param name="file">The file to save to</param>
        public void Save(string file)
        {
            Data.Serializer.TextSerialize(file, this);
        }

        InitialMapState CreateInitialState()
        {
            return new InitialMapState
            {
                Entities = Enumerable.Select(AllEntities, e => new InitialMapState.EntitySpawn()
                {
                    Class = e.Class,
                    Position = e.Position,
                    Forward = e.Forward,
                    Name = e.Name
                }).ToList(),
                Fluids = new List<FluidInstance>(),
                Decals = new List<Decal>()
            };
        }

        /// <summary>
        /// Save this instance as a new map, setting the default state of the class to this
        /// </summary>
        /// <param name="file">The file to save to</param>
        public void SaveAsMap(string file)
        {
            //create default state
            Class.InitialState = CreateInitialState();
            Class.File = null;

            //todo: less hacky (make class ReadOnly and custom serialize?
            var cls = Class;
            _class = null;
            Data.Serializer.TextSerialize(file, cls);
            _class = cls;
            Class.File = file;
        }

        public Dictionary<string, object> DerivedSerialize()
        {
            return new Dictionary<string, object>
            {
                ["Entities"] = AllEntities
            };
        }

        public void DerivedDeserialize(Dictionary<string, object> props)
        {
            if (props.TryGetValue("Entities", out var ents))
            {
                foreach (var ent in Data.Serializer.Cast<List<EntityInstance>>(ents))
                    Spawn(ent);
            }

            if (Class?.InitialState != null)
            {
                foreach (var decal in Class.InitialState.Decals)
                    AddDecal(decal);
            }

            //fluids should be serialized? (maybe only some fluids are serialized)
            //decals load from initial state
            //triggers load from class

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
