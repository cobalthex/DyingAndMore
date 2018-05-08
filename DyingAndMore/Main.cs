using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using System.Reflection;

namespace DyingAndMore
{
#if WINDOWS //win32 startup
    [Takai.Data.Serializer.Ignored]
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
                System.Windows.Forms.Application.EnableVisualStyles();
                game.Run();
            }
        }
    }

#endif

    /// <summary>
    /// The game
    /// </summary>
    public class DyingAndMoreGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager gdm;

#if WINDOWS
        bool useCustomCursor = false;
#endif

        SpriteBatch sbatch;
        Takai.UI.Static ui;

        Takai.Graphics.BitmapFont debugFont;

        Takai.FpsGraph fpsGraph;

        public Matrix uiMatrix;

        /// <summary>
        /// Create the game
        /// </summary>
        public DyingAndMoreGame()
        {
            gdm = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1600,
                PreferredBackBufferHeight = 900,
                PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
                GraphicsProfile = GraphicsProfile.HiDef,
                PreferredBackBufferFormat = SurfaceFormat.Color,
#if WINDOWS_UAP
                PreferMultiSampling = true,
                IsFullScreen = true,
#endif
            };

            gdm.DeviceCreated += GdmDeviceCreated;

            TargetElapsedTime = System.TimeSpan.FromSeconds(1 / 60f); //60 fps
            IsMouseVisible = true;

            IsFixedTimeStep = false;
        }

        void GdmDeviceCreated(object sender, System.EventArgs e)
        {
            Takai.Runtime.Game = this;

            Takai.Data.Serializer.LoadTypesFrom(typeof(DyingAndMoreGame).GetTypeInfo().Assembly);
#if DEBUG
            Takai.Data.Cache.WatchDirectory(Takai.Data.Cache.DefaultRoot);
#endif

            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.PresentationParameters.MultiSampleCount = 8;
            gdm.ApplyChanges();

            #region Mouse Cursor
#if WINDOWS
            if (useCustomCursor)
            {
                System.Drawing.Bitmap cur = new System.Drawing.Bitmap("Content/UI/Pointer.png", true);
                System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(cur);
                System.IntPtr ptr = cur.GetHicon();
                System.Windows.Forms.Cursor c = new System.Windows.Forms.Cursor(ptr);
                System.Windows.Forms.Form.FromHandle(Window.Handle).Cursor = c;
                this.IsMouseVisible = true;
            }
#endif
            #endregion

            sbatch = new SpriteBatch(GraphicsDevice);

            //var state = new Editor.Editor();
            //GameManager.PushState(state);

            Takai.UI.Static.DefaultFont = Takai.Data.Cache.Load<Takai.Graphics.BitmapFont>("Fonts/test.fnt.tk");

            debugFont = Takai.Data.Cache.Load<Takai.Graphics.BitmapFont>("Fonts/rct2.bfnt");

#if WINDOWS //UWP launch activation parameters?
            //parse command line args
            var args = System.Environment.GetCommandLineArgs();
            string presetMap = null;
            string presetMode = null;
            foreach (var arg in args)
            {
                if (arg.StartsWith("map="))
                    presetMap = arg.Substring(4);
                else if (arg.StartsWith("mode="))
                    presetMode = arg.Substring(5).ToLowerInvariant();
                //save =
            }

            //evaluate command line args
            if (presetMap != null)
            {
                try
                {
                    var map = Takai.Data.Cache.Load<Takai.Game.MapClass>(presetMap);
                    map.InitializeGraphics();
                    var instance = map.Instantiate();
                    if (presetMode == "game")
                        ui = new Takai.UI.Static(new Game.Game(instance));
                    else
                        ui = new Takai.UI.Static(new Editor.Editor(instance));
                    return;
                }
                catch (System.IO.FileNotFoundException) { }
            }
#endif

            ui = Takai.Data.Cache.Load<Takai.UI.Static>("UI/NoMap.ui.tk");
            var mapList = (Takai.UI.FileList)ui.FindChildByName("maps");
            mapList.SelectionChanged += delegate
            {
                if (mapList.SelectedIndex < 0)
                    return;

                Takai.Game.MapClass map = null;
                Takai.Game.MapInstance inst = null;

                var loaded = Takai.Data.Cache.Load(mapList.SelectedFile);
                if (loaded is Takai.Game.MapInstance)
                {
                    inst = (Takai.Game.MapInstance)loaded;
                    map = inst.Class;
                }
                else if (loaded is Takai.Game.MapClass)
                {
                    map = (Takai.Game.MapClass)loaded;
                    inst = map.Instantiate();
                }
                else
                    throw new System.ArgumentException("File loaded must be a MapClass or MapInstance");

                map.InitializeGraphics();
                ui = new Takai.UI.Static(new Editor.Editor(inst));
            };

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

            InputState.EnabledGestures =
                GestureType.Tap
                | GestureType.Flick
                | GestureType.FreeDrag
                | GestureType.Pinch;

            uiMatrix = Matrix.Identity;// Matrix.CreateTranslation(-GraphicsDevice.DisplayMode.Width / 2, 0, 0) * Matrix.CreateScale(2);

            ui.HasFocus = true;
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

            if (InputState.IsPress(Keys.F10))
                Takai.UI.Static.DebugFont = (Takai.UI.Static.DebugFont == null ? debugFont : null);

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

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, uiMatrix);
            ui.Draw(sbatch);

            int y = GraphicsDevice.Viewport.Height - 70;
            foreach (var row in Takai.LogBuffer.Entries)
            {
                if (row.text != null && row.time > System.DateTime.UtcNow.Subtract(System.TimeSpan.FromSeconds(3)))
                {
                    var text = $"{row.text} {row.time.Minute:D2}:{row.time.Second:D2}.{row.time.Millisecond:D3}";
                    var sz = debugFont.MeasureString(text);
                    debugFont.Draw(sbatch, text, new Vector2(GraphicsDevice.Viewport.Width - sz.X - 20, y), Color.MediumSeaGreen);
                }
                y -= debugFont.MaxCharHeight;
            }

            sbatch.End();
        }
    }
}