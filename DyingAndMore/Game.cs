using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore
{
    class Game : Takai.States.State
    {
        Entities.Player player;
        Takai.Graphics.Graphic pgfx;

        Takai.Graphics.BitmapFont fnt;

        SpriteBatch sbatch;

        Takai.Game.Map map;

        public Game()
            : base(Takai.States.StateType.Full)
        {

        }

        public override void Load()
        {
            player = new Entities.Player();
            player.Load();

            pgfx = new Takai.Graphics.Graphic
            (
                sharedAssets.Load<Texture2D>("Player", "Textures/Player.png"),
                new Point(48, 48),
                null,
                null,
                2,
                System.TimeSpan.FromMilliseconds(500),
                Takai.AnimationOptions.Loop | Takai.AnimationOptions.StartImmediately
            );
            pgfx.origin = new Vector2(pgfx.width / 2, pgfx.height / 2);

            fnt = sharedAssets.Load<Takai.Graphics.BitmapFont>("fnt", "Fonts/DebugFont.bfnt");

            map = new Takai.Game.Map();
            map.TilesImage = sharedAssets.Load<Texture2D>("tiles", "Textures/Tiles.png");
            map.TileSize = 48;
            map.Width = 4;
            map.Height = 4;
            map.Tiles = new ushort[4, 4]
            {
                { 0, 1, 0, 0 },
                { 0, 0, 2, 0 },
                { 0, 3, 0, 0 },
                { 0, 0, 0, 0 } 
            };

            sbatch = new SpriteBatch(graphicsDevice);
        }

        public override void Update(GameTime Time)
        {
            player.Think(Time);

            if (Takai.Input.InputCatalog.IsKeyPress(Microsoft.Xna.Framework.Input.Keys.Q))
                Takai.States.StateManager.Exit();


            //var dir = new Vector2(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height) / 2;
            var dir = Takai.Input.InputCatalog.MouseState.Position.ToVector2();
            dir -= player.Position;
            dir.Normalize();
            player.Direction = dir;
        }

        /// <summary>
        /// Draw the map, centerd around the camera
        /// </summary>
        /// <param name="Spritebatch">Sprite batch to draw with</param>
        /// <param name="Camera">The position of the camera (in map)</param>
        /// <param name="Bounds">The bounds of the viewport</param>
        public override void Draw(Microsoft.Xna.Framework.GameTime Time)
        {
            sbatch.Begin();
            graphicsDevice.Clear(Color.Black);

            map.Draw(sbatch, Vector2.Zero, graphicsDevice.Viewport.Bounds);
            
            pgfx.Draw(sbatch, player.Position, (float)System.Math.Atan2(player.Direction.Y, player.Direction.X));

            fnt.Draw(sbatch, player.Velocity.Length().ToString(), new Vector2(10), Color.White);
            fnt.Draw(sbatch, player.GetComponent<Components.HealthComponent>().MaxHealth.ToString(), new Vector2(50), Color.GreenYellow);
            sbatch.End();
        }
    }
}
