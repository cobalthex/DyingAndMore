using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    class Projectile : Entity
    {
        ParticleType explosion;
        ParticleType trail, trailGlow;

        public override Vector2 Velocity
        {
            get { return base.Velocity; }
            set
            {
                Direction = Vector2.Normalize(value);
                base.Velocity = value;
            }
        }

        public override Vector2 Direction
        {
            get { return base.Direction; }

            set
            {
                base.Velocity = base.Velocity.Length() * value;
                base.Direction = value;
            }
        }

        public Projectile()
        {
            AlwaysActive = true;
            IgnoreTrace = true;
            
            var curve = new Curve();
            curve.Keys.Add(new CurveKey(0, 0));
            curve.Keys.Add(new CurveKey(0.25f, 0.5f));
            curve.Keys.Add(new CurveKey(1, 1));

            explosion = new ParticleType();
            var tex = Takai.AssetManager.Load<Texture2D>("Textures/Particles/Spark.png");
            explosion.Graphic = new Takai.Graphics.Graphic
            (
                tex//,
                //10,
                //10,
                //6,
                //System.TimeSpan.FromMilliseconds(200),
                //Takai.Graphics.TweenStyle.Overlap,
                //true
            );
            explosion.Graphic.CenterOrigin();
            explosion.BlendMode = BlendState.Additive;
            explosion.Color = new ValueCurve<Color>(curve, Color.White, Color.Transparent);
            explosion.Scale = new ValueCurve<float>(1);
            explosion.Speed = new ValueCurve<float>(curve, 100, 50);

            trail = new ParticleType();
            tex = Takai.AssetManager.Load<Texture2D>("Textures/Particles/trail.png");
            trail.Graphic = new Takai.Graphics.Graphic(tex);
            trail.Graphic.CenterOrigin();
            trail.Color = new ValueCurve<Color>(curve, Color.Orange, Color.Transparent);
            trail.Scale = new ValueCurve<float>(1);
            trail.Speed = new ValueCurve<float>(curve, 100, 0);

            trailGlow = new ParticleType();
            tex = Takai.AssetManager.Load<Texture2D>("Textures/Particles/trailglow.png");
            trailGlow.Graphic = new Takai.Graphics.Graphic(tex);
            trailGlow.Graphic.CenterOrigin();
            trailGlow.BlendMode = BlendState.Additive;
            trailGlow.Color = new ValueCurve<Color>(curve, Color.White, Color.Transparent);
            trailGlow.Scale = new ValueCurve<float>(1);
            trailGlow.Speed = new ValueCurve<float>(curve, 20, 5);

            tex = Takai.AssetManager.Load<Texture2D>("Textures/Projectiles/Sharp.png");
            Sprite = new Takai.Graphics.Graphic(tex);
            Sprite.CenterOrigin();
            Radius = Sprite.Width / 2;
        }

        System.TimeSpan flipTIme;
        public override void Think(GameTime Time)
        {
            ParticleSpawn spawn = new ParticleSpawn();
            spawn.type = trail;
            spawn.position = new Range<Vector2>(Position - (Direction * 10), Position - (Direction * 10));
            spawn.lifetime = System.TimeSpan.FromSeconds(0.25);

            var angle = (float)System.Math.Atan2(Direction.Y, Direction.X);
            spawn.angle = new Range<float>(angle - MathHelper.PiOver2, angle + MathHelper.PiOver2);
            spawn.count = 1;
            Map.Spawn(spawn);

            spawn.type = trailGlow;
            spawn.position = new Range<Vector2>(Position - (Direction * 10) - new Vector2(5), Position - (Direction * 10) + new Vector2(5));
            Map.Spawn(spawn);

            if (Time.TotalGameTime - flipTIme > System.TimeSpan.FromSeconds(1))
            {
                Direction *= -1;
                flipTIme = Time.TotalGameTime;
            }

            base.Think(Time);
        }

        public override void OnSpawn(GameTime Time)
        {
            flipTIme = Time.TotalGameTime;
        }

        public override void OnMapCollision(Point Tile, Vector2 Point, GameTime Time)
        {
            Map.Destroy(this);
        }

        public override void OnEntityCollision(Entity Collider, Vector2 Point, GameTime Time)
        {
            ParticleSpawn spawn = new ParticleSpawn();
            spawn.type = explosion;
            spawn.count = 20;
            spawn.lifetime = new Range<System.TimeSpan>(System.TimeSpan.FromSeconds(1), System.TimeSpan.FromSeconds(2));
            spawn.position = Point;

            var normal = Vector2.Normalize(Point - Collider.Position);
            //normal = Vector2.Reflect(Direction, normal);
            var angle = (float)System.Math.Atan2(normal.Y, normal.X);
            spawn.angle = new Range<float>(angle - 0.75f, angle + 0.75f);

            Map.Spawn(spawn);

            Map.Destroy(this);
        }
    }
}
