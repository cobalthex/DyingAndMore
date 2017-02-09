using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.Graphics;

namespace DyingAndMore.Editor
{
    partial class Editor : Takai.GameState.GameState
    {
        Takai.Game.Entity selectedEntity = null;

        void UpdateEntitiesMode(GameTime Time)
        {
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
                            map.Spawn(ent);
                        }
                    }
                    return;
                }

                var searchRadius = /*isTapping*/ false ? 10 : 1;
                var selected = map.FindEntities(currentWorldPos, searchRadius, true);
                if (selected.Count < 1)
                {
                    if (map.Bounds.Contains(currentWorldPos))
                    {
                        var sel = selectors[(int)modes.Mode] as EntSelector;
                        if (sel != null && sel.ents.Count > 0)
                            selectedEntity = map.Spawn(sel.ents[sel.SelectedItem], currentWorldPos, Vector2.UnitX, Vector2.Zero);
                    }
                    else
                        selectedEntity = null;
                }
                else
                    selectedEntity = selected[0];
            }
            else if (InputState.IsPress(MouseButtons.Right))
            {
                var selected = map.FindEntities(currentWorldPos, 1, true);
                selectedEntity = selected.Count > 0 ? selected[0] : null;
            }
            else if (InputState.IsClick(MouseButtons.Right)/* || isDoubleTapping*/)
            {
                var searchRadius = /*isTapping*/ false ? 10 : 1;
                var selected = map.FindEntities(currentWorldPos, searchRadius, true);
                if (selected.Count > 0 && selected[0] == selectedEntity)
                {
                    map.Destroy(selectedEntity);
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
                    map.Destroy(selectedEntity);
                    selectedEntity = null;
                }
            }
        }

        void DrawEntitiesMode()
        {
            if (selectedEntity != null)
                DrawEntInfo(selectedEntity);

            foreach (var ent in map.ActiveEnts)
                DrawArrow(ent.Position, ent.Direction, ent.Radius * 1.5f);

            //draw basic info about entity screen
            smallFont.Draw(sbatch, $"Visible Entities: {map.ActiveEnts.Count}\nTotal Entities:   {map.TotalEntitiesCount}", new Vector2(uiMargin, 80), Color.LightSeaGreen);
        }


        static readonly string[] entInfoKeys = { "Name", "ID", "Type", "Position", "State" };
        protected void DrawEntInfo(Takai.Game.Entity Ent)
        {
            var props = new[] { Ent.Name, Ent.Id.ToString(), Ent.GetType().Name, Ent.Position.ToString(), Ent.State.ToString() };
            var font = tinyFont;

            var lineHeight = font.Characters[' '].Height;
            var totalHeight = (entInfoKeys.Length * lineHeight) + 10;

            var maxWidth = 0;
            for (var i = 0; i < entInfoKeys.Length; ++i)
            {
                var sz = font.Draw(sbatch, entInfoKeys[i] + ": ", new Vector2(10, GraphicsDevice.Viewport.Height - totalHeight + (lineHeight * i)), Color.White);
                maxWidth = MathHelper.Max(maxWidth, (int)sz.X);
            }

            for (var i = 0; i < props.Length; ++i)
                font.Draw(sbatch, props[i] ?? "Null", new Vector2(10 + maxWidth, GraphicsDevice.Viewport.Height - totalHeight + (lineHeight * i)), props[i] == null ? Color.Gray : Color.White);
        }
    }
}