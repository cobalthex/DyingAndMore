using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class EntitiesEditorMode : EditorMode
    {
        public Takai.Game.Entity SelectedEntity
        {
            get => selectedEntity;
            set
            {
                selectedEntity = value;
                if (value == null)
                    entInfo.Text = "";
                else
                {
                    entInfo.Font = Font;
                    entInfo.Text
                        = $"Name: {(string.IsNullOrWhiteSpace(selectedEntity.Name) ? "(No Name)" : SelectedEntity.Name)}\n"
                        + $"ID: {selectedEntity.Id}\n"
                        + $"Position: {selectedEntity.Position}\n"
                        + $"Type: {selectedEntity.GetType().Name}\n"
                        + $"State: {selectedEntity.State}\n";
                    //controller, faction
                }
                entInfo.AutoSize();
            }
        }
        Takai.Game.Entity selectedEntity;

        Vector2 lastWorldPos;

        Selectors.EntSelector selector;
        Takai.UI.Graphic preview;

        Takai.UI.Static entInfo;

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


                //todo: ent preview
            };

            AddChild(preview = new Takai.UI.Graphic()
            {
                Position = new Vector2(20),
                HorizontalAlignment = Takai.UI.Alignment.End,
                VerticalAlignment = Takai.UI.Alignment.Start,
                OutlineColor = Color.White
            });
            preview.Click += delegate
            {
                AddChild(selector);
            };

            AddChild(entInfo = new Takai.UI.Static()
            {
                Position = new Vector2(20),
                HorizontalAlignment = Takai.UI.Alignment.Start,
                VerticalAlignment = Takai.UI.Alignment.End,
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

        protected override bool UpdateSelf(GameTime time)
        {
            //todo: add return false to cases and move things to use events

            if (InputState.IsPress(Keys.Tab))
            {
                AddChild(selector);
                return false;
            }

            if (!base.UpdateSelf(time))
                return false;

            var currentWorldPos = editor.Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);

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
                        Takai.Game.Entity ent;
                        using (var reader = new System.IO.StreamReader(ofd.OpenFile()))
                            ent = Takai.Data.Serializer.TextDeserialize(reader) as Takai.Game.Entity;

                        if (ent != null)
                        {
                            ent.Position = currentWorldPos;
                            editor.Map.Spawn(ent);
                        }
                    }

                    return false;
                }

                var searchRadius = /*isTapping*/ false ? 10 : 1;
                var selected = editor.Map.FindEntities(currentWorldPos, searchRadius, true);
                if (selected.Count < 1)
                {
                    if (editor.Map.Bounds.Contains(currentWorldPos))
                        SelectedEntity = editor.Map.Spawn(selector.ents[selector.SelectedItem], currentWorldPos, Vector2.UnitX, Vector2.Zero);
                    else
                        SelectedEntity = null;
                }
                else
                    SelectedEntity = selected[0];
            }
            else if (InputState.IsPress(MouseButtons.Right))
            {
                var selected = editor.Map.FindEntities(currentWorldPos, 1, true);
                SelectedEntity = selected.Count > 0 ? selected[0] : null;
            }
            else if (InputState.IsClick(MouseButtons.Right)/* || isDoubleTapping*/)
            {
                var searchRadius = /*isTapping*/ false ? 10 : 1;
                var selected = editor.Map.FindEntities(currentWorldPos, searchRadius, true);
                if (selected.Count > 0 && selected[0] == SelectedEntity)
                {
                    editor.Map.Destroy(SelectedEntity);
                    SelectedEntity = null;
                }
            }

            else if (SelectedEntity != null)
            {
                SelectedEntity.OutlineColor = Color.YellowGreen;

                if (InputState.IsButtonDown(MouseButtons.Left))
                {
                    var delta = currentWorldPos - lastWorldPos;
                    SelectedEntity.Position += delta;
                }

                if (InputState.IsButtonDown(Keys.R))
                {
                    var diff = currentWorldPos - SelectedEntity.Position;
                    diff.Normalize();
                    SelectedEntity.Direction = diff;
                }

                if (InputState.IsPress(Keys.Delete))
                {
                    editor.Map.Destroy(SelectedEntity);
                    SelectedEntity = null;
                }
            }

            lastWorldPos = currentWorldPos;

            return true;
        }
    }
}