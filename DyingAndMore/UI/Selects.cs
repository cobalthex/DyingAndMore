using Takai.UI;
using DyingAndMore.Game.Weapons;
using Takai.Game;

namespace DyingAndMore.UI
{
    public class FactionSelect : EnumSelect<Game.Entities.Factions> { }

    class TriggerFilterSelect : TypeSelect
    {
        public TriggerFilterSelect()
        {
            AddTypeTree<ITriggerFilter>();
        }
    }

    class ControllerSelect : TypeSelect
    {
        public ControllerSelect()
        {
            AddTypeTree<Game.Entities.Controller>();
        }
    }

    class GameCommandSelect : TypeSelect
    {
        public GameCommandSelect()
        {
            AddTypeTree<GameCommand>();
        }
    }

    class GameCommandsList : ItemList<GameCommand> { }

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

    public class EffectsSelect : DropdownSelect<EffectsClass>
    {
        public EffectsSelect()
            : base()
        {
            ItemTemplate = new Static
            {
                Bindings = new System.Collections.Generic.List<Takai.Data.Binding>
                {
                    new Takai.Data.Binding("File", "Text")
                }
            };

            //todo: object cache
            foreach (var entry in System.IO.Directory.EnumerateFiles("Content/Effects", "*.fx.tk", System.IO.SearchOption.AllDirectories))
            {
                try
                {
                    var fx = Takai.Data.Cache.Load<EffectsClass>(entry);
                    Items.Add(fx);
                }
                catch { }
            }
        }
    }


    class SquadSelect : DropdownSelect<Game.Entities.Squad>
    {
        public override void OpenDropdown() //hacky
        {
            Items.Clear();
            if (Takai.Data.Binding.Globals.TryGetValue("Map.Squads", out var squads) &&
                squads is System.Collections.Generic.Dictionary<string, Game.Entities.Squad> sqDict)
            {
                foreach (var squad in sqDict)
                    Items.Add(squad.Value);
            }
            base.OpenDropdown();
        }
    }
}
