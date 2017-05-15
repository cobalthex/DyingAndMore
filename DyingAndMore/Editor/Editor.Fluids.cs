using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class FluidsEditorMode : EditorMode
    {
        System.TimeSpan lastFluidTime = System.TimeSpan.Zero;

        Selectors.FluidSelector selector;
        Takai.UI.Graphic preview;

        public FluidsEditorMode(Editor editor)
            : base("Fluids", editor)
        {
            VerticalAlignment = Takai.UI.Alignment.Stretch;
            HorizontalAlignment = Takai.UI.Alignment.Stretch;

            selector = new Selectors.FluidSelector(editor)
            {
                Size = new Vector2(320, 1),
                VerticalAlignment = Takai.UI.Alignment.Stretch,
                HorizontalAlignment = Takai.UI.Alignment.End
            };
            selector.SelectionChanged += delegate
            {
                var selectedFluid = selector.fluids[selector.SelectedItem]; //todo: can go into selector
                preview.Sprite.Texture = selectedFluid.Texture;
                preview.Sprite.ClipRect = selectedFluid.Texture.Bounds;
                preview.Sprite.Size = selectedFluid.Texture.Bounds.Size;
            };

            AddChild(preview = new Takai.UI.Graphic()
            {
                Sprite = new Takai.Graphics.Sprite(),
                Position = new Vector2(20),
                Size = new Vector2(64),
                HorizontalAlignment = Takai.UI.Alignment.End,
                VerticalAlignment = Takai.UI.Alignment.Start,
                OutlineColor = Color.White
            });
            preview.Click += delegate
            {
                AddChild(selector);
            };
        }

        protected override bool UpdateSelf(GameTime time)
        {
            if (InputState.IsPress(Keys.Tab))
            {
                AddChild(selector);
                return false;
            }

            if (!base.UpdateSelf(time))
                return false;

            var selectedFluid = selector.fluids[selector.SelectedItem];

            var currentWorldPos = editor.Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);

            if (time.TotalGameTime > lastFluidTime + System.TimeSpan.FromMilliseconds(50))
            {
                if (InputState.IsButtonDown(MouseButtons.Left) && editor.Map.Bounds.Contains(currentWorldPos))
                    editor.Map.Spawn(selectedFluid, currentWorldPos, Vector2.Zero);

                else if (InputState.IsButtonDown(MouseButtons.Right))
                {
                    var mapSz = new Vector2(editor.Map.Width, editor.Map.Height);
                    var start = Vector2.Clamp((currentWorldPos / editor.Map.SectorPixelSize) - Vector2.One, Vector2.Zero, mapSz).ToPoint();
                    var end = Vector2.Clamp((currentWorldPos / editor.Map.SectorPixelSize) + Vector2.One, Vector2.Zero, mapSz).ToPoint();

                    for (int y = start.Y; y < end.Y; ++y)
                    {
                        for (int x = start.X; x < end.X; ++x)
                        {
                            var sect = editor.Map.Sectors[y, x];
                            for (var i = 0; i < sect.Fluids.Count; ++i)
                            {
                                var Fluid = sect.Fluids[i];

                                if (Vector2.DistanceSquared(Fluid.position, currentWorldPos) < Fluid.type.Radius * Fluid.type.Radius)
                                {
                                    sect.Fluids[i] = sect.Fluids[sect.Fluids.Count - 1];
                                    sect.Fluids.RemoveAt(sect.Fluids.Count - 1);
                                    --i;
                                }
                            }
                        }
                    }
                }

                lastFluidTime = time.TotalGameTime;
            }

            return true;
        }
    }
}