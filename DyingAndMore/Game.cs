using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore
{
    class Game : Takai.States.State
    {
        Entities.Player player;

        Takai.Graphics.BitmapFont fnt;

        SpriteBatch sbatch;
        
        Takai.Game.Map map;

        public Game()
            : base(Takai.States.StateType.Full)
        {

        }

        Entities.Actor ent;
        public override void Load()
        {
            fnt = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/rct2.bfnt");

            map = Takai.Game.Map.FromCsv(GraphicsDevice, "data/maps/test.csv");
            map.TilesImage = Takai.AssetManager.Load<Texture2D>("Textures/Tiles.png");
            map.TileSize = 48;
            map.BuildMask(map.TilesImage, true);

            map.DebugFont = fnt;
            map.decal = Takai.AssetManager.Load<Texture2D>("Textures/sparkparticle.png");

            player = map.SpawnEntity<Entities.Player>(new Vector2(100), Vector2.UnitX, Vector2.Zero);

            ent = map.SpawnEntity<Entities.Actor>(new Vector2(40), Vector2.UnitX, Vector2.Zero);
            var sprite = new Takai.Graphics.Graphic
            (
                Takai.AssetManager.Load<Texture2D>("Textures/Astrovirus.png"),
                42,
                42,
                6,
                System.TimeSpan.FromMilliseconds(30),
                true
            );
            ent.Radius = sprite.Width / 2;
            sprite.CenterOrigin();
            ent.Sprite = sprite;

            sbatch = new SpriteBatch(GraphicsDevice);
            
            map.debugOptions.showProfileInfo = true;
            //map.PostEffect = Takai.AssetManager.Load<Effect>("Shaders/Fisheye.mgfx");
        }

        public override void Update(GameTime Time)
        {
            map.Update(Time, player.Position, GraphicsDevice.Viewport.Bounds);

            if (Takai.Input.InputCatalog.IsKeyPress(Microsoft.Xna.Framework.Input.Keys.Q))
                Takai.States.StateManager.Exit();

            if (Takai.Input.InputCatalog.IsKeyPress(Microsoft.Xna.Framework.Input.Keys.F1))
                map.debugOptions.showProfileInfo = !map.debugOptions.showProfileInfo;
            if (Takai.Input.InputCatalog.IsKeyPress(Microsoft.Xna.Framework.Input.Keys.F2))
                map.debugOptions.showBlobReflectionMask = !map.debugOptions.showBlobReflectionMask;
            if (Takai.Input.InputCatalog.IsKeyPress(Microsoft.Xna.Framework.Input.Keys.F3))
                map.debugOptions.showOnlyReflections = !map.debugOptions.showOnlyReflections;

            var dir = Takai.Input.InputCatalog.MouseState.Position.ToVector2();
            dir -= new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height) / 2;
            //dir -= player.Position;
            dir.Normalize();
            player.Direction = dir;

            float t;
            var tgt = map.TraceLine(player.Position, player.Direction, out t);
            if (tgt != null)
                tgt.OutlineColor = Color.Gold;
            debugText = t.ToString("N2");

            cpoint = player.Position + player.Direction * t;
        }
        string debugText;

        Vector2 cpoint;

        /// <summary>
        /// Draw the map, centerd around the camera
        /// </summary>
        /// <param name="Spritebatch">Sprite batch to draw with</param>
        /// <param name="Camera">The position of the camera (in map)</param>
        /// <param name="Bounds">The bounds of the viewport</param>
        public override void Draw(Microsoft.Xna.Framework.GameTime Time)
        {
            map.Draw(player.Position, GraphicsDevice.Viewport.Bounds);

            sbatch.Begin(SpriteSortMode.Deferred);

            fnt.Draw(sbatch, debugText, new Vector2(10), Color.CornflowerBlue);
            var sFps = (1 / Time.ElapsedGameTime.TotalSeconds).ToString("N2");
            fnt.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - fnt.MeasureString(sFps).X - 10, 10), Color.LightSteelBlue);
            
            sbatch.End();
            
            ent.OutlineColor = Color.Transparent;
        }
    }
}
