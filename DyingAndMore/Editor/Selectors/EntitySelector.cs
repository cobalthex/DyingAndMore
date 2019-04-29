using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor.Selectors
{
    class EntitySelector : Selector
    {
        public string[] SearchPaths
        {
            get => _searchPaths;
            set
            {
                if (_searchPaths == value)
                    return;

                _searchPaths = value;
                RescanDirectories();
            }
        }
        string[] _searchPaths = new[] { "Actors", "Scenery", "Pickups" };

        public List<Takai.Game.EntityClass> ents = new List<Takai.Game.EntityClass>();
        System.TimeSpan elapsedTime;

        public Takai.Game.EntityClass SelectedEntity
        {
            get => SelectedIndex == -1 ? null : ents[SelectedIndex];
            set
            {
                SelectedIndex = ents.IndexOf(value);
            }
        }

        void RescanDirectories()
        {
            ents.Clear();
            elapsedTime = System.TimeSpan.Zero;
            if (SearchPaths == null)
                return;

            foreach (var path in SearchPaths)
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
            ItemCount = ents.Count;
        }

        public EntitySelector()
        {
            ItemSize = new Point(64, 64);

            //foreach (var obj in Takai.Data.Cache.LoadZip("Content/Actors.zip"))
            //{
            //    if (obj is Takai.Game.EntityClass ent)
            //        ents.Add(ent);
            //}
        }

        protected override void UpdateSelf(GameTime time)
        {
            elapsedTime = time.TotalGameTime;
            base.UpdateSelf(time);
        }

        public override void DrawItem(SpriteBatch spriteBatch, int itemIndex, Rectangle offsetBounds)
        {
            var ent = ents[itemIndex];

            var editorSprite = ent.EditorPreviewSprite.Value;
            if (editorSprite?.Texture != null)
            {
                offsetBounds.Offset(-OffsetContentArea.X, -OffsetContentArea.Y);
                DrawSprite(spriteBatch, editorSprite, offsetBounds);
            }
            else
            {
                //todo: constrain to content area
                offsetBounds.Inflate(-4, -4);
                Takai.Graphics.Primitives2D.DrawX(spriteBatch, Color.Tomato, offsetBounds);
                offsetBounds.Offset(0, 2);
                Takai.Graphics.Primitives2D.DrawX(spriteBatch, Color.Black, offsetBounds);
            }
        }
    }
}
