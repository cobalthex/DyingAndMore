using Microsoft.Xna.Framework;
using Takai.UI;

namespace DyingAndMore.UI
{
    class DevtoolsMenu : List //preview menu?
    {
        System.Collections.Generic.List<Static> items = new System.Collections.Generic.List<Static>();
        int activeItem = -1;

        public DevtoolsMenu()
        {
            Direction = Direction.Vertical;
            Margin = 10;
            IsModal = true;

            On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (DevtoolsMenu)sender;

                if (e.Source.Parent != sender)
                    return UIEventResult.Continue;

                var ci = e.Source.ChildIndex;
                if (ci < 0 || ci >= self.items.Count)
                    return UIEventResult.Continue;

                self.AddChild(self.items[ci]);
                activeItem = ci;

                return UIEventResult.Handled;
            });

            items.Add(new EntViewer
            {
                Name = "Entity Class Inspector",
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch
            });
            //cache viewer

            foreach (var item in items)
                AddChild(new Static(item.Name));

            var close = new Static("Close");
            close.EventCommands["Click"] = "CloseModal";
            AddChild(close);
        }

        protected override bool HandleInput(GameTime time)
        {
            if (activeItem >= 0 && Takai.Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Escape))
            {
                RemoveChild(items[activeItem]);
                activeItem = -1;
                return false;
            }

            return base.HandleInput(time);
        }
    }
}
