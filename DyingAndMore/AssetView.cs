using System;
using System.Collections.Generic;
using Takai.UI;
using Xna = Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace DyingAndMore
{
    class AssetView : Static
    {
        Static assets;
        Static view;

        SoundEffectInstance activeSound;

        public bool ShowNonPreviewableItems { get; set; } = true;

        public AssetView()
        {
            BackgroundColor = Xna.Color.Black;
            HorizontalAlignment = Alignment.Stretch;
            VerticalAlignment = Alignment.Stretch;

            view = new Static();
            var list = new List()
            {
                Direction = Direction.Vertical,
                HorizontalAlignment = Alignment.Stretch,
            };

            foreach (var obj in Takai.Data.Cache.Objects)
            {
                if (!(obj.Value is IDisposable) && !ShowNonPreviewableItems)
                    continue;

                var listItem = new Static()
                {
                    Name = obj.Key,
                    Text = System.IO.Path.GetFileName(obj.Key),
                    HorizontalAlignment = Alignment.Stretch,
                    Padding = new Xna.Vector2(10)
                };
                listItem.AutoSize();
                listItem.Click += ListItem_Click;
                list.AddChild(listItem);
            }

            list.AutoSize();
            assets = new ScrollBox();
            assets.AddChild(list);
            AddChild(assets);
            AddChild(view);
        }

        private void ListItem_Click(object sender, ClickEventArgs e)
        {
            var asset = Takai.Data.Cache.Get(((Static)sender).Name);

            if (activeSound != null)
            {
                activeSound.Stop();
                activeSound = null;
            }

            if (asset is Texture2D tex)
            {
                var gfx = new Graphic()
                {
                    Sprite = new Takai.Graphics.Sprite(tex),
                    HorizontalAlignment = Alignment.Middle,
                    VerticalAlignment = Alignment.Middle,
                    Padding = new Xna.Vector2(10)
                };
                gfx.AutoSize();
                view.ReplaceAllChildren(gfx);
            }
            else if (asset is Takai.Graphics.BitmapFont fnt)
            {
                var inp = new TextInput()
                {
                    Font = fnt,
                    Size = new Xna.Vector2(400, fnt.MaxCharHeight + 4),
                    HorizontalAlignment = Alignment.Middle,
                    VerticalAlignment = Alignment.Middle
                };
                inp.HasFocus = true;
                view.ReplaceAllChildren(inp);
            }
            else if (asset is SoundEffect sfx)
            {
                activeSound = sfx.CreateInstance();

                var list = new List()
                {
                    Direction = Direction.Vertical,
                    HorizontalAlignment = Alignment.Middle,
                    VerticalAlignment = Alignment.Middle,
                    Margin = 10,
                };

                list.AddChild(new Static() { Text = "Pitch", Size = new Xna.Vector2(80, 30) });
                var track = new TrackBar()
                {
                    Text = "Pitch",
                    Minimum = -1000,
                    Maximum = 1000,
                    Value = 0,
                    Increment = 1,
                    HorizontalAlignment = Alignment.Middle,
                    Size = new Xna.Vector2(200, 30)
                };
                track.Click += delegate (object _sender, ClickEventArgs _e)
                {
                    if (activeSound != null)
                    {
                        activeSound.Pitch = ((TrackBar)_sender).Value / 1000f;
                        activeSound.Stop();
                        activeSound.Play();
                    }
                };
                list.AddChild(track);

                list.AddChild(new Static() { Text = "Pan", Size = new Xna.Vector2(80, 30) });
                track = (TrackBar)track.Clone();
                track.Text = "Pan";
                track.Click += delegate (object _sender, ClickEventArgs _e)
                {
                    if (activeSound != null)
                    {
                        activeSound.Pan = ((TrackBar)_sender).Value / 1000f;
                        activeSound.Stop();
                        activeSound.Play();
                    }
                };
                list.AddChild(track);
                list.AutoSize();

                view.ReplaceAllChildren(list);

                activeSound.Play();
            }
            else if (asset is Takai.Game.EntityClass ent)
            {
                var anim = System.Linq.Enumerable.FirstOrDefault(ent.Animations.Values);
                if (anim != null && anim.Sprite != null)
                {
                    var gfx = new Graphic()
                    {
                        Sprite = anim.Sprite,
                        HorizontalAlignment = Alignment.Middle,
                        VerticalAlignment = Alignment.Middle,
                        Padding = new Xna.Vector2(10)
                    };
                    gfx.AutoSize();
                    view.ReplaceAllChildren(gfx);
                }
            }
            else if (asset is Static ui)
            {
                //ui = ui.Clone();
                //foreach (var child in ui.EnumerateRecursive())
                //{
                //}
                view.ReplaceAllChildren(ui);
            }
            else
            {
                var text = new Static
                {
                    Text = "No Preview",
                    Color = new Xna.Color(1, 1, 1, 0.5f),
                    HorizontalAlignment = Alignment.Middle,
                    VerticalAlignment = Alignment.Middle,
                    Padding = new Xna.Vector2(10)
                };
                text.AutoSize();
                view.ReplaceAllChildren(text);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            assets.Dimensions = new Xna.Rectangle(0, 0, 240, (int)Size.Y);
            view.Dimensions = new Xna.Rectangle(240, 0, (int)Size.X - 240, (int)Size.Y);
        }
    }
}
