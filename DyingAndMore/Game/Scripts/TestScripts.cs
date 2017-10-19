using System;
using System.Linq;
using Takai.Game;

namespace DyingAndMore.Game.Scripts
{
    class NavigateToPlayer : EntityScript
    {
        EntityInstance player;

        public NavigateToPlayer(EntityInstance entity)
            : base("NavigateToPlayer", entity) { }

        public override void OnSpawn()
        {
            player = GameInstance.Current.players.FirstOrDefault();
        }

        public override void Step(TimeSpan deltaTime)
        {

        }
    }
}
