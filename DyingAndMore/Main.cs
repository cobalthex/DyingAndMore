//Main.cs

using Takai.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore
{
    #region startup (Windows/Xbox/Zune)
    static class Program
    {
        /// <summary>
        /// The main entry point for the game
        /// </summary>
        [System.STAThread]
        static void Main(string[] args)
        {
            using (DyingAndMoreGame game = new DyingAndMoreGame())
            {
#if WINDOWS
                System.Windows.Forms.Application.EnableVisualStyles();
#endif
                game.Run();
            }
        }
    }
    #endregion

    /// <summary>
    /// The game
    /// </summary>
    public class DyingAndMoreGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager gdm;

        bool useCustomCursor = false;
        bool takingScreenshot = false;

        /// <summary>
        /// Create the game
        /// </summary>
        public DyingAndMoreGame()
        {
            gdm = new GraphicsDeviceManager(this);
            gdm.DeviceCreated += new System.EventHandler<System.EventArgs>(gdm_DeviceCreated);
            gdm.PreferMultiSampling = true;

            TargetElapsedTime = System.TimeSpan.FromSeconds(1 / 60f); //60 fps
            gdm.PreferredBackBufferWidth = 1280;
            gdm.PreferredBackBufferHeight = 800;
            gdm.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            gdm.PreferMultiSampling = true;
            IsMouseVisible = true;
        }

        void gdm_DeviceCreated(object sender, System.EventArgs e)
        {
            Takai.Data.Serializer.LoadTypesFrom(System.Reflection.Assembly.GetEntryAssembly());

            if (StateManager.isInitialized)
                return;
            
            #region Mouse Cursor
#if WINDOWS
            if (useCustomCursor)
            {
                System.Drawing.Bitmap cur = new System.Drawing.Bitmap("Data/Textures/Pointer.png", true);
                System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(cur);
                System.IntPtr ptr = cur.GetHicon();
                System.Windows.Forms.Cursor c = new System.Windows.Forms.Cursor(ptr);
                System.Windows.Forms.Form.FromHandle(Window.Handle).Cursor = c;
                this.IsMouseVisible = true;
            }
#endif
            #endregion
            
            GraphicsDevice.BlendState = BlendState.AlphaBlend;

            Takai.AssetManager.Initialize(GraphicsDevice, "Data\\");

            StateManager.Initialize(this);
            StateManager.PushState(new Game.Game());

            base.Initialize();
        }

        protected override void OnExiting(object sender, System.EventArgs args)
        {
            StateManager.Exit();
        }

        protected override void Update(GameTime gameTime)
        {
            if (Takai.Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.F12))
            takingScreenshot = true;

            //gameTime.ElapsedGameTime = System.TimeSpan.FromMilliseconds(gameTime.ElapsedGameTime.TotalMilliseconds * 0.1f);
            //gameTime.TotalGameTime = System.TimeSpan.FromMilliseconds(gameTime.TotalGameTime.TotalMilliseconds * 0.1f);
            Takai.Input.InputState.Update();
            StateManager.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (takingScreenshot)
            {
                takingScreenshot = false;

                RenderTarget2D rt = new RenderTarget2D(GraphicsDevice,
                    GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

                GraphicsDevice.SetRenderTarget(rt);
                StateManager.Draw(gameTime);
                GraphicsDevice.SetRenderTarget(null);

                var fs = new System.IO.FileStream(System.DateTime.Now.ToString("dd_MMM_HH-mm-ss-fff") + ".png", System.IO.FileMode.Create);
                rt.SaveAsPng(fs, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                fs.Close();
                rt.Dispose();
            }
            else
                StateManager.Draw(gameTime);
        }
    }
}