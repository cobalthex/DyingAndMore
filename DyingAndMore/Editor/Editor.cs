using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.Graphics;
using System.Linq;

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

    class Editor : Takai.States.GameState
    {
        public EditorMode currentMode = EditorMode.Tiles;

        public Takai.Game.Camera camera;

        SpriteBatch sbatch;

        BitmapFont tinyFont, smallFont, largeFont;

        public Takai.Game.Map map;

        Color highlightColor = Color.Gold;

        Selector[] selectors;

        bool isPosSaved = false;
        Vector2 savedWorldPos, lastWorldPos;

        DecalIndex? selectedDecal = null;
        Takai.Game.Entity selectedEntity = null;
        float startRotation, startScale;
        System.TimeSpan lastBlobTime = System.TimeSpan.Zero;

        int uiMargin = 20;

        public Editor() : base(false, false) { }

        void AddSelector(Selector Sel, EditorMode Index)
        {
            selectors[(uint)Index] = Sel;
            Takai.States.GameStateManager.PushState(Sel);
            Sel.Deactivate();
        }

        public override void Load()
        {
            tinyFont = Takai.AssetManager.Load<BitmapFont>("Fonts/rct2.bfnt");
            smallFont = Takai.AssetManager.Load<BitmapFont>("Fonts/UISmall.bfnt");
            largeFont = Takai.AssetManager.Load<BitmapFont>("Fonts/UILarge.bfnt");

            sbatch = new SpriteBatch(GraphicsDevice);

            selectors = new Selector[System.Enum.GetValues(typeof(EditorMode)).Length];
            AddSelector(new TileSelector(this), EditorMode.Tiles);
            AddSelector(new DecalSelector(this), EditorMode.Decals);
            AddSelector(new BlobSelector(this), EditorMode.Blobs);
            AddSelector(new EntSelector(this), EditorMode.Entities);

            map.updateSettings = Takai.Game.MapUpdateSettings.Editor;

            TouchPanel.EnabledGestures = GestureType.Pinch | GestureType.Tap | GestureType.DoubleTap | GestureType.FreeDrag;
        }

        public override void Unload()
        {
            while (Takai.States.GameStateManager.TopState != this
                && Takai.States.GameStateManager.Count > 0)
                Takai.States.GameStateManager.PopState();

            TouchPanel.EnabledGestures = GestureType.None;
        }

        public override void Update(GameTime Time)
        {
            if (selectedEntity != null)
                selectedEntity.OutlineColor = Color.Transparent;

            var selectedItemRect = new Rectangle(
                GraphicsDevice.Viewport.Width - 10 - map.TileSize,
                10,
                map.TileSize,
                map.TileSize
            );

            if (InputState.IsMod(KeyMod.Control))
            {
                if (InputState.IsPress(Keys.S))
                {
                    var sfd = new System.Windows.Forms.SaveFileDialog();
                    sfd.SupportMultiDottedExtensions = true;
                    sfd.Filter = "Map (*.map.tk)|*.map.tk|All Files (*.*)|*.*";
                    sfd.InitialDirectory = System.IO.Path.GetDirectoryName(map.File);
                    sfd.FileName = System.IO.Path.GetFileName(map.File?.Substring(0, map.File.IndexOf('.'))); //file dialog is retarded
                    if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        using (var stream = sfd.OpenFile())
                            map.Save(stream);

                        map.File = sfd.FileName;
                    }
                    return;
                }
                else if (InputState.IsPress(Keys.O))
                {
                    var ofd = new System.Windows.Forms.OpenFileDialog();
                    ofd.SupportMultiDottedExtensions = true;
                    ofd.Filter = "Map (*.map.tk)|*.map.tk|All Files (*.*)|*.*";
                    ofd.InitialDirectory = System.IO.Path.GetDirectoryName(map.File);
                    ofd.FileName = System.IO.Path.GetFileName(map.File);
                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        using (var stream = ofd.OpenFile())
                            map.Load(stream, true);

                        map.File = ofd.FileName;
                        map.updateSettings = Takai.Game.MapUpdateSettings.Editor;

                        selectedDecal = null;
                        selectedEntity = null;

                        camera = new Takai.Game.Camera(map, null)
                        {
                            MoveSpeed = 1600,
                            Viewport = GraphicsDevice.Viewport.Bounds
                        };
                    }
                    return;
                }
            }

            if (InputState.IsMod(KeyMod.Control) && InputState.IsPress(Keys.Q))
            {
                Takai.States.GameStateManager.Exit();
                return;
            }

            if (InputState.IsClick(Keys.F1))
            {
                Takai.States.GameStateManager.NextState(new Game.Game() { map = map });
                return;
            }

            if (InputState.IsPress(Keys.F2))
                map.debugOptions.showBlobReflectionMask ^= true;

            if (InputState.IsPress(Keys.F3))
                map.debugOptions.showOnlyReflections ^= true;

            foreach (int i in System.Enum.GetValues(typeof(EditorMode)))
            {
                if (InputState.IsPress(Keys.D1 + i) || InputState.IsPress(Keys.NumPad1 + i))
                {
                    currentMode = (EditorMode)i;
                    break;
                }
            }

            var worldMousePos = camera.ScreenToWorld(InputState.MouseVector);

            bool isTapping = false, isDoubleTapping = false;

            //touch gestures
            while (TouchPanel.IsGestureAvailable)
            {
                var gesture = TouchPanel.ReadGesture();
                switch (gesture.GestureType)
                {
                    case GestureType.Pinch:
                        {
                            //move
                            if (Vector2.Dot(gesture.Delta, gesture.Delta2) > 0)
                            {
                                camera.Position -= Vector2.TransformNormal(gesture.Delta2, Matrix.Invert(camera.Transform)) / 2;
                            }
                            //scale
                            else
                            {
                                var lp1 = gesture.Position - gesture.Delta;
                                var lp2 = gesture.Position2 - gesture.Delta2;
                                var dist = Vector2.Distance(gesture.Position2, gesture.Position2);
                                var ld = Vector2.Distance(lp1, lp2);

                                var scale = (dist / ld) / 1024;
                                camera.Scale += scale;
                            }
                            break;
                        }

                    case GestureType.Tap:
                        if (selectedItemRect.Contains(gesture.Position))
                        {
                            OpenCurrentSelector(true);
                            return;
                        }

                        isTapping = true;
                        break;

                    case GestureType.DoubleTap:
                        isDoubleTapping = true;
                        break;
                }
            }

            if (InputState.IsButtonDown(MouseButtons.Middle))
            {
                camera.MoveTo(camera.Position + Vector2.TransformNormal(InputState.MouseDelta(), Matrix.Invert(camera.Transform)));
            }
            else
            {
                var d = Vector2.Zero;
                if (InputState.IsButtonDown(Keys.A))
                    d -= Vector2.UnitX;
                if (InputState.IsButtonDown(Keys.W))
                    d -= Vector2.UnitY;
                if (InputState.IsButtonDown(Keys.D))
                    d += Vector2.UnitX;
                if (InputState.IsButtonDown(Keys.S))
                    d += Vector2.UnitY;

                if (d != Vector2.Zero)
                {
                    d.Normalize();
                    camera.Position += d * camera.MoveSpeed * (float)Time.ElapsedGameTime.TotalSeconds; //(camera velocity)
                }
            }

            void OpenCurrentSelector(bool ClickedOpen)
            {
                var selector = selectors[(uint)currentMode];
                if (selector != null)
                {
                    selector.DidClickOpen = ClickedOpen;
                    selector.Activate();
                }
            }

            if (InputState.IsPress(Keys.Tab))
            {
                OpenCurrentSelector(false);
                return;
            }

            if (InputState.IsPress(MouseButtons.Left) && selectedItemRect.Contains(InputState.MousePoint))
            {
                OpenCurrentSelector(true);
                return;
            }

            if (InputState.HasScrolled())
            {
                var delta = InputState.ScrollDelta() / 1024f;
                if (InputState.IsButtonDown(Keys.LeftShift))
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

            //todo: move to entities only
            if (InputState.IsPress(MouseButtons.Left) && InputState.IsMod(KeyMod.Alt))
            {
                var ofd = new System.Windows.Forms.OpenFileDialog()
                {
                    Filter = "Entity Definitions (*.ent.tk)|*.ent.tk"
                };
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Takai.Game.Entity ent;
                    using (var reader = new System.IO.StreamReader(ofd.OpenFile()))
                        ent = Takai.Data.Serializer.TextDeserialize(reader) as Takai.Game.Entity;

                    if (ent != null)
                    {
                        ent.Position = worldMousePos;
                        map.Spawn(ent);
                    }
                }
            }

            if (InputState.IsPress(Keys.LeftControl) || InputState.IsPress(Keys.RightControl))
            {
                isPosSaved = true;
                savedWorldPos = worldMousePos;
            }
            else if (InputState.IsClick(Keys.LeftControl) || InputState.IsClick(Keys.RightControl))
                isPosSaved = false;

            #region Tiles

            if (currentMode == EditorMode.Tiles)
            {
                var tile = short.MinValue;

                if (InputState.IsButtonDown(MouseButtons.Left))
                    tile = (short)selectors[0].SelectedItem;
                else if (InputState.IsButtonDown(MouseButtons.Right))
                    tile = -1;

                if (tile > short.MinValue)
                {
                    if (InputState.IsMod(KeyMod.Shift))
                        TileFill(worldMousePos, tile);
                    else if (isPosSaved)
                    {
                        TileLine(savedWorldPos, worldMousePos, tile);
                        savedWorldPos = worldMousePos;
                    }
                    else if (InputState.IsButtonHeld(tile == -1 ? MouseButtons.Right : MouseButtons.Left)) //todo: improve
                        TileLine(lastWorldPos, worldMousePos, tile);
                }
            }

            #endregion

            #region Decals

            else if (currentMode == EditorMode.Decals)
            {
                if (InputState.IsPress(MouseButtons.Left))
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
                else if (InputState.IsPress(MouseButtons.Right))
                {
                    SelectDecal(worldMousePos);
                }
                else if (InputState.IsClick(MouseButtons.Right))
                {
                    var lastSelected = selectedDecal;
                    SelectDecal(worldMousePos);

                    if (selectedDecal != null && selectedDecal.Equals(lastSelected))
                    {
                        //todo: use swap
                        map.Sectors[selectedDecal.Value.y, selectedDecal.Value.x].decals.RemoveAt(selectedDecal.Value.index);
                        selectedDecal = null;
                    }
                }

                else if (selectedDecal.HasValue)
                {
                    var decal = map.Sectors[selectedDecal.Value.y, selectedDecal.Value.x].decals[selectedDecal.Value.index];

                    if (InputState.IsButtonDown(MouseButtons.Left))
                    {
                        var delta = worldMousePos - lastWorldPos;
                        decal.position += delta;
                    }

                    if (InputState.IsButtonDown(Keys.R))
                    {
                        var diff = worldMousePos - decal.position;

                        var theta = (float)System.Math.Atan2(diff.Y, diff.X);
                        if (InputState.IsPress(Keys.R))
                            startRotation = theta - decal.angle;

                        decal.angle = theta - startRotation;
                    }

                    if (InputState.IsButtonDown(Keys.E))
                    {
                        float dist = Vector2.Distance(worldMousePos, decal.position);

                        if (InputState.IsPress(Keys.E))
                            startScale = dist;

                        decal.scale = MathHelper.Clamp(decal.scale + (dist - startScale) / 25, 0.25f, 10f);
                        startScale = dist;
                    }

                    if (InputState.IsPress(Keys.Delete))
                    {
                        map.Sectors[selectedDecal.Value.y, selectedDecal.Value.x].decals.RemoveAt(selectedDecal.Value.index);
                        selectedDecal = null;
                    }
                    else
                        map.Sectors[selectedDecal.Value.y, selectedDecal.Value.x].decals[selectedDecal.Value.index] = decal;

                    //todo: clone
                }
            }

            #endregion

            #region Blobs

            else if (currentMode == EditorMode.Blobs)
            {
                if (Time.TotalGameTime > lastBlobTime + System.TimeSpan.FromMilliseconds(50))
                {
                    if (InputState.IsButtonDown(MouseButtons.Left))
                    {
                        var sel = selectors[(int)EditorMode.Blobs] as BlobSelector;
                        map.Spawn(sel.blobs[sel.SelectedItem], worldMousePos, Vector2.Zero);
                    }

                    else if (InputState.IsButtonDown(MouseButtons.Right))
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
                                        i--;
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
                if (InputState.IsPress(MouseButtons.Left) || isTapping)
                {
                    var searchRadius = isTapping ? 10 : 1;
                    var selected = map.FindEntities(worldMousePos, searchRadius, true);
                    if (selected.Count < 1)
                    {
                        var sel = selectors[(int)currentMode] as EntSelector;
                        if (sel != null && sel.ents.Count > 0)
                            selectedEntity = map.Spawn(sel.ents[sel.SelectedItem], worldMousePos, Vector2.UnitX, Vector2.Zero);
                    }
                    else
                        selectedEntity = selected[0];
                }
                else if (InputState.IsPress(MouseButtons.Right))
                {
                    var selected = map.FindEntities(worldMousePos, 1, true);
                    selectedEntity = selected.Count > 0 ? selected[0] : null;
                }
                else if (InputState.IsClick(MouseButtons.Right) || isDoubleTapping)
                {
                    var searchRadius = isTapping ? 10 : 1;
                    var selected = map.FindEntities(worldMousePos, searchRadius, true);
                    if (selected.Count > 0 && selected[0] == selectedEntity)
                    {
                        map.Destroy(selectedEntity);
                        selectedEntity = null;
                    }
                }

                else if (selectedEntity != null)
                {
                    if (InputState.IsButtonDown(MouseButtons.Left))
                    {
                        var delta = worldMousePos - lastWorldPos;
                        selectedEntity.Position += delta;
                    }

                    if (InputState.IsButtonDown(Keys.R))
                    {
                        var diff = worldMousePos - selectedEntity.Position;
                        diff.Normalize();
                        selectedEntity.Direction = diff;
                    }

                    if (InputState.IsPress(Keys.Delete))
                    {
                        map.Destroy(selectedEntity);
                        selectedEntity = null;
                    }
                    //todo
                }
            }

            #endregion

            camera.Scale = MathHelper.Clamp(camera.Scale, 0.1f, 10f); //todo: make global and move to some game settings

            lastWorldPos = worldMousePos;

            if (selectedEntity != null && currentMode == EditorMode.Entities)
                selectedEntity.OutlineColor = Color.YellowGreen;
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

        void MapLineRect(Rectangle Rect, Color Color)
        {
            map.DrawLine(new Vector2(Rect.Left, Rect.Top), new Vector2(Rect.Right, Rect.Top), Color);
            map.DrawLine(new Vector2(Rect.Left, Rect.Top), new Vector2(Rect.Left, Rect.Bottom), Color);
            map.DrawLine(new Vector2(Rect.Right, Rect.Top), new Vector2(Rect.Right, Rect.Bottom), Color);
            map.DrawLine(new Vector2(Rect.Left, Rect.Bottom), new Vector2(Rect.Right, Rect.Bottom), Color);
        }

        public override void Draw(GameTime Time)
        {
            if (map == null)
            {
                GraphicsDevice.Clear(new Color(24, 24, 24)); //replace w/ graphic maybe
                sbatch.Begin();

                var noMapStr = new [] {
                    "No map loaded",
                    " ",
                    "Press Ctrl+N to create a new map",
                    "or Ctrl+O to load one"
                };
                float h = 0;
                var   sz = new Vector2[noMapStr.Length];
                for (var i = 0; i < noMapStr.Length; ++i)
                {
                    var mz = largeFont.MeasureString(noMapStr[i]);
                    sz[i] = mz;
                    h += mz.Y;
                }
                h = (GraphicsDevice.Viewport.Height - h) / 2;
                for (var i = 0; i < noMapStr.Length; ++i)
                {
                    largeFont.Draw(sbatch, noMapStr[i], new Vector2((GraphicsDevice.Viewport.Width - sz[i].X) / 2, h), Color.White);
                    h += sz[i].Y;
                }
                sbatch.End();
                return;
            }

            //draw border around map
            MapLineRect(new Rectangle(0, 0, map.Width * map.TileSize, map.Height * map.TileSize), Color.CornflowerBlue);

            camera.Draw();

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            //fps
            var sFps = (1 / Time.ElapsedGameTime.TotalSeconds).ToString("N2");
            var sSz = tinyFont.MeasureString(sFps);
            tinyFont.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - sSz.X - 10, GraphicsDevice.Viewport.Height - sSz.Y - 10), Color.LightSteelBlue);

            var viewPos = camera.WorldToScreen(camera.ActualPosition);

            if (currentMode == EditorMode.Tiles)
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
                    var angle = (int)MathHelper.ToDegrees((float)System.Math.Atan2(-diff.Y, diff.X));
                    if (angle < 0)
                        angle += 360;
                    map.DrawLine(savedWorldPos, lastWorldPos, Color.GreenYellow);
                    tinyFont.Draw(sbatch, string.Format("x:{0} y:{1} deg:{2}", diff.X, diff.Y, angle), camera.WorldToScreen(lastWorldPos) + new Vector2(10, -10), Color.White);
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

                    map.DrawLine(tl, tr, Color.GreenYellow);
                    map.DrawLine(tr, br, Color.GreenYellow);
                    map.DrawLine(br, bl, Color.GreenYellow);
                    map.DrawLine(bl, tl, Color.GreenYellow);
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
                var selectedItemRect = new Rectangle(GraphicsDevice.Viewport.Width - map.TileSize - uiMargin, uiMargin, map.TileSize, map.TileSize);
                selectors[(int)currentMode].DrawItem(Time, selectors[(int)currentMode].SelectedItem, selectedItemRect, sbatch);
                Primitives2D.DrawRect(sbatch, Color.White, selectedItemRect);
            }

            //draw basic info about entity screen
            smallFont.Draw(sbatch, $"Visible Entities: {map.ActiveEnts.Count}\nTotal Entities:   {map.TotalEntitiesCount}", new Vector2(uiMargin, 80), Color.LightSeaGreen);

            sbatch.End();
            sbatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            //draw modes
            {
                var modes = System.Enum.GetNames(typeof(EditorMode));
                var modeSz = new Vector2[modes.Length];
                var modeTotalWidth = 0f;
                var modeMaxHeight = 0f;
                for (var i = 0; i < modes.Length; i++)
                {
                    modeSz[i] = largeFont.MeasureString(modes[i]);
                    modeSz[i].X += uiMargin;
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
                        CenterInRect(font.MeasureString(str), new Rectangle(startX, uiMargin, (int)modeSz[i].X, (int)modeSz[i].Y)),
                        isSel ? Color.White : new Color(1, 1, 1, 0.5f)
                    );
                    startX += (int)modeSz[i].X;
                }
            }

            sbatch.End();
        }

        static readonly Matrix arrowWingTransform = Matrix.CreateRotationZ(120);

        protected void DrawArrow(Vector2 Position, Vector2 Direction, float Magnitude)
        {
            var tip = Position + (Direction * Magnitude);
            map.DrawLine(Position, tip, Color.Yellow);

            Magnitude = MathHelper.Clamp(Magnitude * 0.333f, 5, 30);
            map.DrawLine(tip, tip - (Magnitude * Vector2.Transform(Direction, arrowWingTransform)), Color.Yellow);
            map.DrawLine(tip, tip - (Magnitude * Vector2.Transform(Direction, Matrix.Invert(arrowWingTransform))), Color.Yellow);
        }

        static readonly string[] entInfoKeys = { "Name", "ID", "Type", "Position", "State" };
        protected void DrawEntInfo(Takai.Game.Entity Ent)
        {
            var props = new[] { Ent.Name, Ent.Id.ToString(), Ent.GetType().Name, Ent.Position.ToString(), Ent.State.ToString() };
            var font = tinyFont;

            var lineHeight = font.Characters[' '].Height;
            var totalHeight = (entInfoKeys.Length * lineHeight) + 10;

            var maxWidth = 0;
            for (var i = 0; i < entInfoKeys.Length; ++i)
            {
                var sz = font.Draw(sbatch, entInfoKeys[i] + ": ", new Vector2(10, GraphicsDevice.Viewport.Height - totalHeight + (lineHeight * i)), Color.White);
                maxWidth = MathHelper.Max(maxWidth, (int)sz.X);
            }

            for (var i = 0; i < props.Length; ++i)
                font.Draw(sbatch, props[i] ?? "Null", new Vector2(10 + maxWidth, GraphicsDevice.Viewport.Height - totalHeight + (lineHeight * i)), props[i] == null ? Color.Gray : Color.White);
        }
    }
}