using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor.Selectors
{
    class EntitySelector : Selector
    {
        public List<Takai.Game.EntityClass> ents = new List<Takai.Game.EntityClass>();
        System.TimeSpan elapsedTime;

        public EntitySelector()
        {
            ItemSize = new Point(64, 64);

            var searchPaths = new[] { "Actors", "Scenery", "Pickups" };

            foreach (var path in searchPaths)
            {
                var searchPath = Path.Combine(Takai.Data.Cache.Root, path);
                int i = 0;
                foreach (var file in Directory.EnumerateFiles(searchPath, "*.ent.tk", SearchOption.AllDirectories))
                {
                    try
                    {
                        var ent = Takai.Data.Cache.Load<Takai.Game.EntityClass>(file);
                        if (ent.Animations != null)
                            ents.Add(ent);
                    }
                    catch (System.Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine($"{i} Could not load Entity definitions from {file}:\n  {e}");
                    }
                }
            }

            //foreach (var obj in Takai.Data.Cache.LoadZip("Content/Actors.zip"))
            //{
            //    if (obj is Takai.Game.EntityClass ent)
            //        ents.Add(ent);
            //}

            ItemCount = ents.Count;
        }

        protected override void UpdateSelf(GameTime time)
        {
            elapsedTime = time.TotalGameTime;
            base.UpdateSelf(time);
        }

        public override void DrawItem(SpriteBatch spriteBatch, int itemIndex, Rectangle bounds)
        {
            var ent = ents[itemIndex];

            if ((ent.Animations.TryGetValue("EditorPreview", out var state) ||
                 ent.Animations.TryGetValue(ent.DefaultBaseAnimation, out state)) && 
                state.Sprite?.Texture != null)
                state.Sprite.Draw(spriteBatch, bounds, 0, Color.White, elapsedTime);
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
