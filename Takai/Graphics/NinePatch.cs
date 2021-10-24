using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Takai.Graphics
{
    /// <summary>
    /// A sprite broken into nine sections where the center regions stretch
    /// Useful for things like UI borders/frames
    /// </summary>
    public struct NinePatch
    {
        public Sprite Sprite { get; set; }
        public Rectangle CenterRegion { get; set; }

        public static implicit operator NinePatch(Sprite sprite)
        {
            if (sprite == null)
                return new NinePatch(); //todo ?

            return new NinePatch
            {
                Sprite = sprite,
                CenterRegion = new Rectangle(0, 0, sprite.Width, sprite.Height)
            };
        }

        public static implicit operator NinePatch(Texture2D texture)
        {
            if (texture == null)
                return new NinePatch(); //todo ?

            return new NinePatch
            {
                Sprite = new Sprite(texture),
                CenterRegion = new Rectangle(0, 0, texture.Width, texture.Height)
            };
        }

        //todo: clip rect
        public void Draw(SpriteBatch spriteBatch, Rectangle destination, TimeSpan? elapsedTime = null)
        {
            if (Sprite == null)
                return;

            //todo: handle regions smaller than sprite size

            var lwidth = CenterRegion.X;
            var rwidth = Sprite.Width - CenterRegion.Right;


            var theight = CenterRegion.Y;
            var bheight = Sprite.Height - CenterRegion.Bottom;

            var time = elapsedTime.HasValue ? elapsedTime.Value : Sprite.ElapsedTime;

            if (lwidth == 0 && rwidth == 0 &&
                theight == 0 && bheight == 0)
            {
                Sprite.Draw(
                    spriteBatch,
                    destination,
                    0,
                    Color.White,
                    time
                );
                return;
            }

            var xs = destination.Width - lwidth - rwidth;
            var ys = destination.Height - theight - bheight;

            //draw in order of:
            //  1 2 3
            //  4 5 6
            //  7 8 9
            //sections 2, 4, 5, 6, 8 are stretched (5 in both X and Y)

            //support tiling?

            //1 2 3
            var dest = new Rectangle(destination.X, destination.Y, lwidth, lwidth);
            Sprite.Draw(
                spriteBatch,
                dest,
                new Rectangle(0, 0, lwidth, theight),
                0,
                Color.White,
                time
            );
            dest.X += lwidth;
            dest.Width = xs;
            Sprite.Draw(
                spriteBatch,
                dest,
                new Rectangle(CenterRegion.X, 0, CenterRegion.Width, theight),
                0,
                Color.White,
                time
            );
            dest.X += xs;
            dest.Width = rwidth;
            Sprite.Draw(
                spriteBatch,
                dest,
                new Rectangle(CenterRegion.Right, 0, rwidth, theight),
                0,
                Color.White,
                time
            );

            //4 5 6
            dest = new Rectangle(destination.X, destination.Y + theight, lwidth, ys);
            Sprite.Draw(
                spriteBatch,
                dest,
                new Rectangle(0, CenterRegion.Y, lwidth, CenterRegion.Height),
                0,
                Color.White,
                time
            );
            dest.X += lwidth;
            dest.Width = xs;
            Sprite.Draw(
                spriteBatch,
                dest,
                CenterRegion,
                0,
                Color.White,
                time
            );
            dest.X += xs;
            dest.Width = rwidth;
            Sprite.Draw(
                spriteBatch,
                dest,
                new Rectangle(CenterRegion.Right, CenterRegion.Y, rwidth, CenterRegion.Height),
                0,
                Color.White,
                time
            );
            
            //7 8 9
            dest = new Rectangle(destination.X, destination.Bottom - bheight, lwidth, bheight);
            Sprite.Draw(
                spriteBatch,
                dest,
                new Rectangle(0, CenterRegion.Bottom, lwidth, bheight),
                0,
                Color.White,
                time
            );
            dest.X += lwidth;
            dest.Width = xs;
            Sprite.Draw(
                spriteBatch,
                dest,
                new Rectangle(CenterRegion.X, CenterRegion.Bottom, CenterRegion.Width, bheight),
                0,
                Color.White,
                time
            );
            dest.X += xs;
            dest.Width = rwidth;
            Sprite.Draw(
                spriteBatch,
                dest,
                new Rectangle(CenterRegion.Right, CenterRegion.Bottom, rwidth, bheight),
                0,
                Color.White,
                time
            );
        }
    }
}
