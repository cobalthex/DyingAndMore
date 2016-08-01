using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore
{
    class Editor : Takai.States.State
    {
        Takai.Game.Camera camera;

        Takai.Graphics.BitmapFont fnt;
        string debugText = "";

        SpriteBatch sbatch;
        
        Takai.Game.Map map;

        public Editor() : base(Takai.States.StateType.Full) { }

        public override void Load()
        {
            fnt = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/Debug.bfnt");
            
            sbatch = new SpriteBatch(GraphicsDevice);

            map = new Takai.Game.Map(GraphicsDevice);
            map.debugOptions.showProfileInfo = true;
            map.debugOptions.showEntInfo = true;
            map.DebugFont = fnt;

            using (var stream = new System.IO.FileStream("test.map.tk", System.IO.FileMode.Open))
                map.Load(stream);
            
            camera = new Takai.Game.Camera(map, null);
            camera.MoveSpeed = 1600;
            camera.Viewport = GraphicsDevice.Viewport.Bounds;
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
                camera.Position += d * camera.MoveSpeed * (float)Time.ElapsedGameTime.TotalSeconds;
            }

            camera.Update(Time);
            
            if (InputCatalog.IsMouseClick(InputCatalog.MouseButton.Left) && InputCatalog.KBState.IsKeyDown(Keys.LeftControl))
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
                        ent.Position = camera.ScreenToWorld(InputCatalog.MouseState.Position.ToVector2());
                        map.SpawnEntity(ent);
                    }
                }
            }

            else if (InputCatalog.MouseState.RightButton == ButtonState.Pressed && InputCatalog.KBState.IsKeyDown(Keys.LeftControl))
            {
                var pos = camera.ScreenToWorld(InputCatalog.MouseState.Position.ToVector2());
                if (map.IsInside(pos))
                {
                    var tile = (pos / map.TileSize).ToPoint();
                    map.Tiles[tile.Y, tile.X] = -1;
                }
            }

            foreach (var ent in highlighted)
                ent.OutlineColor = Color.Transparent;
            highlighted = map.FindNearbyEntities(camera.ScreenToWorld(InputCatalog.MouseState.Position.ToVector2()), 5);
            foreach (var ent in highlighted)
                ent.OutlineColor = Color.Yellow;
        }
        System.Collections.Generic.List<Takai.Game.Entity> highlighted = new System.Collections.Generic.List<Takai.Game.Entity>();

        public override void Draw(Microsoft.Xna.Framework.GameTime Time)
        {
            camera.Draw();

            sbatch.Begin(SpriteSortMode.Deferred);

            fnt.Draw(sbatch, debugText, new Vector2(10), Color.CornflowerBlue);
            var sFps = (1 / Time.ElapsedGameTime.TotalSeconds).ToString("N2");
            fnt.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - fnt.MeasureString(sFps).X - 10, 10), Color.LightSteelBlue);
            var view = camera.WorldToScreen(camera.ActualPosition + camera.Offset);

            Takai.Graphics.Primitives2D.DrawRect(sbatch, Color.CornflowerBlue, new Rectangle((int)view.X, (int)view.Y, map.Width * map.TileSize, map.Height * map.TileSize));
            
            sbatch.End();
        }
    }
}
