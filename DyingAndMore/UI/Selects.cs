using Takai.UI;
using DyingAndMore.Game.Weapons;
using DyingAndMore.Game.Entities;
using Takai.Game;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Takai.Data;

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
                    var weapon = Cache.Load<WeaponClass>(entry);
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
                    var weapon = Cache.Load<ConditionClass>(entry);
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
                Bindings = new List<Binding>
                {
                    new Binding("File", "Text")
                }
            };

            //todo: object cache
            foreach (var entry in System.IO.Directory.EnumerateFiles("Content/Effects", "*.fx.tk", System.IO.SearchOption.AllDirectories))
            {
                try
                {
                    var fx = Cache.Load<EffectsClass>(entry);
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
            if (Binding.Globals.TryGetValue("Map.Squads", out var squads) &&
                squads is HashSet<Squad> sqSet)
            {
                foreach (var squad in sqSet)
                    Items.Add(squad);
            }
            base.OpenDropdown();
        }
    }

    class PathSelect : DropdownSelect<Editor.NamedPath>
    {
        public VectorCurve SelectedPath
        {
            get => SelectedIndex >= 0 ? SelectedItem.path : default;
            set => SelectedItem = new Editor.NamedPath { path = value };
        }

        public PathSelect()
        {
            ItemUI = new List { Direction = Direction.Horizontal };
            ItemUI.AddChild(new Static
            {
                Bindings = new List<Binding>
                {
                    new Binding("name", "Text")
                }
            });
            ItemUI.AddChild(new Static
            {
                Bindings = new List<Binding>
                {
                    new Binding("path.Count", "Text")
                    {
                        Converter = new StringFormatConverter("({0} nodes)")
                    }
                }
            });
        }

        public override void OpenDropdown() //hacky
        {
            Items.Clear();
            if (Binding.Globals.TryGetValue("Editor.Paths", out var paths) &&
                paths is List<Editor.NamedPath> pathsList)
            {
                foreach (var path in pathsList)
                    Items.Add(path);
            }
            InvalidateMeasure();
            base.OpenDropdown();
        }
    }

    class ActorList : ItemList<ActorClass> { } //object select?

    class BehaviorList : ItemList<Behavior> { }

    class TaskList : ItemList<Game.Entities.Tasks.ITask> { }

    class TaskSelect : TypeSelect
    {
        public TaskSelect()
        {
            AddTypeTree<Game.Entities.Tasks.ITask>();
        }
    }
    class OffensiveTaskSelect : TypeSelect
    {
        public OffensiveTaskSelect()
        {
            AddTypeTreeByAttribute<Game.Entities.Tasks.OffensiveTaskAttribute>();
        }
    }

    class DefensiveTaskSelect : TypeSelect
    {
        public DefensiveTaskSelect()
        {
            AddTypeTreeByAttribute<Game.Entities.Tasks.DefensiveTaskAttribute>();
        }
    }
    class NavigationTaskSelect : TypeSelect
    {
        public NavigationTaskSelect()
        {
            AddTypeTreeByAttribute<Game.Entities.Tasks.NavigationTaskAttribute>();
        }
    }
    class TargetingTaskSelect : TypeSelect
    {
        public TargetingTaskSelect()
        {
            AddTypeTreeByAttribute<Game.Entities.Tasks.TargetingTaskAttribute>();
        }
    }
    class MiscellaneousTaskSelect : TypeSelect
    {
        public MiscellaneousTaskSelect()
        {
            AddTypeTreeByAttribute<Game.Entities.Tasks.MiscellaneousTaskAttribute>();
        }
    }
}
