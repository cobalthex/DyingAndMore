﻿using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

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

            var searchPath = Path.Combine(Takai.Data.Cache.DefaultRoot, "Actors");
            foreach (var file in Directory.EnumerateFiles(searchPath, "*.ent.tk", SearchOption.AllDirectories))
            {
                try
                {
                    var ent = Takai.Data.Cache.Load<Takai.Game.EntityClass>(file);
                    if (ent is Game.Entities.ActorClass && ent.Animations != null) //+ other classes
                        ents.Add(ent);
                }
                catch (System.Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not load Entity definitions from {file}:\n  {e}");
                }
            }

            //foreach (var obj in Takai.Data.Cache.LoadZip("Content/Actors.zip"))
            //{
            //    if (obj is Takai.Game.EntityClass ent)
            //        ents.Add(ent);
            //}

            ItemCount = ents.Count;
        }

        public override void DrawItem(SpriteBatch spriteBatch, int itemIndex, Rectangle bounds)
        {
            var ent = ents[itemIndex];

            if (ent.Animations.TryGetValue("Idle", out var state) && state.Sprite?.Texture != null)
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
