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
            var tex = Takai.AssetManager.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("Textures/Tuber.png");
            Sprite = new Takai.Graphics.Graphic(tex);
            Sprite.CenterOrigin();
            base.Load();
        }

        public override void OnMapCollision(Point Tile)
        {
            Map.DestroyEntity(this);
        }
    }
}
