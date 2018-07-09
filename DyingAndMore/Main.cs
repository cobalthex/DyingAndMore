using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using System.Reflection;
using System.Runtime.InteropServices;

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
#if WINDOWS
        public enum ScreenOrientation : int
        {
            Angle0 = 0,
            Angle90 = 1,
            Angle180 = 2,
            Angle270 = 3
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;

            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;

            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("User32", CharSet = CharSet.Unicode)]
        public static extern bool EnumDisplaySettings(string szDeviceName, int modeNum, ref DEVMODE devMode);
#endif
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
                SynchronizeWithVerticalRetrace = false,
                //PreferMultiSampling = true,
#if WINDOWS_UAP
                //IsFullScreen = true,
#endif
            };

            gdm.DeviceCreated += GdmDeviceCreated;

            IsMouseVisible = true;

            TargetElapsedTime = System.TimeSpan.FromSeconds(1 / 144f);
            IsFixedTimeStep = false;
        }

        void GdmDeviceCreated(object sender, System.EventArgs e)
        {
            DEVMODE d = new DEVMODE();
            EnumDisplaySettings(null, 1, ref d);
            System.Diagnostics.Debug.WriteLine($"Display {d.dmDeviceName} Hz: {d.dmDisplayFrequency}");

            Takai.Runtime.Game = this;

            Takai.Data.Serializer.LoadTypesFrom(typeof(DyingAndMoreGame).GetTypeInfo().Assembly);
#if DEBUG
            Takai.Data.Cache.WatchDirectory(Takai.Data.Cache.Root);
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

            /*
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
                        ui = new Takai.UI.Static(new Game.GameInstance(new Game.Game { Map = instance }));
                    else
                        ui = new Takai.UI.Static(new Editor.Editor(instance));
                    return;
                }
                catch (System.IO.FileNotFoundException) { }
            }
#endif
            ui = Takai.Data.Cache.Load<Takai.UI.Static>("UI/NoMap.ui.tk");
            var mapList = ui.FindChildByName< Takai.UI.FileList>("maps");
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
            newMap.Click += delegate
            {
                var newMapUi = Takai.Data.Cache.Load<Takai.UI.Static>("UI/Editor/NewMap.ui.tk");
                var create = newMapUi.FindChildByName("create");
                create.Click += delegate
                {
                    var name = newMapUi.FindChildByName("name").Text;
                    var width = newMapUi.FindChildByName<Takai.UI.NumericBase>("width").Value;
                    var height = newMapUi.FindChildByName<Takai.UI.NumericBase>("height").Value;
                    var tileset = Takai.Data.Cache.Load<Takai.Game.Tileset>(newMapUi.FindChildByName<Takai.UI.FileInputBase>("tileset").Value);

                    var map = new Takai.Game.MapClass
                    {
                        Name = name,
                        Tiles = new short[height, width],
                        TileSize = tileset.size,
                        TilesImage = tileset.texture,
                    };
                    map.InitializeGraphics();
                    ui.ReplaceAllChildren(new Editor.Editor(map.Instantiate()));
                };

                ui.ReplaceAllChildren(newMapUi);
            };
            */

            var storySelect = new Game.StorySelect
            {
                Size = new Vector2(400),
                HorizontalAlignment = Takai.UI.Alignment.Middle,
                VerticalAlignment = Takai.UI.Alignment.Middle,
            };
            storySelect.StorySelected += delegate (object _sender, Game.GameStory story)
            {
                var game = new Game.Game
                {
                    Story = story,
                };
                game.LoadNextStoryMap();
                ui.ReplaceAllChildren(new Game.GameInstance(game));
            };
            ui = new Takai.UI.Static(storySelect);

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
                    debugFont.Draw(sbatch, text, new Vector2(GraphicsDevice.Viewport.Width - sz.X - 20, y), Color.LightSeaGreen);
                }
                y -= debugFont.MaxCharHeight;
            }

            sbatch.End();
        }
    }
}