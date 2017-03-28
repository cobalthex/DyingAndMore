using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor
{
    class GroupsConfigurator : Takai.Runtime.GameState
    {
        SpriteBatch sbatch;

        Takai.UI.Element uiContainer;

        public GroupsConfigurator()
            : base(true, false) { }

        public override void Load()
        {
            sbatch = new SpriteBatch(GraphicsDevice);
            uiContainer = new Takai.UI.Element();
            uiContainer.AddChild(new Takai.UI.TextBox()
            {
                Text = "Test",
                Position = new Vector2(10, 10)
            });
        }

        public override void Update(GameTime time)
        {
            uiContainer.Update(time);
        }

        public override void Draw(GameTime time)
        {
            sbatch.Begin();
            uiContainer.Draw(sbatch);
            sbatch.End();
        }
    }

    class GroupsEditorMode : EditorMode
    {
        //todo: on mode load, set all existing groups to purple colored

        Takai.Game.Group selectedGroup = null;

        GroupsConfigurator configurator;

        public GroupsEditorMode(Editor editor)
            : base("Groups", editor)
        {
        }

        public override void OpenConfigurator(bool DidClickOpen)
        {
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

        public override void Update(GameTime time)
        {
            foreach (var ent in editor.Map.ActiveEnts)
            {
                if (selectedGroup != null && selectedGroup.Entities.Contains(ent))
                    ent.OutlineColor = Color.GreenYellow; //todo: unify color
                else
                    ent.OutlineColor = Color.Purple; //todo: unify color
            }


        }

        public override void Draw(SpriteBatch sbatch)
        {
        }
    }
}