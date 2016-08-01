using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore
{
    class Editor : Takai.States.State
    {
        Entities.Actor player;
        Entities.Actor testEnt;

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
            
            map.debugOptions.showProfileInfo = true;
            map.debugOptions.showEntInfo = true;

            camera = new Takai.Game.Camera(map, null);
            camera.MoveSpeed = 800;
            camera.Viewport = GraphicsDevice.Viewport.Bounds;
            //camera.PostEffect = Takai.AssetManager.Load<Effect>("Shaders/Fisheye.mgfx");

            var tex = Takai.AssetManager.Load<Texture2D>("Textures/SparkParticle.png");
            map.AddDecal(tex, new Vector2(200, 100));
            map.AddDecal(tex, new Vector2(300, 120), 0, 2);
        }

        public override void Update(GameTime Time)
        {
            if (Takai.Input.InputCatalog.IsKeyPress(Keys.Q))
                Takai.States.StateManager.Exit();

            if (Takai.Input.InputCatalog.IsKeyPress(Keys.F1))
                map.debugOptions.showProfileInfo ^= true;
            if (Takai.Input.InputCatalog.IsKeyPress(Keys.F2))
                map.debugOptions.showEntInfo ^= true;
            if (Takai.Input.InputCatalog.IsKeyPress(Keys.F3))
                map.debugOptions.showBlobReflectionMask ^= true;
            if (Takai.Input.InputCatalog.IsKeyPress(Keys.F4))
                map.debugOptions.showOnlyReflections ^= true;

            var d = Vector2.Zero;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Keys.A))
                d -= Vector2.UnitX;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Keys.W))
                d -= Vector2.UnitY;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Keys.D))
                d += Vector2.UnitX;
            if (Takai.Input.InputCatalog.KBState.IsKeyDown(Keys.S))
                d += Vector2.UnitY;

            if (camera.Follow != null)
                player.Move(d);
            
            if (camera.Follow == null)
            {
                if (d != Vector2.Zero)
                    d.Normalize();
                camera.Position += d * camera.MoveSpeed * (float)Time.ElapsedGameTime.TotalSeconds;
            }

            camera.Update(Time);

            if (isEditMode)
            {
                if (Takai.Input.InputCatalog.IsMouseClick(Takai.Input.InputCatalog.MouseButton.Left) && Takai.Input.InputCatalog.KBState.IsKeyDown(Keys.LeftControl))
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
                            ent.Position = camera.ScreenToWorld(Takai.Input.InputCatalog.MouseState.Position.ToVector2());
                            map.SpawnEntity(ent);
                        }
                    }
                }

                else if (Takai.Input.InputCatalog.MouseState.RightButton == ButtonState.Pressed && Takai.Input.InputCatalog.KBState.IsKeyDown(Keys.LeftControl))
                {
                    var pos = camera.ScreenToWorld(Takai.Input.InputCatalog.MouseState.Position.ToVector2());
                    if (map.IsInside(pos))
                    {
                        var tile = (pos / map.TileSize).ToPoint();
                        map.Tiles[tile.Y, tile.X] = -1;
                    }
                }
            }

            foreach (var ent in highlighted)
                ent.OutlineColor = Color.Transparent;
            highlighted = map.FindNearbyEntities(camera.ScreenToWorld(Takai.Input.InputCatalog.MouseState.Position.ToVector2()), 5);
            foreach (var ent in highlighted)
                ent.OutlineColor = Color.Yellow;
        }
        System.Collections.Generic.List<Takai.Game.Entity> highlighted = new System.Collections.Generic.List<Takai.Game.Entity>();
        bool isEditMode = true;

        public override void Draw(Microsoft.Xna.Framework.GameTime Time)
        {
            camera.Draw();

            sbatch.Begin(SpriteSortMode.Deferred);

            fnt.Draw(sbatch, debugText, new Vector2(10), Color.CornflowerBlue);
            var sFps = (1 / Time.ElapsedGameTime.TotalSeconds).ToString("N2");
            fnt.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - fnt.MeasureString(sFps).X - 10, 10), Color.LightSteelBlue);
            
            sbatch.End();
            
            testEnt.OutlineColor = Color.Transparent;
        }
    }
}
