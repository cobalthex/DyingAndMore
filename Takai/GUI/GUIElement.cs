using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.GUI
{
    /// <summary>
    /// The base of every 2D GUI control
    /// </summary>
    public class GUIElement
    {
        #region Data

        /// <summary>
        /// The name of the element
        /// </summary>
        public string name;
        /// <summary>
        /// Optional text of this element
        /// </summary>
        public string text;

        /// <summary>
        /// The bounds of this element
        /// </summary>
        public Rectangle bounds;

        /// <summary>
        /// The parent element of this (null if root)
        /// </summary>
        public GUIElement parent;
        /// <summary>
        /// Any and all child elements
        /// </summary>
        public System.Collections.Generic.List<GUIElement> children { get; protected set; }

        /// <summary>
        /// Is this element visible
        /// </summary>
        public bool isVisible;
        /// <summary>
        /// Is this element updating
        /// </summary>
        public bool isEnabled;

        /// <summary>
        /// The current orientation anchor
        /// </summary>
        public AnchorLocation anchor;

        /// <summary>
        /// Can this element be focused?
        /// </summary>
        public bool canFocus;
        /// <summary>
        /// Does this element have focus
        /// </summary>
        public bool hasFocus;

        #endregion

        #region Initialization

        /// <summary>
        /// Create a default gui element (not enabled)
        /// </summary>
        public GUIElement()
        {
            parent = null;
            children = new System.Collections.Generic.List<GUIElement>();
            bounds = Rectangle.Empty;
            name = "";
            text = "";

            isEnabled = false;
            isVisible = false;
            anchor = AnchorLocation.None;
            hasFocus = false;
            canFocus = false;
        }

        /// <summary>
        /// Create an enabled GUI element
        /// </summary>
        /// <param name="Name">The name of the element (for reference)</param>
        /// <param name="Text">The text</param>
        /// <param name="Bounds">The bounds of this element and the frame of reference for all child elements</param>
        /// <param name="Anchor">How this should be anchored relative to the parent</param>
        /// <param name="CanFocus">Can this element be focused?</param>
        public GUIElement(string Name, string Text, Rectangle Bounds, AnchorLocation Anchor, bool CanFocus)
        {
            parent = null;
            children = new System.Collections.Generic.List<GUIElement>();
            bounds = Bounds;
            name = Name;
            text = Text;

            isEnabled = true;
            isVisible = true;
            anchor = Anchor;
            hasFocus = false;
            canFocus = CanFocus;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Enumerate through all child elements
        /// </summary>
        /// <returns>The enumerator</returns>
        public System.Collections.Generic.List<GUIElement>.Enumerator GetEnumerator()
        {
            return children.GetEnumerator();
        }

        /// <summary>
        /// Add a child element and focus it
        /// </summary>
        /// <param name="Element">The element to add</param>
        public void AddChild(GUIElement Element)
        {
            Element.parent = this;
            children.Add(Element);
            if (Element.canFocus)
                Focus(Element);
        }

        //TODO: GetRect function to get child rectangle also with anchor adjustment

        //TODO: check children function to check point clicking (including anchor adjustment)
        /// <summary>
        /// Get the control under the point
        /// </summary>
        /// <returns></returns>
        public GUIElement GetControl(ref Point P)
        {
            return null;
        }

        /// <summary>
        /// Focus a specific element while defocusing everything else
        /// </summary>
        /// <param name="Element">The calling or a child of the calling element</param>
        public void Focus(GUIElement Element)
        {
            if (this == Element)
            {
                Defocus();
                this.hasFocus = true;
            }
            else
            {
                this.hasFocus = false;
                for (int i = 0; i < children.Count; i++)
                    children[i].Focus(Element);
            }
        }

        /// <summary>
        /// Defocus this and all children
        /// </summary>
        public void Defocus()
        {
            this.hasFocus = false;
            for (int i = 0; i < children.Count; i++)
                children[i].Defocus();
        }

        /// <summary>
        /// Gets the first focused element in this or any children
        /// </summary>
        /// <returns></returns>
        public GUIElement GetFocused()
        {
            if (hasFocus)
                return this;

            for (int i = 0; i < children.Count; i++)
            {
                var ret = children[i].GetFocused();
                if (ret != null)
                    return ret;
            }

            return null;
        }

        /// <summary>
        /// Update the element
        /// </summary>
        /// <param name="Time">Game time</param>
        public virtual void Update(GameTime Time)
        {
            UpdateChildren(Time);

            if (OnClick != null)
            {
                if (Takai.Input.TouchAbstractor.IsClick(bounds))
                {
                    if (canFocus)
                    {
                        if (parent == null)
                            Focus(this);
                        else
                            parent.Focus(this);
                    }
                    OnClick(this);
                }
#if WINDOWS
                else if (hasFocus && (Takai.Input.InputCatalog.IsKeyPress(Microsoft.Xna.Framework.Input.Keys.Enter) ||
                    Takai.Input.InputCatalog.IsKeyPress(Microsoft.Xna.Framework.Input.Keys.Space)))
                {
                    OnClick(this);
                }
#endif

#if WINDOWS || XBOX
                if (hasFocus && Takai.Input.InputCatalog.IsButtonPress(Microsoft.Xna.Framework.Input.Buttons.A, Takai.Input.InputCatalog.ActivePlayer))
                    OnClick(this);
#endif
            }
#if WINDOWS
            if (children.Count > 0)
            {
                //tab to move to next/previous element
                if (Takai.Input.InputCatalog.IsKeyPress(Microsoft.Xna.Framework.Input.Keys.Up) || ((Takai.Input.InputCatalog.KBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) ||
                    Takai.Input.InputCatalog.KBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift)) && Takai.Input.InputCatalog.IsKeyPress(Microsoft.Xna.Framework.Input.Keys.Tab)))
                {
                    //find previous element while ignoring not focusable elements
                    int idx = children.IndexOf(GetFocused());
                    int next; for (next = (idx - 1 < 0 ? children.Count - 1 : idx - 1); next != idx; next = (next - 1 < 0 ? children.Count - 1 : next - 1))
                        if (children[next].canFocus) break;
                    Focus(children[next]);
                }
                else if (Takai.Input.InputCatalog.IsKeyPress(Microsoft.Xna.Framework.Input.Keys.Down) || Takai.Input.InputCatalog.IsKeyPress(Microsoft.Xna.Framework.Input.Keys.Tab))
                {
                    //find next element while ignoring not focusable elements
                    int idx = children.IndexOf(GetFocused());
                    int next; for (next = (idx + 1) % children.Count; next != idx; next = (next + 1) % children.Count)
                        if (children[next].canFocus) break;
                    Focus(children[next]);
                }
            }
#endif
#if WINDOWS || XBOX

            if (children.Count > 0)
            {
                if (Takai.Input.InputCatalog.IsButtonPress(Microsoft.Xna.Framework.Input.Buttons.DPadDown, Takai.Input.InputCatalog.ActivePlayer) ||
                    Takai.Input.InputCatalog.IsButtonPress(Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickDown, Takai.Input.InputCatalog.ActivePlayer))
                {
                    //find next element while ignoring not focusable elements
                    int idx = children.IndexOf(GetFocused());
                    int next; for (next = (idx + 1) % children.Count; next != idx; next = (next + 1) % children.Count)
                        if (children[next].canFocus) break;
                    Focus(children[next]);
                }
                if (Takai.Input.InputCatalog.IsButtonPress(Microsoft.Xna.Framework.Input.Buttons.DPadUp, Takai.Input.InputCatalog.ActivePlayer) ||
                    Takai.Input.InputCatalog.IsButtonPress(Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickUp, Takai.Input.InputCatalog.ActivePlayer))
                {
                    //find previous element while ignoring not focusable elements
                    int idx = children.IndexOf(GetFocused());
                    int next; for (next = (idx - 1 < 0 ? children.Count - 1 : idx - 1); next != idx; next = (next - 1 < 0 ? children.Count - 1 : next - 1))
                        if (children[next].canFocus) break;
                    Focus(children[next]);
                }
            }
#endif
        }
        protected virtual void UpdateChildren(GameTime Time)
        {
            for (int i = 0; i < children.Count; i++)
                if (children[i].isEnabled)
                    children[i].Update(Time);
        }

        /// <summary>
        /// Draw the element
        /// </summary>
        /// <param name="SpriteBatch">The sprite batch to use</param>
        /// <param name="Time">Game time</param>
        /// <remarks>Base function will draw children</remarks>
        public void Draw(SpriteBatch SpriteBatch, GameTime Time)
        {
            if (!isVisible)
                return;
            if (OnDraw != null)
                OnDraw(SpriteBatch, Time);
            DrawChildren(SpriteBatch, Time);
        }
        protected void DrawChildren(SpriteBatch SpriteBatch, GameTime Time)
        {
            for (int i = 0; i < children.Count; i++)
                if (children[i].isVisible)
                    children[i].Draw(SpriteBatch, Time);
        }

        public delegate void DrawFunction(SpriteBatch SpriteBatch, GameTime Time);
        /// <summary>
        /// Called when the control is drawn
        /// </summary>
        public DrawFunction OnDraw;

        /// <summary>
        /// Handle a single touch/mouse click
        /// </summary>
        /// <param name="Sender">The object calling this</param>
        public delegate void ClickHandler(GUIElement Sender);

        /// <summary>
        /// Called when the element is clicked
        /// </summary>
        public ClickHandler OnClick;

        #endregion
    }

    #region Enumerations

    /// <summary>
    /// Object anchoring: Draws object relative to that direction (none = top and left by default)
    /// Bitwise OR (|) capable for corners. Opposite edges will center
    /// </summary>
    public enum AnchorLocation
    {
        /// <summary>
        /// No anchoring
        /// </summary>
        None = 0,
        /// <summary>
        /// Anchor on the left
        /// </summary>
        Left = 1,
        /// <summary>
        /// Anchor on the top
        /// </summary>
        Top = 2,
        /// <summary>
        /// Anchor on the right
        /// </summary>
        Right = 4,
        /// <summary>
        /// Anchor on the bottom
        /// </summary>
        Bottom = 8,
        /// <summary>
        /// Centered (all anchors)
        /// </summary>
        Center = Left | Right | Bottom | Top,
        /// <summary>
        /// Anchor to the top left
        /// </summary>
        TopLeft = Top | Left,
        /// <summary>
        /// Anchor to the top right
        /// </summary>
        TopRight = Top | Right,
        /// <summary>
        /// Anchor to the bottom left
        /// </summary>
        BottomLeft = Bottom | Left,
        /// <summary>
        /// Anchor to the bottom right
        /// </summary>
        BottomRight = Bottom | Right
    }

    #endregion
}
