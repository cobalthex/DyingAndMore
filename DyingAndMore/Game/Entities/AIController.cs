using System;
using System.Collections.Generic;
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
        Supremecy       = 0b0000000000010000, //more allies than enemies
        Outnumbered     = 0b0000000000100000, //more enemies than allies
        HasTarget       = 0b0000000001000000,
        TargetVisible   = 0b0000000010000000 + HasTarget,
        //has parent?
    }

    public class AIController : Controller
    {
        /// <summary>
        /// The current target actor for behaviors (e.g. enemy to shoot at or ally to follow)
        /// </summary>
        public ActorInstance Target { get; set; }

        public TimeSpan TargetLastSeenTime { get; protected set; }
        public Vector2 TargetLastSeenPosition { get; protected set; }

        /// <summary>
        /// Possible behaviors to start this AI with
        /// </summary>
        public List<Behavior> DefaultBehaviors { get; set; }

        public Dictionary<ActorBroadcast, Behavior> AllyBroadcasts { get; set; } //list of behaviors>?
        //conditions? (has line of sight, etc)

        public Dictionary<ActorBroadcast, Behavior> EnemyBroadcasts { get; set; } //list of behaviors>?

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
            }
        }
        private Behavior _currentBehavior;
        /// <summary>
        /// The current task running in the <see cref="CurrentBehavior"/>
        /// </summary>
        public int CurrentTask { get; set; }
        //task start time


        public override void Think(TimeSpan deltaTime)
        {
            //remove target on its death?

            // run every X frames?
            // events?
            var senses = BuildSenses();

            if (CurrentBehavior != null && CurrentTask < CurrentBehavior.Tasks.Count)
            {
                DebugPropertyDisplay.AddRow(Actor.ToString(), CurrentBehavior);

                var result = CurrentBehavior.Tasks[CurrentTask].Think(deltaTime, this);
                if (result == Tasks.TaskResult.Failure)
                    CurrentTask = 0;
                else if (result == Tasks.TaskResult.Success)
                    ++CurrentTask;

            }
            //else return to default behavior after behavior completed?
        }
        public Senses BuildSenses()
        {
            var senses = Senses.None;

            //calculate senses on demand

            var maxHealth = ((ActorClass)Actor.Class).MaxHealth;
            if (Actor.CurrentHealth >= maxHealth)
                senses |= Senses.FullHealth;
            else if (Actor.CurrentHealth <= maxHealth * 0.2f)
                senses |= Senses.LowHealth;

            if (Actor.Weapon != null)
            {
                if (Actor.Weapon is Weapons.GunInstance gi && gi.CurrentAmmo < gi.MaxAmmo * 0.2f)
                    senses |= Senses.LowAmmo;
            }

            //outnumbered, supremecy

            if (Target != null)
                senses |= Senses.HasTarget;

            //target visible

            return senses;
        }
    }

    public class Behavior : Takai.Data.INamedObject
    {
        public string Name { get; set; }
        public string File { get; set; }

        /// <summary>
        /// The tasks in this behavior to run, in order.
        /// </summary>
        public List<Tasks.ITask> Tasks { get; set; }
    }
}