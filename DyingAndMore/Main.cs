using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore
{
    #region startup (Windows/Xbox/Zune)
    [Takai.Data.Serializer.Ignored]
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

        Takai.Graphics.BitmapFont debugFont;

        Takai.FpsGraph fpsGraph;

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

            IsFixedTimeStep = true;
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

            sbatch = new SpriteBatch(GraphicsDevice);

            //var state = new Editor.Editor();
            //GameManager.PushState(state);

            Takai.UI.Static.DefaultFont = Takai.Data.Cache.Load<Takai.Graphics.BitmapFont>("UI/Fonts/UISmall.bfnt");

            debugFont = Takai.Data.Cache.Load<Takai.Graphics.BitmapFont>("UI/Fonts/rct2.bfnt");

            //testAutoObj = new Takai.Graphics.Sprite() { FrameLength = System.TimeSpan.FromMilliseconds(100) };

            //var testAutoUi = Takai.UI.Static.GeneratePropSheet(
            //    testAutoObj, Takai.UI.Static.DefaultFont, Color.White);

            //var box = new Takai.UI.ScrollBox() { Position = new Vector2(100) };
            //box.AddChild(testAutoUi);
            //box.Size = new Vector2(300, 400);
            //box.BorderColor = Color.LightBlue;

            ui = Takai.Data.Cache.Load<Takai.UI.Static>("UI/NoMap.ui.tk");
            var mapList = (Takai.UI.List)ui.FindChildByName("maps");

            foreach (var file in System.IO.Directory.EnumerateFiles(System.IO.Path.Combine(Takai.Data.Cache.DefaultRoot, "Maps"), "*.map.tk"))
            {
                var row = new Takai.UI.Static()
                {
                    Text = file,
                    HorizontalAlignment = Takai.UI.Alignment.Stretch,
                };
                row.Click += delegate (object _sender, Takai.UI.ClickEventArgs _e)
                {
                    var _file = ((Takai.UI.Static)_sender).Text;
                    var map = Takai.Data.Cache.Load<Takai.Game.MapClass>(_file);
                    map.InitializeGraphics();
                    ui = new Takai.UI.Static(new Editor.Editor(map.Create()));
                };
                row.AutoSize(10);
                mapList.AddChild(row);
            }
            mapList.AutoSize();

            var newMap = ui.FindChildByName("new");
            newMap.Click += delegate (object _sender, Takai.UI.ClickEventArgs _e)
            {
                ui.ReplaceAllChildren(Takai.Data.Cache.Load<Takai.UI.Static>("UI/Editor/NewMap.ui.tk"));
                //todo: hook up
            };

            fpsGraph = new Takai.FpsGraph()
            {
                Bounds = new Rectangle(20, 20, 800, 100),
                HorizontalAlignment = Takai.UI.Alignment.Middle
            };

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
                System.Diagnostics.Process.Start("all.tk");
            }

            if (InputState.IsPress(Keys.F7))
            {
                ui.ReplaceAllChildren(new AssetView());
            }
            if (InputState.IsPress(Keys.F8))
            {
                if (!fpsGraph.RemoveFromParent())
                {
                    fpsGraph.Clear();
                    ui.AddChild(fpsGraph);
                }
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

                int y = GraphicsDevice.Viewport.Height - 70;
                foreach (var row in Takai.LogBuffer.Entries)
                {
                    if (row.text != null && row.time > System.DateTime.UtcNow.Subtract(System.TimeSpan.FromSeconds(3)))
                    {
                        var sz = debugFont.MeasureString(row.text);
                        debugFont.Draw(sbatch, row.text, new Vector2(GraphicsDevice.Viewport.Width - sz.X - 20, y), Color.MediumSeaGreen);
                    }
                    y -= debugFont.MaxCharHeight;
                }

                sbatch.End();
            }
        }
    }
}