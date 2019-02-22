namespace DyingAndMore.UI
{
    class ControllerSelect : Takai.UI.TypeSelect
    {
        public ControllerSelect()
        {
            AddTypeTree<Game.Entities.Controller>();
        }
    }
}
