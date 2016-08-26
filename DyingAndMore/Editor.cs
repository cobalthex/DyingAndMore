using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.Graphics;

namespace DyingAndMore
{
    enum EditorMode
    {
        Tiles,
        Decals,
        Blobs,
        Entities,

        Count
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

        TileSelector tileSelector;
        DecalSelector decalSelector;

        bool isPosSaved = false;
        Vector2 savedWorldPos, lastWorldPos;
        float startRotation, startScale;

        DecalIndex? selectedDecal = null;
        Takai.Game.Entity selectedEntity = null; 
        
        public Editor() : base(Takai.States.StateType.Full) { }
        
        public override void Load()
        {
            fnt = Takai.AssetManager.Load<BitmapFont>("Fonts/rct2.bfnt");

            smallFont = Takai.AssetManager.Load<BitmapFont>("Fonts/UISmall.bfnt");
            largeFont = Takai.AssetManager.Load<BitmapFont>("Fonts/UILarge.bfnt");

            sbatch = new SpriteBatch(GraphicsDevice);

            map = new Takai.Game.Map(GraphicsDevice);
            map.debugOptions.showProfileInfo = true;
            map.debugOptions.showEntInfo = true;
            map.DebugFont = fnt;

            using (var stream = new System.IO.FileStream("Data/Maps/test.map.tk", System.IO.FileMode.Open))
                map.Load(stream);
            
            camera = new Takai.Game.Camera(map, null);
            camera.MoveSpeed = 1600;
            camera.Viewport = GraphicsDevice.Viewport.Bounds;

            tileSelector = new TileSelector(this);
            Takai.States.StateManager.PushState(tileSelector);
            tileSelector.Deactivate();

            decalSelector = new DecalSelector(this);
            Takai.States.StateManager.PushState(decalSelector);
            decalSelector.Deactivate();
            
            //todo: load all ent defs in ents folder (in ent selector)
        }
        
        public override void Update(GameTime Time)
        {
            if (InputCatalog.IsKeyPress(Keys.Q))
                Takai.States.StateManager.Exit();

            if (InputCatalog.IsKeyPress(Keys.F1))
                map.debugOptions.showProfileInfo ^= true;
            if (InputCatalog.IsKeyPress(Keys.F2))
                map.debugOptions.showEntInfo ^= true;
            if (InputCatalog.IsKeyPress(Keys.F3))
                map.debugOptions.showBlobReflectionMask ^= true;
            if (InputCatalog.IsKeyPress(Keys.F4))
                map.debugOptions.showOnlyReflections ^= true;
            
            if (InputCatalog.IsKeyPress(Keys.D1) || InputCatalog.IsKeyPress(Keys.NumPad1))
                currentMode = EditorMode.Tiles;
            if (InputCatalog.IsKeyPress(Keys.D2) || InputCatalog.IsKeyPress(Keys.NumPad2))
                currentMode = EditorMode.Decals;
            if (InputCatalog.IsKeyPress(Keys.D3) || InputCatalog.IsKeyPress(Keys.NumPad3))
                currentMode = EditorMode.Blobs;
            if (InputCatalog.IsKeyPress(Keys.D4) || InputCatalog.IsKeyPress(Keys.NumPad4))
                currentMode = EditorMode.Entities;

            if (InputCatalog.MouseState.MiddleButton == ButtonState.Pressed)
                camera.MoveTo(camera.Position + ((InputCatalog.LastMouseState.Position.ToVector2() - InputCatalog.MouseState.Position.ToVector2()) * (1 / camera.Scale)));
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
            {
                switch (currentMode)
                {
                    case EditorMode.Tiles:
                        tileSelector.Activate();
                        break;
                    case EditorMode.Decals:
                        decalSelector.Activate();
                        break;
                }
            }

            if (InputCatalog.HasMouseScrolled())
            {
                camera.Scale += (InputCatalog.MouseState.ScrollWheelValue - InputCatalog.LastMouseState.ScrollWheelValue) / 1024f;
                camera.Scale = MathHelper.Clamp(camera.Scale, 0.1f, 2f);
                //todo: translate to mouse cursor
            }

            camera.Update(Time);
            var worldMousePos = camera.ScreenToWorld(InputCatalog.MouseState.Position.ToVector2());

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

            if (currentMode == EditorMode.Tiles)
            {
                var tile = short.MinValue;

                if (InputCatalog.MouseState.LeftButton == ButtonState.Pressed)
                    tile = (short)tileSelector.SelectedItem;
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
            else if (currentMode == EditorMode.Decals)
            {
                if (InputCatalog.IsMousePress(InputCatalog.MouseButton.Left))
                {
                    if (!SelectDecal(worldMousePos))
                    {
                        map.AddDecal(decalSelector.textures[decalSelector.SelectedItem], worldMousePos);
                        var pos = (worldMousePos / map.SectorPixelSize).ToPoint();
                        selectedDecal = new DecalIndex { x = pos.X, y = pos.Y, index = map.Sectors[pos.Y, pos.X].decals.Count - 1 };
                    }
                    else
                    {
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

                        decal.scale += (dist - startScale) / 25;
                        startScale = dist;
                    }

                    map.Sectors[selectedDecal.Value.y, selectedDecal.Value.x].decals[selectedDecal.Value.index] = decal;
                }
            }

            foreach (var ent in highlighted)
                ent.OutlineColor = Color.Transparent;
            highlighted = map.FindNearbyEntities(worldMousePos, 5);
            foreach (var ent in highlighted)
                ent.OutlineColor = highlightColor;

            lastWorldPos = worldMousePos;

        }
        System.Collections.Generic.List<Takai.Game.Entity> highlighted = new System.Collections.Generic.List<Takai.Game.Entity>();

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

            var selectedItemRect = new Rectangle(GraphicsDevice.Viewport.Width - map.TileSize - 20, 20, map.TileSize, map.TileSize);
            if (currentMode == EditorMode.Tiles)
            {
                //draw selected tile in corner of screen
                sbatch.Draw
                (
                    map.TilesImage,
                    selectedItemRect,
                    new Rectangle((tileSelector.SelectedItem % map.TilesPerRow) * map.TileSize, (tileSelector.SelectedItem / map.TilesPerRow) * map.TileSize, map.TileSize, map.TileSize),
                    Color.White
                );

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
                sbatch.Draw
                (
                       decalSelector.textures[decalSelector.SelectedItem],
                       selectedItemRect,
                       Color.White
                );
                
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
            Primitives2D.DrawRect(sbatch, Color.White, selectedItemRect);

            //draw modes
            {
                var modeSz = new Vector2[(int)EditorMode.Count];
                var modeTotalWidth = 0f;
                var modeMaxHeight = 0f;
                for (var i = 0; i < (int)EditorMode.Count; i++)
                {
                    modeSz[i] = largeFont.MeasureString(System.Enum.GetName(typeof(EditorMode), i));
                    modeSz[i].X += 20;
                    modeTotalWidth += modeSz[i].X;
                    modeMaxHeight = MathHelper.Max(modeMaxHeight, modeSz[i].Y);
                }

                var startX = (int)(GraphicsDevice.Viewport.Width - modeTotalWidth) / 2;
                for (var i = 0; i < (int)EditorMode.Count; i++)
                {
                    var isSel = (int)currentMode == i;
                    var font = isSel ? largeFont : smallFont;
                    var str = System.Enum.GetName(typeof(EditorMode), i);
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
    }
}
