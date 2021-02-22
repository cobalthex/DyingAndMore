using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor
{
    class TilesEditorMode : SelectorEditorMode<Selectors.TileSelector>
    {
        bool isPosSaved = false;
        Point savedTilePos;
        Point lastTilePos;

        bool tilesChanged = false;

        Takai.Graphics.Sprite previewSprite;

        Takai.TilemapGenerator generator;

        //short[,] clipboard;

        public TilesEditorMode(Editor editor)
            : base("Tiles", editor, new Selectors.TileSelector(editor.Map.Class.Tileset))
        {
            previewSprite = preview.Sprite;
            preview.StretchToFit = true;

            generator = new Takai.TilemapGenerator(editor.Map.Class.Tileset);

            On(PressEvent, OnPress);
            On(DragEvent, OnDrag);
        }

        public override void End()
        {
            if (tilesChanged) //if can, PatchTileLayoutTexture should handle this
            {
                editor.Map.Class.GenerateCollisionMaskCPU();
                tilesChanged = false;
            }
            base.End();
        }

        public override void OnMapChanged()
        {
            selector = new Selectors.TileSelector(editor.Map.Class.Tileset);
            generator = new Takai.TilemapGenerator(editor.Map.Class.Tileset);
            UpdatePreview(selector.SelectedIndex);
        }

        protected UIEventResult OnPress(Static sender, UIEventArgs e)
        {
            var pea = (PointerEventArgs)e;
            var tilePos = (editor.Camera.ScreenToWorld(LocalToScreen(pea.position)) / editor.Map.Class.TileSize).ToPoint();
            var tile = (short)selector.SelectedIndex;
            if (pea.device == DeviceType.Mouse)
            {
                if (pea.button == (int)MouseButtons.Right)
                    tile = -1;
                else if (pea.button != (int)MouseButtons.Left)
                    return UIEventResult.Continue;
            }
            else if (pea.device == DeviceType.Touch)
                lastTilePos = tilePos;

            if (isPosSaved)
            {
                if (InputState.IsMod(KeyMod.Shift))
                    TileRect(savedTilePos, tilePos, tile);
                else
                    TileLine(savedTilePos, tilePos, tile);

                savedTilePos = tilePos;
            }
            else if (InputState.IsMod(KeyMod.Shift))
                TileFlood(tilePos, tile);
            else
            {
                 TileRect(tilePos, tilePos, tile);
                //SmartPlaceTile(tilePos, tile);
            }

            return UIEventResult.Handled;
        }

        protected UIEventResult OnDrag(Static sender, UIEventArgs e)
        {
            var dea = (DragEventArgs)e;
            var tilePos = (editor.Camera.ScreenToWorld(LocalToScreen(dea.position)) / editor.Map.Class.TileSize).ToPoint();
            var lastPos = (editor.Camera.ScreenToWorld(LocalToScreen(dea.position - dea.delta)) / editor.Map.Class.TileSize).ToPoint();
            var tile = (short)selector.SelectedIndex;
            if (dea.device == DeviceType.Mouse)
            {
                if (dea.button == (int)MouseButtons.Right)
                    tile = -1;
                else if (dea.button != (int)MouseButtons.Left)
                    return UIEventResult.Continue;
            }
            else if (dea.device == DeviceType.Touch)
                lastTilePos = tilePos;

            TileLine(lastPos, tilePos, tile);

            return UIEventResult.Handled;
        }

        protected override void UpdatePreview(int selectedItem)
        {
            if (preview.Sprite == null)
                preview.Sprite = previewSprite;

            preview.Sprite.ClipRect = new Rectangle(
                (selector.SelectedIndex % editor.Map.Class.Tileset.TilesPerRow) * editor.Map.Class.TileSize,
                (selector.SelectedIndex / editor.Map.Class.Tileset.TilesPerRow) * editor.Map.Class.TileSize,
                editor.Map.Class.TileSize,
                editor.Map.Class.TileSize
            );
        }

#if DEBUG
        protected override void UpdateSelf(GameTime time)
        {
            DyingAndMoreGame.DebugDisplay("X", lastTilePos.X);
            DyingAndMoreGame.DebugDisplay("Y", lastTilePos.Y);
            if (lastTilePos.X >= 0 && lastTilePos.X < editor.Map.Class.Width &&
                lastTilePos.Y >= 0 && lastTilePos.Y < editor.Map.Class.Height)
                DyingAndMoreGame.DebugDisplay("TV", editor.Map.Class.Tiles[lastTilePos.Y, lastTilePos.X]);

            if (isPosSaved && !InputState.IsMod(KeyMod.Shift))
            {
                var angle = (int)MathHelper.ToDegrees(Takai.Util.Angle((lastTilePos - savedTilePos).ToVector2()));
                if (angle < 0)
                    angle += 360;
                DyingAndMoreGame.DebugDisplay("Angle", angle);
            }

            base.UpdateSelf(time);
        }
#endif

        protected override bool HandleInput(GameTime time)
        {
#if  WINDOWS
            lastTilePos = (editor.Camera.ScreenToWorld(InputState.MouseVector) / editor.Map.Class.TileSize).ToPoint();
#endif

            if (InputState.IsPress(Keys.LeftControl) || InputState.IsPress(Keys.RightControl))
            {
                isPosSaved = true;
                savedTilePos = lastTilePos;
            }
            else if (InputState.IsButtonUp(Keys.LeftControl) && InputState.IsButtonUp(Keys.RightControl))
                isPosSaved = false;

#if DEBUG
            //debugging
            if (InputState.IsPress(Keys.R))
                editor.Map.Class.PatchTileLayoutTexture(editor.Map.Class.TileBounds);
#endif

            return base.HandleInput(time);
        }

        //Takai.Game.Camera clipCam = new Takai.Game.Camera();
        protected override void DrawSelf(DrawContext context)
        {

            var tileSize = new Point(editor.Map.Class.TileSize);
            var lastPos = lastTilePos * tileSize;
            var savedPos = savedTilePos * tileSize;

            //draw rect around tile under cursor
            if (editor.Map.Class.TileBounds.Contains(lastTilePos))
                editor.Map.DrawRect(new Rectangle(lastPos, tileSize), Color.Orange);

            if (isPosSaved)
            {
                //todo: draw draw rects

                if (InputState.IsMod(KeyMod.Shift))
                {
                    var rect = Takai.Util.AbsRectangle(savedPos, lastPos);
                    rect.Size += tileSize;
                    rect.Inflate(-1, -1);
                    editor.Map.DrawRect(rect, Color.GreenYellow);
                }
                else
                {
                    var half = tileSize.ToVector2() / 2;
                    editor.Map.DrawLine(savedPos.ToVector2() + half, lastPos.ToVector2() + half, Color.GreenYellow);
                }
            }

#if DEBUG
            for (int r = 0; r < editor.Map.Class.Height; ++r)
            {
                for (int c = 0; c < editor.Map.Class.Width; ++c)
                {
                    if (editor.Map.Class.Tiles[r, c] < -1)
                    {
                        editor.Map.DrawX(
                            new Vector2(c + 0.5f, r + 0.5f) * editor.Map.Class.TileSize,
                            editor.Map.Class.TileSize / 2 - 2,
                            Color.Tomato
                        );
                    }
                }
            }
#endif

            //else if (clipboard != null && clipboard.Length > 0)
            //{
            //    var context = new Takai.Game.MapBaseInstance.RenderContext();
            //    var view = new Rectangle(
            //        InputState.MousePoint.X,
            //        InputState.MousePoint.Y,
            //        clipboard.GetLength(1) * editor.Map.Class.TileSize,
            //        clipboard.GetLength(0) * editor.Map.Class.TileSize
            //    );
            //    context.viewTransform = editor.Camera.Transform * Matrix.CreateOrthographicOffCenter(view, 0, 1);
            //    context.camera = clipCam;
            //    cam.Viewport = view;
            //    editor.Map.DrawTiles(ref context, new Color(255, 255, 255, 127));
            //    Takai.Graphics.Primitives2D.DrawRect(spriteBatch, Color.Aquamarine, view);
            //}
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
            var rect = Rectangle.Intersect(new Rectangle(start, end - start + new Point(1)), editor.Map.Class.TileBounds);
            for (int y = rect.Top; y < rect.Bottom; ++y)
            {
                for (int x = rect.Left; x < rect.Right; ++x)
                {
                    editor.Map.Class.Tiles[y, x] = value; //use Array.Fill in the future
                    editor.Map.NavInfo[y, x].heuristic = uint.MaxValue;
                }
            }

            editor.Map.Class.PatchTileLayoutTexture(rect);
            tilesChanged = true;
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
            var cur = start;
            while (true)
            {
                if (bounds.Contains(cur))
                {
                    editor.Map.Class.Tiles[cur.Y, cur.X] = value;
                    editor.Map.NavInfo[cur.Y, cur.X].heuristic = uint.MaxValue;
                    //SmartPlaceTile(cur, value);
                }

                if (cur.X == end.X && cur.Y == end.Y)
                    break;

                var e2 = err;
                if (e2 > -diff.X)
                {
                    err -= diff.Y;
                    cur.X += sx;
                }
                if (e2 < diff.Y)
                {
                    err += diff.X;
                    cur.Y += sy;
                }
            }

            if (cur.X < end.X)
            {
                var s = cur.X;
                cur.X = end.X;
                end.X = s;
            }
            if (cur.Y < end.Y)
            {
                var s = cur.Y;
                cur.Y = end.Y;
                end.Y = s;
            }

            var rect = Takai.Util.AbsRectangle(start, end);
            rect.Size += new Point(1);
            editor.Map.Class.PatchTileLayoutTexture(rect);
            tilesChanged = true;
        }

        void TileFlood(Point tile, short value)
        {
            if (!editor.Map.Class.TileBounds.Contains(tile))
                return;

            var initialValue = editor.Map.Class.Tiles[tile.Y, tile.X];
            if (initialValue == value)
                return;

            Point min = tile;
            Point max = tile;

            var queue = new Queue<Point>();
            queue.Enqueue(tile);
            while (queue.Count > 0)
            {
                var first = queue.Dequeue();

                var left = first.X;
                var right = first.X;
                for (; left > 0 && editor.Map.Class.Tiles[first.Y, left - 1] == initialValue; --left) ;
                for (; right < editor.Map.Class.Width && editor.Map.Class.Tiles[first.Y, right] == initialValue; ++right) ;

                for (; left < right; ++left)
                {
                    editor.Map.Class.Tiles[first.Y, left] = value;
                    editor.Map.NavInfo[first.Y, left].heuristic = uint.MaxValue;
                    //SmartPlaceTile(new Point(left, first.Y), value);

                    if (first.Y > 0 && 
                        editor.Map.Class.Tiles[first.Y - 1, left] == initialValue)
                        queue.Enqueue(new Point(left, first.Y - 1));

                    if (first.Y < editor.Map.Class.Height - 1 &&
                        editor.Map.Class.Tiles[first.Y + 1, left] == initialValue)
                        queue.Enqueue(new Point(left, first.Y + 1));
                }

                min.X = System.Math.Min(min.X, left);
                min.Y = System.Math.Min(min.Y, first.Y);
                max.X = System.Math.Max(max.X, right);
                max.Y = System.Math.Max(max.Y, first.Y);
            }
            
            var bnd = new Rectangle(min, max - min);
            bnd.Inflate(1, 1);
            editor.Map.Class.PatchTileLayoutTexture(bnd);
            tilesChanged = true;
        }

        void SmartPlaceTile(Point pos, short tileValue)
        {
            var mc = editor.Map.Class;

            if (pos.X < 0 || pos.Y < 0 || 
                pos.X >= mc.Width || pos.Y >= mc.Height)
                return;

            mc.Tiles[pos.Y, pos.X] = tileValue;
            if (pos.X > 0 && mc.Tiles[pos.Y, pos.X - 1] < 0) // todo: check if old tile is valid and replace if not
                SetTileAware(pos.X - 1, pos.Y);
            if (pos.X < mc.Width - 1 && mc.Tiles[pos.Y, pos.X + 1] < 0)
                SetTileAware(pos.X + 1, pos.Y);
            if (pos.Y > 0 && mc.Tiles[pos.Y - 1, pos.X] < 0)
                SetTileAware(pos.X, pos.Y - 1);
            if (pos.Y < mc.Height - 1 && mc.Tiles[pos.Y + 1, pos.X] < 0)
                SetTileAware(pos.X, pos.Y + 1);

            editor.Map.Class.PatchTileLayoutTexture(new Rectangle(pos.X - 1, pos.Y - 1, 3, 3));
        }

        void SetTileAware(int x, int y)
        {
            var mc = editor.Map.Class;

            bool filled = false;
            var available = new HashSet<short>();
            if (x > 0)
            {
                available.UnionWith(generator.Adjacencies[mc.Tiles[y, x - 1] + 1].right);
                filled = true;
            }
            if (x < mc.Width - 1)
            {
                var edges = generator.Adjacencies[mc.Tiles[y, x + 1] + 1].left;
                if (!filled)
                    available.UnionWith(edges);
                else
                    available.IntersectWith(edges);
                filled = true;
            }
            if (y > 0)
            {
                var edges = generator.Adjacencies[mc.Tiles[y - 1, x] + 1].bottom;
                if (!filled)
                    available.UnionWith(edges);
                else
                    available.IntersectWith(edges);
                filled = true;
            }

            if (y < mc.Height - 1)
            {
                var edges = generator.Adjacencies[mc.Tiles[y + 1, x] + 1].top;
                if (!filled)
                    available.UnionWith(edges);
                else
                    available.IntersectWith(edges);
            }

            if (available.Count == 0)
                mc.Tiles[y, x] = Takai.TilemapGenerator.TILE_CLEAR; // error
            else
                mc.Tiles[y, x] = (short)(System.Linq.Enumerable.ElementAt(available, Takai.Util.RandomGenerator.Next(available.Count)) - 1);
        }
    }
}