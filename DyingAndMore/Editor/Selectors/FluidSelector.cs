using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore.Editor.Selectors
{
    class FluidSelector : Selector
    {
        public List<Takai.Game.FluidType> Fluids;

        public FluidSelector(Editor Editor) : base(Editor) { }

        public override void Load()
        {
            base.Load();

            base.ItemSize = new Point(64, 64);

            Fluids = new List<Takai.Game.FluidType>();
            foreach (var file in System.IO.Directory.EnumerateFiles("Defs\\Fluids", "*", System.IO.SearchOption.AllDirectories))
            {
                try
                {
                    using (var stream = new System.IO.StreamReader(file))
                    {
                        var Fluid = Takai.Data.Serializer.TextDeserialize(stream) as Takai.Game.FluidType;
                        if (Fluid != null)
                            Fluids.Add(Fluid);
                    }
                }
                catch { } //add diagnostic output
            }
            ItemCount = Fluids.Count;
            ItemSize = new Point(64);
            Padding = 5;
        }

        public override void DrawItem(GameTime Time, int ItemIndex, Rectangle Bounds, SpriteBatch Sbatch = null)
        {
            if (ItemIndex >= 0 && ItemIndex < Fluids.Count && Fluids[ItemIndex].Texture != null)
                (Sbatch ?? sbatch).Draw(Fluids[ItemIndex].Texture, Bounds, Color.White);
        }
    }
}
