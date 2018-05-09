using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor.Selectors
{
    class DecalSelector : Selector
    {
        public List<Texture2D> textures;

        public DecalSelector(Editor Editor)
            : base(Editor)
        {
            textures = new List<Texture2D>();
            var searchPath = Path.Combine(Takai.Data.Cache.DefaultRoot, "Decals");
            foreach (var file in Directory.EnumerateFiles(searchPath, "*", SearchOption.AllDirectories))
            {
                var tex = Takai.Data.Cache.Load<Texture2D>(file);
                if (tex != null)
                    textures.Add(tex);
            }
            ItemCount = textures.Count;
            ItemSize = new Point(64, 64);
            Padding = 5;
        }

        public override void DrawItem(SpriteBatch spriteBatch, int itemIndex, Rectangle bounds)
        {
            spriteBatch.Draw(textures[itemIndex], bounds, Color.White);
        }
    }
}
