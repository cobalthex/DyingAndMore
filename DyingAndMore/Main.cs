using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

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
                //System.Windows.Forms.Application.EnableVisualStyles();
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
            gdm = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 800,
                PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
                //PreferMultiSampling = true,
                GraphicsProfile = GraphicsProfile.HiDef,
            };
            gdm.DeviceCreated += GdmDeviceCreated;

            TargetElapsedTime = System.TimeSpan.FromSeconds(1 / 60f); //60 fps
            IsMouseVisible = true;

            IsFixedTimeStep = false;
        }

        void GdmDeviceCreated(object sender, System.EventArgs e)
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

            //var state = new Editor.Editor();
            //GameManager.PushState(state);

            Takai.UI.Static.DefaultFont = Takai.AssetManager.Load<Takai.Graphics.BitmapFont>(
                "Fonts/UISmall.bfnt");

            //testAutoObj = new Takai.Graphics.Sprite() { FrameLength = System.TimeSpan.FromMilliseconds(100) };

            //var testAutoUi = Takai.UI.Static.GeneratePropSheet(
            //    testAutoObj, Takai.UI.Static.DefaultFont, Color.White);

            //var box = new Takai.UI.ScrollBox() { Position = new Vector2(100) };
            //box.AddChild(testAutoUi);
            //box.Size = new Vector2(300, 400);
            //box.BorderColor = Color.LightBlue;

            ui = new Takai.UI.Static();

            var sbox = new Takai.UI.ScrollBox()
            {
                HorizontalAlignment = Takai.UI.Alignment.Middle,
                VerticalAlignment = Takai.UI.Alignment.Middle,
                Size = new Vector2(400)
            };
            ui.AddChild(sbox);
            var list = new Takai.UI.List()
            {
                HorizontalAlignment = Takai.UI.Alignment.Stretch,
                VerticalAlignment = Takai.UI.Alignment.Stretch,
            };
            sbox.AddChild(list);
            foreach (var file in System.IO.Directory.EnumerateFiles("Maps", "*.map.tk"))
            {
                var row = new Takai.UI.Static()
                {
                    Text = file,
                    HorizontalAlignment = Takai.UI.Alignment.Stretch,
                };
                row.Click += delegate (object _sender, Takai.UI.ClickEventArgs _e)
                {
                    var map = Takai.Data.Serializer.TextDeserialize<Takai.Game.Map>(((Takai.UI.Static)_sender).Text);
                    ui = new Takai.UI.Static(new Editor.Editor(map));
                };
                row.AutoSize(10);
                list.AddChild(row);
            }
            list.AutoSize();
            
            /*
            //var map = Takai.Data.Serializer.CastType<Takai.Game.Map>(
            //    Takai.Data.Serializer.TextDeserialize("Data/Maps/playground.map.tk"));
            var map = new Takai.Game.Map()
            {
                Tiles = new short[8, 8],
                TilesImage = Takai.AssetManager.Load<Texture2D>("Textures/Tiles2.png"),
                TileSize = 48,
            };
            map.BuildTileMask(map.TilesImage, true);
            map.BuildSectors();
            map.InitializeGraphics();
            ui = new Takai.UI.Static(new Editor.Editor(map));

            var klass = Takai.Data.Cache.Load<Takai.Game.EntityClass>("Defs/Entities/Player.ent.tk");
            var inst = map.Spawn(klass, new Vector2(128), Vector2.UnitX, Vector2.Zero);
            inst.Name = "Player";

            testTex = Takai.AssetManager.Load<Texture2D>("Textures/Background.png");
            */
            base.Initialize();
        }
        Texture2D testTex;

        protected override void Update(GameTime gameTime)
        {
            if (InputState.IsPress(Keys.Q)
            && InputState.IsMod(KeyMod.Control))
                Takai.Runtime.IsExiting = true;

            if (Takai.Runtime.IsExiting)
            {
                Exit();
                return;
            }

            if (InputState.IsPress(Keys.F12))
                takingScreenshot = true;

            if (InputState.IsPress(Keys.F6))
            {
                Takai.Data.Cache.SaveAllToFile("all.tk");
            }

            InputState.Update(GraphicsDevice.Viewport.Bounds);

            ui.Update(gameTime);
            ui.Bounds = GraphicsDevice.Viewport.Bounds;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            if (takingScreenshot)
            {
                takingScreenshot = false;

                using (var rt = new RenderTarget2D(GraphicsDevice,
                    GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height))
                {
                    GraphicsDevice.SetRenderTarget(rt);
                    GraphicsDevice.SetRenderTarget(null);

                    using (var fs = new System.IO.FileStream(System.DateTime.Now.ToString("dd_MMM_HH-mm-ss-fff") + ".png", System.IO.FileMode.Create))
                    {
                        rt.SaveAsPng(fs, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                    }
                }
            }
            else
            {
                sbatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                ui.Draw(sbatch);
                sbatch.End();
            }
        }
    }
}