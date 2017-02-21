using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.Graphics;

namespace DyingAndMore.Editor
{
    class EntitiesEditorMode : EditorMode
    {
        Takai.Game.Entity selectedEntity = null;

        Vector2 lastWorldPos;

        Selectors.EntSelector selector;

        public EntitiesEditorMode(Editor editor)
            : base("Entities", editor)
        {
            selector = new Selectors.EntSelector(editor);
            selector.Load();
        }

        public override void OpenConfigurator(bool DidClickOpen)
        {
            selector.DidClickOpen = DidClickOpen;
            Takai.Runtime.GameManager.PushState(selector);
        }

        public override void Start()
        {
            lastWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);
        }

        public override void End()
        {
            if (selectedEntity != null)
            {
                selectedEntity.OutlineColor = Color.Transparent;
                selectedEntity = null;
            }
        }

        public override void Update(GameTime Time)
        {
            var currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);

            if (selectedEntity != null)
                selectedEntity.OutlineColor = Color.Transparent;

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
                    return;
                }

                var searchRadius = /*isTapping*/ false ? 10 : 1;
                var selected = editor.Map.FindEntities(currentWorldPos, searchRadius, true);
                if (selected.Count < 1)
                {
                    if (editor.Map.Bounds.Contains(currentWorldPos))
                        selectedEntity = editor.Map.Spawn(selector.ents[selector.SelectedItem], currentWorldPos, Vector2.UnitX, Vector2.Zero);
                    else
                        selectedEntity = null;
                }
                else
                    selectedEntity = selected[0];
            }
            else if (InputState.IsPress(MouseButtons.Right))
            {
                var selected = editor.Map.FindEntities(currentWorldPos, 1, true);
                selectedEntity = selected.Count > 0 ? selected[0] : null;
            }
            else if (InputState.IsClick(MouseButtons.Right)/* || isDoubleTapping*/)
            {
                var searchRadius = /*isTapping*/ false ? 10 : 1;
                var selected = editor.Map.FindEntities(currentWorldPos, searchRadius, true);
                if (selected.Count > 0 && selected[0] == selectedEntity)
                {
                    editor.Map.Destroy(selectedEntity);
                    selectedEntity = null;
                }
            }

            else if (selectedEntity != null)
            {
                selectedEntity.OutlineColor = Color.YellowGreen;

                if (InputState.IsButtonDown(MouseButtons.Left))
                {
                    var delta = currentWorldPos - lastWorldPos;
                    selectedEntity.Position += delta;
                }

                if (InputState.IsButtonDown(Keys.R))
                {
                    var diff = currentWorldPos - selectedEntity.Position;
                    diff.Normalize();
                    selectedEntity.Direction = diff;
                }

                if (InputState.IsPress(Keys.Delete))
                {
                    editor.Map.Destroy(selectedEntity);
                    selectedEntity = null;
                }
            }

            lastWorldPos = currentWorldPos;
        }

        public override void Draw(SpriteBatch sbatch)
        {
            if (selectedEntity != null)
                DrawEntInfo(sbatch, selectedEntity);

            foreach (var ent in editor.Map.ActiveEnts)
                editor.DrawArrow(ent.Position, ent.Direction, ent.Radius * 1.5f);

            //draw basic info about entity screen
            editor.SmallFont.Draw(sbatch, $"Visible Entities: {editor.Map.ActiveEnts.Count}\nTotal Entities:   {editor.Map.TotalEntitiesCount}", new Vector2(20, 80), Color.LightSeaGreen);
        }


        static readonly string[] entInfoKeys = { "Name", "ID", "Type", "Position", "State" };
        protected void DrawEntInfo(SpriteBatch sbatch, Takai.Game.Entity ent)
        {
            var props = new[] { ent.Name, ent.Id.ToString(), ent.GetType().Name, ent.Position.ToString(), ent.State.ToString() };
            var font = editor.DebugFont;

            var lineHeight = font.Characters[' '].Height;
            var totalHeight = (entInfoKeys.Length * lineHeight) + 10;

            var maxWidth = 0;
            for (var i = 0; i < entInfoKeys.Length; ++i)
            {
                var sz = font.Draw(sbatch, entInfoKeys[i] + ": ", new Vector2(10, editor.GraphicsDevice.Viewport.Height - totalHeight + (lineHeight * i)), Color.White);
                maxWidth = MathHelper.Max(maxWidth, (int)sz.X);
            }

            for (var i = 0; i < props.Length; ++i)
                font.Draw(sbatch, props[i] ?? "Null", new Vector2(10 + maxWidth, editor.GraphicsDevice.Viewport.Height - totalHeight + (lineHeight * i)), props[i] == null ? Color.Gray : Color.White);
        }
    }
}