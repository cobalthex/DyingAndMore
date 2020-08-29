using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor.Selectors
{
    class DecalSelector : UI.Selector
    {
        public List<Texture2D> textures;

        public DecalSelector()
        {
            textures = new List<Texture2D>();
            foreach (var file in EnumerateFiles("Decals", "*"))
            {
                try
                {
                    var tex = Takai.Data.Cache.Load<Texture2D>(file);
                    if (tex != null)
                        textures.Add(tex);
                }
                catch { } //todo
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
