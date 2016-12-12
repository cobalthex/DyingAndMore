using System;
using System.Collections.Generic;

namespace Takai
{
    /// <summary>
    /// Forms the base of any state in the state machine
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Should this state stay active even after finishing
        /// </summary>
        bool IsLooping { get; set; }

        /// <summary>
        /// Restart the animation (Called automatically if IsLooping = true and HasFinished() = true)
        /// </summary>
        void Start();

        /// <summary>
        /// Is the state ready to transition (Should ignore internal loop settings)
        /// </summary>
        /// <returns>True if the state is finished playing</returns>
        bool HasFinished();

        /// <summary>
        /// Update the state
        /// </summary>
        /// <param name="Time">Time since the last update</param>
        void Update(TimeSpan DeltaTime);
    }

    /// <summary>
    /// A state machine that allows multiple active states (blending) and handles transitioning between states
    /// </summary>
    /// <typeparam name="TKey">The Identifier identifying each state</typeparam>
    /// <typeparam name="TState">The individual states</typeparam>
    public class StateMachine<TKey, TState> where TState : IState
    {
        /// <summary>
        /// All of the available states 
        /// </summary>
        public Dictionary<TKey, TState> States { get; set; } = new Dictionary<TKey, TState>(); //todo: check not null

        /// <summary>
        /// THe currently active states, and cached state values
        /// </summary>
        public HashSet<TKey> ActiveStates { get; set; } = new HashSet<TKey>(); //todo: make private w/ public enumerator

        /// <summary>
        /// Active transitions between states
        /// </summary>
        /// <remarks>Transitions are removed after they finish</remarks>
        protected Dictionary<TKey, TKey> Transitions { get; set; } = new Dictionary<TKey, TKey>();

        /// <summary>
        /// Add a state and optionally make it active
        /// </summary>
        /// <param name="Key">The state key</param>
        /// <param name="State">The state</param>
        /// <param name="AddToActive">Add the new state to the active set of states</param>
        /// <remarks>Overwrites any existing state w/ Key</remarks>
        public void AddState(TKey Key, TState State, bool AddToActive = false)
        {
            States[Key] = State;
            if (AddToActive)
                ActiveStates.Add(Key);
        }

        /// <summary>
        /// A helper method to check if a state is active
        /// </summary>
        /// <param name="State">The state to check</param>
        /// <returns>True if the state is currently active</returns>
        public bool HasActive(TKey State)
        {
            return ActiveStates.Contains(State);
        }
        
        /// <summary>
        /// Transition from one state to another
        /// </summary>
        /// <param name="CurrentState">The current state to transition from. If Default(TKey) then NextState is added immediately</param>
        /// <param name="NextState">The next state to transition to. If Default(TKey) then CurrentState will be removed when it is finished playing</param>
        /// <returns>False if the states did not exist in the state machine</returns>
        public virtual bool Transition(TKey CurrentState, TKey NextState)
        {
            if (CurrentState != null)
            {
                Transitions.Add(CurrentState, NextState);
                return true;
            }
            if (NextState != null && States.TryGetValue(NextState, out var next))
            {
                next.Start();
                ActiveStates.Add(NextState);
            }
            return false;
        }

        List<TKey> added = new List<TKey>();
        List<TKey> removed = new List<TKey>();

        /// <summary>
        /// Update all of the animations 
        /// </summary>
        /// <param name="DeltaTime">How much time has passed since the last update</param>
        public virtual void Update(System.TimeSpan DeltaTime)
        {
            added.Clear();
            removed.Clear();

            foreach (var key in ActiveStates)
            {
                var state = States[key];
                state.Update(DeltaTime);
                
                if (state.HasFinished())
                {
                    if (Transitions.TryGetValue(key, out var transition))
                    {
                        removed.Add(key);
                        if (transition != null)
                            added.Add(transition);
                        Transitions.Remove(key);
                    }
                    else if (state.IsLooping)
                        state.Start();
                    else
                        removed.Add(key);
                }
            }

            foreach (var key in added)
            {
                States[key].Start();
                ActiveStates.Add(key);
            }
            foreach (var key in removed)
                ActiveStates.Remove(key);
        }
    }
}