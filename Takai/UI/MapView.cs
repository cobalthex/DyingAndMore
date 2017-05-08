using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class MapView : Static
    {
        public Game.Map Map { get; set; } = null;

        public override void AutoSize(float padding = 0)
        {
            Size = new Vector2(800, 600) + new Vector2(padding);
        }

        protected override bool UpdateSelf(GameTime time)
        {
            if (Map != null)
            {
                if (Map.ActiveCamera != null)
                    Map.ActiveCamera.Viewport = AbsoluteBounds;
                Map.Update(time);
            }
            return base.UpdateSelf(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            Map.Draw();

            //todo: camera/viewport?
        }
    }
}
