using Microsoft.Xna.Framework;

namespace DyingAndMore.Weapons
{
    class BlobGun : Weapon
    {
        /// <summary>
        /// The type of blob to fire
        /// </summary>
        public Takai.Game.BlobType blob;
        /// <summary>
        /// The intial speed of the blob
        /// </summary>
        public float speed = 0;

        protected override void SingleFire(Takai.Game.Entity Entity)
        {
            var pos = Entity.Position + (Entity.Direction * (Entity.Radius + blob.Radius + 1));
            Entity.Map.SpawnBlob(blob, pos, Entity.Direction * speed);
        }
    }
}
