using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.Graphics;

namespace DyingAndMore.Editor
{
    partial class Editor : Takai.GameState.GameState
    {
        bool isPosSaved = false;
        Vector2 savedWorldPos;

        void UpdateTilesMode(GameTime Time)
        {
            
            if (InputState.IsPress(Keys.LeftControl) || InputState.IsPress(Keys.RightControl))
            {
                isPosSaved = true;
                savedWorldPos = currentWorldPos;
            }
            else if (InputState.IsClick(Keys.LeftControl) || InputState.IsClick(Keys.RightControl))
                isPosSaved = false;

            var tile = short.MinValue;

            if (InputState.IsButtonDown(MouseButtons.Left))
                tile = (short)selectors[0].SelectedItem;
            else if (InputState.IsButtonDown(MouseButtons.Right))
                tile = -1;

            if (tile > short.MinValue)
            {
                if (isPosSaved)
                {
                    //draw rect
                    if (InputState.IsMod(KeyMod.Shift))
                    {
                        var start = (savedWorldPos / map.TileSize).ToPoint();
                        var end = (currentWorldPos / map.TileSize).ToPoint();

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

                        var bounds = new Rectangle(0, 0, map.Width, map.Height);

                        for (var y = start.Y; y <= end.Y; ++y)
                        {
                            for (var x = start.X; x <= end.X; ++x)
                            {
                                if (bounds.Contains(x, y))
                                    map.Tiles[y, x] = tile;
                            }
                        }
                    }
                    else
                        TileLine(savedWorldPos, currentWorldPos, tile);

                    savedWorldPos = currentWorldPos;
                }
                else if (InputState.IsMod(KeyMod.Shift))
                    TileFill(currentWorldPos, tile);
                else if (InputState.IsButtonHeld(tile == -1 ? MouseButtons.Right : MouseButtons.Left)) //todo: improve
                    TileLine(lastWorldPos, currentWorldPos, tile);
            }
        }

        void DrawTilesMode()
        {
            //draw rect around tile under cursor
            if (map.IsInside(lastWorldPos))
            {
                MapLineRect
                (
                    new Rectangle
                    (
                        (int)(lastWorldPos.X / map.TileSize) * map.TileSize,
                        (int)(lastWorldPos.Y / map.TileSize) * map.TileSize,
                        map.TileSize, map.TileSize
                    ),
                    highlightColor
                );
            }

            if (isPosSaved)
            {
                var diff = lastWorldPos - savedWorldPos;

                if (InputState.IsMod(KeyMod.Shift))
                {
                    MapLineRect(new Rectangle(savedWorldPos.ToPoint(), diff.ToPoint()), Color.GreenYellow);

                    var w = (System.Math.Abs((int)diff.X) - 1) / map.TileSize + 1;
                    var h = (System.Math.Abs((int)diff.Y) - 1) / map.TileSize + 1;
                    tinyFont.Draw(sbatch, $"w:{w}, h:{w}", camera.WorldToScreen(lastWorldPos) + new Vector2(10, -10), Color.White);
                }
                else
                {
                    var angle = (int)MathHelper.ToDegrees((float)System.Math.Atan2(-diff.Y, diff.X));
                    if (angle < 0)
                        angle += 360;
                    map.DrawLine(savedWorldPos, lastWorldPos, Color.GreenYellow);

                    diff /= map.TileSize;
                    tinyFont.Draw(sbatch, $"x:{System.Math.Ceiling(diff.X)} y:{System.Math.Ceiling(diff.Y)} deg:{angle}",
                        camera.WorldToScreen(lastWorldPos) + new Vector2(10, -10), Color.White);
                }
            }
        }

        //Tile a line, start and end in world coords
        void TileLine(Vector2 Start, Vector2 End, short TileValue)
        {
            var start = new Vector2((int)Start.X / map.TileSize, (int)Start.Y / map.TileSize).ToPoint();
            var end = new Vector2((int)End.X / map.TileSize, (int)End.Y / map.TileSize).ToPoint();

            var diff = end - start;
            var sx = diff.X > 0 ? 1 : -1;
            var sy = diff.Y > 0 ? 1 : -1;
            diff.X = System.Math.Abs(diff.X);
            diff.Y = System.Math.Abs(diff.Y);

            var err = (diff.X > diff.Y ? diff.X : -diff.Y) / 2;

            var bounds = new Rectangle(0, 0, map.Width, map.Height);
            while (true)
            {
                if (bounds.Contains(start))
                    map.Tiles[start.Y, start.X] = TileValue;

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
        }

        void TileFill(Vector2 Position, short TileValue)
        {
            if (!map.IsInside(Position))
                return;

            var initial = (Position / map.TileSize).ToPoint();
            var initialValue = map.Tiles[initial.Y, initial.X];

            if (initialValue == TileValue)
                return;

            var queue = new System.Collections.Generic.Queue<Point>();
            queue.Enqueue(initial);

            while (queue.Count > 0)
            {
                var first = queue.Dequeue();

                var left = first.X;
                var right = first.X;
                for (; left > 0 && map.Tiles[first.Y, left - 1] == initialValue; left--) ;
                for (; right < map.Width - 1 && map.Tiles[first.Y, right + 1] == initialValue; right++) ;

                for (; left <= right; left++)
                {
                    map.Tiles[first.Y, left] = TileValue;

                    if (first.Y > 0 && map.Tiles[first.Y - 1, left] == initialValue)
                        queue.Enqueue(new Point(left, first.Y - 1));

                    if (first.Y < map.Height - 1 && map.Tiles[first.Y + 1, left] == initialValue)
                        queue.Enqueue(new Point(left, first.Y + 1));
                }
            }
        }

    }
}