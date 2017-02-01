using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.Graphics;

namespace DyingAndMore.Editor
{
    partial class Editor : Takai.States.GameState
    {
        DecalIndex? selectedDecal = null;

        void UpdateDecalsMode(GameTime Time)
        {
            if (InputState.IsPress(MouseButtons.Left))
            {
                if (!SelectDecal(currentWorldPos) && map.Bounds.Contains(currentWorldPos))
                {
                    //add new decal none under cursor
                    var sel = selectors[(int)modeSelector.Mode] as DecalSelector;
                    map.AddDecal(sel.textures[sel.SelectedItem], currentWorldPos);
                    var pos = (currentWorldPos / map.SectorPixelSize).ToPoint();
                    selectedDecal = new DecalIndex { x = pos.X, y = pos.Y, index = map.Sectors[pos.Y, pos.X].decals.Count - 1 };
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
                    //todo: use swap
                    map.Sectors[selectedDecal.Value.y, selectedDecal.Value.x].decals.RemoveAt(selectedDecal.Value.index);
                    selectedDecal = null;
                }
            }

            else if (selectedDecal.HasValue)
            {
                var decal = map.Sectors[selectedDecal.Value.y, selectedDecal.Value.x].decals[selectedDecal.Value.index];

                if (InputState.IsButtonDown(MouseButtons.Left))
                {
                    var delta = currentWorldPos - lastWorldPos;
                    decal.position += delta;
                }

                if (InputState.IsButtonDown(Keys.R))
                {
                    var diff = currentWorldPos - decal.position;

                    var theta = (float)System.Math.Atan2(diff.Y, diff.X);
                    if (InputState.IsPress(Keys.R))
                        startRotation = theta - decal.angle;

                    decal.angle = theta - startRotation;
                }

                if (InputState.IsButtonDown(Keys.E))
                {
                    float dist = Vector2.Distance(currentWorldPos, decal.position);

                    if (InputState.IsPress(Keys.E))
                        startScale = dist;

                    decal.scale = MathHelper.Clamp(decal.scale + (dist - startScale) / 25, 0.25f, 10f);
                    startScale = dist;
                }

                if (InputState.IsPress(Keys.Delete))
                {
                    map.Sectors[selectedDecal.Value.y, selectedDecal.Value.x].decals.RemoveAt(selectedDecal.Value.index);
                    selectedDecal = null;
                }
                else
                    map.Sectors[selectedDecal.Value.y, selectedDecal.Value.x].decals[selectedDecal.Value.index] = decal;

                //todo: clone
            }

            lastWorldPos = currentWorldPos;
        }

        void DrawDecalsMode()
        {
            if (selectedDecal.HasValue)
            {
                var decal = map.Sectors[selectedDecal.Value.y, selectedDecal.Value.x].decals[selectedDecal.Value.index];
                var transform = Matrix.CreateScale(decal.scale) * Matrix.CreateRotationZ(decal.angle) * Matrix.CreateTranslation(new Vector3(decal.position, 0));

                var w2 = decal.texture.Width / 2;
                var h2 = decal.texture.Height / 2;
                var tl = Vector2.Transform(new Vector2(-w2, -h2), transform);
                var tr = Vector2.Transform(new Vector2(w2, -h2), transform);
                var bl = Vector2.Transform(new Vector2(-w2, h2), transform);
                var br = Vector2.Transform(new Vector2(w2, h2), transform);

                map.DrawLine(tl, tr, Color.GreenYellow);
                map.DrawLine(tr, br, Color.GreenYellow);
                map.DrawLine(br, bl, Color.GreenYellow);
                map.DrawLine(bl, tl, Color.GreenYellow);
            }
        }
    }
}