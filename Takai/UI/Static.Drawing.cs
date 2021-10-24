using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Graphics;

namespace Takai.UI
{
    public struct DrawContext
    {
        public GameTime gameTime;
        public SpriteBatch spriteBatch;
        public TextRenderer textRenderer;
    }

    public partial class Static
    {
        /// <summary>
        /// Draw this element, its decorators, and any children
        ///
        /// Draws depth-first, parent-most first
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to use</param>
        public virtual void Draw(DrawContext context)
        {
#if DEBUG
            boop.Restart();
#endif
            if (!IsEnabled)
                return;

            var draws = new Stack<Static>(Children.Count + 1);
            draws.Push(this);

            Static debugDraw = null;
            while (draws.Count > 0)
            {
                var toDraw = draws.Pop();
                if (toDraw.VisibleBounds.Width == 0 || toDraw.VisibleBounds.Height == 0)
                    continue;

                if (toDraw.BackgroundColor.A > 0)
                    Primitives2D.DrawFill(context.spriteBatch, toDraw.BackgroundColor, toDraw.VisibleBounds);
                if (toDraw.BackgroundSprite.Sprite != null)
                {
                    toDraw.BackgroundSprite.Sprite.ElapsedTime += context.gameTime.ElapsedGameTime;
                    toDraw.BackgroundSprite.Draw(context.spriteBatch, toDraw.VisibleBounds);
                }

                toDraw.DrawSelf(context);

                var borderColor = /*(toDraw.HasFocus && toDraw.CanFocus) ? FocusedBorderColor : */toDraw.BorderColor;
                if (DisplayDebugInfo && borderColor == Color.Transparent)
                    borderColor = isMeasureValid && isArrangeValid ? Color.SteelBlue : Color.Tomato;

                if (DisplayDebugInfo && toDraw.VisibleBounds.Contains(Input.InputState.MousePoint))
                    debugDraw = toDraw;

                if (borderColor.A > 0)
                {
                    var offsetRect = toDraw.OffsetContentArea;
                    offsetRect.Inflate(toDraw.Padding.X, toDraw.Padding.Y);
                    var offset = offsetRect.Location.ToVector2();
                    DrawHLine(context.spriteBatch, borderColor, 0, 0, offsetRect.Width, offset, toDraw.VisibleBounds);
                    DrawVLine(context.spriteBatch, borderColor, offsetRect.Width, 0, offsetRect.Height, offset, toDraw.VisibleBounds);
                    DrawHLine(context.spriteBatch, borderColor, offsetRect.Height, 0, offsetRect.Width, offset, toDraw.VisibleBounds);
                    DrawVLine(context.spriteBatch, borderColor, 0, 0, offsetRect.Height, offset, toDraw.VisibleBounds);
                }

                for (int i = toDraw.Children.Count - 1; i >= 0; --i)
                {
                    if (toDraw.Children[i].IsEnabled)
                        draws.Push(toDraw.Children[i]);
                }
            }

#if DEBUG //todo: re-evaluate
            if (debugDraw != null)
            {
                DrawTextOptions drawText = new DrawTextOptions(
                    $"Measure Count: {totalMeasureCount}" +
                    $"\nArrange Count: {totalArrangeCount}" +
                    $"\nTotal Elements Created: {idCounter}" +
                    $"\nTotal binding Updates: {Data.Binding.TotalUpdateCount}" +
                    $"\nTotal cached styles: {StylesDictionary.Default.RenderCacheCount}" +
                    $"\nHovered Element: {HoveredElement?.DebugId}",
                    DebugFont, DebugTextStyle, Color.CornflowerBlue, new Vector2(10)
                );
                context.textRenderer.Draw(drawText);

                debugDraw.DrawDebugInfo(context);

                if (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Pause))
                    debugDraw.BreakOnThis();
            }
            lastDrawDuration = boop.Elapsed;
#endif
        }

        public void DrawDebugInfo(DrawContext context)
        {
            Primitives2D.DrawRect(context.spriteBatch, Color.Cyan, VisibleBounds);

            var rect = OffsetContentArea;
            Primitives2D.DrawRect(context.spriteBatch, new Color(Color.Orange, 0.5f), rect);

            rect.Inflate(Padding.X, Padding.Y);
            Primitives2D.DrawRect(context.spriteBatch, Color.OrangeRed, rect);

            string info = $"`_{GetType().Name}`_\n"
#if DEBUG
                        + $"ID: {DebugId}\n"
#endif
                        + $"Name: {(Name ?? "(No name)")}\n"
#if DEBUG
                        + $"Parent ID: {Parent?.DebugId}\n"
#endif
                        + $"Children: {Children?.Count ?? 0}\n"
                        + $"Bounds: {OffsetContentArea}\n" //visible bounds?
                        + $"Position: {Position}, Size: {Size}, Padding: {Padding}\n"
                        + $"HAlign: {HorizontalAlignment}, VAlign: {VerticalAlignment}\n"
                        + $"Styles: {Styles}\n"
                        + (HasFocus ? "(Has focus)\n" : "")
                        + $"Bindings: {(Bindings == null ? "(None)" : string.Join(",", Bindings))}\n"
                        + $"Events: {events?.Count}, Commands: {CommandActions?.Count}\n";

            var drawPos = rect.Location + new Point(rect.Width + 10, rect.Height + 10);
            var size = DebugFont.MeasureString(info, DebugTextStyle);
            drawPos = Util.Clamp(new Rectangle(drawPos.X, drawPos.Y, (int)size.X, (int)size.Y), Runtime.GraphicsDevice.Viewport.Bounds);
            drawPos -= new Point(10);

            var drawText = new DrawTextOptions(info, DebugFont, DebugTextStyle, Color.Gold, drawPos.ToVector2());
            context.textRenderer.Draw(drawText);
        }

        /// <summary>
        /// Draw only this item (no children)
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to use</param>
        protected virtual void DrawSelf(DrawContext context)
        {
            DrawText(context.textRenderer, Text, Vector2.Zero);
        }

        //todo: pass in context to methods below

        /// <summary>
        /// Draw text clipped to the visible region of this element
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to use</param>
        /// <param name="position">The relative position (to the element) to draw this text</param>
        protected void DrawText(TextRenderer textRenderer, string text, Vector2 position)
        {
            if (Font == null || text == null)
                return;

            position += (OffsetContentArea.Location - VisibleContentArea.Location).ToVector2();

            var drawText = new DrawTextOptions(
                text,
                Font,
                TextStyle,
                Color,
                VisibleContentArea.Location.ToVector2()
            )
            {
                clipSize = VisibleContentArea.Size.ToVector2(),
                relativeOffset = position
            };
            textRenderer.Draw(drawText);
            //Font.Draw(spriteBatch, Text, 0, Text.Length, VisibleContentArea, position, Color);
        }

        /// <summary>
        /// Draw an arbitrary line, clipped to the visible content area.
        /// Note: more computationally expensive than DrawVLine or DrawHLine
        /// </summary>
        /// <param name="spriteBatch">spriteBatch to use</param>
        /// <param name="color">Color to draw the line</param>
        /// <param name="a">The start of the line</param>
        /// <param name="b">The end of the line</param>
        protected void DrawLine(SpriteBatch spriteBatch, Color color, Vector2 a, Vector2 b)
        {
            var offset = OffsetContentArea.Location.ToVector2();
            a += offset;
            b += offset;

            if (!Util.ClipLine(ref a, ref b, VisibleContentArea))
                return;

            Primitives2D.DrawLine(spriteBatch, color, a, b);
        }

        protected void DrawVLine(SpriteBatch spriteBatch, Color color, float x, float y1, float y2)
        {
            DrawVLine(spriteBatch, color, x, y1, y2, OffsetContentArea.Location.ToVector2(), VisibleContentArea);
        }
        private void DrawVLine(SpriteBatch spriteBatch, Color color, float x, float y1, float y2, Vector2 offset, Rectangle visibleClip)
        {
            x += offset.X;
            if (x < visibleClip.Left || x > visibleClip.Right)
                return;

            y1 = Util.Clamp(y1 + offset.Y, visibleClip.Top, visibleClip.Bottom);
            y2 = Util.Clamp(y2 + offset.Y, visibleClip.Top, visibleClip.Bottom);

            if (y1 == y2)
                return;

            Primitives2D.DrawLine(spriteBatch, color, new Vector2(x, y1), new Vector2(x, y2));
        }

        protected void DrawHLine(SpriteBatch spriteBatch, Color color, float y, float x1, float x2)
        {
            DrawHLine(spriteBatch, color, y, x1, x2, OffsetContentArea.Location.ToVector2(), VisibleContentArea);
        }
        private void DrawHLine(SpriteBatch spriteBatch, Color color, float y, float x1, float x2, Vector2 offset, Rectangle visibleClip)
        {
            y += offset.Y;
            if (y < visibleClip.Top || y > visibleClip.Bottom)
                return;

            x1 = Util.Clamp(x1 + offset.X, visibleClip.Left, visibleClip.Right);
            x2 = Util.Clamp(x2 + offset.X, visibleClip.Left, visibleClip.Right);

            if (x1 == x2)
                return;

            Primitives2D.DrawLine(spriteBatch, color, new Vector2(x1, y), new Vector2(x2, y));
        }

        protected void DrawRect(SpriteBatch spriteBatch, Color color, Rectangle localRect)
        {
            var offset = OffsetContentArea.Location.ToVector2();
            DrawHLine(spriteBatch, color, localRect.Top, localRect.Left, localRect.Right, offset, VisibleContentArea);
            DrawVLine(spriteBatch, color, localRect.Right, localRect.Top, localRect.Bottom, offset, VisibleContentArea);
            DrawHLine(spriteBatch, color, localRect.Bottom, localRect.Left, localRect.Right, offset, VisibleContentArea);
            DrawVLine(spriteBatch, color, localRect.Left, localRect.Top, localRect.Bottom, offset, VisibleContentArea);
        }

        protected void DrawFill(SpriteBatch spriteBatch, Color color, Rectangle localRect)
        {
            localRect.Offset(OffsetContentArea.Location);
            Primitives2D.DrawFill(spriteBatch, color, Rectangle.Intersect(VisibleContentArea, localRect));
        }

        protected void DrawSprite(SpriteBatch spriteBatch, Sprite sprite, Rectangle localRect)
        {

            if (sprite == null)
                return;

            DrawSpriteCustomRegion(spriteBatch, sprite, localRect, VisibleContentArea, sprite.ElapsedTime);
        }

        protected void DrawSprite(SpriteBatch spriteBatch, Sprite sprite, Rectangle localRect, TimeSpan elapsedTime)
        {
            DrawSpriteCustomRegion(spriteBatch, sprite, localRect, VisibleContentArea, elapsedTime);
        }

        void DrawSpriteCustomRegion(SpriteBatch spriteBatch, Sprite sprite, Rectangle localRect, Rectangle clipRegion, TimeSpan elapsedTime)
        {
            if (sprite?.Texture == null || localRect.Width == 0 || localRect.Height == 0)
                return;

            //adjust sprite based on offset of container and clip to clipRegion
            localRect.Offset(clipRegion.Location - VisibleOffset);
            var finalRect = Rectangle.Intersect(localRect, clipRegion);

            //scale the clip region by the size of the destRect
            //to get a relative clip size
            var sx = sprite.Width / (float)localRect.Width;
            var sy = sprite.Height / (float)localRect.Height;

            var vx = (int)((localRect.Width - finalRect.Width) * sx);
            var vy = (int)((localRect.Height - finalRect.Height) * sy);
            var clip = new Rectangle(
                vx,
                vy,
                sprite.Width - vx,
                sprite.Height - vy
            );

            //todo: do this without conditionals
            if (finalRect.Right == clipRegion.Right)
                clip.X = 0;
            if (finalRect.Bottom == clipRegion.Bottom)
                clip.Y = 0;

            sprite.Draw(spriteBatch, finalRect, clip, 0, Color.White, elapsedTime);
            //Primitives2D.DrawRect(spriteBatch, Color.LightSteelBlue, finalRect);
        }

    }
}
