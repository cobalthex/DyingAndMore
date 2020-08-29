using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor.Selectors
{
    class FluidSelector : UI.Selector
    {
        public List<Takai.Game.FluidClass> fluids;

        public FluidSelector()
        {
            fluids = new List<Takai.Game.FluidClass>();
            foreach (var file in EnumerateFiles("Fluids", "*.fluid.tk"))
            {
                try
                {
                    fluids.Add(Takai.Data.Cache.Load<Takai.Game.FluidClass>(file));
                }
                catch (System.Exception)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not load Fluid definitions from {file}");
                }
            }

            ItemCount = fluids.Count;
            ItemSize = new Vector2(64, 64);
        }

        public override void DrawItem(SpriteBatch spriteBatch, int itemIndex, Rectangle bounds)
        {
            if (fluids[itemIndex].Texture == null)
                return;

            bounds.Offset(OffsetContentArea.Location);
            bounds = Rectangle.Intersect(bounds, VisibleContentArea);
            spriteBatch.Draw(fluids[itemIndex].Texture, bounds, Color.White);
        }
    }
}
