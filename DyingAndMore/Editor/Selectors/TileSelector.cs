using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore.Editor.Selectors
{
    class TileSelector : Selector
    {
        public TileSelector(Editor editor)
            : base(editor)
        {
            var tileSz = editor.Map.Class.TileSize;
            ItemSize = new Point(tileSz, tileSz);
            ItemCount = editor.Map.Class.TilesPerRow * (editor.Map.Class.TilesImage.Height / tileSz);
        }

        public override void DrawItem(SpriteBatch spriteBatch, int itemIndex, Rectangle bounds)
        {
            var clip = new Rectangle(
                (itemIndex % editor.Map.Class.TilesPerRow) * ItemSize.X,
                (itemIndex / editor.Map.Class.TilesPerRow) * ItemSize.Y,
                ItemSize.X,
                ItemSize.Y
            );
            spriteBatch.Draw(editor.Map.Class.TilesImage, bounds, clip, Color.White);
        }
    }
}
