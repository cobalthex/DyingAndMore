using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore
{
    enum EditorMode
    {
        Tiles,
        Decals,
        Blobs,
        Entities
    }

    class Editor : Takai.States.State
    {
        EditorMode currentMode = EditorMode.Tiles;

        Takai.Game.Camera camera;

        Takai.Graphics.BitmapFont fnt;
        string debugText = "";

        SpriteBatch sbatch;
        
        public Takai.Game.Map map;
        public short selectedTile = 0;

        Color highlightColor = Color.Gold;

        TileSelector tileSelector;

        bool isPosSaved = false;
        Vector2 savedWorldPos, lastWorldPos;

        float zoom = 1;

        public Editor() : base(Takai.States.StateType.Full) { }
        
        public override void Load()
        {
            fnt = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/Debug.bfnt");
            
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

            if (InputCatalog.MouseState.MiddleButton == ButtonState.Pressed)
                camera.MoveTo(camera.Position + InputCatalog.lastMouseState.Position.ToVector2() - InputCatalog.MouseState.Position.ToVector2());
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
                    d.Normalize();

                float mvs = camera.MoveSpeed;

                camera.Position += d * camera.MoveSpeed * (float)Time.ElapsedGameTime.TotalSeconds; //(camera velocity)
            }

            if (InputCatalog.IsKeyPress(Keys.Tab))
                tileSelector.Activate();

            if (InputCatalog.HasMouseScrolled())
            {
                zoom += (InputCatalog.MouseState.ScrollWheelValue - InputCatalog.lastMouseState.ScrollWheelValue) / 1024f;
                zoom = MathHelper.Clamp(zoom, 0.1f, 2f);
                camera.Transform = Matrix.CreateScale(zoom);
                //todo: translate to mouse cursor
            }

            camera.Update(Time);
            var worldMousePos = camera.ScreenToWorld(Vector2.Transform(InputCatalog.MouseState.Position.ToVector2(), Matrix.Invert(camera.Transform)));

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
                savedWorldPos = camera.ScreenToWorld(Vector2.Transform(InputCatalog.MouseState.Position.ToVector2(), Matrix.Invert(camera.Transform)));
            }
            else if (InputCatalog.IsKeyClick(Keys.LeftControl))
                isPosSaved = false;

            if (currentMode == EditorMode.Tiles)
            {
                if (isPosSaved)
                {
                    if (InputCatalog.IsMousePress(InputCatalog.MouseButton.Left))
                    {
                        TileLine(savedWorldPos, worldMousePos, selectedTile);
                        savedWorldPos = worldMousePos;
                    }
                    else if (InputCatalog.IsMousePress(InputCatalog.MouseButton.Right))
                    {
                        TileLine(savedWorldPos, worldMousePos, -1);
                        savedWorldPos = worldMousePos;
                    }
                }
                
                if (InputCatalog.MouseState.LeftButton == ButtonState.Pressed)
                {
                    if (map.IsInside(worldMousePos))
                        TileLine(lastWorldPos, worldMousePos, selectedTile);
                }
                else if (InputCatalog.MouseState.RightButton == ButtonState.Pressed)
                {
                    if (map.IsInside(worldMousePos))
                        TileLine(lastWorldPos, worldMousePos, -1);
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

        void MapDrawRect(Rectangle Rect, Color Color)
        {
            map.DebugLine(new Vector2(Rect.Left, Rect.Top), new Vector2(Rect.Right, Rect.Top), Color);
            map.DebugLine(new Vector2(Rect.Left, Rect.Top), new Vector2(Rect.Left, Rect.Bottom), Color);
            map.DebugLine(new Vector2(Rect.Right, Rect.Top), new Vector2(Rect.Right, Rect.Bottom), Color);
            map.DebugLine(new Vector2(Rect.Left, Rect.Bottom), new Vector2(Rect.Right, Rect.Bottom), Color);
        }

        public override void Draw(GameTime Time)
        {
            //draw border around map
            MapDrawRect(new Rectangle(0, 0, map.Width * map.TileSize, map.Height * map.TileSize), Color.CornflowerBlue);

            camera.Draw();

            sbatch.Begin(SpriteSortMode.Deferred);

            fnt.Draw(sbatch, debugText, new Vector2(10), Color.CornflowerBlue);
            var sFps = (1 / Time.ElapsedGameTime.TotalSeconds).ToString("N2");
            fnt.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - fnt.MeasureString(sFps).X - 10, 10), Color.LightSteelBlue);

            var viewPos = camera.WorldToScreen(camera.ActualPosition + camera.Offset);
            var worldMousePos = camera.ScreenToWorld(Vector2.Transform(InputCatalog.MouseState.Position.ToVector2(), Matrix.Invert(camera.Transform)));

            if (currentMode == EditorMode.Tiles)
            {
                //draw selected tile in corner of screen
                var tilePos = new Vector2(GraphicsDevice.Viewport.Width - map.TileSize - 20, 20);
                sbatch.Draw
                (
                    map.TilesImage,
                    tilePos,
                    new Rectangle((selectedTile % map.TilesPerRow) * map.TileSize, (selectedTile / map.TilesPerRow) * map.TileSize, map.TileSize, map.TileSize),
                    Color.White
                );
                Takai.Graphics.Primitives2D.DrawRect(sbatch, Color.White, new Rectangle((int)tilePos.X, (int)tilePos.Y, map.TileSize, map.TileSize));

                //draw rect around tile under cursor
                if (map.IsInside(worldMousePos))
                {
                    MapDrawRect
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
                    map.DebugLine(savedWorldPos, worldMousePos, Color.GreenYellow);
                    var diff = worldMousePos - savedWorldPos;
                    var angle = (int)MathHelper.ToDegrees((float)System.Math.Atan2(-diff.Y, diff.X));
                    if (angle < 0)
                        angle += 360;
                    fnt.Draw(sbatch, string.Format("x:{0} y:{1} deg:{2}", diff.X, diff.Y, angle), Vector2.Transform(viewPos + worldMousePos, camera.Transform) + new Vector2(10, -10), Color.White);
                } 
            }

            sbatch.End();
        }
    }
}
