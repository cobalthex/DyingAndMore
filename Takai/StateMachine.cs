using System;
using System.Collections.Generic;

namespace Takai
{
    /// <summary>
    /// Forms the base of any state in the state machine
    /// </summary>
    public interface IState : ICloneable
    {
        /// <summary>
        /// Should this state stay active even after finishing
        /// </summary>
        bool IsLooping { get; set; }

        /// <summary>
        /// If false, this state replaces the active state, otherwise it is added to a list with all other overlay states
        /// </summary>
        bool IsOverlay { get; set; }

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
    public class StateMachine<TKey, TState> : ICloneable where TState : IState
    {
        /// <summary>
        /// All of the available states
        /// </summary>
        public Dictionary<TKey, TState> States { get; set; } = new Dictionary<TKey, TState>();

        /// <summary>
        /// The currently active 'base' state. Only one base state can be active at a time
        /// </summary>
        public TKey BaseState { get; set; }

        /// <summary>
        /// THe currently active overlaid states
        /// </summary>
        public HashSet<TKey> OverlaidStates { get; set; } = new HashSet<TKey>(); //todo: make private w/ public enumerator

        /// <summary>
        /// Active transitions between states
        /// </summary>
        /// <remarks>Transitions are removed after they finish</remarks>
        protected Dictionary<TKey, TKey> Transitions { get; set; } = new Dictionary<TKey, TKey>();

        protected static readonly EqualityComparer<TKey> EqComparer = EqualityComparer<TKey>.Default;

        public virtual object Clone()
        {
            var cloned = (StateMachine<TKey, TState>)MemberwiseClone();
            cloned.OverlaidStates = new HashSet<TKey>(OverlaidStates);
            cloned.Transitions = new Dictionary<TKey, TKey>(Transitions);
            cloned.States = new Dictionary<TKey, TState>(States.Count);
            foreach (var kvp in States)
                cloned.States.Add(kvp.Key, (TState)kvp.Value.Clone());
            return cloned;
        }

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
                Transition(default(TKey), Key);
        }

        /// <summary>
        /// Check if a state is active
        /// </summary>
        /// <param name="State">The state to check</param>
        /// <returns>True if the state is currently active</returns>
        public bool Is(TKey State)
        {
            return (EqComparer.Equals(BaseState, State) || OverlaidStates.Contains(State));
        }

        /// <summary>
        /// Transition immediately to a state
        /// </summary>
        /// <param name="NextState">The new state</param>
        /// <returns>False if the state does not exist</returns>
        public virtual bool Transition(TKey NextState)
        {
            if (!EqComparer.Equals(NextState, default(TKey)) && States.TryGetValue(NextState, out var next))
            {
                next.Start();
                if (next.IsOverlay)
                    OverlaidStates.Add(NextState);
                else
                    BaseState = NextState;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Transition from one state to another
        /// </summary>
        /// <param name="CurrentState">The current state to transition from. If Default(TKey) then NextState is added immediately</param>
        /// <param name="NextState">The next state to transition to. If Default(TKey) then CurrentState will be removed when it is finished playing</param>
        /// <returns>False if the states did not exist in the state machine</returns>
        public virtual bool Transition(TKey CurrentState, TKey NextState)
        {
            if (!EqComparer.Equals(CurrentState, default(TKey)))
            {
                Transitions[CurrentState] = NextState;
                return true;
            }
            if (!EqComparer.Equals(NextState, default(TKey)) && States.TryGetValue(NextState, out var next))
            {
                next.Start();
                if (next.IsOverlay)
                    OverlaidStates.Add(NextState);
                else
                    BaseState = NextState;
                return true;
            }
            return false;
        }

        List<TKey> added = new List<TKey>();
        List<TKey> removed = new List<TKey>();

        void UpdateState(TKey stateKey, TimeSpan deltaTime)
        {
            var state = States[stateKey];
            state.Update(deltaTime);

            if (state.HasFinished())
            {
                if (Transitions.TryGetValue(stateKey, out var transition))
                {
                    if (EqComparer.Equals(transition, default(TKey)))
                        BaseState = default(TKey);
                    else
                        BaseState = transition;
                    Transitions.Remove(stateKey);
                }
            }
        }

        /// <summary>
        /// Update all of the animations
        /// </summary>
        /// <param name="deltaTime">How much time has passed since the last update</param>
        public virtual void Update(TimeSpan deltaTime)
        {
            //update active state
            UpdateState(BaseState, deltaTime);

            //update overlays
            added.Clear();
            removed.Clear();

            foreach (var key in OverlaidStates)
            {
                UpdateState(key, deltaTime);
            }

            foreach (var key in added)
            {
                States[key].Start();
                OverlaidStates.Add(key);
            }
            foreach (var key in removed)
                OverlaidStates.Remove(key);
        }

        public override string ToString()
        {
            return $"{BaseState};{String.Join(", ", OverlaidStates)}";
        }
    }
}