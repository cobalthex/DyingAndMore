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
                catch { }
            }
            ItemCount = ents.Count;
            ItemSize = new Point(64);
            Padding = 5;
        }

        public override void DrawItem(GameTime Time, int ItemIndex, Rectangle Bounds, SpriteBatch Sbatch = null)
        {
            if (ItemIndex >= 0 && ItemIndex < ents.Count)
            {
                foreach (var key in ents[ItemIndex].State.ActiveStates)
                {
                    var state = ents[ItemIndex].State.States[key];
                    if (state.Sprite?.Texture != null)
                    {
                        Bounds.X += Bounds.Width / 2;
                        Bounds.Y += Bounds.Height / 2;
                        state.Sprite.Draw(Sbatch ?? sbatch, Bounds, 0, Color.White, Time.TotalGameTime);
                    }
                    else
                        ; //todo: draw some missing icon
                }
            }
        }
    }
}
