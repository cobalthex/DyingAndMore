using Microsoft.Xna.Framework;

namespace DyingAndMore.Entities
{
    class Projectile : Takai.Game.Entity
    {
        public Projectile()
        {
            AlwaysActive = true;
            IgnoreTrace = true;
        }

        public override void Load()
        {
            var tex = Takai.AssetManager.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("Textures/Blood.png");
            Sprite = new Takai.Graphics.Graphic
            (
                tex,
                10,
                10,
                6,
                System.TimeSpan.FromMilliseconds(200),
                Takai.Graphics.TweenStyle.Overlap,
                true
            );
            Sprite.CenterOrigin();
            Radius = Sprite.Width / 2;
            base.Load();
        }

        public override void OnMapCollision(Point Tile, Vector2 Point)
        {
            Map.Destroy(this);
        }

        public override void OnEntityCollision(Takai.Game.Entity Collider, Vector2 Point)
        {
            Map.Destroy(this);
        }
    }
}
