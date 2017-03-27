using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Weapons
{
    class FluidGun : Weapon
    {
        /// <summary>
        /// The type of Fluid to fire
        /// </summary>
        public Takai.Game.FluidType Fluid = null;
        /// <summary>
        /// The intial speed of the Fluid
        /// </summary>
        public float speed = 0;

        protected override void SingleFire(Takai.Game.Entity Entity)
        {
            var pos = Entity.Position + (Entity.Direction * (Entity.Radius + Fluid.Radius + 1));
            Entity.Map.Spawn(Fluid, pos, Entity.Direction * speed);
        }
    }
}
