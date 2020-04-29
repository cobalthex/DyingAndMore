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

    public enum ActorBroadcastType
    {
        Unknown,
        Spawn,
        Death,
        AIBehaviorChange,
    }

    public struct ActorBroadcast
    {
        public Game.Entities.ActorInstance actor;
        public ActorBroadcastType type;
        public object message;

        public ActorBroadcast(Game.Entities.ActorInstance actor, ActorBroadcastType type, object message = null)
        {
            this.actor = actor;
            this.type = type;
            this.message = message;
        }
    }

    public class MapInstance : MapBaseInstance
    {
        [Takai.Data.Serializer.AsReference]
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

        /// <summary>
        /// Current broadcasts. Reset every frame.
        /// 
        /// </summary>
        public System.Collections.ObjectModel.ReadOnlyCollection<ActorBroadcast> Broadcasts { get; private set; }
        private List<ActorBroadcast> broadcasts = new List<ActorBroadcast>(16);
        private List<ActorBroadcast> nextBroadcasts = new List<ActorBroadcast>(16);

        public void Broadcast(ActorBroadcast broadcast)
        {
            nextBroadcasts.Add(broadcast);
        }

        public MapInstance() : this(null) { }
        public MapInstance(MapClass @class)
            : base(@class)
        {
            if (@class == null)
                return;

            Broadcasts = broadcasts.AsReadOnly();
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
            {
                var tmp = broadcasts;
                broadcasts = nextBroadcasts;
                nextBroadcasts = tmp;
                nextBroadcasts.Clear();
                Broadcasts = broadcasts.AsReadOnly();
            }

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
