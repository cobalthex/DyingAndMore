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
                    ents.Add(Takai.Data.Cache.Load<Takai.Game.EntityClass>(file));
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

            if (ent.States.TryGetValue(Takai.Game.EntStateId.Idle, out var state) && state.Sprite?.Texture != null)
            {
                //bounds.X += (int)state.Sprite.Origin.X;
                //bounds.Y += (int)state.Sprite.Origin.Y;
                state.Sprite.Draw(spriteBatch, bounds, 0, Color.White, editor.Map.ElapsedTime);
            }
            else
            {
                //todo: draw x as placeholder
                bounds.Inflate(-4, -4);
                Takai.Graphics.Primitives2D.DrawX(spriteBatch, Color.Tomato, bounds);
                bounds.Offset(0, 2);
                Takai.Graphics.Primitives2D.DrawX(spriteBatch, Color.Black, bounds);
            }
        }
    }
}
