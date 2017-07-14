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
        TKey StateId { get; set; }

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
        public Dictionary<TKey, TClass> States { get; set; }

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
        /// <summary>
        /// Check if a state is active
        /// </summary>
        /// <param name="activeState">The state to check</param>
        /// <returns>True if the state is currently active</returns>
        public bool Is(TKey activeState)
        {
            return (BaseState.StateId.Equals(activeState) || OverlaidStates.ContainsKey(activeState));
        }

        /// <summary>
        /// Check if a state is active and return it
        /// </summary>
        /// <param name="activeState">the state to check</param>
        /// <returns>true if the state was found</returns>
        public bool TryGet(TKey activeState, out TInstance instance)
        {
            if (BaseState.Equals(activeState))
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
        public virtual bool Transition(TKey NextState)
        {
            if (!NextState.Equals(default(TKey)) && States.TryGetValue(NextState, out var next))
            {
                var instance = (TInstance)next.Create();
                instance.StateId = NextState; //todo: automate

                if (next.IsOverlay)
                    OverlaidStates.Add(NextState, instance);
                else
                    BaseState = instance;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Transition from one state to another
        /// </summary>
        /// <param name="currentState">The state to transition from when it is finished</param>
        /// <param name="nextState">The state to transition to after currentState is finished/></param>
        public virtual void Transition(TKey currentState, TKey nextState)
        {
            Transitions[currentState] = nextState;
        }

        List<TKey> added = new List<TKey>();
        List<TKey> removed = new List<TKey>();

        /// <summary>
        /// Update all of the animations
        /// </summary>
        /// <param name="deltaTime">How much time has passed since the last update</param>
        public virtual void Update(TimeSpan deltaTime)
        {
            //foreach (var key in added)
            //{
            //    var instance = (TInstance)States[key].Create();
            //    instance.StateId = key;
            //    if (States[key].IsOverlay)
            //        OverlaidStates.Add(key, instance);
            //}

            foreach (var state in OverlaidStates)
            {
                if (Transitions.TryGetValue(state.Key, out var nextOverlay))
                {
                    Transitions.Remove(state.Key);
                    added.Add(nextOverlay);
                    removed.Add(state.Key);
                }
                else
                {
                    state.Value.ElapsedTime += deltaTime;
                    state.Value.Update(deltaTime);

                    if (state.Value.ElapsedTime > state.Value.Class.TotalTime)
                        removed.Add(state.Key);
                }
            }

            if (Transitions.TryGetValue(BaseState.StateId, out var nextBase))
            {
                Transitions.Remove(BaseState.StateId);
                BaseState = (TInstance)States[nextBase].Create();
                BaseState.StateId = nextBase;
            }
            else
            {
                BaseState.ElapsedTime += deltaTime;
                BaseState.Update(deltaTime);
            }

            foreach (var key in added)
            {
                var instance = (TInstance)States[key].Create();
                instance.StateId = key;
                OverlaidStates.Add(key, instance);
            }
            foreach (var key in removed)
                OverlaidStates.Remove(key);

            //update overlays
            added.Clear();
            removed.Clear();
        }

        public override string ToString()
        {
            return $"{BaseState};{String.Join(", ", OverlaidStates)}";
        }
    }
}