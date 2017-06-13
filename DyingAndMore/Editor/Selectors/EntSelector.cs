using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor.Selectors
{
    class EntSelector : Selector
    {
        public List<Takai.Game.Entity> ents;

        public EntSelector(Editor Editor)
            : base(Editor)
        {
            ItemSize = new Point(64);
            Padding = 5;

            ents = new List<Takai.Game.Entity>();
            foreach (var file in System.IO.Directory.EnumerateFiles("Defs\\Entities", "*", System.IO.SearchOption.AllDirectories))
            {
                try
                {
                    var deserialized = Takai.Data.Serializer.TextDeserializeAll(file);
                    foreach (var obj in deserialized)
                    {
                        if (obj is Takai.Game.Entity ent)
                            ents.Add(ent);
                    }
                }
                catch (System.Exception)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not load Entity definitions from {file}");
                }
            }

            ItemCount = ents.Count;
        }

        public override void DrawItem(SpriteBatch spriteBatch, int itemIndex, Rectangle bounds)
        {
            var ent = ents[itemIndex];

            bool didDraw = false;
            foreach (var sprite in ent.Sprites)
            {
                if (sprite?.Texture == null)
                    continue;

                bounds.X += bounds.Width / 2;
                bounds.Y += bounds.Height / 2;
                sprite.Draw(spriteBatch, bounds, 0, Color.White, editor.Map.ElapsedTime); //todo: correct time

                didDraw = true;
            }

#if DEBUG //Draw [X] in place of ent graphic
            if (!didDraw)
            {
                //todo: draw x as placeholder
            }
#endif
        }
    }
}
