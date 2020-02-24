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

    class EntitiesEditorMode : SelectorEditorMode<Selectors.EntitySelector>
    {
        public Takai.Game.EntityInstance SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                if (_selectedEntity != null)
                    _selectedEntity.OutlineColor = Color.Transparent;
                _selectedEntity = value;
                if (_selectedEntity != null)
                    _selectedEntity.OutlineColor = Color.Gold;
                entInfo.BindTo(value);
            }
        }
        Takai.Game.EntityInstance _selectedEntity;

        Vector2 currentWorldPos;

        Static entInfo;
        Static entEditor;

        bool isBatchDeleting = false;
        Vector2 savedWorldPos;
        Rectangle deleteRect;

        public EntitiesEditorMode(Editor editor)
            : base("Entities", editor)
        {
            AddChild(entInfo = Takai.Data.Cache.Load<Static>("UI/Editor/Entities/EntityInfo.ui.tk").CloneHierarchy());
            entEditor = Takai.Data.Cache.Load<Static>("UI/Editor/Entities/EntityEditor.ui.tk").CloneHierarchy();

            On(PressEvent, OnPress);
            On(ClickEvent, OnClick);
            On(DragEvent, OnDrag);
        }

        protected override void UpdatePreview(int selectedItem)
        {
            if (selectedItem >= 0 && selector.ents.Count > selectedItem &&
                (selector.ents[selectedItem].Animations.TryGetValue("EditorPreview", out var animation) ||
                 selector.ents[selectedItem].Animations.TryGetValue(selector.ents[selectedItem].DefaultBaseAnimation, out animation)))
            {
                preview.Sprite = animation.Sprite;
                preview.Size = Vector2.Max(new Vector2(32), preview.Sprite?.Size.ToVector2() ?? new Vector2(32));
            }
            else
            {
                preview.Sprite = null;
                preview.Size = new Vector2(32);
            }
        }

        public override void Start()
        {
            editor.Map.renderSettings.drawEntityForwardVectors = true;
            editor.Map.renderSettings.drawEntityHierarchies = true;
        }

        public override void End()
        {
            SelectedEntity = null;
            editor.Map.renderSettings.drawEntityForwardVectors = false;
            editor.Map.renderSettings.drawEntityHierarchies = false;
        }

        protected UIEventResult OnPress(Static sender, UIEventArgs e)
        {
            var pea = (PointerEventArgs)e;

            var inputSearchRadius = pea.device == DeviceType.Touch ? 30 : 20;

            if (pea.button == 0)
            {
                /*
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

                    return UIEventResult.Handled;
                }
#endif
                */
                var selected = editor.Map.FindEntitiesInRegion(currentWorldPos, inputSearchRadius);

                if (InputState.IsMod(KeyMod.Alt) && SelectedEntity != null && selected.Count > 0)
                {
                    editor.Map.Attach(selected[0], SelectedEntity);
                    return UIEventResult.Handled;
                }

                if (selected.Count < 1)
                {
                    if (editor.Map.Class.Bounds.Contains(currentWorldPos) && selector.ents.Count > 0)
                        SelectedEntity = editor.Map.Spawn(selector.ents[selector.SelectedIndex], currentWorldPos, Vector2.UnitX, Vector2.Zero);
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
                        //maintain hierarchy?
                        editor.Map.Spawn(SelectedEntity);
                    }
                }
            }

            else if (pea.button == (int)MouseButtons.Right) //mouse only?
            {
                var selected = editor.Map.FindEntitiesInRegion(currentWorldPos, inputSearchRadius);
                SelectedEntity = selected.Count > 0 ? selected[0] : null;
            }

            return UIEventResult.Continue;
        }

        protected UIEventResult OnClick(Static sender, UIEventArgs e)
        {
            var pea = (PointerEventArgs)e;

            if (pea.button == (int)MouseButtons.Right) //mouse only?
            {
                var searchRadius = /*isTapping*/ false ? 10 : 1;
                var selected = editor.Map.FindEntitiesInRegion(currentWorldPos, searchRadius);
                if (selected.Count > 0 && selected[0] == SelectedEntity)
                {
                    editor.Map.Destroy(SelectedEntity);
                    SelectedEntity = null;
                }
            }

            return UIEventResult.Continue;
        }

        protected UIEventResult OnDrag(Static sender, UIEventArgs e)
        {
            var dea = (DragEventArgs)e;

            if (dea.button == 0 && SelectedEntity != null)
            {
                var delta = editor.Camera.LocalToWorld(dea.delta);
                editor.Map.MoveEnt(SelectedEntity, SelectedEntity.Position + delta, SelectedEntity.Forward);
                return UIEventResult.Handled;
            }

            return UIEventResult.Continue;
        }

        protected override bool HandleInput(GameTime time)
        {
            currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);
            //todo: child entity movement can sometimes break
            if (SelectedEntity != null)
            {
                if (InputState.IsButtonDown(Keys.R))
                {
                    //needs to take into account parent rotations
                    var diff = currentWorldPos - SelectedEntity.WorldPosition;
                    Vector2 newForward;
                    if (InputState.IsMod(KeyMod.Shift))
                    {
                        var theta = Util.Angle(diff);
                        theta = (float)System.Math.Round(theta / editor.config.snapAngle) * editor.config.snapAngle;
                        newForward = new Vector2(
                            (float)System.Math.Cos(theta),
                            (float)System.Math.Sin(theta)
                        );
                    }
                    else
                        newForward = diff;

                    editor.Map.MoveEnt(
                        SelectedEntity, 
                        SelectedEntity.Position, 
                        newForward
                    );
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
                    entEditor.BindTo(SelectedEntity); //todo: this is blowing away internal bindings
                    entEditor.FocusFirstAvailable();
                    AddChild(entEditor);
                    return false;
                }
            }

#if DEBUG
            //fill map with whatever the current entity template is (primarily for stress testing)
            if (InputState.IsPress(Keys.OemCloseBrackets) && selector.SelectedEntity != null)
            {
                var ent = selector.SelectedEntity;
                var spacing = 100;
                var sz = (new Vector2(editor.Map.Class.Width, editor.Map.Class.Height) * editor.Map.Class.TileSize).ToPoint();
                var offset = new Vector2(10 + Util.RandomGenerator.Next(0, spacing / 2));
                for (var y = 0; y < sz.Y / spacing; ++y)
                {
                    for (var x = 0; x < sz.X / spacing; ++x)
                    {
                        var pos = offset + new Vector2(x, y) * new Vector2(spacing);

                        if (!editor.Map.Class.IsInsideMap(pos))
                            continue;

                        editor.Map.Spawn(ent, pos, Vector2.UnitX, Vector2.Zero);
                    }
                }
            }


            if (InputState.IsPress(Keys.X))
            {
                isBatchDeleting = true;
                savedWorldPos = currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);
                if (float.IsNaN(currentWorldPos.X) || float.IsNaN(currentWorldPos.Y))
                {
                    savedWorldPos = currentWorldPos = new Vector2();
                }
                return false;
            }

            if (isBatchDeleting)
            {
                deleteRect = Util.AbsRectangle(savedWorldPos, currentWorldPos);
                editor.Map.DrawRect(deleteRect, Color.Red);
            }

            if (InputState.IsClick(Keys.X))
            {
                isBatchDeleting = false;
                foreach (var ent in editor.Map.FindEntitiesInRegion(deleteRect))
                    editor.Map.Destroy(ent);

                return false;
            }
#endif

            return base.HandleInput(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            foreach (var squad in editor.Map.Squads)
            {
                editor.Map.DrawCircle(squad.SpawnPosition, squad.SpawnRadius, new Color(Color.Cyan, 0.6f), 3, 4 * MathHelper.Pi);

                var squadNameSize = Font.MeasureString(squad.Name);
                Font.Draw(
                    spriteBatch,
                    squad.Name,
                    editor.Camera.WorldToScreen(squad.SpawnPosition) - (squadNameSize / 2),
                    new Color(Color.White, 0.4f)
                );
            }

            DrawEntityInfo(spriteBatch);
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