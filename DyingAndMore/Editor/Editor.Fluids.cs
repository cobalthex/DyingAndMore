using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Takai.Input;
using Takai;

namespace DyingAndMore.Editor
{
    class FluidsEditorMode : SelectorEditorMode<Selectors.FluidSelector>
    {
        System.TimeSpan lastFluidTime = System.TimeSpan.Zero;

        public FluidsEditorMode(Editor editor)
            : base("Fluids", editor)
        {
            VerticalAlignment = Takai.UI.Alignment.Stretch;
            HorizontalAlignment = Takai.UI.Alignment.Stretch;
        }

        protected override void UpdatePreview(int selectedItem)
        {
            if (selectedItem < 0)
            {
                preview.Sprite.Texture = null;
                return;
            }

            var selectedFluid = selector.fluids[selector.SelectedItem];
            preview.Sprite.Texture = selectedFluid.Texture;
            preview.Sprite.ClipRect = selectedFluid.Texture.Bounds;
            preview.Sprite.Size = new Point(selectedFluid.Texture.Bounds.Width, selectedFluid.Texture.Bounds.Height);
        }

        protected override bool HandleInput(GameTime time)
        {
            var currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);

            if (selector.SelectedItem >= 0 && time.TotalGameTime > lastFluidTime + System.TimeSpan.FromMilliseconds(50))
            {
            var selectedFluid = selector.fluids[selector.SelectedItem];
                lastFluidTime = time.TotalGameTime;

                if (InputState.IsButtonDown(MouseButtons.Left) && editor.Map.Class.Bounds.Contains(currentWorldPos.ToPoint()))
                {
                    editor.Map.Spawn(selectedFluid, currentWorldPos, Vector2.Zero);
                    return false;
                }

                if (InputState.IsButtonDown(MouseButtons.Right))
                {
                    var sectors = editor.Map.GetOverlappingSectors(new Rectangle((int)currentWorldPos.X - 1, (int)currentWorldPos.Y - 1, 2, 2));
                    //get overlapping sectors
                    for (int y = sectors.Top; y < sectors.Bottom; ++y)
                    {
                        for (int x = sectors.Left; x < sectors.Right; ++x)
                        {
                            var sect = editor.Map.Sectors[y, x];
                            for (var i = 0; i < sect.fluids.Count; ++i)
                            {
                                var Fluid = sect.fluids[i];

                                if (Vector2.DistanceSquared(Fluid.position, currentWorldPos) < Fluid.Class.Radius * Fluid.Class.Radius)
                                {
                                    sect.fluids[i] = sect.fluids[sect.fluids.Count - 1];
                                    sect.fluids.RemoveAt(sect.fluids.Count - 1);
                                    --i;
                                }
                            }
                        }
                    }

                    return false;
                }
            }

            return base.HandleInput(time);
        }
    }
}