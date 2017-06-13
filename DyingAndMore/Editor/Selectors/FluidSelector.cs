using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore.Editor.Selectors
{
    class FluidSelector : Selector
    {
        public List<Takai.Game.FluidType> fluids;

        public FluidSelector(Editor editor)
            : base(editor)
        {
            fluids = new List<Takai.Game.FluidType>();
            foreach (var file in System.IO.Directory.EnumerateFiles("Defs\\Fluids", "*", System.IO.SearchOption.AllDirectories))
            {
                try
                {
                    var deserialized = Takai.Data.Serializer.TextDeserializeAll(file);
                    foreach (var obj in deserialized)
                    {
                        if (obj is Takai.Game.FluidType fluid)
                            fluids.Add(fluid);
                    }
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
