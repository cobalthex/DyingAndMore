using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System.Reflection;
using System.Collections.Generic;
using Takai.Data;
using Takai.Input;
using Takai.UI;
using Takai.Game;
using Takai.Graphics;

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

        Matrix textTransform;
        SpriteBatch sbatch;
        Static ui;
        Static debugUI;
        Takai.FpsGraph fpsGraph;

        public static Table DebugPropertyDisplay { get; private set; }

        public static void DebugDisplay(string key, object value)
        {
#if DEBUG
            DebugPropertyDisplay.AddChildren(
                new Static(key) { Color = Color.Cyan },
                new Static(value.ToString()) { Color = Color.Aquamarine }
            );
#endif
        }

        /// <summary>
        /// Create the game
        /// </summary>
        public DyingAndMoreGame()
        {
            gdm = new GraphicsDeviceManager(this)
            {
                PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
                GraphicsProfile = GraphicsProfile.HiDef,
                PreferredBackBufferFormat = SurfaceFormat.Color,
                SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight,
#if WINDOWS_UAP
                SynchronizeWithVerticalRetrace = true,
                //IsFullScreen = true,
#elif ANDROID
                IsFullScreen = false,
                PreferMultiSampling = true,
#else
                PreferMultiSampling = true,
                SynchronizeWithVerticalRetrace = false,
                PreferredBackBufferWidth = 1600,
                PreferredBackBufferHeight = 900,
                PreferHalfPixelOffset = false,
#endif
            };

            gdm.DeviceCreated += GdmDeviceCreated;
            gdm.PreparingDeviceSettings += delegate (object sender, PreparingDeviceSettingsEventArgs e)
            {
                //required for splitscreen (currentsly
                //e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            };

            Window.ClientSizeChanged += Window_ClientSizeChanged;

            IsMouseVisible = true;
            TargetElapsedTime = System.TimeSpan.FromSeconds(1 / 144f);
            IsFixedTimeStep = false;
        }

        Matrix CreateScreenTransform()
        {
            Matrix.CreateOrthographicOffCenter(
                0,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height,
                0,
                0, -1,
                out var transform
            );
            if (GraphicsDevice.UseHalfPixelOffset)
            {
                transform.M41 += -0.5f * transform.M11;
                transform.M42 += -0.5f * transform.M22;
            }
            return transform;
        }

        private void Window_ClientSizeChanged(object sender, System.EventArgs e)
        {
            textTransform = CreateScreenTransform();
            ui?.InvalidateMeasure();
        }

        void GdmDeviceCreated(object sender, System.EventArgs e)
        {
            Takai.Runtime.Game = this;

            Serializer.LoadTypesFrom(typeof(DyingAndMoreGame).GetTypeInfo().Assembly);

#if DEBUG && WINDOWS
            Cache.WatchDirectory(Cache.ContentRoot);
#endif

            GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            TextRenderer.Default = new TextRenderer(Cache.Load<Effect>("Shaders/SDFTex.mgfx"));
            textTransform = CreateScreenTransform();

            Static.DebugFont = Cache.Load<Font>("Fonts/Debug.fnt.tk");
            Static.DebugTextStyle = new TextStyle
            {
                size = 15,
                outlineColor = Color.Black,
                outlineThickness = 0.2f,
            };

            //custom cursor
            //Mouse.SetCursor(MouseCursor.FromTexture2D(Cache.Load<Texture2D>("UI/Pointer.png"), 0, 0));

            InputState.EnabledGestures
                = GestureType.Tap
                | GestureType.Flick
                | GestureType.FreeDrag
                | GestureType.Pinch;

            sbatch = new SpriteBatch(GraphicsDevice);

            Static.MergeStyleRules(Cache.Load<Dictionary<string, Dictionary<string, object>>>("UI/Styles.tk"));
#if ANDROID
            Static.MergeStyleRules(Cache.Load<Dictionary<string, Dictionary<string, object>>>("UI/Styles.Mobile.tk"));
#endif

            //var state = new Editor.Editor();
            //GameManager.PushState(state);

            // = Cache.Load<Takai.Graphics.BitmapFneedle.ent.rtkont>("Fonts/test.fnt.tk");

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

                ui?.RemoveFromParent();
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

            Static.GlobalCommands["DragModal"] = delegate (Static ui, object arg)
            {
                while (ui != null && !ui.IsModal)
                    ui = ui.Parent;

                if (ui != null)
                {
                    var newPos = ui.Position;
                    if (InputState.touches.Count > 0)
                        newPos += InputState.TouchDelta(0) * 2;
                    else
                        newPos += InputState.MouseDelta() * 2;

                    //var containerSize = ui.Parent == null
                    //    ? new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height)
                    //    : ui.Parent.ContentArea.Size.ToVector2();

                    ////todo: needs to be bounds not content area
                    ////todo: broken with centering
                    //ui.Position = Vector2.Clamp(newPos, Vector2.Zero, containerSize - ui.ContentArea.Size.ToVector2());

                    ui.Position = newPos;
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
                HorizontalAlignment = Alignment.Middle,
                VerticalAlignment = Alignment.Middle,
                BasePath = System.IO.Path.Combine(Cache.ContentRoot, "Mapsrc"),
                FilterRegex = "\\.(map\\.tk|d2map\\.zip)$",
            };
            childUI.On(Static.SelectionChangedEvent, delegate (Static s, UIEventArgs ee)
            {
                var fl = (FileList)s;
#if !ANDROID
                if (System.IO.File.Exists(fl.SelectedFile))
#endif
                {
                    if (System.IO.Path.GetExtension(fl.SelectedFile) == ".zip")
                    {
                        var packmap = (MapInstance)MapBaseInstance.FromPackage(fl.SelectedFile);
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

#if DEBUG && ANDROID
            Static.DisplayDebugInfo = true;
#endif

            //sp = new Takai.Graphics.Sprite();
            //childUI = Static.GeneratePropSheet(sp);
            //childUI.Size = new Vector2(400, 500);

            //childUI = new UI.UIDesigner()
            //{
            //    Size = new Vector2(3000, 2000)
            //};

            //{
            //var map = Cache.Load<MapInstance>("mapsrc/aitest.map.tk");
            //childUI = new Editor.Editor(map);
            //map.renderSettings.drawEntityForwardVectors = true;
            //map.renderSettings.drawEntityHierarchies = true;
            //}

            //var ui = Cache.Load<Static>("UI/SelectStory.ui.tk");
            //if (ui is Game.StorySelect ss)
            //{
            //    ss.StorySelected += delegate (object _sender, Game.GameStory story)
            //    {
            //        var game = new Game.Game
            //        {
            //            Story = story,
            //        };
            //        game.LoadNextStoryMap();
            //        ui.ReplaceAllChildren(new Game.GameInstance(game));
            //        ui.ReplaceAllChildren(new Editor.Editor(story.LoadMapIndex(0)));
            //    };
            //}

            ui = new Static
            //ui = new ScrollBox
            {
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch,
            };
            ui.AddChild(new ScrollBox(childUI)
            {
                HorizontalAlignment = Alignment.Middle,
                VerticalAlignment = Alignment.Stretch
            });
            var acc = new Accordian
            {
                Position = new Vector2(100),
                ShadeTitleUI = new Static
                {
                    Bindings = new List<Binding> { new Binding("Name", "Text") },
                    BackgroundColor = Color.Tomato,
                    HorizontalAlignment = Alignment.Stretch
                },
            };
            acc.AddChild(new Static("test 1") { Name = "A" });
            acc.AddChild(new Static("test 2") { Name = "B" });
            acc.AddChild(new Static("test 3") { Name = "C" });
            ui.AddChild(acc);

            //Static.DebugFont = Static.DefaultFont;

            debugUI = new Static
            {
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch,
            };
#if DEBUG
            debugUI.AddChild(DebugPropertyDisplay = new Table(2)
            {
                Name = "Debug Display",
                HorizontalAlignment = Alignment.Right,
                VerticalAlignment = Alignment.Top,
                Position = new Vector2(30),
                Margin = new Vector2(5),
            });
#endif
            debugUI.AddChild(fpsGraph = new Takai.FpsGraph
            {
                Name = "FPS Graph",
                IsEnabled = false,
                Position = new Vector2(0, 20),
                Size = new Vector2(600, 100),
                HorizontalAlignment = Alignment.Center,
            });

            ui.HasFocus = true;
            base.Initialize();

            GraphicsDevice.SetRenderTarget(null);
        }

        protected override void Update(GameTime gameTime)
        {
#if DEBUG
            DebugPropertyDisplay.RemoveAllChildren();
#endif

            InputState.Update(GraphicsDevice.Viewport.Bounds);

            if (InputState.touches.Count > 0)
                lastTouchPos = InputState.touches[0].Position;

            //DebugDisplay("Viewport", GraphicsDevice.Viewport.Bounds);
            //DebugDisplay("Touch", lastTouchPos);

            if (InputState.IsPress(Keys.Q)
            && InputState.IsMod(KeyMod.Control))
                Takai.Runtime.IsExiting = true;

            if (Takai.Runtime.IsExiting)
            {
                Exit();
                return;
            }

            else if (InputState.IsPress(Keys.F8))
                fpsGraph.IsEnabled ^= true;

            else if (InputState.IsPress(Keys.F9))
            {
                var uiTree = new UITree(ui.FindChildAtPoint(InputState.MousePoint));
                var container = new ScrollBox(uiTree)
                {
                    Size = new Vector2(600),
                    Style = "Frame",
                    HorizontalAlignment = Alignment.Center,
                    VerticalAlignment = Alignment.Center,
                    IsModal = true,
                    EventCommands =
                    {
                        [Static.ClickEvent] = "CloseModal",
                    }
                };
                ui.AddChild(container);
            }
            else if (InputState.IsPress(Keys.F10))
                Static.DisplayDebugInfo ^= true;
            else if (InputState.IsPress(Keys.F11))
                ui.DebugInvalidateTree();

            ui.Update(gameTime);
            debugUI.Update(gameTime);
        }
        Vector2 lastTouchPos;

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            var drawContext = new DrawContext
            {
                spriteBatch = sbatch,
                textRenderer = TextRenderer.Default
            };
            ui.Draw(drawContext);
            debugUI.Draw(drawContext);

#if DEBUG
            float y = GraphicsDevice.Viewport.Height - 70;
            foreach (var row in Takai.LogBuffer.Entries) //convert to UI?
            {
                if (row.text != null && row.time > System.DateTime.UtcNow.Subtract(System.TimeSpan.FromSeconds(3)))
                {
                    var text = $"{row.text} {row.time.Minute:D2}:{row.time.Second:D2}.{row.time.Millisecond:D3}";

                    var sz = Static.DebugFont.MeasureString(text, Static.DebugTextStyle);
                    var drawText = new DrawTextOptions(
                        text,
                        Static.DebugFont,
                        Static.DebugTextStyle,
                        Color.LightSeaGreen,
                        new Vector2(GraphicsDevice.Viewport.Width - sz.X - 20, y)
                    );
                    TextRenderer.Default.Draw(drawText);
                    y -= sz.Y;
                }
            }


            Primitives2D.DrawCross(sbatch, Color.Tomato,
                new Rectangle((int)lastTouchPos.X - 20, (int)lastTouchPos.Y - 20, 40, 40));
#endif

            sbatch.End();
            
            TextRenderer.Default.Present(GraphicsDevice, textTransform);
        }
    }
}