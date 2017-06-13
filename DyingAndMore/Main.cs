//Main.cs

using System.Runtime.InteropServices;
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

            IsFixedTimeStep = false;
        }

        void gdm_DeviceCreated(object sender, System.EventArgs e)
        {
            Takai.Data.Serializer.LoadTypesFrom(System.Reflection.Assembly.GetEntryAssembly());

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

            Takai.Runtime.Game = this;
            Takai.AssetManager.Initialize(GraphicsDevice, "Data\\");

            sbatch = new SpriteBatch(GraphicsDevice);

            var map = new Takai.Game.Map()
            {
                updateSettings = Takai.Game.MapUpdateSettings.Editor
            };
            map.InitializeGraphics();

            //var state = new Editor.Editor();
            //GameManager.PushState(state);

            var smallFont = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>(
                "Fonts/UISmall.bfnt");

            testAutoObj = new Takai.Graphics.Sprite();
            var testAutoUi = Takai.UI.Static.GeneratePropSheet(
                testAutoObj, smallFont, Color.White);
            var box = new Takai.UI.ScrollBox() { Position = new Vector2(50) };
            box.AddChild(testAutoUi);
            box.Size = new Vector2(300, 400);
            box.OutlineColor = Color.LightBlue;
            ui = new Takai.UI.Static(box);
            ui.Font = smallFont;

            //ui = new Editor.Editor();

            //Takai.UI.Static.DebugFont = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>("Fonts/RCT2.bfnt");

            base.Initialize();
        }
        object testAutoObj;

        protected override void OnExiting(object sender, System.EventArgs args)
        {
        }

        protected override void Update(GameTime gameTime)
        {
            if (Takai.Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Q)
            && Takai.Input.InputState.IsMod(Takai.Input.KeyMod.Control))
                Takai.Runtime.IsExiting = true;

            if (Takai.Runtime.IsExiting)
            {
                Exit();
                return;
            }

            if (Takai.Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Down))
                offset.Y -= 2;

            if (Takai.Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Up))
                offset.Y += 2;

            if (Takai.Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.F12))
                takingScreenshot = true;

            Takai.Input.InputState.Update(GraphicsDevice.Viewport.Bounds);

            ui.Update(gameTime);
            ui.Bounds = GraphicsDevice.Viewport.Bounds;
        }

        Point offset = new Point(0, 0);
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            if (takingScreenshot)
            {
                takingScreenshot = false;

                RenderTarget2D rt = new RenderTarget2D(GraphicsDevice,
                    GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

                GraphicsDevice.SetRenderTarget(rt);
                GraphicsDevice.SetRenderTarget(null);

                var fs = new System.IO.FileStream(System.DateTime.Now.ToString("dd_MMM_HH-mm-ss-fff") + ".png", System.IO.FileMode.Create);
                rt.SaveAsPng(fs, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                fs.Close();
                rt.Dispose();
            }
            else
            {
                sbatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                ui.Draw(sbatch);

                //var rect = new Rectangle(100, 100, 100, 50);

                //Takai.Graphics.Primitives2D.DrawRect(sbatch, Color.Orange, rect);
                //ui.Font.Draw(sbatch, "abc\ndef\nghi", 0, -1, rect, offset, Color.White);

                //rect.X += rect.Width + 20;
                //Takai.Graphics.Primitives2D.DrawRect(sbatch, Color.Gray, rect);
                //rect.Height += 1000;
                //ui.Font.Draw(sbatch, "abc\ndef\nghi", (rect.Location + offset).ToVector2(), Color.Gray);

                //ui.Font.Draw(sbatch, offset.ToString(), new Vector2(20), Color.Aqua);

                sbatch.End();
            }
        }
    }
}