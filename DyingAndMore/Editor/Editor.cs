﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.Graphics;

namespace DyingAndMore.Editor
{
    enum EditorMode
    {
        Tiles,
        Decals,
        Blobs,
        Entities,
        Regions,
        Paths,
    }

    struct DecalIndex
    {
        public int x, y, index;
    }

    class Editor : Takai.States.State
    {
        EditorMode currentMode = EditorMode.Tiles;

        Takai.Game.Camera camera;

        BitmapFont fnt;
        string debugText = "";

        SpriteBatch sbatch;

        BitmapFont smallFont, largeFont;
        
        public Takai.Game.Map map;

        Color highlightColor = Color.Gold;

        Selector[] selectors;
        
        bool isPosSaved = false;
        Vector2 savedWorldPos, lastWorldPos;

        DecalIndex? selectedDecal = null;
        Takai.Game.Entity selectedEntity = null;
        float startRotation, startScale;
        System.TimeSpan lastBlobTime = System.TimeSpan.Zero;

        public Editor() : base(Takai.States.StateType.Full) { }
        
        void AddSelector(Selector Sel, int Index)
        {
            selectors[Index] = Sel;
            Takai.States.StateManager.PushState(Sel);
            Sel.Deactivate();
        }

        public override void Load()
        {
            fnt = Takai.AssetManager.Load<BitmapFont>("Fonts/rct2.bfnt");

            smallFont = Takai.AssetManager.Load<BitmapFont>("Fonts/UISmall.bfnt");
            largeFont = Takai.AssetManager.Load<BitmapFont>("Fonts/UILarge.bfnt");

            sbatch = new SpriteBatch(GraphicsDevice);

            map = new Takai.Game.Map(GraphicsDevice);

            using (var stream = new System.IO.FileStream("Data/Maps/test.map.tk", System.IO.FileMode.Open))
                map.Load(stream);
            
            camera = new Takai.Game.Camera(map, null);
            camera.MoveSpeed = 1600;
            camera.Viewport = GraphicsDevice.Viewport.Bounds;

            selectors = new Selector[System.Enum.GetValues(typeof(EditorMode)).Length];
            AddSelector(new TileSelector(this), 0);
            AddSelector(new DecalSelector(this), 1);
            AddSelector(new BlobSelector(this), 2);
            AddSelector(new EntSelector(this), 3);
        }
        
        public override void Update(GameTime Time)
        {
            if (InputCatalog.KBState.IsKeyDown(Keys.LeftControl) || InputCatalog.KBState.IsKeyDown(Keys.RightControl))
            {
                if (InputCatalog.IsKeyPress(Keys.S))
                {
                    var sfd = new System.Windows.Forms.SaveFileDialog();
                    sfd.Filter = "Map (*.map.tk)|*.map.tk|All Files (*.*)|*.*";
                    if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        using (var stream = sfd.OpenFile())
                            map.Save(stream);
                    }
                    return;
                }
                else if (InputCatalog.IsKeyPress(Keys.O))
                {
                    var ofd = new System.Windows.Forms.OpenFileDialog();
                    ofd.Filter = "Map (*.map.tk)|*.map.tk|All Files (*.*)|*.*";
                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        using (var stream = ofd.OpenFile())
                            map.Load(stream, true);

                        selectedDecal = null;
                        selectedEntity = null;
                    }
                    return;
                }
            }

            if (InputCatalog.IsKeyPress(Keys.Q))
                Takai.States.StateManager.Exit();

            if (InputCatalog.IsKeyPress(Keys.F1))
                map.debugOptions.showBlobReflectionMask ^= true;
            if (InputCatalog.IsKeyPress(Keys.F2))
                map.debugOptions.showOnlyReflections ^= true;

            foreach (int i in System.Enum.GetValues(typeof(EditorMode)))
            {
                if (InputCatalog.IsKeyPress(Keys.D1 + i) || InputCatalog.IsKeyPress(Keys.NumPad1 + i))
                {
                    currentMode = (EditorMode)i;
                    break;
                }
            }

            var worldMousePos = camera.ScreenToWorld(InputCatalog.MouseState.Position.ToVector2());

            if (InputCatalog.MouseState.MiddleButton == ButtonState.Pressed)
            {
                var delta = InputCatalog.LastMouseState.Position - InputCatalog.MouseState.Position;
                camera.MoveTo(camera.Position + Vector2.TransformNormal(delta.ToVector2(), Matrix.Invert(camera.Transform)));
            }
            else
            {
                var d = Vector2.Zero;
                if (InputCatalog.KBState.IsKeyDown(Keys.A))
                    d -= Vector2.UnitX;
                if (InputCatalog.KBState.IsKeyDown(Keys.W))
                    d -= Vector2.UnitY;
                if (InputCatalog.KBState.IsKeyDown(Keys.D))
                    d += Vector2.UnitX;
                if (InputCatalog.KBState.IsKeyDown(Keys.S))
                    d += Vector2.UnitY;

                if (d != Vector2.Zero)
                {
                    d.Normalize();
                    camera.Position += d * camera.MoveSpeed * (float)Time.ElapsedGameTime.TotalSeconds; //(camera velocity)
                }
            }

            if (InputCatalog.IsKeyPress(Keys.Tab))
                selectors[(int)currentMode]?.Activate();

            if (InputCatalog.HasMouseScrolled())
            {
                var delta = InputCatalog.ScrollDelta() / 1024f;
                if (InputCatalog.KBState.IsKeyDown(Keys.LeftShift))
                {
                    camera.Rotation += delta;
                }
                else
                {
                    camera.Scale += delta;
                    camera.Scale = MathHelper.Clamp(camera.Scale, 0.1f, 2f);
                }
                //todo: translate to mouse cursor
            }

            camera.Update(Time);

            if (InputCatalog.IsMousePress(InputCatalog.MouseButton.Left) && InputCatalog.KBState.IsKeyDown(Keys.LeftAlt))
            {
                var ofd = new System.Windows.Forms.OpenFileDialog();
                ofd.Filter = "Entity Definitions (*.ent.tk)|*.ent.tk";
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Takai.Game.Entity ent;
                    using (var reader = new System.IO.StreamReader(ofd.OpenFile()))
                        ent = Takai.Data.Serializer.TextDeserialize(reader) as Takai.Game.Entity;

                    if (ent != null)
                    {
                        ent.Position = worldMousePos;
                        map.SpawnEntity(ent);
                    }
                }
            }

            if (InputCatalog.IsKeyPress(Keys.LeftControl))
            {
                isPosSaved = true;
                savedWorldPos = worldMousePos;
            }
            else if (InputCatalog.IsKeyClick(Keys.LeftControl))
                isPosSaved = false;

            #region Tiles
            if (currentMode == EditorMode.Tiles)
            {
                var tile = short.MinValue;

                if (InputCatalog.MouseState.LeftButton == ButtonState.Pressed)
                    tile = (short)selectors[0].SelectedItem;
                else if (InputCatalog.MouseState.RightButton == ButtonState.Pressed)
                    tile = -1;

                if (tile > short.MinValue)
                {
                    if (InputCatalog.KBState.IsKeyDown(Keys.LeftShift))
                        TileFill(worldMousePos, tile);
                    else if (isPosSaved)
                    {
                        TileLine(savedWorldPos, worldMousePos, tile);
                        savedWorldPos = worldMousePos;
                    }
                    else
                        TileLine(lastWorldPos, worldMousePos, tile);
                }
            }
            #endregion

            #region Decals
            else if (currentMode == EditorMode.Decals)
            {
                if (InputCatalog.IsMousePress(InputCatalog.MouseButton.Left))
                {
                    if (!SelectDecal(worldMousePos))
                    {
                        //add new decal none under cursor
                        var sel = selectors[(int)currentMode] as DecalSelector;
                        map.AddDecal(sel.textures[sel.SelectedItem], worldMousePos);
                        var pos = (worldMousePos / map.SectorPixelSize).ToPoint();
                        selectedDecal = new DecalIndex { x = pos.X, y = pos.Y, index = map.Sectors[pos.Y, pos.X].decals.Count - 1 };
                    }
                }
                else if (InputCatalog.IsMousePress(InputCatalog.MouseButton.Right))
                {
                    SelectDecal(worldMousePos);
                }
                else if (InputCatalog.IsMouseClick(InputCatalog.MouseButton.Right))
                {
                    var lastSelected = selectedDecal;
                    SelectDecal(worldMousePos);

                    if (selectedDecal != null && selectedDecal.Equals(lastSelected))
                    {
                        map.Sectors[selectedDecal.Value.y, selectedDecal.Value.x].decals.RemoveAt(selectedDecal.Value.index);
                        selectedDecal = null;
                    }
                }

                else if (selectedDecal.HasValue)
                {
                    var decal = map.Sectors[selectedDecal.Value.y, selectedDecal.Value.x].decals[selectedDecal.Value.index];

                    if (InputCatalog.MouseState.LeftButton == ButtonState.Pressed)
                    {
                        var delta = worldMousePos - lastWorldPos;
                        decal.position += delta;
                    }

                    if (InputCatalog.KBState.IsKeyDown(Keys.R))
                    {
                        var diff = worldMousePos - decal.position;

                        var theta = (float)System.Math.Atan2(diff.Y, diff.X);
                        if (InputCatalog.lastKBState.IsKeyUp(Keys.R))
                            startRotation = theta - decal.angle;

                        decal.angle = theta - startRotation;
                    }

                    if (InputCatalog.KBState.IsKeyDown(Keys.E))
                    {
                        float dist = Vector2.Distance(worldMousePos, decal.position);

                        if (InputCatalog.lastKBState.IsKeyUp(Keys.E))
                            startScale = dist;

                        decal.scale = MathHelper.Clamp(decal.scale + (dist - startScale) / 25, 0.25f, 10f);
                        startScale = dist;
                    }

                    map.Sectors[selectedDecal.Value.y, selectedDecal.Value.x].decals[selectedDecal.Value.index] = decal;
                }
            }
            #endregion

            #region Blobs
            else if (currentMode == EditorMode.Blobs)
            {
                if (Time.TotalGameTime > lastBlobTime + System.TimeSpan.FromMilliseconds(50))
                {
                    if (InputCatalog.MouseState.LeftButton == ButtonState.Pressed)
                    {
                        var sel = selectors[(int)EditorMode.Blobs] as BlobSelector;
                        map.SpawnBlob(sel.blobs[sel.SelectedItem], worldMousePos, Vector2.Zero);
                    }

                    else if (InputCatalog.MouseState.RightButton == ButtonState.Pressed)
                    {
                        var mapSz = new Vector2(map.Width, map.Height);
                        var start = Vector2.Clamp((worldMousePos / map.SectorPixelSize) - Vector2.One, Vector2.Zero, mapSz).ToPoint();
                        var end = Vector2.Clamp((worldMousePos / map.SectorPixelSize) + Vector2.One, Vector2.Zero, mapSz).ToPoint();
                        
                        for (int y = start.Y; y < end.Y; y++)
                        {
                            for (int x = start.X; x < end.X; x++)
                            {
                                var sect = map.Sectors[y, x];
                                for (var i = 0; i < sect.blobs.Count; i++)
                                {
                                    var blob = sect.blobs[i];
                                    
                                    if (Vector2.DistanceSquared(blob.position, worldMousePos) < blob.type.Radius * blob.type.Radius)
                                    {
                                        sect.blobs[i] = sect.blobs[sect.blobs.Count - 1];
                                        sect.blobs.RemoveAt(sect.blobs.Count - 1);
                                    }
                                }
                            }
                        }
                    }

                    lastBlobTime = Time.TotalGameTime;
                }
            }
            #endregion

            #region Entities
            else if (currentMode == EditorMode.Entities)
            {
                if (InputCatalog.IsMousePress(InputCatalog.MouseButton.Left))
                {
                    var selected = map.FindNearbyEntities(worldMousePos, 1, true);
                    if (selected.Count < 1)
                    {
                        var sel = selectors[(int)currentMode] as EntSelector;
                        selectedEntity = map.SpawnEntity(sel.ents[sel.SelectedItem], worldMousePos, Vector2.UnitX, Vector2.Zero);
                    }
                    else
                        selectedEntity = selected[0];
                }
                else if (InputCatalog.IsMousePress(InputCatalog.MouseButton.Right))
                {
                    var selected = map.FindNearbyEntities(worldMousePos, 1, true);
                    selectedEntity = selected.Count > 0 ? selected[0] : null;
                }
                else if (InputCatalog.IsMouseClick(InputCatalog.MouseButton.Right))
                {
                    var selected = map.FindNearbyEntities(worldMousePos, 1, true);
                    if (selected.Count > 0 && selected[0] == selectedEntity)
                    {
                        map.Destroy(selectedEntity);
                        selectedEntity = null;
                    }
                }

                else if (selectedEntity != null)
                {
                    if (InputCatalog.MouseState.LeftButton == ButtonState.Pressed)
                    {
                        var delta = worldMousePos - lastWorldPos;
                        selectedEntity.Position += delta;
                    }

                    if (InputCatalog.KBState.IsKeyDown(Keys.R))
                    {
                        var diff = worldMousePos - selectedEntity.Position;
                        diff.Normalize();
                        selectedEntity.Direction = diff;
                    }
                }
            }
            #endregion
            
            lastWorldPos = worldMousePos;

        }

        bool SelectDecal(Vector2 WorldPosition)
        {
            //find closest decal
            var mapSz = new Vector2(map.Width, map.Height);
            var start = Vector2.Clamp((WorldPosition / map.SectorPixelSize) - Vector2.One, Vector2.Zero, mapSz).ToPoint();
            var end = Vector2.Clamp((WorldPosition / map.SectorPixelSize) + Vector2.One, Vector2.Zero, mapSz).ToPoint();

            selectedDecal = null;
            for (int y = start.Y; y < end.Y; y++)
            {
                for (int x = start.X; x < end.X; x++)
                {
                    for (var i = 0; i < map.Sectors[y, x].decals.Count; i++)
                    {
                        var decal = map.Sectors[y, x].decals[i];

                        //todo: transform worldPosition by decal matrix and perform transformed comparison
                        var transform = Matrix.CreateScale(decal.scale) * Matrix.CreateRotationZ(decal.angle) * Matrix.CreateTranslation(new Vector3(decal.position, 0));

                        if (Vector2.DistanceSquared(decal.position, WorldPosition) < decal.texture.Width * decal.texture.Width * decal.scale)
                        {
                            selectedDecal = new DecalIndex { x = x, y = y, index = i };
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        Vector2 CenterInRect(Vector2 Size, Rectangle Region)
        {
            return new Vector2(Region.X + (Region.Width - Size.X) / 2, Region.Y + (Region.Height - Size.Y) / 2);
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

            var rect = new Rectangle(0, 0, map.Width, map.Height);
            while (true)
            {
                if (rect.Contains(start))
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

                //if (map.Tiles[first.Y, first.X] != initialValue)
                //    continue;
                
                var left = first.X;
                var right = first.X;
                for (; left > 0 && map.Tiles[first.Y, left - 1] == initialValue; left--) ;
                for (; right < map.Width - 1&& map.Tiles[first.Y, right + 1] == initialValue; right++) ;
                
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
        
        void MapLineRect(Rectangle Rect, Color Color)
        {
            map.DebugLine(new Vector2(Rect.Left, Rect.Top), new Vector2(Rect.Right, Rect.Top), Color);
            map.DebugLine(new Vector2(Rect.Left, Rect.Top), new Vector2(Rect.Left, Rect.Bottom), Color);
            map.DebugLine(new Vector2(Rect.Right, Rect.Top), new Vector2(Rect.Right, Rect.Bottom), Color);
            map.DebugLine(new Vector2(Rect.Left, Rect.Bottom), new Vector2(Rect.Right, Rect.Bottom), Color);
        }

        public override void Draw(GameTime Time)
        {
            //draw border around map
            MapLineRect(new Rectangle(0, 0, map.Width * map.TileSize, map.Height * map.TileSize), Color.CornflowerBlue);

            camera.Draw();

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            fnt.Draw(sbatch, debugText, new Vector2(10), Color.CornflowerBlue);
            var sFps = (1 / Time.ElapsedGameTime.TotalSeconds).ToString("N2");
            var sSz = fnt.MeasureString(sFps);
            fnt.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - sSz.X - 10, GraphicsDevice.Viewport.Height - sSz.Y - 10), Color.LightSteelBlue);

            var viewPos = camera.WorldToScreen(camera.ActualPosition);
            var worldMousePos = camera.ScreenToWorld(InputCatalog.MouseState.Position.ToVector2());

            if (currentMode == EditorMode.Tiles)
            {
                //draw rect around tile under cursor
                if (map.IsInside(worldMousePos))
                {
                    MapLineRect
                    (
                        new Rectangle
                        (
                            (int)(worldMousePos.X / map.TileSize) * map.TileSize,
                            (int)(worldMousePos.Y / map.TileSize) * map.TileSize,
                            map.TileSize, map.TileSize
                        ),
                        highlightColor
                    );
                }

                if (isPosSaved)
                {
                    var diff = worldMousePos - savedWorldPos;
                    var angle = (int)MathHelper.ToDegrees((float)System.Math.Atan2(-diff.Y, diff.X));
                    if (angle < 0)
                        angle += 360;
                    map.DebugLine(savedWorldPos, worldMousePos, Color.GreenYellow);
                    fnt.Draw(sbatch, string.Format("x:{0} y:{1} deg:{2}", diff.X, diff.Y, angle), camera.WorldToScreen(worldMousePos) + new Vector2(10, -10), Color.White);
                }
            }
            else if (currentMode == EditorMode.Decals)
            {   
                if (selectedDecal.HasValue)
                {
                    var decal = map.Sectors[selectedDecal.Value.y, selectedDecal.Value.x].decals[selectedDecal.Value.index];
                    var transform = Matrix.CreateScale(decal.scale) * Matrix.CreateRotationZ(decal.angle) * Matrix.CreateTranslation(new Vector3(decal.position, 0));

                    var w2 = decal.texture.Width / 2;
                    var h2 = decal.texture.Height / 2;
                    var tl = Vector2.Transform(new Vector2(-w2, -h2), transform);
                    var tr = Vector2.Transform(new Vector2(w2, -h2), transform);
                    var bl = Vector2.Transform(new Vector2(-w2, h2), transform);
                    var br = Vector2.Transform(new Vector2(w2, h2), transform);

                    map.DebugLine(tl, tr, Color.GreenYellow);
                    map.DebugLine(tr, br, Color.GreenYellow);
                    map.DebugLine(br, bl, Color.GreenYellow);
                    map.DebugLine(bl, tl, Color.GreenYellow);
                }
            }
            else if (currentMode == EditorMode.Entities)
            {
                if (selectedEntity != null)
                    DrawEntInfo(selectedEntity);

                foreach (var ent in map.ActiveEnts)
                    DrawArrow(ent.Position, ent.Direction, ent.Radius * 1.5f);
            }

            //draw selected item in top right corner
            if (selectors[(int)currentMode] != null)
            {
                var selectedItemRect = new Rectangle(GraphicsDevice.Viewport.Width - map.TileSize - 20, 20, map.TileSize, map.TileSize);
                selectors[(int)currentMode].DrawItem(Time, selectors[(int)currentMode].SelectedItem, selectedItemRect, sbatch);
                Primitives2D.DrawRect(sbatch, Color.White, selectedItemRect);
            }

            //draw modes
            {
                var modes = System.Enum.GetNames(typeof(EditorMode));
                var modeSz = new Vector2[modes.Length];
                var modeTotalWidth = 0f;
                var modeMaxHeight = 0f;
                for (var i = 0; i < modes.Length; i++)
                {
                    modeSz[i] = largeFont.MeasureString(modes[i]);
                    modeSz[i].X += 20;
                    modeTotalWidth += modeSz[i].X;
                    modeMaxHeight = MathHelper.Max(modeMaxHeight, modeSz[i].Y);
                }

                var startX = (int)(GraphicsDevice.Viewport.Width - modeTotalWidth) / 2;
                for (var i = 0; i < modes.Length; i++)
                {
                    var isSel = (int)currentMode == i;
                    var font = isSel ? largeFont : smallFont;
                    var str = modes[i];
                    font.Draw
                    (
                        sbatch,
                        str,
                        CenterInRect(font.MeasureString(str), new Rectangle(startX, 20, (int)modeSz[i].X, (int)modeSz[i].Y)),
                        isSel ? Color.White : new Color(1, 1, 1, 0.5f)
                    );
                    startX += (int)modeSz[i].X;
                }
            }

            sbatch.End();
        }

        static readonly Matrix ArrowRotation = Matrix.CreateRotationZ(120);

        protected void DrawArrow(Vector2 Position, Vector2 Direction, float Magnitude)
        {
            var tip = Position + (Direction * Magnitude);
            map.DebugLine(Position, tip, Color.Yellow);

            Magnitude = MathHelper.Clamp(Magnitude * 0.333f, 5, 30);
            map.DebugLine(tip, tip - (Magnitude * Vector2.Transform(Direction, ArrowRotation)), Color.Yellow);
            map.DebugLine(tip, tip - (Magnitude * Vector2.Transform(Direction, Matrix.Invert(ArrowRotation))), Color.Yellow);
        }

        protected void DrawEntInfo(Takai.Game.Entity Ent)
        {
            //draw bounding box
            MapLineRect(new Rectangle(
                (int)(Ent.Position.X - Ent.Radius),
                (int)(Ent.Position.Y - Ent.Radius),
                (int)(Ent.Radius * 2),
                (int)(Ent.Radius * 2)
            ), Color.GreenYellow);

            //draw ent info string
            string str;
            Vector2 sz, pos;

            if (Ent.Name != null)
            {
                str = Ent.Name;
                sz = smallFont.MeasureString(str);
                pos = camera.WorldToScreen(Ent.Position - new Vector2(0, Ent.Radius + 2));
                pos = new Vector2((int)(pos.X - sz.X / 2), (int)(pos.Y - sz.Y));
                smallFont.Draw(sbatch, str, pos, Color.White);
            }

            str = string.Format("{0:N1},{1:N1}", Ent.Position.X, Ent.Position.Y);
            sz = smallFont.MeasureString(str);
            pos = camera.WorldToScreen(Ent.Position + new Vector2(0, Ent.Radius + 2));
            pos = new Vector2((int)(pos.X - sz.X / 2), (int)pos.Y);
            smallFont.Draw(sbatch, str, pos, Color.White);
        }
    }
}