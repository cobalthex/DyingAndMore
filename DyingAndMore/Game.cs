using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore
{
    class Game : Takai.States.State
    {
        Entities.Player player;

        Takai.Graphics.BitmapFont fnt;

        SpriteBatch sbatch;
        Effect postEffect = null;
        RenderTarget2D renderTarget;

        Texture2D background;

        Takai.Game.Map map;

        public Game()
            : base(Takai.States.StateType.Full)
        {

        }

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
            player.OutlineColor = Color.Blue;

            var ent = map.SpawnEntity<Entities.Actor>(new Vector2(40), Vector2.UnitX, Vector2.Zero);
            var sprite = new Takai.Graphics.Graphic
            (
                Takai.AssetManager.Load<Texture2D>("Textures/Astrovirus.png"),
                new Point(42, 42),
                null,
                null,
                6,
                System.TimeSpan.FromMilliseconds(30),
                Takai.AnimationOptions.Loop | Takai.AnimationOptions.StartImmediately,
                Takai.Graphics.TweenStyle.Sequentially
            );
            ent.Radius = 24;
            sprite.origin = new Vector2(sprite.Width / 2, sprite.Height / 2);
            ent.Sprite = sprite;

            sbatch = new SpriteBatch(GraphicsDevice);

            //postEffect = Takai.AssetManager.Load<Effect>("Shaders/test.mgfx");
            //background = Takai.AssetManager.Load<Texture2D>("Textures/Background.png");
             
            renderTarget = new RenderTarget2D
            (
                GraphicsDevice,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height,
                false,
                SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8
            );
        }

        public override void Update(GameTime Time)
        {
            map.Update(Time, player.Position, GraphicsDevice.Viewport.Bounds);

            if (Takai.Input.InputCatalog.IsKeyPress(Microsoft.Xna.Framework.Input.Keys.Q))
                Takai.States.StateManager.Exit();
            
            var dir = Takai.Input.InputCatalog.MouseState.Position.ToVector2();
            dir -= new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height) / 2;
            //dir -= player.Position;
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
            GraphicsDevice.SetRenderTarget(renderTarget);
            sbatch.Begin(SpriteSortMode.Deferred);
            
            map.Draw(player.Position, GraphicsDevice.Viewport.Bounds);

            fnt.Draw(sbatch, player.Velocity.Length().ToString("N2"), new Vector2(10), Color.White);

            var sFps = (1 / Time.ElapsedGameTime.TotalSeconds).ToString("N2");
            fnt.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - fnt.MeasureString(sFps).X - 10, 10), Color.LightSteelBlue);

            sbatch.End();
            GraphicsDevice.SetRenderTarget(null);
            
            //post fx
            sbatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, postEffect, null);
            sbatch.Draw(renderTarget, GraphicsDevice.Viewport.Bounds, Color.White);
            sbatch.End();
        }
    }
}
