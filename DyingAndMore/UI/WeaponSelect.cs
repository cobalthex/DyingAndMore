using System.Collections.Generic;
using DyingAndMore.Game.Weapons;
using Takai.UI;

namespace DyingAndMore.UI
{
    public class WeaponSelect : DropdownSelect<WeaponClass>
    {
        /// <summary>
        /// An instance of the selected weapon. Created on demand (and cached)
        /// </summary>
        public WeaponInstance Instance
        {
            get => _instance ?? (_instance = SelectedItem?.Instantiate());
            set
            {
                if (_instance == value)
                    return;

                _instance = value;
                SelectedItem = _instance?.Class;
            }
        }
        private WeaponInstance _instance;

        public WeaponSelect()
        {
            BorderColor = Microsoft.Xna.Framework.Color.White;

            ItemTemplate = new Static
            {
                Bindings = new List<Takai.Data.Binding>
                {
                    new Takai.Data.Binding("Name", "Text")
                }
            };

            //todo: object cache
            foreach (var entry in System.IO.Directory.EnumerateFiles("Content/Weapons", "*.wpn.tk", System.IO.SearchOption.AllDirectories))
            {
                try
                {
                    var weapon = Takai.Data.Cache.Load<WeaponClass>(entry);
                    Items.Add(weapon);
                }
                catch { }
            }


        }

        protected UIEventResult OnSelectedItemChanged(Static sender, SelectionChangedEventArgs e)
        {
            _instance = null;
            System.Diagnostics.Debug.WriteLine(SelectedItem);
            return UIEventResult.Handled;
        }
    }
}
