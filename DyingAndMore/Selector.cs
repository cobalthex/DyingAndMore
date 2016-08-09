using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace DyingAndMore
{
    abstract class Selector : Takai.States.State
    {
        public Editor editor;

        protected int width = 320;
        protected const int splitterWidth = 8;
        protected Vector2 Start { get { return new Vector2(GraphicsDevice.Viewport.Width - width, 0); } }

        protected SpriteBatch sbatch;

        protected bool isResizing = false;
        int resizeXOffset = 0;
        bool isHoveringSplitter = false;

        public Selector(Editor Editor)
            : base(Takai.States.StateType.Popup)
        {
            editor = Editor;
        }

        public override void Load()
        {
            sbatch = new SpriteBatch(GraphicsDevice);
        }

        public override void Update(GameTime time)
        {
            if (InputCatalog.IsKeyClick(Keys.Tab))
                Deactivate();

            var mouse = InputCatalog.MouseState.Position;

            var splitterX = GraphicsDevice.Viewport.Width - width - splitterWidth;
            isHoveringSplitter = (mouse.X >= splitterX && mouse.X <= splitterX + splitterWidth);

            if (InputCatalog.IsMousePress(InputCatalog.MouseButton.Left))
            {
                if (isHoveringSplitter)
                {
                    isResizing = true;
                    resizeXOffset = mouse.X - splitterX;
                }
            }
            else if (InputCatalog.IsMouseClick(InputCatalog.MouseButton.Left))
                isResizing = false;

            else if (isResizing)
                width = GraphicsDevice.Viewport.Width - splitterWidth - mouse.X + resizeXOffset;

            width = MathHelper.Clamp(width, 0, GraphicsDevice.Viewport.Width - splitterWidth);
        }
        
        public override void Draw(GameTime Time)
        {
            sbatch.Begin();

            Vector2 start = Start;

            Takai.Graphics.Primitives2D.DrawFill(sbatch, new Color(1, 1, 1, 0.75f), new Rectangle((int)start.X, (int)start.Y, width, GraphicsDevice.Viewport.Height));
            Takai.Graphics.Primitives2D.DrawFill
            (
                sbatch, 
                isHoveringSplitter ? (InputCatalog.MouseState.LeftButton == ButtonState.Pressed ? Color.DarkGray : Color.LightGray) : Color.Gray, 
                new Rectangle((int)start.X - splitterWidth, (int)start.Y, splitterWidth, GraphicsDevice.Viewport.Height)
            );

            DrawContents(Time, start);

            sbatch.End();
        }

        public abstract void DrawContents(GameTime Time, Vector2 Position);
    }
}
