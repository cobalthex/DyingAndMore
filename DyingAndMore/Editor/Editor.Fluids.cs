using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Takai.Input;
using Takai;

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

            AddChild(preview = new Takai.UI.Graphic()
            {
                Sprite = new Takai.Graphics.Sprite(),
                Position = new Vector2(20),
                Size = new Vector2(64),
                HorizontalAlignment = Takai.UI.Alignment.End,
                VerticalAlignment = Takai.UI.Alignment.Start,
                BorderColor = Color.White
            });
            preview.Click += delegate
            {
                AddChild(selector);
            };

            selector = new Selectors.FluidSelector()
            {
                Size = new Vector2(320, 1),
                VerticalAlignment = Takai.UI.Alignment.Stretch,
                HorizontalAlignment = Takai.UI.Alignment.End
            };
            selector.SelectionChanged += delegate
            {
                if (selector.SelectedItem < 0)
                    return;

                var selectedFluid = selector.fluids[selector.SelectedItem]; //todo: can go into selector
                preview.Sprite.Texture = selectedFluid.Texture;
                preview.Sprite.ClipRect = selectedFluid.Texture.Bounds;
                preview.Sprite.Size = new Point(selectedFluid.Texture.Bounds.Width, selectedFluid.Texture.Bounds.Height);
            };
            selector.SelectedItem = 0;
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(Keys.Tab))
            {
                AddChild(selector);
                return false;
            }

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