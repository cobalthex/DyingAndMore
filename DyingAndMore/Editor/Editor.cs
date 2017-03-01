using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.Graphics;

namespace DyingAndMore.Editor
{
    class EditorMode
    {
        public string Name { get; set; }
        
        protected readonly Editor editor;

        public EditorMode(string name, Editor editor)
        {
            Name = name;
            this.editor = editor;
        }

        public virtual void OpenConfigurator(bool DidClickOpen) { }

        public virtual void Start() { }
        public virtual void End() { }
        public virtual void Update(GameTime time) { }
        public virtual void Draw(SpriteBatch sbatch) { }
    }
   
    partial class Editor : Takai.Runtime.GameState
    {
        public Takai.Game.Map Map { get; set; }
        public Takai.Game.Camera Camera { get; set; }

        public BitmapFont LargeFont { get; set; }
        public BitmapFont SmallFont { get; set; }
        public BitmapFont DebugFont { get; set; }

        SpriteBatch sbatch;
        
        public static readonly Color ActiveColor = Color.GreenYellow;
        public static readonly Color InactiveColor = new Color(Color.Purple, 0.5f);

        int uiMargin = 20;
        Takai.UI.Element uiContainer;
        Takai.UI.Element selectorPreview;

        public ModeSelector modes;

        Vector2 lastWorldPos, currentWorldPos;

        public Editor() : base(false, false) { }

        public override void Load()
        {
            DebugFont = Takai.AssetManager.Load<BitmapFont>("Fonts/rct2.bfnt");
            SmallFont = Takai.AssetManager.Load<BitmapFont>("Fonts/UISmall.bfnt");
            LargeFont = Takai.AssetManager.Load<BitmapFont>("Fonts/UILarge.bfnt");

            using (var file = new System.IO.StreamWriter("UISmall.fnt.tk"))
                Takai.Data.Serializer.TextSerialize(file, DebugFont);

            sbatch = new SpriteBatch(GraphicsDevice);

            uiContainer = new Takai.UI.Element();

            TouchPanel.EnabledGestures = GestureType.Pinch | GestureType.Tap | GestureType.DoubleTap | GestureType.FreeDrag;

            if (Map == null)
            {
                var list = new Takai.UI.List()
                {
                    HorizontalAlignment = Takai.UI.Alignment.Middle,
                    VerticalAlignment = Takai.UI.Alignment.Middle,
                    Size = GraphicsDevice.Viewport.Bounds.Size.ToVector2()
                };

                Takai.UI.Element elem;
                list.AddChild(elem = new Takai.UI.Element()
                {
                    Text = "No map loaded",
                    Font = LargeFont,
                    HorizontalAlignment = Takai.UI.Alignment.Middle
                });
                elem.AutoSize(20);

                list.AddChild(elem = new Takai.UI.Element()
                {
                    Text = "Press Ctrl+N to create a new map",
                    Font = SmallFont,
                    HorizontalAlignment = Takai.UI.Alignment.Middle
                });
                elem.AutoSize(10);

                list.AddChild(elem = new Takai.UI.Element()
                {
                    Text = "or Ctrl+O to open a map",
                    Font = SmallFont,
                    HorizontalAlignment = Takai.UI.Alignment.Middle
                });
                elem.AutoSize(10);
                elem.OnClick += delegate { OpenMap(); };

                list.AddChild(elem = new Takai.UI.TextBox()
                {
                    Text = "Test",
                    Size = new Vector2(200, 30),
                    Font = SmallFont,
                    HorizontalAlignment = Takai.UI.Alignment.Middle
                });

                list.AutoSize();
                uiContainer.AddChild(list);
            }
            else
                StartMap();
        }

        public override void Unload()
        {
            while (Takai.Runtime.GameManager.TopState != this
                && Takai.Runtime.GameManager.Count > 0)
                Takai.Runtime.GameManager.PopState();

            TouchPanel.EnabledGestures = GestureType.None;
        }

        public void StartMap()
        {
            Camera = new Takai.Game.Camera(Map)
            {
                Viewport = GraphicsDevice.Viewport.Bounds
            };

            Map.updateSettings = Takai.Game.MapUpdateSettings.Editor;

            if (modes == null)
            {
                modes = new ModeSelector(LargeFont, SmallFont)
                {
                    HorizontalAlignment = Takai.UI.Alignment.Middle,
                    VerticalAlignment = Takai.UI.Alignment.Start,
                    Position = new Vector2(0, 40)
                };
                
                modes.AddMode(new TilesEditorMode(this));
                modes.AddMode(new DecalsEditorMode(this));
                modes.AddMode(new BlobsEditorMode(this));
                modes.AddMode(new EntitiesEditorMode(this));
                modes.AddMode(new GroupsEditorMode(this));
                modes.ModeIndex = 0;

                selectorPreview = new Takai.UI.Element()
                {
                    HorizontalAlignment = Takai.UI.Alignment.End,
                    VerticalAlignment = Takai.UI.Alignment.Start,
                    Position = new Vector2(10),
                    Size = new Vector2(Map.TileSize)
                };
                selectorPreview.OnClick += delegate
                {
                    modes.Mode?.OpenConfigurator(true);
                };

                uiContainer = new Takai.UI.Element(modes, selectorPreview)
                {
                    Size = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height)
                };
            }

            //start zoomed out to see the whole map
            var mapSize = new Vector2(Map.Width, Map.Height) * Map.TileSize;
            var xyScale = new Vector2(GraphicsDevice.Viewport.Width - 20, GraphicsDevice.Viewport.Height - 20) / mapSize;
            Camera.Scale = MathHelper.Clamp(MathHelper.Min(xyScale.X, xyScale.Y), 0.1f, 1f);
            Camera.Position = mapSize / 2;
        }

        public bool OpenMap()
        {
            var ofd = new System.Windows.Forms.OpenFileDialog()
            {
                SupportMultiDottedExtensions = true,
                Filter = "Map (*.map.tk,*.map.tkz)|*.map.tk;*.map.tkz|All Files (*.*)|*.*",
                InitialDirectory = System.IO.Path.GetDirectoryName(Map?.File),
                FileName = System.IO.Path.GetFileName(Map?.File)
            };

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (var stream = ofd.OpenFile())
                {
                    if (ofd.FileName.EndsWith("tkz"))
                    {
                        using (var decompress = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress))
                            Map = Takai.Game.Map.Load(decompress);
                    }
                    else
                        Map = Takai.Game.Map.Load(stream);
                }

                Map.File = ofd.FileName;
                StartMap();

                return true;
            }

            return false;
        }

        public bool SaveMap()
        {
            if (Map == null)
                return false;

            var sfd = new System.Windows.Forms.SaveFileDialog()
            {
                SupportMultiDottedExtensions = true,
                Filter = "Map (*.map.tk)|*.map.tk|All Files (*.*)|*.*",
                InitialDirectory = System.IO.Path.GetDirectoryName(Map.File),
                FileName = System.IO.Path.GetFileName(Map.File?.Substring(0, Map.File.IndexOf('.'))) //file dialog is retarded
            };

            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //todo: optional compression
                using (var stream = new System.IO.StreamWriter(sfd.OpenFile()))
                    Takai.Data.Serializer.TextSerialize(stream, Map);

                Map.File = sfd.FileName;
                return true;
            }

            return false;
        }
        
        private Rectangle lastViewport;
        public override void Update(GameTime time)
        {
            var viewport = GraphicsDevice.Viewport.Bounds;
            if (viewport != lastViewport)
            {
                lastViewport = viewport;
                uiContainer.Size = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            }

            if (!uiContainer.Update(time))
                return;

            if (InputState.IsMod(KeyMod.Control))
            {
                if ((InputState.IsPress(Keys.S) && SaveMap()) ||
                    (InputState.IsPress(Keys.O) && OpenMap()))
                    return;

                if (InputState.IsPress(Keys.Q))
                {
                    Takai.Runtime.GameManager.Exit();
                    return;
                }
            }

            if (Map == null)
                return;

            lastWorldPos = currentWorldPos;
            currentWorldPos = Camera.ScreenToWorld(InputState.MouseVector);

            if (InputState.IsClick(Keys.F1))
            {
                Takai.Runtime.GameManager.NextState(new Game.Game() { map = Map });
                return;
            }

            if (InputState.IsPress(Keys.F2))
                Map.debugOptions.showBlobReflectionMask ^= true;

            if (InputState.IsPress(Keys.F3))
                Map.debugOptions.showOnlyReflections ^= true;

            if (InputState.IsPress(Keys.F4))
            {
                Camera.Position = Vector2.Zero;
                Camera.Scale = 1;
            }

            //touch gestures
            while (TouchPanel.IsGestureAvailable)
            {
                var gesture = TouchPanel.ReadGesture();
                switch (gesture.GestureType)
                {
                    case GestureType.Pinch:
                        {
                            //move
                            if (Vector2.Dot(gesture.Delta, gesture.Delta2) > 0)
                            {
                                //todo: maybe add velocity
                                Camera.Position -= Vector2.TransformNormal(gesture.Delta2, Matrix.Invert(Camera.Transform)) / 2;
                            }
                            //scale
                            else
                            {
                                var lp1 = gesture.Position - gesture.Delta;
                                var lp2 = gesture.Position2 - gesture.Delta2;
                                var dist = Vector2.Distance(gesture.Position2, gesture.Position2);
                                var ld = Vector2.Distance(lp1, lp2);

                                var scale = (dist / ld) / 1024;
                                Camera.Scale += scale;
                            }
                            break;
                        }

                }
            }

            //camera
            if (InputState.IsButtonDown(MouseButtons.Middle))
            {
                Camera.MoveTo(Camera.Position + Vector2.TransformNormal(InputState.MouseDelta(), Matrix.Invert(Camera.Transform)));
            }
            else
            {
                var d = Vector2.Zero;
                if (InputState.IsButtonDown(Keys.A) || InputState.IsButtonDown(Keys.Left))
                    d -= Vector2.UnitX;
                if (InputState.IsButtonDown(Keys.W) || InputState.IsButtonDown(Keys.Up))
                    d -= Vector2.UnitY;
                if (InputState.IsButtonDown(Keys.D) || InputState.IsButtonDown(Keys.Right))
                    d += Vector2.UnitX;
                if (InputState.IsButtonDown(Keys.S) || InputState.IsButtonDown(Keys.Down))
                    d += Vector2.UnitY;

                if (d != Vector2.Zero)
                {
                    d.Normalize();
                    d = d * Camera.MoveSpeed * (float)time.ElapsedGameTime.TotalSeconds; //(camera velocity)
                    Camera.Position += Vector2.TransformNormal(d, Matrix.Invert(Camera.Transform));
                }
            }

            //open current selector
            if (InputState.IsPress(Keys.Tab))
            {
                modes.Mode?.OpenConfigurator(false);
                return;
            }

            if (InputState.HasScrolled())
            {
                var delta = InputState.ScrollDelta() / 1024f;
                if (InputState.IsButtonDown(Keys.LeftShift))
                {
                    Camera.Rotation += delta;
                }
                else
                {
                    Camera.Scale += delta;
                    if (System.Math.Abs(Camera.Scale - 1) < 0.1f) //snap to 100% when near
                        Camera.Scale = 1;
                    else
                        Camera.Scale = MathHelper.Clamp(Camera.Scale, 0.1f, 2f);
                }
                //todo: translate to mouse cursor
            }

            Camera.Scale = MathHelper.Clamp(Camera.Scale, 0.1f, 10f); //todo: make global and move to some game settings
            Camera.Update(time);
        }

        Vector2 CenterInRect(Vector2 size, Rectangle region)
        {
            return new Vector2(region.X + (region.Width - size.X) / 2, region.Y + (region.Height - size.Y) / 2);
        }

        public override void Draw(GameTime time)
        {
            if (Map == null)
            {
                GraphicsDevice.Clear(new Color(24, 24, 24)); //todo: replace w/ graphic maybe

                sbatch.Begin();
                uiContainer.Draw(sbatch);
                sbatch.End();
                return;
            }

            //draw border around map
            Map.DrawRect(new Rectangle(0, 0, Map.Width * Map.TileSize, Map.Height * Map.TileSize), Color.Orange);

            Camera.Draw();

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            //fps
            var sFps = (1 / time.ElapsedGameTime.TotalSeconds).ToString("N2");
            var sSz = DebugFont.MeasureString(sFps);
            DebugFont.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - sSz.X - 10, GraphicsDevice.Viewport.Height - sSz.Y - 10), Color.LightSteelBlue);

            DebugFont.Draw(sbatch, $"Zoom: {(Camera.Scale * 100):N1}%", new Vector2(10, GraphicsDevice.Viewport.Height - sSz.Y - 10), Color.LightSteelBlue);

            //draw selected item in top right corner
            //if (modes.Mode?.Selector != null)
            //{
            //    modes.Mode.Selector.DrawItem(time, modes.Mode.Selector.SelectedItem, selectorPreview.AbsoluteBounds, sbatch);
            //    Primitives2D.DrawRect(sbatch, Color.White, selectorPreview.AbsoluteBounds);
            //}

            sbatch.End();

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            uiContainer.Draw(sbatch);
            sbatch.End();
        }

        static readonly Matrix arrowWingTransform = Matrix.CreateRotationZ(120);
        public void DrawArrow(Vector2 Position, Vector2 Direction, float Magnitude)
        {
            var tip = Position + (Direction * Magnitude);
            Map.DrawLine(Position, tip, Color.Yellow);

            Magnitude = MathHelper.Clamp(Magnitude * 0.333f, 5, 30);
            Map.DrawLine(tip, tip - (Magnitude * Vector2.Transform(Direction, arrowWingTransform)), Color.Yellow);
            Map.DrawLine(tip, tip - (Magnitude * Vector2.Transform(Direction, Matrix.Invert(arrowWingTransform))), Color.Yellow);
        }
    }
}