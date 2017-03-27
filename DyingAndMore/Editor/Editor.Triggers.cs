using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class TriggersConfigurator : Takai.Runtime.GameState
    {
        SpriteBatch sbatch;

        Takai.UI.Element uiContainer;

        public TriggersConfigurator()
            : base(true, false) { }

        public override void Load()
        {
        }

        public override void Update(GameTime time)
        {
        }

        public override void Draw(GameTime time)
        {
        }
    }

    class TriggersEditorMode : EditorMode
    {
        bool isPosSaved = false;
        Vector2 savedWorldPos, lastWorldPos;

        Takai.Game.Trigger activeTrigger = null;

        TriggersConfigurator configurator;

        public TriggersEditorMode(Editor editor)
            : base("Triggers", editor)
        {
        }

        public override void OpenConfigurator(bool DidClickOpen)
        {
        }

        public override void Start()
        {
            editor.Map.renderSettings.drawTriggers = true;
        }

        public override void End()
        {
            editor.Map.renderSettings.drawTriggers = false;
        }

        public override void Update(GameTime time)
        {
            var currentWorldPos = editor.Map.ActiveCamera.ScreenToWorld(InputState.MouseVector);

            if (InputState.IsPress(MouseButtons.Left))
            {
                //todo: check for existing triggers

                activeTrigger = new Takai.Game.Trigger(new Rectangle(InputState.MousePoint, new Point(1)));
                editor.Map.AddTrigger(activeTrigger);
                savedWorldPos = currentWorldPos;
            }

            else if (InputState.IsButtonHeld(MouseButtons.Left))
            {
                var start = savedWorldPos.ToPoint();
                var end = currentWorldPos.ToPoint();

                if (start.X > end.X)
                {
                    var tmp = start.X;
                    start.X = end.X;
                    end.X = tmp;
                }
                if (start.Y > end.Y)
                {
                    var tmp = start.Y;
                    start.Y = end.Y;
                    end.Y = tmp;
                }

                var diff = end - start;
                activeTrigger.Region = new Rectangle(start, diff);
            }

            lastWorldPos = currentWorldPos;
        }

        public override void Draw(SpriteBatch sbatch)
        {
        }
    }
}