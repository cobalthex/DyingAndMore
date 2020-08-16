using Takai.UI;
using DyingAndMore.Game.Weapons;
using DyingAndMore.Game.Entities;
using Takai.Game;
using DyingAndMore.Editor;
using System.Collections.Generic;

namespace DyingAndMore.UI
{
    public class FactionSelect : EnumSelect<Factions> { }

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
            AddTypeTree<Controller>();
        }
    }

    class GameCommandSelect : TypeSelect
    {
        public GameCommandSelect()
        {
            AddTypeTree<ICommand>();
        }
    }

    class GameCommandsList : ItemList<ICommand> { }

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

    public class ConditionSelect : ObjectSelect<ConditionClass, ConditionInstance>
    {
        public ConditionSelect()
            : base()
        {
            //todo: object cache
            foreach (var entry in System.IO.Directory.EnumerateFiles("Content/Actors/Conditions", "*.cond.tk", System.IO.SearchOption.AllDirectories))
            {
                try
                {
                    var weapon = Takai.Data.Cache.Load<ConditionClass>(entry);
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
            ItemUI = new Static
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

    class SquadSelect : DropdownSelect<Squad>
    {
        public override void OpenDropdown() //hacky
        {
            Items.Clear();
            if (Takai.Data.Binding.Globals.TryGetValue("Map.Squads", out var squads) &&
                squads is System.Collections.Generic.HashSet<Squad> sqSet)
            {
                foreach (var squad in sqSet)
                    Items.Add(squad);
            }
            base.OpenDropdown();
        }
    }

    class ActorList : ItemList<ActorClass> { } //object select?

    class BehaviorList : ItemList<Behavior> { }

    class TaskList : ItemList<Game.Entities.Tasks.ITask>
    {
        public TaskList()
        {
            ItemUI = new Static //todo: for testing
            {
                Bindings = new List<Takai.Data.Binding>
                {
                    new Takai.Data.Binding(":typename", "Text")
                }
            };
        }
    }

    class TaskSelect : TypeSelect
    {
        public TaskSelect()
        {
            AddTypeTree<Game.Entities.Tasks.ITask>();
        }
    }
}
