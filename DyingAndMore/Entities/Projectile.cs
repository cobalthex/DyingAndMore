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
            var tex = Takai.AssetManager.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("Textures/Powerball.png");
            Sprite = new Takai.Graphics.Graphic
            (
                tex,
                24,
                24,
                8,
                System.TimeSpan.FromMilliseconds(200),
                true
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
