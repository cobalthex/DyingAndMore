using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai;
using Takai.UI;

using System.Reflection;

namespace DyingAndMore.UI
{
    public class UIDesigner : Static
    {
        public Vector2 GridSize { get; set; } = new Vector2(20);
        public Color GridColor { get; set; } = new Color(255, 255, 255, 127);

        Static propsEditor;
        Static newItemPopup;

        Static newItem;

        bool sizing;
        Rectangle sizingRect;

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
                    }
                }
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
                //IsModal = true
            };

            On(PressEvent, OnPress);
            On(DragEvent, OnDrag);
            On(ClickEvent, OnClick);
        }

        UIEventResult OnPress(Static sender, UIEventArgs args)
        {
            var pea = (PointerEventArgs)args;
            if (pea.device == Takai.Input.DeviceType.Mouse && pea.deviceIndex == 0)
            {
                var pos = showTargeter ? targetPoint : pea.position;
                sizing = true;
                sizingRect = new Rectangle((int)pos.X, (int)pos.Y, 0, 0);
                return UIEventResult.Handled;
            }
            return UIEventResult.Continue;
        }
        UIEventResult OnDrag(Static sender, UIEventArgs args)
        {
            var dea = (DragEventArgs)args;
            if (sizing && dea.device == Takai.Input.DeviceType.Mouse && dea.deviceIndex == 0)
            {
                sizingRect = Util.AbsRectangle(sizingRect.Location, (showTargeter ? targetPoint : dea.position).ToPoint());
                return UIEventResult.Handled;
            }
            return UIEventResult.Continue;
        }
        UIEventResult OnClick(Static sender, UIEventArgs args)
        {
            var pea = (PointerEventArgs)args;
            if (sizing && pea.device == Takai.Input.DeviceType.Mouse && pea.deviceIndex == 0)
            {
                sizing = false;
                newItemPopup.Position = pea.position;
                AddChild(newItemPopup);
                return UIEventResult.Handled;
            }
            return UIEventResult.Continue;
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

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var offset = GridSize - new Vector2(VisibleOffset.X % GridSize.X, VisibleOffset.Y % GridSize.Y);
            for (int y = 0; y < VisibleBounds.Height / GridSize.Y; ++y)
            {
                for (int x = 0; x < VisibleBounds.Width / GridSize.X; ++x)
                {
                    //use shader?
                    Takai.Graphics.Primitives2D.DrawDot(spriteBatch, GridColor,
                        new Vector2(VisibleBounds.Left + x * GridSize.X, VisibleBounds.Top + y * GridSize.Y) + offset);
                }
            }

            if (sizing)
                DrawRect(spriteBatch, Color.Gold, sizingRect);

            const int targeterRadius = 10;
            if (showTargeter)
            {
                DrawVLine(spriteBatch, Color.LawnGreen, targetPoint.X, targetPoint.Y - targeterRadius, targetPoint.Y + targeterRadius);
                DrawHLine(spriteBatch, Color.Tomato, targetPoint.Y, targetPoint.X - targeterRadius, targetPoint.X + targeterRadius);
            }
        }
    }
}