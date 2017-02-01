﻿using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Weapons
{
    class BlobGun : Weapon
    {
        /// <summary>
        /// The type of blob to fire
        /// </summary>
        public Takai.Game.BlobType blob = null;
        /// <summary>
        /// The intial speed of the blob
        /// </summary>
        public float speed = 0;

        protected override void SingleFire(Takai.Game.Entity Entity)
        {
            var pos = Entity.Position + (Entity.Direction * (Entity.Radius + blob.Radius + 1));
            Entity.Map.Spawn(blob, pos, Entity.Direction * speed);
        }
    }
}
