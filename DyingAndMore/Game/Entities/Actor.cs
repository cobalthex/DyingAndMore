﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai.Game;
using Takai.Graphics;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// Available factions. Work as bit flags (one actor can have multiple factions)
    /// </summary>
    [System.Flags]
    enum Factions
    {
        None        = 0,
        Player      = (1 << 0),
        Enemy       = (1 << 1),
        Powerup     = (1 << 2),

        Virus       = (1 << 24),

        Common      = (1 << 56),
        //auto-immune
        //cancerous

        //max = 1 << 63
    }

    class ActorClass : EntityClass
    {
        /// <summary>
        /// The default maximum allowed health of the entity (overhealing allowed)
        /// </summary>
        /// <remarks>Whenever this value is modified, the difference is added to current health</remarks>
        public int MaxHealth { get; set; }

        public float FieldOfView { get; set; } = MathHelper.PiOver4 * 3;

        public float MoveForce { get; set; }

        //inherited
        public Range<float> MaxSpeed { get; set; }
        public Weapons.WeaponClass DefaultWeapon { get; set; } = null;
        public Factions DefaultFaction { get; set; } = Factions.None;
        public Controller DefaultController { get; set; } = null;

        public override EntityInstance Create()
        {
            return new ActorInstance(this);
        }
    }

    class ActorInstance : EntityInstance
    {
        public override EntityClass Class
        {
            get => base.Class;
            set
            {
                System.Diagnostics.Contracts.Contract.Assert(value is ActorClass);
                base.Class = value;
                _Class = value as ActorClass;
            }
        }
        private ActorClass _Class;

        /// <summary>
        /// The current faction. Typically used by the AI to determine enemies
        /// </summary>
        /// <remarks>0 is any/no faction</remarks>
        public Factions Faction { get; set; } = Factions.None;

        /// <summary>
        /// The current controller over this actor (null for none)
        /// </summary>
        /// <remarks>The controller's actor is automatically updated when this is set</remarks>
        public Controller Controller
        {
            get { return controller; }
            set
            {
                if (controller != null)
                    controller.actor = null;

                controller = value;
                if (controller != null)
                    controller.actor = this;
            }
        }
        private Controller controller;

        /// <summary>
        /// The current health of the actor
        /// </summary>
        public int CurrentHealth { get; set; }

        private Vector2 lastVelocity;

        #region Inherited

        public float MaxSpeed { get; set; }

        public Weapons.WeaponInstance Weapon { get; set; } = null;

        #endregion

        public ActorInstance() { }
        public ActorInstance(ActorClass @class)
            : base(@class)
        {
            MaxSpeed = RandomRange.Next(_Class.MaxSpeed);
            CurrentHealth = _Class.MaxHealth;
            Weapon = _Class.DefaultWeapon?.Create();
            Faction = _Class.DefaultFaction;
            Controller = _Class.DefaultController;
        }

        public override void Think(System.TimeSpan DeltaTime)
        {
            Controller?.Think(DeltaTime);

            //todo: move to physics
            var vel = Velocity;

            if (vel == lastVelocity)
                vel = Vector2.Lerp(vel, Vector2.Zero, 10 * (float)DeltaTime.TotalSeconds);
            if (System.Math.Abs(vel.X) < 0.01f)
                vel.X = 0;
            if (System.Math.Abs(vel.Y) < 0.01f)
                vel.Y = 0;

            Velocity = vel;
            lastVelocity = Velocity;

            if (CurrentHealth <= 0 && !State.Is(EntStateId.Dead))
                State.Transition(EntStateId.Dead);

            base.Think(DeltaTime);
        }

        public override void OnEntityCollision(EntityInstance Collider, Vector2 Point, System.TimeSpan DeltaTime)
        {
            if (Collider is ActorInstance actor)
            {
            }
        }

        public void FireWeapon()
        {
            Weapon?.Fire(this);
        }

        public void Accelerate(Vector2 direction)
        {
            var vel = Velocity + (direction * _Class.MoveForce);
            var lSq = vel.LengthSquared();
            if (lSq > MaxSpeed * MaxSpeed)
                vel = (vel / (float)System.Math.Sqrt(lSq)) * MaxSpeed;
            Velocity = vel;
        }

        public void MoveTo(Vector2 newPosition, Vector2 newDirection)
        {
            throw new System.NotImplementedException();//todo
        }

        #region Helpers

        /// <summary>
        /// Can this actor see a point given its field of view?
        /// </summary>
        /// <param name="Point">The point to check</param>
        /// <returns>True if this entity is facing Point</returns>
        public bool CanSee(Vector2 Point)
        {
            var diff = Point - Position;
            diff.Normalize();

            var dot = Vector2.Dot(Direction, diff);

            return (dot > (1 - (_Class.FieldOfView / MathHelper.Pi)));
        }

        /// <summary>
        /// Is this entity behind another (The other entity cannot see this one)
        /// </summary>
        /// <param name="Ent">The entity to check</param>
        /// <returns>True if this entity is behind Ent</returns>
        public bool IsBehind(ActorInstance Ent)
        {
            var diff = Ent.Position - Position;
            diff.Normalize();

            var dot = Vector2.Dot(diff, Ent.Direction);
            return (dot > (_Class.FieldOfView / MathHelper.Pi) - 1);
        }

        #endregion
    }
}
