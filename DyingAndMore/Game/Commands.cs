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

    class AllSquadUnitsEntityCommand : ICommand
    {
        public string ActionName { get; set; }
        public object ActionParameter { get; set; }

        [Takai.Data.Serializer.AsReference]
        public Entities.Squad Squad { get; set; }

        protected EntityCommand entCommand = new EntityCommand();

        public void Invoke(MapBaseInstance map)
        {
            if (Squad == null)
                return;

            entCommand.ActionName = ((ICommand)this).ActionName;
            entCommand.ActionParameter = ((ICommand)this).ActionParameter;

            foreach (var unit in Squad.Units)
                entCommand.Invoke(unit);
        }

        public override string ToString()
        {
            return nameof(AllSquadUnitsEntityCommand) + $" - Squad: {Squad}";
        }
    }

    class DestroySquadUnitsCommand : ICommand
    {
        string ICommand.ActionName { get; set; }
        object ICommand.ActionParameter { get; set; }

        [Takai.Data.Serializer.AsReference]
        public Entities.Squad Squad { get; set; }

        public void Invoke(MapBaseInstance map)
        {
            Squad?.DestroyAllUnits();
        }

        public override string ToString()
        {
            return nameof(DestroySquadUnitsCommand) + $" - Squad: {Squad}";
        }
    }

    class PauseGameCommand : ICommand
    {
        string ICommand.ActionName { get; set; }
        object ICommand.ActionParameter { get; set; }

        public void Invoke(MapBaseInstance map)
        {
            // TODO: redesign commands to be registered?
        }
    }
}
