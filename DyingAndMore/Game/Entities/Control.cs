using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    enum ControlValue
    {
        Off    = 0,
        On     = 1,
        Toggle = 2
    }

    class Control : Device
    {
        public bool PlayerOnly { get; set; } = false;

        public string Trigger
        {
            get { return trigger; }
            set
            {
                if (Map != null)
                {
                    Map.RemoveEventHandler(trigger, SetState);
                    Map.AddEventHandler(value, SetState);
                }

                trigger = value;
            }
        }
        private string trigger;

        public ControlValue TriggerValue { get; set; } = ControlValue.Toggle;

        public override void OnEntityCollision(Entity Collider, Vector2 Point, System.TimeSpan DeltaTime)
        {
            Map.TriggerEvent(Trigger, (int)TriggerValue);
        }

        void SetState(string Name, int Value)
        {
            //todo
        }

        public override void OnSpawn()
        {
            Map.RemoveEventHandler(Trigger, SetState);
            Map.AddEventHandler(Trigger, SetState);
            base.OnSpawn();
        }

        public override void OnDestroy()
        {
            Map.RemoveEventHandler(Trigger, SetState);
        }
    }
}
