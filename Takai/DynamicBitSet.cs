using System;

using TStorage = System.UInt32;

namespace Takai
{
    /// <summary>
    /// A bitset that allows registering and reserving of slots, allowing a dynamic style of flags enum
    /// </summary>
    public class DynamicBitSet32
    {
        private const int NBits = 32;

        private TStorage availability;
        public TStorage Slots { get; private set; }

        //public byte SlotsInUse => BitOperations.PopCount(availability);

        public DynamicBitSet32(byte preregisteredSlots = 0)
        {
            if (preregisteredSlots >= NBits)
                throw new ArgumentOutOfRangeException(nameof(preregisteredSlots));

            Slots = 0;
            availability = ((TStorage)1 << preregisteredSlots) - 1;
        }

        /// <summary>
        /// Attempt to register a slot
        /// </summary>
        /// <returns>The slot index, or null if full</returns>
        public byte? RegisterSlot()
        {
            // todo: replace with first bit set (can use least bit set fn)
            byte? slot = ctz(~availability);
            if (!slot.HasValue)
                slot = 0;

            availability |= ((TStorage)1 << slot.Value);
            Slots &= ~((TStorage)1 << slot.Value);
            return slot;
        }

        public void UnregisterSlot(byte slot)
        {
            if (slot >= NBits)
                throw new ArgumentOutOfRangeException(nameof(slot));

            availability &= ~((TStorage)1 << slot);
        }

        /// <summary>
        /// Get or set a value in this bitset.
        /// Performs bounds checks but does not validate if a slot is in use
        /// </summary>
        /// <param name="slot">The index to check. Returned from RegisterSlot()</param>
        /// <returns>True if thie bit is set, false otherwise</returns>
        public bool this[byte slot]
        {
            get
            {
                if (slot >= NBits)
                    throw new ArgumentOutOfRangeException(nameof(slot));

                return (Slots & ((TStorage)1 << slot)) > 0;
            }
            set
            {
                if (slot >= NBits)
                    throw new ArgumentOutOfRangeException(nameof(slot));

                var bits = ((TStorage)1 << slot);
                if (value == true)
                    Slots |= bits;
                else
                    Slots &= ~bits;
            }
        }

        /// <summary>
        /// Set a slot
        /// </summary>
        /// <param name="slot">The slot to change</param>
        /// <param name="value">The value of the slot to set to</param>
        /// <returns>Whether or not the slot was modified</returns>
        public bool SetSlot(byte slot, bool value)
        {
            var bits = ((TStorage)1 << slot);
            var last = (Slots & bits);

            if (value == true)
                Slots |= bits;
            else
                Slots &= ~bits;

            return last != (Slots & bits);
        }

        /// <summary>
        /// Toggle a slot
        /// </summary>
        /// <param name="slot">The slot to toggle</param>
        /// <returns>The new slot value</returns>
        public bool ToggleSlot(byte slot)
        {
            var bits = ((TStorage)1 << slot);
            Slots ^= bits;
            return (Slots & bits) > 0;
        }

        private static byte? ctz(TStorage n)
        {
            if (n == 0)
                return null;

            byte t = 1;
            byte r = 0;
            while ((n & t) == 0)
            {
                t <<= 1;
                r += 1;
            }
            return r;
        }

        public override string ToString()
        {
            // todo: display spaces for unused slots
            return Convert.ToString(Slots, 2);
        }
    }
}
