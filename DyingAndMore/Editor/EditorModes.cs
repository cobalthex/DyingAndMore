using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Takai.Data;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor
{

    public abstract class EditorMode : Static
    {
        protected readonly Editor editor;

        public override bool CanFocus => true;

        public EditorMode(string name, Editor editor)
        {
            Name = name;
            this.editor = editor;

            ignoreEnterKey = true;
            ignoreSpaceKey = true;

            VerticalAlignment = Alignment.Stretch;
            HorizontalAlignment = Alignment.Stretch;
        }

        public virtual void Start() { }
        public virtual void End() { }

        public virtual void OnMapChanged() { }
    }

    public abstract class SelectorEditorMode<TSelector> : EditorMode
        where TSelector : UI.Selector, new()
    {
        public TSelector selector;
        protected List toolbox;
        public Graphic preview;

        private Drawer selectorDrawer;

        public SelectorEditorMode(string name, Editor editor, TSelector selector = null)
            : base(name, editor)
        {
            preview = new Graphic()
            {
                Sprite = new Takai.Graphics.Sprite()
                {
                    Texture = editor.Map.Class.Tileset.texture,
                    Width = editor.Map.Class.TileSize,
                    Height = editor.Map.Class.TileSize,
                },
                HorizontalAlignment = Alignment.Right,
                Styles = "Editor.Selector.Preview",
            };
            preview.EventCommands[ClickEvent] = "OpenSelector";
            CommandActions["OpenSelector"] = delegate (Static sender, object arg)
            {
                ((SelectorEditorMode<TSelector>)sender).selectorDrawer.IsEnabled = true;
            };

            this.selector = selector ?? new TSelector();
            this.selector.HorizontalAlignment = Alignment.Stretch;

            //todo: store in list with preview
            var eraserButton = new Graphic(Cache.Load<Texture2D>("UI/Editor/Eraser.png"))
            {
                HorizontalAlignment = Alignment.Right,
                Styles = "Editor.Selector.Eraser"
            };
            eraserButton.On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                this.selector.SelectedIndex = -1;
                return UIEventResult.Handled;
            });

            toolbox = new List
            {
                Position = new Vector2(20),
                Margin = 20,
                HorizontalAlignment = Alignment.Right
            };
            AddChild(toolbox);
            toolbox.AddChildren(preview, eraserButton);

            On(SelectionChangedEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (SelectorEditorMode<TSelector>)sender;
                self.selectorDrawer.IsEnabled = false;
                if (self.selector.SelectedIndex < 0)
                    self.preview.Sprite = null;
                else
                    self.UpdatePreview(self.selector.SelectedIndex);
                return UIEventResult.Handled;
            });

            selectorDrawer = new Drawer
            {
                Size = new Vector2(6, float.NaN) * (this.selector.ItemSize + this.selector.ItemMargin)
                     + this.selector.ItemMargin + new Vector2(8), //measure selector given this width and see if requires scrolling?
                HorizontalAlignment = Alignment.Right,
                VerticalAlignment = Alignment.Stretch,
                IsEnabled = false
            };
            selectorDrawer.AddChild(new ScrollBox(this.selector) //do first and measure?
            {
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch
            });
            AddChild(selectorDrawer);

            this.selector.SelectedIndex = 0; //initialize preview
        }

        protected override void FinalizeClone()
        {
            toolbox = (List)Children[0];
            preview = (Graphic)toolbox.Children[0];
            selectorDrawer = (Drawer)Children[1];
            base.FinalizeClone();
        }

        protected abstract void UpdatePreview(int selectedItem);

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(Keys.Tab))
            {
                selectorDrawer.IsEnabled = true;
                return false;
            }

            return base.HandleInput(time);
        }
    }
}
