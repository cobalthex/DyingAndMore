using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System.Reflection;
using Takai.Data;
using Takai.Input;

namespace DyingAndMore
{
#if WINDOWS //win32 startup
    [Serializer.Ignored]
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
                PreferMultiSampling = true,
#if WINDOWS_UAP
                SynchronizeWithVerticalRetrace = true,
                IsFullScreen = true,
#else
                SynchronizeWithVerticalRetrace = false,
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

            Serializer.LoadTypesFrom(typeof(DyingAndMoreGame).GetTypeInfo().Assembly);
#if DEBUG
            Cache.WatchDirectory(Cache.Root);
#endif

            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
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

            Takai.UI.Static.DefaultFont = Cache.Load<Takai.Graphics.BitmapFont>("Fonts/test.fnt.tk");

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
                    var map = Cache.Load<Takai.Game.MapClass>(presetMap);
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
            */

            ui = new Takai.UI.FileList
            {
                HorizontalAlignment = Takai.UI.Alignment.Middle,
                VerticalAlignment = Takai.UI.Alignment.Middle,
                BasePath = System.IO.Path.Combine(Cache.Root, "Maps"),
                FilterRegex = "\\.map.tk$",
            };
            ((Takai.UI.FileList)ui).SelectionChanged += delegate (object s, Takai.UI.SelectionChangedEventArgs se)
            {
                var fl = (Takai.UI.FileList)s;
                if (System.IO.File.Exists(fl.SelectedFile))
                {
                    var map = Cache.Load<Takai.Game.MapClass>(fl.SelectedFile);
                    map.InitializeGraphics();
                    ui.ReplaceAllChildren(new Editor.Editor(map.Instantiate()));
                }
            };
            //var ui = Cache.Load<Takai.UI.Static>("UI/SelectStory.ui.tk");
            //if (ui is Game.StorySelect ss)
            //{
            //    ss.StorySelected += delegate (object _sender, Game.GameStory story)
            //    {
            //        //var game = new Game.Game
            //        //{
            //        //    Story = story,
            //        //};
            //        //game.LoadNextStoryMap();
            //        //ui.ReplaceAllChildren(new Game.GameInstance(game));
            //        ui.ReplaceAllChildren(new Editor.Editor(story.LoadMapIndex(0)));
            //    };
            //}
            //ui = Cache.Load<Takai.UI.Static>("UI/test/elements.ui.tk");

            ui = new Takai.UI.Static(ui)
            {
                HorizontalAlignment = Takai.UI.Alignment.Stretch,
                VerticalAlignment = Takai.UI.Alignment.Stretch
            };
            //Takai.UI.Static.DebugFont = Takai.UI.Static.DefaultFont;

            fpsGraph = new Takai.FpsGraph()
            {
                Position = new Vector2(0, 100),
                Size = new Vector2(800, 100),
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
            if (InputState.IsPress(Keys.F11))
                ui.Reflow();

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
            //ui.Size = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
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