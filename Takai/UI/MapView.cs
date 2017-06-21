using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    /// <summary>
    /// A view of the map. Updating only occurs in Update()
    /// </summary>
    public class MapView : Static
    {
        public Game.Map Map
        {
            get => map;
            set
            {
                if (map != value)
                {
                    map = value;
                    MapChanged?.Invoke(this, System.EventArgs.Empty);
                    OnMapChanged(System.EventArgs.Empty);
                }
            }
        }
        private Game.Map map;

        /// <summary>
        /// Called whenever the map changes
        /// </summary>
        public System.EventHandler MapChanged;
        protected virtual void OnMapChanged(System.EventArgs e) { }

        public override void AutoSize(float padding = 0)
        {
            Size = new Vector2(800, 600) + new Vector2(padding);
        }

        protected override void UpdateSelf(GameTime time)
        {
            if (Map != null)
            {
                if (Map.ActiveCamera != null)
                    Map.ActiveCamera.Viewport = VisibleBounds;
                Map.Update(time);
            }
            base.UpdateSelf(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            Map.Draw();

            //todo: camera/viewport?
        }
    }
}
