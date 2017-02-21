using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class DecalsEditorMode : EditorMode
    {
        Takai.Game.Decal selectedDecal = null;
        float startRotation, startScale;

        Vector2 lastWorldPos;

        Selectors.DecalSelector selector;

        //todo: start,end to update selected

        public DecalsEditorMode(Editor editor)
            : base("Decals", editor)
        {
            selector = new Selectors.DecalSelector(editor);
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

        Takai.Game.MapSector GetDecalSector(Takai.Game.Decal decal)
        {
            var sector = editor.Map.GetSector(decal.position);
            return editor.Map.Sectors[sector.Y, sector.X];
        }

        public override void Update(GameTime time)
        {
            var currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);

            if (InputState.IsPress(MouseButtons.Left))
            {
                if (!SelectDecal(currentWorldPos) && editor.Map.Bounds.Contains(currentWorldPos))
                {
                    //add new decal under cursor
                    selectedDecal = editor.Map.AddDecal(selector.textures[selector.SelectedItem], currentWorldPos);
                    var pos = (currentWorldPos / editor.Map.SectorPixelSize).ToPoint();
                }
            }
            else if (InputState.IsPress(MouseButtons.Right))
            {
                SelectDecal(currentWorldPos);
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
            }

            else if (selectedDecal != null)
            {
                if (InputState.IsButtonDown(MouseButtons.Left))
                {
                    var delta = currentWorldPos - lastWorldPos;
                    selectedDecal.position += delta;
                }

                if (InputState.IsButtonDown(Keys.R))
                {
                    var diff = currentWorldPos - selectedDecal.position;

                    var theta = (float)System.Math.Atan2(diff.Y, diff.X);
                    if (InputState.IsPress(Keys.R))
                        startRotation = theta - selectedDecal.angle;

                    selectedDecal.angle = theta - startRotation;
                }

                if (InputState.IsButtonDown(Keys.E))
                {
                    float dist = Vector2.Distance(currentWorldPos, selectedDecal.position);

                    if (InputState.IsPress(Keys.E))
                        startScale = dist;

                    selectedDecal.scale = MathHelper.Clamp(selectedDecal.scale + (dist - startScale) / 25, 0.25f, 10f);
                    startScale = dist;
                }

                if (InputState.IsPress(Keys.Delete))
                {
                    GetDecalSector(selectedDecal).decals.Remove(selectedDecal);
                    selectedDecal = null;
                }

                //todo: clone
            }

            lastWorldPos = currentWorldPos;
        }

        public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            var visibleRegion = editor.Camera.VisibleRegion;
            var visibleSectors = editor.Map.GetVisibleSectors(visibleRegion);

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

                        var color = decal == selectedDecal ? Editor.ActiveColor : Editor.InactiveColor;
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
            var mapSz = new Vector2(editor.Map.Width, editor.Map.Height);
            var start = Vector2.Clamp((worldPosition / editor.Map.SectorPixelSize) - Vector2.One, Vector2.Zero, mapSz).ToPoint();
            var end = Vector2.Clamp((worldPosition / editor.Map.SectorPixelSize) + Vector2.One, Vector2.Zero, mapSz).ToPoint();

            selectedDecal = null;
            for (int y = start.Y; y < end.Y; y++)
            {
                for (int x = start.X; x < end.X; x++)
                {
                    for (var i = 0; i < editor.Map.Sectors[y, x].decals.Count; i++)
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
            return false;
        }
    }
}