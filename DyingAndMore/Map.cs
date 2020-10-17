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

        /// <summary>
        /// Reset squad spawn timer/coounters and spawn (up to limits, including previously spawned members)
        /// </summary>
        /// <param name="squad">The squad to spawn</param>
        public void Spawn(Game.Entities.Squad squad)
        {
            if (Squads == null)
                Squads = new HashSet<Game.Entities.Squad>();
            
            //if (!Squads.ContainsKey(squad.Name))
            {
                if (string.IsNullOrEmpty(squad.Name))
                    squad.Name = Takai.Util.RandomString(prefix: "squad_");
                Squads.Add(squad);
                squad.SpawnUnits(this);
            }
        }

        //todo: squads should be clustered?

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
