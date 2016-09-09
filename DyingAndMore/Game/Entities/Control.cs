using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    enum ControlValue
    {
        Off = 0,
        On = 1,
        Toggle = 2
    }

    class Control : Entity
    {
        public string Trigger { get; set; } = null;
        public ControlValue Value { get; set; } = ControlValue.Off;
    }
}
