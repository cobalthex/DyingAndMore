using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Takai.Input;
using Takai.UI;
using Takai;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor
{
    //todo: initial map state should be updated as entities are updated (created/moved/deleted, etc)

    class EntitiesEditorMode : EditorMode
    {
        public Takai.Game.EntityInstance SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                _selectedEntity = value;
                entInfo.BindTo(value);
            }
        }
        Takai.Game.EntityInstance _selectedEntity;

        Vector2 lastWorldPos, currentWorldPos;

        Selectors.EntSelector selector;
        Graphic preview;

        Static entInfo;
        Static entEditor;

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
                if (selector.SelectedItem >= 0 && selector.ents[selector.SelectedItem].Animations.TryGetValue("EditorPreview", out var animation))
                {
                    preview.Sprite = animation.Sprite;
                    preview.Size = Vector2.Max(new Vector2(32), preview.Sprite?.Size.ToVector2() ?? new Vector2(32));
                }
                else
                {
                    preview.Sprite = null;
                    preview.Size = new Vector2(32);
                }
            };
            selector.SelectedItem = 0;

            AddChild(entInfo = Takai.Data.Cache.Load<Static>("UI/Editor/EntityInfo.ui.tk").Clone());
            entEditor = Takai.Data.Cache.Load<Static>("UI/Editor/EntityEditor.ui.tk").Clone();
        }

        public override void Start()
        {
            lastWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);
            editor.Map.renderSettings.drawEntityForwardVectors = true;
        }

        public override void End()
        {
            if (SelectedEntity != null)
            {
                SelectedEntity.OutlineColor = Color.Transparent;
                SelectedEntity = null;
            }
            editor.Map.renderSettings.drawEntityForwardVectors = false;
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(Keys.Tab))
            {
                AddChild(selector);
                return false;
            }

            lastWorldPos = currentWorldPos;
            currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);

            if (SelectedEntity != null)
                SelectedEntity.OutlineColor = Color.Transparent;

            if (InputState.IsPress(MouseButtons.Left))
            {
#if WINDOWS
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
#endif

                var searchRadius = 30; //increase if touch
                var selected = editor.Map.FindEntitiesInRegion(currentWorldPos, searchRadius);
                if (selected.Count < 1)
                {
                    if (editor.Map.Class.Bounds.Contains(currentWorldPos) && selector.ents.Count > 0)
                        SelectedEntity = editor.Map.Spawn(selector.ents[selector.SelectedItem], currentWorldPos, Vector2.UnitX, Vector2.Zero);
                    else
                        SelectedEntity = null;
                }
                else
                {
                    SelectedEntity = selected[0];
                    SelectedEntity.Velocity = Vector2.Zero;

                    if (InputState.IsMod(KeyMod.Control))
                    {
                        SelectedEntity = SelectedEntity.Clone();
                        editor.Map.Spawn(SelectedEntity);
                    }
                }

                return false;
            }
            else if (InputState.IsPress(MouseButtons.Right))
            {
                var selected = editor.Map.FindEntitiesInRegion(currentWorldPos, 1);
                SelectedEntity = selected.Count > 0 ? selected[0] : null;

                return false;
            }
            else if (InputState.IsClick(MouseButtons.Right)/* || isDoubleTapping*/)
            {
                var searchRadius = /*isTapping*/ false ? 10 : 1;
                var selected = editor.Map.FindEntitiesInRegion(currentWorldPos, searchRadius);
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

                if (InputState.IsPress(Keys.Space))
                {
                    entEditor.BindTo(SelectedEntity);
                    entEditor.BindCommands("$Close", (obj) => entEditor.RemoveFromParent());
                    AddChild(entEditor);
                    return false;
                }
            }

            return base.HandleInput(time);
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

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            DrawEntityInfo(spriteBatch);

            base.DrawSelf(spriteBatch);
        }

        //todo: make work in both editor and game
        void DrawEntityInfo(SpriteBatch spriteBatch)
        {
            //todo: only draw around cursor?

            //foreach (var ent in editor.Map.ActiveEntities)
            //{
            //    var pos = editor.Camera.WorldToScreen(ent.Position) + new Vector2(ent.Radius / 2 * editor.Camera.Scale);

            //    var sb = new System.Text.StringBuilder();
            //    sb.Append($"{ent.Id}: {ent.Name} {{{string.Join(",", ent.ActiveAnimations)}}}\n");

            //    if (ent is Game.Entities.ActorInstance actor)
            //    {
            //        sb.Append($"`f76{actor.CurrentHealth} {string.Join(",", actor.ActiveAnimations)}`x\n");
            //        if (actor.Weapon is Game.Weapons.GunInstance gun)
            //            sb.Append($"`bcf{gun.CurrentAmmo} {gun.State} {gun.Charge:N2}`x\n");
            //        if (actor.Controller is Game.Entities.AIController ai)
            //        {
            //            sb.Append($"`ad4{ai.Target}");
            //            for (int i = 0; i < ai.ChosenBehaviors.Length; ++i)
            //            {
            //                if (ai.ChosenBehaviors[i] != null)
            //                {
            //                    sb.Append("\n");
            //                    sb.Append(ai.ChosenBehaviors[i].ToString());
            //                }
            //            }
            //            sb.Append("`x\n");
            //        }
            //    }
            //    DefaultFont.Draw(spriteBatch, sb.ToString(), pos, Color.White);
            //}
        }
    }
}