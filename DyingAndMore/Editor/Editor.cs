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

        public BitmapFont LargeFont { get; set; }
        public BitmapFont SmallFont { get; set; }
        public BitmapFont DebugFont { get; set; }

        SpriteBatch sbatch;

        public static readonly Color ActiveColor = Color.GreenYellow;
        public static readonly Color InactiveColor = new Color(Color.Purple, 0.5f);

        Takai.UI.Element uiContainer;
        Takai.UI.Element selectorPreview;
        Takai.UI.Element renderSettingsConsole;

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

            uiContainer = new Takai.UI.Element() { Name = "container" };

            TouchPanel.EnabledGestures = GestureType.Pinch | GestureType.Tap | GestureType.DoubleTap | GestureType.FreeDrag;

            renderSettingsConsole = new Takai.UI.List()
            {
                Name = "RenderSettings",
                Position = new Vector2(20, 0),
                HorizontalAlignment = Takai.UI.Alignment.Start,
                VerticalAlignment = Takai.UI.Alignment.Middle,
                Margin = 5
            };

            if (Map == null)
            {
                //uiContainer = (Takai.UI.Element)Takai.Data.Serializer.TextDeserialize("test.ui.tk");
                //void SetFont(Takai.UI.Element zz)
                //{
                //    zz.Font = LargeFont;
                //    foreach (var child in zz.Children)
                //        SetFont(child);
                //    if (!(zz is Takai.UI.TextBox))
                //        zz.AutoSize();
                //}
                //SetFont(uiContainer);
                //Takai.Data.Serializer.TextSerialize("out.ui.tk", uiContainer);
                //return;

                var list = new Takai.UI.List()
                {
                    Name = "Menu",
                    HorizontalAlignment = Takai.UI.Alignment.Middle,
                    VerticalAlignment = Takai.UI.Alignment.Middle,
                    Size = GraphicsDevice.Viewport.Bounds.Size.ToVector2()
                };

                Takai.UI.Element elem;
                list.AddChild(elem = new Takai.UI.Element()
                {
                    Name = "Title",
                    Text = "No map loaded",
                    Font = LargeFont,
                    HorizontalAlignment = Takai.UI.Alignment.Middle
                });
                elem.AutoSize(20);

                list.AddChild(elem = new Takai.UI.Element()
                {
                    Name = "New",
                    Text = "Press Ctrl+N to create a new map",
                    Font = SmallFont,
                    HorizontalAlignment = Takai.UI.Alignment.Middle
                });
                elem.AutoSize(10);

                list.AddChild(elem = new Takai.UI.Element()
                {
                    Name = "Open",
                    Text = "or Ctrl+O to open a map",
                    Font = SmallFont,
                    HorizontalAlignment = Takai.UI.Alignment.Middle
                });
                elem.AutoSize(10);
                elem.OnClick += delegate { OpenMap(); };

                list.AddChild(elem = new Takai.UI.TextBox()
                {
                    Name = "Textbox",
                    Text = "Test",
                    Size = new Vector2(200, 30),
                    Font = SmallFont,
                    HorizontalAlignment = Takai.UI.Alignment.Middle
                });

                list.AutoSize();
                uiContainer.AddChild(list);

                Takai.Data.Serializer.TextSerialize("test.ui.tk", uiContainer);
            }
            else
                StartMap();
        }

        public override void Unload()
        {
            TouchPanel.EnabledGestures = GestureType.None;
            modes.Mode?.End();
        }

        public void StartMap()
        {
            Map.updateSettings = Takai.Game.MapUpdateSettings.Editor;
            Map.renderSettings.drawBordersAroundNonDrawingEntities = true;
            Map.renderSettings.drawGrids = true;

            renderSettingsConsole.RemoveAllChildren();
            foreach (var setting in Map.renderSettings.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                var checkbox = new Takai.UI.Checkbox()
                {
                    Text = BeautifyPropName(setting.Name),
                    Font = LargeFont,
                    HorizontalAlignment = Takai.UI.Alignment.Start,
                    VerticalAlignment = Takai.UI.Alignment.Middle,
                    IsChecked = (bool)setting.GetValue(Map.renderSettings)
                };
                checkbox.OnClick += delegate (Takai.UI.Element sender, Takai.UI.ClickEventArgs args)
                {
                    setting.SetValue(Map.renderSettings, ((Takai.UI.Checkbox)sender).IsChecked);
                };
                checkbox.AutoSize();

                renderSettingsConsole.AddChild(checkbox);
            }
            renderSettingsConsole.AutoSize();

            Map.ActiveCamera = new Takai.Game.Camera();

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
                modes.AddMode(new FluidsEditorMode(this));
                modes.AddMode(new EntitiesEditorMode(this));
                modes.AddMode(new GroupsEditorMode(this));
                modes.AddMode(new PathsEditorMode(this));
                modes.AddMode(new TriggersEditorMode(this));
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
            //Camera.Scale = MathHelper.Clamp(MathHelper.Min(xyScale.X, xyScale.Y), 0.1f, 1f);
            //Camera.Position = mapSize / 2;
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

        public override void Update(GameTime time)
        {
            var viewport = GraphicsDevice.Viewport.Bounds;
            uiContainer.Size = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

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

            Map.ActiveCamera.Viewport = viewport;

            lastWorldPos = currentWorldPos;
            currentWorldPos = Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);

            if (InputState.IsClick(Keys.F1))
            {
                Takai.Runtime.GameManager.NextState(new Game.Game() { map = Map });
                return;
            }

            if (InputState.IsPress(Keys.F2))
            {
                if (!renderSettingsConsole.RemoveFromParent())
                    uiContainer.AddChild(renderSettingsConsole);
            }

            if (InputState.IsPress(Keys.G))
                Map.renderSettings.drawGrids ^= true;

            if (InputState.IsPress(Keys.F4))
            {
                Map.ActiveCamera.Position = Vector2.Zero;
                Map.ActiveCamera.Scale = 1;
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
                                Map.ActiveCamera.Position -= Vector2.TransformNormal(gesture.Delta2, Matrix.Invert(Map.ActiveCamera.Transform)) / 2;
                            }
                            //scale
                            else
                            {
                                var lp1 = gesture.Position - gesture.Delta;
                                var lp2 = gesture.Position2 - gesture.Delta2;
                                var dist = Vector2.Distance(gesture.Position2, gesture.Position2);
                                var ld = Vector2.Distance(lp1, lp2);

                                var scale = (dist / ld) / 1024;
                                Map.ActiveCamera.Scale += scale;
                            }
                            break;
                        }

                }
            }

            //camera
            if (InputState.IsButtonDown(MouseButtons.Middle))
            {
                Map.ActiveCamera.MoveTo(Map.ActiveCamera.Position + Vector2.TransformNormal(InputState.MouseDelta(), Matrix.Invert(Map.ActiveCamera.Transform)));
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
                    d = d * Map.ActiveCamera.MoveSpeed * (float)time.ElapsedGameTime.TotalSeconds; //(camera velocity)
                    Map.ActiveCamera.Position += Vector2.TransformNormal(d, Matrix.Invert(Map.ActiveCamera.Transform));
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
                    Map.ActiveCamera.Rotation += delta;
                }
                else
                {
                    Map.ActiveCamera.Scale += delta;
                    if (System.Math.Abs(Map.ActiveCamera.Scale - 1) < 0.1f) //snap to 100% when near
                        Map.ActiveCamera.Scale = 1;
                    else
                        Map.ActiveCamera.Scale = MathHelper.Clamp(Map.ActiveCamera.Scale, 0.1f, 2f);
                }
                //todo: translate to mouse cursor
            }

            Map.ActiveCamera.Scale = MathHelper.Clamp(Map.ActiveCamera.Scale, 0.1f, 10f); //todo: make global and move to some game settings
            Map.ActiveCamera.Update(time);
            Map.Update(time);
        }

        Vector2 CenterInRect(Vector2 size, Rectangle region)
        {
            return new Vector2(region.X + (region.Width - size.X) / 2, region.Y + (region.Height - size.Y) / 2);
        }

        public override void Draw(GameTime time)
        {
            if (Map == null)
            {
                var t = time.TotalGameTime.TotalSeconds;
                //GraphicsDevice.Clear(new Color(24, 24, 24)); //todo: replace w/ graphic maybe
                GraphicsDevice.Clear(new Color(
                   (float)System.Math.Sin(0.3f * t + 0) * 0.25f + 0.75f,
                   (float)System.Math.Sin(0.2f * t + 2) * 0.25f + 0.75f,
                   (float)System.Math.Sin(0.1f * t + 4) * 0.25f + 0.75f
                ));

                sbatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
                uiContainer.Draw(sbatch);
                sbatch.End();
                return;
            }

            //draw border around map
            Map.DrawRect(new Rectangle(0, 0, Map.Width * Map.TileSize, Map.Height * Map.TileSize), Color.Orange);
            Map.Draw();

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            //fps
            var sFps = (1 / time.ElapsedGameTime.TotalSeconds).ToString("N2");
            var sSz = DebugFont.MeasureString(sFps);
            DebugFont.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - sSz.X - 10, GraphicsDevice.Viewport.Height - sSz.Y - 10), Color.LightSteelBlue);

            DebugFont.Draw(sbatch, $"Zoom: {(Map.ActiveCamera.Scale * 100):N1}%", new Vector2(10, GraphicsDevice.Viewport.Height - sSz.Y - 10), Color.LightSteelBlue);

            //todo
            //draw selected item in top right corner
            //if (modes.Mode?.Selector != null)
            //{
            //    modes.Mode.Selector.DrawItem(time, modes.Mode.Selector.SelectedItem, selectorPreview.AbsoluteBounds, sbatch);
            //    Primitives2D.DrawRect(sbatch, Color.White, selectorPreview.AbsoluteBounds);
            //}

            sbatch.End();

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            uiContainer.Draw(sbatch);
            DebugFont.Draw(sbatch, Map.debugOut, new Vector2(100), Color.ForestGreen);
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

        static string BeautifyPropName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            var builder = new System.Text.StringBuilder(name.Length + 4);
            builder.Append(char.ToUpper(name[0]));
            for (int i = 1; i < name.Length; ++i)
            {
                if (char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                {
                    builder.Append(' ');
                    builder.Append(char.ToLower(name[i]));
                }
                else
                    builder.Append(name[i]);
            }

            return builder.ToString();
        }
    }
}