using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DyingAndMore.Editor
{
    class SquadsEditorMode : EditorMode
    {
        public Game.Squad SelectedSquad { get; set; }

        public Takai.Graphics.Sprite SquadIcon { get; set; }

        public SquadsEditorMode(Editor editor)
            : base("Squads", editor)
        {
            SquadIcon = new Takai.Graphics.Sprite(Takai.Data.Cache.Load<Texture2D>("UI/Editor/squad.png"));
            SquadIcon.CenterOrigin();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            var clampScale = Takai.Util.Clamp(editor.Camera.ActualScale, 0.4f, 1);

            foreach (var squad in editor.Map.Squads)
            {
                editor.Map.DrawCircle(squad.Value.SpawnPosition, squad.Value.SpawnRadius, Color.Cyan);
                SquadIcon.Draw(
                    spriteBatch, 
                    editor.Camera.WorldToScreen(squad.Value.SpawnPosition),
                    0,
                    Color.White, 
                    clampScale
                );

                var squadNameSize = DefaultFont.MeasureString(squad.Key);
                DefaultFont.Draw(
                    spriteBatch,
                    squad.Key,
                    editor.Camera.WorldToScreen(squad.Value.SpawnPosition + new Vector2(0, squad.Value.SpawnRadius))
                        + new Vector2(squadNameSize.X / -2, 10 * clampScale),
                    Color.White
                );
            }
        }
    }
}
