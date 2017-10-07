using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore.Editor.Selectors
{
    class FluidSelector : Selector
    {
        public List<Takai.Game.FluidClass> fluids;

        public FluidSelector(Editor editor)
            : base(editor)
        {
            fluids = new List<Takai.Game.FluidClass>();
            var searchPath = Path.Combine(Takai.Data.Cache.DefaultRoot, "Fluids");
            foreach (var file in Directory.EnumerateFiles(searchPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    fluids.Add(Takai.Data.Cache.Load<Takai.Game.FluidClass>(Path.Combine("Fluids", Path.GetFileName(file))));
                }
                catch (System.Exception)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not load Fluid definitions from {file}");
                }
            }

            ItemCount = fluids.Count;
            ItemSize = new Point(64);
        }

        public override void DrawItem(SpriteBatch spriteBatch, int ItemIndex, Rectangle Bounds)
        {
            if (fluids[ItemIndex].Texture != null)
                spriteBatch.Draw(fluids[ItemIndex].Texture, Bounds, Color.White);
        }
    }
}
