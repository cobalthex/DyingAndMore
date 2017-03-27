using Microsoft.Xna.Framework;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class FluidsEditorMode : EditorMode
    {
        System.TimeSpan lastFluidTime = System.TimeSpan.Zero;

        Selectors.FluidSelector selector;

        public FluidsEditorMode(Editor editor)
            : base("Fluids", editor)
        {
            selector = new Selectors.FluidSelector(editor);
            selector.Load();
        }

        public override void OpenConfigurator(bool DidClickOpen)
        {
            selector.DidClickOpen = DidClickOpen;
            Takai.Runtime.GameManager.PushState(selector);
        }

        public override void Update(GameTime time)
        {
            var currentWorldPos = editor.Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);

            if (time.TotalGameTime > lastFluidTime + System.TimeSpan.FromMilliseconds(50))
            {
                if (InputState.IsButtonDown(MouseButtons.Left) && editor.Map.Bounds.Contains(currentWorldPos))
                    editor.Map.Spawn(selector.Fluids[selector.SelectedItem], currentWorldPos, Vector2.Zero);

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
        }
    }
}