using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game
{
    /// <summary>
    /// commands that affect the game (like complete level/etc)
    /// </summary>
    class GameCommand : ICommand
    {
        public string ActionName { get; set; }
        public object ActionParameter { get; set; }

        public void Invoke(MapBaseInstance map)
        {
            if (GameInstance.Current != null &&
                GameInstance.Current.GameActions.TryGetValue(ActionName, out var action))
                action.Invoke(ActionParameter);
        }
    }

    class SpawnSquadCommand : ICommand
    {
        string ICommand.ActionName { get; set; }
        object ICommand.ActionParameter { get; set; }

        public string SquadName { get; set; } = null;

        public void Invoke(MapBaseInstance map)
        {
            var minst = (MapInstance)map;
            if (minst == null)
                return;
            var squad = minst.Squads[SquadName];
            if (squad != null)
                minst.Spawn(squad);
        }

        public override string ToString()
        {
            return nameof(SpawnSquadCommand) + $" - Squad: {SquadName}";
        }
    }
}
