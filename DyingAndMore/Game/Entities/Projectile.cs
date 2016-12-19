using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    class Projectile : Entity
    {
        public ParticleType explosion;
        public ParticleType trail, trailGlow;

        public int damage = 20;

        /// <summary>
        /// How far this shot will go before destroying itself
        /// </summary>
        /// <remarks>Use zero for infinite</remarks>
        public float Range { get; set; } = 0;

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

            var sprite = new Takai.Graphics.Sprite(Takai.AssetManager.Load<Texture2D>("Textures/Projectiles/Sharp.png"))
            {
                Origin = new Vector2(12, 4)
            };
            State.AddState(EntStateKey.Idle, new EntState { Sprite = sprite, IsLooping = true }, true);
            Radius = sprite.Width / 2;
            
            var curve = new Curve();
            curve.Keys.Add(new CurveKey(0, 0));
            curve.Keys.Add(new CurveKey(0.25f, 0.5f));
            curve.Keys.Add(new CurveKey(1, 1));

            var tex = Takai.AssetManager.Load<Texture2D>("Textures/Particles/Spark.png");
            explosion = new ParticleType()
            {
                Graphic = new Takai.Graphics.Sprite
                (
                    tex//,
                       //10,
                       //10,
                       //6,
                       //System.TimeSpan.FromMilliseconds(200),
                       //Takai.Graphics.TweenStyle.Overlap,
                       //true
                ),
                BlendMode = BlendState.Additive,
                Color = new ValueCurve<Color>(curve, Color.White, Color.Transparent),
                Scale = new ValueCurve<float>(1),
                Speed = new ValueCurve<float>(curve, 100, 50)
            };
            explosion.Graphic.CenterOrigin();

            tex = Takai.AssetManager.Load<Texture2D>("Textures/Particles/trail.png");
            trail = new ParticleType()
            {
                Graphic = new Takai.Graphics.Sprite(tex),
                Color = new ValueCurve<Color>(curve, Color.Orange, Color.Transparent),
                Scale = new ValueCurve<float>(1),
                Speed = new ValueCurve<float>(curve, 100, 0)
            };
            trail.Graphic.CenterOrigin();

            tex = Takai.AssetManager.Load<Texture2D>("Textures/Particles/trailglow.png");
            trailGlow = new ParticleType()
            {
                Graphic = new Takai.Graphics.Sprite(tex),
                BlendMode = BlendState.Additive,
                Color = new ValueCurve<Color>(curve, Color.White, Color.Transparent),
                Scale = new ValueCurve<float>(1),
                Speed = new ValueCurve<float>(curve, 20, 5)
            };
            trailGlow.Graphic.CenterOrigin();
        }

        System.TimeSpan flipTime;
        public override void Think(System.TimeSpan DeltaTime)
        {
            ParticleSpawn spawn = new ParticleSpawn()
            {
                type = trail,
                position = new Range<Vector2>(Position - (Direction * Radius), Position - (Direction * Radius)),
                lifetime = System.TimeSpan.FromSeconds(0.25)
            };
            var angle = (float)System.Math.Atan2(Direction.Y, Direction.X);
            spawn.angle = new Range<float>(angle - MathHelper.PiOver2, angle + MathHelper.PiOver2);
            spawn.count = 1;
            Map.Spawn(spawn);

            spawn.type = trailGlow;
            spawn.position = new Range<Vector2>(Position - (Direction * Radius) - new Vector2(5), Position - (Direction * Radius) + new Vector2(5));
            Map.Spawn(spawn);

            //todo: destroy if 0 velocity

            base.Think(DeltaTime);
        }

        public override void OnSpawn()
        {
            flipTime = Map.ElapsedTime;
        }

        public override void OnMapCollision(Point Tile, Vector2 Point, System.TimeSpan DeltaTime)
        {
            Map.Destroy(this);
        }

        public override void OnEntityCollision(Entity Collider, Vector2 Point, System.TimeSpan DeltaTime)
        {
            ParticleSpawn spawn = new ParticleSpawn()
            {
                type = explosion,
                count = 20,
                lifetime = new Range<System.TimeSpan>(System.TimeSpan.FromSeconds(1), System.TimeSpan.FromSeconds(2)),
                position = Point
            };
            var normal = Vector2.Normalize(Point - Collider.Position);
            //normal = Vector2.Reflect(Direction, normal);
            var angle = (float)System.Math.Atan2(normal.Y, normal.X);
            spawn.angle = new Range<float>(angle - 0.75f, angle + 0.75f);

            Map.Spawn(spawn);
            Map.Destroy(this);

            var actor = Collider as Actor;
            if (actor != null)
            {
                actor.CurrentHealth -= damage;
            }
        }
    }
}
