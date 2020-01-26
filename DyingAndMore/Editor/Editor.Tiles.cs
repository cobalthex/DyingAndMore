using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor
{
    class TilesEditorMode : SelectorEditorMode<Selectors.TileSelector>
    {
        bool isPosSaved = false;
        Vector2 savedWorldPos, lastWorldPos, currentWorldPos;

        public TilesEditorMode(Editor editor)
            : base("Tiles", editor, new Selectors.TileSelector(editor.Map.Class.Tileset))
        {
        }

        protected override void UpdatePreview(int selectedItem)
        {
            if (preview.Sprite == null)
                return;

            preview.Sprite.ClipRect = new Rectangle(
                (selector.SelectedIndex % editor.Map.Class.TilesPerRow) * editor.Map.Class.TileSize,
                (selector.SelectedIndex / editor.Map.Class.TilesPerRow) * editor.Map.Class.TileSize,
                editor.Map.Class.TileSize,
                editor.Map.Class.TileSize
            );
        }
        protected override bool HandleInput(GameTime time)
        {
            lastWorldPos = currentWorldPos;
            currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);

            if (InputState.IsPress(Keys.LeftControl) || InputState.IsPress(Keys.RightControl))
            {
                isPosSaved = true;
                savedWorldPos = currentWorldPos;
            }
            else if (InputState.IsButtonUp(Keys.LeftControl) && InputState.IsButtonUp(Keys.RightControl))
                isPosSaved = false;

            var tile = short.MinValue;

            if (DidPressInside(MouseButtons.Left))
                tile = InputState.IsMod(KeyMod.Alt) ? (short)-1 : (short)selector.SelectedIndex;
            else if (DidPressInside(MouseButtons.Right))
                tile = -1;

            if (tile > short.MinValue)
            {
                var curTile = Takai.Util.Ceiling(currentWorldPos / editor.Map.Class.TileSize).ToPoint();
                if (isPosSaved)
                {
                    var savedTile = (savedWorldPos / editor.Map.Class.TileSize).ToPoint();
                    //draw rect
                    if (InputState.IsMod(KeyMod.Shift))
                        TileRect(savedTile, curTile, tile);
                    else
                        TileLine(savedTile, curTile, tile);

                    savedWorldPos = currentWorldPos;
                }

                //todo: fix when coming out of selector
                else if (InputState.IsMod(KeyMod.Shift))
                    TileFlood(curTile - new Point(1), tile);
                else
                    TileLine((lastWorldPos / editor.Map.Class.TileSize).ToPoint(), curTile - new Point(1), tile);

                return false;
            }

            return base.HandleInput(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            //draw rect around tile under cursor
            if (editor.Map.Class.Bounds.Contains(lastWorldPos))
            {
                editor.Map.DrawRect
                (
                    new Rectangle
                    (
                        (int)(lastWorldPos.X / editor.Map.Class.TileSize) * editor.Map.Class.TileSize,
                        (int)(lastWorldPos.Y / editor.Map.Class.TileSize) * editor.Map.Class.TileSize,
                        editor.Map.Class.TileSize, editor.Map.Class.TileSize
                    ),
                    Color.Orange
                );
            }

            if (isPosSaved)
            {
                var diff = lastWorldPos - savedWorldPos;

                if (InputState.IsMod(KeyMod.Shift))
                {
                    //todo: snap to tiles
                    editor.Map.DrawRect(new Rectangle((int)savedWorldPos.X, (int)savedWorldPos.Y, (int)diff.X, (int)diff.Y), Color.GreenYellow);

                    var w = (System.Math.Abs((int)diff.X) - 1) / editor.Map.Class.TileSize + 1;
                    var h = (System.Math.Abs((int)diff.Y) - 1) / editor.Map.Class.TileSize + 1;
                    Font.Draw(spriteBatch, $"w:{w}, h:{w}", editor.Camera.WorldToScreen(lastWorldPos) + new Vector2(10, -10), Color.White);
                }
                else
                {
                    var angle = (int)MathHelper.ToDegrees((float)System.Math.Atan2(-diff.Y, diff.X));
                    if (angle < 0)
                        angle += 360;
                    editor.Map.DrawLine(savedWorldPos, lastWorldPos, Color.GreenYellow);

                    diff /= editor.Map.Class.TileSize;
                    Font.Draw(spriteBatch, $"x:{System.Math.Ceiling(diff.X)} y:{System.Math.Ceiling(diff.Y)} deg:{angle}",
                        editor.Camera.WorldToScreen(lastWorldPos) + new Vector2(10, -10), Color.White);
                }
            }
        }

        void TileRect(Point start, Point end, short value)
        {
            //use Util.AbsRectangle
            if (start.X > end.X)
            {
                var tmp = start.X;
                start.X = end.X;
                end.X = tmp;
            }
            if (start.Y > end.Y)
            {
                var tmp = start.Y;
                start.Y = end.Y;
                end.Y = tmp;
            }

            var bounds = new Rectangle(0, 0, editor.Map.Class.Width, editor.Map.Class.Height);
            for (var y = start.Y; y < end.Y; ++y)
            {
                for (var x = start.X; x < end.X; ++x)
                {
                    if (bounds.Contains(x, y))
                        editor.Map.Class.Tiles[y, x] = value;
                }
            }

            //todo: first row/col dont render

            editor.Map.Class.PatchTileLayoutTexture(new Rectangle(start, end - start));
        }

        void TileLine(Point start, Point end, short value)
        {
            var diff = end - start;
            var sx = diff.X > 0 ? 1 : -1;
            var sy = diff.Y > 0 ? 1 : -1;
            diff.X = System.Math.Abs(diff.X);
            diff.Y = System.Math.Abs(diff.Y);

            var err = (diff.X > diff.Y ? diff.X : -diff.Y) / 2;

            var bounds = editor.Map.Class.TileBounds;
            while (true)
            {
                if (bounds.Contains(start))
                    editor.Map.Class.Tiles[start.Y, start.X] = value;

                if (start.X == end.X && start.Y == end.Y)
                    break;

                var e2 = err;
                if (e2 > -diff.X)
                {
                    err -= diff.Y;
                    start.X += sx;
                }
                if (e2 < diff.Y)
                {
                    err += diff.X;
                    start.Y += sy;
                }
            }

            if (start.X < end.X)
            {
                var s = start.X;
                start.X = end.X;
                end.X = s;
            }
            if (start.Y < end.Y)
            {
                var s = start.Y;
                start.Y = end.Y;
                end.Y = s;
            }
            editor.Map.Class.PatchTileLayoutTexture(new Rectangle(start, Takai.Util.Max(new Point(1), end - start)));
        }

        void TileFlood(Point tile, short value)
        {
            if (!editor.Map.Class.TileBounds.Contains(tile))
                return;

            var initialValue = editor.Map.Class.Tiles[tile.Y, tile.X];
            if (initialValue == value)
                return;

            Point min = editor.Map.Class.TileBounds.Size;
            Point max = new Point(0);

            var queue = new System.Collections.Generic.Queue<Point>();
            queue.Enqueue(tile);
            while (queue.Count > 0)
            {
                var first = queue.Dequeue();

                var left = first.X;
                var right = first.X;
                for (; left > 0 && editor.Map.Class.Tiles[first.Y, left - 1] == initialValue; --left) ;
                for (; right < editor.Map.Class.Width - 1 && editor.Map.Class.Tiles[first.Y, right + 1] == initialValue; ++right) ;

                for (; left <= right; ++left)
                {
                    editor.Map.Class.Tiles[first.Y, left] = value;

                    if (first.Y > 0 && editor.Map.Class.Tiles[first.Y - 1, left] == initialValue)
                        queue.Enqueue(new Point(left, first.Y - 1));

                    if (first.Y < editor.Map.Class.Height - 1 && editor.Map.Class.Tiles[first.Y + 1, left] == initialValue)
                        queue.Enqueue(new Point(left, first.Y + 1));
                }

                min.X = System.Math.Min(min.X, left);
                min.Y = System.Math.Min(min.Y, first.Y);
                max.X = System.Math.Max(max.X, right);
                max.Y = System.Math.Max(max.Y, first.Y);
            }
            //editor.Map.Class.PatchTileLayoutTexture(new Rectangle(min, Takai.Util.Max(new Point(1), max - min)));
            editor.Map.Class.PatchTileLayoutTexture(editor.Map.Class.TileBounds);
        }
    }
}