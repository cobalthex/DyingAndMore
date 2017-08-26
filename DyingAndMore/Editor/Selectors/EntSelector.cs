using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor.Selectors
{
    class EntSelector : Selector
    {
        public List<Takai.Game.EntityClass> ents = new List<Takai.Game.EntityClass>();

        public EntSelector(Editor Editor)
            : base(Editor)
        {
            ItemSize = new Point(64);
            Padding = 5;

            foreach (var file in System.IO.Directory.EnumerateFiles("Defs\\Entities", "*", System.IO.SearchOption.AllDirectories))
            {
                try
                {
                    var ent = Takai.Data.Cache.Load<Takai.Game.EntityClass>(file);
                    if (ent is Game.Entities.ActorClass) //+ other classes
                        ents.Add(ent);
                }
                catch (System.Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not load Entity definitions from {file}:\n  {e}");
                }
            }

            ItemCount = ents.Count;
        }

        public override void DrawItem(SpriteBatch spriteBatch, int itemIndex, Rectangle bounds)
        {
            var ent = ents[itemIndex];

            if (ent.States.TryGetValue("Idle", out var state) && state.Sprite?.Texture != null)
                state.Sprite.Draw(spriteBatch, bounds, 0, Color.White, editor.Map.ElapsedTime);
            else
            {
                bounds.Inflate(-4, -4);
                Takai.Graphics.Primitives2D.DrawX(spriteBatch, Color.Tomato, bounds);
                bounds.Offset(0, 2);
                Takai.Graphics.Primitives2D.DrawX(spriteBatch, Color.Black, bounds);
            }
        }
    }
}
