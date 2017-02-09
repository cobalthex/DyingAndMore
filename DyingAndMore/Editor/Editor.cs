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

        public Selector Selector { get; set; }

        public System.Action Start { get; set; }
        public System.Action End { get; set; }
        public System.Action<GameTime> Update { get; set; }
        public System.Action Draw { get; set; }
    }

    struct DecalIndex
    {
        public int x, y, index;
    }

    partial class Editor : Takai.GameState.GameState
    {
        public Takai.Game.Camera camera;

        SpriteBatch sbatch;

        BitmapFont tinyFont, smallFont, largeFont;

        public Takai.Game.Map map;

        Color highlightColor = Color.Gold;

        int uiMargin = 20;
        Takai.UI.Element uiContainer;
        Takai.UI.Element selectorPreview;

        public ModeSelector modes;

        Vector2 lastWorldPos, currentWorldPos;

        public Editor() : base(false, false) { }

        public override void Load()
        {
            tinyFont = Takai.AssetManager.Load<BitmapFont>("Fonts/rct2.bfnt");
            smallFont = Takai.AssetManager.Load<BitmapFont>("Fonts/UISmall.bfnt");
            largeFont = Takai.AssetManager.Load<BitmapFont>("Fonts/UILarge.bfnt");

            using (var file = new System.IO.StreamWriter("UISmall.fnt.tk"))
                Takai.Data.Serializer.TextSerialize(file, tinyFont);

            sbatch = new SpriteBatch(GraphicsDevice);

            uiContainer = new Takai.UI.Element();

            TouchPanel.EnabledGestures = GestureType.Pinch | GestureType.Tap | GestureType.DoubleTap | GestureType.FreeDrag;

            if (map == null)
            {
                var container = new Takai.UI.Element()
                {
                    HorizontalOrientation = Takai.UI.Orientation.Middle,
                    VerticalOrientation = Takai.UI.Orientation.Middle
                };

                var el = new Takai.UI.Element()
                {
                    Text = "No map loaded",
                    Font = largeFont,
                    HorizontalOrientation = Takai.UI.Orientation.Middle
                };
                el.AutoSize();
                container.AddChild(el);

                el = new Takai.UI.Element()
                {
                    Text = "Press Ctrl+N to create a new map",
                    Font = smallFont,
                    Position = new Vector2(0, 50),
                    HorizontalOrientation = Takai.UI.Orientation.Middle
                };
                el.AutoSize();
                container.AddChild(el);

                el = new Takai.UI.Element()
                {
                    Text = "or Ctrl+O to load one",
                    Font = smallFont,
                    Position = new Vector2(0, 75),
                    HorizontalOrientation = Takai.UI.Orientation.Middle
                };
                el.AutoSize();
                container.AddChild(el);

                container.AutoSize();
                uiContainer.AddChild(container);
                //todo: create list view (stack panel?)
            }
            else
                StartMap();
        }

        public override void Unload()
        {
            while (Takai.GameState.GameStateManager.TopState != this
                && Takai.GameState.GameStateManager.Count > 0)
                Takai.GameState.GameStateManager.PopState();

            TouchPanel.EnabledGestures = GestureType.None;
        }

        public void StartMap()
        {
            camera = new Takai.Game.Camera(map)
            {
                Viewport = GraphicsDevice.Viewport.Bounds
            };

            map.updateSettings = Takai.Game.MapUpdateSettings.Editor;

            uiContainer = new Takai.UI.Element();
            uiContainer.AddChild(modes = new ModeSelector(largeFont, smallFont)
            {
                HorizontalOrientation = Takai.UI.Orientation.Middle,
                VerticalOrientation = Takai.UI.Orientation.Start,
                Position = new Vector2(0, 40)
            });

            uiContainer.AddChild(selectorPreview = new Takai.UI.Element()
            {
                HorizontalOrientation = Takai.UI.Orientation.End,
                VerticalOrientation = Takai.UI.Orientation.Start,
                Position = new Vector2(10),
                Size = new Vector2(map.TileSize)
            });
            uiContainer.Size = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            selectorPreview.OnClick += delegate
            {
                OpenCurrentSelector(true);
                isPosSaved = false;
            };

            selectedDecal = null;
            selectedEntity = null;

            //start zoomed out to see the whole map
            var mapSize = new Vector2(map.Width, map.Height) * map.TileSize;
            var xyScale = new Vector2(GraphicsDevice.Viewport.Width - 20, GraphicsDevice.Viewport.Height - 20) / mapSize;
            camera.Scale = MathHelper.Clamp(MathHelper.Min(xyScale.X, xyScale.Y), 0.1f, 1f);
            camera.Position = mapSize / 2;
        }

        public bool OpenMap()
        {
            var ofd = new System.Windows.Forms.OpenFileDialog()
            {
                SupportMultiDottedExtensions = true,
                Filter = "Map (*.map.tk,*.map.tkz)|*.map.tk;*.map.tkz|All Files (*.*)|*.*",
                InitialDirectory = System.IO.Path.GetDirectoryName(map?.File),
                FileName = System.IO.Path.GetFileName(map?.File)
            };

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                map = new Takai.Game.Map(GraphicsDevice);
                using (var stream = ofd.OpenFile())
                {
                    if (ofd.FileName.EndsWith("tkz"))
                    {
                        using (var decompress = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress))
                            map.Load(decompress);
                    }
                    else
                        map.Load(stream);
                }

                map.File = ofd.FileName;
                StartMap();

                return true;
            }

            return false;
        }

        public bool SaveMap()
        {
            if (map == null)
                return false;

            var sfd = new System.Windows.Forms.SaveFileDialog()
            {
                SupportMultiDottedExtensions = true,
                Filter = "Map (*.map.tk)|*.map.tk|All Files (*.*)|*.*",
                InitialDirectory = System.IO.Path.GetDirectoryName(map.File),
                FileName = System.IO.Path.GetFileName(map.File?.Substring(0, map.File.IndexOf('.'))) //file dialog is retarded
            };

            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //todo: optional compression
                using (var stream = new System.IO.StreamWriter(sfd.OpenFile()))
                    Takai.Data.Serializer.TextSerialize(stream, map);

                map.File = sfd.FileName;
                return true;
            }

            return false;
        }

        private Rectangle lastViewport;
        public override void Update(GameTime Time)
        {
            var viewport = GraphicsDevice.Viewport.Bounds;
            if (viewport != lastViewport)
            {
                lastViewport = viewport;
                uiContainer.Size = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            }

            if (!uiContainer.Update(Time.ElapsedGameTime))
                return;

            if (InputState.IsMod(KeyMod.Control))
            {
                if ((InputState.IsPress(Keys.S) && SaveMap()) ||
                    (InputState.IsPress(Keys.O) && OpenMap()))
                    return;

                if (InputState.IsPress(Keys.Q))
                {
                    Takai.GameState.GameStateManager.Exit();
                    return;
                }
            }

            if (map == null)
                return;

            lastWorldPos = currentWorldPos;
            currentWorldPos = camera.ScreenToWorld(InputState.MouseVector);

            if (InputState.IsClick(Keys.F1))
            {
                Takai.GameState.GameStateManager.NextState(new Game.Game() { map = map });
                return;
            }

            if (InputState.IsPress(Keys.F2))
                map.debugOptions.showBlobReflectionMask ^= true;

            if (InputState.IsPress(Keys.F3))
                map.debugOptions.showOnlyReflections ^= true;

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
                                camera.Position -= Vector2.TransformNormal(gesture.Delta2, Matrix.Invert(camera.Transform)) / 2;
                            }
                            //scale
                            else
                            {
                                var lp1 = gesture.Position - gesture.Delta;
                                var lp2 = gesture.Position2 - gesture.Delta2;
                                var dist = Vector2.Distance(gesture.Position2, gesture.Position2);
                                var ld = Vector2.Distance(lp1, lp2);

                                var scale = (dist / ld) / 1024;
                                camera.Scale += scale;
                            }
                            break;
                        }

                }
            }

            //camera
            if (InputState.IsButtonDown(MouseButtons.Middle))
            {
                camera.MoveTo(camera.Position + Vector2.TransformNormal(InputState.MouseDelta(), Matrix.Invert(camera.Transform)));
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
                    d = d * camera.MoveSpeed * (float)Time.ElapsedGameTime.TotalSeconds; //(camera velocity)
                    camera.Position += Vector2.TransformNormal(d, Matrix.Invert(camera.Transform));
                }
            }

            //open current selector
            if (InputState.IsPress(Keys.Tab))
            {
                OpenCurrentSelector(false);
                return;
            }

            if (InputState.HasScrolled())
            {
                var delta = InputState.ScrollDelta() / 1024f;
                if (InputState.IsButtonDown(Keys.LeftShift))
                {
                    camera.Rotation += delta;
                }
                else
                {
                    camera.Scale += delta;
                    camera.Scale = MathHelper.Clamp(camera.Scale, 0.1f, 2f);
                }
                //todo: translate to mouse cursor
            }

            camera.Scale = MathHelper.Clamp(camera.Scale, 0.1f, 10f); //todo: make global and move to some game settings
            camera.Update(Time);

            switch (modes.Mode)
            {
                case EditorMode.Tiles:
                    UpdateTilesMode(Time);
                    break;
                case EditorMode.Decals:
                    UpdateDecalsMode(Time);
                    break;
                case EditorMode.Blobs:
                    UpdateBlobsMode(Time);
                    break;
                case EditorMode.Entities:
                    UpdateEntitiesMode(Time);
                    break;
                case EditorMode.Groups:
                    UpdateGroupsMode(Time);
                    break;
            }
        }

        bool SelectDecal(Vector2 WorldPosition)
        {
            //find closest decal
            var mapSz = new Vector2(map.Width, map.Height);
            var start = Vector2.Clamp((WorldPosition / map.SectorPixelSize) - Vector2.One, Vector2.Zero, mapSz).ToPoint();
            var end = Vector2.Clamp((WorldPosition / map.SectorPixelSize) + Vector2.One, Vector2.Zero, mapSz).ToPoint();

            selectedDecal = null;
            for (int y = start.Y; y < end.Y; y++)
            {
                for (int x = start.X; x < end.X; x++)
                {
                    for (var i = 0; i < map.Sectors[y, x].decals.Count; i++)
                    {
                        var decal = map.Sectors[y, x].decals[i];

                        //todo: transform worldPosition by decal matrix and perform transformed comparison
                        var transform = Matrix.CreateScale(decal.scale) * Matrix.CreateRotationZ(decal.angle) * Matrix.CreateTranslation(new Vector3(decal.position, 0));

                        if (Vector2.DistanceSquared(decal.position, WorldPosition) < decal.texture.Width * decal.texture.Width * decal.scale)
                        {
                            selectedDecal = new DecalIndex { x = x, y = y, index = i };
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        Vector2 CenterInRect(Vector2 Size, Rectangle Region)
        {
            return new Vector2(Region.X + (Region.Width - Size.X) / 2, Region.Y + (Region.Height - Size.Y) / 2);
        }

        void MapLineRect(Rectangle Rect, Color Color)
        {
            map.DrawLine(new Vector2(Rect.Left, Rect.Top), new Vector2(Rect.Right, Rect.Top), Color);
            map.DrawLine(new Vector2(Rect.Left, Rect.Top), new Vector2(Rect.Left, Rect.Bottom), Color);
            map.DrawLine(new Vector2(Rect.Right, Rect.Top), new Vector2(Rect.Right, Rect.Bottom), Color);
            map.DrawLine(new Vector2(Rect.Left, Rect.Bottom), new Vector2(Rect.Right, Rect.Bottom), Color);
        }

        public override void Draw(GameTime Time)
        {
            if (map == null)
            {
                GraphicsDevice.Clear(new Color(24, 24, 24)); //todo: replace w/ graphic maybe

                sbatch.Begin();
                uiContainer.Draw(sbatch);
                sbatch.End();
                return;
            }

            //draw border around map
            MapLineRect(new Rectangle(0, 0, map.Width * map.TileSize, map.Height * map.TileSize), Color.Orange);

            camera.Draw();

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            //fps
            var sFps = (1 / Time.ElapsedGameTime.TotalSeconds).ToString("N2");
            var sSz = tinyFont.MeasureString(sFps);
            tinyFont.Draw(sbatch, sFps, new Vector2(GraphicsDevice.Viewport.Width - sSz.X - 10, GraphicsDevice.Viewport.Height - sSz.Y - 10), Color.LightSteelBlue);

            switch (modes.Mode)
            {
                case EditorMode.Tiles:
                    DrawTilesMode();
                    break;
                case EditorMode.Decals:
                    DrawDecalsMode();
                    break;
                case EditorMode.Blobs:
                    DrawBlobsMode();
                    break;
                case EditorMode.Entities:
                    DrawEntitiesMode();
                    break;
                case EditorMode.Groups:
                    DrawGroupsMode();
                    break;
            }

            //draw selected item in top right corner
            if (selectors[(int)modes.Mode] != null)
            {
                selectors[(int)modes.Mode].DrawItem(Time, selectors[(int)modes.Mode].SelectedItem, selectorPreview.AbsoluteBounds, sbatch);
                Primitives2D.DrawRect(sbatch, Color.White, selectorPreview.AbsoluteBounds);
            }

            sbatch.End();

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            uiContainer.Draw(sbatch);
            sbatch.End();
        }

        static readonly Matrix arrowWingTransform = Matrix.CreateRotationZ(120);
        protected void DrawArrow(Vector2 Position, Vector2 Direction, float Magnitude)
        {
            var tip = Position + (Direction * Magnitude);
            map.DrawLine(Position, tip, Color.Yellow);

            Magnitude = MathHelper.Clamp(Magnitude * 0.333f, 5, 30);
            map.DrawLine(tip, tip - (Magnitude * Vector2.Transform(Direction, arrowWingTransform)), Color.Yellow);
            map.DrawLine(tip, tip - (Magnitude * Vector2.Transform(Direction, Matrix.Invert(arrowWingTransform))), Color.Yellow);
        }
    }
}