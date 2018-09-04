using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DyingAndMore.Game.Weapons;
using DyingAndMore.Game.Entities;

namespace DyingAndMoreTest
{
    [TestClass]
    public class GunInstanceTest
    {
        GunClass gunClass;
        GunInstance gunInstance;

        [TestInitialize]
        public void Initialize()
        {
            gunClass = new GunClass
            {
                AnimationClass = "Gun"
            };
            gunInstance = (GunInstance)gunClass.Instantiate();
            Assert.AreEqual(gunClass, gunInstance.Class);

            gunInstance.Actor = (ActorInstance)(new ActorClass
            {
                Animations = new System.Collections.Generic.Dictionary<string, Takai.Game.AnimationClass>
                {
                    ["GunChargeWeapon"] = new Takai.Game.AnimationClass(),
                    ["GunDischargeWeapon"] = new Takai.Game.AnimationClass()
                }
            }).Instantiate();
            var map = Takai.Game.MapClass.CreateCanvasMap(10, false);
            map.Spawn(gunInstance.Actor);
        }

        [TestMethod]
        public void ClassBoundOnInstantiate()
        {
            gunClass.MaxAmmo = 100;
            Assert.AreEqual(gunClass.MaxAmmo, gunInstance.MaxAmmo);
            Assert.AreNotEqual(gunClass.MaxAmmo, gunInstance.CurrentAmmo);

            gunInstance = (GunInstance)gunClass.Instantiate();
            Assert.AreEqual(gunInstance.Class, gunClass);
            Assert.AreEqual(gunInstance.CurrentAmmo, gunClass.MaxAmmo);
        }

        [TestMethod]
        public void SignleShotWeapon()
        {
            gunInstance.CurrentAmmo = 100;
            gunClass.MaxBursts = 1;
            gunClass.RoundsPerBurst = 1;
            gunClass.ProjectilesPerRound = 1;

            Assert.IsTrue(gunInstance.CanUse(TimeSpan.Zero));
            Assert.IsFalse(gunInstance.IsDepleted());
            Assert.IsFalse(gunInstance.Actor.IsPlayingAnimation("GunChargeWeapon"));

            gunInstance.TryUse();
            Assert.AreEqual(gunInstance.State, WeaponState.Charging);
            Assert.IsTrue(gunInstance.Actor.IsPlayingAnimation("GunChargeWeapon"));
            gunInstance.Think(TimeSpan.Zero);
        }
    }
}
