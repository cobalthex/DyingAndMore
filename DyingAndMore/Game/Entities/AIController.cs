using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai;

namespace DyingAndMore.Game.Entities
{
    //behavior constraints (cant run while dead/etc)

    public class AIController : Controller
    {
        public ActorInstance Target { get; set; }

        public List<Behavior> Behaviors { get; set; }

        protected Behavior currentBehavior;
        
        public override void Think(TimeSpan deltaTime)
        {
            if (Behaviors == null)
                return;

            //remove target on its death?

            foreach (var behavior in Behaviors)
            {
                if (behavior == currentBehavior)
                    continue; //re-evaluate?

                if (behavior.CanSchedule())
                {
                    var chance = (float)Util.RandomGenerator.NextDouble();
                    if (chance <= behavior.ScheduleChance)
                    {
                        currentBehavior = behavior;
                        currentBehavior.Reset();
                    }
                }
            }

            if (currentBehavior != null)
            {
                DebugPropertyDisplay.AddRow(Actor.ToString(), currentBehavior);
                currentBehavior.Think(deltaTime, this);
            }
        }
    }

    public class Behavior : Takai.Data.INamedObject
    {
        public string Name { get; set; }
        public string File { get; set; }

        public List<Tasks.ITask> Tasks { get; set; }
        protected int currentTask;

        public float ScheduleChance { get; set; } = 0.5f;

        //can pre-empt

        public virtual bool CanSchedule()
        {
            //todo: 
            if (Tasks == null || Tasks.Count < 1)
                return false;

            //todo

            return true;
        }

        public void Reset()
        {
            currentTask = 0;
        }

        public void Think(TimeSpan deltaTime, AIController ai)
        {
            if (currentTask >= Tasks.Count)
                return;

            var result = Tasks[currentTask].Think(deltaTime, ai);
            if (result == Entities.Tasks.TaskResult.Failure)
                LogBuffer.Append("Task failed"); //todo (reset to 0?)
            else if (result == Entities.Tasks.TaskResult.Success)
                ++currentTask;
        }

        public override string ToString()
        {
            if (Tasks == null || currentTask >= Tasks.Count)
                return $"{nameof(Behavior)}:{Name}";
            return $"{nameof(Behavior)}:{Name} {(Tasks[currentTask].GetType().Name)}";
        }

        //have AI controller run tasks?
    }
}