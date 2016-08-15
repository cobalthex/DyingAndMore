using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore
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

        public override void Update(GameTime Time)
        {
            base.Update(Time);
            
            var mouse = InputCatalog.MouseState.Position;

            if (InputCatalog.IsMousePress(InputCatalog.MouseButton.Left))
            {
                mouse -= Start.ToPoint();

                //var tileSz = editor.map.TileSize + tileMargin;
                //var columns = (width - tileMargin) / tileSz;
                //editor.selectedTile = (short)((mouse.X / tileSz) + ((mouse.Y / tileSz) * columns));
            }
        }

        public override void DrawItem(GameTime Time, int ItemIndex, Rectangle Bounds)
        {
            var clip = new Rectangle((ItemIndex % editor.map.TilesPerRow) * ItemSize.X, (ItemIndex / editor.map.TilesPerRow) * ItemSize.Y, ItemSize.X, ItemSize.Y);
            sbatch.Draw(editor.map.TilesImage, Bounds, clip, Color.White);
        }
    }
}
