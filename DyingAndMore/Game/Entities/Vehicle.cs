﻿using System;
using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    public class VehicleClass : EntityClass
    {

    }

    public class VehicleInstance : EntityInstance
    {
        [Takai.Data.Serializer.Ignored]
        public VehicleClass _Class
        {
            get => (VehicleClass)base.Class;
            set => base.Class = value;
        }
    }
}
