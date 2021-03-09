using System;

namespace DyingAndMore.Game.Entities.Tasks
{
    public enum TaskResult
    {
        Continue, //rename? (correct conjugation)
        Failure,
        Success
    }

    /// <summary>
    /// An individual/atomic action a bot can perform.
    /// e.g. Pick target, move to target (set locomotor), kill target
    /// </summary>
    public interface ITask
    {
        TaskResult Think(TimeSpan deltaTime, AIController ai);
    }

    public class DefensiveTaskAttribute : Attribute { }
    public class MiscellaneousTaskAttribute : Attribute { }
    public class MovementTaskAttribute : Attribute { }
    public class OffensiveTaskAttribute : Attribute { }
    public class SquadTaskAttribute : Attribute { }
    public class TargetingTaskAttribute : Attribute { }

    public enum SetOperation
    {
        Replace,
        Union,
        Intersection,
        Outersection, //symmetric difference/xor
        Difference,
    }
}


/* ****** TODO ******

- todo: play animations
- todo: recategorize some of these

- teleport (possibly with delay, e.g. burrowing)
    still follows path/trajectory

- vent / tunnels ?
    can enter one, choose one near player to exit out of after some delay (based on distance/move speed?)


- actor shoots part of self, losing health, must recollect to regain health (non-recoverable energy used to fire)


- alt weapons/grenades
- berserk

- broadcast behavior/command
- ^ listen for command (tie to squads?)

--- ⬇ can be 'set behavior'
- defend squadleader
- heal squadleader

- join nearby squad

- promote self to leader (check if last leader died)

---- 
- auto create squad for player(s)

*/