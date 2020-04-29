namespace Takai.Game
{
    public interface ICommand
    {
        string ActionName { get; set; }
        object ActionParameter { get; set; }

        void Invoke(MapBaseInstance map);
    }

    public delegate void CommandAction(object parameter);

    public class EntityCommand : ICommand
    {
        public string ActionName { get; set; }
        public object ActionParameter { get; set; }

        public EntityInstance Target { get; set; }

        public void Invoke(MapBaseInstance map)
        {
            if (ActionName == null ||
                Target?.Actions == null ||
                !Target.Actions.TryGetValue(ActionName, out var action))
                return;

            action.Invoke(ActionParameter);
        }

        /// <summary>
        /// Override this command and invoke on a specific target
        /// </summary>
        /// <param name="target">The target to apply to</param>
        public void Invoke(EntityInstance target)
        {
            if (ActionName == null ||
                target?.Actions == null ||
                !target.Actions.TryGetValue(ActionName, out var action))
                return;

            action.Invoke(ActionParameter);
        }
    }
}
