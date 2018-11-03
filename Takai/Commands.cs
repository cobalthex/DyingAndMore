using System;
using System.Collections.Generic;

namespace Takai
{
    /// <summary>
    /// Command handler for commands issued from UI
    /// These are called after event handles
    /// </summary>
    /// <param name="source">The source UI object issuing the command</param>
    public delegate void CommandHandler(object source);

    public static class Commander
    {
        static Dictionary<string, List<CommandHandler>> allHandlers;

        public static void AddHandler(string command, CommandHandler handler)
        {
            if (command == null)
                throw new ArgumentNullException("Command handler cannot be null");

            if (!allHandlers.TryGetValue(command, out var handlers))
                allHandlers[command] = handlers = new List<CommandHandler>();
            handlers.Add(handler);
        }

        public static bool RemoveHandler(string command, CommandHandler handler)
        {
            if (allHandlers.TryGetValue(command, out var handlers))
                return handlers.Remove(handler);
            return false;
        }

        public static bool RemoveHandlers(string command)
        {
            return allHandlers.Remove(command);
        }

        public static void RemoveAllhandlers()
        {
            allHandlers.Clear();
        }

        public static bool Invoke(string command, object source)
        {
            if (command == null)
                return false;

            if (allHandlers.TryGetValue(command, out var handlers))
            {
                foreach (var handler in handlers)
                    handler.Invoke(source);
                return true;
            }

            return false;
        }
    }
}
