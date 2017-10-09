using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor
{
    class EntitiesEditorMode : EditorMode
    {
        public Takai.Game.EntityInstance SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                _selectedEntity = value;
                if (value == null)
                    entInfo.Text = "";
                else
                {
                    entInfo.Font = Font;
                    entInfo.Text
                        = $"`8df{BeautifyMemberName(_selectedEntity.Class?.Name)}`x\n"
                        + $"Name: {(string.IsNullOrWhiteSpace(_selectedEntity.Name) ? "(No Name)" : SelectedEntity.Name)}\n"
                        + $"ID: {_selectedEntity.Id}\n"
                        + $"Position: {_selectedEntity.Position}\n"
                        + $"State: {_selectedEntity.State}\n";

                    if (_selectedEntity is Game.Entities.ActorInstance actor &&
                        actor.Class is Game.Entities.ActorClass @class)
                    {
                        entInfo.Text
                            += $"Health: {actor.CurrentHealth}/{@class.MaxHealth}\n"
                            +  $"Faction(s): {actor.Faction}\n"
                            +  $"Controller: {actor.Controller}\n"
                            +  $"Weapon: {actor.Weapon}\n";
                    }
                }
                entInfo.AutoSize();
            }
        }
        Takai.Game.EntityInstance _selectedEntity;

        Vector2 lastWorldPos, currentWorldPos;

        Selectors.EntSelector selector;
        Graphic preview;

        Static entInfo;

        public EntitiesEditorMode(Editor editor)
            : base("Entities", editor)
        {
            VerticalAlignment = Alignment.Stretch;
            HorizontalAlignment = Alignment.Stretch;

            AddChild(preview = new Graphic()
            {
                Position = new Vector2(20),
                HorizontalAlignment = Alignment.End,
                VerticalAlignment = Alignment.Start,
                BorderColor = Color.White,
                DrawXIfMissingSprite = true,
            });
            preview.Click += delegate
            {
                AddChild(selector);
            };

            selector = new Selectors.EntSelector(editor)
            {
                Size = new Vector2(320, 1),
                VerticalAlignment = Alignment.Stretch,
                HorizontalAlignment = Alignment.End
            };
            selector.SelectionChanged += delegate
            {
                var state = selector.ents[selector.SelectedItem].States.GetEnumerator();
                if (state.MoveNext())
                {
                    preview.Sprite = state.Current.Value?.Sprite;
                    preview.Size = Vector2.Max(new Vector2(32), preview.Sprite.Size.ToVector2());
                }
                else
                {
                    preview.Sprite = null;
                    preview.Size = new Vector2(32);
                }
            };
            selector.SelectedItem = 0;

            AddChild(entInfo = new Static()
            {
                Position = new Vector2(20),
                HorizontalAlignment = Alignment.Start,
                VerticalAlignment = Alignment.End,
                Font = Font
            });

            watcher = new FileSystemWatcher(Path.Combine(Takai.Data.Cache.DefaultRoot, "Actors"), "*.ent.tk")
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };
            watcher.Changed += Watcher_Changed;
        }
        ~EntitiesEditorMode()
        {
            watcher.Dispose();
        }

        FileSystemWatcher watcher;
        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            //todo: can use e.Name?
            System.Threading.Thread.Sleep(500);
            var path = e.FullPath.Replace(System.Environment.CurrentDirectory, "");
            try
            {
                var newClass = Takai.Data.Cache.Load<Takai.Game.EntityClass>(path, null, true);
                foreach (var ent in editor.Map.AllEntities)
                {
                    if (ent.Class.File == newClass.File)
                        ent.Class = newClass;
                }

                for (int i = 0; i < selector.ents.Count; ++i)
                {
                    if (selector.ents[i].File == newClass.File)
                        selector.ents[i] = newClass;
                }
                selector.SelectedItem = selector.SelectedItem;
            }
            catch { }
        }

        public override void Start()
        {
            lastWorldPos = editor.Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);
        }

        public override void End()
        {
            if (SelectedEntity != null)
            {
                SelectedEntity.OutlineColor = Color.Transparent;
                SelectedEntity = null;
            }
        }

        protected override void OnPress(ClickEventArgs e)
        {
            base.OnPress(e);
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(Keys.Tab))
            {
                AddChild(selector);
                return false;
            }

            lastWorldPos = currentWorldPos;
            currentWorldPos = editor.Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);

            if (SelectedEntity != null)
                SelectedEntity.OutlineColor = Color.Transparent;

            if (InputState.IsPress(MouseButtons.Left)/* || isTapping*/)
            {
                //load entity from file
                if (InputState.IsMod(KeyMod.Alt))
                {
                    var ofd = new System.Windows.Forms.OpenFileDialog()
                    {
                        Filter = "Entity Definitions (*.ent.tk)|*.ent.tk"
                    };
                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        var ent = Takai.Data.Cache.Load<Takai.Game.EntityClass>(ofd.FileName);
                        if (ent != null)
                            SelectedEntity = editor.Map.Spawn(ent, currentWorldPos, Vector2.UnitX, Vector2.Zero);
                    }

                    return false;
                }

                var searchRadius = /*isTapping*/ false ? 10 : 1;
                var selected = editor.Map.FindEntities(currentWorldPos, searchRadius);
                if (selected.Count < 1)
                {
                    if (editor.Map.Class.Bounds.Contains(currentWorldPos) && selector.ents.Count > 0)
                        SelectedEntity = editor.Map.Spawn(selector.ents[selector.SelectedItem], currentWorldPos, Vector2.UnitX, Vector2.Zero);
                    else
                        SelectedEntity = null;
                }
                else
                    SelectedEntity = selected[0];

                return false;
            }
            else if (InputState.IsPress(MouseButtons.Right))
            {
                var selected = editor.Map.FindEntities(currentWorldPos, 1);
                SelectedEntity = selected.Count > 0 ? selected[0] : null;

                return false;
            }
            else if (InputState.IsClick(MouseButtons.Right)/* || isDoubleTapping*/)
            {
                var searchRadius = /*isTapping*/ false ? 10 : 1;
                var selected = editor.Map.FindEntities(currentWorldPos, searchRadius);
                if (selected.Count > 0 && selected[0] == SelectedEntity)
                {
                    editor.Map.Destroy(SelectedEntity);
                    SelectedEntity = null;

                    return false;
                }
            }

            else if (SelectedEntity != null)
            {
                SelectedEntity.OutlineColor = Color.YellowGreen;

                if (InputState.IsButtonDown(MouseButtons.Left))
                {
                    var delta = currentWorldPos - lastWorldPos;
                    MoveEnt(SelectedEntity, _selectedEntity.Position + delta);

                    return false;
                }

                if (InputState.IsButtonDown(Keys.R))
                {
                    //todo: sector modification?
                    var diff = currentWorldPos - SelectedEntity.Position;
                    if (InputState.IsMod(KeyMod.Shift))
                    {
                        var theta = (float)System.Math.Atan2(diff.Y, diff.X);
                        theta = (float)System.Math.Round(theta / editor.config.snapAngle) * editor.config.snapAngle;
                        SelectedEntity.Forward = new Vector2(
                            (float)System.Math.Cos(theta),
                            (float)System.Math.Sin(theta)
                        );
                    }
                    else
                        SelectedEntity.Forward = Vector2.Normalize(diff);
                    return false;
                }

                if (InputState.IsPress(Keys.Delete))
                {
                    editor.Map.Destroy(SelectedEntity);
                    SelectedEntity = null;

                    return false;
                }
            }

            return base.HandleInput(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            foreach (var ent in editor.Map.EnumerateVisibleEntities())
                editor.Map.DrawArrow(ent.Position, ent.Forward, ent.Radius * 1.3f, Color.Gold);
        }

        void MoveEnt(Takai.Game.EntityInstance ent, Vector2 newPosition)
        {
            var sectors = editor.Map.GetOverlappingSectors(ent.AxisAlignedBounds);

            //todo: standarize somewhere?

            for (int y = sectors.Top; y < sectors.Bottom; ++y)
            {
                for (int x = sectors.Left; x < sectors.Right; ++x)
                    editor.Map.Sectors[y, x].entities.Remove(ent);
            }
            ent.Position = newPosition;

            sectors = editor.Map.GetOverlappingSectors(ent.AxisAlignedBounds);
            for (int y = sectors.Top; y < sectors.Bottom; ++y)
            {
                for (int x = sectors.Left; x < sectors.Right; ++x)
                    editor.Map.Sectors[y, x].entities.Add(ent);
            }
        }
    }
}