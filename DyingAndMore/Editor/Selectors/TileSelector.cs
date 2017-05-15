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
            var tileSz = editor.Map.TileSize;
            ItemSize = new Point(tileSz);
            ItemCount = editor.Map.TilesPerRow * (editor.Map.TilesImage.Height / tileSz);
        }

        public override void DrawItem(SpriteBatch spriteBatch, int itemIndex, Rectangle bounds)
        {
            var clip = new Rectangle(
                (itemIndex % editor.Map.TilesPerRow) * ItemSize.X,
                (itemIndex / editor.Map.TilesPerRow) * ItemSize.Y,
                ItemSize.X,
                ItemSize.Y
            );
            spriteBatch.Draw(editor.Map.TilesImage, bounds, clip, Color.White);
        }
    }
}
