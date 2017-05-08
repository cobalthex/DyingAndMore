//Main.cs

using System.Runtime.InteropServices;
using Takai.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore
{
    #region startup (Windows/Xbox/Zune)
    static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AttachConsole(int pid = -1);

        /// <summary>
        /// The main entry point for the game
        /// </summary>
        [System.STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].Equals("MakeDef", System.StringComparison.OrdinalIgnoreCase))
                {
                    AttachConsole();

                    using (var stream = new System.IO.StreamWriter(System.Console.OpenStandardOutput()))
                    {
                        stream.AutoFlush = true;
                        System.Console.SetOut(stream);

                        stream.WriteLine("Test");

                        for (int i = 1; i < args.Length; ++i)
                        {
                            if (Takai.Data.Serializer.RegisteredTypes.TryGetValue(args[i], out var type))
                            {
                                var obj = System.Activator.CreateInstance(type);
                                Takai.Data.Serializer.TextSerialize(stream, obj);
                                stream.WriteLine();
                            }
                        }

                        stream.WriteLine();
                    }
                    return;
                }
            }


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

        SpriteBatch sbatch;
        Takai.UI.Static ui;

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

            if (GameManager.IsInitialized)
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

            GameManager.Initialize(this);
            sbatch = new SpriteBatch(GraphicsDevice);

            var map = new Takai.Game.Map(GraphicsDevice)
            {
                updateSettings = Takai.Game.MapUpdateSettings.Editor
            };

            //var state = new Editor.Editor();
            //GameManager.PushState(state);

            ui = new Editor.Editor2();

            base.Initialize();
        }

        protected override void OnExiting(object sender, System.EventArgs args)
        {
            GameManager.Exit();
        }

        protected override void Update(GameTime gameTime)
        {
            if (Takai.Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.F12))
                takingScreenshot = true;

            Takai.Input.InputState.Update(GraphicsDevice.Viewport.Bounds);
            //GameManager.Update(gameTime);
            ui.Update(gameTime);
            ui.Bounds = GraphicsDevice.Viewport.Bounds;
        }

        protected override void Draw(GameTime gameTime)
        {
            if (takingScreenshot)
            {
                takingScreenshot = false;

                RenderTarget2D rt = new RenderTarget2D(GraphicsDevice,
                    GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

                GraphicsDevice.SetRenderTarget(rt);
                GameManager.Draw(gameTime);
                GraphicsDevice.SetRenderTarget(null);

                var fs = new System.IO.FileStream(System.DateTime.Now.ToString("dd_MMM_HH-mm-ss-fff") + ".png", System.IO.FileMode.Create);
                rt.SaveAsPng(fs, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                fs.Close();
                rt.Dispose();
            }
            else
            {
                sbatch.Begin();
                //GameManager.Draw(gameTime);
                ui.Draw(sbatch);
                sbatch.End();
            }
        }
    }
}