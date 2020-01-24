using Takai.UI;
using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.UI
{
    public class Balloon : Static
    {
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(3);

        private TimeSpan elapsedTime;

        public bool ClickToClose { get; set; } = true;

        public Balloon()
        {
            On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                var source = (Balloon)sender;
                if (source.ClickToClose)
                {
                    source.RemoveFromParent();
                    return UIEventResult.Handled;
                }
                return UIEventResult.Continue;
            });
        }

        public Balloon(string text)
            : this()
        {
            Text = text;
        }

        protected override void OnParentChanged(Static oldParent)
        {
            ResetTimer();
            base.OnParentChanged(oldParent);
        }

        protected override void UpdateSelf(GameTime time)
        {
            elapsedTime += time.ElapsedGameTime;
            if (elapsedTime > Duration)
                RemoveFromParent();
            base.UpdateSelf(time);
        }

        public void ResetTimer()
        {
            elapsedTime = TimeSpan.Zero;
        }

        //animation/opacity ?
    }
}
