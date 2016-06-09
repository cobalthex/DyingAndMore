namespace Takai.States
{
    /// <summary>
    /// A stack of states for the state manager
    /// </summary>
    public class StateStack
    {
        /// <summary>
        /// All of the states in this stack
        /// </summary>
        private System.Collections.Generic.List<State> states;

        /// <summary>
        /// How many states are in this stack
        /// </summary>
        public int Count { get { return states.Count; } }

        /// <summary>
        /// Create a new state stack
        /// </summary>
        public StateStack()
        {
            states = new System.Collections.Generic.List<State>();
        }

        /// <summary>
        /// Access all of the states like an array
        /// </summary>
        /// <param name="index">Index to access</param>
        /// <returns>Selected state</returns>
        public State this[int index]
        {
            get { if (states.Count > 0) return states[index]; else return null; }
        }

        /// <summary>
        /// Add a new state to the list (null entries are ignored)
        /// </summary>
        /// <param name="s">State to add</param>
        public void Push(State s)
        {
            if (s != null)
                states.Add(s);
        }

        /// <summary>
        /// Remove a state from the stack (null if no state on the stack)
        /// </summary>
        /// <returns>The state that was removed</returns>
        public State Pop()
        {
            if (states.Count > 0)
            {
                int idx = states.Count - 1;
                State s = states[idx];
                states.RemoveAt(idx);
                return s;
            }

            return null;
        }

        /// <summary>
        /// Check the state at the top of the stack (null if no states on the stack)
        /// </summary>
        /// <returns>The state at the top of the stack</returns>
        public State Peek()
        {
            if (states.Count > 0)
                return states[states.Count - 1];

            return null;
        }
    }
}
