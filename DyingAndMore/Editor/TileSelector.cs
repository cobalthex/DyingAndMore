using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class TileSelector : Selector
    {
        public TileSelector(Editor Editor) : base(Editor) { }

        public override void Load()
        {
            base.Load();
            var tileSz = editor.map.TileSize;
            ItemSize = new Point(tileSz);
            ItemCount = editor.map.TilesPerRow * (editor.map.TilesImage.Height / tileSz);
        }

        public override void DrawItem(GameTime Time, int ItemIndex, Rectangle Bounds, SpriteBatch Sbatch = null)
        {
            var clip = new Rectangle((ItemIndex % editor.map.TilesPerRow) * ItemSize.X, (ItemIndex / editor.map.TilesPerRow) * ItemSize.Y, ItemSize.X, ItemSize.Y);
            (Sbatch ?? sbatch).Draw(editor.map.TilesImage, Bounds, clip, Color.White);
        }
    }
}
