using System;
using System.Collections.Generic;
using Takai;

namespace DyingAndMore.Game.Entities
{
    [Flags]
    public enum Senses
    {
        None                = 0b00000000000000000000000000000000,

        //only one of these will be set
        //todo: revisit ^
        FullHealth          = 0b00000000000000000000000000000001, //>= max health
        [Takai.UI.DisplayName("Health < 75%")]
        HealthLessThan75Pct = 0b00000000000000000000000000000010,
        [Takai.UI.DisplayName("Health < 55%")]
        HealthLessThan50Pct = 0b00000000000000000000000000000100,
        [Takai.UI.DisplayName("Health < 25%")]
        HealthLessThan25Pct = 0b00000000000000000000000000001000,
        [Takai.UI.DisplayName("Health < 10%")]
        HealthLessThan10Pct = 0b00000000000000000000000000010000,

        DamageTaken         = 0b00000000000000000000000000100000, //+ HasTarget ?,
        LowAmmo             = 0b00000000000000000000000001000000, //+ HasTarget ?, //rename?

        HasTarget           = 0b00000000000000000000000010000000, //more allies than enemies
        TargetVisible       = 0b00000000000000000000000100000000, //more enemies than allies
        TargetCanSeeMe      = 0b00000000000000000000001000000000,

        Supremecy           = 0b00000000000000000000010000000000,
        Outnumbered         = 0b00000000000000000000100000000000, // >= 1
        AllyDied            = 0b00000000000000000001000000000000, // >= 1
        EnemyDied           = 0b00000000000000000010000000000000,
        AllyNearby          = 0b00000000000000000100000000000000,
        EnemyNearby         = 0b00000000000000001000000000000000,
        Attached            = 0b00000000000000010000000000000000,

        LastSquadUnit       = 0b00000000000000100000000000000000,
        SquadLeaderDead     = 0b00000000000001000000000000000000, //leader cannot be null
        //squad unit has low health

        // stuck: not moved for more than half a second (but force applied, or has locomotor, something like that)

        //target close/far
        //target fleeing?
    }

    public class AIController : Controller
    {
        public float SightRange { get; set; } = 1000;

        /// <summary>
        /// The current target actor for behaviors (e.g. enemy to shoot at or ally to follow)
        /// </summary>
        [Takai.Data.Serializer.AsReference, Takai.UI.Hidden]
        public ActorInstance Target { get; set; }

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

        /// <summary>
        /// The current 
        /// </summary>
        [Takai.UI.Hidden]
        public Behavior CurrentBehavior
        {
            get => _currentBehavior;
            set
            {
                _currentBehavior = value;
                CurrentTaskIndex = 0;
                CurrentTaskState = 0;
                CurrentTaskStartTime = Actor?.Map?.ElapsedTime ?? TimeSpan.Zero;
                //System.Diagnostics.Debug.WriteLine($"{Actor.Id} set behavior to {_currentBehavior?.Name}");
            }
        }
        private Behavior _currentBehavior;
        /// <summary>
        /// The current task running in the <see cref="CurrentBehavior"/>
        /// </summary>
        [Takai.UI.Hidden]
        public int CurrentTaskIndex { get; set; }

        public TimeSpan CurrentTaskStartTime { get; private set; }

        /// <summary>
        /// A simple state marker that can be used by tasks to mark internal state
        /// Reset to 0 when tasks change/fail
        /// </summary>
        /// <remarks>Manually editing this value may cause unintended results</remarks>
        [Takai.UI.Hidden]
        public int CurrentTaskState { get; set; } = 0;

        /// <summary>
        /// The current method this entity is moving.
        /// Null to stay in place
        /// </summary>
        [Takai.UI.Hidden]
        public ILocomotor CurrentLocomotor { get; set; } = null;

        public Senses KnownSenses { get; private set; } = 0;

        /// <summary>
        /// when to next check preemptive behaviors
        /// </summary>
        TimeSpan nextSenseCheck;

        float lastHealth;

        public void Reset()
        {
            CurrentBehavior = null;
            KnownSenses = 0;
            Target = null;
            nextSenseCheck = TimeSpan.Zero;
            lastHealth = Actor?.CurrentHealth ?? 0;
            //part of ICloneable?
        }

        public override string ToString()
        {
            if (CurrentBehavior == null)
                return "(No active behavior)";

            var sb = new System.Text.StringBuilder();
            sb.Append(CurrentBehavior.Name);
            if (CurrentTaskIndex < CurrentBehavior.Tasks.Count)
            {
                sb.Append(": ");
                sb.Append(CurrentBehavior.Tasks[CurrentTaskIndex].GetType().Name);
                //sb.Append("\n(");
                //sb.Append(KnownSenses);
                //sb.Append(")");
            }
            sb.Append($" (Target: {Target?.Id})");
            return sb.ToString();
        }

        public void MaybeInterruptLocomotion(bool shouldInterrupt = true)
        {
            if (shouldInterrupt)
                CurrentLocomotor = null;
        }

        public override void Think(TimeSpan deltaTime)
        {
            if (GameInstance.Current != null && !GameInstance.Current.GameplaySettings.isAiEnabled)
                return;

            // run every X frames?
            //remove target on its death?
            //reactions to events

            KnownSenses = 0;

            if (Target != null && Target.Map == null) //is alive?
                Target = null;

            //minimal runtime per behavior?
            if (Actor.Map.ElapsedTime >= nextSenseCheck && (PreemptiveBehaviors != null && PreemptiveBehaviors.Count > 0))
            {
                var behavior = PreemptiveBehaviors[Util.RandomGenerator.Next(0, PreemptiveBehaviors.Count)];
                KnownSenses |= BuildSenses((behavior.RequisiteSenses | behavior.RequisiteNotSenses) & ~KnownSenses);
                if ((behavior.RequisiteSenses & KnownSenses) == behavior.RequisiteSenses &&
                    (behavior.RequisiteNotSenses & KnownSenses) == 0 &&
                    (float)Util.RandomGenerator.NextDouble() < behavior.QueueChance)
                    CurrentBehavior = behavior; //increase next check if one is chosen? && verify not current task

                //programmable delay?
                nextSenseCheck = Actor.Map.ElapsedTime + TimeSpan.FromMilliseconds(200);
            }

            if (CurrentBehavior?.Tasks != null && CurrentTaskIndex < CurrentBehavior.Tasks.Count)
            {
                var result = CurrentBehavior.Tasks[CurrentTaskIndex].Think(deltaTime, this);
                if (result == TaskResult.Failure)
                {
                    System.Diagnostics.Debug.WriteLine($"{CurrentBehavior.Name}: Task {CurrentTaskIndex} failed with result {result}");

                    switch (CurrentBehavior.OnTaskFailure)
                    {
                        case TaskFailureAction.RestartBehavior:
                            CurrentTaskIndex = 0;
                            break;
                        case TaskFailureAction.CancelBehavior:
                            CurrentBehavior = null;
                            break;
                        case TaskFailureAction.RetryTask:
                            break;
                        case TaskFailureAction.Ignore:
                            ++CurrentTaskIndex;
                            break;
                    }
                    CurrentTaskStartTime = Actor.Map.ElapsedTime;
                    CurrentTaskState = 0;
                }
                else if (result == TaskResult.Success)
                {
                    ++CurrentTaskIndex;
                    //System.Diagnostics.Debug.WriteLine($"Advancing task to {CurrentTaskIndex}");
                    CurrentTaskState = 0;
                    CurrentTaskStartTime = Actor.Map.ElapsedTime;
                }

            }
            else if (DefaultBehaviors != null)
            {
                Behavior newBehavior = null;
                //pick random one and test?
                foreach (var behavior in DefaultBehaviors)
                {
                    KnownSenses |= BuildSenses((behavior.RequisiteSenses | behavior.RequisiteNotSenses) & ~KnownSenses);
                    if ((behavior.RequisiteSenses & KnownSenses) == behavior.RequisiteSenses &&
                        (behavior.RequisiteNotSenses & KnownSenses) == 0 &&
                        (float)Util.RandomGenerator.NextDouble() < behavior.QueueChance)
                        newBehavior = behavior;
                }
                CurrentBehavior = newBehavior;
            }

            if (CurrentLocomotor != null)
            {
                if (CurrentLocomotor.Move(deltaTime, this) == LocomotionResult.Finished)
                    CurrentLocomotor = null;
            }
        }

        public Senses BuildSenses(Senses testSenses)
        {
            var senses = Senses.None;

            var maxHealth = ((ActorClass)Actor.Class).MaxHealth;
            var curHealth = Actor.CurrentHealth;
            if (curHealth >= maxHealth)
                senses |= Senses.FullHealth;
            else if (Actor.CurrentHealth <= maxHealth * 0.10f)
                senses |= Senses.HealthLessThan10Pct;
            else if (Actor.CurrentHealth <= maxHealth * 0.25f)
                senses |= Senses.HealthLessThan25Pct;
            else if (Actor.CurrentHealth <= maxHealth * 0.50f)
                senses |= Senses.HealthLessThan50Pct;
            else if (Actor.CurrentHealth <= maxHealth * 0.75f)
                senses |= Senses.HealthLessThan75Pct;

            if (Actor.CurrentHealth < lastHealth)
                senses |= Senses.DamageTaken;
            lastHealth = Actor.CurrentHealth;

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

            testedSenses = testSenses & (Senses.LastSquadUnit | Senses.SquadLeaderDead);
            if (testedSenses > 0 && Actor.Squad != null)
            {
                if ((testSenses & Senses.LastSquadUnit) > 0 &&
                    Actor.Squad.Units.Count == 1)
                    senses |= Senses.LastSquadUnit;

                if ((testSenses & Senses.SquadLeaderDead) > 0 &&
                    (Actor.Squad.Leader != null && !Actor.Squad.Leader.IsAlive))
                    senses |= Senses.SquadLeaderDead;
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
        public List<ITask> Tasks { get; set; }    

        public TaskFailureAction OnTaskFailure { get; set; }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(Name);
            sb.Append(":");
            if (Tasks != null && Tasks.Count > 0)
            {
                foreach (var task in Tasks)
                {
                    sb.Append(' ');
                    sb.Append(task.GetType().Name);
                }
            }
            else
                sb.Append("[none]");
            return sb.ToString();
        }
    }
}

//generic counters for behaviors to use for limiting repeats (e.g. clone behavior can only run 4x)


//behavior influencers/sources/whatever (entering a region adds possible behaviors)