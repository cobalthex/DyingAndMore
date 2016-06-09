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

            fnt = sharedAssets.Load<Takai.Graphics.BitmapFont>("fnt", "Fonts/DebugFont.bfnt");

            sbatch = new SpriteBatch(graphicsDevice);
        }

        public override void Update(GameTime Time)
        {
            player.Think(Time);

            if (Takai.Input.InputCatalog.IsKeyPress(Microsoft.Xna.Framework.Input.Keys.Q))
                Takai.States.StateManager.Exit();
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
            
            pgfx.Draw(sbatch, player.Position);

            fnt.Draw(sbatch, player.Velocity.Length().ToString(), new Vector2(10), Color.White);
            sbatch.End();
        }
    }
}
