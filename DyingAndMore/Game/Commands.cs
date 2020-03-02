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
            foreach (var squad in minst.Squads)
            {
                if (squad.Name == SquadName)
                {
                    minst.Spawn(squad);
                    break;
                }
            }
        }

        public override string ToString()
        {
            return nameof(SpawnSquadCommand) + $" - Squad: {SquadName}";
        }
    }

    class DestroySquadUnitsCommand : ICommand
    {
        string ICommand.ActionName { get; set; }
        object ICommand.ActionParameter { get; set; }

        public string SquadName { get; set; } = null;

        public void Invoke(MapBaseInstance map)
        {
            var minst = (MapInstance)map;
            if (minst == null)
                return;
            foreach (var squad in minst.Squads)
            {
                if (squad.Name == SquadName)
                {
                    squad.DestroyAllUnits();
                    break;
                }
            }
        }

        public override string ToString()
        {
            return nameof(DestroySquadUnitsCommand) + $" - Squad: {SquadName}";
        }
    }
}
