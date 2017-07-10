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
                BorderColor = Color.White
            });
            preview.Click += delegate
            {
                AddChild(selector);
            };
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(Keys.Tab))
            {
                AddChild(selector);
                return false;
            }

            var selectedFluid = selector.fluids[selector.SelectedItem];

            var currentWorldPos = editor.Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);

            if (time.TotalGameTime > lastFluidTime + System.TimeSpan.FromMilliseconds(50))
            {
                lastFluidTime = time.TotalGameTime;

                if (InputState.IsButtonDown(MouseButtons.Left) && editor.Map.Bounds.Contains(currentWorldPos))
                {
                    editor.Map.Spawn(selectedFluid, currentWorldPos, Vector2.Zero);
                    return false;
                }

                if (InputState.IsButtonDown(MouseButtons.Right))
                {
                    var sectors = editor.Map.GetOverlappingSectors(new Rectangle((currentWorldPos - Vector2.One).ToPoint(), new Point(2)));
                    //get overlapping sectors
                    for (int y = sectors.Top; y < sectors.Bottom; ++y)
                    {
                        for (int x = sectors.Left; x < sectors.Right; ++x)
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

                    return false;
                }
            }

            return base.HandleInput(time);
        }
    }
}