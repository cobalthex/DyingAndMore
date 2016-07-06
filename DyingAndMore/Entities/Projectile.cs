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
            Radius = 12;
            base.Load();
        }

        public override void OnMapCollision(Point Tile, Vector2 Point)
        {
            Map.DebugLine(Point - new Vector2(12), Point + new Vector2(12), Color.GreenYellow);
            Map.DebugLine(Point + new Vector2(-12, 12), Point + new Vector2(12, -12), Color.GreenYellow);
            Map.Destroy(this);
        }

        public override void OnEntityCollision(Takai.Game.Entity Collider, Vector2 Point)
        {
            Map.DebugLine(Point - new Vector2(12), Point + new Vector2(12), Color.GreenYellow);
            Map.DebugLine(Point + new Vector2(-12, 12), Point + new Vector2(12, -12), Color.GreenYellow);
            Map.Destroy(this);
        }
    }
}
