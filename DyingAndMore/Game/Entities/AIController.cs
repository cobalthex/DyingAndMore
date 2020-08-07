using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Takai;

namespace DyingAndMore.Game.Entities
{
    [Flags]
    public enum Senses
    {
        None            = 0b0000000000000000,
        FullHealth      = 0b0000000000000001,
        LowHealth       = 0b0000000000000010,
        DamageTaken     = 0b0000000000000100,
        LowAmmo         = 0b0000000000001000,
        HasTarget       = 0b0000000000010000,
        TargetVisible   = 0b0000000000100000, //+ HasTarget ?,
        TargetCanSeeMe  = 0b0000000001000000, //+ HasTarget ?, //rename?
        Supremecy       = 0b0000000010000000, //more allies than enemies
        Outnumbered     = 0b0000000100000000, //more enemies than allies
        AllyDied        = 0b0000001000000000,
        EnemyDied       = 0b0000010000000000,
        AllyNearby      = 0b0000100000000000, // >= 1
        EnemyNearby     = 0b0001000000000000, // >= 1
        Attached        = 0b0010000000000000,
        //target close/far
        //target fleeing?
        //target out of range
    }

    public class AIController : Controller
    {
        public float SightRange { get; set; } = 400;

        /// <summary>
        /// The current target actor for behaviors (e.g. enemy to shoot at or ally to follow)
        /// </summary>
        [Takai.Data.Serializer.AsReference]
        public ActorInstance Target { get; set; }

        public TimeSpan TargetLastSeenTime { get; protected set; }
        public Vector2 TargetLastSeenPosition { get; protected set; }

        /// <summary>
        /// Possible behaviors to start this AI with
        /// </summary>
        public List<Behavior> DefaultBehaviors { get; set; }
        /// <summary>
        /// Behaviors that can pre-empt the current behavior if they have matching senses
        /// (one is picked randomly and tested)
        /// <seealso cref="nextSenseCheck"/>
        /// </summary>
        public List<Behavior> PreemptiveBehaviors { get; set; } //merge with default?

        //todo
        public Dictionary<ActorBroadcast, Behavior> AllyBroadcasts { get; set; } //list of behaviors?

        public Dictionary<ActorBroadcast, Behavior> EnemyBroadcasts { get; set; } //list of behaviors?

        /// <summary>
        /// The current 
        /// </summary>
        public Behavior CurrentBehavior
        {
            get => _currentBehavior;
            set
            {
                _currentBehavior = value;
                CurrentTask = 0;
                CurrentTaskStartTime = Actor?.Map?.ElapsedTime ?? TimeSpan.Zero;
            }
        }
        private Behavior _currentBehavior;
        /// <summary>
        /// The current task running in the <see cref="CurrentBehavior"/>
        /// </summary>
        public int CurrentTask { get; set; }

        public TimeSpan CurrentTaskStartTime { get; private set; }

        public Senses KnownSenses { get; private set; }

        /// <summary>
        /// when to next check preemptive behaviors
        /// </summary>
        TimeSpan nextSenseCheck;

        public void Reset()
        {
            CurrentBehavior = null;
            KnownSenses = 0;
            Target = null;
            nextSenseCheck = TimeSpan.Zero;
            //part of ICloneable?
        }

        public override string ToString()
        {
            if (CurrentBehavior == null)
                return "(No active behavior)";

            var sb = new System.Text.StringBuilder();
            sb.Append(CurrentBehavior.Name);
            if (CurrentTask < CurrentBehavior.Tasks.Count)
            {
                sb.Append(": ");
                sb.Append(CurrentBehavior.Tasks[CurrentTask].GetType().Name);
                //sb.Append("\n(");
                //sb.Append(KnownSenses);
                //sb.Append(")");
            }
            return sb.ToString();
        }

        public override void Think(TimeSpan deltaTime)
        {
            if (GameInstance.Current != null && !GameInstance.Current.GameplaySettings.isAiEnabled)
                return;

            // run every X frames?
            //remove target on its death?
            //reactions to events

            KnownSenses = 0;

            //minimal runtime per behavior?
            if (Actor.Map.ElapsedTime >= nextSenseCheck && (PreemptiveBehaviors != null && PreemptiveBehaviors.Count > 0))
            {
                var behavior = PreemptiveBehaviors[Util.RandomGenerator.Next(0, PreemptiveBehaviors.Count)];
                KnownSenses |= BuildSenses((behavior.RequisiteSenses | behavior.RequisiteNotSenses) & ~KnownSenses);
                if ((behavior.RequisiteSenses & KnownSenses) == behavior.RequisiteSenses &&
                    (behavior.RequisiteNotSenses & KnownSenses) == 0 &&
                    (float)Util.RandomGenerator.NextDouble() < behavior.QueueChance)
                    CurrentBehavior = behavior;

                //programmable delay?
                nextSenseCheck = Actor.Map.ElapsedTime + TimeSpan.FromMilliseconds(200);
            }

            if (CurrentBehavior != null && CurrentTask < CurrentBehavior.Tasks.Count)
            {
                var result = CurrentBehavior.Tasks[CurrentTask].Think(deltaTime, this);
                if (result == Tasks.TaskResult.Failure)
                {
                    switch (CurrentBehavior.OnTaskFailure)
                    {
                        case TaskFailureAction.RestartBehavior:
                            CurrentTask = 0;
                            break;
                        case TaskFailureAction.CancelBehavior:
                            CurrentBehavior = null;
                            break;
                        case TaskFailureAction.RetryTask:
                            break;
                        case TaskFailureAction.Ignore:
                            ++CurrentTask;
                            break;
                    }
                    CurrentTaskStartTime = Actor.Map.ElapsedTime;
                }
                else if (result == Tasks.TaskResult.Success)
                {
                    ++CurrentTask;
                    CurrentTaskStartTime = Actor.Map.ElapsedTime;
                }

            }
            else if (DefaultBehaviors != null)
            {
                //pick random one and test?
                foreach (var behavior in DefaultBehaviors)
                {
                    KnownSenses |= BuildSenses((behavior.RequisiteSenses | behavior.RequisiteNotSenses) & ~KnownSenses);
                    if ((behavior.RequisiteSenses & KnownSenses) == behavior.RequisiteSenses &&
                        (behavior.RequisiteNotSenses & KnownSenses) == 0 &&
                        (float)Util.RandomGenerator.NextDouble() < behavior.QueueChance)
                        CurrentBehavior = behavior;
                }
            }
            //else return to default behavior after behavior completed?
        }

        public Senses BuildSenses(Senses testSenses)
        {
            var senses = Senses.None;

            var maxHealth = ((ActorClass)Actor.Class).MaxHealth;
            if (Actor.CurrentHealth >= maxHealth)
                senses |= Senses.FullHealth;
            else if (Actor.CurrentHealth <= maxHealth * 0.2f)
                senses |= Senses.LowHealth;

            if (Actor.WorldParent != null)
                senses |= Senses.Attached;

            if ((testSenses & Senses.LowAmmo) > 0 && Actor.Weapon != null)
            {
                if (Actor.Weapon is Weapons.GunInstance gi && gi.CurrentAmmo < gi.MaxAmmo * 0.2f)
                    senses |= Senses.LowAmmo;
            }

            if ((testSenses & (Senses.Supremecy | Senses.Outnumbered | Senses.AllyNearby | Senses.EnemyNearby)) > 0)
            {
                int allyCount = 0, enemyCount = 0;
                //this is probably expensive
                foreach (var sector in Actor.Map.TraceSectors(Actor.WorldPosition, Actor.WorldForward, SightRange))
                {
                    foreach (var ent in sector.entities)
                    {
                        if (ent == Actor || !(ent is ActorInstance actor))
                            continue;
                        if (Actor.IsAlliedWith(actor.Factions))
                            ++allyCount;
                        else
                            ++enemyCount;
                    }
                }

                if (enemyCount > 0)
                    senses |= Senses.EnemyNearby;
                if (allyCount > 0)
                    senses |= Senses.AllyNearby;

                if (enemyCount > allyCount + 3)
                    senses |= Senses.Outnumbered;
                else if (allyCount > enemyCount + 3)
                    senses |= Senses.Supremecy;
            }

            var testedSenses = testSenses & (Senses.HasTarget | Senses.TargetVisible | Senses.TargetCanSeeMe);
            if (testedSenses > 0 && Target != null)
            {
                // itemize?
                senses |= Senses.HasTarget;

                if ((testSenses & Senses.TargetVisible) > 0 &&
                    Actor.CanSee(Target.WorldPosition, (int)SightRange))
                    senses |= Senses.TargetVisible;

                //todo: Actor.CanSee

                if ((testSenses & Senses.TargetCanSeeMe) > 0 &&
                    Target.CanSee(Actor.WorldPosition, (int)SightRange))
                    senses |= Senses.TargetCanSeeMe; //trace tiles?
            }

            return senses & testSenses;
        }
    }

    /// <summary>
    /// What to do if a behavior's task fails
    /// </summary>
    public enum TaskFailureAction
    {
        RestartBehavior, //restart from the beginning
        CancelBehavior, //pick another behavior
        RetryTask, //retry the task
        Ignore, //continue to next task
    }

    /// <summary>
    /// A behavior is a list of tasks to run in sequence, and rules on when to queue the tasks
    /// </summary>
    public class Behavior : Takai.Data.INamedObject
    {
        public string Name { get; set; }
        public string File { get; set; }

        /// <summary>
        /// Senses required for this behavior to be picked. (Tasks are not affected by this)
        /// Leave blank to always pick.
        /// Ignored once this behavior is queued
        /// </summary>
        public Senses RequisiteSenses { get; set; }

        /// <summary>
        /// Senses that cannot be set for this behavior to be picked.
        /// </summary>
        public Senses RequisiteNotSenses { get; set; } //rename

        //failure senses?

        /// <summary>
        /// If this behavior is chosen, how likely is it to actually queue
        /// </summary>
        public float QueueChance { get; set; } = 0.5f;

        /// <summary>
        /// The tasks that comprise this behavior
        /// </summary>
        public List<Tasks.ITask> Tasks { get; set; }    

        public TaskFailureAction OnTaskFailure { get; set; }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(Name);
            sb.Append(":");
            foreach (var task in Tasks)
            {
                sb.Append(' ');
                sb.Append(task.GetType().Name);
            }
            return sb.ToString();
        }
    }
}

//generic counters for behaviors to use for limiting repeats (e.g. clone behavior can only run 4x)


//behavior influencers/sources/whatever (entering a region adds possible behaviors)