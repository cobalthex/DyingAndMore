using System;
using System.Collections.Generic;

namespace Takai
{
    /// <summary>
    /// Forms the base of any state in the state machine
    /// </summary>
    public interface IStateClass<TKey, TInstance> : IObjectClass<TInstance>
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
        /// The total length of this state
        /// </summary>
        TimeSpan TotalTime { get; set; }
    }

    public interface IStateInstance<TKey, TClass> : IObjectInstance<TClass>
    {
        /// <summary>
        /// Unique ID for the state
        /// </summary>
        TKey Id { get; set; }

        /// <summary>
        /// How long this state has been active
        /// </summary>
        TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// Update the current state (<see cref="ElapsedTime" updated separately)/>
        /// </summary>
        /// <param name="deltaTime">time since last frame</param>
        void Update(TimeSpan deltaTime);
    }

    public class StateCompleteEventArgs<TInstance> : EventArgs
    {
        public TInstance State { get; set; }
    }

    public class TransitionEventArgs<TInstance> : EventArgs
    {
        public TInstance Previous { get; set; }
        public TInstance Next { get; set; }
    }

    /// <summary>
    /// A state machine that allows multiple active states (blending) and handles transitioning between states
    /// </summary>
    /// <typeparam name="TKey">The Identifier identifying each state</typeparam>
    public class StateMachine<TKey, TClass, TInstance>
        where TClass : IStateClass<TKey, TInstance>
        where TInstance : IStateInstance<TKey, TClass>
    {
        /// <summary>
        /// All of the available states
        /// </summary>
        public Dictionary<TKey, TClass> States
        {
            get => _states;
            set
            {
                _states = value;
                if (BaseState != null)
                    BaseState.Class = _states[BaseState.Id];
                foreach (var state in OverlaidStates)
                    state.Value.Class = _states[state.Value.Id];
            }
        }
        private Dictionary<TKey, TClass> _states;

        /// <summary>
        /// The currently active 'base' state. Only one base state can be active at a time
        /// </summary>
        public TInstance BaseState { get; set; }

        /// <summary>
        /// THe currently active overlaid states
        /// </summary>
        public Dictionary<TKey, TInstance> OverlaidStates { get; set; }
            = new Dictionary<TKey, TInstance>(); //todo: readonly?

        /// <summary>
        /// All of the current transitions
        /// </summary>
        public Dictionary<TKey, TKey> Transitions { get; set; } = new Dictionary<TKey, TKey>();

        public event EventHandler<StateCompleteEventArgs<TInstance>> StateComplete = null;
        protected virtual void OnStateComplete(StateCompleteEventArgs<TInstance> e) { }

        public event EventHandler<TransitionEventArgs<TInstance>> Transition = null;
        protected virtual void OnTransition(TransitionEventArgs<TInstance> e) { }

        /// <summary>
        /// Check if a state is active
        /// </summary>
        /// <param name="activeState">The state to check</param>
        /// <returns>True if the state is currently active</returns>
        public bool Is(TKey activeState)
        {
            return (BaseState.Id.Equals(activeState) || OverlaidStates.ContainsKey(activeState));
        }

        /// <summary>
        /// Check if a state is active and return it
        /// </summary>
        /// <param name="activeState">the state to check</param>
        /// <returns>true if the state was found</returns>
        public bool TryGet(TKey activeState, out TInstance instance)
        {
            if (BaseState.Id.Equals(activeState))
            {
                instance = BaseState;
                return true;
            }
            if (OverlaidStates.TryGetValue(activeState, out var state))
            {
                instance = state;
                return true;
            }

            instance = default(TInstance);
            return false;
        }

        /// <summary>
        /// Transition immediately to a state
        /// </summary>
        /// <param name="NextState">The new state</param>
        /// <returns>False if the state does not exist</returns>
        public virtual bool TransitionTo(TKey NextState)
        {
            if (!NextState.Equals(default(TKey)) && States.TryGetValue(NextState, out var next))
            {
                var instance = (TInstance)next.Create();
                instance.Id = NextState; //todo: automate

                var evArgs = new TransitionEventArgs<TInstance>()
                {
                    Next = instance
                };

                if (next.IsOverlay)
                    OverlaidStates.Add(NextState, instance);
                else
                {
                    evArgs.Previous = BaseState;
                    BaseState = instance;
                }

                OnTransition(evArgs);
                Transition?.Invoke(this, evArgs);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Transition from one state to another
        /// </summary>
        /// <param name="currentState">The state to transition from when it is finished</param>
        /// <param name="nextState">The state to transition to after currentState is finished/></param>
        public virtual void TransitionTo(TKey currentState, TKey nextState)
        {
            Transitions[currentState] = nextState;
        }

        List<TInstance> added = new List<TInstance>();
        List<TKey> removed = new List<TKey>();

        /// <summary>
        /// Update all of the animations
        /// </summary>
        /// <param name="deltaTime">How much time has passed since the last update</param>
        public virtual void Update(TimeSpan deltaTime)
        {
            if (Transitions.TryGetValue(BaseState.Id, out var nextBase))
            {
                Transitions.Remove(BaseState.Id);

                var evArgs = new TransitionEventArgs<TInstance>()
                {
                    Previous = BaseState
                };

                evArgs.Next = BaseState = (TInstance)States[nextBase].Create();
                BaseState.Id = nextBase;

                OnTransition(evArgs);
                Transition?.Invoke(this, evArgs);
            }
            else
            {
                bool notComplete = BaseState.ElapsedTime < BaseState.Class.TotalTime;

                BaseState.ElapsedTime += deltaTime;
                BaseState.Update(deltaTime);

                if (!notComplete && BaseState.ElapsedTime >= BaseState.Class.TotalTime)
                {
                    var completed = new StateCompleteEventArgs<TInstance>()
                    {
                        State = BaseState
                    };
                    OnStateComplete(completed);
                    StateComplete?.Invoke(this, completed);
                }
            }

            foreach (var state in OverlaidStates)
            {
                if (Transitions.TryGetValue(state.Key, out var nextOverlay))
                {
                    Transitions.Remove(state.Key);
                    removed.Add(state.Key);

                    var next = States[nextOverlay].Create();
                    added.Add(next);

                    var evArgs = new TransitionEventArgs<TInstance>()
                    {
                        Previous = state.Value,
                        Next = next
                    };
                    OnTransition(evArgs);
                    Transition?.Invoke(this, evArgs);
                }
                else
                {
                    bool notComplete = state.Value.ElapsedTime < state.Value.Class.TotalTime;

                    state.Value.ElapsedTime += deltaTime;
                    state.Value.Update(deltaTime);

                    if (notComplete && state.Value.ElapsedTime >= state.Value.Class.TotalTime)
                    {
                        var completed = new StateCompleteEventArgs<TInstance>()
                        {
                            State = state.Value
                        };
                        OnStateComplete(completed);
                        StateComplete?.Invoke(this, completed);
                    }

                    if (state.Value.ElapsedTime > state.Value.Class.TotalTime)
                        removed.Add(state.Key);
                }
            }

            //udpate overlays
            foreach (var inst in added)
                OverlaidStates.Add(inst.Id, inst);
            foreach (var key in removed)
                OverlaidStates.Remove(key);

            added.Clear();
            removed.Clear();
        }

        public override string ToString()
        {
            return $"{BaseState};{String.Join(", ", OverlaidStates)}";
        }
    }
}