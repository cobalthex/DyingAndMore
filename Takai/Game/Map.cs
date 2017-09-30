using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    public partial class MapClass : IObjectClass<MapInstance>
    {
        [Data.Serializer.Ignored]
        public string File { get; set; }

        public string Name { get; set; }

        //todo: move ⬇ to graphics code

        /// <summary>
        /// The collision mask for the tilemap
        /// </summary>
        [Data.Serializer.Ignored]
        public System.Collections.BitArray CollisionMask { get; set; }

        /// <summary>
        /// All of the tiles in the map (y,x)
        /// </summary>
        [Data.Serializer.Ignored]
        public short[,] Tiles { get; set; } = new short[0, 0];

        /// <summary>
        /// The Width and height of each tile in the map
        /// </summary>
        public int TileSize
        {
            get { return _tileSize; }
            set
            {
                _tileSize = value;
                TilesPerRow = (TilesImage != null ? (TilesImage.Width / value) : 0);
                SectorPixelSize = SectorSize * value;
            }
        }
        private int _tileSize = 1;
        [Data.Serializer.Ignored]
        public int TilesPerRow { get; private set; } = 1;

        /// <summary>
        /// The horizontal size of the map in tiles
        /// </summary>
        [Data.Serializer.Ignored]
        public int Width => Tiles.GetLength(1);
        /// <summary>
        /// The vertical size of the map in tiles
        /// </summary>
        [Data.Serializer.Ignored]
        public int Height => Tiles.GetLength(0);

        public const int SectorSize = 4; //The number of tiles that a map sector spans
        [Data.Serializer.Ignored]
        public int SectorPixelSize { get; private set; } = 0;

        /// <summary>
        /// The bounds of the map in pixels
        /// </summary>
        [Data.Serializer.Ignored]
        public Rectangle Bounds
        {
            get { return new Rectangle(0, 0, Width * TileSize, Height * TileSize); }
        }

        public MapInstance Create()
        {
            throw new System.NotImplementedException();
        }

    }

    public class MapInstance : IObjectInstance<MapClass>
    {
        public MapClass Class { get; set; }

        /// <summary>
        /// Grid based spacial storage for objects in the map
        /// </summary>
        /// <remarks>Size is Height,Width</remarks>
        [Data.Serializer.Ignored]
        public MapSector[,] Sectors { get; protected set; } = new MapSector[0, 0];
    }
}