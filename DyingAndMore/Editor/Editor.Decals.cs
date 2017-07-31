using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor
{
    class DecalsEditorMode : EditorMode
    {
        Takai.Game.Decal selectedDecal = null;
        float startRotation, startScale;

        Vector2 currentWorldPos, lastWorldPos;

        Selectors.DecalSelector selector;
        Takai.UI.Graphic preview;

        //todo: start,end to update selected

        public DecalsEditorMode(Editor editor)
            : base("Decals", editor)
        {
            VerticalAlignment = Takai.UI.Alignment.Stretch;
            HorizontalAlignment = Takai.UI.Alignment.Stretch;

            AddChild(preview = new Takai.UI.Graphic()
            {
                Sprite = new Takai.Graphics.Sprite(),
                Position = new Vector2(20),
                HorizontalAlignment = Takai.UI.Alignment.End,
                VerticalAlignment = Takai.UI.Alignment.Start,
                BorderColor = Color.White
            });
            preview.Click += delegate
            {
                AddChild(selector);
            };

            selector = new Selectors.DecalSelector(editor)
            {
                Size = new Vector2(320, 1),
                VerticalAlignment = Alignment.Stretch,
                HorizontalAlignment = Alignment.End
            };
            selector.SelectionChanged += delegate
            {
                var selectedDecal = selector.textures[selector.SelectedItem];
                preview.Sprite.Texture = selectedDecal;
                preview.Sprite.ClipRect = selectedDecal.Bounds;
                preview.Sprite.Size = selectedDecal.Bounds.Size;
                preview.Size = Vector2.Clamp(preview.Sprite.Size.ToVector2(),
                                             new Vector2(32), new Vector2(96));
            };
            selector.SelectedItem = 0;
        }

        public override void Start()
        {
            lastWorldPos = editor.Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);
        }

        Takai.Game.MapSector GetDecalSector(Takai.Game.Decal decal)
        {
            var sector = editor.Map.GetOverlappingSector(decal.position);
            return editor.Map.Sectors[sector.Y, sector.X];
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(Keys.Tab))
            {
                AddChild(selector);
                return false;
            }

            lastWorldPos = currentWorldPos;
            currentWorldPos = editor.Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);

            if (InputState.IsPress(MouseButtons.Left))
            {
                if (!SelectDecal(currentWorldPos) && editor.Map.Bounds.Contains(currentWorldPos))
                {
                    //add new decal under cursor
                    selectedDecal = editor.Map.AddDecal(selector.textures[selector.SelectedItem], currentWorldPos);
                    var pos = (currentWorldPos / editor.Map.SectorPixelSize).ToPoint();
                    return false;
                }
            }
            else if (InputState.IsPress(MouseButtons.Right))
            {
                SelectDecal(currentWorldPos);
                return false;
            }
            else if (InputState.IsClick(MouseButtons.Right))
            {
                var lastSelected = selectedDecal;
                SelectDecal(currentWorldPos);

                if (selectedDecal != null && selectedDecal.Equals(lastSelected))
                {
                    GetDecalSector(selectedDecal).decals.Remove(selectedDecal);
                    selectedDecal = null;
                }
                return false;
            }

            else if (selectedDecal != null)
            {
                if (InputState.IsButtonDown(MouseButtons.Left))
                {
                    var delta = currentWorldPos - lastWorldPos;
                    selectedDecal.position += delta;
                    return false;
                }

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
                    return false;
                }

                //todo: clone
            }

            return base.HandleInput(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var visibleRegion = editor.Map.ActiveCamera.VisibleRegion;
            var visibleSectors = editor.Map.GetOverlappingSectors(visibleRegion);

            for (var y = visibleSectors.Top; y < visibleSectors.Bottom; ++y)
            {
                for (var x = visibleSectors.Left; x < visibleSectors.Right; ++x)
                {
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
        }

        bool SelectDecal(Vector2 worldPosition)
        {
            //find closest decal
            var sectors = editor.Map.GetOverlappingSectors(new Rectangle((worldPosition - new Vector2(5)).ToPoint(), new Point(10)));

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
                        if (Vector2.DistanceSquared(decal.position, worldPosition) < decal.texture.Width * decal.texture.Height * decal.scale)
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