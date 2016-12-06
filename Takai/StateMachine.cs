using System;
using System.Collections.Generic;

namespace Takai.Game
{
    public class StateMachine<TKey, TState>
    {
        public class Transition
        {
            TKey Source;
            HashSet<TKey> Destinations;
        }
        
        /// <summary>
        /// All of the available states 
        /// </summary>
        public Dictionary<TKey, TState> States { get; set; } = new Dictionary<TKey, TState>();

        /// <summary>
        /// Transitions between states (Not all state pairs have transitions)
        /// </summary>
        public Dictionary<TKey, HashSet<TKey>> Transitions { get; set; } = new Dictionary<TKey, HashSet<TKey>>();

        /// <summary>
        /// THe currently active states, and cached state value
        /// </summary>
        public Dictionary<TKey, TState> ActiveStates { get; set; }

        public List<Tuple<TState, TState>> ActiveTransitions { get; set; }

        /// <summary>
        /// Transition between two states
        /// </summary>
        /// <param name="Current">The current state to move from. If null/default, immediately transitions to <see cref="Destination"/></param>
        /// <param name="Destination">The state to transition to. If null/default, removes <see cref="Current"/></param>
        /// <remarks>If both Current and Destination are both not found, nothing happens</remarks>
        public void MoveTo(TKey Current, TKey Destination)
        {
            if (ActiveStates.ContainsKey(Current))
            {
            }
            else
            {

            }
        }
    }
}

//todo: automatic transfer between states
//todo: full transition states necessary?