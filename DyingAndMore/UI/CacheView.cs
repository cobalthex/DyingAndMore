using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Takai.Data;
using Takai.Graphics;
using Takai.UI;

namespace DyingAndMore.UI
{
    class CacheView : List
    {
        ScrollBox previewContainer = new ScrollBox(new Static("None selected"))
        {
            HorizontalAlignment = Alignment.Stretch,
            VerticalAlignment = Alignment.Stretch,
            Padding = new Vector2(10),
        };
        List items = new List
        {
            Margin = 10,
        };

        public CacheView()
        {
            Direction = Direction.Horizontal;

            // todo: make not shit (maybe?)

            AddChildren(
                new ScrollBox(items)
                {
                    Size = new Vector2(400, AutoSize.Y),
                    VerticalAlignment = Alignment.Stretch,
                    Padding = new Vector2(10),
                },
                previewContainer
            );

            On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                if (e.Source.Parent != items)
                    return UIEventResult.Continue;

                if (e.Source.ChildIndex == 0)
                {
                    sender.RemoveFromParent();
                    return UIEventResult.Handled;
                }

                previewContainer.RemoveChildAt(0); // ReplaceAllChildren's fucked for types with shadow children

                var cacheItem = Cache.TryGet(e.Source.Name);

                if (cacheItem is Texture2D tex)
                    previewContainer.AddChild(new Graphic(tex));
                else if (cacheItem is Sprite spr)
                    previewContainer.AddChild(new Graphic(spr)); // display sprite atlas?
                else if (cacheItem is Effect fx)
                    previewContainer.AddChild(new Static($"[Effect] {fx.Name}"));
                else
                {
                    using (var serialized = new StringWriter())
                    {
                        Serializer.TextSerialize(serialized, cacheItem, serializeExternals: true);
                        previewContainer.AddChild(new Static(serialized.ToString())
                        {
                            HorizontalAlignment = Alignment.Center,
                            VerticalAlignment = Alignment.Center
                        });
                    }
                }

                return UIEventResult.Handled;
            });

            items.AddChild(new Static("[ Close ]"));
            foreach (var obj in Cache.Objects)
            {
                items.AddChild(new Static
                {
                    Text = obj.Key,
                    Name = obj.Key,
                });
            }
        }

        protected override void DrawSelf(DrawContext context)
        {
            base.DrawSelf(context);
        }
    }
}
