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
            return new MapInstance();
        }
    }

    public class MapInstance : MapBaseInstance
    {
        [Takai.Data.Serializer.AsReference]
        public Dictionary<string, Game.Squad> Squads { get; set; }

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
        public void Spawn(Game.Squad squad)
        {
            if (Squads == null)
                Squads = new Dictionary<string, Game.Squad>();
            Squads[squad.Name] = squad;

            squad.OnSpawn(this);
        }

        //todo: squads should be clustered?
        
        protected override void UpdateEntities(TimeSpan deltaTime)
        {
            if (updateSettings.isEntityLogicEnabled)
            {
                if (Squads != null)
                {
                    foreach (var squad in Squads)
                        squad.Value.Update(this, deltaTime);
                }
            }

            base.UpdateEntities(deltaTime);
        }
    }
}
