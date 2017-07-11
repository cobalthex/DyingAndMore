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
                        = $"`8df{BeautifyMemberName(_selectedEntity.Class.Name)}`x\n"
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
                            +  $"Controller: {actor.Controller?.GetType().Name}\n";
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
            VerticalAlignment = Takai.UI.Alignment.Stretch;
            HorizontalAlignment = Takai.UI.Alignment.Stretch;

            selector = new Selectors.EntSelector(editor)
            {
                Size = new Vector2(320, 1),
                VerticalAlignment = Takai.UI.Alignment.Stretch,
                HorizontalAlignment = Takai.UI.Alignment.End
            };
            selector.SelectionChanged += delegate
            {
                //var sprite = selector.ents[selector.SelectedItem].Sprites.GetEnumerator();
                //if (sprite.MoveNext())
                //{
                //    preview.Sprite = sprite.Current;
                //    preview.AutoSize();
                //}
                //else
                {
                    preview.Sprite = null;
                    preview.Size = new Vector2(32);
                }
            };

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
            selector.SelectedItem = 0;

            AddChild(entInfo = new Takai.UI.Static()
            {
                Position = new Vector2(20),
                HorizontalAlignment = Alignment.Start,
                VerticalAlignment = Alignment.End,
                Font = Font
            });
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

            if (SelectedEntity != null && InputState.IsPress(Keys.Space))
            {
                var modal = Takai.Data.Serializer.TextDeserialize<Static>("Defs/UI/Editor/PropsEditor.ui.tk");
                var props = GeneratePropSheet(SelectedEntity, DefaultFont, DefaultColor);
                props.HorizontalAlignment = Alignment.Stretch;
                var scrollBox = modal.FindChildByName("Props");
                scrollBox.AddChild(props);
                modal.FindChildByName("Apply").Click += delegate
                {
                    //todo: apply props. move info generation to separate function
                    var ent = SelectedEntity;
                    SelectedEntity = null;
                    SelectedEntity = ent;
                    modal.RemoveFromParent();
                };
                modal.FindChildByName("Cancel").Click += delegate
                {
                    modal.RemoveFromParent();
                };
                AddChild(modal);
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
                        Takai.Game.EntityClass ent;
                        using (var reader = new System.IO.StreamReader(ofd.OpenFile()))
                            ent = Takai.Data.Serializer.TextDeserialize(reader) as Takai.Game.EntityClass;

                        if (ent != null)
                            SelectedEntity = editor.Map.Spawn(ent, currentWorldPos, Vector2.UnitX, Vector2.Zero);
                    }

                    return false;
                }

                var searchRadius = /*isTapping*/ false ? 10 : 1;
                var selected = editor.Map.FindEntities(currentWorldPos, searchRadius, true);
                if (selected.Count < 1)
                {
                    if (editor.Map.Bounds.Contains(currentWorldPos) && selector.ents.Count > 0)
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
                var selected = editor.Map.FindEntities(currentWorldPos, 1, true);
                SelectedEntity = selected.Count > 0 ? selected[0] : null;

                return false;
            }
            else if (InputState.IsClick(MouseButtons.Right)/* || isDoubleTapping*/)
            {
                var searchRadius = /*isTapping*/ false ? 10 : 1;
                var selected = editor.Map.FindEntities(currentWorldPos, searchRadius, true);
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
                        var snapAngle = MathHelper.ToRadians(editor.config.snapAngle);
                        var theta = (float)System.Math.Atan2(diff.Y, diff.X);
                        theta = (float)System.Math.Round(theta / snapAngle) * snapAngle;
                        SelectedEntity.Direction = new Vector2(
                            (float)System.Math.Cos(theta),
                            (float)System.Math.Sin(theta)
                        );
                    }
                    else
                        SelectedEntity.Direction = Vector2.Normalize(diff);
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
            foreach (var ent in editor.Map.ActiveEnts)
            {
                editor.Map.DrawArrow(ent.Position, ent.Direction, ent.Radius * 1.5f, Color.Gold);
            }
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