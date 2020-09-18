using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.UI;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class SquadsEditorMode : EditorMode
    {
        public Game.Entities.Squad SelectedSquad { get; set; }

        protected Static nameUI;
        protected Static editUI;

        bool creatingSquad;
        Vector2 createOrigin;

        public SquadsEditorMode(Editor editor)
            : base("Squads", editor)
        {
            nameUI = Takai.Data.Cache.Load<Static>("UI/Editor/Name.ui.tk");
            editUI = Takai.Data.Cache.Load<Static>("UI/Editor/Squad.ui.tk");

            //todo: these should be context-free
            nameUI.CommandActions["Accept"] = delegate (Static sender, object argument)
            {
                sender.RemoveFromParent();
                editor.Map.Spawn(SelectedSquad);
                creatingSquad = false;
            };
            nameUI.CommandActions["Cancel"] = delegate (Static sender, object argument)
            {
                sender.RemoveFromParent();
                creatingSquad = false;
            };

            CommandActions["SelectedSquadSpawnUnits"] = delegate (Static sender, object argument)
            {
                var self = (SquadsEditorMode)sender;
                self.SelectedSquad?.SpawnUnits(self.editor.Map);
            };

            On(PressEvent, OnPress);
            On(ClickEvent, OnClick);
            On(DragEvent, OnDrag);
        }

        protected UIEventResult OnPress(Static sender, UIEventArgs e)
        {
            var pea = (PointerEventArgs)e;
            var worldPos = editor.Camera.ScreenToWorld(LocalToScreen(pea.position));

            if (pea.button == 0)
            {
                SelectedSquad = null;
                float lastDist = float.PositiveInfinity;
                if (editor.Map.Squads != null)
                {
                    foreach (var squad in editor.Map.Squads) //search backwards?
                    {
                        var dist = Vector2.DistanceSquared(squad.SpawnPosition, worldPos);
                        if (dist <= (squad.SpawnRadius * squad.SpawnRadius) &&
                            dist < lastDist)
                        {
                            lastDist = dist;
                            SelectedSquad = squad;
                        }
                    }

                    if (SelectedSquad != null)
                    {
                        if (InputState.IsMod(KeyMod.Control))
                        {
                            var newSquad = SelectedSquad.Clone();
                            newSquad.Name = Takai.Util.IncrementName(newSquad.Name);
                            editor.Map.Squads.Add(newSquad);
                            SelectedSquad = newSquad;
                        }

                        return UIEventResult.Handled;
                    }
                }
                creatingSquad = true;
                createOrigin = worldPos;
            }

            return UIEventResult.Continue;
        }

        protected UIEventResult OnClick(Static sender, UIEventArgs e)
        {
            if (creatingSquad)
            {
                if (SelectedSquad != null && SelectedSquad.SpawnRadius > 10)
                {
                    var ui = nameUI.CloneHierarchy();
                    ui.BindTo(SelectedSquad);
                    AddChild(ui);
                }
            }

            return UIEventResult.Continue;
        }

        protected UIEventResult OnDrag(Static sender, UIEventArgs e)
        {
            var dea = (DragEventArgs)e;

            if (dea.button == 0)
            {
                if (creatingSquad)
                {
                    var worldPos = editor.Camera.ScreenToWorld(LocalToScreen(dea.position));
                    if (SelectedSquad == null)
                        SelectedSquad = new Game.Entities.Squad { SpawnPosition = createOrigin };
                    SelectedSquad.SpawnRadius = System.Math.Max(10, Vector2.Distance(worldPos, createOrigin));
                }

                else if (SelectedSquad != null)
                {
                    SelectedSquad.SpawnPosition += editor.Camera.LocalToWorld(dea.delta);
                    return UIEventResult.Handled;
                }
            }

            return UIEventResult.Continue;
        }

        protected override void UpdateSelf(GameTime time)
        {
            base.UpdateSelf(time);
        }

        protected override bool HandleInput(GameTime time)
        {
            if (SelectedSquad?.Name != null)
            {
                if (InputState.IsPress(Keys.Delete))
                {
                    editor.Map.Squads.Remove(SelectedSquad);
                    return false;
                }

                if (InputState.IsPress(Keys.Space))
                {
                    var ui = editUI;//.CloneHierarchy();
                    ui.BindTo(SelectedSquad);
                    
                    //todo: changing name (if allowed?) will break map dictionary

                    AddChild(ui);
                    return false;
                }

                if (InputState.IsButtonHeld(Keys.R))
                {
                    var worldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);
                    SelectedSquad.SpawnRadius = System.Math.Max(10, Vector2.Distance(SelectedSquad.SpawnPosition, worldPos));
                }
            }

            if (InputState.IsPress(Keys.Escape))
            {
                creatingSquad = false;
                return false;
            }

            return base.HandleInput(time);
        }

        protected override void DrawSelf(DrawContext context)
        {
            base.DrawSelf(context);

            if (editor.Map.Squads != null && editUI.Parent == null)
            {
                foreach (var squad in editor.Map.Squads)
                {
                    editor.Map.DrawCircle(squad.SpawnPosition, squad.SpawnRadius, SelectedSquad == squad ? Color.Gold : Color.Cyan, 3);
                    
                    var squadNameSize = Font.MeasureString(squad.Name, TextStyle);
                    var drawText = new Takai.Graphics.DrawTextOptions(
                        squad.Name,
                        Font,
                        TextStyle,
                        Color.White,
                        editor.Camera.WorldToScreen(squad.SpawnPosition) - (squadNameSize / 2)
                    );
                    context.textRenderer.Draw(drawText);
                }
            }

            if (creatingSquad && SelectedSquad != null)
                editor.Map.DrawCircle(SelectedSquad.SpawnPosition, SelectedSquad.SpawnRadius, new Color(Color.White, 0.6f), 3, 4 * MathHelper.Pi, (float)editor.Map.RealTime.TotalGameTime.TotalSeconds * 40);
        }
    }
}
