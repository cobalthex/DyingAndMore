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
                _selectedEntity = value;
                entInfo.BindTo(value);
            }
        }
        Takai.Game.EntityInstance _selectedEntity;

        Vector2 currentWorldPos;

        Static entInfo;
        Static entEditor;

        public EntitiesEditorMode(Editor editor)
            : base("Entities", editor)
        {
            AddChild(entInfo = Takai.Data.Cache.Load<Static>("UI/Editor/EntityInfo.ui.tk").CloneHierarchy());
            entEditor = Takai.Data.Cache.Load<Static>("UI/Editor/EntityEditor.ui.tk").CloneHierarchy();

            On(PressEvent, OnPress);
            On(ClickEvent, OnClick);
            On(DragEvent, OnDrag);
        }

        protected override void UpdatePreview(int selectedItem)
        {
            if (selectedItem >= 0 && selector.ents[selectedItem].Animations.TryGetValue("EditorPreview", out var animation))
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

        protected UIEventResult OnPress(Static sender, UIEventArgs e)
        {
            var pea = (PointerEventArgs)e;

            var inputSearchRadius = pea.device == DeviceType.Touch ? 30 : 20;

            if (pea.button == 0)
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

                    return UIEventResult.Handled;
                }
#endif


                var selected = editor.Map.FindEntitiesInRegion(currentWorldPos, inputSearchRadius);
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
                MoveEnt(SelectedEntity, SelectedEntity.Position + delta, SelectedEntity.Position);
                return UIEventResult.Handled;
            }

            return UIEventResult.Continue;
        }

        protected override bool HandleInput(GameTime time)
        {
            currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);

            if (SelectedEntity != null)
            {
                if (InputState.IsButtonDown(Keys.R))
                {
                    //todo: sector modification?
                    var diff = currentWorldPos - SelectedEntity.Position;
                    Vector2 newForward;
                    if (InputState.IsMod(KeyMod.Shift))
                    {
                        var theta = (float)System.Math.Atan2(diff.Y, diff.X);
                        theta = (float)System.Math.Round(theta / editor.config.snapAngle) * editor.config.snapAngle;
                        newForward = new Vector2(
                            (float)System.Math.Cos(theta),
                            (float)System.Math.Sin(theta)
                        );
                    }
                    else
                        newForward = diff;
                    MoveEnt(SelectedEntity, SelectedEntity.Position, newForward);
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
                    entEditor.FocusFirstAvailable();
                    AddChild(entEditor);
                    return false;
                }
            }

            return base.HandleInput(time);
        }

        void MoveEnt(Takai.Game.EntityInstance ent, Vector2 newPosition, Vector2 newForward)
        {
            var sectors = editor.Map.GetOverlappingSectors(ent.AxisAlignedBounds);

            //todo: move to Map

            for (int y = sectors.Top; y < sectors.Bottom; ++y)
            {
                for (int x = sectors.Left; x < sectors.Right; ++x)
                    editor.Map.Sectors[y, x].entities.Remove(ent);
            }

            ent.Position = newPosition;
            ent.Forward = Vector2.Normalize(newForward);

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