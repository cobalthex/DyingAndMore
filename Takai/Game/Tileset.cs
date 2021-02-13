using Microsoft.Xna.Framework.Graphics;

namespace Takai.Game
{
    /// <summary>
    /// An intermediate struct encapsulating tileset properties
    /// </summary>
    public struct Tileset : Data.ISerializeExternally
    {
        [Data.Serializer.Ignored]
        public string File { get; set; }

        /// <summary>
        /// the tiles texture
        /// </summary>
        public Texture2D texture; //sprite?
        /// <summary>
        /// individual tile size (width & height)
        /// </summary>
        public int size;
        /// <summary>
        /// what type of material the tiles are represented as (for effects/collision detection)
        /// </summary>
        public string material;

        [Data.Serializer.Ignored]
        public int TilesPerRow { get; internal set; }

        internal void CalculateTilesPerRow()
        {
            TilesPerRow = texture != null ? texture.Width / size : 1;
        }

        public Tileset(Texture2D texture, int size, string material = "Tiles")
        {
            this.File = null;
            this.texture = texture;
            this.size = size;
            this.material = material;
            this.TilesPerRow = 1;
            CalculateTilesPerRow();
        }
    }
}
