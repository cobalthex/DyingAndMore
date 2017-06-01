﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class GroupsEditorMode : EditorMode
    {
        //todo: on mode load, set all existing groups to purple colored

        Takai.Game.Group selectedGroup = null;

        Takai.UI.Static selector;

        public GroupsEditorMode(Editor editor)
            : base("Groups", editor)
        {
            VerticalAlignment = Takai.UI.Alignment.Stretch;
            HorizontalAlignment = Takai.UI.Alignment.Stretch;

            selector = new Takai.UI.List()
            {
                HorizontalAlignment = Takai.UI.Alignment.Middle,
                VerticalAlignment = Takai.UI.Alignment.Middle
            };

        }

        public override void Start()
        {
        }

        public override void End()
        {
            //clear all group colors
            selectedGroup = null;

            foreach (var ent in editor.Map.AllEntities)
                ent.OutlineColor = Color.Transparent;
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Tab))
            {
                AddChild(selector);
                return false;
            }

            foreach (var ent in editor.Map.ActiveEnts)
            {
                if (selectedGroup != null && selectedGroup.Entities.Contains(ent))
                    ent.OutlineColor = Color.GreenYellow; //todo: unify color
                else
                    ent.OutlineColor = Color.MediumPurple; //todo: unify color
            }

            return base.HandleInput(time);
        }
    }
}