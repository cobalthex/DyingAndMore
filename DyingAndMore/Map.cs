using Takai.Game;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System;

namespace DyingAndMore
{
    public class MapClass : MapBaseClass
    {
        public override MapBaseInstance Instantiate()
        {
            return new MapInstance(this);
        }
    }

    public class MapInstance : MapBaseInstance
    {
        public HashSet<Game.Entities.Squad> Squads
        {
            get => _squads;
            set
            {
                if (_squads == value)
                    return;

                _squads = value;
                Takai.Data.Binding.Globals["Map.Squads"] = _squads;
            }
        }
        HashSet<Game.Entities.Squad> _squads; //List?

        public MapInstance() : this(null) { }
        public MapInstance(MapClass @class)
            : base(@class)
        {
            if (@class == null)
                return;
        }

        protected override void UpdateEntities(TimeSpan deltaTime)
        {
            if (updateSettings.isEntityLogicEnabled)
            {
                //todo: only squads in sector
                if (Squads != null)
                {
                    foreach (var squad in Squads)
                        squad.Update(this, deltaTime);
                }
            }

            base.UpdateEntities(deltaTime);
        }
    }
}
