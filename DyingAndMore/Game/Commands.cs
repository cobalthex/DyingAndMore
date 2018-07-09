using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game
{
    class GameCommand : Command
    {
        public override void Invoke()
        {
            if (GameInstance.Current != null &&
                GameInstance.Current.GameActions.TryGetValue(ActionName, out var action))
                action.Invoke(ActionParameter);
        }
    }
}
