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
        GraphicsDeviceManager gdm;

#if WINDOWS
        bool useCustomCursor = false;
#endif

        SpriteBatch sbatch;
        Takai.UI.Static ui;

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
                PreferMultiSampling = false,
#if WINDOWS_UAP
                SynchronizeWithVerticalRetrace = true,
                IsFullScreen = true,
#endif
            };

            gdm.DeviceCreated += GdmDeviceCreated;
            gdm.PreparingDeviceSettings += delegate (object sender, PreparingDeviceSettingsEventArgs e)
            {
                //required for splitscreen (currently)
                //e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            };

            IsMouseVisible = true;

            TargetElapsedTime = System.TimeSpan.FromSeconds(1 / 144f);
            IsFixedTimeStep = false;
        }

        void GdmDeviceCreated(object sender, System.EventArgs e)
        {
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

            var selectStoryUI = Takai.Data.Cache.Load<Takai.UI.Static>("UI/SelectStory.ui.tk");
            if (selectStoryUI is Game.StorySelect ss)
            {
                ss.StorySelected += delegate (object _sender, Game.GameStory story)
                {
                    //var game = new Game.Game
                    //{
                    //    Story = story,
                    //};
                    //game.LoadNextStoryMap();
                    //ui.ReplaceAllChildren(new Game.GameInstance(game));
                    ui.ReplaceAllChildren(new Editor.Editor(story.LoadMapIndex(0)));
                };
            }
            ui = new Takai.UI.Static(selectStoryUI);

            ui.BindCommand("asdf", (s) => s.BackgroundColor = Takai.Graphics.ColorUtil.Random());

            fpsGraph = new Takai.FpsGraph()
            {
                Dimensions = new Rectangle(20, 20, 800, 100),
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
                Takai.UI.Static.DebugFont = (Takai.UI.Static.DebugFont == null ? Takai.UI.Static.DefaultFont : null);

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
            ui.Dimensions = GraphicsDevice.Viewport.Bounds;
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
                    var sz = Takai.UI.Static.DefaultFont.MeasureString(text);
                    Takai.UI.Static.DefaultFont.Draw(sbatch, text, new Vector2(GraphicsDevice.Viewport.Width - sz.X - 20, y), Color.LightSeaGreen);
                }
                y -= Takai.UI.Static.DefaultFont.MaxCharHeight;
            }

            sbatch.End();
        }
    }
}