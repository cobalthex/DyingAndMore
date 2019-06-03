using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore.Editor.Selectors
{
    class TileSelector : Selector
    {
        readonly Takai.Game.Tileset tileset;
        readonly int tilesPerRow;

        public TileSelector() { }

        public TileSelector(Takai.Game.Tileset tileset)
        {
            this.tileset = tileset;

            ItemSize = new Vector2(tileset.size);
            tilesPerRow = tileset.TilesPerRow;

            if (tileset.texture != null)
                ItemCount = tileset.TilesPerRow * (tileset.texture.Height / tileset.size);
        }

        public override void DrawItem(SpriteBatch spriteBatch, int itemIndex, Rectangle bounds)
        {
            var clip = new Rectangle(
                (int)((itemIndex % tilesPerRow) * ItemSize.X),
                (int)((itemIndex / tilesPerRow) * ItemSize.Y),
                (int)ItemSize.X,
                (int)ItemSize.Y
            );
            spriteBatch.Draw(tileset.texture, bounds, clip, Color.White);
        }
    }
}
