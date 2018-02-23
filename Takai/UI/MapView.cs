using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    /// <summary>
    /// A view of the map. Updating only occurs in Update()
    /// </summary>
    public class MapView : Static
    {
        /// <summary>
        /// Pause the update cycle of the map?
        /// </summary>
        public bool IsPaused { get; set; } = false;

        public Game.MapInstance Map
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
        private Game.MapInstance map;

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
            if (Map != null && !IsPaused)
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
