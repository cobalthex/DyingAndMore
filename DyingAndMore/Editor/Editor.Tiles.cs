using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.Graphics;

namespace DyingAndMore.Editor
{
    class TilesEditorMode : EditorMode
    {
        bool isPosSaved = false;
        Vector2 savedWorldPos, lastWorldPos;

        Selectors.TileSelector selector;

        public TilesEditorMode(Editor editor)
            : base("Tiles", editor)
        {
            selector = new Selectors.TileSelector(editor);
            selector.Load();
        }

        public override void OpenConfigurator(bool DidClickOpen)
        {
            selector.DidClickOpen = DidClickOpen;
            Takai.Runtime.GameManager.PushState(selector);
        }
        
        public override void Update(GameTime time)
        {
            var currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);

            if (InputState.IsPress(Keys.LeftControl) || InputState.IsPress(Keys.RightControl))
            {
                isPosSaved = true;
                savedWorldPos = currentWorldPos;
            }
            else if (InputState.IsClick(Keys.LeftControl) || InputState.IsClick(Keys.RightControl))
                isPosSaved = false;

            var tile = short.MinValue;

            if (InputState.IsButtonDown(MouseButtons.Left))
                tile = (short)selector.SelectedItem;
            else if (InputState.IsButtonDown(MouseButtons.Right))
                tile = -1;

            if (tile > short.MinValue)
            {
                if (isPosSaved)
                {
                    //draw rect
                    if (InputState.IsMod(KeyMod.Shift))
                    {
                        var start = (savedWorldPos / editor.Map.TileSize).ToPoint();
                        var end = (currentWorldPos / editor.Map.TileSize).ToPoint();

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

                        var bounds = new Rectangle(0, 0, editor.Map.Width, editor.Map.Height);
                        for (var y = start.Y; y <= end.Y; ++y)
                        {
                            for (var x = start.X; x <= end.X; ++x)
                            {
                                if (bounds.Contains(x, y))
                                    editor.Map.Tiles[y, x] = tile;
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

            lastWorldPos = currentWorldPos;
        }

        public override void Draw(SpriteBatch sbatch)
        {
            //draw rect around tile under cursor
            if (editor.Map.IsInside(lastWorldPos))
            {
                editor.Map.DrawRect
                (
                    new Rectangle
                    (
                        (int)(lastWorldPos.X / editor.Map.TileSize) * editor.Map.TileSize,
                        (int)(lastWorldPos.Y / editor.Map.TileSize) * editor.Map.TileSize,
                        editor.Map.TileSize, editor.Map.TileSize
                    ),
                    Editor.ActiveColor
                );
            }

            if (isPosSaved)
            {
                var diff = lastWorldPos - savedWorldPos;

                if (InputState.IsMod(KeyMod.Shift))
                {
                    editor.Map.DrawRect(new Rectangle(savedWorldPos.ToPoint(), diff.ToPoint()), Color.GreenYellow);

                    var w = (System.Math.Abs((int)diff.X) - 1) / editor.Map.TileSize + 1;
                    var h = (System.Math.Abs((int)diff.Y) - 1) / editor.Map.TileSize + 1;
                    editor.DebugFont.Draw(sbatch, $"w:{w}, h:{w}", editor.Camera.WorldToScreen(lastWorldPos) + new Vector2(10, -10), Color.White);
                }
                else
                {
                    var angle = (int)MathHelper.ToDegrees((float)System.Math.Atan2(-diff.Y, diff.X));
                    if (angle < 0)
                        angle += 360;
                    editor.Map.DrawLine(savedWorldPos, lastWorldPos, Color.GreenYellow);

                    diff /= editor.Map.TileSize;
                    editor.DebugFont.Draw(sbatch, $"x:{System.Math.Ceiling(diff.X)} y:{System.Math.Ceiling(diff.Y)} deg:{angle}",
                        editor.Camera.WorldToScreen(lastWorldPos) + new Vector2(10, -10), Color.White);
                }
            }
        }

        //Tile a line, start and end in world coords
        void TileLine(Vector2 Start, Vector2 End, short TileValue)
        {
            var start = new Vector2((int)Start.X / editor.Map.TileSize, (int)Start.Y / editor.Map.TileSize).ToPoint();
            var end = new Vector2((int)End.X / editor.Map.TileSize, (int)End.Y / editor.Map.TileSize).ToPoint();

            var diff = end - start;
            var sx = diff.X > 0 ? 1 : -1;
            var sy = diff.Y > 0 ? 1 : -1;
            diff.X = System.Math.Abs(diff.X);
            diff.Y = System.Math.Abs(diff.Y);

            var err = (diff.X > diff.Y ? diff.X : -diff.Y) / 2;

            var bounds = new Rectangle(0, 0, editor.Map.Width, editor.Map.Height);
            while (true)
            {
                if (bounds.Contains(start))
                    editor.Map.Tiles[start.Y, start.X] = TileValue;

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
            if (!editor.Map.IsInside(Position))
                return;

            var initial = (Position / editor.Map.TileSize).ToPoint();
            var initialValue = editor.Map.Tiles[initial.Y, initial.X];

            if (initialValue == TileValue)
                return;

            var queue = new System.Collections.Generic.Queue<Point>();
            queue.Enqueue(initial);

            while (queue.Count > 0)
            {
                var first = queue.Dequeue();

                var left = first.X;
                var right = first.X;
                for (; left > 0 && editor.Map.Tiles[first.Y, left - 1] == initialValue; left--) ;
                for (; right < editor.Map.Width - 1 && editor.Map.Tiles[first.Y, right + 1] == initialValue; right++) ;

                for (; left <= right; left++)
                {
                    editor.Map.Tiles[first.Y, left] = TileValue;

                    if (first.Y > 0 && editor.Map.Tiles[first.Y - 1, left] == initialValue)
                        queue.Enqueue(new Point(left, first.Y - 1));

                    if (first.Y < editor.Map.Height - 1 && editor.Map.Tiles[first.Y + 1, left] == initialValue)
                        queue.Enqueue(new Point(left, first.Y + 1));
                }
            }
        }

    }
}