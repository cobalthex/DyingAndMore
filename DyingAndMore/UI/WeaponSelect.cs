using DyingAndMore.Game.Weapons;
using Takai.UI;

namespace DyingAndMore.UI
{
    public class WeaponSelect : ObjectSelect<WeaponClass, WeaponInstance>
    {
        public WeaponSelect()
            : base()
        {
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
    }
}
