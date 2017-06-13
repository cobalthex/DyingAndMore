using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor
{
    class GroupsEditorMode : EditorMode
    {
        //todo: on mode load, set all existing groups to purple colored

        Takai.Game.Group selectedGroup = null;

        Static selector;

        public GroupsEditorMode(Editor editor)
            : base("Groups", editor)
        {
            VerticalAlignment = Alignment.Stretch;
            HorizontalAlignment = Alignment.Stretch;

            selector = new Selectors.EntSelector(editor)
            {
                HorizontalAlignment = Alignment.Middle,
                VerticalAlignment = Alignment.Middle,
                Size = new Vector2(400, 300)
            };

        }

        public override void Start()
        {
            selectedGroup = new Takai.Game.Group()
                ;
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
                    ent.OutlineColor = Color.GreenYellow; //todo: standardize color
                else
                    ent.OutlineColor = Color.MediumPurple; //todo: standardize color
            }

            return base.HandleInput(time);
        }

        protected override void OnPress(ClickEventArgs e)
        {
            var mousePos = editor.Map.ActiveCamera.ScreenToWorld(e.position);
            var ents = editor.Map.FindEntities(mousePos, 10);

            foreach (var ent in ents)
                ent.Group = selectedGroup;

            base.OnPress(e);
        }
    }
}