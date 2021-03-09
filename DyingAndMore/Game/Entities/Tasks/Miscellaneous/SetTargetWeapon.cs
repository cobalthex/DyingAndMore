using System;
using DyingAndMore.Game.Weapons;

namespace DyingAndMore.Game.Entities.Tasks.Miscellaneous
{
    [MiscellaneousTask]
    public struct SetTargetWeapon : ITask
    {
        public WeaponClass weapon;

        public TaskResult Think(TimeSpan deltaTime, AIController ai)
        {
            if (ai.Target == null)
                return TaskResult.Failure;

            ai.Target.Weapon = weapon.Instantiate();
            return TaskResult.Success;
        }
    }
}