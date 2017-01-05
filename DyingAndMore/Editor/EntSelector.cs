using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class EntSelector : Selector
    {
        public List<Takai.Game.Entity> ents;

        public EntSelector(Editor Editor) : base(Editor) { }

        public override void Load()
        {
            base.Load();

            base.ItemSize = new Point(64, 64);

            ents = new List<Takai.Game.Entity>();
            foreach (var file in System.IO.Directory.EnumerateFiles("Defs\\Entities", "*", System.IO.SearchOption.AllDirectories))
            {
                try
                {
                    using (var stream = new System.IO.StreamReader(file))
                    {
                        var ent = Takai.Data.Serializer.TextDeserialize(stream) as Takai.Game.Entity;
                        if (ent != null)
                            ents.Add(ent);
                    }
                }
                catch (DeviceLostException) { }
            }
            ItemCount = ents.Count;
            ItemSize = new Point(64);
            Padding = 5;
        }

        public override void DrawItem(GameTime Time, int ItemIndex, Rectangle Bounds, SpriteBatch Sbatch = null)
        {
            if (ItemIndex >= 0 && ItemIndex < ents.Count)
            {
                var ent = ents[ItemIndex];

                bool didDraw = false;
                foreach (var sprite in ent.Sprites)
                {
                    if (sprite?.Texture == null)
                        continue;

                    Bounds.X += Bounds.Width / 2;
                    Bounds.Y += Bounds.Height / 2;
                    sprite.Draw(Sbatch ?? sbatch, Bounds, 0, Color.White, Time.TotalGameTime);

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
}
