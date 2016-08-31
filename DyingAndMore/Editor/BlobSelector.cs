using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class BlobSelector : Selector
    {
        public List<Takai.Game.BlobType> blobs;

        public BlobSelector(Editor Editor) : base(Editor) { }

        public override void Load()
        {
            base.Load();

            base.ItemSize = new Point(64, 64);

            blobs = new List<Takai.Game.BlobType>();
            foreach (var file in System.IO.Directory.EnumerateFiles("Defs\\Blobs", "*", System.IO.SearchOption.AllDirectories))
            {
                using (var stream = new System.IO.StreamReader(file))
                {
                    var blob = Takai.Data.Serializer.TextDeserialize(stream) as Takai.Game.BlobType;
                    if (blob != null)
                        blobs.Add(blob);
                }
            }
            ItemCount = blobs.Count;
            ItemSize = new Point(64);
            Padding = 5;
        }

        public override void DrawItem(GameTime Time, int ItemIndex, Rectangle Bounds, SpriteBatch Sbatch = null)
        {
            if (ItemIndex >= 0 && ItemIndex < blobs.Count && blobs[ItemIndex].Texture != null)
                (Sbatch ?? sbatch).Draw(blobs[ItemIndex].Texture, Bounds, Color.White);
        }
    }
}
