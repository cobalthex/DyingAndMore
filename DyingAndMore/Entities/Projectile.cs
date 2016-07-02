using Microsoft.Xna.Framework;

namespace DyingAndMore.Entities
{
    class Projectile : Takai.Game.Entity
    {
        public Projectile()
        {
            AlwaysActive = true;
        }

        public override void Load()
        {
            var tex = Takai.AssetManager.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("Textures/Powerball.png");
            Sprite = new Takai.Graphics.Graphic
            (
                tex,
                new Point(24, 24),
                null,
                null,
                8,
                System.TimeSpan.FromMilliseconds(200),
                Takai.AnimationOptions.All,
                Takai.Graphics.TweenStyle.Sequentially
            );
            Sprite.CenterOrigin();
            base.Load();
        }

        public override void OnMapCollision(Point Tile)
        {
            Map.Destroy(this);
        }

        public override void OnEntityCollision(Takai.Game.Entity Collider)
        {
            Map.Destroy(this);
        }
    }
}
