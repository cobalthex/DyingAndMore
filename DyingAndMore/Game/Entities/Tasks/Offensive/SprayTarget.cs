using System;

namespace DyingAndMore.Game.Entities.Tasks.Offensive
{
    [OffensiveTask]
    public struct SprayTarget : ITask
    {
        /// <summary>
        /// The total spray angle range to spray
        /// </summary>
        public float sprayAngle;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            //option to turn to -angle/2 first?
            //option to go ccw vs cw
            //step 1: move to -angle/2
            //step 2: spray to +angle

            return TaskResult.Success;
        }
    }

    //set all behaviors

    //possess/takeover?

    //Unpossess task (auto queued)?

    //possess target
    //alt weapons/grenades
    //berserk

    //issue commands
}