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

    class SpawnEncounterCommand : Takai.Game.GameCommand
    {
        public string EncounterName;

        public override void Invoke(MapBaseInstance map)
        {
            var mclass = (MapClass)map.Class;
            if (mclass.Encounters.TryGetValue(EncounterName, out var encounter))
                ((MapInstance)map).Spawn(encounter);
        }
    }
}
