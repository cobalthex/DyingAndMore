//Main.cs
//Copyright Dejitaru Forge 2012

using Takai.States;

//empty class to test on pc with xbox settings
#if XBOX && WINDOWS
namespace Microsoft.Xna.Framework.GamerServices { class Guide { public static bool IsTrialMode = false; public static void ShowMarketplace(PlayerIndex p) { } } }
#endif

namespace DyingAndMore
{
    #region startup (Windows/Xbox/Zune)
#if !WINDOWS_PHONE
    static class Program
    {
        /// <summary>
        /// The main entry point for the game
        /// </summary>
#if WINDOWS
        [System.STAThread]
#endif
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
#endif
    #endregion

    /// <summary>
    /// The game
    /// </summary>
    public class DyingAndMoreGame : Microsoft.Xna.Framework.Game
    {
        Microsoft.Xna.Framework.GraphicsDeviceManager gdm;

        bool useCustomCursor = true;
        bool takingScreenshot = false;

        /// <summary>
        /// Create the game
        /// </summary>
        public DyingAndMoreGame()
        {
#if XBOX
            Components.Add(new Microsoft.Xna.Framework.GamerServices.GamerServicesComponent(this));
#endif

            gdm = new Microsoft.Xna.Framework.GraphicsDeviceManager(this);
            gdm.DeviceCreated += new System.EventHandler<System.EventArgs>(gdm_DeviceCreated);
            gdm.PreferMultiSampling = true;

#if WINDOWS_PHONE
            TargetElapsedTime = System.TimeSpan.FromSeconds(1 / 30f); //30 fps
            gdm.SupportedOrientations = Microsoft.Xna.Framework.DisplayOrientation.LandscapeLeft | Microsoft.Xna.Framework.DisplayOrientation.LandscapeRight;
            gdm.IsFullScreen = true;
#elif WINDOWS
            TargetElapsedTime = System.TimeSpan.FromSeconds(1 / 60f); //60 fps
            gdm.PreferredBackBufferWidth = 800;
            gdm.PreferredBackBufferHeight = 600;
            IsMouseVisible = true;
#elif XBOX
            TargetElapsedTime = System.TimeSpan.FromSeconds(1 / 60f); //60 fps
            gdm.PreferredBackBufferWidth = 800;
            gdm.PreferredBackBufferHeight = 600;
#endif
        }

        void gdm_DeviceCreated(object sender, System.EventArgs e)
        {
            if (StateManager.isInitialized)
                return;

            Takai.Input.TouchAbstractor.Initialize();

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

            GraphicsDevice.BlendState = Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend;

            //load all included assets
            Takai.AssetManager asm = new Takai.AssetManager(GraphicsDevice, "Data\\");

            StateManager.Initialize(this, asm);
            StateManager.PushState(new Game());
           
            base.Initialize();
        }

        protected override void OnExiting(object sender, System.EventArgs args)
        {
            StateManager.Exit();
        }

        protected override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            StateManager.Update(gameTime);
            Takai.Input.InputCatalog.Update();
            Takai.Input.TouchAbstractor.Update();

#if WINDOWS
            if (Takai.Input.InputCatalog.IsKeyClick(Microsoft.Xna.Framework.Input.Keys.F12))
                takingScreenshot = true;
#endif
        }

        protected override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
#if WINDOWS
            if (takingScreenshot)
            {
                takingScreenshot = false;

                Microsoft.Xna.Framework.Graphics.RenderTarget2D rt = new Microsoft.Xna.Framework.Graphics.RenderTarget2D(GraphicsDevice,
                    GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, false, Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color,
                    Microsoft.Xna.Framework.Graphics.DepthFormat.Depth24Stencil8);

                GraphicsDevice.SetRenderTarget(rt);
                StateManager.Draw(gameTime);
                GraphicsDevice.SetRenderTarget(null);

                var fs = new System.IO.FileStream(System.DateTime.Now.ToString("dd_MMM_HH-mm-ss-fff") + ".png", System.IO.FileMode.Create);
                rt.SaveAsPng(fs, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                fs.Close();
                rt.Dispose();
            }
            else
#endif
                StateManager.Draw(gameTime);
        }
    }
}