﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    /// <summary>
    /// All of the possible entity states
    /// </summary>
    public enum EntStateId
    {
        Invalid,
        Dead,
        Idle,
        Inactive,
        Active,

        //game specific (come up with better way?)

        ChargeWeapon = 128,
        DischargeWeapon
    }

    /// <summary>
    /// Forms the base of any state in the state machine
    /// </summary>
    public class EntStateClass : IObjectClass<EntStateInstance>
    {
        [Data.Serializer.Ignored]
        public string File { get; set; } = null;

        /// <summary>
        /// A unique name for this state
        /// </summary>
        public string Name { get; set; }

        public Graphics.Sprite Sprite { get; set; }

        /// <summary>
        /// Looping state sound
        /// </summary>
        public SoundClass Sound { get; set; }

        /// <summary>
        /// An effect to play (repeatedly) while the state is active
        /// </summary>
        public EffectsClass Effect { get; set; }
        //todo: enter/exit effects?

        public float Radius =>
            Sprite != null ? MathHelper.Max(Sprite.Width, Sprite.Height) / 2 : 1;

        [Data.Serializer.Ignored]
        public bool IsLooping
        {
            get
            {
                return Sprite?.IsLooping ?? false;
            }
            set
            {
                Sprite.IsLooping = value;
            }
        }

        [Data.Serializer.Ignored]
        public TimeSpan TotalTime => Sprite?.TotalLength ?? TimeSpan.Zero;

        public EntStateInstance Create()
        {
            return new EntStateInstance()
            {
                Class = this,
                ElapsedTime = TimeSpan.Zero
            };
        }
    }

    public struct EntStateInstance : IObjectInstance<EntStateClass>
    {
        /// <summary>
        /// The state that this instance was created with. May be different from the current state
        /// </summary>
        public EntStateId Id { get; set; }

        public EntStateClass Class
        {
            get => _class;
            set
            {
                _class = value;

                if (_class != null)
                {
                    Sound.Instance?.Dispose();
                    if (_class.Sound != null)
                    {
                        Sound = _class.Sound.Create();
                        if (Sound.Instance != null)
                        {
                            Sound.Instance.IsLooped = _class.IsLooping;
                            Sound.Instance.Play();
                        }
                    }
                }
            }
        }
        private EntStateClass _class;

        [Data.Serializer.Ignored]
        public SoundInstance Sound { get; set; }

        /// <summary>
        /// The current elapsed time of this state
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        public bool HasFinished()
        {
            return ElapsedTime >= Class.TotalTime;
        }

        public override int GetHashCode()
        {
            return (int)Id;
        }

        public void Update(TimeSpan deltaTime)
        {

        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }

    public class StateCompleteEventArgs : EventArgs
    {
        public EntStateId State { get; set; }
        public EntStateInstance Instance { get; set; }
    }

    public class TransitionEventArgs : EventArgs
    {
        public EntStateId PreviousState { get; set; }
        public EntStateInstance PreviousInstance { get; set; }
        public EntStateId NextState { get; set; }
        public EntStateInstance NextInstance { get; set; }
    }

    /// <summary>
    /// The state machine for entities. Allows blending, transitions, and events
    /// </summary>
    public class EntityStateMachine
    {
        /// <summary>
        /// All of the available states
        /// </summary>
        [Data.Serializer.Ignored]
        public Dictionary<string, EntStateClass> States
        {
            get => _states;
            set
            {
                _states = value;

                if (value == null)
                    return;

                foreach (var state in _states)
                {
                    if (state.Value.Name == null)
                        state.Value.Name = state.Key;
                }

                if (instance.Class?.Name != null)
                    instance.Class = _states[instance.Class.Name];
            }
        }
        private Dictionary<string, EntStateClass> _states;

        //todo: come up with better name for state/instance

        /// <summary>
        /// The actual state set. The current instance may not match if there was no accompanying class
        /// </summary>
        public EntStateId State { get; set; }
        /// <summary>
        /// The currently active state instance. <see cref="State"/> for notes
        /// </summary>
        [Data.CustomSerialize("SerializeInstance")]
        public EntStateInstance instance;

        private object SerializeInstance()
        {
            return instance.Class.Name;
        }

        /// <summary>
        /// All of the current transitions
        /// </summary>
        public Dictionary<EntStateId, (EntStateId Id, string name)> Transitions { get; set; }
            = new Dictionary<EntStateId, (EntStateId, string)>();

        public event EventHandler<StateCompleteEventArgs> StateComplete;
        protected virtual void OnStateComplete(StateCompleteEventArgs e) { }

        public event EventHandler<TransitionEventArgs> Transition;
        protected virtual void OnTransition(TransitionEventArgs e) { }

        /// <summary>
        /// Transition to a state immediately
        /// </summary>
        /// <param name="nextState">The state key</param>
        /// <param name="nextClass">The animation to use</param>
        /// <returns></returns>
        public EntStateInstance TransitionTo(EntStateId nextState, string nextClass)
        {
            State = nextState;
            if (States != null && States.TryGetValue(nextClass, out var stateClass))
            {
                instance.Sound.Instance?.Dispose();
                instance = stateClass.Create();
                instance.Id = nextState;
                return instance;
            }
            return default(EntStateInstance);
        }

        /// <summary>
        /// Transition from one state to another
        /// </summary>
        /// <param name="currentState">The state to transition from when it is finished</param>
        /// <param name="nextState">The state to transition to after currentState is finished/></param>
        /// <param name="immediate">If true, the current state is swapped with the next state</param>
        public virtual void TransitionTo(EntStateId currentState, EntStateId nextState,
                                         string nextClass)
        {
            Transitions[currentState] = (nextState, nextClass);
        }

        /// <summary>
        /// Update the current state and handle transitions
        /// </summary>
        /// <param name="deltaTime">How much time has passed since the last update</param>
        public virtual void Update(TimeSpan deltaTime)
        {
            bool wasNotFinished = !instance.HasFinished();

            instance.ElapsedTime += deltaTime;
            instance.Update(deltaTime);

            bool hasFinished = !wasNotFinished && instance.HasFinished();
            if (hasFinished)
            {
                var completed = new StateCompleteEventArgs()
                {
                    State = State,
                    Instance = instance
                };
                OnStateComplete(completed);
                StateComplete?.Invoke(this, completed);
            }

            if ((instance.Id != State || hasFinished) &&
                Transitions.TryGetValue(State, out var next))
            {
                Transitions.Remove(State);

                var evArgs = new TransitionEventArgs()
                {
                    PreviousState = State,
                    PreviousInstance = instance
                };

                evArgs.NextState = State = next.Id;
                if (States.TryGetValue(next.name, out var stateClass))
                {
                    evArgs.NextInstance = instance = stateClass.Create();
                    instance.Id = next.Id;
                }
                OnTransition(evArgs);
                Transition?.Invoke(this, evArgs);
            }
        }

        public override string ToString()
        {
            return $"{State} ({instance.Class.Name ?? instance.Id.ToString()})";
        }
    }
}