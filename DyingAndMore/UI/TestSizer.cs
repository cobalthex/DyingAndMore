using Microsoft.Xna.Framework;
using Takai.UI;

namespace DyingAndMore.UI
{
    class TestSizer : Static
    {
        protected override void UpdateSelf(GameTime time)
        {
            Size = new Vector2(400, 500) * (1 + (float)System.Math.Sin(time.TotalGameTime.TotalSeconds));
            base.UpdateSelf(time);
        }
    }
}
