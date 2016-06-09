namespace Takai.Input
{
    /// <summary>
    /// A single input sequence
    /// </summary>
    public class InputSequence
    {
        /// <summary>
        /// The name of this sequence
        /// </summary>
        public string name;

        /// <summary>
        /// The sequence of buttons
        /// </summary>
        public Microsoft.Xna.Framework.Input.Buttons[] sequence;

        /// <summary>
        /// Is this move a basis for other sequences (like more complex sequences)
        /// </summary>
        public bool isBasis = false;

        public InputSequence(string Name, params Microsoft.Xna.Framework.Input.Buttons[] Sequence)
        {
            name = Name;
            sequence = Sequence;
        }

        public bool Check(InputSequencer inSeq)
        {
            if (inSeq.stream.Count < sequence.Length)
                return false;

            for (int i = 1; i <= sequence.Length; i++) //loop through stream backwards (from most recent) to see if the same
                if (inSeq.stream[inSeq.stream.Count - i] != sequence[sequence.Length - i])
                    return false;

            //no other move uses this so clear the stream
            if (!isBasis)
                inSeq.Clear();

            return true;
        }
    }
}