using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    class Projectile : Entity
    {
        ParticleType explosion;
        ParticleType trail, trailGlow;

        public Projectile()
        {
            AlwaysActive = true;
            IgnoreTrace = true;
            
            var curve = new Curve();
            curve.Keys.Add(new CurveKey(0, 0));
            curve.Keys.Add(new CurveKey(0.25f, 0.5f));
            curve.Keys.Add(new CurveKey(1, 1));

            explosion = new ParticleType();
            var tex = Takai.AssetManager.Load<Texture2D>("Textures/Particles/Blood.png");
            explosion.Graphic = new Takai.Graphics.Graphic
            (
                tex,
                10,
                10,
                6,
                System.TimeSpan.FromMilliseconds(200),
                Takai.Graphics.TweenStyle.Overlap,
                true
            );
            explosion.Graphic.CenterOrigin();
            explosion.Color = new ValueCurve<Color>(curve, Color.White, Color.Transparent);
            explosion.Scale = new ValueCurve<float>(1);
            explosion.Speed = new ValueCurve<float>(curve, 100, 0);

            trail = new ParticleType();
            tex = Takai.AssetManager.Load<Texture2D>("Textures/Particles/trail.png");
            trail.Graphic = new Takai.Graphics.Graphic(tex);
            trail.Graphic.CenterOrigin();
            trail.Color = new ValueCurve<Color>(curve, Color.White, Color.Transparent);
            trail.Scale = new ValueCurve<float>(1);
            trail.Speed = new ValueCurve<float>(curve, 100, 0);

            trailGlow = new ParticleType();
            tex = Takai.AssetManager.Load<Texture2D>("Textures/Particles/trailglow.png");
            trailGlow.Graphic = new Takai.Graphics.Graphic(tex);
            trailGlow.Graphic.CenterOrigin();
            trailGlow.BlendMode = BlendState.Additive;
            trailGlow.Color = new ValueCurve<Color>(curve, Color.White, Color.Transparent);
            trailGlow.Scale = new ValueCurve<float>(1);
            trailGlow.Speed = new ValueCurve<float>(curve, 100, 0);

            tex = Takai.AssetManager.Load<Texture2D>("Textures/Projectiles/Sharp.png");
            Sprite = new Takai.Graphics.Graphic(tex);
            Sprite.CenterOrigin();
            Radius = Sprite.Width / 2;
        }

        public override void Think(GameTime Time)
        {
            ParticleSpawn spawn = new ParticleSpawn();
            spawn.type = trail;
            spawn.position = new Range<Vector2>(Position - (Direction * 10), Position - (Direction * 10));
            spawn.lifetime = System.TimeSpan.FromSeconds(0.25);

            var angle = (float)System.Math.Atan2(Direction.Y, Direction.X);
            spawn.angle = new Range<float>(angle - 0.8f, angle + 0.8f);
            spawn.count = 1;
            Map.Spawn(spawn);

            spawn.type = trailGlow;
            spawn.position = new Range<Vector2>(Position - (Direction * 10) - new Vector2(5), Position - (Direction * 10) + new Vector2(5));
            Map.Spawn(spawn);

            base.Think(Time);
        }

        public override void OnMapCollision(Point Tile, Vector2 Point)
        {
            Map.Destroy(this);
        }

        public override void OnEntityCollision(Entity Collider, Vector2 Point)
        {
            ParticleSpawn spawn = new ParticleSpawn();
            spawn.type = explosion;
            spawn.position = Point;
            spawn.lifetime = System.TimeSpan.FromSeconds(0.5);
            spawn.angle = new Range<float>(0, MathHelper.TwoPi);
            spawn.count = 10;
            Map.Spawn(spawn);

            Map.Destroy(this);
        }
    }
}
