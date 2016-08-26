﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore
{
    class DecalSelector : Selector
    {
        public List<Texture2D> textures;

        public DecalSelector(Editor Editor) : base(Editor) { }

        public override void Load()
        {
            base.Load();

            textures = new List<Texture2D>();
            foreach (var file in System.IO.Directory.EnumerateFiles("Data\\Textures\\Decals"))
            {
                var tex = Takai.AssetManager.Load<Texture2D>(file);
                if (tex != null)
                    textures.Add(tex);
            }
            ItemCount = textures.Count;
            ItemSize = new Point(64);
            Padding = 5;
        }

        public override void DrawItem(GameTime Time, int ItemIndex, Rectangle Bounds)
        {
            sbatch.Draw(textures[ItemIndex], Bounds, Color.White);
        }
    }
}
