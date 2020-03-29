using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai;

namespace DyingAndMore.Game.Entities
{
    //behavior constraints (cant run while dead/etc)

    public enum BehaviorMask
    {
        Unknown,
        Targeting,
        Movement,
        Weapons,

        _Count_
        //Exclusive option?
        //bitmask?
    }

    public enum BehaviorPriority : int
    {
        Never   = int.MinValue,
        Low     = -100,
        Normal  = 0,
        High    = 100,
    }

    public enum BehaviorFilters : uint
    {
        None               = 0b00000000,
        RequiresTarget     = 0b00000001,
        RequiresParent     = 0b00010000,
    }

    public abstract class Behavior
    {
        [Takai.Data.Serializer.Ignored]
        public AIController AI { get; set; }

        /// <summary>
        /// Only one of each behavior mask can be executed at a time
        /// </summary>
        [Takai.Data.Serializer.Ignored]
        public abstract BehaviorMask Mask { get; }

        /// <summary>
        /// Automatically restrict this behavior's application
        /// </summary>
        [Takai.Data.Serializer.Ignored]
        public virtual BehaviorFilters Filter => BehaviorFilters.None;

        public Range<TimeSpan> ActiveTime { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Calculate the priority of this action. Return <see cref="BehaviorPriority.Never"/> to disregard
        /// The behavior with the highest priority will be chosen
        /// </summary>
        /// <returns>The priority factor of this action</returns>
        public abstract BehaviorPriority CalculatePriority();

        public abstract void Think(TimeSpan deltaTime);

        public override string ToString()
        {
            return GetType().Name;
        }
    }

    public class AIController : Controller
    {
        protected struct ChosenBehavior
        {
            public Behavior behavior;
            public BehaviorPriority priority;
            public TimeSpan endTime;
        }

        public List<Behavior> Behaviors { get; set; }

        [Takai.Data.Serializer.Ignored]
        protected ChosenBehavior[] chosenBehaviors = new ChosenBehavior[(int)BehaviorMask._Count_];

        [Takai.Data.Serializer.AsReference]
        public ActorInstance Target
        {
            get => _target;
            set => SetNextTarget(value);
        }

        public uint TargetFlowSeekValue { get; set; } = 0;

        private ActorInstance _target, nextTarget;
        private bool isNextTargetSet;

        public TimeSpan TargetTime { get; set; }

        public Vector2 LastKnownTargetPosition { get; set; }

        public void SetNextTarget(ActorInstance actor)
        {
            nextTarget = actor;
            isNextTargetSet = true;
        }

        public AIController()
        {
        }

        public override string ToString()
        {
            return $"{base.ToString()} ({(Behaviors == null ? "no behaviors" : string.Join(",", Behaviors))})";
        }
        public override void Think(TimeSpan deltaTime)
        {
            if ((GameInstance.Current != null && !GameInstance.Current.GameplaySettings.isAiEnabled) ||
                Actor.Map == null) //this shouldnt be necessary
                return;

            if (isNextTargetSet)
            {
                _target = nextTarget;
                TargetTime = Actor.Map.ElapsedTime;
                isNextTargetSet = false;

                if (_target == null)
                {
                    for (int i = 0; i < chosenBehaviors.Length; ++i)
                    {
                        if (chosenBehaviors[i].behavior != null &&
                            (chosenBehaviors[i].behavior.Filter & BehaviorFilters.RequiresTarget) == 0)
                            chosenBehaviors[i].behavior = null;
                        System.Diagnostics.Debug.WriteLine("@" + chosenBehaviors[i].behavior?.GetType().Name);
                    } //todo: repeat for any filter
                }
            }

            var filters = BehaviorFilters.None;
            if (Target != null)
                filters |= BehaviorFilters.RequiresTarget;
            if (Actor.WorldParent != null)
                filters |= BehaviorFilters.RequiresParent;

            foreach (var behavior in Behaviors) //allow higher priority behaviors to co-opt
            {
                behavior.AI = this;

                var mask = (int)behavior.Mask;

                var behaviorFilters = behavior.Filter;
                if ((filters & behaviorFilters) != behaviorFilters)
                    continue;

                var priority = behavior.CalculatePriority();
                if (priority == BehaviorPriority.Never)
                    continue;

                if (chosenBehaviors[mask].behavior != null && priority <= chosenBehaviors[mask].priority)
                    continue;

                if (Util.RandomGenerator.Next(0, 10) < 4) //todo: make this make sense
                {
                    chosenBehaviors[mask] = new ChosenBehavior
                    {
                        behavior = behavior,
                        priority = priority,
                        endTime = Actor.Map.ElapsedTime + behavior.ActiveTime.Random()
                    };
                }
            }

            for (int i = 0; i < chosenBehaviors.Length; ++i)
            {
                if (chosenBehaviors[i].behavior == null)
                    continue;

                if (Actor.Map.ElapsedTime >= chosenBehaviors[i].endTime)
                {
                    chosenBehaviors[i].behavior = null;
                    continue;
                }

                //todo: this shouldnt be necessary
                var behaviorFilters = chosenBehaviors[i].behavior.Filter;
                if ((filters & behaviorFilters) != behaviorFilters)
                    continue;
                chosenBehaviors[i].behavior.Think(deltaTime);
            }
        }
    }
}