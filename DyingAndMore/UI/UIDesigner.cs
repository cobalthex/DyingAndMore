using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai;
using Takai.UI;
using Takai.Graphics;

using System.Reflection;

namespace DyingAndMore.UI
{
    public class UIDesigner : Static
    {
        public Vector2 GridSize { get; set; } = new Vector2(20);
        public Color GridColor { get; set; } = new Color(255, 255, 255, 127);

        Static propsEditor;
        Static newItemPopup;

        Static parent;
        Static newItem;

        Static hoverElement;

        bool sizing;
        Rectangle sizingRect;
        const int sizingEpsilon = 5;

        bool showTargeter;
        Vector2 targetPoint;

        public UIDesigner()
        {
            var newItemMenu = new ItemList<System.Type>()
            {
                ItemUI = new Static
                {
                    Padding = new Vector2(5),
                    Bindings = new List<Takai.Data.Binding> {
                        new Takai.Data.Binding("Name", "Text")
                    },
                },
                AllowSelection = true
            };
            newItemMenu.On(SelectionChangedEvent, delegate (Static sender, UIEventArgs e)
            {
                var sea = (SelectionChangedEventArgs)e;
                if (sea.newIndex < 0)
                    return UIEventResult.Continue;

                var list = (ItemList<System.Type>)sender;
                var selectedItem = list.Items[sea.newIndex];
                sender.BubbleCommand("CreateElement", selectedItem);
                list.SelectedItem = null;
                return UIEventResult.Handled;
            });

            CommandActions["CreateElement"] = delegate (Static sender, object arg)
            {
                var self = (UIDesigner)sender;
                self.sizing = false;

                var newElement = (Static)System.Activator.CreateInstance((System.Type)arg); //list of Statics?
                if (sizingRect.Width > sizingEpsilon || sizingRect.Height > sizingEpsilon)
                    newElement.Size = new Vector2(self.sizingRect.Width, self.sizingRect.Height);

                //transform coords
                newElement.Position = new Vector2(self.sizingRect.X, self.sizingRect.Y);

                //test props
                newElement.BorderColor = Color.White;

                self.parent.AddChild(newElement);
                self.RemoveChild(newItemPopup);
            };

            var typeInfo = typeof(Static).GetTypeInfo();
            foreach (var rtype in Takai.Data.Serializer.RegisteredTypes)
            {
                var rti = rtype.Value.GetTypeInfo();
                if (!rti.IsAbstract && typeInfo.IsAssignableFrom(rtype.Value))
                    newItemMenu.Items.Add(rtype.Value);
            }

            newItemPopup = new ScrollBox(newItemMenu)
            {
                Style = "Frame",
                Size = new Vector2(160, 300),
                IsModal = true
            };

            On(PressEvent, OnPress);
            On(DragEvent, OnDrag);
            On(ClickEvent, OnClick);
        }

        //todo: route event objects correctly
        static UIEventResult OnPress(Static sender, UIEventArgs args)
        {
            var self = (UIDesigner)sender;
            var pea = (PointerEventArgs)args;

            if (pea.device == Takai.Input.DeviceType.Mouse && pea.button == 0)
            {
                var pos = self.showTargeter ? self.targetPoint : pea.position;
                self.parent = args.Source;
                self.sizing = true;
                self.sizingRect = new Rectangle((int)pos.X, (int)pos.Y, 0, 0);
                return UIEventResult.Handled;
            }
            return UIEventResult.Continue;
        }
        static UIEventResult OnDrag(Static sender, UIEventArgs args)
        {
            var self = (UIDesigner)sender;
            var dea = (DragEventArgs)args;
            if (self.sizing && dea.device == Takai.Input.DeviceType.Mouse && dea.button == 0)
            {
                self.sizingRect = Util.AbsRectangle(self.sizingRect.Location, (self.showTargeter ? self.targetPoint : dea.position).ToPoint());
                return UIEventResult.Handled;
            }

            if (self != args.Source) //don't hover on the designer
                self.hoverElement = args.Source;
            else
                self.hoverElement = null;

            return UIEventResult.Continue;
        }
        static UIEventResult OnClick(Static sender, UIEventArgs args)
        {
            var self = (UIDesigner)sender;
            var pea = (PointerEventArgs)args;
            if (self.sizing && pea.device == Takai.Input.DeviceType.Mouse && pea.button == 0)
            {
                self.newItemPopup.Position = Vector2.Clamp(
                    pea.position,
                    Vector2.Zero,
                    new Vector2(self.VisibleContentArea.Right - self.newItemPopup.VisibleBounds.Width,
                        self.VisibleContentArea.Bottom - self.newItemPopup.VisibleBounds.Height)
                );
                self.AddChild(self.newItemPopup);
                return UIEventResult.Handled;
            }
            return UIEventResult.Continue;
        }

        protected override void OnChildRemeasure(Static child)
        {
            InvalidateMeasure();
            InvalidateArrange();
        }

        protected override bool HandleInput(GameTime time)
        {
            if (showTargeter = Takai.Input.InputState.IsMod(Takai.Input.KeyMod.Control))
            {
                targetPoint = Takai.Input.InputState.MouseVector - OffsetContentArea.Location.ToVector2();
                targetPoint.X = (float)System.Math.Round(targetPoint.X / GridSize.X) * GridSize.X;
                targetPoint.Y = (float)System.Math.Round(targetPoint.Y / GridSize.Y) * GridSize.Y;
            }

            return base.HandleInput(time);
        }

        protected override void DrawSelf(DrawContext context)
        {
            var offset = GridSize - new Vector2(VisibleOffset.X % GridSize.X, VisibleOffset.Y % GridSize.Y);
            for (int y = 0; y < VisibleBounds.Height / GridSize.Y; ++y)
            {
                for (int x = 0; x < VisibleBounds.Width / GridSize.X; ++x)
                {
                    //use shader?
                    Primitives2D.DrawDot(context.spriteBatch, GridColor,
                        new Vector2(VisibleBounds.Left + x * GridSize.X, VisibleBounds.Top + y * GridSize.Y) + offset);
                }
            }

            if (hoverElement != null)
                DrawRect(context.spriteBatch, Color.Cyan, hoverElement.OffsetContentArea);

            if (sizing)
            {
                if (sizingRect.Width < sizingEpsilon || sizingRect.Height < sizingEpsilon)
                    Primitives2D.DrawX(context.spriteBatch, Color.Gold, new Rectangle(sizingRect.X - 4, sizingRect.Y - 4, 8, 8));
                DrawRect(context.spriteBatch, Color.Gold, sizingRect);
            }

            const int targeterRadius = 10;
            if (showTargeter)
            {
                DrawVLine(context.spriteBatch, Color.LawnGreen, targetPoint.X, targetPoint.Y - targeterRadius, targetPoint.Y + targeterRadius);
                DrawHLine(context.spriteBatch, Color.Tomato, targetPoint.Y, targetPoint.X - targeterRadius, targetPoint.X + targeterRadius);
            }
        }
    }
}