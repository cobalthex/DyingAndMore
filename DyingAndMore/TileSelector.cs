using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore
{
    class TileSelector : Selector
    {
        const int tileMargin = 2;

        public TileSelector(Editor Editor) : base(Editor) { }

        public override void Load()
        {
            base.Load();
            width = tileMargin + (6 * (editor.map.TileSize + tileMargin));
        }

        public override void Update(GameTime Time)
        {
            base.Update(Time);
            
            var mouse = InputCatalog.MouseState.Position;

            if (InputCatalog.IsMousePress(InputCatalog.MouseButton.Left))
            {
                mouse -= Start.ToPoint();

                var tileSz = editor.map.TileSize + tileMargin;
                var columns = (width - tileMargin) / tileSz;
                editor.selectedTile = (short)((mouse.X / tileSz) + ((mouse.Y / tileSz) * columns));
            }
        }

        public override void DrawContents(GameTime Time, Vector2 Position)
        {
            var tileSz = editor.map.TileSize + tileMargin;
            var srcRows = editor.map.TilesImage.Height / editor.map.TileSize;
            var columns = (width - tileMargin) / tileSz;
            int n = 0;

            if (columns > 0)
            {
                for (int y = 0; y < srcRows; y++)
                {
                    for (int x = 0; x < editor.map.TilesPerRow; x++)
                    {
                        var tilePos = Position + new Vector2(tileMargin + (tileSz * (n % columns)), tileMargin + (tileSz * (n / columns)));

                        sbatch.Draw
                        (
                            editor.map.TilesImage,
                            tilePos,
                            new Rectangle(x * editor.map.TileSize, y * editor.map.TileSize, editor.map.TileSize, editor.map.TileSize),
                            Color.White
                        );

                        if (editor.selectedTile == n)
                        {
                            var rct = new Rectangle((int)tilePos.X, (int)tilePos.Y, editor.map.TileSize, editor.map.TileSize);
                            Takai.Graphics.Primitives2D.DrawRect(sbatch, new Color(1, 1, 1, 0.5f), rct);
                            rct.Inflate(1, 1);
                            Takai.Graphics.Primitives2D.DrawRect(sbatch, Color.Black, rct);
                        }

                        n++;
                    }
                }
            }
        }
    }
}
