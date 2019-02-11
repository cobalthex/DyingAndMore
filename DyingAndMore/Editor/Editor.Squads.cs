﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.UI;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class SquadsEditorMode : EditorMode
    {
        public Game.Squad SelectedSquad { get; set; }

        public Takai.Graphics.Sprite SquadIcon { get; set; }

        protected Static nameUI;
        protected Static editUI;

        bool creatingSquad;
        Vector2 createOrigin;

        public SquadsEditorMode(Editor editor)
            : base("Squads", editor)
        {
            nameUI = Takai.Data.Cache.Load<Static>("UI/Editor/Name.ui.tk");
            editUI = Takai.Data.Cache.Load<Static>("UI/Editor/Squad.ui.tk");

            SquadIcon = new Takai.Graphics.Sprite(Takai.Data.Cache.Load<Texture2D>("UI/Editor/squad.png"));
            SquadIcon.CenterOrigin();

            On(PressEvent, OnPress);
            On(ClickEvent, OnClick);
            On(DragEvent, OnDrag);
        }

        protected UIEventResult OnPress(Static sender, UIEventArgs e)
        {
            var pea = (PointerEventArgs)e;

            var worldPos = editor.Camera.ScreenToWorld(pea.position);

            if (pea.button == 0)
            {
                foreach (var squad in editor.Map.Squads) //search backwards?
                {
                    if (Vector2.DistanceSquared(squad.Value.SpawnPosition, worldPos) 
                        <= (squad.Value.SpawnRadius * squad.Value.SpawnRadius))
                    {
                        SelectedSquad = squad.Value;
                        return UIEventResult.Handled;
                    }
                }

                SelectedSquad = null;
                creatingSquad = true;
                createOrigin = worldPos;
            }

            return UIEventResult.Continue;
        }

        protected UIEventResult OnClick(Static sender, UIEventArgs e)
        {
            var pea = (PointerEventArgs)e;

            if (creatingSquad)
            {
                if (SelectedSquad != null && SelectedSquad.SpawnRadius > 5)
                {
                    var ui = nameUI.CloneHierarchy();
                    ui.BindTo(SelectedSquad);
                    ui.CommandActions["Accept"] = delegate (Static source, object argument)
                    {
                        ui.RemoveFromParent();
                        editor.Map.Spawn(SelectedSquad);
                        creatingSquad = false;
                    };
                    ui.CommandActions["Cancel"] = delegate (Static source, object argument)
                    {
                        ui.RemoveFromParent();
                        creatingSquad = false;
                    };
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
                    var worldPos = editor.Camera.ScreenToWorld(dea.position);
                    if (SelectedSquad == null)
                        SelectedSquad = new Game.Squad { SpawnPosition = createOrigin };
                    SelectedSquad.SpawnRadius = Vector2.Distance(worldPos, createOrigin);
                }

                else if (SelectedSquad != null)
                {
                    SelectedSquad.SpawnPosition += editor.Camera.LocalToWorld(dea.delta);
                    return UIEventResult.Handled;
                }
            }

            return UIEventResult.Continue;
        }

        protected override bool HandleInput(GameTime time)
        {
            if (SelectedSquad?.Name != null)
            {
                if (InputState.IsPress(Keys.Delete))
                {
                    editor.Map.Squads.Remove(SelectedSquad.Name);
                    return false;
                }

                if (InputState.IsPress(Keys.Space))
                {
                    var ui = editUI.CloneHierarchy();
                    ui.BindTo(SelectedSquad);

                    AddChild(ui);
                }
            }

            return base.HandleInput(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            var clampScale = Takai.Util.Clamp(editor.Camera.ActualScale, 0.4f, 1);

            if (editor.Map.Squads != null)
            {
                foreach (var squad in editor.Map.Squads)
                {
                    editor.Map.DrawCircle(squad.Value.SpawnPosition, squad.Value.SpawnRadius, SelectedSquad == squad.Value ? Color.Gold : Color.Cyan);
                    SquadIcon.Draw(
                        spriteBatch,
                        editor.Camera.WorldToScreen(squad.Value.SpawnPosition),
                        0,
                        Color.White,
                        clampScale
                    );

                    var squadNameSize = DefaultFont.MeasureString(squad.Key);
                    DefaultFont.Draw(
                        spriteBatch,
                        squad.Key,
                        editor.Camera.WorldToScreen(squad.Value.SpawnPosition + new Vector2(0, squad.Value.SpawnRadius))
                            + new Vector2(squadNameSize.X / -2, 10 * clampScale),
                        Color.White
                    );
                }
            }

            if (creatingSquad && SelectedSquad != null)
                editor.Map.DrawCircle(SelectedSquad.SpawnPosition, SelectedSquad.SpawnRadius, new Color(Color.LightGoldenrodYellow, 0.6f));
        }
    }
}
