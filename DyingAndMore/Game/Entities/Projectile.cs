using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    class Projectile : Entity
    {
        ParticleType pType;

        public Projectile()
        {
            AlwaysActive = true;
            IgnoreTrace = true;

            pType = new ParticleType();
            var tex = Takai.AssetManager.Load<Texture2D>("Textures/Particles/Blood.png");
            pType.Graphic = new Takai.Graphics.Graphic
            (
                tex,
                10,
                10,
                6,
                System.TimeSpan.FromMilliseconds(200),
                Takai.Graphics.TweenStyle.Overlap,
                true
            );
            pType.Graphic.CenterOrigin();

            var curve = new Curve();
            curve.Keys.Add(new CurveKey(0, 0));
            curve.Keys.Add(new CurveKey(0.25f, 0.5f));
            curve.Keys.Add(new CurveKey(1, 1));

            pType.Color = new ValueCurve<Color>(curve, Color.White, Color.Transparent);
            pType.Scale = new ValueCurve<float>(1);
            pType.Speed = new ValueCurve<float>(100);

            tex = Takai.AssetManager.Load<Texture2D>("Textures/Projectiles/Sharp.png");
            Sprite = new Takai.Graphics.Graphic(tex);
            Sprite.CenterOrigin();
            Radius = Sprite.Width / 2;
        }

        public override void OnMapCollision(Point Tile, Vector2 Point)
        {
            Map.Destroy(this);
        }

        public override void OnEntityCollision(Entity Collider, Vector2 Point)
        {
            ParticleSpawn spawn = new ParticleSpawn();
            spawn.type = pType;
            spawn.position = new Range<Vector2>(Point);
            spawn.lifetime = new Range<System.TimeSpan>(System.TimeSpan.FromSeconds(0.5));
            spawn.angle = new Range<float>(0, MathHelper.TwoPi);
            spawn.count = new Range<int>(20);
            Map.Spawn(spawn);

            Map.Destroy(this);
        }
    }
}
