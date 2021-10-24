using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.UI;
using Takai;

namespace DyingAndMore.Editor
{
    class DecalsEditorMode :  SelectorEditorMode<Selectors.DecalSelector>
    {
        Takai.Game.Decal selectedDecal = null;
        float startRotation, startScale;

        Vector2 currentWorldPos, lastWorldPos;
        //todo: start,end to update selected

        bool isBatchDeleting = false;
        Vector2 savedWorldPos;
        Rectangle deleteRect;

        Takai.Graphics.Sprite previewSprite;

        public DecalsEditorMode(Editor editor)
            : base("Decals", editor)
        {
            previewSprite = preview.Sprite;

            On(PressEvent, OnPress);
            On(DragEvent, OnDrag);
        }

        protected UIEventResult OnPress(Static sender, UIEventArgs e)
        {
            var pea = (PointerEventArgs)e;
            if (pea.button == 0)
            {
                var worldPos = editor.Camera.ScreenToWorld(LocalToScreen(pea.position));

                if (SelectDecal(worldPos))
                {
                    if (selector.SelectedIndex < 0)
                    {
                        var sector = GetDecalSector(selectedDecal);
                        sector.decals.Remove(selectedDecal);
                        selectedDecal = null;
                    }
                }
                else if (selector.SelectedIndex >= 0 && editor.Map.Class.Bounds.Contains(worldPos))
                {
                    selectedDecal = editor.Map.AddDecal(selector.textures[selector.SelectedIndex], worldPos);
                    return UIEventResult.Handled;
                }
            }
            return UIEventResult.Continue;
        }

        protected UIEventResult OnDrag(Static sender, UIEventArgs e)
        {
            var dea = (DragEventArgs)e;
            if (dea.device == DeviceType.Mouse && dea.button != (int)MouseButtons.Left)
                return UIEventResult.Continue;

            if (selectedDecal != null)
            {
                var oldSector = GetDecalSector(selectedDecal);
                selectedDecal.position += editor.Camera.LocalToWorld(dea.delta);
                var newSector = GetDecalSector(selectedDecal);
                if (oldSector != newSector)
                {
                    oldSector.decals.Remove(selectedDecal);
                    newSector.decals.Add(selectedDecal);
                }
                return UIEventResult.Handled;
            }

            return UIEventResult.Continue;
        }

        protected override void UpdatePreview(int selectedItem)
        {
            if (preview.Sprite == null)
                preview.Sprite = previewSprite;

            var selectedDecal = selector.textures[selectedItem];
            preview.Sprite.Texture = selectedDecal;
            preview.Sprite.ClipRect = selectedDecal.Bounds;
            preview.Sprite.Size = new Point(selectedDecal.Bounds.Width, selectedDecal.Bounds.Height);
            preview.Size = Vector2.Clamp(preview.Sprite.Size.ToVector2(), new Vector2(32), new Vector2(96));
        }

        public override void Start()
        {
            lastWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);
        }

        Takai.Game.MapSector GetDecalSector(Takai.Game.Decal decal)
        {
            var sector = editor.Map.GetOverlappingSector(decal.position);
            return editor.Map.Sectors[sector.Y, sector.X];
        }

        protected override bool HandleInput(GameTime time)
        {
            lastWorldPos = currentWorldPos;
            currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);

            //todo: move to events
            if (selectedDecal != null)
            {
                bool didAct = false;
                if (InputState.IsButtonDown(Keys.R))
                {
                    var diff = currentWorldPos - selectedDecal.position;

                    var theta = (float)System.Math.Atan2(diff.Y, diff.X);

                    if (InputState.IsMod(KeyMod.Shift))
                    {
                        theta = (float)System.Math.Round(theta / editor.config.snapAngle) * editor.config.snapAngle;
                    }

                    if (InputState.IsPress(Keys.R))
                        startRotation = theta - selectedDecal.angle;

                    selectedDecal.angle = theta - startRotation;
                    didAct = true;
                }

                if (InputState.IsButtonDown(Keys.E))
                {
                    float dist = Vector2.Distance(currentWorldPos, selectedDecal.position);

                    if (InputState.IsPress(Keys.E))
                        startScale = dist;

                    selectedDecal.scale = MathHelper.Clamp(selectedDecal.scale + (dist - startScale) / 25, 0.25f, 10f);
                    startScale = dist;
                    didAct = true;
                }
                if (didAct)
                    return false;

                if (InputState.IsPress(Keys.Delete))
                {
                    GetDecalSector(selectedDecal).decals.Remove(selectedDecal);
                    selectedDecal = null;
                    //todo: fix sector movement
                    return false;
                }

                //todo: clone
            }

            //todo: move to events
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
                foreach (var sector in editor.Map.EnumeratateSectorsInRegion(deleteRect))
                {
                    for (int i = 0; i < sector.decals.Count; ++i)
                    {
                        if (deleteRect.Contains(sector.decals[i].position)) //todo: check texture bounds?
                            sector.decals.SwapAndDrop(i--);
                    }
                }

                return false;
            }

            return base.HandleInput(time);
        }

        protected override void DrawSelf(DrawContext context)
        {
            var visibleRegion = editor.Camera.VisibleRegion;
            var visibleSectors = editor.Map.GetOverlappingSectors(visibleRegion);

            int visibleDecals = 0;

            for (var y = visibleSectors.Top; y < visibleSectors.Bottom; ++y)
            {
                for (var x = visibleSectors.Left; x < visibleSectors.Right; ++x)
                {
                    visibleDecals += editor.Map.Sectors[y, x].decals.Count;
                    foreach (var decal in editor.Map.Sectors[y, x].decals)
                    {
                        var transform = Matrix.CreateScale(decal.scale)
                                      * Matrix.CreateRotationZ(decal.angle)
                                      * Matrix.CreateTranslation(new Vector3(decal.position, 0));

                        var w2 = decal.texture.Width / 2;
                        var h2 = decal.texture.Height / 2;
                        var tl = Vector2.Transform(new Vector2(-w2, -h2), transform);
                        var tr = Vector2.Transform(new Vector2(w2, -h2), transform);
                        var bl = Vector2.Transform(new Vector2(-w2, h2), transform);
                        var br = Vector2.Transform(new Vector2(w2, h2), transform);

                        var color = decal == selectedDecal ? Color.GreenYellow : Color.Purple; //todo
                        editor.Map.DrawLine(tl, tr, color);
                        editor.Map.DrawLine(tr, br, color);
                        editor.Map.DrawLine(br, bl, color);
                        editor.Map.DrawLine(bl, tl, color);
                    }
                }
            }

            DyingAndMoreGame.DebugDisplay("Visible Decals", visibleDecals);
        }

        bool SelectDecal(Vector2 worldPosition)
        {
            //find closest decal
            var sectors = editor.Map.GetOverlappingSectors(new Rectangle((int)worldPosition.X - 5, (int)worldPosition.Y - 5, 10, 10)); //todo: fuzzing here should be global setting


            for (int y = sectors.Top; y < sectors.Bottom; ++y)
            {
                for (int x = sectors.Left; x < sectors.Right; ++x)
                {
                    for (var i = 0; i < editor.Map.Sectors[y, x].decals.Count; ++i)
                    {
                        var decal = editor.Map.Sectors[y, x].decals[i];

                        //todo: transform worldPosition by decal matrix and perform transformed comparison
                        var transform = Matrix.CreateScale(decal.scale)
                                      * Matrix.CreateRotationZ(decal.angle)
                                      * Matrix.CreateTranslation(new Vector3(decal.position, 0));

                        //todo: use correct box checking (invert transform rectangle check)
                        if (Vector2.DistanceSquared(Vector2.Transform(Vector2.Zero, transform), worldPosition) < decal.texture.Width * decal.texture.Height * decal.scale)
                        {
                            selectedDecal = decal;
                            return true;
                        }
                    }
                }
            }
            selectedDecal = null;
            return false;
        }
    }
}