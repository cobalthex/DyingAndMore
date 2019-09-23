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
        None           = 0b0000,
        RequiresTarget = 0b0001,
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
        public struct PrioritizedBehavior
        {
            public BehaviorPriority priority;
            public Behavior behavior;

            public PrioritizedBehavior(BehaviorPriority priority, Behavior behavior)
            {
                this.priority = priority;
                this.behavior = behavior;
            }
        }

        public List<Behavior> Behaviors { get; set; }

        List<PrioritizedBehavior>[] behaviorCosts
            = new List<PrioritizedBehavior>[(int)BehaviorMask._Count_];

        [Takai.Data.Serializer.Ignored]
        public Behavior[] ChosenBehaviors { get; set; } = new Behavior[(int)BehaviorMask._Count_];

        [Takai.Data.Serializer.AsReference]
        public ActorInstance Target
        {
            get => _target;
            set => SetNextTarget(value);
        }

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
            for (int i = 0; i < behaviorCosts.Length; ++i)
                behaviorCosts[i] = new List<PrioritizedBehavior>();
        }

        public override void Think(TimeSpan deltaTime)
        {
            if (GameInstance.Current != null && !GameInstance.Current.GameplaySettings.isAiEnabled)
                return;

            if (isNextTargetSet)
            {
                _target = nextTarget;
                TargetTime = Actor.Map?.ElapsedTime ?? TimeSpan.Zero;
                isNextTargetSet = false;
            }

            var filters = BehaviorFilters.None;
            if (Target != null)
                filters |= BehaviorFilters.RequiresTarget;

            foreach (var behavior in Behaviors)
            {
                behavior.AI = this;

                var mask = (int)behavior.Mask;

                var behaviorFilters = behavior.Filter;
                if ((filters & behaviorFilters) != behaviorFilters)
                    continue;

                var priority = behavior.CalculatePriority();
                if (priority == BehaviorPriority.Never)
                    continue;

                if (behaviorCosts[mask].Count > 0)
                {
                    if (priority < behaviorCosts[mask][0].priority)
                        continue;

                    if (priority > behaviorCosts[mask][0].priority)
                        behaviorCosts[mask].Clear();
                }
                behaviorCosts[mask].Add(new PrioritizedBehavior(priority, behavior));
            }

            for (int i = 0; i < behaviorCosts.Length; ++i)
            {
                if (behaviorCosts[i].Count > 0)
                {
                    var choice = Util.RandomGenerator.Next(0, behaviorCosts[i].Count);
                    ChosenBehaviors[i] = behaviorCosts[i][choice].behavior;
                    ChosenBehaviors[i].Think(deltaTime);
                    behaviorCosts[i].Clear();
                }
            }
        }
    }
}


/*
 * target finding based on origins for flow field
 * navigate using flow field
 * navigate using A*
*/