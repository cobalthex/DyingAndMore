using Takai.UI;

namespace DyingAndMore.UI
{
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
