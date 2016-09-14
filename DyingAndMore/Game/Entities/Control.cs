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

        public string Trigger { get; set; } = null;
        public ControlValue TriggerValue { get; set; } = ControlValue.Toggle;

        public override void OnEntityCollision(Entity Collider, Vector2 Point)
        {
            Map.TriggerEvent(Trigger, (int)TriggerValue);
        }
        public override void OnSpawn()
        {
            if (!string.IsNullOrEmpty(PowerGroup))
            {
                TriggeredEvent evt = (string Name, int Value) =>
                {
                    CurrentState = CurrentState == EntState.Idle ? EntState.Pressed : EntState.Idle;
                };

                //todo: create function for this
                if (!Map.EventHandlers.ContainsKey(PowerGroup))
                    Map.EventHandlers[PowerGroup] = evt;
                else
                    Map.EventHandlers[PowerGroup] += evt;
            }

            base.OnSpawn();
        }
    }
}
