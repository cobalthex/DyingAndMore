using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore.Editor.Selectors
{
    class TileSelector : Selector
    {
        public TileSelector(Editor Editor) : base(Editor) { }

        public override void Load()
        {
            base.Load();
            var tileSz = editor.Map.TileSize;
            ItemSize = new Point(tileSz);
            ItemCount = editor.Map.TilesPerRow * (editor.Map.TilesImage.Height / tileSz);
        }

        public override void DrawItem(GameTime Time, int ItemIndex, Rectangle Bounds, SpriteBatch Sbatch = null)
        {
            var clip = new Rectangle((ItemIndex % editor.Map.TilesPerRow) * ItemSize.X, (ItemIndex / editor.Map.TilesPerRow) * ItemSize.Y, ItemSize.X, ItemSize.Y);
            (Sbatch ?? sbatch).Draw(editor.Map.TilesImage, Bounds, clip, Color.White);
        }
    }
}
