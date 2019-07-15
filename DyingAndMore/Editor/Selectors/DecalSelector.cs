using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor.Selectors
{
    class DecalSelector : Selector
    {
        public List<Texture2D> textures;

        public DecalSelector()
        {
            textures = new List<Texture2D>();
            var searchPath = Path.Combine(Takai.Data.Cache.Root, "Decals");
            foreach (var file in Directory.EnumerateFiles(searchPath, "*", SearchOption.AllDirectories))
            {
                var tex = Takai.Data.Cache.Load<Texture2D>(file);
                if (tex != null)
                    textures.Add(tex);
            }
            ItemCount = textures.Count;
            ItemSize = new Vector2(64, 64);
            Padding = new Vector2(5);
        }

        public override void DrawItem(SpriteBatch spriteBatch, int itemIndex, Rectangle bounds)
        {
            bounds.Offset(OffsetContentArea.Location);
            bounds = Rectangle.Intersect(bounds, VisibleContentArea);
            spriteBatch.Draw(textures[itemIndex], bounds, Color.White);
        }
    }
}
