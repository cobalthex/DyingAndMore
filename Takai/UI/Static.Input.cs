using Takai.Input;
using System.Collections.Specialized;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Takai.UI
{
    public partial class Static
    {
        /// <summary>
        /// Disable the default behavior of the tab key
        /// </summary>
        protected bool ignoreTabKey = false;
        /// <summary>
        /// Disable the default behavior of the space key
        /// </summary>
        protected bool ignoreSpaceKey = false;
        /// <summary>
        /// Disable the default behavior of the enter key
        /// </summary>
        protected bool ignoreEnterKey = false;

        /// <summary>
        /// Was the current mouse press inside this element
        /// </summary>
        private BitVector32 didPress = new BitVector32(0);

        /// <summary>
        /// Was a drag event registered? (prevents click events)
        /// </summary>
        private bool didDrag; //meld with didPress?

        /// <summary>
        /// Was the mouse pressed inside this static (and is the mouse still down)
        /// </summary>
        /// <returns>True if the mouse is currently down and was pressed inside this static</returns>
        protected bool DidPressInside(MouseButtons button) =>
            didPress[1 << (int)button] && InputState.IsButtonDown(button);

        /// <summary>
        /// Was the touch pressed inside this static (and is the mouse still down)
        /// </summary>
        /// <returns>True if the mouse is currently down and was pressed inside this static</returns>
        protected bool DidPressInside(int touchIndex) =>
            didPress[1 << (touchIndex + (int)MouseButtons._TouchIndex)] && InputState.IsButtonDown(touchIndex);

        /// <summary>
        /// React to user input here. Updating should be performed in <see cref="UpdateSelf"/>
        /// </summary>
        /// <param name="time">game time</param>
        /// <returns>False if the input has been handled by this UI</returns>
        protected virtual bool HandleInput(GameTime time)
        {
            if (HasFocus)
            {
                if ((!ignoreTabKey && InputState.IsPress(Keys.Tab)) ||
                    InputState.IsAnyPress(Buttons.RightShoulder))
                {
                    if (InputState.IsMod(KeyMod.Shift))
                        FocusPrevious();
                    else
                        FocusNext();
                    return false;
                }

                if (InputState.IsAnyPress(Buttons.LeftShoulder))
                {
                    FocusPrevious();
                    return false;
                }

                var thumb = InputState.Thumbsticks().Left;
                var lastThumb = InputState.LastThumbsticks().Left;
                if (thumb != Vector2.Zero && lastThumb == Vector2.Zero)
                {
                    if (FocusGeographically(thumb) != null)
                        return false;
                }

                if (!ignoreEnterKey && InputState.IsPress(Keys.Enter))
                {
                    TriggerClick(Vector2.Zero, (int)Keys.Enter, DeviceType.Keyboard);
                    return false;
                }
                if (!ignoreSpaceKey && InputState.IsPress(Keys.Space))
                {
                    TriggerClick(Vector2.Zero, (int)Keys.Space, DeviceType.Keyboard);
                    return false;
                }
                if (InputState.IsAnyPress(Buttons.A, out var player))
                {
                    TriggerClick(Vector2.Zero, (int)Keys.Enter, DeviceType.Gamepad, (int)player);
                    return false;
                }
            }

            if (!HandleTouchInput())
                return false;

            var mouse = InputState.MousePoint;
            return HandleMouseInput(mouse, MouseButtons.Left) &&
                HandleMouseInput(mouse, MouseButtons.Right) &&
                HandleMouseInput(mouse, MouseButtons.Middle);
        }

        bool HandleTouchInput()
        {
            /* gestures don't work very conveniently 
            if (InputState.Gestures.TryGetValue(GestureType.Tap, out var gesture) &&
                VisibleBounds.Contains(gesture.Position))
            {
                var pea = new PointerEventArgs(this)
                {
                    position = (gesture.Position - OffsetContentArea.Location.ToVector2()) + Padding,
                    button = 0,
                    device = DeviceType.Touch
                };
                BubbleEvent(PressEvent, pea);
                BubbleEvent(ClickEvent, pea);

                if (CanFocus)
                {
                    HasFocus = true;
                    return false;
                }
            }
            else if (InputState.Gestures.TryGetValue(GestureType.FreeDrag, out gesture) && 
                VisibleBounds.Contains(gesture.Position))
            {
                //todo: this needs to support didPress style of moving finger outside of control

                var pea = new DragEventArgs(this)
                {
                    position = (gesture.Position - OffsetContentArea.Location.ToVector2()) + Padding,
                    button = 0,
                    device = DeviceType.Touch,
                    delta = gesture.Delta
                };
                BubbleEvent(DragEvent, pea);

                if (CanFocus)
                {
                    HasFocus = true;
                    return false;
                }
            }*/

            bool touched = false;
            for (int touchIndex = InputState.touches.Count - 1; touchIndex >= 0; --touchIndex)
            {
                var touch = InputState.touches[touchIndex];

                if (InputState.IsPress(touchIndex) && VisibleBounds.Contains(touch.Position))
                {
                    var pea = new PointerEventArgs(this)
                    {
                        position = touch.Position - OffsetContentArea.Location.ToVector2() + Padding,
                        button = touchIndex,
                        device = DeviceType.Touch
                    };
                    BubbleEvent(PressEvent, pea);

                    didPress[1 << (touchIndex + (int)MouseButtons._TouchIndex)] = true;
                    if (CanFocus)
                    {
                        HasFocus = true;
                        return false;
                    }
                }

                //input capture
                //todo: maybe add capture setting
                else if (InputState.lastTouches.Count > touchIndex && DidPressInside(touchIndex))
                {
                    // first if clause ^ shouldnt be necessary

                    var lastTouch = InputState.lastTouches[touchIndex];
                    if (lastTouch.Position != touch.Position)
                    {
                        var dea = new DragEventArgs(this)
                        {
                            delta = touch.Position - lastTouch.Position,
                            position = touch.Position - OffsetContentArea.Location.ToVector2() + Padding,
                            button = touchIndex,
                            device = DeviceType.Touch
                        };
                        BubbleEvent(DragEvent, dea);
                    }
                    return false;
                }
            }

            for (int touchIndex = 0; touchIndex < InputState.lastTouches.Count; ++touchIndex)
            {
                var touch = InputState.lastTouches[touchIndex];

                if (InputState.IsButtonUp(touchIndex)) //may need to be alted for touch
                {
                    if (didPress[1 << (touchIndex + (int)MouseButtons._TouchIndex)])
                    {
                        didPress[1 << (touchIndex + (int)MouseButtons._TouchIndex)] = false;
                        if (VisibleBounds.Contains(touch.Position)) //gesture pos
                        {
                            //todo: only trigger click if did not drag (?) (only if drag event)

                            TriggerClick(
                                touch.Position - OffsetContentArea.Location.ToVector2() + Padding,
                                touchIndex,
                                DeviceType.Touch
                            );
                            return false;
                        }
                    }
                }
            }

            return !touched;
        }

        bool HandleMouseInput(Point mousePosition, MouseButtons button)
        {
            bool isHovering = VisibleBounds.Contains(mousePosition);
            if (InputState.IsPress(button) && isHovering)
            {
                didPress[1 << (int)button] = true;

                var pea = new PointerEventArgs(this)
                {
                    position = (mousePosition - OffsetContentArea.Location).ToVector2() + Padding,
                    button = (int)button,
                    device = DeviceType.Mouse
                };
                BubbleEvent(PressEvent, pea);

                ApplyStateStyle();
                if (CanFocus)
                {
                    HasFocus = true;
                    return false; //todo: always return false?
                }
            }

            //input capture
            //todo: maybe add capture setting
            else if (DidPressInside(button))
            {
                //if (isHovering)
                //    ApplyStyles(GetStyles(Style, "Press"));

                var lastMousePosition = InputState.LastMousePoint;
                if (lastMousePosition != mousePosition)
                {
                    //wrap mouse in window?
                    var dea = new DragEventArgs(this)
                    {
                        delta = (mousePosition - lastMousePosition).ToVector2(),
                        position = (mousePosition - OffsetContentArea.Location).ToVector2() + Padding,
                        button = (int)button,
                        device = DeviceType.Mouse
                    };

                    //not perfect
                    didDrag |= (BubbleEvent(DragEvent, dea) != null);
                }
                return false;
            }

            else if (InputState.IsButtonUp(button))
            {
                if (isHovering && HoveredElement == null)
                    HoveredElement = this;

                if (didPress[1 << (int)button])
                {
                    didPress[1 << (int)button] = false;
                    if (!didDrag && VisibleBounds.Contains(mousePosition)) //gesture pos
                    {
                        TriggerClick(
                            (mousePosition - OffsetContentArea.Location).ToVector2() + Padding,
                            (int)button,
                            DeviceType.Mouse
                        );

                        return false;
                    }

                    didDrag = false;
                }
            }

            return true;
        }

        public void TriggerClick(Vector2 relativePosition, int button = 0, DeviceType device = DeviceType.Mouse, int deviceIndex = 0)
        {
            var ce = new PointerEventArgs(this)
            {
                position = relativePosition,
                button = button,
                device = device,
                deviceIndex = deviceIndex
            };
            BubbleEvent(ClickEvent, ce);
        }

    }
}
