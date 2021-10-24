using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Takai.Input;
using Takai.UI;
using Takai;
using Takai.Game;
using System.Collections.Generic;

namespace DyingAndMore.Editor
{
    //todo: initial map state should be updated as entities are updated (created/moved/deleted, etc)

    class EntitiesEditorMode : SelectorEditorMode<Selectors.EntitySelector>
    {
        private const int MaxSelectedEntityCount = 50;

        readonly List<EntityInstance> selectedEntities = new List<EntityInstance>();

        Vector2 currentWorldPos;

        Static entEditor;

        bool isBoxSelecting = false;
        Vector2 savedWorldPos;
        Rectangle selectRect;

        bool didClone = false;

        public Vector2 DefaultForward = -Vector2.UnitX; //load from config?

        public EntitiesEditorMode(Editor editor)
            : base("Entities", editor)
        {
            entEditor = Takai.Data.Cache.Load<Static>("UI/Editor/Entities/EntityEditor.ui.tk").CloneHierarchy();

            On(PressEvent, OnPress);
            On(DragEvent, OnDrag);
            On(DropEvent, OnDrop);
            On(ClickEvent, OnClick);
        }

        protected override void UpdatePreview(int selectedItem)
        {
            if (selectedItem >= 0 && selector.ents.Count > selectedItem &&
                (selector.ents[selectedItem].Animations.TryGetValue("EditorPreview", out var animation) ||
                 selector.ents[selectedItem].Animations.TryGetValue(selector.ents[selectedItem].DefaultBaseAnimation, out animation)))
            {
                preview.Sprite = animation.Sprite;
                //preview.Size = Vector2.Max(new Vector2(32), preview.Sprite?.Size.ToVector2() ?? new Vector2(32));
            }
            else
            {
                preview.Sprite = null;
                //preview.Size = new Vector2(32);
            }
        }

        public override void Start()
        {
            editor.Map.renderSettings.drawEntityForwardVectors = true;
            editor.Map.renderSettings.drawEntityHierarchies = true;
        }

        public override void End()
        {
            selectedEntities.Clear();
            editor.Map.renderSettings.drawEntityForwardVectors = false;
            editor.Map.renderSettings.drawEntityHierarchies = false;
        }

        bool ShouldMultiSelect() => InputState.IsMod(KeyMod.Shift);

        void SelectEntity(EntityInstance ent)//, bool testIfAlreadySelected = false)
        {
            if (ShouldMultiSelect())
            {
                if (selectedEntities.Count < MaxSelectedEntityCount)
                    //(!testIfAlreadySelected || !selectedEntities.Contains(ent)))
                    selectedEntities.Add(ent);
            }
            else
            {
                selectedEntities.Clear();
                selectedEntities.Add(ent);
            }
        }

        protected UIEventResult OnPress(Static sender, UIEventArgs e)
        {
            didClone = false;

            var pea = (PointerEventArgs)e;
            var worldPos = editor.Camera.ScreenToWorld(LocalToScreen(pea.position));

            var inputSearchRadius = pea.device == DeviceType.Touch ? 30 : 20;

            if (pea.button == 0)
            {
                // todo: check if already selected & exit if so

                // todo: limit selection size

                var selected = editor.Map.FindEntitiesInRegion(worldPos, inputSearchRadius);
                if (selected.Count > 0)
                {
                    var last = selected[selected.Count - 1];

                    if (selector.SelectedIndex < 0)
                    {
                        editor.Map.Destroy(last);
                        return UIEventResult.Handled;
                    }

                    //if (InputState.IsMod(KeyMod.Alt) && SelectedEntity != null)
                    //    editor.Map.Attach(selected[selected.Count - 1], SelectedEntity);

                    else if (!selectedEntities.Contains(last))
                    {

                        last.Velocity = Vector2.Zero;
                        SelectEntity(last);
                    }
                }
                else
                {
                    if (!ShouldMultiSelect())
                        selectedEntities.Clear();

                    savedWorldPos = worldPos;
                }

                return UIEventResult.Handled;
            }

            else if (pea.button == 1)
                selectedEntities.Clear();

            return UIEventResult.Continue;
        }

        protected UIEventResult OnDrag(Static sender, UIEventArgs e)
        {
            var dea = (DragEventArgs)e;

            if (dea.button == 0)
            {
                if (!didClone && InputState.IsMod(KeyMod.Control))
                {
                    for (int i = 0; i < selectedEntities.Count; ++i)
                    {
                        var clone = selectedEntities[i].Clone();
                        clone.Velocity = Vector2.Zero;
                        editor.Map.Spawn(clone);
                        selectedEntities[i] = clone; // in-place list swap
                    }
                    didClone = true;
                }

                if (selectedEntities.Count > 0)
                {
                    var delta = editor.Camera.LocalToWorld(dea.delta);
                    foreach (var ent in selectedEntities)
                        editor.Map.MoveEnt(ent, ent.Position + delta, ent.Forward);
                }
                else
                {
                    isBoxSelecting = true; // necessary?
                    selectRect = Util.AbsRectangle(savedWorldPos, currentWorldPos);
                }

                return UIEventResult.Handled;
            }

            return UIEventResult.Continue;
        }

        protected UIEventResult OnDrop(Static sender, UIEventArgs e)
        {
            if (!isBoxSelecting || (selectedEntities.Count == 0 && Util.Diagonal(selectRect) < 15))
            {
                return OnClick(sender, e);
                //return UIEventResult.Continue;
            }

            var ents = editor.Map.FindEntitiesInRegion(selectRect);
            if (ShouldMultiSelect())
            {
                foreach (var ent in ents)
                {
                    if (!selectedEntities.Contains(ent))
                        selectedEntities.Add(ent);
                }
            }
            else
            {
                selectedEntities.Clear();
                selectedEntities.AddRange(ents);
            }

            isBoxSelecting = false;
            return UIEventResult.Handled;
        }

        protected UIEventResult OnClick(Static sender, UIEventArgs e)
        {
            var pea = (PointerEventArgs)e;
            var worldPos = editor.Camera.ScreenToWorld(LocalToScreen(pea.position));

            if (pea.button == 0 &&
                selectedEntities.Count == 0 &&
                editor.Map.Class.Bounds.Contains(worldPos) &&
                selector.ents.Count > 0)
            {
                SelectEntity(editor.Map.Spawn(
                    selector.SelectedEntity,
                    worldPos,
                    DefaultForward,
                    Vector2.Zero
                ));

                return UIEventResult.Handled;
            }

            return UIEventResult.Continue;
        }

        protected override bool HandleInput(GameTime time)
        {
            currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);
            //todo: child entity movement can sometimes break
            if (selectedEntities.Count > 0)
            {
                if (InputState.IsButtonDown(Keys.R))
                {
                    foreach (var ent in selectedEntities)
                    {
                        // calculate fwd individually?
                        var relOffset = currentWorldPos - ent.WorldPosition;

                        // needs to take into account parent rotations
                        Vector2 newForward;
                        if (InputState.IsMod(KeyMod.Shift))
                        {
                            var theta = Util.Angle(relOffset);
                            theta = (float)System.Math.Round(theta / editor.config.snapAngle) * editor.config.snapAngle;
                            newForward = new Vector2(
                                (float)System.Math.Cos(theta),
                                (float)System.Math.Sin(theta)
                            );
                        }
                        else
                            newForward = relOffset;

                        editor.Map.MoveEnt(
                            ent,
                            ent.Position,
                            newForward
                        );
                    }
                    return false;
                }

                // duplicate selected entity and place under cursor
                if (InputState.IsPress(Keys.V))
                {
                    var relOffset = currentWorldPos - CalculateCollectiveCenter(selectedEntities);
                    for (int i = 0; i < selectedEntities.Count; ++i)
                    {
                        var clone = selectedEntities[i].Clone();
                        clone.Velocity = Vector2.Zero;
                        clone.SetPositionTransformed(clone.WorldPosition + relOffset);
                        editor.Map.Spawn(clone);
                        selectedEntities[i] = clone; // in-place list swap
                    }
                }

                if (InputState.IsPress(Keys.Delete))
                {
                    foreach (var ent in selectedEntities)
                        editor.Map.Destroy(ent);
                    selectedEntities.Clear();

                    return false;
                }

                if (InputState.IsPress(Keys.Space))
                {
                    if (selectedEntities.Count > 1)
                        editor.DisplayError("You can only edit the properties of one entity at a time");
                    else
                    {
                        entEditor.BindTo(selectedEntities[0]); //todo: this is blowing away internal bindings
                        entEditor.FocusFirstAvailable();
                        AddChild(entEditor);
                    }
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
#endif
            
            if (InputState.IsClick(MouseButtons.Left))
            {

            }

            return base.HandleInput(time);
        }

        Vector2 CalculateCollectiveCenter(List<EntityInstance> entities)
        {
            var center = Vector2.Zero;
            foreach (var ent in entities)
                center += ent.WorldPosition;

            if (entities.Count > 0)
                center /= entities.Count;

            return center;
        }

        protected override void DrawSelf(DrawContext context)
        {
            base.DrawSelf(context);

            if (isBoxSelecting)
                editor.Map.DrawRect(selectRect, Color.Yellow);

            // todo: draw outlines around all selected ents
            foreach (var ent in selectedEntities)
                editor.Map.DrawCircle(ent.WorldPosition, ent.Radius, Color.Gold);
        }
    }
}