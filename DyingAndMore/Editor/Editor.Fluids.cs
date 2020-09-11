using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Takai.Input;
using Takai;
using Takai.UI;

namespace DyingAndMore.Editor
{
    class FluidsEditorMode : SelectorEditorMode<Selectors.FluidSelector>
    {
        System.TimeSpan elapsedTime = System.TimeSpan.Zero;
        System.TimeSpan nextFluidTime = System.TimeSpan.Zero;

        static System.TimeSpan fluidDelay = System.TimeSpan.FromMilliseconds(50);
        Takai.Graphics.Sprite previewSprite;

        bool isBatchDeleting = false;
        Vector2 savedWorldPos;
        Rectangle deleteRect;

        public FluidsEditorMode(Editor editor)
            : base("Fluids", editor)
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

                if (editor.Map.Class.Bounds.Contains(worldPos.ToPoint()))
                {
                    if (selector.SelectedIndex < 0 || 
                        (pea.device == DeviceType.Mouse && pea.button == (int)MouseButtons.Right))
                    {
                        var rect = new Rectangle(worldPos.ToPoint(), new Point(1));
                        if (pea.device == DeviceType.Touch)
                            rect.Inflate(5, 5);
                        DeleteFluids(rect);
                    }
                    else
                    {
                        editor.Map.Spawn(selector.fluids[selector.SelectedIndex], worldPos, Vector2.Zero);
                        nextFluidTime = elapsedTime + fluidDelay;
                    }
                }

                return UIEventResult.Handled;
            }
            return UIEventResult.Continue;
        }

        protected UIEventResult OnDrag(Static sender, UIEventArgs e)
        {
            var dea = (DragEventArgs)e;
            if (dea.button == 0)
            {
                var worldPos = editor.Camera.ScreenToWorld(LocalToScreen(dea.position));

                if ((elapsedTime >= nextFluidTime || dea.delta.LengthSquared() > 50) &&
                    editor.Map.Class.Bounds.Contains(worldPos.ToPoint()))
                {
                    if (selector.SelectedIndex < 0 ||
                        (dea.device == DeviceType.Mouse && dea.button == (int)MouseButtons.Right))
                    {
                        var rect = new Rectangle(worldPos.ToPoint(), new Point(1));
                        if (dea.device == DeviceType.Touch)
                            rect.Inflate(5, 5);
                        DeleteFluids(rect);
                    }
                    else
                    {
                        editor.Map.Spawn(selector.fluids[selector.SelectedIndex], worldPos, Vector2.Zero);
                        nextFluidTime = elapsedTime + fluidDelay;
                    }
                }

                return UIEventResult.Handled;
            }
            return UIEventResult.Handled;
        }

        protected override void UpdatePreview(int selectedItem)
        {
            if (preview.Sprite == null)
                preview.Sprite = previewSprite;

            var selectedFluid = selector.fluids[selector.SelectedIndex];
            preview.Sprite.Texture = selectedFluid.Texture;
            preview.Sprite.ClipRect = selectedFluid.Texture.Bounds;
            preview.Sprite.Size = new Point(selectedFluid.Texture.Bounds.Width, selectedFluid.Texture.Bounds.Height);
        }

        protected override void UpdateSelf(GameTime time)
        {
            elapsedTime = time.TotalGameTime;
            base.UpdateSelf(time);
        }

        void DeleteFluids(Rectangle rect)
        {
            var sectors = editor.Map.GetOverlappingSectors(rect);
            for (int y = sectors.Top; y < sectors.Bottom; ++y)
            {
                for (int x = sectors.Left; x < sectors.Right; ++x)
                {
                    var sect = editor.Map.Sectors[y, x];
                    for (var i = 0; i < sect.fluids.Count; ++i)
                    {
                        var fluid = sect.fluids[i];
                        var frect = new Rectangle(fluid.position.ToPoint(), new Point(1));
                        frect.Inflate(fluid.Class.Radius / 4, fluid.Class.Radius / 4);
                        if (rect.Intersects(frect))
                        {
                            sect.fluids[i] = sect.fluids[sect.fluids.Count - 1];
                            sect.fluids.RemoveAt(sect.fluids.Count - 1);
                            --i;
                        }
                    }
                }
            }
        }

        protected override bool HandleInput(GameTime time)
        {
            var currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);

            //move to events
            if (InputState.IsPress(Keys.X))
            {
                isBatchDeleting = true;
                savedWorldPos = currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);
                if (float.IsNaN(currentWorldPos.X) || float.IsNaN(currentWorldPos.Y))
                {
                    savedWorldPos = new Vector2();
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
                    for (int i = 0; i < sector.fluids.Count; ++i)
                    {
                        if (deleteRect.Contains(sector.fluids[i].position))
                            sector.fluids.SwapAndDrop(i--);
                    }
                }

                return false;
            }

            return base.HandleInput(time);
        }
    }
}