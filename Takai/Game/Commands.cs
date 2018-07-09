﻿namespace Takai.Game
{
    public abstract class Command
    {
        public string ActionName { get; set; }
        public object ActionParameter { get; set; }

        public abstract void Invoke();
    }

    public delegate void CommandAction(object parameter);

    //todo: data model command, map command

    //todo: command delays?

    public class EntityCommand : Command
    {
        public EntityInstance Target { get; set; }

        public override void Invoke()
        {
            if (ActionName == null ||
                Target?.Actions == null ||
                !Target.Actions.TryGetValue(ActionName, out var action))
                return;

            action.Invoke(ActionParameter);
        }
    }
}
