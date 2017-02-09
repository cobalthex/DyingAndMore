using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.Graphics;

namespace DyingAndMore.Editor
{
    partial class Editor : Takai.GameState.GameState
    {
        //todo: on mode load, set all existing groups to purple colored

        Takai.Game.Group activeGroup;

        void UpdateGroupsMode(GameTime Time)
        {
            foreach (var ent in map.ActiveEnts)
            {
                if (activeGroup != null && activeGroup.Entities.Contains(ent))
                    ent.OutlineColor = Color.GreenYellow; //todo: unify
                else
                    ent.OutlineColor = Color.Purple; //todo: unify
            }
        }

        void DrawGroupsMode()
        {
        }
    }
}