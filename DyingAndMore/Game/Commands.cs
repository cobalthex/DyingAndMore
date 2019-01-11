using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game
{
    class GameCommand : Takai.Game.GameCommand
    {
        public override void Invoke(MapBaseInstance map)
        {
            if (GameInstance.Current != null &&
                GameInstance.Current.GameActions.TryGetValue(ActionName, out var action))
                action.Invoke(ActionParameter);
        }
    }

    class SpawnSquadCommand : Takai.Game.GameCommand
    {
        public string SquadName { get; set; } = null;

        public override void Invoke(MapBaseInstance map)
        {
            var minst = (MapInstance)map;
            var squad = minst.Squads[SquadName];
            if (squad != null)
                minst.Spawn(squad);
        }
    }
}
