namespace DyingAndMore.UI
{
    class TriggerFilterSelect : Takai.UI.TypeSelect
    {
        public TriggerFilterSelect()
        {
            AddTypeTree<Takai.Game.ITriggerFilter>();
        }
    }
}
