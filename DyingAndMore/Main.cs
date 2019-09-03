using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System.Reflection;
using Takai.Data;
using Takai.Input;
using Takai.UI;

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
        Static ui;

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
                SynchronizeWithVerticalRetrace = true,
                //IsFullScreen = true,
#else
                PreferMultiSampling = true,
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

            InputState.EnabledGestures
                = GestureType.Tap
                | GestureType.Flick
                | GestureType.FreeDrag
                | GestureType.Pinch;

            sbatch = new SpriteBatch(GraphicsDevice);

            //var state = new Editor.Editor();
            //GameManager.PushState(state);

            Static.DefaultFont = Cache.Load<Takai.Graphics.BitmapFont>("Fonts/test.fnt.tk");

            //todo: move elsewhere
            Static.GlobalCommands["AddUI"] = delegate (Static ui, object arg)
            {
                if (!(arg is Static child))
                    return;

                ui.AddChild(child);
            };
            Static.GlobalCommands["AddRootUI"] = delegate (Static ui, object arg)
            {
                if (!(arg is Static child))
                    return;

                if (ui.ChildBindScope != null)
                    child.BindTo(ui.GetChildBindScope());

                ui.GetRoot().AddChild(child);
            };
            Static.GlobalCommands["CloseModal"] = delegate (Static ui, object arg)
            {
                while (ui != null && !ui.IsModal)
                    ui = ui.Parent;

                if (ui != null)
                    ui.RemoveFromParent();
            };
            Static.GlobalCommands["RemoveUI"] = delegate (Static ui, object arg)
            {
                if (arg is string str)
                    ui.GetRoot().FindChildByName(str)?.RemoveFromParent();
            };

            Static.GlobalCommands["EnableUI"] = delegate (Static ui, object arg)
            {
                if (arg is string str)
                {
                    ui = ui.GetRoot().FindChildByName(str);
                    if (ui != null)
                        ui.IsEnabled = true;
                }
                //int (debug id), static
            };
            Static.GlobalCommands["DisableUI"] = delegate (Static ui, object arg)
            {
                if (arg is string str)
                {
                    ui = ui.GetRoot().FindChildByName(str);
                    if (ui != null)
                        ui.IsEnabled = false;
                }
                //int (debug id), static
            };

            Static.GlobalCommands["Multiple"] = delegate (Static ui, object arg)
            {
                if (arg is System.Collections.IEnumerable ie)
                {
                    foreach (var i in ie)
                    {
                        var cmd = Serializer.Cast<EventCommandBinding>(i);
                        ui.BubbleCommand(cmd.command, cmd.argument);
                    }
                }
            };

            Static.GlobalCommands["Routed"] = delegate (Static ui, object arg)
            {
                //solve this via tunneling?

                if (!(arg is System.Collections.IList il) || il.Count < 2 || 
                    !(il[0] is string name))
                        return;

                var namedUI = ui.GetRoot().FindChildByName(name);
                if (namedUI != null)
                {
                    var cmd = Serializer.Cast<EventCommandBinding>(il[1]);
                    namedUI.BubbleCommand(cmd.command, cmd.argument);
                }

            };

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
                        ui = new Static(new Game.GameInstance(new Game.Game { Map = instance }));
                    else
                        ui = new Static(new Editor.Editor(instance));
                    return;
                }
                catch (System.IO.FileNotFoundException) { }
            }
#endif
            */
            Static childUI;

            childUI = new FileList
            {
                //Size = new Vector2(400, 600),
                HorizontalAlignment = Alignment.Middle,
                VerticalAlignment = Alignment.Middle,
                BasePath = System.IO.Path.Combine(Cache.Root, "Mapsrc"),
                FilterRegex = "\\.(map\\.tk|d2map\\.zip)$",
            };
            childUI.On(Static.SelectionChangedEvent, delegate (Static s, UIEventArgs ee)
            {
                var fl = (FileList)s;
                if (System.IO.File.Exists(fl.SelectedFile))
                {
                    if (System.IO.Path.GetExtension(fl.SelectedFile) == ".zip")
                    {
                        var packmap = (MapInstance)Takai.Game.MapBaseInstance.FromPackage(fl.SelectedFile);
                        packmap.Class.InitializeGraphics();
                        ui.ReplaceAllChildren(new Editor.Editor(packmap));
                        return UIEventResult.Handled;
                    }

                    var map = Cache.Load(fl.SelectedFile);
                    if (map is MapClass mc)
                    {
                        mc.InitializeGraphics();
                        ui.ReplaceAllChildren(new Editor.Editor((MapInstance)mc.Instantiate()));
                    }
                    else if (map is MapInstance mi)
                    {
                        mi.Class.InitializeGraphics();
                        ui.ReplaceAllChildren(new Editor.Editor(mi));
                    }
                }
                return UIEventResult.Handled;
            });

            //childUI = new UI.TestSizer()
            //{
            //    Position = new Vector2(10),
            //    BackgroundColor = new Color(200, 100, 40)
            //};
            //childUI = new ScrollBox(childUI)
            //{
            //    Size = new Vector2(300),
            //    Position = new Vector2(50)
            //};

            //childUI = Cache.Load<Static>("UI/test/scrollbox.ui.tk");

            //var ui = Cache.Load<Static>("UI/SelectStory.ui.tk");
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

            ui = new Static
            {
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch
            };
            ui.AddChild(childUI);
            //Static.DebugFont = Static.DefaultFont;

            //fpsGraph = new Takai.FpsGraph()
            //{
            //    Position = new Vector2(0, 100),
            //    Size = new Vector2(800, 100),
            //    HorizontalAlignment = Alignment.Middle
            //};

            //devtoolsMenu = new UI.DevtoolsMenu
            //{
            //    HorizontalAlignment = Alignment.Middle,
            //    VerticalAlignment = Alignment.Middle
            //};

            ui.HasFocus = true;
            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            InputState.Update(GraphicsDevice.Viewport.Bounds);

            if (InputState.IsPress(Keys.Q)
            && InputState.IsMod(KeyMod.Control))
                Takai.Runtime.IsExiting = true;

            if (Takai.Runtime.IsExiting)
            {
                Exit();
                return;
            }

            //F9 used in UI code
            else if (InputState.IsPress(Keys.F10))
                Static.DebugFont = (Static.DebugFont == null ? Static.DefaultFont : null);
            else if (InputState.IsPress(Keys.F11))
                ui.DebugInvalidateTree();

            ui.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            ui.Draw(sbatch);

            int y = GraphicsDevice.Viewport.Height - 70;
            foreach (var row in Takai.LogBuffer.Entries)
            {
                if (row.text != null && row.time > System.DateTime.UtcNow.Subtract(System.TimeSpan.FromSeconds(3)))
                {
                    var text = $"{row.text} {row.time.Minute:D2}:{row.time.Second:D2}.{row.time.Millisecond:D3}";
                    var sz = Static.DefaultFont.MeasureString(text);
                    Static.DefaultFont.Draw(sbatch, text, new Vector2(GraphicsDevice.Viewport.Width - sz.X - 20, y), Color.LightSeaGreen);
                }
                y -= Static.DefaultFont.MaxCharHeight;
            }

            sbatch.End();
        }
    }
}